namespace CustomerService.Domain.Enums;

public enum ECustomerType
{
    Individual = 1,
    Corporate = 2
}

public enum ECustomerStatus
{
    Prospect = 1,
    Active = 2,
    Restricted = 3,
    Closed = 4
}

public enum EKycStatus
{
    NotStarted = 1,
    Pending = 2,
    InReview = 3,
    Approved = 4,
    Rejected = 5,
    Expired = 6
}

public enum EKycLevel
{
    None = 0,
    Basic = 1,
    Full = 2
}

public enum EAddressType
{
    Home = 1,
    Work = 2,
    Registered = 3
}

public enum EDocumentType
{
    IdentityCard = 1,
    Passport = 2,
    Residence = 3,
    Selfie = 4
}

public enum EDocumentStatus
{
    Uploaded = 1,
    Verified = 2,
    Rejected = 3
}

public enum EConsentType
{
    Kvkk = 1,
    Marketing = 2,
    ElectronicMessage = 3
}

public enum EAuditAction
{
    CustomerCreated = 1,
    CustomerUpdated = 2,
    KycSubmitted = 3,
    KycApproved = 4,
    KycRejected = 5
}
