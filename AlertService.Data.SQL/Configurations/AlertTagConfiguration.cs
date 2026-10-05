using AlertService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertService.Data.SQL.Configurations;

public class AlertTagConfiguration : IEntityTypeConfiguration<AlertTag>
{
    public void Configure(EntityTypeBuilder<AlertTag> builder)
    {
        builder.ToTable("AlertTags");

        builder.HasKey(alertTag => new { alertTag.AlertId, alertTag.TagId });

        builder.HasOne(alertTag => alertTag.Alert)
            .WithMany(alert => alert.AlertTags)
            .HasForeignKey(alertTag => alertTag.AlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(alertTag => alertTag.Tag)
            .WithMany(tag => tag.AlertTags)
            .HasForeignKey(alertTag => alertTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(alertTag => alertTag.TagId);
    }
}