using System.Net;
using System.Text.Json;
using Core.Exceptions;

namespace Api.Middlewares;

/// <summary>
/// Turns exceptions into a JSON <see cref="ExceptionResult"/>.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, IHostEnvironment environment, ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>
    /// Invokes the rest of the pipeline.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ExceptionResult
        {
            TraceId = context.TraceIdentifier,
        };

        if (exception is IAppException appException)
        {
            context.Response.StatusCode = (int)appException.Code;
            response.Message = exception.Message;
            if (exception is BadRequestException badRequest)
            {
                response.Detail = badRequest.Detail;
            }
            response.Exception = environment.IsDevelopment() ? exception.StackTrace : null;
            logger.LogWarning(exception, "Business error {TraceId} {Message}", context.TraceIdentifier, exception.Message);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.Message = environment.IsDevelopment() ? exception.Message : "ERR_INTERNAL";
            response.Exception = environment.IsDevelopment() ? exception.StackTrace : null;
            logger.LogError(exception, "Unhandled error {TraceId}", context.TraceIdentifier);
        }

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
