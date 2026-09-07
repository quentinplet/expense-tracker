using System;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Extensions;

public static class AuthCookieExtension
{
    /// Génère et persiste un nouveau refresh token pour l'utilisateur, puis le pose
    /// en cookie HttpOnly — logique partagée par login, register et changement d'email
    /// (§4 règle 1 : une seule émission de token, jamais copiée-collée par flux).
    public static async Task IssueRefreshTokenCookie(
        this ControllerBase controller, UserManager<AppUser> userManager, ITokenService tokenService, AppUser user)
    {
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Set to true in production
            SameSite = SameSiteMode.None, // front and API are on separate origins
            Expires = DateTime.UtcNow.AddDays(7)
        };
        controller.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
