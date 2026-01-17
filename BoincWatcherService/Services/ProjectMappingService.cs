using BoincWatcherService.Models;
using BoincWatcherService.Services.Interfaces;
using BoincWatchService.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BoincWatcherService.Services;

public class ProjectMappingService(StatsDbContext context) : IProjectMappingService {
	/// <summary>
	/// Both project name and url can change, so we check both for matches.
	/// </summary>
	/// <param name="projectName"></param>
	/// <param name="projectUrl"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<Project> GetOrCreateProject(string projectName, string projectUrl, CancellationToken cancellationToken) {
		var projectNameNormalized = projectName.ToLower().Trim();
		var projectUrlNormalized = projectUrl.ToLower().Trim();
		var existingProjectMapping = await context.ProjectMappings
			.Include(x => x.Project)
			.FirstOrDefaultAsync(p => p.ProjectName == projectNameNormalized || p.ProjectUrl == projectUrlNormalized, cancellationToken);
		if (existingProjectMapping != null) {
			return existingProjectMapping.Project;
		}
		var project = new Models.Project {
			ProjectId = Guid.NewGuid(),
			ProjectNameDisplay = projectName,
		};
		var newMapping = new Models.ProjectMapping {
			ProjectName = projectNameNormalized,
			ProjectUrl = projectUrlNormalized,
			ProjectId = project.ProjectId,
			Project = project,
		};
		await context.Projects.AddAsync(project, cancellationToken);
		await context.ProjectMappings.AddAsync(newMapping, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
		return newMapping.Project;
	}

}
