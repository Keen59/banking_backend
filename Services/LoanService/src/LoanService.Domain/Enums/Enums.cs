namespace LoanService.Domain.Enums;

public enum EAccountProjectionStatus
{
    Active = 1
}

public enum ELoanStatus
{
    Applied = 1,
    Disbursed = 2,
    Rejected = 3,
    Completed = 4
}

public enum EInstallmentStatus
{
    Pending = 1,
    Paid = 2
}
