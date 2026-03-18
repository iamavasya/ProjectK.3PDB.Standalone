using System.Threading.Tasks;

namespace ProjectK._3PDB.Standalone.BL.Interfaces
{
    public interface IUpdateService
    {
        Task<string?> CheckForUpdatesAsync();
        Task DownloadUpdateAsync();
        void ApplyAndRestart();
        string GetCurrentVersion();
        Task<string> GetReleaseNotes(string version);
    }
}
