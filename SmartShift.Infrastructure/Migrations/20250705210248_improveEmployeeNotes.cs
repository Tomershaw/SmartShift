using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShift.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class improveEmployeeNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WorkTypes",
                table: "Employees",
                newName: "EmployeeNotes");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Employees",
                newName: "AdminNotes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmployeeNotes",
                table: "Employees",
                newName: "WorkTypes");

            migrationBuilder.RenameColumn(
                name: "AdminNotes",
                table: "Employees",
                newName: "Notes");
        }
    }
}
