using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK._3PDB.Standalone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteForParticipantHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ParticipantHistories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ParticipantHistories");
        }
    }
}
