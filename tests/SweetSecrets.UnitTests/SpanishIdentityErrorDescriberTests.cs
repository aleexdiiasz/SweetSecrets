using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.UnitTests;

public sealed class SpanishIdentityErrorDescriberTests
{
    private readonly SpanishIdentityErrorDescriber _describer = new();

    [Fact]
    public void PasswordValidationErrors_AreLocalizedInSpanish()
    {
        Assert.Equal(
            "La contraseña debe tener al menos 10 caracteres.",
            _describer.PasswordTooShort(10).Description);
        Assert.Equal(
            "La contraseña debe contener al menos un carácter especial.",
            _describer.PasswordRequiresNonAlphanumeric().Description);
        Assert.Equal(
            "La contraseña debe contener al menos un dígito.",
            _describer.PasswordRequiresDigit().Description);
        Assert.Equal(
            "La contraseña debe contener al menos una letra minúscula.",
            _describer.PasswordRequiresLower().Description);
        Assert.Equal(
            "La contraseña debe contener al menos una letra mayúscula.",
            _describer.PasswordRequiresUpper().Description);
    }
}
