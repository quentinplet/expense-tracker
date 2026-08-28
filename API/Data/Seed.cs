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

        // Chaque utilisateur reçoit sa propre copie du jeu de départ (14 catégories,
        // dont les 2 verrouillées "Other") — jamais une ligne partagée entre deux
        // utilisateurs. Idempotent par utilisateur, pas seulement globalement.
        var users = await context.Users.ToListAsync();
        foreach (var user in users)
        {
            if (await context.Categories.AnyAsync(c => c.UserId == user.Id)) continue;

            var personalCategories = data.Categories.Select(c => new Category
            {
                Id = Guid.NewGuid(),
                Name = c.Name,
                Enabled = c.Enabled,
                Type = c.TransactionTypeId == 2 ? TransactionType.Income : TransactionType.Expense,
                Icon = c.Icon,
                Color = c.Color,
                TranslationKey = c.TranslationKey,
                IsLocked = c.IsLocked,
                UserId = user.Id
            }).ToList();

            context.Categories.AddRange(personalCategories);
        }
        await context.SaveChangesAsync();

        if (!await context.Transactions.AnyAsync())
        {
            var john = await context.Users.FirstOrDefaultAsync(u => u.UserName == "john");
            if (john == null) return;

            var johnCategories = await context.Categories.Where(c => c.UserId == john.Id).ToListAsync();

            // Les identifiants du fichier de seed sont locaux : on les remappe vers les
            // catégories personnelles de John (même Name + Type).
            var categoryIds = new Dictionary<int, Guid>();
            foreach (var dto in data.Categories)
            {
                var type = dto.TransactionTypeId == 2 ? TransactionType.Income : TransactionType.Expense;
                var match = johnCategories.FirstOrDefault(c => c.Name == dto.Name && c.Type == type);
                if (match != null) categoryIds[dto.Id] = match.Id;
            }

            var categoryTypes = johnCategories.ToDictionary(c => c.Id, c => c.Type);

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
