using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AddLatLonToProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Lat",
                table: "Property",
                type: "decimal(12,9)",
                precision: 12,
                scale: 9,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Lon",
                table: "Property",
                type: "decimal(12,9)",
                precision: 12,
                scale: 9,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "Lon",
                table: "Property");
        }
    }
}
