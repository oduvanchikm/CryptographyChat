using Microsoft.EntityFrameworkCore;
using SecureChat.Common.Models;
using SecureChat.Database.Configurations;

namespace SecureChat.Database;

public class SecureChatDbContext(DbContextOptions<SecureChatDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Chats> Chat { get; set; }
    public DbSet<Sessions> Session { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new SessionsConfiguration());
        modelBuilder.ApplyConfiguration(new ChatsConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }
}