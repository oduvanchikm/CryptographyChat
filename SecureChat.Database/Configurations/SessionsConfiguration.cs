using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Common.Models;

namespace SecureChat.Database.Configurations;

public class SessionsConfiguration : IEntityTypeConfiguration<Sessions>
{
    public void Configure(EntityTypeBuilder<Sessions> builder)
    {
        builder.HasKey(x => new { x.UserId, x.ChatId });
        
        builder.HasOne(x => x.User)
            .WithMany(x => x.Session)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.Chat)
            .WithMany(x => x.Session)
            .HasForeignKey(x => x.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}