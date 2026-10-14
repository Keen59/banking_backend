namespace RiskService.Domain.Enums;

public enum ERiskChannel
{
    Transfer = 1,
    Fast = 2,
    Eft = 3,
    IncomingFast = 4,
    IncomingEft = 5,
    Card = 6,
    Fx = 7,
    Loan = 8
}

public enum ERiskDirection
{
    Outbound = 1,
    Inbound = 2
}

public enum EAlertStatus
{
    Open = 1,
    Closed = 2
}
