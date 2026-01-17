using System.Threading;
using System.Threading.Tasks;

namespace BoincWatcherService.Services.Interfaces;

public interface IProjectMappingService {
	Task<Models.Project> GetOrCreateProject(string projectName, string projectUrl, CancellationToken cancellationToken);
}
