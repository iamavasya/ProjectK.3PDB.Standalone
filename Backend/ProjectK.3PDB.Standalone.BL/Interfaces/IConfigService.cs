using System.Threading.Tasks;

namespace ProjectK._3PDB.Standalone.BL.Interfaces
{
    public interface IConfigService
    {
        Task<string> GetTitleSuffixAsync();
        Task UpdateTitleSuffixAsync(string suffix);
    }
}
