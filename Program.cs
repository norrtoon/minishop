using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Extensions;
using MiniShop.Middleware;
using MiniShop.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDatabase(builder.Configuration)
    .AddApplicationServices()
    .AddJwtAuth(builder.Configuration)
    .AddSwaggerWithJwt();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        var problem = ctx.ProblemDetails;
        switch (problem.Status)
        {
            case StatusCodes.Status400BadRequest when problem is Microsoft.AspNetCore.Mvc.ValidationProblemDetails:
                problem.Title = "Ошибка валидации";
                problem.Detail ??= "Проверьте правильность заполнения полей.";
                break;
            case StatusCodes.Status401Unauthorized:
                problem.Title = "Требуется аутентификация";
                problem.Detail ??= "Войдите в систему: запрос требует действительный токен.";
                break;
            case StatusCodes.Status403Forbidden:
                problem.Title = "Доступ запрещён";
                problem.Detail ??= "У вас недостаточно прав для этого действия.";
                break;
            case StatusCodes.Status404NotFound:
                problem.Title = "Не найдено";
                break;
            case StatusCodes.Status405MethodNotAllowed:
                problem.Title = "Метод не поддерживается";
                break;
        }
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(o => o.DocumentTitle = "MiniShop API");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallback("/api/{**path}", () => Results.Problem(
    statusCode: StatusCodes.Status404NotFound,
    title: "Не найдено",
    detail: "Такого метода API не существует."));

app.Run();
