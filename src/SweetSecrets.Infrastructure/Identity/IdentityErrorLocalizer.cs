using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace SweetSecrets.Infrastructure.Identity;

public sealed class IdentityErrorLocalizer
{
    private readonly IdentityOptions _options;

    public IdentityErrorLocalizer(IOptions<IdentityOptions> options)
    {
        _options = options.Value;
    }

    public string Localize(IEnumerable<IdentityError> errors)
    {
        var messages = errors
            .Select(Localize)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return messages.Count == 0
            ? "No fue posible completar la operación."
            : string.Join(" ", messages);
    }

    public string Localize(IdentityError error) => error.Code switch
    {
        "PasswordMismatch" => "La contraseña actual es incorrecta.",
        "PasswordTooShort" => $"La contraseña debe tener al menos {_options.Password.RequiredLength} caracteres.",
        "PasswordRequiresNonAlphanumeric" => "La contraseña debe contener al menos un carácter especial.",
        "PasswordRequiresDigit" => "La contraseña debe contener al menos un dígito.",
        "PasswordRequiresLower" => "La contraseña debe contener al menos una letra minúscula.",
        "PasswordRequiresUpper" => "La contraseña debe contener al menos una letra mayúscula.",
        "PasswordRequiresUniqueChars" => $"La contraseña debe contener al menos {_options.Password.RequiredUniqueChars} caracteres diferentes.",
        "InvalidToken" => "El enlace de recuperación no es válido o ya expiró.",
        _ => "No fue posible completar la operación solicitada."
    };
}
