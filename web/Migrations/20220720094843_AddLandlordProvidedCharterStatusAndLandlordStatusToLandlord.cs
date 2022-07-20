using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AddLandlordProvidedCharterStatusAndLandlordStatusToLandlord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LandlordProvidedCharterStatus",
                table: "Landlord",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LandlordStatus",
                table: "Landlord",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LandlordProvidedCharterStatus",
                table: "Landlord");

            migrationBuilder.DropColumn(
                name: "LandlordStatus",
                table: "Landlord");
        }
    }
}
