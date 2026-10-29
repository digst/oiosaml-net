using System.Linq;
using DK.Gov.Oio.Saml.Exceptions;
using DK.Gov.Oio.Saml.Schema.Protocol;
using FluentValidation.Results;

namespace DK.Gov.Oio.Saml.Extensions;

internal static class FluentValidationExtensions
{
    extension(ValidationResult result)
    {
        public string GetValidationErrors() => string.Join(", ", result.Errors.Select(x => x.ErrorMessage));

        // A failed status check carries the IdP's Status as its custom state.
        public Saml20ValidationException ToException() =>
            result.Errors.Select(x => x.CustomState).OfType<Status>().FirstOrDefault() is { } status
                ? new Saml20StatusException(result.GetValidationErrors(), status)
                : new Saml20ValidationException(result.GetValidationErrors());
    }
}
