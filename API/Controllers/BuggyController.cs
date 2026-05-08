using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class BuggyController : BaseApiController
{
    [HttpGet("auth")] // error 401
    public IActionResult GetAuth()
    {
        return Unauthorized();
    }

    //error 403
    [HttpGet("forbidden")] // error 403
    public IActionResult GetForbidden()
    {
        return Forbid();
    }

    [HttpGet("not-found")] // error 404
    public IActionResult GetNotFound()
    {
        return NotFound();
    }

    [HttpGet("server-error")] // error 500
    public IActionResult GetServerError()
    {
        throw new Exception("This is a server error");
    }

    [HttpGet("bad-request")] // error 400
    public IActionResult GetBadRequest()
    {
        return BadRequest("This is a bad request");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-secret")]
    public ActionResult<string> GetSecret()
    {
        return Ok("Only admins should see this");
    }



}
