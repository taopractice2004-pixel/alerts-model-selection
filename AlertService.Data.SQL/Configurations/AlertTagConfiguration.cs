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
    }
}