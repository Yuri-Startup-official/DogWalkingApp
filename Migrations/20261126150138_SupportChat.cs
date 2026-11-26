using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DogWalkingApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class SupportChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "support_conversations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    requester_id = table.Column<long>(type: "bigint", nullable: false),
                    assigned_manager_id = table.Column<long>(type: "bigint", nullable: true),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    topic = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_conversations", x => x.id);
                    table.ForeignKey(
                        name: "FK_support_conversations_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_support_conversations_users_assigned_manager_id",
                        column: x => x.assigned_manager_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_support_conversations_users_requester_id",
                        column: x => x.requester_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "support_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conversation_id = table.Column<long>(type: "bigint", nullable: false),
                    sender_id = table.Column<long>(type: "bigint", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_support_messages_support_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "support_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_support_messages_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_support_conversations_assigned_manager_id",
                table: "support_conversations",
                column: "assigned_manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_support_conversations_order_id",
                table: "support_conversations",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_support_conversations_requester_id_updated_at",
                table: "support_conversations",
                columns: new[] { "requester_id", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_support_conversations_status_updated_at",
                table: "support_conversations",
                columns: new[] { "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_support_messages_conversation_id_id",
                table: "support_messages",
                columns: new[] { "conversation_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_support_messages_sender_id",
                table: "support_messages",
                column: "sender_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_messages");

            migrationBuilder.DropTable(
                name: "support_conversations");
        }
    }
}
