namespace LedgerService.Domain.Enums;

public enum ELedgerAccountKind
{
    CustomerDemandDeposit = 1,
    InternalClearing = 2
}

public enum ELedgerAccountStatus
{
    Active = 1,
    Frozen = 2,
    Closed = 3
}

public enum EEntrySide
{
    Debit = 1,
    Credit = 2
}

public enum EHoldStatus
{
    Active = 1,
    Released = 2
}
