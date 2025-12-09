namespace ProjectK._3PDB.Standalone.BL.Models;
public class ParticipantDto
{
    public Guid ParticipantKey { get; set; }
    public string FullName { get; set; }
    public int? Kurin { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }

    // Booleans
    public bool IsProbeOpen { get; set; }
    public bool IsMotivationLetterWritten { get; set; }
    public bool IsFormFilled { get; set; }
    public bool IsProbeContinued { get; set; }
    public bool IsProbeFrozen { get; set; }

    // Dates
    public DateTime? ProbeOpenDate { get; set; }
    public DateTime? BirthDate { get; set; }

    public string Notes { get; set; }

    public List<ParticipantHistoryDto> History { get; set; } = new();
}