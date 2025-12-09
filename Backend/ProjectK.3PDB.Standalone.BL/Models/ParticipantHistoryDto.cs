namespace ProjectK._3PDB.Standalone.BL.Models;

public class ParticipantHistoryDto
{
    public int Id { get; set; }
    public string PropertyName { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
}
