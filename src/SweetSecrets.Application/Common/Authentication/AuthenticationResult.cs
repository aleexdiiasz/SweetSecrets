namespace SweetSecrets.Application.Common.Authentication;

public class AuthenticationResult
{
    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    public string? Message { get; set; }

    public Guid? UserId { get; set; }

    public Guid? SessionId { get; set; }

    public Guid? TenantId { get; set; }
}