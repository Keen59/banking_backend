namespace PaymentService.Domain.Enums;

public enum EAccountProjectionStatus
{
    Active = 1,
    Frozen = 2,
    Closed = 3
}

public enum ETransferStatus
{
    Initiated = 1,
    Completed = 2,
    Rejected = 3,
    Held = 4
}
