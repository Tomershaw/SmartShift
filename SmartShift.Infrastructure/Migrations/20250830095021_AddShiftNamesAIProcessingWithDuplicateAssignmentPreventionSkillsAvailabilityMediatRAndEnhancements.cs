using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShift.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftNamesAIProcessingWithDuplicateAssignmentPreventionSkillsAvailabilityMediatRAndEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShiftAvailability",
                table: "ShiftRegistrations");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ShiftAvailability",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Shifts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "ShiftAvailability",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Employees");

            migrationBuilder.AddColumn<int>(
                name: "ShiftAvailability",
                table: "ShiftRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
