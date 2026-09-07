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
                Email = "john@test.com",
                FirstName = "John",
                LastName = "Doe"
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
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User"
        };

        await userManager.CreateAsync(admin, "Pa$$w0rd");
        await userManager.AddToRolesAsync(admin, ["Member", "Admin"]);
    }

    /// Jeu de départ (14 catégories, dont les 2 verrouillées "Other") pour un
    /// utilisateur donné — utilisé par le seed au démarrage et par l'inscription
    /// (AccountController.Register), seul point d'entrée réel désormais.
    public static async Task<List<Category>> CreateStarterCategoriesAsync(Guid userId)
    {
        var seedData = await File.ReadAllTextAsync("Data/SeedData.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<SeedDataDto>(seedData, options);

        return data == null ? [] : BuildStarterCategories(data.Categories, userId);
    }

    private static List<Category> BuildStarterCategories(List<CategorySeedDto> items, Guid userId) =>
        items.Select(c => new Category
        {
            Id = Guid.NewGuid(),
            Name = c.Name,
            Enabled = c.Enabled,
            Type = c.TransactionTypeId == 2 ? TransactionType.Income : TransactionType.Expense,
            Icon = c.Icon,
            Color = c.Color,
            TranslationKey = c.TranslationKey,
            IsLocked = c.IsLocked,
            UserId = userId
        }).ToList();

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

            context.Categories.AddRange(BuildStarterCategories(data.Categories, user.Id));
        }
        await context.SaveChangesAsync();

        var johnUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "john");
        if (johnUser == null) return;

        var johnCategories = await context.Categories.Where(c => c.UserId == johnUser.Id).ToListAsync();

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

        if (!await context.Transactions.AnyAsync())
        {
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
                        UserId = johnUser.Id
                    };
                }).ToList();

            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();
        }

        if (!await context.Budgets.AnyAsync())
        {
            var budgets = data.Budgets
                .Where(b => b.CategoryId == null || categoryIds.ContainsKey(b.CategoryId.Value))
                .Select(b => new Budget
                {
                    AmountLimit = b.AmountLimit,
                    Month = b.Month,
                    AutoRenew = b.AutoRenew,
                    CategoryId = b.CategoryId.HasValue ? categoryIds[b.CategoryId.Value] : null,
                    UserId = johnUser.Id
                }).ToList();

            context.Budgets.AddRange(budgets);
            await context.SaveChangesAsync();
        }

        if (!await context.RecurringTransactions.AnyAsync())
        {
            var recurringTransactions = data.RecurringTransactions
                .Where(r => categoryIds.ContainsKey(r.CategoryId))
                .Select(r =>
                {
                    var categoryId = categoryIds[r.CategoryId];
                    return new RecurringTransaction
                    {
                        Label = r.Label,
                        Amount = r.Amount,
                        Type = categoryTypes[categoryId],
                        Frequency = Enum.Parse<Frequency>(r.Frequency, ignoreCase: true),
                        NextDueDate = r.NextDueDate,
                        Active = r.Active,
                        CategoryId = categoryId,
                        UserId = johnUser.Id
                    };
                }).ToList();

            context.RecurringTransactions.AddRange(recurringTransactions);
            await context.SaveChangesAsync();
        }
    }
}
