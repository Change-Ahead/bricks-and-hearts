using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AddMembershipId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MembershipId",
                table: "Landlord",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MembershipId",
                table: "Landlord");
        }
    }
}
