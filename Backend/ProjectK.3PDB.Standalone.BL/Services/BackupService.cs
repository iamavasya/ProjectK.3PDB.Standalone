using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectK._3PDB.Standalone.BL.Interfaces;
using ProjectK._3PDB.Standalone.BL.Models;
using ProjectK._3PDB.Standalone.Infrastructure.Context;
using ProjectK._3PDB.Standalone.Infrastructure.Paths;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectK._3PDB.Standalone.BL.Services
{
    public class BackupService : IBackupService
    {
        /// <summary>How many rotated <c>auto_*.db</c> snapshots to keep.</summary>
        private const int MaxAutoBackups = 10;

        private const string ArchiveEntryName = "backup.json";
        private const string SqliteMagic = "SQLite format 3";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        private readonly AppDbContext _context;
        private readonly AppPaths _paths;

        public BackupService(AppDbContext context, AppPaths paths)
        {
            _context = context;
            _paths = paths;
        }

        public async Task<byte[]> DownloadDbAsync()
        {
            var temp = Path.Combine(Path.GetTempPath(), $"3pdb_snapshot_{Guid.NewGuid():N}.db");
            try
            {
                await CreateSnapshotAsync(temp);
                return await File.ReadAllBytesAsync(temp);
            }
            finally
            {
                TryDelete(temp);
            }
        }

        public async Task<RestoreResultDto> StageDbRestoreAsync(Stream uploaded)
        {
            _paths.EnsureDirectories();

            var temp = Path.Combine(Path.GetTempPath(), $"3pdb_restore_{Guid.NewGuid():N}.db");
            try
            {
                await using (var fs = File.Create(temp))
                {
                    await uploaded.CopyToAsync(fs);
                }

                await ValidateSqliteDbAsync(temp);

                string? safetyBackup = null;
                if (File.Exists(_paths.DbPath))
                {
                    safetyBackup = Path.Combine(_paths.BackupsFolder, $"pre-restore_{Timestamp()}.db");
                    await CreateSnapshotAsync(safetyBackup);
                }

                TryDelete(_paths.PendingRestorePath);
                File.Move(temp, _paths.PendingRestorePath);

                return new RestoreResultDto
                {
                    RequiresRestart = true,
                    SafetyBackupPath = safetyBackup,
                };
            }
            finally
            {
                TryDelete(temp);
            }
        }

        public async Task<byte[]> ExportArchiveAsync()
        {
            var participants = await _context.Participants
                .Include(p => p.History)
                .AsNoTracking()
                .ToListAsync();

            var configs = await _context.AppConfigs
                .AsNoTracking()
                .ToListAsync();

            var archive = new BackupArchiveDto
            {
                SchemaVersion = 1,
                ExportedAt = DateTime.Now,
                AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(),
                Participants = participants,
                Configs = configs,
            };

            var json = JsonSerializer.SerializeToUtf8Bytes(archive, JsonOptions);

            using var memoryStream = new MemoryStream();
            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry(ArchiveEntryName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(json);
            }

            return memoryStream.ToArray();
        }

        public async Task ImportArchiveAsync(Stream uploaded)
        {
            var archive = await ReadArchiveAsync(uploaded);

            if (archive is null)
            {
                throw new InvalidDataException("Не вдалося прочитати архів.");
            }

            if (archive.SchemaVersion > 1)
            {
                throw new InvalidDataException($"Непідтримувана версія архіву: {archive.SchemaVersion}.");
            }

            // Safety snapshot of the current database before we overwrite anything.
            _paths.EnsureDirectories();
            await CreateSnapshotAsync(Path.Combine(_paths.BackupsFolder, $"pre-import_{Timestamp()}.db"));

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                // Children first, then parents (FK order).
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM ParticipantHistories;");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Participants;");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM AppConfigs;");

                foreach (var participant in archive.Participants)
                {
                    // Drop back-references so EF tracks the graph via the History collection only.
                    foreach (var history in participant.History)
                    {
                        history.Participant = null;
                    }

                    _context.Participants.Add(participant);
                }

                foreach (var config in archive.Configs)
                {
                    _context.AppConfigs.Add(config);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            });
        }

        public async Task CreateAutoBackupAsync()
        {
            _paths.EnsureDirectories();

            var target = Path.Combine(_paths.BackupsFolder, $"auto_{Timestamp()}.db");
            await CreateSnapshotAsync(target);

            RotateAutoBackups();
        }

        /// <summary>
        /// Produces a consistent single-file copy of the live database using
        /// <c>VACUUM INTO</c>, which reads a stable view even with the app connected.
        /// </summary>
        private async Task CreateSnapshotAsync(string targetPath)
        {
            // VACUUM INTO requires the destination file not to exist.
            TryDelete(targetPath);

            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Pooling=False so the ad-hoc connection releases the file handle on dispose
            // instead of lingering in the pool.
            await using var connection = new SqliteConnection($"Data Source={_paths.DbPath};Pooling=False");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"VACUUM INTO '{targetPath.Replace("'", "''")}';";
            await command.ExecuteNonQueryAsync();
        }

        private static async Task<BackupArchiveDto?> ReadArchiveAsync(Stream uploaded)
        {
            // Buffer to a seekable stream so ZipArchive can read the central directory.
            await using var buffer = new MemoryStream();
            await uploaded.CopyToAsync(buffer);
            buffer.Position = 0;

            using var zip = new ZipArchive(buffer, ZipArchiveMode.Read);
            var entry = zip.GetEntry(ArchiveEntryName)
                        ?? zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                throw new InvalidDataException("Архів не містить файлу backup.json.");
            }

            await using var entryStream = entry.Open();
            return await JsonSerializer.DeserializeAsync<BackupArchiveDto>(entryStream, JsonOptions);
        }

        private static async Task ValidateSqliteDbAsync(string path)
        {
            if (new FileInfo(path).Length == 0)
            {
                throw new InvalidDataException("Файл порожній.");
            }

            var header = new byte[16];
            await using (var fs = File.OpenRead(path))
            {
                var read = await fs.ReadAsync(header.AsMemory(0, header.Length));
                if (read < header.Length ||
                    Encoding.ASCII.GetString(header, 0, SqliteMagic.Length) != SqliteMagic)
                {
                    throw new InvalidDataException("Файл не є базою даних SQLite.");
                }
            }

            // Pooling=False so the handle to this temp file is freed on dispose, allowing
            // the subsequent File.Move of the staged database to succeed.
            await using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Participants';";
            var participantsExists = Convert.ToInt64(await command.ExecuteScalarAsync() ?? 0L) > 0;

            if (!participantsExists)
            {
                throw new InvalidDataException("У базі відсутня таблиця Participants — це не файл бази 3ПДБ.");
            }
        }

        private void RotateAutoBackups()
        {
            var stale = Directory.GetFiles(_paths.BackupsFolder, "auto_*.db")
                .OrderByDescending(f => f)
                .Skip(MaxAutoBackups)
                .ToList();

            foreach (var file in stale)
            {
                TryDelete(file);
            }
        }

        // Sortable timestamp: lexical order == chronological order.
        private static string Timestamp() => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup; ignore locked/temp files.
            }
        }
    }
}
