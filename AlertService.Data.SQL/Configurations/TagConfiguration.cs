using AlertService.Common.Constants;
using AlertService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertService.Data.SQL.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(tag => tag.Id);

        builder.Property(tag => tag.Name)
            .IsRequired()
            .HasMaxLength(AlertConstants.TagMaxLength);

        builder.Property(tag => tag.NormalizedName)
            .IsRequired()
            .HasMaxLength(AlertConstants.TagMaxLength);

        builder.HasIndex(tag => tag.NormalizedName)
            .IsUnique();

        builder.HasMany(tag => tag.AlertTags)
            .WithOne(alertTag => alertTag.Tag)
            .HasForeignKey(alertTag => alertTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}