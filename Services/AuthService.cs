using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CompiaBackend.Data;
using CompiaBackend.DTOs;
using CompiaBackend.Models;

namespace CompiaBackend.Services;

public class AuthService(AppDbContext db, IConfiguration config, EmailService emailService)
{
    public async Task<(AuthResponse? response, string? error)> RegisterAsync(RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return (null, "Este e-mail já está cadastrado.");

        var token = Guid.NewGuid().ToString("N");

        var user = new User
        {
            FullName          = req.FullName,
            Email             = req.Email,
            PasswordHash      = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role              = "cliente",
            EmailConfirmed    = false,
            EmailConfirmToken = token
        };

        db.Users.Add(user);
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId     = user.Id,
            Action     = "signup",
            EntityType = "user",
            Details    = $"{{\"email\":\"{req.Email}\"}}"
        });
        await db.SaveChangesAsync();

        await emailService.SendConfirmationEmailAsync(user.Email, user.FullName, token);

        return (null, null);
    }

    public async Task<(AuthResponse? response, string? error)> LoginAsync(LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return (null, "Credenciais inválidas.");

        if (!user.EmailConfirmed)
            return (null, "Confirme seu e-mail antes de entrar. Verifique sua caixa de entrada.");

        db.ActivityLogs.Add(new ActivityLog
        {
            UserId     = user.Id,
            Action     = "login",
            EntityType = "user",
            Details    = $"{{\"email\":\"{req.Email}\"}}"
        });
        await db.SaveChangesAsync();

        return (BuildResponse(user), null);
    }

    public async Task<(bool ok, string? error)> ConfirmEmailAsync(string token)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.EmailConfirmToken == token);
        if (user is null) return (false, "Token inválido ou expirado.");

        user.EmailConfirmed    = true;
        user.EmailConfirmToken = null;
        await db.SaveChangesAsync();

        return (true, null);
    }

    private AuthResponse BuildResponse(User user) =>
        new(GenerateJwt(user), user.Id.ToString(), user.FullName, user.Email, user.Role);

    private string GenerateJwt(User user)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(int.Parse(config["Jwt:ExpiresInDays"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Name,           user.FullName),
            new Claim(ClaimTypes.Role,           user.Role),
        };

        var jwt = new JwtSecurityToken(
            issuer:            config["Jwt:Issuer"],
            audience:          config["Jwt:Audience"],
            claims:            claims,
            expires:           expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}