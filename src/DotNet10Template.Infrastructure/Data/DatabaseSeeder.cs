using DotNet10Template.Domain.Entities;
using DotNet10Template.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNet10Template.Infrastructure.Data;

/// <summary>
/// Database seeder for initial data
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            // Apply migrations
            await context.Database.MigrateAsync();

            // Seed roles
            await SeedRolesAsync(roleManager, logger);

            // Seed admin user
            await SeedAdminUserAsync(userManager, logger);

            // Seed sample categories
            await SeedCategoriesAsync(context, logger);

            // Seed sample products
            await SeedProductsAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var roles = new[]
        {
            new ApplicationRole { Name = "Admin", Description = "Administrator with full access" },
            new ApplicationRole { Name = "Manager", Description = "Manager with elevated access" },
            new ApplicationRole { Name = "User", Description = "Standard user" }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                await roleManager.CreateAsync(role);
                logger.LogInformation("Created role: {RoleName}", role.Name);
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminEmail = "admin@dotnet10template.com";
        const string adminPassword = "Admin@123456";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                logger.LogWarning("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        var categories = new List<Category>
        {
            new() { Name = "Electronics", Description = "Electronic devices and accessories" },
            new() { Name = "Clothing", Description = "Apparel and fashion items" },
            new() { Name = "Books", Description = "Books and publications" },
            new() { Name = "Home & Garden", Description = "Home improvement and garden supplies" },
            new() { Name = "Sports", Description = "Sports equipment and accessories" }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} categories", categories.Count);
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Products.AnyAsync())
        {
            return;
        }

        var electronicsCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Electronics");
        var clothingCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Clothing");
        var booksCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Books");

        var products = new List<Product>
        {
            new()
            {
                Name = "Wireless Headphones",
                Description = "High-quality wireless headphones with noise cancellation",
                Price = 149.99m,
                StockQuantity = 50,
                SKU = "ELEC-WH-001",
                CategoryId = electronicsCategory?.Id
            },
            new()
            {
                Name = "Smart Watch",
                Description = "Feature-rich smartwatch with health monitoring",
                Price = 299.99m,
                StockQuantity = 30,
                SKU = "ELEC-SW-001",
                CategoryId = electronicsCategory?.Id
            },
            new()
            {
                Name = "Cotton T-Shirt",
                Description = "Comfortable 100% cotton t-shirt",
                Price = 24.99m,
                StockQuantity = 100,
                SKU = "CLTH-TS-001",
                CategoryId = clothingCategory?.Id
            },
            new()
            {
                Name = "Programming Guide",
                Description = "Comprehensive guide to modern programming",
                Price = 49.99m,
                StockQuantity = 75,
                SKU = "BOOK-PG-001",
                CategoryId = booksCategory?.Id
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} products", products.Count);
    }
}
