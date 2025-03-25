using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureChat.Common.Models;

namespace SecureChat.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.PasswordHash)
            .IsRequired();
        
        builder.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PublicKey)
            .IsRequired();
        
        builder.HasMany(x => x.Session)
            .WithOne(ch => ch.User)
            .HasForeignKey(ch => ch.UserId);
    }
}