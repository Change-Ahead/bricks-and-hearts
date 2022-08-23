using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class RenameTenantBooleans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Over35",
                table: "Tenant",
                newName: "Under35");

            migrationBuilder.RenameColumn(
                name: "ETT",
                table: "Tenant",
                newName: "NotInEET");

            migrationBuilder.RenameColumn(
                name: "AcceptsOver35",
                table: "Property",
                newName: "AcceptsUnder35");

            migrationBuilder.RenameColumn(
                name: "AcceptsNotEET",
                table: "Property",
                newName: "AcceptsNotInEET");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Under35",
                table: "Tenant",
                newName: "Over35");

            migrationBuilder.RenameColumn(
                name: "NotInEET",
                table: "Tenant",
                newName: "ETT");

            migrationBuilder.RenameColumn(
                name: "AcceptsUnder35",
                table: "Property",
                newName: "AcceptsOver35");

            migrationBuilder.RenameColumn(
                name: "AcceptsNotInEET",
                table: "Property",
                newName: "AcceptsNotEET");
        }
    }
}
