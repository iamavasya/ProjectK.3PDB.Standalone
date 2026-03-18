using Microsoft.AspNetCore.Mvc;
using ProjectK._3PDB.Standalone.BL.Services;

namespace ProjectK._3PDB.Standalone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ConfigService _configService;

        public ConfigController(ConfigService configService)
        {
            _configService = configService;
        }

        [HttpGet("title-suffix")]
        public async Task<ActionResult<TitleSuffixResponse>> GetTitleSuffix()
        {
            var suffix = await _configService.GetTitleSuffixAsync();
            return Ok(new TitleSuffixResponse(suffix));
        }

        [HttpPost("title-suffix")]
        public async Task<IActionResult> UpdateTitleSuffix([FromBody] UpdateTitleSuffixRequest request)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            await _configService.UpdateTitleSuffixAsync(request.Suffix ?? string.Empty);
            return NoContent();
        }

        public sealed record TitleSuffixResponse(string Suffix);
        public sealed record UpdateTitleSuffixRequest(string? Suffix);
    }
}
