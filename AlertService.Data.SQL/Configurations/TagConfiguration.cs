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

        // Tag names are stored in a single canonical representation; the unique index enforces
        // case-insensitive uniqueness under the database's default (case-insensitive) collation.
        builder.HasIndex(t => t.Name)
            .IsUnique();
    }
}
