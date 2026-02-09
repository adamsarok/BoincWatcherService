using BoincRpc;
using BoincWatcherService.Models;
using BoincWatcherService.Services.Interfaces;
using BoincWatcherService.Tests.Helpers;
using BoincWatchService.Data;
using BoincWatchService.Jobs;
using BoincWatchService.Services;
using BoincWatchService.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Quartz;
using static BoincWatchService.Services.HostState;

namespace BoincWatcherService.Tests.Jobs;

public class StatsJobTests
{
    [Fact]
    public async Task Execute_WhenOneHostDown_StatsOnlySavedForAliveHosts()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsJob>>();
        var boincService = Substitute.For<IBoincService>();

        var options = new DbContextOptionsBuilder<StatsDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        await using var context = new StatsDbContext(options);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var functionAppService = Substitute.For<IFunctionAppService>();
        functionAppService.IsEnabled.Returns(false);

        var statsService = new StatsService(
            Substitute.For<ILogger<StatsService>>(),
            context,
            httpClientFactory,
            functionAppService);

        var job = new StatsJob(logger, boincService, statsService);

        // Setup: 2 hosts, one down
        var hostStates = new List<HostState>
        {
            new HostState
            {
                HostName = "Host1",
                IP = "192.168.1.1",
                State = HostStates.Down,
                ErrorMsg = "Connection failed"
            },
            new HostState
            {
                HostName = "Host2",
                IP = "192.168.1.2",
                State = HostStates.OK,
                CoreClientState = CreateMockCoreClientState("Host2", "TestProject", 500, 1000)
            }
        };

        boincService.GetHostStates().Returns(hostStates);

        var jobContext = Substitute.For<IJobExecutionContext>();
        jobContext.CancellationToken.Returns(CancellationToken.None);

        // Act
        await job.Execute(jobContext);

        // Assert
        var savedHostStats = await context.HostStats.ToListAsync();
        savedHostStats.Should().HaveCount(1);
        savedHostStats[0].HostName.Should().Be("Host2");
        savedHostStats[0].TotalCredit.Should().Be(500);

