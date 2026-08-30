using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Infrastructure.Identity;

namespace NileTechno.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<CreateUserResult> CreateUserAsync(string email, string password, string fullName, Guid? userId = null, bool emailConfirmed = false)
    {
        var id = userId ?? Guid.NewGuid();
        try
        {
            var user = new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = emailConfirmed
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return new CreateUserResult(true, id, Array.Empty<string>());

            foreach (var roleName in new[] { "User", "Admin", "MainAdmin" })
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }

            await _userManager.AddToRoleAsync(user, "User");
            return new CreateUserResult(true, user.Id, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AspNetUsers unavailable; continuing with Eco login table for {Email}", email);
            return new CreateUserResult(true, id, Array.Empty<string>());
        }
    }

    public async Task<UserLookupDto?> FindUserAsync(string email)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FindUser skipped because AspNetUsers Id is not Guid");
            return null;
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            return await _userManager.FindByEmailAsync(email) is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> CheckPasswordAsync(string email, string password)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return false;
            return await _userManager.CheckPasswordAsync(user, password);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<IList<string>> GetRolesAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return new List<string> { "User" };
            var roles = await _userManager.GetRolesAsync(user);
            return roles.Count > 0 ? roles : new List<string> { "User" };
        }
        catch (Exception)
        {
            return new List<string> { "User" };
        }
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return Guid.NewGuid().ToString("N");
            return await _userManager.GenerateEmailConfirmationTokenAsync(user);
        }
        catch (Exception)
        {
            return Guid.NewGuid().ToString("N");
        }
    }

    public async Task<bool> ConfirmEmailAsync(string email, string token)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return true;
            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }
        catch (Exception)
        {
            return true;
        }
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email)
                ?? throw new InvalidOperationException("المستخدم غير موجود.");
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception)
        {
            return Guid.NewGuid().ToString("N");
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return false;
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task SaveRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAtUtc)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return;
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiresAtUtc = expiresAtUtc;
            await _userManager.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Skip AspNetUsers refresh token update");
        }
    }

    public async Task<UserLookupDto?> GetUserByValidRefreshTokenAsync(string refreshToken)
    {
        try
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
        catch (Exception)
        {
            return null;
        }
    }

    public async Task RevokeRefreshTokenAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return;
            user.RefreshToken = null;
            user.RefreshTokenExpiresAtUtc = null;
            await _userManager.UpdateAsync(user);
        }
        catch (Exception)
        {
            /* Eco table is the session source */
        }
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return;
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
        catch (Exception)
        {
            /* Eco table is the session source */
        }
    }
}
