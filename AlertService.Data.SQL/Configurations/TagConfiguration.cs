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

        builder.Property(t => t.NormalizedName)
            .IsRequired()
            .HasMaxLength(AlertConstants.TagMaxLength);

        builder.HasIndex(t => t.NormalizedName)
            .IsUnique();

        builder.HasMany(t => t.Alerts)
            .WithMany(a => a.Tags)
            .UsingEntity<Dictionary<string, object>>(
                "AlertTags",
                join => join.HasOne<Alert>().WithMany().HasForeignKey("AlertId").OnDelete(DeleteBehavior.Cascade),
                join => join.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("AlertTags");
                    join.HasKey("AlertId", "TagId");
                    join.HasIndex("TagId");
                });
    }
}
