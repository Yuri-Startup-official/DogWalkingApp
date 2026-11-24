using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DogWalkingApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    phone_verified = table.Column<bool>(type: "boolean", nullable: false),
                    rating_as_sitter = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    reviews_as_sitter = table.Column<int>(type: "integer", nullable: false),
                    rating_as_owner = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    reviews_as_owner = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pets",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    species = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    breed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    sex = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    sterilized = table.Column<bool>(type: "boolean", nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    medical_notes = table.Column<string>(type: "text", nullable: true),
                    medications = table.Column<string>(type: "text", nullable: true),
                    vaccinated = table.Column<bool>(type: "boolean", nullable: false),
                    vaccination_expires = table.Column<DateOnly>(type: "date", nullable: true),
                    behavior_notes = table.Column<string>(type: "text", nullable: true),
                    aggression_level = table.Column<int>(type: "integer", nullable: false),
                    special_needs = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pets", x => x.id);
                    table.ForeignKey(
                        name: "FK_pets_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    sitter_id = table.Column<long>(type: "bigint", nullable: true),
                    pet_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    area = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    location_address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    approximate_latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    approximate_longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    report_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    total_price = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    platform_fee = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    payment_intent_id = table.Column<string>(type: "text", nullable: true),
                    payment_status = table.Column<string>(type: "text", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_orders_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_users_sitter_id",
                        column: x => x.sitter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "boarding_orders",
                columns: table => new
                {
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    check_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    check_out_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    pets_count = table.Column<int>(type: "integer", nullable: false),
                    actual_check_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_check_out = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boarding_orders", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_boarding_orders_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_applications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    sitter_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_applications", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_applications_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_applications_users_sitter_id",
                        column: x => x.sitter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "walking_orders",
                columns: table => new
                {
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_min = table.Column<int>(type: "integer", nullable: false),
                    meeting_point = table.Column<string>(type: "text", nullable: true),
                    pets_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_walking_orders", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_walking_orders_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_applications_order_id_sitter_id",
                table: "order_applications",
                columns: new[] { "order_id", "sitter_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_applications_sitter_id",
                table: "order_applications",
                column: "sitter_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_owner_id",
                table: "orders",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_pet_id",
                table: "orders",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_public_id",
                table: "orders",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_sitter_id",
                table: "orders",
                column: "sitter_id");

            migrationBuilder.CreateIndex(
                name: "IX_pets_owner_id",
                table: "pets",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_pets_public_id",
                table: "pets",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_public_id",
                table: "users",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "boarding_orders");

            migrationBuilder.DropTable(
                name: "order_applications");

            migrationBuilder.DropTable(
                name: "walking_orders");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "pets");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
