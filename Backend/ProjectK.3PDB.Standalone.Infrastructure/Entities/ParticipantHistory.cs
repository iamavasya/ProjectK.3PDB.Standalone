using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK._3PDB.Standalone.Infrastructure.Entities;

public class ParticipantHistory
{
    public Guid ParticipantHistoryKey { get; set; }

    public Guid ParticipantKey { get; set; }
    public string PropertyName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }

    public Participant? Participant { get; set; }
}
