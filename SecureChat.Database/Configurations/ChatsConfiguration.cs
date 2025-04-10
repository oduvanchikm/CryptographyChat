using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Common.Models;

namespace SecureChat.Database.Configurations;

public class ChatsConfiguration : IEntityTypeConfiguration<Chats>
{
    public void Configure(EntityTypeBuilder<Chats> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Algorithm)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasMany(x => x.ChatUser)
            .WithOne(ch => ch.Chat)
            .HasForeignKey(ch => ch.ChatId);
    }
}