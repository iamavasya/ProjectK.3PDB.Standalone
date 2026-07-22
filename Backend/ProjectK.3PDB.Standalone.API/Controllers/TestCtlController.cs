using Microsoft.AspNetCore.Mvc;
using ProjectK._3PDB.Standalone.BL.Interfaces;

namespace ProjectK._3PDB.Standalone.API.Controllers
{
    /// <summary>
    /// Test-only control surface for driving the mocked update flow (version + changelog).
    /// Active only when mock-update mode is enabled; otherwise every action returns 404.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestCtlController : ControllerBase
    {
        private readonly IMockUpdateControl? _control;

        // IEnumerable so the controller resolves even when the mock control is not registered.
        public TestCtlController(IEnumerable<IMockUpdateControl> controls)
        {
            _control = controls.FirstOrDefault();
        }

        [HttpGet("state")]
        public IActionResult State()
            => _control is null ? Disabled() : Ok(_control.GetState());

        [HttpPost("version")]
        public IActionResult SetVersion([FromBody] VersionRequest request)
        {
            if (_control is null) return Disabled();
            _control.SetCurrentVersion(request.Version);
            return Ok(_control.GetState());
        }

        [HttpPost("available")]
        public IActionResult SetAvailable([FromBody] VersionRequest request)
        {
            if (_control is null) return Disabled();
            _control.SetAvailableNewVersion(request.Version);
            return Ok(_control.GetState());
        }

        [HttpPost("release-notes")]
        public IActionResult SetReleaseNotes([FromBody] ReleaseNotesRequest request)
        {
            if (_control is null) return Disabled();
            _control.SetReleaseNotes(request.Version, request.Notes);
            return Ok(_control.GetState());
        }

        [HttpPost("simulate-update")]
        public IActionResult SimulateUpdate([FromBody] SimulateUpdateRequest request)
        {
            if (_control is null) return Disabled();
            _control.SimulateUpdate(request.ToVersion, request.Notes);
            return Ok(_control.GetState());
        }

        private IActionResult Disabled()
            => NotFound(new { error = "Test control is not enabled (set PROJECTK_MOCK_UPDATE=true)." });

        public record VersionRequest(string Version);
        public record ReleaseNotesRequest(string Version, string Notes);
        public record SimulateUpdateRequest(string ToVersion, string? Notes);
    }
}
