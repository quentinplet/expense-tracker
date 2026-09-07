using System;
using API.Data;
using API.DTOs;
using API.DTOs.Requests;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(UserManager<AppUser> userManager, ITokenService tokenService, AppDbContext context) : BaseApiController
{
    [HttpPost("login")] // api/account/login
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);

        if (user == null) return Unauthorized("Invalid email address");

        var result = await userManager.CheckPasswordAsync(user, loginDto.Password);

        if (!result) return Unauthorized("Invalid password");

        await this.IssueRefreshTokenCookie(userManager, tokenService, user);
        return await user.ToDto(tokenService);
    }

    [HttpPost("register")] // api/account/register
    public async Task<ActionResult<UserDto>> Register(RegisterRequestDto registerDto)
    {
        var user = new AppUser
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
        };

        // La création de l'utilisateur (via UserManager, qui fait son propre
        // SaveChanges interne) et l'insertion des catégories de départ doivent
        // réussir ensemble : un compte sans ses catégories est un état incohérent
        // qu'on ne veut jamais pouvoir observer.
        using var transaction = await context.Database.BeginTransactionAsync();

        var result = await userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                return Conflict("Email is already in use.");

            return BadRequest(result.Errors.Select(e => e.Description));
        }

        await userManager.AddToRoleAsync(user, "Member");

        var categories = await Seed.CreateStarterCategoriesAsync(user.Id);
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        await this.IssueRefreshTokenCookie(userManager, tokenService, user);
        return StatusCode(StatusCodes.Status201Created, await user.ToDto(tokenService));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<UserDto>> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken)) return NoContent();

        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return Unauthorized("Invalid or expired refresh token");

        await this.IssueRefreshTokenCookie(userManager, tokenService, user);
        return await user.ToDto(tokenService);
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await userManager.UpdateAsync(user);
            }
        }

        // Mêmes attributs qu'à la pose, sinon le navigateur garde le cookie.
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });

        return NoContent();
    }
}
