using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Application.OperationResults;
using Shared.Kernel.Common.Exceptions;

namespace Shared.Kernel.Common.Middlewares;

public sealed class NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException notFoundException)
        {
            return false;
        }

        logger.LogError(
            notFoundException,
            "Exception occurred: {Message}",
            notFoundException.Message);

        var problem = OperationResult.Fail(exception.Message, true, StatusCodes.Status404NotFound);


        httpContext.Response.StatusCode = problem.StatusCode;

        await httpContext.Response
            .WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}