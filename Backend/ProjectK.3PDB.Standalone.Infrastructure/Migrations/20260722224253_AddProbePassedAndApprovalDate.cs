using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK._3PDB.Standalone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProbePassedAndApprovalDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDate",
                table: "Participants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProbePassed",
                table: "Participants",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalDate",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "IsProbePassed",
                table: "Participants");
        }
    }
}
