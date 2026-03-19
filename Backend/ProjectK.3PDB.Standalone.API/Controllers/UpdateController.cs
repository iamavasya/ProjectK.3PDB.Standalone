using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK._3PDB.Standalone.BL.Interfaces;

namespace ProjectK._3PDB.Standalone.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateController : ControllerBase
    {
        private readonly IUpdateService _updateService;

        public UpdateController(IUpdateService updateService)
        {
            _updateService = updateService;
        }

        [HttpGet("check")]
        public async Task<IActionResult> Check()
        {
            var version = await _updateService.CheckForUpdatesAsync();
            return Ok(new { available = version != null, version = version });
        }

        [HttpPost("download")]
        public async Task<IActionResult> Download()
        {
            try
            {
                await _updateService.DownloadUpdateAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("apply")]
        public IActionResult Apply()
        {
            _updateService.ApplyAndRestart();
            return Ok();
        }

        [HttpGet("current-version")]
        public ActionResult GetCurrentVersion()
        {
            var version = _updateService.GetCurrentVersion();
            return Ok(new { version });
        }

        [HttpGet("readiness")]
        public ActionResult GetReadiness()
        {
            var version = _updateService.GetCurrentVersion();
            return Ok(new
            {
                ready = true,
                version,
                serverTimeUtc = DateTime.UtcNow
            });
        }

        [HttpGet("release-notes/{version}")]
        public async Task<IActionResult> GetReleaseNotes(string version)
        {
            var notes = await _updateService.GetReleaseNotes(version);
            return Ok(notes);
        }
    }
}
