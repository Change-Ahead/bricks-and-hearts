using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AddHasGuarantorAndChangeEETInTenants : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NotInEET",
                table: "Tenant",
                newName: "InEET");

            migrationBuilder.AddColumn<bool>(
                name: "HasGuarantor",
                table: "Tenant",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasGuarantor",
                table: "Tenant");

            migrationBuilder.RenameColumn(
                name: "InEET",
                table: "Tenant",
                newName: "NotInEET");
        }
    }
}
