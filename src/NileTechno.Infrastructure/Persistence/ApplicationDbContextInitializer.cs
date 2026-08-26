using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NileTechno.Infrastructure.Identity;

namespace NileTechno.Infrastructure.Persistence;

public class ApplicationDbContextInitializer
{
    private readonly ILogger<ApplicationDbContextInitializer> _logger;
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public ApplicationDbContextInitializer(
        ILogger<ApplicationDbContextInitializer> logger,
        ApplicationDbContext context,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            var pending = await _context.Database.GetPendingMigrationsAsync();
            if (pending.Any())
                await _context.Database.MigrateAsync();
            else
                await _context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "حصل خطأ أثناء تطبيق الـ Migrations على قاعدة البيانات.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        foreach (var role in new[] { "User", "Admin", "MainAdmin" })
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await SeedInitialAdminAsync();
    }

    private async Task SeedInitialAdminAsync()
    {
        var section = _configuration.GetSection("InitialAdmin");
        var email = section["Email"];
        var password = section["Password"];
        var fullName = section["FullName"] ?? "المدير الرئيسي";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is null)
        {
            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("فشل إنشاء أول أدمن: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }

            existingUser = admin;
            _logger.LogInformation("تم إنشاء أول أدمن بالإيميل: {Email}", email);
        }

        if (!await _userManager.IsInRoleAsync(existingUser, "MainAdmin"))
        {
            await _userManager.AddToRoleAsync(existingUser, "MainAdmin");
        }

        if (!existingUser.EmailConfirmed)
        {
            existingUser.EmailConfirmed = true;
            await _userManager.UpdateAsync(existingUser);
        }
    }
}
