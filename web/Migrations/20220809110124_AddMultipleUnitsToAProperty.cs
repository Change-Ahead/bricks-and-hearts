using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AddMultipleUnitsToAProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OccupiedUnits",
                table: "Property",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalUnits",
                table: "Property",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "OccupiedUnits",
                table: "Property",
                sql: "[OccupiedUnits] <= [TotalUnits]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "OccupiedUnits",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "OccupiedUnits",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "TotalUnits",
                table: "Property");
        }
    }
}