        var savedProjectStats = await context.ProjectStats.ToListAsync();
        savedProjectStats.Should().HaveCount(1);
        savedProjectStats[0].ProjectName.Should().Be("TestProject");
    }

    [Fact]
    public async Task Execute_WhenDatabaseUpsertFails_JobCompletesWithoutThrowing()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsJob>>();
        var boincService = Substitute.For<IBoincService>();
        var statsService = Substitute.For<IStatsService>();

        // Make stats service throw exception
        statsService.UpsertHostStats(Arg.Any<HostStats>(), Arg.Any<CancellationToken>())
            .ThrowsAsyncForAnyArgs(new Exception("Database connection failed"));

        var job = new StatsJob(logger, boincService, statsService);

        var hostStates = new List<HostState>
        {
            new HostState
            {
                HostName = "Host1",
                IP = "192.168.1.1",
                State = HostStates.OK,
                CoreClientState = CreateMockCoreClientState("Host1", "TestProject", 500, 1000)
            }
        };

        boincService.GetHostStates().Returns(hostStates);

        var jobContext = Substitute.For<IJobExecutionContext>();
        jobContext.CancellationToken.Returns(CancellationToken.None);

        // Act & Assert - Should not throw
        var act = () => job.Execute(jobContext);
        await act.Should().NotThrowAsync();

        // Verify error was logged
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Execute_WhenAllHostsDown_NoStatsAreSaved()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsJob>>();
        var boincService = Substitute.For<IBoincService>();

        var options = new DbContextOptionsBuilder<StatsDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        await using var context = new StatsDbContext(options);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var functionAppService = Substitute.For<IFunctionAppService>();
        functionAppService.IsEnabled.Returns(false);

        var statsService = new StatsService(
            Substitute.For<ILogger<StatsService>>(),
            context,
            httpClientFactory,
            functionAppService);

        var job = new StatsJob(logger, boincService, statsService);

        var hostStates = new List<HostState>
        {
            new HostState
            {
                HostName = "Host1",
                IP = "192.168.1.1",
                State = HostStates.Down,
                ErrorMsg = "Connection failed"
            },
            new HostState
            {
                HostName = "Host2",
                IP = "192.168.1.2",
                State = HostStates.Down,
                ErrorMsg = "Timeout"
            }
        };

        boincService.GetHostStates().Returns(hostStates);

        var jobContext = Substitute.For<IJobExecutionContext>();
        jobContext.CancellationToken.Returns(CancellationToken.None);

        // Act
        await job.Execute(jobContext);

        // Assert
        var savedHostStats = await context.HostStats.ToListAsync();
        savedHostStats.Should().BeEmpty();

        var savedProjectStats = await context.ProjectStats.ToListAsync();
        savedProjectStats.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_WithMultipleProjects_SavesAllProjectStats()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsJob>>();
        var boincService = Substitute.For<IBoincService>();

        var options = new DbContextOptionsBuilder<StatsDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        await using var context = new StatsDbContext(options);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var functionAppService = Substitute.For<IFunctionAppService>();
        functionAppService.IsEnabled.Returns(false);

        var statsService = new StatsService(
            Substitute.For<ILogger<StatsService>>(),
            context,
            httpClientFactory,
            functionAppService);

        var job = new StatsJob(logger, boincService, statsService);

        var hostStates = new List<HostState>
        {
            new HostState
            {
                HostName = "Host1",
                IP = "192.168.1.1",
                State = HostStates.OK,
                CoreClientState = CreateMockCoreClientStateMultiProject("Host1")
            }
        };

        boincService.GetHostStates().Returns(hostStates);

        var jobContext = Substitute.For<IJobExecutionContext>();
        jobContext.CancellationToken.Returns(CancellationToken.None);

        // Act
        await job.Execute(jobContext);

        // Assert
        var savedProjectStats = await context.ProjectStats.ToListAsync();
        savedProjectStats.Should().HaveCount(2);
        savedProjectStats.Should().Contain(p => p.ProjectName == "Project1" && p.TotalCredit == 2000);
        savedProjectStats.Should().Contain(p => p.ProjectName == "Project2" && p.TotalCredit == 3000);

        var savedHostProjectStats = await context.HostProjectStats.ToListAsync();
        savedHostProjectStats.Should().HaveCount(2);
    }

    [Fact]
    public async Task Execute_WhenBoincServiceThrows_JobCompletesWithoutThrowing()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsJob>>();
        var boincService = Substitute.For<IBoincService>();
        var statsService = Substitute.For<IStatsService>();

        boincService.GetHostStates().ThrowsAsyncForAnyArgs(new Exception("Network failure"));

        var job = new StatsJob(logger, boincService, statsService);

        var jobContext = Substitute.For<IJobExecutionContext>();
        jobContext.CancellationToken.Returns(CancellationToken.None);

        // Act & Assert
        var act = () => job.Execute(jobContext);
        await act.Should().NotThrowAsync();

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private CoreClientState CreateMockCoreClientState(string hostName, string projectName, double hostCredit, double userCredit)
    {
        var project = Substitute.For<Project>();
        project.ProjectName.Returns(projectName);
        project.MasterUrl.Returns($"https://{projectName.ToLower()}.org/");
        project.UserTotalCredit.Returns(userCredit);
        project.HostTotalCredit.Returns(hostCredit);

        var result = Substitute.For<Result>();
        result.ProjectUrl.Returns($"https://{projectName.ToLower()}.org/");
        result.ReceivedTime.Returns(DateTimeOffset.UtcNow.AddHours(-1));
        result.CurrentCpuTime.Returns(TimeSpan.FromSeconds(100));

        var hostInfo = Substitute.For<HostInfo>();
        hostInfo.DomainName.Returns(hostName);

        var coreClientState = Substitute.For<CoreClientState>();
        coreClientState.HostInfo.Returns(hostInfo);
        coreClientState.Projects.Returns(new List<Project> { project });
        coreClientState.Results.Returns(new List<Result> { result });

        return coreClientState;
    }

    private CoreClientState CreateMockCoreClientStateMultiProject(string hostName)
    {
        var project1 = Substitute.For<Project>();
        project1.ProjectName.Returns("Project1");
        project1.MasterUrl.Returns("https://project1.org/");
        project1.UserTotalCredit.Returns(2000.0);
        project1.HostTotalCredit.Returns(1000.0);

        var project2 = Substitute.For<Project>();
        project2.ProjectName.Returns("Project2");
        project2.MasterUrl.Returns("https://project2.org/");
        project2.UserTotalCredit.Returns(3000.0);
        project2.HostTotalCredit.Returns(1500.0);

        var result1 = Substitute.For<Result>();
        result1.ProjectUrl.Returns("https://project1.org/");
        result1.ReceivedTime.Returns(DateTimeOffset.UtcNow);
        result1.CurrentCpuTime.Returns(TimeSpan.FromSeconds(100));

        var result2 = Substitute.For<Result>();
        result2.ProjectUrl.Returns("https://project2.org/");
        result2.ReceivedTime.Returns(DateTimeOffset.UtcNow);
        result2.CurrentCpuTime.Returns(TimeSpan.FromSeconds(100));

        var hostInfo = Substitute.For<HostInfo>();
        hostInfo.DomainName.Returns(hostName);

        var coreClientState = Substitute.For<CoreClientState>();
        coreClientState.HostInfo.Returns(hostInfo);
        coreClientState.Projects.Returns(new List<Project> { project1, project2 });
        coreClientState.Results.Returns(new List<Result> { result1, result2 });

        return coreClientState;
    }
}
