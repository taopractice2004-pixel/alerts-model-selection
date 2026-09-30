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

        builder.HasIndex(t => t.Name).IsUnique();

        builder.HasMany(t => t.Alerts)
            .WithMany(a => a.Tags)
            .UsingEntity<Dictionary<string, object>>(
                "AlertTags",
                j => j.HasOne<Alert>().WithMany().HasForeignKey("AlertId").HasConstraintName("FK_AlertTags_Alerts_AlertId"),
                j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId").HasConstraintName("FK_AlertTags_Tags_TagId"),
                j =>
                {
                    j.HasKey("AlertId", "TagId");
                    j.ToTable("AlertTags");
                });
    }
}
