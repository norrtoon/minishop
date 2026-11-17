using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Exceptions;

namespace MiniShop.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, title, detail) = ex switch
        {
            AppException app => (app.StatusCode, app.Title, app.Message),

            DbUpdateException => (StatusCodes.Status409Conflict, "Конфликт данных",
                "Операция нарушает ограничения базы данных."),

            BadHttpRequestException bad => (bad.StatusCode, "Некорректный запрос", bad.Message),

            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера",
                env.IsDevelopment() ? ex.Message : "Произошла непредвиденная ошибка. Попробуйте позже.")
        };

        if (status >= 500)
            logger.LogError(ex, "Необработанная ошибка при запросе {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning("Запрос {Method} {Path} завершился с кодом {Status}: {Message}",
                context.Request.Method, context.Request.Path, status, ex.Message);

        if (context.Response.HasStarted)
            return;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
