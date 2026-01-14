using BoincWatcherService.Models;
using Microsoft.EntityFrameworkCore;

namespace BoincWatchService.Data;

public class StatsDbContext : DbContext {
	public StatsDbContext(DbContextOptions<StatsDbContext> options) : base(options) {
	}

	public DbSet<HostStats> HostStats { get; set; } = null!;
	public DbSet<ProjectStats> ProjectStats { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<HostStats>()
			.HasKey(x => new { x.YYYYMMDD, x.HostName });
		modelBuilder.Entity<HostStats>()
			.Property(x => x.YYYYMMDD).HasMaxLength(8);

		modelBuilder.Entity<ProjectStats>()
			.HasKey(x => new { x.YYYYMMDD, x.ProjectName });
		modelBuilder.Entity<ProjectStats>()
			.Property(x => x.YYYYMMDD).HasMaxLength(8);
	}
}
