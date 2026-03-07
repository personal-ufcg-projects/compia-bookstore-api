using Microsoft.AspNetCore.Mvc;
using CompiaBackend.DTOs;
using CompiaBackend.Services;

namespace CompiaBackend.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Cadastra um novo usuário e envia e-mail de confirmação</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "E-mail e senha são obrigatórios." });

        var (_, error) = await authService.RegisterAsync(req);

        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(new { message = "Cadastro realizado! Verifique seu e-mail para confirmar a conta." });
    }

    /// <summary>Confirma o e-mail via token recebido no link</summary>
    [HttpGet("confirmar-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        var (ok, error) = await authService.ConfirmEmailAsync(token);
        if (!ok) return BadRequest(new { message = error });
        return Ok(new { message = "E-mail confirmado com sucesso! Você já pode fazer login." });
    }

    /// <summary>Faz login e retorna token JWT</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var (response, error) = await authService.LoginAsync(req);
        if (error is not null)
            return Unauthorized(new { message = error });

        return Ok(response);
    }
}