using BoincWatcherService.Models;
using Microsoft.EntityFrameworkCore;

namespace BoincWatchService.Data;

public class StatsDbContext : DbContext {
	public StatsDbContext(DbContextOptions<StatsDbContext> options) : base(options) {
	}

	public DbSet<HostStats> HostStats { get; set; } = null!;
	public DbSet<HostProjectStats> HostProjectStats { get; set; } = null!;
	public DbSet<ProjectStats> ProjectStats { get; set; } = null!;
	public DbSet<BoincTask> BoincTasks { get; set; } = null!;
	public DbSet<BoincApp> BoincApps { get; set; } = null!;
	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<HostStats>()
			.HasKey(x => new { x.YYYYMMDD, x.HostName });
		modelBuilder.Entity<HostStats>()
			.Property(x => x.YYYYMMDD).HasMaxLength(8);

		modelBuilder.Entity<HostProjectStats>()
			.HasKey(x => new { x.YYYYMMDD, x.HostName, x.ProjectName });
		modelBuilder.Entity<HostProjectStats>()
			.Property(x => x.YYYYMMDD).HasMaxLength(8);

		modelBuilder.Entity<ProjectStats>()
			.HasKey(x => new { x.YYYYMMDD, x.ProjectName });
		modelBuilder.Entity<ProjectStats>()
			.Property(x => x.YYYYMMDD).HasMaxLength(8);

		modelBuilder.Entity<BoincTask>()
			.HasKey(x => new { x.ProjectName, x.TaskName, x.HostName });

		modelBuilder.Entity<BoincApp>()
			.HasKey(x => new { x.ProjectName, x.Name });


	}
}
