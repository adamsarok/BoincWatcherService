using System.Collections.Generic;
using System.Threading.Tasks;

namespace BoincWatchService.Services.Interfaces;

public interface IBoincService {
	public Task<IEnumerable<HostState>> GetHostStates();
}
