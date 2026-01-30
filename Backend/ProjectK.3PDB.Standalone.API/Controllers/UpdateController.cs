using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK._3PDB.Standalone.BL.Services;

namespace ProjectK._3PDB.Standalone.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateController : ControllerBase
    {
        private readonly UpdateService _updateService;

        public UpdateController(UpdateService updateService)
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
        public async Task<IActionResult> GetCurrentVersion()
        {
            var version = UpdateService.GetCurrentVersion();
            return Ok(new { version });
        }
    }
}
