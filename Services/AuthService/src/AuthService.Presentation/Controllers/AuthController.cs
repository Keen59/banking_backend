using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Application.Commands.ChangePassword;
using AuthService.Application.Commands.DisableTwoFactor;
using AuthService.Application.Commands.EnableTwoFactor;
using AuthService.Application.Commands.ForgotPassword;
using AuthService.Application.Commands.GetCurrentUser;
using AuthService.Application.Commands.GrantRole;
using AuthService.Application.Commands.Login;
using AuthService.Application.Commands.Logout;
using AuthService.Application.Commands.RefreshToken;
using AuthService.Application.Commands.Register;
using AuthService.Application.Commands.ResetPassword;
using AuthService.Application.Commands.SendEmailOtp;
using AuthService.Application.Commands.VerifyEmailOtp;
using AuthService.Application.Commands.VerifyDisableTwoFactor;
using AuthService.Application.Commands.VerifyEnableTwoFactor;
using AuthService.Application.Commands.VerifyLoginOtp;
using AuthService.Application.DTOs.Authentication;
using AuthService.Domain.Enums;
using AuthService.Presentation.Requests.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new RegisterCommand
        {
            Email = request.Email,
            Username = request.Username,
            Password = request.Password,
            PhoneNumber = request.PhoneNumber
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<GetCurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var response = await mediator.Send(new GetCurrentUserCommand
        {
            UserId = userId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var response = await mediator.Send(new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("roles/grant")]
    [Authorize]
    public async Task<ActionResult<GrantRoleResponse>> GrantRole(
        [FromBody] GrantRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actorUserId))
            return Unauthorized();

        var response = await mediator.Send(new GrantRoleCommand
        {
            ActorUserId = actorUserId,
            TargetUserId = request.UserId,
            RoleName = request.RoleName
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new LoginCommand
        {
            Email = request.Email,
            Password = request.Password,
            DeviceId = request.DeviceId,
            DeviceName = request.DeviceName,
            Browser = request.Browser,
            OperatingSystem = request.OperatingSystem,
            IpAddress = GetClientIpAddress()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("login/verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> VerifyLoginOtp(
        [FromBody] VerifyLoginOtpRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new VerifyLoginOtpCommand
        {
            Email = request.Email,
            Code = request.Code,
            DeviceId = request.DeviceId,
            DeviceName = request.DeviceName,
            Browser = request.Browser,
            OperatingSystem = request.OperatingSystem,
            IpAddress = GetClientIpAddress()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<ActionResult<EnableTwoFactorResponse>> EnableTwoFactor(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var response = await mediator.Send(new EnableTwoFactorCommand
        {
            UserId = userId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("2fa/enable/verify")]
    [Authorize]
    public async Task<ActionResult<VerifyEnableTwoFactorResponse>> VerifyEnableTwoFactor(
        [FromBody] VerifyTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var response = await mediator.Send(new VerifyEnableTwoFactorCommand
        {
            UserId = userId,
            Code = request.Code
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<ActionResult<DisableTwoFactorResponse>> DisableTwoFactor(
        [FromBody] DisableTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var response = await mediator.Send(new DisableTwoFactorCommand
        {
            UserId = userId,
            Password = request.Password
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("2fa/disable/verify")]
    [Authorize]
    public async Task<ActionResult<VerifyDisableTwoFactorResponse>> VerifyDisableTwoFactor(
        [FromBody] VerifyTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var response = await mediator.Send(new VerifyDisableTwoFactorCommand
        {
            UserId = userId,
            Code = request.Code
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new RefreshTokenCommand
        {
            RefreshToken = request.RefreshToken,
            IpAddress = GetClientIpAddress()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<LogoutResponse>> Logout(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new LogoutCommand
        {
            UserId = GetUserId(),
            SessionId = GetSessionId()
        }, cancellationToken);

        if (!response.IsSuccess)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("email-otp/send")]
    [AllowAnonymous]
    public async Task<ActionResult<SendEmailOtpResponse>> SendEmailOtp(
        [FromBody] SendEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new SendEmailOtpCommand
        {
            Email = request.Email,
            Purpose = request.Purpose
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("email-otp/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<VerifyEmailOtpResponse>> VerifyEmailOtp(
        [FromBody] VerifyEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new VerifyEmailOtpCommand
        {
            Email = request.Email,
            Code = request.Code,
            Purpose = EOtpPurpose.EmailVerification
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new ForgotPasswordCommand
        {
            Email = request.Email
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new ResetPasswordCommand
        {
            Email = request.Email,
            Token = request.Token,
            NewPassword = request.NewPassword
        }, cancellationToken);

        return Ok(response);
    }

    private string GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip;
            }
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(GetUserId(), out userId) && userId != Guid.Empty;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? string.Empty;
    }

    private string GetSessionId()
    {
        return User.FindFirstValue("session_id") ?? string.Empty;
    }
}
