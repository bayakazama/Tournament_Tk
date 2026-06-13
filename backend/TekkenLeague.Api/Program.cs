using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TekkenLeague.Api.Data;
using TekkenLeague.Api.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<TekkenLeagueDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var discordClientId = builder.Configuration["Discord:ClientId"];
var discordClientSecret = builder.Configuration["Discord:ClientSecret"];
var discordRedirectUri = builder.Configuration["Discord:RedirectUri"];

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "dojo_league_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        app = "Tekken League API",
        timestamp = DateTime.UtcNow
    });
});

app.MapGet("/api/auth/discord/login", () =>
{
    if (string.IsNullOrWhiteSpace(discordClientId) || string.IsNullOrWhiteSpace(discordRedirectUri))
    {
        return Results.Problem("Discord OAuth settings are missing.");
    }

    var state = Guid.NewGuid().ToString("N");
    var scope = Uri.EscapeDataString("identify email");
    var redirectUri = Uri.EscapeDataString(discordRedirectUri);
    var authorizeUrl = $"https://discord.com/oauth2/authorize?client_id={discordClientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}&state={state}";

    return Results.Redirect(authorizeUrl);
});

app.MapGet("/api/auth/discord/callback", async (
    string? code,
    string? error,
    IHttpClientFactory httpClientFactory,
    TekkenLeagueDbContext db,
    HttpContext httpContext) =>
{
    if (!string.IsNullOrWhiteSpace(error))
    {
        return Results.BadRequest(new { message = "Discord login was cancelled or failed.", error });
    }

    if (string.IsNullOrWhiteSpace(code))
    {
        return Results.BadRequest(new { message = "Missing Discord authorization code." });
    }

    if (string.IsNullOrWhiteSpace(discordClientId) || string.IsNullOrWhiteSpace(discordClientSecret) || string.IsNullOrWhiteSpace(discordRedirectUri))
    {
        return Results.Problem("Discord OAuth settings are missing.");
    }

    var httpClient = httpClientFactory.CreateClient();

    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token")
    {
        Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = discordClientId,
            ["client_secret"] = discordClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = discordRedirectUri
        })
    };

    var tokenResponse = await httpClient.SendAsync(tokenRequest);

    if (!tokenResponse.IsSuccessStatusCode)
    {
        var tokenError = await tokenResponse.Content.ReadAsStringAsync();
        return Results.BadRequest(new
        {
            message = "Failed to exchange Discord authorization code.",
            error = tokenError
        });
    }

    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
    using var tokenDocument = JsonDocument.Parse(tokenJson);
    var accessToken = tokenDocument.RootElement.GetProperty("access_token").GetString();

    if (string.IsNullOrWhiteSpace(accessToken))
    {
        return Results.BadRequest(new { message = "Discord did not return an access token." });
    }

    var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
    userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var userResponse = await httpClient.SendAsync(userRequest);

    if (!userResponse.IsSuccessStatusCode)
    {
        var userError = await userResponse.Content.ReadAsStringAsync();
        return Results.BadRequest(new
        {
            message = "Failed to fetch Discord user profile.",
            error = userError
        });
    }

    var userJson = await userResponse.Content.ReadAsStringAsync();
    using var userDocument = JsonDocument.Parse(userJson);
    var discordUser = userDocument.RootElement;

    var discordId = discordUser.GetProperty("id").GetString() ?? string.Empty;
    var username = discordUser.GetProperty("username").GetString() ?? string.Empty;
    var avatar = discordUser.TryGetProperty("avatar", out var avatarProperty)
        ? avatarProperty.GetString()
        : null;

    var avatarUrl = string.IsNullOrWhiteSpace(discordId) || string.IsNullOrWhiteSpace(avatar)
        ? null
        : $"https://cdn.discordapp.com/avatars/{discordId}/{avatar}.png";

    var user = await db.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId);

    if (user is null)
    {
        user = new User
        {
            DiscordId = discordId,
            Username = username,
            AvatarUrl = avatarUrl,
            Role = "Player"
        };

        db.Users.Add(user);
    }
    else
    {
        user.Username = username;
        user.AvatarUrl = avatarUrl;
    }

    await db.SaveChangesAsync();

    var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new(ClaimTypes.Name, user.Username),
    new(ClaimTypes.Role, user.Role),
    new("discord_id", user.DiscordId)
};

var identity = new ClaimsIdentity(
    claims,
    CookieAuthenticationDefaults.AuthenticationScheme
);

var principal = new ClaimsPrincipal(identity);

await httpContext.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme,
    principal,
    new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
    }
);

var frontendBaseUrl =
    builder.Configuration["Frontend:BaseUrl"]
    ?? "http://localhost:5173";

return Results.Redirect($"{frontendBaseUrl}/auth/success");
});

app.MapGet("/api/auth/me", async (
    ClaimsPrincipal principal,
    TekkenLeagueDbContext db) =>
{
    var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!int.TryParse(idValue, out var userId))
    {
        return Results.Unauthorized();
    }

    var user = await db.Users.FindAsync(userId);

    if (user is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        user.Id,
        user.DiscordId,
        user.Username,
        user.AvatarUrl,
        user.Role
    });
})
.RequireAuthorization();

app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme
    );

    return Results.NoContent();
});

app.Run();