using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AlterCharterStatusFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LandlordProvidedCharterStatus",
                table: "Landlord");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LandlordProvidedCharterStatus",
                table: "Landlord",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
