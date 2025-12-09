using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK._3PDB.Standalone.Infrastructure.Migrations
{
    /    public partial class InitialMigration : Migration
    {
        /        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    ParticipantKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Kurin = table.Column<int>(type: "INTEGER", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    IsProbeOpen = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMotivationLetterWritten = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFormFilled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsProbeContinued = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsProbeFrozen = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProbeOpenDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.ParticipantKey);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantHistories",
                columns: table => new
                {
                    ParticipantHistoryKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParticipantKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyName = table.Column<string>(type: "TEXT", nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantHistories", x => x.ParticipantHistoryKey);
                    table.ForeignKey(
                        name: "FK_ParticipantHistories_Participants_ParticipantKey",
                        column: x => x.ParticipantKey,
                        principalTable: "Participants",
                        principalColumn: "ParticipantKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantHistories_ParticipantKey",
                table: "ParticipantHistories",
                column: "ParticipantKey");
        }

        /        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParticipantHistories");

            migrationBuilder.DropTable(
                name: "Participants");
        }
    }
}
