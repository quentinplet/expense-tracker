using System;
using System.Text.Json;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class Seed
{
    public static async Task SeedUsers(UserManager<AppUser> userManager)
    {
        if (await userManager.Users.AnyAsync()) return;

        var users = new List<AppUser>
        {
            new() {
                UserName = "john",
                Email = "john@test.com"
            },
        };

        foreach (var user in users)
        {
            var result = await userManager.CreateAsync(user, "Pa$$w0rd");
            if (!result.Succeeded)
            {
                Console.WriteLine(result.Errors.First().Description);
            }

            await userManager.AddToRoleAsync(user, "Member");
        }

        var admin = new AppUser
        {
            UserName = "admin",
            Email = "admin@test.com"
        };

        await userManager.CreateAsync(admin, "Pa$$w0rd");
        await userManager.AddToRolesAsync(admin, ["Member", "Admin"]);
    }

    public static async Task SeedData(AppDbContext context)
    {
        var seedData = await File.ReadAllTextAsync("Data/SeedData.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<SeedDataDto>(seedData, options);

        if (data == null) return;

        // Les identifiants du fichier de seed sont locaux : on les remappe vers les Guid générés.
        var categoryIds = new Dictionary<int, Guid>();

        if (!await context.Categories.AnyAsync())
        {
            var categories = data.Categories.Select(c => new Category
            {
                Id = Guid.NewGuid(),
                Name = c.Name,
                Enabled = c.Enabled,
                Type = c.TransactionTypeId == 2 ? TransactionType.Income : TransactionType.Expense,
                Icon = c.Icon,
                Color = c.Color,
                TranslationKey = c.TranslationKey,
                IsSystem = true,
                UserId = null
            }).ToList();

            foreach (var (dto, entity) in data.Categories.Zip(categories))
            {
                categoryIds[dto.Id] = entity.Id;
            }

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }
        else
        {
            var existing = await context.Categories.ToListAsync();
            foreach (var dto in data.Categories)
            {
                var match = existing.FirstOrDefault(c => c.Name == dto.Name);
                if (match != null) categoryIds[dto.Id] = match.Id;
            }
        }

        if (!await context.Transactions.AnyAsync())
        {
            var john = await context.Users.FirstOrDefaultAsync(u => u.UserName == "john");
            if (john == null) return;

            var categoryTypes = await context.Categories
                .ToDictionaryAsync(c => c.Id, c => c.Type);

            var transactions = data.Transactions
                .Where(t => categoryIds.ContainsKey(t.CategoryId))
                .Select(t =>
                {
                    var categoryId = categoryIds[t.CategoryId];
                    return new Transaction
                    {
                        Amount = t.Amount,
                        // Le sens était porté par la catégorie ; il est désormais sur la transaction.
                        Type = categoryTypes[categoryId],
                        Date = t.Date,
                        Label = t.Description ?? "Untitled",
                        CategoryId = categoryId,
                        UserId = john.Id
                    };
                }).ToList();

            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();
        }
    }
}
