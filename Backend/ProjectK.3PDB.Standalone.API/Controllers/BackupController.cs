using Microsoft.AspNetCore.Mvc;
using ProjectK._3PDB.Standalone.BL.Interfaces;
using System.Diagnostics;

namespace ProjectK._3PDB.Standalone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackupController : ControllerBase
    {
        private readonly IBackupService _service;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<BackupController> _logger;

        public BackupController(
            IBackupService service,
            IHostApplicationLifetime lifetime,
            ILogger<BackupController> logger)
        {
            _service = service;
            _lifetime = lifetime;
            _logger = logger;
        }

        [HttpGet("download-db")]
        public async Task<IActionResult> DownloadDb()
        {
            var bytes = await _service.DownloadDbAsync();
            var fileName = $"3pdb_backup_{DateTime.Now:yyyy-MM-dd_HH-mm}.db";
            return File(bytes, "application/octet-stream", fileName);
        }

        [HttpPost("restore-db")]
        public async Task<IActionResult> RestoreDb(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не обрано або він порожній");

            if (!file.FileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Підтримуються тільки .db файли");

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _service.StageDbRestoreAsync(stream);
                return Ok(result);
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Помилка відновлення: {ex.Message}");
            }
        }

        [HttpGet("export-archive")]
        public async Task<IActionResult> ExportArchive()
        {
            var bytes = await _service.ExportArchiveAsync();
            var fileName = $"3pdb_archive_{DateTime.Now:yyyy-MM-dd_HH-mm}.zip";
            return File(bytes, "application/zip", fileName);
        }

        [HttpPost("import-archive")]
        public async Task<IActionResult> ImportArchive(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не обрано або він порожній");

            if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Підтримуються тільки .zip архіви");

            try
            {
                using var stream = file.OpenReadStream();
                await _service.ImportArchiveAsync(stream);
                return Ok(new { message = "Архів імпортовано успішно" });
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Помилка імпорту архіву: {ex.Message}");
            }
        }

        /// <summary>
        /// Relaunches the application so a staged raw-db restore is applied at startup.
        /// Responds first, then restarts on a short delay so the client receives the reply.
        /// </summary>
        [HttpPost("restart")]
        public IActionResult Restart()
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                RelaunchProcess();
                _lifetime.StopApplication();
            });

            return Ok(new { message = "Перезапуск застосунку..." });
        }

        private void RelaunchProcess()
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    _logger.LogWarning("Cannot determine process path; skipping relaunch.");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "--restarted",
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to relaunch the application after restore.");
            }
        }
    }
}
