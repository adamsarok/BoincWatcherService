
using BoincWatcherService.Models;
using BoincWatchService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatcherService.Data;

public class EntityInterceptor : SaveChangesInterceptor {
	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) {
		if (eventData.Context is StatsDbContext context) UpdateEntities(context);
		return base.SavingChanges(eventData, result);
	}
	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
		if (eventData.Context is StatsDbContext context) UpdateEntities(context);
		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}
	private static void UpdateEntities(StatsDbContext? context) {
		if (context == null) return;
		foreach (var entry in context.ChangeTracker.Entries<Entity>()) {
			if (entry.State == EntityState.Added) {
				entry.Entity.CreatedAt = DateTime.UtcNow;
			}
			if (entry.State == EntityState.Added || entry.State == EntityState.Modified) {
				entry.Entity.UpdatedAt = DateTime.UtcNow;
			}
		}
	}
}
