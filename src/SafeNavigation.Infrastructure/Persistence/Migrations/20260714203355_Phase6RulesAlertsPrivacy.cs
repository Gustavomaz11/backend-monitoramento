using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeNavigation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6RulesAlertsPrivacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "privacy_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unblock_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    decision_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unblock_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_unblock_requests_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_privacy_requests_guardian_id_request_type_created_at",
                table: "privacy_requests",
                columns: new[] { "guardian_id", "request_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_unblock_requests_device_id_status_created_at",
                table: "unblock_requests",
                columns: new[] { "device_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_unblock_requests_domain",
                table: "unblock_requests",
                column: "domain");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "privacy_requests");

            migrationBuilder.DropTable(
                name: "unblock_requests");
        }
    }
}
