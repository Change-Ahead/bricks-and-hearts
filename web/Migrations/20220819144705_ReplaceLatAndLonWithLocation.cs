using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class ReplaceLatAndLonWithLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "Lon",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "Lon",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Postcodes");

            migrationBuilder.DropColumn(
                name: "Lon",
                table: "Postcodes");

            migrationBuilder.AddColumn<string>(
                name: "PostcodeId",
                table: "Tenant",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostcodeId",
                table: "Property",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Postcodes",
                type: "geography",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_PostcodeId",
                table: "Tenant",
                column: "PostcodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Property_PostcodeId",
                table: "Property",
                column: "PostcodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Property_Postcodes_PostcodeId",
                table: "Property",
                column: "PostcodeId",
                principalTable: "Postcodes",
                principalColumn: "Postcode");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenant_Postcodes_PostcodeId",
                table: "Tenant",
                column: "PostcodeId",
                principalTable: "Postcodes",
                principalColumn: "Postcode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Property_Postcodes_PostcodeId",
                table: "Property");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenant_Postcodes_PostcodeId",
                table: "Tenant");

            migrationBuilder.DropIndex(
                name: "IX_Tenant_PostcodeId",
                table: "Tenant");

            migrationBuilder.DropIndex(
                name: "IX_Property_PostcodeId",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "PostcodeId",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "PostcodeId",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Postcodes");

            migrationBuilder.AddColumn<decimal>(
                name: "Lat",
                table: "Tenant",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Lon",
                table: "Tenant",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Tenant",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Lat",
                table: "Property",
                type: "decimal(12,9)",
                precision: 12,
                scale: 9,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Lon",
                table: "Property",
                type: "decimal(12,9)",
                precision: 12,
                scale: 9,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Property",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Lat",
                table: "Postcodes",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Lon",
                table: "Postcodes",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
