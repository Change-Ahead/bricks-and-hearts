using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AddDisabledToLandlord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Disabled",
                table: "Landlord",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Disabled",
                table: "Landlord");
        }
    }
}
