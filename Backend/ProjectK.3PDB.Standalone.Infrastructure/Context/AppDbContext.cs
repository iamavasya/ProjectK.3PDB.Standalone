using Microsoft.EntityFrameworkCore;
using ProjectK._3PDB.Standalone.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK._3PDB.Standalone.Infrastructure.Context;

public class AppDbContext : DbContext
{
    public DbSet<Participant> Participants { get; set; }
    public DbSet<ParticipantHistory> ParticipantHistories { get; set; }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Participant>()
            .HasKey(p => p.ParticipantKey);
        modelBuilder.Entity<Participant>()
            .HasMany(p => p.History)
            .WithOne(h => h.Participant)
            .HasForeignKey(h => h.ParticipantKey)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ParticipantHistory>()
            .HasKey(h => h.ParticipantHistoryKey);
    }
}
