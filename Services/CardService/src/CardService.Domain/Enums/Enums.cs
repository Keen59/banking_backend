namespace CardService.Domain.Enums;

public enum ECardStatus
{
    Active = 1,
    Frozen = 2
}

public enum EAuthorizationStatus
{
    Initiated = 1,
    Completed = 2,
    Rejected = 3,
    Held = 4
}
