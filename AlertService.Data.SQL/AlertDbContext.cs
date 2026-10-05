using AlertService.Models;
using Microsoft.EntityFrameworkCore;

namespace AlertService.Data.SQL;

public class AlertDbContext : DbContext
{
    public AlertDbContext(DbContextOptions<AlertDbContext> options)
        : base(options)
    {
    }

    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlertDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
