using System;
using API.DTOs;
using API.DTOs.Requests;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class UsersController(UserManager<AppUser> userManager, ITokenService tokenService) : BaseApiController
{
    [HttpPut("me/password")] // api/users/me/password
    public async Task<ActionResult> ChangePassword(ChangePasswordRequestDto dto)
    {
        var user = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("Authenticated user not found.");

        var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        // L'access token en cours reste valide jusqu'à expiration (courte durée de
        // vie) ; aucun refresh ultérieur ne doit fonctionner avec l'ancien secret.
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await userManager.UpdateAsync(user);

        return NoContent();
    }

    [HttpPut("me/name")] // api/users/me/name
    public async Task<ActionResult<UserDto>> ChangeName(ChangeNameRequestDto dto)
    {
        var user = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("Authenticated user not found.");

        // Pas de confirmation par mot de passe : contrairement à l'email ou au mot
        // de passe, le nom n'est pas un identifiant de connexion.
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return await user.ToDto(tokenService);
    }

    [HttpPut("me/email")] // api/users/me/email
    public async Task<ActionResult<UserDto>> ChangeEmail(ChangeEmailRequestDto dto)
    {
        var user = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("Authenticated user not found.");

        if (!await userManager.CheckPasswordAsync(user, dto.CurrentPassword))
            return BadRequest("Current password is incorrect.");

        var emailResult = await userManager.SetEmailAsync(user, dto.NewEmail);
        if (!emailResult.Succeeded)
        {
            if (emailResult.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                return Conflict("Email is already in use.");

            return BadRequest(emailResult.Errors.Select(e => e.Description));
        }

        // UserName reflète Email dans cette app (pas de champ nom d'utilisateur
        // séparé) : les deux doivent rester synchronisés.
        await userManager.SetUserNameAsync(user, dto.NewEmail);

        // Le JWT courant porte l'ancien email en claim : le frontend ne doit plus
        // s'en servir après un changement réussi.
        await this.IssueRefreshTokenCookie(userManager, tokenService, user);
        return await user.ToDto(tokenService);
    }

    [HttpDelete("me")] // api/users/me
    public async Task<ActionResult> DeleteAccount(DeleteAccountRequestDto dto)
    {
        var user = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("Authenticated user not found.");

        if (!await userManager.CheckPasswordAsync(user, dto.CurrentPassword))
            return BadRequest("Current password is incorrect.");

        // Suppression définitive et immédiate : cascade en base sur toutes les
        // données de l'utilisateur (comptes, catégories, transactions, budgets,
        // transactions récurrentes, notifications — vérifié sur chaque FK
        // AppUser dans AppDbContext/les migrations, toutes en DeleteBehavior.Cascade).
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });

        return NoContent();
    }
}
