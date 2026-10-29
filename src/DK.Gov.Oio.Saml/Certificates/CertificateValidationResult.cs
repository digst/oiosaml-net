namespace DK.Gov.Oio.Saml.Certificates;

public enum CertificateStatus
{
    Valid,
    Expired,
    NotYetValid,
    Revoked,
    RevocationStatusUnknown,
}

public sealed class CertificateValidationResult(CertificateStatus status, string? reason = null)
{
    public static readonly CertificateValidationResult Valid = new(CertificateStatus.Valid);

    public CertificateStatus Status { get; } = status;
    public string? Reason { get; } = reason;
    public bool IsValid => Status == CertificateStatus.Valid;

    public override string ToString() => Reason == null ? Status.ToString() : $"{Status}: {Reason}";
}
