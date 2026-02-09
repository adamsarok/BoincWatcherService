using BoincWatcherService.Models;
using BoincWatcherService.Services.Interfaces;
using BoincWatchService.Data;
using BoincWatchService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BoincWatcherService.Tests.Services;

public class StatsServiceTests
{
    [Fact]
    public async Task UpsertAggregateStats_WhenOneFunctionAppCallFails_ContinuesProcessingAll()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsService>>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var functionAppService = Substitute.For<IFunctionAppService>();

        functionAppService.IsEnabled.Returns(true);

        var options = new DbContextOptionsBuilder<StatsDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        await using var context = new StatsDbContext(options);

        // Add test data - 3 hosts
        var today = DateTime.UtcNow.Date;
        var yyyymmdd = today.ToString("yyyyMMdd");

        context.HostStats.AddRange(
            new HostStats { YYYYMMDD = yyyymmdd, HostName = "Host1", TotalCredit = 1000, Timestamp = DateTimeOffset.UtcNow },
            new HostStats { YYYYMMDD = yyyymmdd, HostName = "Host2", TotalCredit = 2000, Timestamp = DateTimeOffset.UtcNow },
            new HostStats { YYYYMMDD = yyyymmdd, HostName = "Host3", TotalCredit = 3000, Timestamp = DateTimeOffset.UtcNow }
        );

        context.ProjectStats.AddRange(
            new ProjectStats { YYYYMMDD = yyyymmdd, ProjectName = "Project1", TotalCredit = 5000 },
            new ProjectStats { YYYYMMDD = yyyymmdd, ProjectName = "Project2", TotalCredit = 6000 }
        );

        await context.SaveChangesAsync();

        var httpClient = new HttpClient();
        httpClientFactory.CreateClient().Returns(httpClient);

        // Make second host upload fail
        var callCount = 0;
        functionAppService.UploadStatsToFunctionApp(Arg.Any<HttpClient>(), Arg.Any<Common.Models.StatsTableEntity>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                callCount++;
                return callCount != 2; // Second call fails
            });

        functionAppService.UploadAppRuntimeToFunctionApp(Arg.Any<HttpClient>(), Arg.Any<Common.Models.AppRuntimeTableEntity>(), Arg.Any<CancellationToken>())
            .Returns(true);

        functionAppService.UploadEfficiencyToFunctionApp(Arg.Any<HttpClient>(), Arg.Any<Common.Models.EfficiencyTableEntity>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var service = new StatsService(logger, context, httpClientFactory, functionAppService);

        // Act
        var result = await service.UpsertAggregateStats();

        // Assert
        result.Should().BeFalse(); // Overall failure because one upload failed

        // Verify all uploads were attempted (3 hosts + 2 projects = 5 stats entities)
        await functionAppService.Received(5).UploadStatsToFunctionApp(
            Arg.Any<HttpClient>(),
            Arg.Any<Common.Models.StatsTableEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertHostStats_WhenDatabaseFails_ReturnsFalseAndLogsError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsService>>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var functionAppService = Substitute.For<IFunctionAppService>();

        // Create a disposed context to simulate database failure
        var options = new DbContextOptionsBuilder<StatsDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new StatsDbContext(options);
        await context.DisposeAsync();

        var service = new StatsService(logger, context, httpClientFactory, functionAppService);

        var hostStats = new HostStats
        {
            YYYYMMDD = "20260101",
            HostName = "TestHost",
            TotalCredit = 1000
        };

        // Act
        var result = await service.UpsertHostStats(hostStats);

        // Assert
        result.Should().BeFalse();
        // Verify error was logged (check that Log was called with LogLevel.Error)
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task UpsertHostStats_WithValidData_InsertsSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsService>>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var functionAppService = Substitute.For<IFunctionAppService>();

        var options = new DbContextOptionsBuilder<StatsDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        await using var context = new StatsDbContext(options);

        var service = new StatsService(logger, context, httpClientFactory, functionAppService);

        var hostStats = new HostStats
        {
            YYYYMMDD = "20260101",
            HostName = "TestHost",
            TotalCredit = 1000
        };

        // Act
        var result = await service.UpsertHostStats(hostStats);

        // Assert
        result.Should().BeTrue();
        var saved = await context.HostStats.FirstOrDefaultAsync(h => h.HostName == "TestHost");
        saved.Should().NotBeNull();
        saved!.TotalCredit.Should().Be(1000);
    }

    [Fact]
    public async Task UpsertHostStats_WithExistingData_UpdatesSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsService>>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var functionAppService = Substitute.For<IFunctionAppService>();

        var options = new DbContextOptionsBuilder<StatsDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        await using var context = new StatsDbContext(options);

        var existing = new HostStats
        {
            YYYYMMDD = "20260101",
            HostName = "TestHost",
            TotalCredit = 1000,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
        };
        context.HostStats.Add(existing);
        await context.SaveChangesAsync();

        var service = new StatsService(logger, context, httpClientFactory, functionAppService);

        var updated = new HostStats
        {
            YYYYMMDD = "20260101",
            HostName = "TestHost",
            TotalCredit = 2000
        };

        // Act
        var result = await service.UpsertHostStats(updated);

        // Assert
        result.Should().BeTrue();
        var saved = await context.HostStats.FirstOrDefaultAsync(h => h.HostName == "TestHost");
        saved!.TotalCredit.Should().Be(2000);
        context.HostStats.Count().Should().Be(1); // Should update, not insert
    }

    [Fact]
    public async Task UpsertHostStats_WithInvalidData_ReturnsFalse()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsService>>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var functionAppService = Substitute.For<IFunctionAppService>();

        var options = new DbContextOptionsBuilder<StatsDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        await using var context = new StatsDbContext(options);

        var service = new StatsService(logger, context, httpClientFactory, functionAppService);

        var hostStats = new HostStats
        {
            YYYYMMDD = "", // Invalid
            HostName = "TestHost",
            TotalCredit = 1000
        };

        // Act
        var result = await service.UpsertHostStats(hostStats);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpsertAggregateStats_WhenFunctionAppDisabled_ReturnsFalse()
    {
        // Arrange
        var logger = Substitute.For<ILogger<StatsService>>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var functionAppService = Substitute.For<IFunctionAppService>();

        functionAppService.IsEnabled.Returns(false);

        var options = new DbContextOptionsBuilder<StatsDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        await using var context = new StatsDbContext(options);

        var service = new StatsService(logger, context, httpClientFactory, functionAppService);

        // Act
        var result = await service.UpsertAggregateStats();

        // Assert
        result.Should().BeFalse();
        await functionAppService.DidNotReceive().UploadStatsToFunctionApp(
            Arg.Any<HttpClient>(),
            Arg.Any<Common.Models.StatsTableEntity>(),
            Arg.Any<CancellationToken>());
    }
}
