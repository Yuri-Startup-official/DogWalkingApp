using System;
using DogWalkingApp.Api.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DogWalkingApp.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261124120000_AddOrderApplicationsAndLocation")]
public partial class AddOrderApplicationsAndLocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Area",
            table: "Orders",
            type: "character varying(120)",
            maxLength: 120,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "Orders",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "LocationAddress",
            table: "Orders",
            type: "character varying(300)",
            maxLength: 300,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            name: "OrderApplications",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OrderId = table.Column<int>(type: "integer", nullable: false),
                WalkerId = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderApplications", application => application.Id);
                table.ForeignKey(
                    name: "FK_OrderApplications_Orders_OrderId",
                    column: application => application.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_OrderApplications_Users_WalkerId",
                    column: application => application.WalkerId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OrderApplications_OrderId_WalkerId",
            table: "OrderApplications",
            columns: new[] { "OrderId", "WalkerId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OrderApplications_WalkerId",
            table: "OrderApplications",
            column: "WalkerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OrderApplications");
        migrationBuilder.DropColumn(name: "Area", table: "Orders");
        migrationBuilder.DropColumn(name: "Description", table: "Orders");
        migrationBuilder.DropColumn(name: "LocationAddress", table: "Orders");
    }
}
