using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using ExpenseTrackerApi.Data;
using ExpenseTrackerApi.DTOs;
using ExpenseTrackerApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=expenses.db"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? string.Empty)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/signup", async (SignUpRequest request, AppDbContext db, IPasswordHasher<User> hasher) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Username, email, and password are required." });
    }

    var normalizedEmail = request.Email.Trim().ToLowerInvariant();
    if (await db.Users.AnyAsync(u => u.Email == normalizedEmail))
    {
        return Results.Conflict(new { message = "Email is already registered." });
    }

    var user = new User
    {
        Username = request.Username.Trim(),
        Email = normalizedEmail
    };

    user.PasswordHash = hasher.HashPassword(user, request.Password);
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Username, user.Email });
});

app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db, IPasswordHasher<User> hasher) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Email and password are required." });
    }

    var normalizedEmail = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var verificationResult = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
    if (verificationResult == PasswordVerificationResult.Failed)
    {
        return Results.Unauthorized();
    }

    var token = GenerateJwtToken(user, builder.Configuration);
    return Results.Ok(new AuthResponse { Token = token, Username = user.Username, Email = user.Email });
});

app.MapGet("/api/expenses", async (HttpContext context, AppDbContext db, string? filter, DateTime? startDate, DateTime? endDate) =>
{
    var userId = GetUserId(context);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var query = db.Expenses.Where(e => e.UserId == userId.Value).AsQueryable();
    var today = DateTime.UtcNow.Date;

    if (!string.IsNullOrWhiteSpace(filter))
    {
        query = filter.ToLowerInvariant() switch
        {
            "past-week" => query.Where(e => e.Date >= today.AddDays(-7) && e.Date <= today),
            "past-month" => query.Where(e => e.Date >= today.AddMonths(-1) && e.Date <= today),
            "past-3-months" => query.Where(e => e.Date >= today.AddMonths(-3) && e.Date <= today),
            "custom" when startDate.HasValue && endDate.HasValue => query.Where(e => e.Date >= startDate.Value.Date && e.Date <= endDate.Value.Date),
            "custom" => query.Where(e => false),
            _ => query
        };
    }

    var results = await query
        .OrderByDescending(e => e.Date)
        .Select(e => new ExpenseResponse
        {
            Id = e.Id,
            Title = e.Title,
            Amount = e.Amount,
            Category = e.Category,
            Date = e.Date,
            Notes = e.Notes
        })
        .ToListAsync();

    return Results.Ok(results);
}).RequireAuthorization();

app.MapPost("/api/expenses", async (HttpContext context, CreateExpenseRequest request, AppDbContext db) =>
{
    var userId = GetUserId(context);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var expense = new Expense
    {
        Title = request.Title.Trim(),
        Amount = request.Amount,
        Category = request.Category,
        Date = request.Date.Date,
        Notes = request.Notes?.Trim(),
        UserId = userId.Value
    };

    db.Expenses.Add(expense);
    await db.SaveChangesAsync();
    return Results.Created($"/api/expenses/{expense.Id}", expense);
}).RequireAuthorization();

app.MapPut("/api/expenses/{id}", async (HttpContext context, int id, UpdateExpenseRequest request, AppDbContext db) =>
{
    var userId = GetUserId(context);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId.Value);
    if (expense is null)
    {
        return Results.NotFound();
    }

    expense.Title = request.Title.Trim();
    expense.Amount = request.Amount;
    expense.Category = request.Category;
    expense.Date = request.Date.Date;
    expense.Notes = request.Notes?.Trim();

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/api/expenses/{id}", async (HttpContext context, int id, AppDbContext db) =>
{
    var userId = GetUserId(context);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId.Value);
    if (expense is null)
    {
        return Results.NotFound();
    }

    db.Expenses.Remove(expense);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.Run();

static string GenerateJwtToken(User user, IConfiguration configuration)
{
    var jwtSettings = configuration.GetSection("JwtSettings");
    var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT secret is required.");
    var issuer = jwtSettings["Issuer"] ?? string.Empty;
    var audience = jwtSettings["Audience"] ?? string.Empty;
    var duration = int.TryParse(jwtSettings["DurationInMinutes"], out var minutes) ? minutes : 60;

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("username", user.Username)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(duration),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

static int? GetUserId(HttpContext context)
{
    var claimValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    return int.TryParse(claimValue, out var id) ? id : null;
}
