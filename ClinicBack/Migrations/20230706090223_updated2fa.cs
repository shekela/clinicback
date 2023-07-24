using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicBack.Migrations
{
    /// <inheritdoc />
    public partial class updated2fa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Security",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Security",
                table: "Clients");
        }
    }
}
