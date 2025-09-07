using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Shared.Kernel.Application.OperationResults;

namespace Shared.Kernel.Common.Middlewares;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception, "Exception occurred: {Message}", exception.Message);

        var problem = OperationResult.Fail(exception.Message, true, StatusCodes.Status500InternalServerError);

        httpContext.Response.StatusCode = problem.StatusCode;

        await httpContext.Response
            .WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}