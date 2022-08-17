using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AddLatLongToTenant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Lat",
                table: "Tenant",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Lon",
                table: "Tenant",
                type: "decimal(18,2)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "Lon",
                table: "Tenant");
        }
    }
}
