using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AddTenantProfileToProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptsBenefits",
                table: "Property",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptsCouple",
                table: "Property",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptsFamily",
                table: "Property",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptsNotEET",
                table: "Property",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptsPets",
                table: "Property",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptsSingleTenant",
                table: "Property",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptsWithoutGuarantor",
                table: "Property",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptsBenefits",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "AcceptsCouple",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "AcceptsFamily",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "AcceptsNotEET",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "AcceptsPets",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "AcceptsSingleTenant",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "AcceptsWithoutGuarantor",
                table: "Property");
        }
    }
}
