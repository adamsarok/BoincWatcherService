using System.Reflection;
using System.Runtime.CompilerServices;
using BoincRpc;

namespace BoincWatcherService.Tests.Helpers;

/// <summary>
/// Creates BoincRpc objects for testing. These types have internal constructors
/// and non-virtual properties, so they cannot be mocked with NSubstitute.
/// Uses RuntimeHelpers.GetUninitializedObject to bypass constructors and
/// reflection to set backing fields.
/// </summary>
public static class BoincRpcFactory
{
    public static Project CreateProject(
        string projectName = "TestProject",
        string masterUrl = "https://testproject.org/",
        double userTotalCredit = 1000,
        double hostTotalCredit = 500)
    {
        var project = CreateInstance<Project>();
        SetField(project, nameof(Project.ProjectName), projectName);
        SetField(project, nameof(Project.MasterUrl), masterUrl);
        SetField(project, nameof(Project.UserTotalCredit), userTotalCredit);
        SetField(project, nameof(Project.HostTotalCredit), hostTotalCredit);
        return project;
    }

    public static Result CreateResult(
        string projectUrl = "https://testproject.org/",
        DateTimeOffset? receivedTime = null,
        TimeSpan? currentCpuTime = null,
        TimeSpan? elapsedTime = default,
        TimeSpan? finalCpuTime = default,
        TimeSpan? finalElapsedTime = default,
        TimeSpan? estimatedCpuTimeRemaining = default,
        double fractionDone = 0,
        string name = "TestTask",
        string workunitName = "TestWorkunit")
    {
        var result = CreateInstance<Result>();
        SetField(result, nameof(Result.ProjectUrl), projectUrl);
        SetField(result, nameof(Result.ReceivedTime), receivedTime ?? DateTimeOffset.UtcNow);
        SetField(result, nameof(Result.CurrentCpuTime), currentCpuTime ?? TimeSpan.FromSeconds(100));
        SetField(result, nameof(Result.ElapsedTime), elapsedTime ?? TimeSpan.Zero);
        SetField(result, nameof(Result.FinalCpuTime), finalCpuTime ?? TimeSpan.Zero);
        SetField(result, nameof(Result.FinalElapsedTime), finalElapsedTime ?? TimeSpan.Zero);
        SetField(result, nameof(Result.EstimatedCpuTimeRemaining), estimatedCpuTimeRemaining ?? TimeSpan.Zero);
        SetField(result, nameof(Result.FractionDone), fractionDone);
        SetField(result, nameof(Result.Name), name);
        SetField(result, nameof(Result.WorkunitName), workunitName);
        return result;
    }

    public static HostInfo CreateHostInfo(string domainName = "TestHost")
    {
        var hostInfo = CreateInstance<HostInfo>();
        SetField(hostInfo, nameof(HostInfo.DomainName), domainName);
        return hostInfo;
    }

    public static CoreClientState CreateCoreClientState(
        HostInfo? hostInfo = null,
        IReadOnlyList<Project>? projects = null,
        IReadOnlyList<Result>? results = null,
        IReadOnlyList<App>? apps = null,
        IReadOnlyList<Workunit>? workunits = null)
    {
        var state = CreateInstance<CoreClientState>();
        SetField(state, nameof(CoreClientState.HostInfo), hostInfo ?? CreateHostInfo());
        SetField(state, nameof(CoreClientState.Projects), projects ?? new List<Project>());
        SetField(state, nameof(CoreClientState.Results), results ?? new List<Result>());
        SetField(state, nameof(CoreClientState.Apps), apps ?? new List<App>());
        SetField(state, nameof(CoreClientState.Workunits), workunits ?? new List<Workunit>());
        return state;
    }

    private static T CreateInstance<T>()
    {
        return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    }

    private static void SetField<T>(T obj, string propertyName, object? value) where T : notnull
    {
        // Try auto-property backing field first
        var field = typeof(T).GetField($"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field != null)
        {
            field.SetValue(obj, value);
            return;
        }

        // Fall back to matching private/internal field (lowercase or with underscore prefix)
        field = typeof(T).GetField(char.ToLower(propertyName[0]) + propertyName[1..],
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(T).GetField("_" + char.ToLower(propertyName[0]) + propertyName[1..],
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
            throw new InvalidOperationException(
                $"Could not find backing field for property '{propertyName}' on type '{typeof(T).Name}'.");

        field.SetValue(obj, value);
    }
}
