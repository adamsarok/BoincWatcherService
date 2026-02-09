using System.Net;

namespace BoincWatcherService.Tests.Helpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _content = "{}";
    private Exception? _exception;

    public void SetResponse(HttpStatusCode statusCode, string content = "{}")
    {
        _statusCode = statusCode;
        _content = content;
        _exception = null;
    }

    public void SetException(Exception exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_exception != null)
        {
            throw _exception;
        }

        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_content)
        });
    }
}
