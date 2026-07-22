namespace ProjectK._3PDB.Standalone.Infrastructure.Paths;

/// <summary>
/// Central provider for the well-known on-disk locations used by the application.
/// Registered as a singleton so both host startup (<c>Program.cs</c>) and the
/// backup service resolve the exact same paths.
/// </summary>
public class AppPaths
{
    /// <summary>File name of the live SQLite database.</summary>
    public const string DbFileName = "projectk_3pdb_standalone_v2.db";

    /// <summary>File name of the staged database awaiting a restart-time swap.</summary>
    public const string PendingRestoreFileName = "projectk_3pdb_standalone_v2.pending.db";

    public AppPaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var root = Path.Combine(localAppData, "ProjectK3PDB");

        DataFolder = Path.Combine(root, "data");
        BackupsFolder = Path.Combine(root, "backups");

        DbPath = Path.Combine(DataFolder, DbFileName);
        PendingRestorePath = Path.Combine(DataFolder, PendingRestoreFileName);
    }

    /// <summary>%LOCALAPPDATA%\ProjectK3PDB\data — holds the live database.</summary>
    public string DataFolder { get; }

    /// <summary>%LOCALAPPDATA%\ProjectK3PDB\backups — holds automatic and pre-restore snapshots.</summary>
    public string BackupsFolder { get; }

    /// <summary>Full path to the live SQLite database file.</summary>
    public string DbPath { get; }

    /// <summary>Full path where an uploaded database is staged before a restart-time swap.</summary>
    public string PendingRestorePath { get; }

    /// <summary>Ensures the data and backups directories exist.</summary>
    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(BackupsFolder);
    }
}
