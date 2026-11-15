using System;
using DogWalkingApp.Api.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogWalkingApp.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261115190000_AddServiceBookingDetails")]
public partial class AddServiceBookingDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Date",
            table: "Orders",
            newName: "StartAt");

        migrationBuilder.AddColumn<int>(
            name: "DurationMinutes",
            table: "Orders",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "EndAt",
            table: "Orders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReportText",
            table: "Orders",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ServiceType",
            table: "Orders",
            type: "text",
            nullable: false,
            defaultValue: "Walk");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DurationMinutes", table: "Orders");
        migrationBuilder.DropColumn(name: "EndAt", table: "Orders");
        migrationBuilder.DropColumn(name: "ReportText", table: "Orders");
        migrationBuilder.DropColumn(name: "ServiceType", table: "Orders");

        migrationBuilder.RenameColumn(
            name: "StartAt",
            table: "Orders",
            newName: "Date");
    }
}
