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

        // Tags are shared/global rows: the same tag text (case-insensitively) is never
        // duplicated. SQL Server's default collation is case-insensitive, so a unique index
        // on Name enforces this at the database level too.
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
