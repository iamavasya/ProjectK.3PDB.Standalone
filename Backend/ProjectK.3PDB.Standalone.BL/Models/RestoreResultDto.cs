namespace ProjectK._3PDB.Standalone.BL.Models
{
    /// <summary>
    /// Result of staging a raw <c>.db</c> restore. The actual file swap happens on
    /// the next startup because the live database file is locked while the app runs.
    /// </summary>
    public class RestoreResultDto
    {
        /// <summary>Always true for a raw-db restore: the app must restart to apply it.</summary>
        public bool RequiresRestart { get; set; } = true;

        /// <summary>Path of the automatic safety copy taken of the current database.</summary>
        public string? SafetyBackupPath { get; set; }
    }
}
