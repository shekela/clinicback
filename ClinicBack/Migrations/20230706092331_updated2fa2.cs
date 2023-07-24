using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicBack.Migrations
{
    /// <inheritdoc />
    public partial class updated2fa2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Security",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Security",
                table: "Admins",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Security",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Security",
                table: "Admins");
        }
    }
}
