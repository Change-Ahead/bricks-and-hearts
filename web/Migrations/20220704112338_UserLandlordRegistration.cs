using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class UserLandlordRegistration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LandlordId",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Landlord",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Landlord",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Landlord",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Landlord",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_User_LandlordId",
                table: "User",
                column: "LandlordId",
                unique: true,
                filter: "[LandlordId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Landlord_LandlordId",
                table: "User",
                column: "LandlordId",
                principalTable: "Landlord",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Landlord_LandlordId",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_LandlordId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "LandlordId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Landlord");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Landlord");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Landlord");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Landlord");
        }
    }
}
