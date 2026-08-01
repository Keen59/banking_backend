using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Domain.Enums
{
    public enum EUserStatus
    {
        Active = 1,
        Suspended = 2,
        Locked = 3,
        Deleted = 4
    }
    public enum ELoginFailureReason
    {
        InvalidPassword = 1,
        UserNotFound = 2,
        Locked = 3,
        Disabled = 4,
        InvalidOtp = 5
    }
    public enum EAuditAction
    {
        Login = 1,
        Logout = 2,
        PasswordChanged = 3,
        TokenRefreshed = 4,
        RoleAssigned = 5,
        PermissionGranted = 6
    }
}
