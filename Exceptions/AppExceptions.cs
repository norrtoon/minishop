namespace MiniShop.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }

    public abstract int StatusCode { get; }

    public abstract string Title { get; }
}

public class BadRequestException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public override string Title => "Некорректный запрос";
}

public class UnauthorizedException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
    public override string Title => "Требуется аутентификация";
}

public class ForbiddenException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
    public override string Title => "Доступ запрещён";
}

public class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public override string Title => "Не найдено";

    public NotFoundException(string entity, object id)
        : this($"{entity} с id={id} не найден(а).") { }
}

public class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public override string Title => "Конфликт";
}
