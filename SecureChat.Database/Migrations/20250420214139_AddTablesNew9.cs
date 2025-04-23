using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureChat.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTablesNew9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModeCipher",
                table: "Chat",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Padding",
                table: "Chat",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModeCipher",
                table: "Chat");

            migrationBuilder.DropColumn(
                name: "Padding",
                table: "Chat");
        }
    }
}
