using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace ProjectK._3PDB.Standalone.BL.Services
{
    public class UpdateService
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
            mgr.ApplyUpdatesAndRestart(_foundUpdate.TargetFullRelease);
        }

        public static string GetCurrentVersion()
        {
            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
            return version ?? "0.0.0";
        }
    }
}
