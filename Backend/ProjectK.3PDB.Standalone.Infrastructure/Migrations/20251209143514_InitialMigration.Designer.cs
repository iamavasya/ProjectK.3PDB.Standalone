using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProjectK._3PDB.Standalone.Infrastructure.Context;

#nullable disable

namespace ProjectK._3PDB.Standalone.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20251209143514_InitialMigration")]
    partial class InitialMigration
    {
        /        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.11");

            modelBuilder.Entity("ProjectK._3PDB.Standalone.Infrastructure.Entities.Participant", b =>
                {
                    b.Property<Guid>("ParticipantKey")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("BirthDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsFormFilled")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsMotivationLetterWritten")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsProbeContinued")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsProbeFrozen")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsProbeOpen")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Kurin")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Notes")
                        .HasColumnType("TEXT");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("ProbeOpenDate")
                        .HasColumnType("TEXT");

                    b.HasKey("ParticipantKey");

                    b.ToTable("Participants");
                });

            modelBuilder.Entity("ProjectK._3PDB.Standalone.Infrastructure.Entities.ParticipantHistory", b =>
                {
                    b.Property<Guid>("ParticipantHistoryKey")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ChangedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("NewValue")
                        .HasColumnType("TEXT");

                    b.Property<string>("OldValue")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ParticipantKey")
                        .HasColumnType("TEXT");

                    b.Property<string>("PropertyName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("ParticipantHistoryKey");

                    b.HasIndex("ParticipantKey");

                    b.ToTable("ParticipantHistories");
                });

            modelBuilder.Entity("ProjectK._3PDB.Standalone.Infrastructure.Entities.ParticipantHistory", b =>
                {
                    b.HasOne("ProjectK._3PDB.Standalone.Infrastructure.Entities.Participant", "Participant")
                        .WithMany("History")
                        .HasForeignKey("ParticipantKey")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Participant");
                });

            modelBuilder.Entity("ProjectK._3PDB.Standalone.Infrastructure.Entities.Participant", b =>
                {
                    b.Navigation("History");
                });
#pragma warning restore 612, 618
        }
    }
}
