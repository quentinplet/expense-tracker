using System;
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
}
