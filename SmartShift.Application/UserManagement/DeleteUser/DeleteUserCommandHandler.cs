using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartShift.Domain.Data;
using SmartShift.Domain.Features.RefreshTokens;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Application.Features.UserManagement.DeleteUser;

public sealed class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommand, DeleteUserResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public DeleteUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<DeleteUserResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // 1. שליפת המשתמש
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId == request.TenantId, cancellationToken);

        if (user is null)
        {
            return new DeleteUserResult
            {
                Success = false,
                Message = "User not found or already deleted",
                Errors = new[] { "המשתמש לא נמצא או כבר נמחק" }
            };
        }

        // 2. הגנה על מחיקת משתמש Admin
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            return new DeleteUserResult
            {
                Success = false,
                Message = "Cannot delete admin users",
                Errors = new[] { "לא ניתן למחוק משתמש בעל תפקיד Admin" }
            };
        }

        try
        {
            // 3. ביצוע ה-Soft Delete

            // א. עדכון המשתמש (User) - כדי שלא יוכל להתחבר
            user.IsActive = false;
            user.DeletedAt = DateTimeOffset.UtcNow;

            // ב. 👇👇👇 התוספת הקריטית: עדכון העובד (Employee) - כדי שיופיע באדום 👇👇👇
            // אנחנו מחפשים את העובד שמקושר ל-UserId הזה
            var employee = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id, cancellationToken);

            if (employee != null)
            {
                employee.IsActive = false;
                // אין צורך ב-Update כאן כי ה-dbContext עוקב אחריו, ה-SaveChangesAsync בסוף ישמור אותו
            }

            // ג. מחיקת RefreshTokens
            var refreshTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == user.Id)
                .ToListAsync(cancellationToken);

            if (refreshTokens.Count > 0)
            {
                _dbContext.RefreshTokens.RemoveRange(refreshTokens);
            }

            // ד. שמירת השינויים ב-Identity
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return new DeleteUserResult
                {
                    Success = false,
                    Message = "Failed to delete user",
                    Errors = updateResult.Errors.Select(e => e.Description).ToArray()
                };
            }

            // ה. שמירת השינויים ב-Employees וב-RefreshTokens
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new DeleteUserResult
            {
                Success = true,
                Message = "User deleted successfully"
            };
        }
        catch (Exception ex)
        {
            return new DeleteUserResult
            {
                Success = false,
                Message = "Error deleting user",
                Errors = new[] { ex.Message }
            };
        }
    }
}