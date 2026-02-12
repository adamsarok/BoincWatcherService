using BoincRpc;
using BoincWatchService.Services;
using static BoincWatchService.Services.HostState;

namespace BoincWatcherService.Tests.Helpers;

public class HostStateBuilder
{
    private string _ip = "127.0.0.1";
    private string _hostName = "TestHost";
    private HostStates _state = HostStates.OK;
    private List<Project> _projects = new();
    private List<Result> _results = new();
    private string? _errorMsg;

    public HostStateBuilder WithIP(string ip)
    {
        _ip = ip;
        return this;
    }

    public HostStateBuilder WithHostName(string hostName)
    {
        _hostName = hostName;
        return this;
    }

    public HostStateBuilder WithState(HostStates state)
    {
        _state = state;
        return this;
    }

    public HostStateBuilder WithError(string errorMsg)
    {
        _errorMsg = errorMsg;
        _state = HostStates.Down;
        return this;
    }

    public HostStateBuilder WithProjects(params Project[] projects)
    {
        _projects.AddRange(projects);
        return this;
    }

    public HostStateBuilder WithResults(params Result[] results)
    {
        _results.AddRange(results);
        return this;
    }

    public HostState Build()
    {
        var hostState = new HostState
        {
            IP = _ip,
            HostName = _hostName,
            State = _state,
            ErrorMsg = _errorMsg,
            TasksStarted = _results.Count(r => r.CurrentCpuTime.TotalSeconds > 1)
        };

        if (_state != HostStates.Down && (_projects.Any() || _results.Any()))
        {
            var hostInfo = BoincRpcFactory.CreateHostInfo(_hostName);

            var coreClientState = BoincRpcFactory.CreateCoreClientState(
                hostInfo: hostInfo,
                projects: _projects,
                results: _results);

            hostState.CoreClientState = coreClientState;

            if (_results.Any())
            {
                hostState.LatestTaskDownloadTimePerProjectUrl = _results
                    .GroupBy(r => r.ProjectUrl)
                    .ToDictionary(g => g.Key, g => g.Max(r => r.ReceivedTime));
            }
        }

        return hostState;
    }

    public static Project CreateProject(string name, string masterUrl, double userTotalCredit = 1000, double hostTotalCredit = 500)
    {
        return BoincRpcFactory.CreateProject(name, masterUrl, userTotalCredit, hostTotalCredit);
    }

    public static Result CreateResult(string projectUrl, DateTimeOffset receivedTime)
    {
        return BoincRpcFactory.CreateResult(projectUrl: projectUrl, receivedTime: receivedTime);
    }
}
