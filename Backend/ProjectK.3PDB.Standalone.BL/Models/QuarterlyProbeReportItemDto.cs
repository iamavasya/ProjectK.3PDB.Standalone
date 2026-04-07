namespace ProjectK._3PDB.Standalone.BL.Models;

public class QuarterlyProbeReportItemDto
{
    public Guid? ParticipantHistoryKey { get; set; }
    public Guid ParticipantKey { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? Kurin { get; set; }
    public DateTime? ProbeOpenDate { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
