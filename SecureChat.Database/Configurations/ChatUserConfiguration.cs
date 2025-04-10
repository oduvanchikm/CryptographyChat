using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Common.Models;

namespace SecureChat.Database.Configurations;

public class ChatUserConfiguration : IEntityTypeConfiguration<ChatUser>
{
    public void Configure(EntityTypeBuilder<ChatUser> builder)
    {
        builder.HasKey(x => new { x.UserId, x.ChatId });

        builder.HasOne(x => x.User)
            .WithMany(x => x.ChatUser)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Chat)
            .WithMany(x => x.ChatUser)
            .HasForeignKey(x => x.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.JoinedAt)
            .IsRequired();
    }
}