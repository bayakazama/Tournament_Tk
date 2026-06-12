using Microsoft.EntityFrameworkCore;
using TekkenLeague.Api.Models;

namespace TekkenLeague.Api.Data;

public class TekkenLeagueDbContext : DbContext
{
    public TekkenLeagueDbContext(DbContextOptions<TekkenLeagueDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}