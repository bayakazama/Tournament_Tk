namespace TekkenLeague.Api.Models;

public class User
{
    public int Id { get; set; }

    public string DiscordId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = "Player";
}