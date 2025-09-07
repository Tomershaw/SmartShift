using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShift.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeSkillsAvailabilityShiftRegistrationEnumsAndAI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftAvailability",
                table: "ShiftRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ShiftRegistrations",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShiftAvailability",
                table: "ShiftRegistrations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ShiftRegistrations");
        }
    }
}
