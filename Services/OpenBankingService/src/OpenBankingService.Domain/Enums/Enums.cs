namespace OpenBankingService.Domain.Enums;

public enum EAccountProjectionStatus
{
    Active = 1
}

public enum EConsentStatus
{
    Active = 1,
    Revoked = 2
}

[Flags]
public enum EConsentScope
{
    Ais = 1,
    Pis = 2
}

public enum EInitiationStatus
{
    Initiated = 1,
    Completed = 2,
    Rejected = 3
}
