using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class SetPostcodeAsKeyForPostcodeTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Postcodes",
                columns: table => new
                {
                    Postcode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Lat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Lon = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Postcodes", x => x.Postcode);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Postcodes");
        }
    }
}
