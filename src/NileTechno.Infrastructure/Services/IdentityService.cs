using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Infrastructure.Identity;

namespace NileTechno.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<CreateUserResult> CreateUserAsync(string email, string password, string fullName)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
            return new CreateUserResult(false, null, result.Errors.Select(e => e.Description).ToArray());

        await _userManager.AddToRoleAsync(user, "User");

        return new CreateUserResult(true, user.Id, Array.Empty<string>());
    }

    public async Task<UserLookupDto?> FindUserAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return null;

        return new UserLookupDto(
            user.Id,
            user.Email ?? email,
            user.FullName,
            await _userManager.IsEmailConfirmedAsync(user),
            user.IsBlocked);
    }

    public async Task<bool> EmailExistsAsync(string email) =>
        await _userManager.FindByEmailAsync(email) is not null;

    public async Task<bool> CheckPasswordAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return false;
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IList<string>> GetRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Array.Empty<string>();
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<bool> ConfirmEmailAsync(string email, string token)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return false;
        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return false;
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded;
    }

    public async Task SaveRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAtUtc)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAtUtc = expiresAtUtc;
        await _userManager.UpdateAsync(user);
    }

    public async Task<UserLookupDto?> GetUserByValidRefreshTokenAsync(string refreshToken)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        if (user is null) return null;
        if (user.RefreshTokenExpiresAtUtc is null || user.RefreshTokenExpiresAtUtc < DateTime.UtcNow) return null;

        return new UserLookupDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            await _userManager.IsEmailConfirmedAsync(user),
            user.IsBlocked);
    }

    public async Task RevokeRefreshTokenAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;
        user.RefreshToken = null;
        user.RefreshTokenExpiresAtUtc = null;
        await _userManager.UpdateAsync(user);
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }
}
