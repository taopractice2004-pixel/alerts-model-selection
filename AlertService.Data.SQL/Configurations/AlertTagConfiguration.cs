using AlertService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertService.Data.SQL.Configurations;

public class AlertTagConfiguration : IEntityTypeConfiguration<AlertTag>
{
    public void Configure(EntityTypeBuilder<AlertTag> builder)
    {
        builder.ToTable("AlertTags");

        builder.HasKey(at => new { at.AlertId, at.TagId });

        builder.HasOne(at => at.Alert)
            .WithMany(a => a.AlertTags)
            .HasForeignKey(at => at.AlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(at => at.Tag)
            .WithMany(t => t.AlertTags)
            .HasForeignKey(at => at.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(at => at.TagId);
    }
}