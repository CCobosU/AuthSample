using Microsoft.AspNetCore.Mvc;
using MediatR;
using Application.Commands;
using Application.DTOs;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var res = await _mediator.Send(new RegisterCommand(req));
        return Ok(res);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var res = await _mediator.Send(new LoginCommand(req));
        return Ok(res);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var res = await _mediator.Send(new RefreshCommand(req));
        return Ok(res);
    }
}
