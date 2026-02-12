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

public class StatsJobTests {
	[Fact]
	public async Task Execute_WhenOneHostDown_StatsOnlySavedForAliveHosts() {
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
	public async Task Execute_WhenDatabaseUpsertFails_ThrowsAndLogsError() {
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

		// Act & Assert - Should rethrow so Quartz knows the job failed
		var act = () => job.Execute(jobContext);
		await act.Should().ThrowAsync<Exception>().WithMessage("Database connection failed");

		// Verify error was logged before rethrowing
		logger.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task Execute_WhenAllHostsDown_NoStatsAreSaved() {
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
	public async Task Execute_WithMultipleProjects_SavesAllProjectStats() {
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
	public async Task Execute_WhenBoincServiceThrows_ThrowsAndLogsError() {
		// Arrange
		var logger = Substitute.For<ILogger<StatsJob>>();
		var boincService = Substitute.For<IBoincService>();
		var statsService = Substitute.For<IStatsService>();

		boincService.GetHostStates().ThrowsAsyncForAnyArgs(new Exception("Network failure"));

		var job = new StatsJob(logger, boincService, statsService);

		var jobContext = Substitute.For<IJobExecutionContext>();
		jobContext.CancellationToken.Returns(CancellationToken.None);

		// Act & Assert - Should rethrow so Quartz knows the job failed
		var act = () => job.Execute(jobContext);
		await act.Should().ThrowAsync<Exception>().WithMessage("Network failure");

		logger.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	private CoreClientState CreateMockCoreClientState(string hostName, string projectName, double hostCredit, double userCredit) {
		var masterUrl = $"https://{projectName.ToLower()}.org/";
		var project = BoincRpcFactory.CreateProject(projectName, masterUrl, userCredit, hostCredit);
		var result = BoincRpcFactory.CreateResult(
			projectUrl: masterUrl,
			receivedTime: DateTimeOffset.UtcNow.AddHours(-1));
		var hostInfo = BoincRpcFactory.CreateHostInfo(hostName);

		return BoincRpcFactory.CreateCoreClientState(
			hostInfo: hostInfo,
			projects: [project],
			results: [result]);
	}

	private CoreClientState CreateMockCoreClientStateMultiProject(string hostName) {
		var project1 = BoincRpcFactory.CreateProject("Project1", "https://project1.org/", 2000, 1000);
		var project2 = BoincRpcFactory.CreateProject("Project2", "https://project2.org/", 3000, 1500);

		var result1 = BoincRpcFactory.CreateResult(projectUrl: "https://project1.org/", receivedTime: DateTimeOffset.UtcNow);
		var result2 = BoincRpcFactory.CreateResult(projectUrl: "https://project2.org/", receivedTime: DateTimeOffset.UtcNow);

		var hostInfo = BoincRpcFactory.CreateHostInfo(hostName);

		return BoincRpcFactory.CreateCoreClientState(
			hostInfo: hostInfo,
			projects: [project1, project2],
			results: [result1, result2]);
	}
}
