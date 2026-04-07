using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectK._3PDB.Standalone.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteForParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Participants",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Participants");
        }
    }
}
