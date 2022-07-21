using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BricksAndHearts.Migrations
{
    public partial class AddNewFieldsToPropertyTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Property",
                newName: "TownOrCity");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Property",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Property",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine3",
                table: "Property",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "Property",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTime",
                table: "Property",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Property",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NumOfBedrooms",
                table: "Property",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Property",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PropertyType",
                table: "Property",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Rent",
                table: "Property",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "AddressLine3",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "County",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "CreationTime",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "NumOfBedrooms",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "PropertyType",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "Rent",
                table: "Property");

            migrationBuilder.RenameColumn(
                name: "TownOrCity",
                table: "Property",
                newName: "Address");
        }
    }
}
