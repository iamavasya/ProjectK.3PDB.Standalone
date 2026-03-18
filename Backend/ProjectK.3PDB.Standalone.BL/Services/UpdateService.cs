using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK._3PDB.Standalone.BL.Interfaces;
using Velopack;
using Velopack.Sources;

namespace ProjectK._3PDB.Standalone.BL.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly string _githubUrl = "https://github.com/iamavasya/ProjectK.3PDB.Standalone";
        private UpdateInfo? _foundUpdate;

        public UpdateService(ILogger<UpdateService> logger)
        {
            _logger = logger;
        }

        public async Task<string?> CheckForUpdatesAsync()
        {
            try
            {
                var mgr = new UpdateManager(new GithubSource(_githubUrl, null, false));
                var updateInfo = await mgr.CheckForUpdatesAsync();

                if (updateInfo == null) return null;

                _foundUpdate = updateInfo;

                return updateInfo.TargetFullRelease.Version.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during update check");
                return null;
            }
        }

        public async Task DownloadUpdateAsync()
        {
            if (_foundUpdate == null)
            {
                throw new InvalidOperationException("Firstly check for updates.");
            }

            var mgr = new UpdateManager(new GithubSource(_githubUrl, null, false));
            await mgr.DownloadUpdatesAsync(_foundUpdate);
        }

        public void ApplyAndRestart()
        {
            if (_foundUpdate == null) return;

            var mgr = new UpdateManager(new GithubSource(_githubUrl, null, false));
            mgr.ApplyUpdatesAndRestart(_foundUpdate.TargetFullRelease, new[] {"--restarted"});
        }

        public string GetCurrentVersion()
        {
            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
            if (version == null)
            {
                return "0.0.0";
            }
            var parts = version.Split('.') ;
            return parts.Length >= 3 ? string.Join(".", parts[0], parts[1], parts[2]) : "0.0.0";
        }

        public async Task<string> GetReleaseNotes(string version)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "ProjectK.3PDB.Standalone");

                // Ensure tag format vX.Y.Z
                var tag = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
                
                var response = await client.GetAsync($"https://api.github.com/repos/iamavasya/ProjectK.3PDB.Standalone/releases/tags/{tag}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Release notes not found for tag {Tag}. Status: {Status}", tag, response.StatusCode);
                    return "Release notes unavailable.";
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("body", out var bodyElement))
                {
                    return bodyElement.GetString() ?? "No details provided.";
                }
                
                return "Release notes format error.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch release notes for version {Version}", version);
                return "Could not load release notes.";
            }
        }
    }
}
