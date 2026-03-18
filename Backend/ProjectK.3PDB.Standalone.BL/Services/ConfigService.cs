using Microsoft.EntityFrameworkCore;
using ProjectK._3PDB.Standalone.Infrastructure.Context;
using ProjectK._3PDB.Standalone.Infrastructure.Entities;

namespace ProjectK._3PDB.Standalone.BL.Services;

public class ConfigService
{
    private const int SingletonConfigKey = 1;
    private readonly AppDbContext _context;

    public ConfigService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetTitleSuffixAsync()
    {
        var config = await _context.AppConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AppConfigKey == SingletonConfigKey);

        return config?.TitleSuffix ?? string.Empty;
    }

    public async Task UpdateTitleSuffixAsync(string suffix)
    {
        var config = await _context.AppConfigs
            .FirstOrDefaultAsync(c => c.AppConfigKey == SingletonConfigKey);

        if (config is null)
        {
            config = new AppConfig
            {
                AppConfigKey = SingletonConfigKey,
                TitleSuffix = suffix
            };

            _context.AppConfigs.Add(config);
        }
        else
        {
            config.TitleSuffix = suffix;
        }

        await _context.SaveChangesAsync();
    }
}
