using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using WebAPI.Filters;
using WebAPI.Middleware;
using WebAPI.ErrorHandling;
using Xunit;

namespace WebAPI.Tests;

public class ErrorHandlingAndCorrelationIdTests
{
    [Fact]
    public void OnException_ShouldReturnStandardizedErrorResponse_WhenUnhandledExceptionOccurs()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-correlation-id";
        httpContext.Request.Path = "/test-path";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var exceptionContext = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new InvalidOperationException("boom")
        };

        var logger = NullLogger<GlobalExceptionFilter>.Instance;
        var filter = new GlobalExceptionFilter(logger);

        // Act
        filter.OnException(exceptionContext);

        // Assert
        var objectResult = exceptionContext.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var payload = objectResult.Value as ApiErrorResponse;
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(StatusCodes.Status500InternalServerError);
        payload.Type.Should().Be(ErrorResponseTypes.GenericErrorType);
        payload.Title.Should().Be(ErrorResponseTitles.GenericErrorTitle);
        payload.Detail.Should().Be(ErrorResponseMapper.GenericClientSafeServerErrorDetail);
        payload.Instance.Should().Be("/test-path");
        payload.CorrelationId.Should().Be("test-correlation-id");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetCorrelationIdHeader_WhenHeaderIsMissing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var logger = NullLogger<CorrelationIdMiddleware>.Instance;

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.TryGetValue(CorrelationIdDefaults.CorrelationIdHeaderName, out var headerValues)
            .Should().BeTrue();

        var headerValue = headerValues.ToString();
        headerValue.Should().NotBeNullOrWhiteSpace();
        context.TraceIdentifier.Should().Be(headerValue);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHonorExistingCorrelationIdHeader_WhenHeaderIsPresent()
    {
        // Arrange
        const string existingCorrelationId = "existing-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdDefaults.CorrelationIdHeaderName] = existingCorrelationId;

        var logger = NullLogger<CorrelationIdMiddleware>.Instance;
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.TraceIdentifier.Should().Be(existingCorrelationId);
        context.Response.Headers[CorrelationIdDefaults.CorrelationIdHeaderName].ToString()
            .Should().Be(existingCorrelationId);
    }
}