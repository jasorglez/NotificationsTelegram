using Microsoft.EntityFrameworkCore;
using NotificationsTelegram.Models;

namespace NotificationsTelegram;

public class SecurityDbContext : DbContext
{
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}
