using BoincWatcherService.Services;
using BoincWatcherService.Tests.Helpers;
using BoincWatchService.Options;
using Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;

namespace BoincWatcherService.Tests.Services;

public class FunctionAppServiceTests {
	[Fact]
	public async Task UploadStatsToFunctionApp_When404Response_ReturnsFalseAndLogs() {
		// Arrange
		var logger = Substitute.For<ILogger<FunctionAppService>>();
		var options = Options.Create(new FunctionAppOptions {
			BaseUrl = "https://test.azurewebsites.net"
		});

		var service = new FunctionAppService(options, logger);

		var mockHandler = new MockHttpMessageHandler();
		mockHandler.SetResponse(HttpStatusCode.NotFound, "Not found");
		var httpClient = new HttpClient(mockHandler);

		var entity = new StatsTableEntity {
			PartitionKey = "HOST_STATS",
			RowKey = "TestHost",
			CreditTotal = 1000
		};

		// Act
		var result = await service.UploadStatsToFunctionApp(httpClient, entity, CancellationToken.None);

		// Assert
		result.Should().BeFalse();
		logger.Received(1).Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task UploadStatsToFunctionApp_WhenHttpClientThrows_ReturnsFalseAndLogsError() {
		// Arrange
		var logger = Substitute.For<ILogger<FunctionAppService>>();
		var options = Options.Create(new FunctionAppOptions {
			BaseUrl = "https://test.azurewebsites.net"
		});

		var service = new FunctionAppService(options, logger);

		var mockHandler = new MockHttpMessageHandler();
		mockHandler.SetException(new HttpRequestException("Network error"));
		var httpClient = new HttpClient(mockHandler);

		var entity = new StatsTableEntity {
			PartitionKey = "HOST_STATS",
			RowKey = "TestHost",
			CreditTotal = 1000
		};

		// Act
		var result = await service.UploadStatsToFunctionApp(httpClient, entity, CancellationToken.None);

		// Assert
		result.Should().BeFalse();
		logger.Received(1).Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task UploadStatsToFunctionApp_WhenSuccessful_ReturnsTrue() {
		// Arrange
		var logger = Substitute.For<ILogger<FunctionAppService>>();
		var options = Options.Create(new FunctionAppOptions {
			BaseUrl = "https://test.azurewebsites.net"
		});

		var service = new FunctionAppService(options, logger);

		var mockHandler = new MockHttpMessageHandler();
		mockHandler.SetResponse(HttpStatusCode.OK, "{}");
		var httpClient = new HttpClient(mockHandler);

		var entity = new StatsTableEntity {
			PartitionKey = "HOST_STATS",
			RowKey = "TestHost",
			CreditTotal = 1000
		};

		// Act
		var result = await service.UploadStatsToFunctionApp(httpClient, entity, CancellationToken.None);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task UploadAppRuntimeToFunctionApp_When500Response_ReturnsFalseAndLogs() {
		// Arrange
		var logger = Substitute.For<ILogger<FunctionAppService>>();
		var options = Options.Create(new FunctionAppOptions {
			BaseUrl = "https://test.azurewebsites.net"
		});

		var service = new FunctionAppService(options, logger);

		var mockHandler = new MockHttpMessageHandler();
		mockHandler.SetResponse(HttpStatusCode.InternalServerError, "Server error");
		var httpClient = new HttpClient(mockHandler);

		var entity = new AppRuntimeTableEntity("Host1", "Project1", "App1") {
			CPUHoursTotal = 100
		};

		// Act
		var result = await service.UploadAppRuntimeToFunctionApp(httpClient, entity, CancellationToken.None);

		// Assert
		result.Should().BeFalse();
		logger.Received(1).Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task UploadStatsToFunctionApp_WhenBaseUrlMissing_ThrowsInvalidOperationException() {
		// Arrange
		var logger = Substitute.For<ILogger<FunctionAppService>>();
		var options = Options.Create(new FunctionAppOptions {
			BaseUrl = null!
		});

		var service = new FunctionAppService(options, logger);
		var mockHandler = new MockHttpMessageHandler();
		var httpClient = new HttpClient(mockHandler);

		var entity = new StatsTableEntity {
			PartitionKey = "HOST_STATS",
			RowKey = "TestHost",
			CreditTotal = 1000
		};

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => service.UploadStatsToFunctionApp(httpClient, entity, CancellationToken.None));
	}
}
