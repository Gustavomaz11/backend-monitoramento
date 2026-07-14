using FluentValidation;

namespace SafeNavigation.Api.Validation;

public static class RequestValidation
{
    public static async Task EnsureValidAsync<T>(this IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid) return;

        throw new SafeNavigation.Application.Errors.ValidationFailedException(
            string.Join(" ", result.Errors.Select(x => x.ErrorMessage)));
    }
}
