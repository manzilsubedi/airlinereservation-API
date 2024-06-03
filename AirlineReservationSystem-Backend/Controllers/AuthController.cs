using AirlineReservationSystem_Backend.Models;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;

    public AuthController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = _userService.Authenticate(request.Email, request.Password);

        if (user == null)
            return Unauthorized();

        var token = _userService.GenerateJwtToken(user);
        return Ok(new { Token = token, role = user.Role });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        var user = _userService.Register(request.Name, request.Email, request.Password, request.Role);
        var token = _userService.GenerateJwtToken(user);
        return Ok(new { Token = token });
    }
}
