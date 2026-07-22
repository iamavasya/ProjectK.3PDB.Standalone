using ProjectK._3PDB.Standalone.BL.Models;

namespace ProjectK._3PDB.Standalone.BL.Interfaces
{
    public interface IBackupService
    {
        /// <summary>Returns a consistent single-file snapshot of the live database.</summary>
        Task<byte[]> DownloadDbAsync();

        /// <summary>
        /// Validates and stages an uploaded raw <c>.db</c> file for a full replace.
        /// Takes a safety backup of the current database first; the swap is applied on restart.
        /// </summary>
        Task<RestoreResultDto> StageDbRestoreAsync(Stream uploaded);

        /// <summary>Returns a schema-versioned JSON/ZIP archive of all data.</summary>
        Task<byte[]> ExportArchiveAsync();

        /// <summary>
        /// Fully replaces all data with the contents of a JSON/ZIP archive
        /// (keys and history preserved). Takes a safety backup first.
        /// </summary>
        Task ImportArchiveAsync(Stream uploaded);

        /// <summary>Creates a rotated automatic snapshot in the backups folder.</summary>
        Task CreateAutoBackupAsync();
    }
}
