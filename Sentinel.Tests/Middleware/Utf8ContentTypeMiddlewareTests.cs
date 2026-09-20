using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Sentinel.Middleware;

namespace Sentinel.Tests.Middleware;

public sealed class Utf8ContentTypeMiddlewareTests
{
    [Theory]
    [InlineData("text/html")]
    [InlineData("text/csv")]
    [InlineData("application/json")]
    [InlineData("application/problem+json")]
    [InlineData("application/xml")]
    [InlineData("image/svg+xml")]
    public async Task InvokeAsync_AddsUtf8CharsetToTextualResponse(string contentType)
    {
        var context = new DefaultHttpContext();
        var responseFeature = new StartableResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        var middleware = new Utf8ContentTypeMiddleware(async httpContext =>
        {
            httpContext.Response.ContentType = contentType;
            await httpContext.Response.WriteAsync("test response");
        });

        await middleware.InvokeAsync(context);
        await responseFeature.StartAsync();

        Assert.Equal($"{contentType}; charset=utf-8", context.Response.ContentType);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/octet-stream")]
    [InlineData("text/html; charset=iso-8859-1")]
    public async Task InvokeAsync_LeavesBinaryAndAlreadyEncodedResponsesUnchanged(string contentType)
    {
        var context = new DefaultHttpContext();
        var responseFeature = new StartableResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        var middleware = new Utf8ContentTypeMiddleware(async httpContext =>
        {
            httpContext.Response.ContentType = contentType;
            await httpContext.Response.WriteAsync("test response");
        });

        await middleware.InvokeAsync(context);
        await responseFeature.StartAsync();

        Assert.Equal(contentType, context.Response.ContentType);
    }

    private sealed class StartableResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted { get; private set; }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            ArgumentNullException.ThrowIfNull(callback);
            if (HasStarted)
            {
                throw new InvalidOperationException("The response has already started.");
            }

            _onStarting.Add((callback, state));
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            // Completion callbacks are not relevant to this response-header test.
        }

        public async Task StartAsync()
        {
            if (HasStarted)
            {
                return;
            }

            for (var index = _onStarting.Count - 1; index >= 0; index--)
            {
                var callback = _onStarting[index];
                await callback.Callback(callback.State);
            }

            HasStarted = true;
        }
    }
}
