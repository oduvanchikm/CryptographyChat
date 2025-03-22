using Microsoft.EntityFrameworkCore;
using SecureChat.Common.Models;
using SecureChat.Database.Configurations;

namespace SecureChat.Database;

public class SecureChatDbContext(DbContextOptions<SecureChatDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }
}