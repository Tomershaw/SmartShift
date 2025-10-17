using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShift.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveShiftRegistrationUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftRegistration_ShiftId_EmployeeId",
                table: "ShiftRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_ShiftRegistrations_TenantId",
                table: "ShiftRegistrations");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRegistrations_ShiftId",
                table: "ShiftRegistrations",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "UX_ShiftRegistration_Active",
                table: "ShiftRegistrations",
                columns: new[] { "TenantId", "EmployeeId", "ShiftId" },
                unique: true,
                filter: "[Status] IN (0,1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShiftRegistrations_ShiftId",
                table: "ShiftRegistrations");

            migrationBuilder.DropIndex(
                name: "UX_ShiftRegistration_Active",
                table: "ShiftRegistrations");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRegistration_ShiftId_EmployeeId",
                table: "ShiftRegistrations",
                columns: new[] { "ShiftId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRegistrations_TenantId",
                table: "ShiftRegistrations",
                column: "TenantId");
        }
    }
}
