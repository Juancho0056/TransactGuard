using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace BuildingBlocks.Application.Behaviors;

public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken)))
                .ConfigureAwait(false);

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(error => error is not null)
                .Cast<ValidationFailure>()
                .ToList();

            if (failures.Count != 0)
            {
                throw new Exceptions.ValidationException(failures);
            }
        }

        return await next().ConfigureAwait(false);
    }
}
