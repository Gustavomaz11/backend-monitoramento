using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeNavigation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceUsageSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "usage_schedule_json",
                table: "device_configs",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "usage_schedule_json",
                table: "device_configs");
        }
    }
}
