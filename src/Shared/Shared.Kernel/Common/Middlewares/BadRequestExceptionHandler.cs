using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Application.OperationResults;
using Shared.Kernel.Common.Exceptions;

namespace Shared.Kernel.Common.Middlewares;

public sealed class BadRequestExceptionHandler(ILogger<BadRequestExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadRequestException badRequestException)
        {
            return false;
        }

        logger.LogError(
            badRequestException,
            "Exception occurred: {Message}",
            badRequestException.Message);

        var problem = OperationResult.Fail(exception.Message, true, StatusCodes.Status400BadRequest);

        httpContext.Response.StatusCode = problem.StatusCode;

        await httpContext.Response
            .WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}