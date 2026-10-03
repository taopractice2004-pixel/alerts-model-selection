using AlertService.Common.Constants;
using AlertService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertService.Data.SQL.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(AlertConstants.TitleMaxLength);

        builder.Property(a => a.Description)
            .HasMaxLength(AlertConstants.DescriptionMaxLength);

        // Store the enum as a readable string instead of an int.
        builder.Property(a => a.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Dates are stored as UTC; mark them as UTC again when reading back.
        builder.Property(a => a.CreatedDate)
            .IsRequired()
            .HasColumnType("datetime2")
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // No HasDefaultValue(true) here: EF would skip inserting "false" (the CLR default)
        // and the database default would silently turn it into "true".
        builder.Property(a => a.IsActive)
            .IsRequired();

        builder.HasIndex(a => a.IsActive);

        builder.HasMany(a => a.Tags)
            .WithMany(t => t.Alerts)
            .UsingEntity<Dictionary<string, object>>(
                "AlertTags",
                right => right
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey("TagId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<Alert>()
                    .WithMany()
                    .HasForeignKey("AlertId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("AlertId", "TagId");
                    join.ToTable("AlertTags");
                    join.HasIndex("TagId");
                });
    }
}
