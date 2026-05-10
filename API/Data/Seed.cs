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
        if (await context.TransactionTypes.AnyAsync()) return;
        // Seed transaction types
        var transactionTypes = new List<TransactionType>
    {
        new() { Name = TransactionTypeName.Income },
        new() { Name = TransactionTypeName.Expense }
    };
        context.TransactionTypes.AddRange(transactionTypes);
        await context.SaveChangesAsync();

        if (await context.Categories.AnyAsync()) return;
        // Seed categories
        var seedData = await File.ReadAllTextAsync("Data/SeedData.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<SeedDataDto>(seedData, options);

        if (data == null) return;

        var categories = data.Categories.Select(c => new Category
        {
            Name = c.Name,
            Enabled = c.Enabled,
            TransactionTypeId = c.TransactionTypeId
        }).ToList();

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        if (await context.Transactions.AnyAsync()) return;
        // Seed transactions
        // Récupérer le vrai ID de john
        var john = await context.Users.FirstOrDefaultAsync(u => u.UserName == "john");
        if (john == null) return;
        if (data == null) return;

        var transactions = data.Transactions.Select(t => new Transaction
        {
            Amount = t.Amount,
            Date = DateTime.SpecifyKind(t.Date, DateTimeKind.Utc),
            Description = t.Description,
            CategoryId = t.CategoryId,
            UserId = john.Id  // vrai ID de la BDD
        }).ToList();

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();
    }
}
