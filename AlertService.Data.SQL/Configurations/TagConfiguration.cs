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

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(AlertConstants.TagMaxLength);

        // Case-insensitive uniqueness relies on the database's default CI collation so an
        // alert never links two global tags that differ only by case.
        builder.HasIndex(t => t.Name)
            .IsUnique();

        builder.HasMany(t => t.Alerts)
            .WithMany(a => a.Tags)
            .UsingEntity("AlertTag");
    }
}
