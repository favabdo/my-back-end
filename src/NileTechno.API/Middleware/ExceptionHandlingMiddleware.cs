using System.Net;
using System.Text.Json;
using NileTechno.Application.Common.Exceptions;
using ValidationException = NileTechno.Application.Common.Exceptions.ValidationException;

namespace NileTechno.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object payload;

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                payload = new { title = "بيانات غير صحيحة", status = 400, errors = validationEx.Errors };
                break;

            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                payload = new { title = notFoundEx.Message, status = 404 };
                break;

            case ForbiddenAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                payload = new { title = "ليس لديك صلاحية للقيام بهذه العملية", status = 403 };
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                payload = new { title = "غير مصرح لك بالدخول", status = 401 };
                break;

            default:
                _logger.LogError(exception, "خطأ غير متوقع في الخادم");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                payload = new { title = "حصل خطأ غير متوقع في الخادم، برجاء المحاولة لاحقًا", status = 500 };
                break;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
