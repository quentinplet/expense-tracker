using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Tests.TestHelpers;

public static class ControllerTestExtensions
{
    /// Stands in for the JWT-derived ClaimsPrincipal ASP.NET Core would normally
    /// attach to HttpContext.User — every controller in this app resolves the
    /// current user via `User.GetMemberId()`, which reads this claim.
    public static void SetUser(this ControllerBase controller, Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
