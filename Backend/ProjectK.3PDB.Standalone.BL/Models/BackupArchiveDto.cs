using ProjectK._3PDB.Standalone.Infrastructure.Entities;

namespace ProjectK._3PDB.Standalone.BL.Models
{
    /// <summary>
    /// Portable, schema-versioned snapshot of the whole database. Unlike the CSV
    /// export it preserves participant keys, change history, soft-deleted rows and
    /// application config, so it round-trips faithfully across app versions.
    /// </summary>
    public class BackupArchiveDto
    {
        /// <summary>Bumped when the archive shape changes in a non-backward-compatible way.</summary>
        public int SchemaVersion { get; set; } = 1;

        public DateTime ExportedAt { get; set; }

        /// <summary>Assembly version that produced the archive (informational).</summary>
        public string? AppVersion { get; set; }

        /// <summary>All participants including soft-deleted, each with nested history.</summary>
        public List<Participant> Participants { get; set; } = [];

        /// <summary>Application configuration rows (e.g. window title suffix).</summary>
        public List<AppConfig> Configs { get; set; } = [];
    }
}
