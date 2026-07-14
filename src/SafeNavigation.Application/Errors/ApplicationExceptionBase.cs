namespace SafeNavigation.Application.Errors;

public abstract class ApplicationExceptionBase(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
}

public sealed class ValidationFailedException(string message) : ApplicationExceptionBase(message)
{
    public override int StatusCode => 400;
}

public sealed class UnauthorizedOperationException(string message = "Unauthorized.") : ApplicationExceptionBase(message)
{
    public override int StatusCode => 401;
}

public sealed class ForbiddenOperationException(string message = "Forbidden.") : ApplicationExceptionBase(message)
{
    public override int StatusCode => 403;
}

public sealed class ResourceNotFoundException(string message = "Resource not found.") : ApplicationExceptionBase(message)
{
    public override int StatusCode => 404;
}

public sealed class ConflictException(string message) : ApplicationExceptionBase(message)
{
    public override int StatusCode => 409;
}

public sealed class GoneException(string message) : ApplicationExceptionBase(message)
{
    public override int StatusCode => 410;
}
