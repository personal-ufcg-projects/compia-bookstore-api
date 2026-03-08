using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CompiaBackend.Data;
using CompiaBackend.Models;
using CompiaBackend.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Banco de dados ────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── HTTP Clients (usado pelo ShippingService para chamar Correios) ─
builder.Services.AddHttpClient();

// ── Serviços da aplicação ─────────────────────────────────────────
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ShippingService>();
builder.Services.AddScoped<OrderService>();

// ── JWT ───────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddPolicy("FrontendPolicy", p => p
        .WithOrigins("http://localhost:5173", "http://localhost:8080")
        .AllowAnyHeader()
        .AllowAnyMethod()));

// ── Controllers + OpenAPI ─────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Migrations automáticas + Seed do Admin ────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    db.Database.Migrate();

    const string adminEmail = "compiaeditorabookstore@gmail.com";
    if (!db.Users.Any(u => u.Email == adminEmail))
    {
        var adminPassword = config["Admin:SeedPassword"]
            ?? throw new InvalidOperationException(
                "Defina 'Admin:SeedPassword' em user-secrets antes de iniciar.");

        db.Users.Add(new User
        {
            FullName          = "COMPIA Editora",
            Email             = adminEmail,
            PasswordHash      = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role              = "admin",
            EmailConfirmed    = true,
            EmailConfirmToken = null,
        });

        db.SaveChanges();
        Console.WriteLine("Administrador zero criado com sucesso.");
    }
}

// ── OpenAPI + Scalar ──────────────────────────────────────────────
// UI:   http://localhost:5260/scalar/v1
// JSON: http://localhost:5260/openapi/v1.json
app.MapOpenApi();
app.MapScalarApiReference(opt => opt.Title = "COMPIA Editora API");

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();