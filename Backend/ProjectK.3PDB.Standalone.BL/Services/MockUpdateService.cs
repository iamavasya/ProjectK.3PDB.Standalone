using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectK._3PDB.Standalone.BL.Interfaces;

namespace ProjectK._3PDB.Standalone.BL.Services
{
    /// <summary>
    /// Test/E2E stand-in for <see cref="UpdateService"/>. It never touches Velopack or GitHub;
    /// instead it serves a configurable current version, an optional "available" version and
    /// per-version release notes, all controllable at runtime via <see cref="IMockUpdateControl"/>.
    /// Seeded from environment variables: MOCK_CURRENT_VERSION, MOCK_NEW_VERSION, MOCK_RELEASE_NOTES.
    /// </summary>
    public class MockUpdateService : IUpdateService, IMockUpdateControl
    {
        private readonly ILogger<MockUpdateService> _logger;
        private readonly object _lock = new();
        private readonly Dictionary<string, string> _releaseNotes = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _defaultNotes;

        private string _currentVersion;
        private string? _availableNewVersion;

        public MockUpdateService(IConfiguration config, ILogger<MockUpdateService> logger)
        {
            _logger = logger;

            _currentVersion = Blank(config["MOCK_CURRENT_VERSION"]) ?? "1.0.0";
            _availableNewVersion = Blank(config["MOCK_NEW_VERSION"]);
            _defaultNotes = Blank(config["MOCK_RELEASE_NOTES"])
                ?? "## Що нового\n\nТестовий ченджлог (mock).";

            if (_availableNewVersion != null)
            {
                _releaseNotes[_availableNewVersion] = _defaultNotes;
            }

            _logger.LogInformation(
                "MockUpdateService initialized. Current={Current} Available={Available}",
                _currentVersion, _availableNewVersion);
        }

        // ---- IUpdateService ----

        public Task<string?> CheckForUpdatesAsync()
        {
            lock (_lock)
            {
                var hasUpdate = _availableNewVersion != null
                    && !string.Equals(_availableNewVersion, _currentVersion, StringComparison.OrdinalIgnoreCase);
                return Task.FromResult(hasUpdate ? _availableNewVersion : null);
            }
        }

        public Task DownloadUpdateAsync() => Task.CompletedTask;

        public void ApplyAndRestart()
        {
            // Simulate the restart taking effect: the app now runs the new version.
            lock (_lock)
            {
                if (_availableNewVersion != null)
                {
                    _currentVersion = _availableNewVersion;
                    _logger.LogInformation("MockUpdateService applied update. Now running {Version}", _currentVersion);
                }
            }
        }

        public string GetCurrentVersion()
        {
            lock (_lock) { return _currentVersion; }
        }

        public Task<string> GetReleaseNotes(string version)
        {
            lock (_lock)
            {
                return Task.FromResult(_releaseNotes.TryGetValue(version, out var notes) ? notes : _defaultNotes);
            }
        }

        // ---- IMockUpdateControl ----

        public void SetCurrentVersion(string version)
        {
            lock (_lock) { _currentVersion = version; }
        }

        public void SetAvailableNewVersion(string? version)
        {
            lock (_lock) { _availableNewVersion = Blank(version); }
        }

        public void SetReleaseNotes(string version, string notes)
        {
            lock (_lock) { _releaseNotes[version] = notes; }
        }

        public void SimulateUpdate(string toVersion, string? notes)
        {
            lock (_lock)
            {
                if (notes != null)
                {
                    _releaseNotes[toVersion] = notes;
                }
                _currentVersion = toVersion;
                _availableNewVersion = toVersion; // already applied -> no pending update
            }
        }

        public MockUpdateState GetState()
        {
            lock (_lock)
            {
                return new MockUpdateState(_currentVersion, _availableNewVersion, _releaseNotes.Keys.ToList());
            }
        }

        private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
