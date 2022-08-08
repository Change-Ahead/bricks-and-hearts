using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AlterTenantProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Tenant");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Tenant",
                newName: "Name");

            migrationBuilder.AddColumn<bool>(
                name: "HousingBenefits",
                table: "Tenant",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HousingBenefits",
                table: "Tenant");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Tenant",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Tenant",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Tenant",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
