using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShift.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMinimumEarlyEmployeesAndShiftArrivalType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShiftAvailability",
                table: "Shifts",
                newName: "MinimumEarlyEmployees");

            migrationBuilder.AddColumn<int>(
                name: "ShiftArrivalType",
                table: "ShiftRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShiftArrivalType",
                table: "ShiftRegistrations");

            migrationBuilder.RenameColumn(
                name: "MinimumEarlyEmployees",
                table: "Shifts",
                newName: "ShiftAvailability");
        }
    }
}
