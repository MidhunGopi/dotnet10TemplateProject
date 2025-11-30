using FluentValidation;

namespace DotNet10Template.API.Middleware;

/// <summary>
/// Validation filter for automatic request validation
/// </summary>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument == null)
        {
            return await next(context);
        }

        var validationResult = await _validator.ValidateAsync(argument);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        return await next(context);
    }
}
