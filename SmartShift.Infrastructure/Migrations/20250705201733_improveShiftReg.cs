using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShift.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class improveShiftReg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Shifts");

            migrationBuilder.RenameColumn(
                name: "RequiredPriorityRating",
                table: "Shifts",
                newName: "SkillLevelRequired");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MinimumEmployeeCount",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RequiredEmployeeCount",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxShiftsPerWeek",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SkillLevel",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WorkTypes",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "MinimumEmployeeCount",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "RequiredEmployeeCount",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "MaxShiftsPerWeek",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SkillLevel",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WorkTypes",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "SkillLevelRequired",
                table: "Shifts",
                newName: "RequiredPriorityRating");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Shifts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
