using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK._3PDB.Standalone.Infrastructure.Entities;

public class Participant
{
    public Guid ParticipantKey { get; set; }

    [Required]
    public string FullName { get; set; }
    public int? Kurin { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }

    public bool IsProbeOpen { get; set; }
    public bool IsMotivationLetterWritten { get; set; }
    public bool IsFormFilled { get; set; }
    public bool IsProbeContinued { get; set; }
    public bool IsProbeFrozen { get; set; }

    public DateTime? ProbeOpenDate { get; set;}
    public DateTime? BirthDate { get; set; }

    public string? Notes { get; set; }
    
    public List<ParticipantHistory> History { get; set; } = [];
}
