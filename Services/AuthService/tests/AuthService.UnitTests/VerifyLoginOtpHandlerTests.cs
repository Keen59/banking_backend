using AuthService.Application.Commands.VerifyLoginOtp;
using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AuthService.UnitTests;

public class VerifyLoginOtpHandlerTests
{
    [Fact]
    public async Task Handle_valid_login_otp_issues_session()
    {
        var user = AuthFakes.ActiveUser(twoFactor: true);
        var users = Substitute.For<IUserRepository>();
        users.GetByEmailAsync(user.Email, cancellationToken: Arg.Any<CancellationToken>()).Returns(user);

        var unitOfWork = AuthFakes.UnitOfWork(users);
        var otp = Substitute.For<IEmailOtpService>();
        otp.VerifyAndConsumeAsync(user.Email, "123456", EOtpPurpose.Login, Arg.Any<CancellationToken>())
            .Returns(user);

        var expected = new LoginResponse { AccessToken = "access", RefreshToken = "refresh" };
        var sessions = Substitute.For<ILoginSessionService>();
        sessions.IssueSessionAsync(user, Arg.Any<LoginContext>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var handler = new VerifyLoginOtpHandler(otp, sessions, unitOfWork);
        var response = await handler.Handle(new VerifyLoginOtpCommand
        {
            Email = user.Email,
            Code = "123456",
            DeviceId = "device-1",
            IpAddress = "127.0.0.1"
        }, CancellationToken.None);

        Assert.Same(expected, response);
        await sessions.Received(1).IssueSessionAsync(user, Arg.Any<LoginContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_invalid_otp_records_failed_attempt_and_throws()
    {
        var user = AuthFakes.ActiveUser(twoFactor: true);
        var users = Substitute.For<IUserRepository>();
        users.GetByEmailAsync(user.Email, cancellationToken: Arg.Any<CancellationToken>()).Returns(user);

        var loginAttempts = Substitute.For<ILoginAttemptRepository>();
        var unitOfWork = AuthFakes.UnitOfWork(users, loginAttempts: loginAttempts);

        var otp = Substitute.For<IEmailOtpService>();
        otp.VerifyAndConsumeAsync(user.Email, "000000", EOtpPurpose.Login, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException("Geçersiz veya süresi dolmuş kod."));

        var handler = new VerifyLoginOtpHandler(otp, Substitute.For<ILoginSessionService>(), unitOfWork);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new VerifyLoginOtpCommand
            {
                Email = user.Email,
                Code = "000000",
                DeviceId = "device-1",
                IpAddress = "127.0.0.1"
            }, CancellationToken.None));

        await loginAttempts.Received(1).AddAsync(
            Arg.Is<LoginAttempt>(attempt =>
                attempt.UserId == user.Id &&
                attempt.IsSuccessful == false &&
                attempt.FailureReason == ELoginFailureReason.InvalidOtp),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_user_without_two_factor_throws()
    {
        var user = AuthFakes.ActiveUser(twoFactor: false);
        var users = Substitute.For<IUserRepository>();
        users.GetByEmailAsync(user.Email, cancellationToken: Arg.Any<CancellationToken>()).Returns(user);

        var otp = Substitute.For<IEmailOtpService>();
        var handler = new VerifyLoginOtpHandler(
            otp,
            Substitute.For<ILoginSessionService>(),
            AuthFakes.UnitOfWork(users));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new VerifyLoginOtpCommand
            {
                Email = user.Email,
                Code = "123456",
                DeviceId = "device-1",
                IpAddress = "127.0.0.1"
            }, CancellationToken.None));

        await otp.DidNotReceive().VerifyAndConsumeAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EOtpPurpose>(),
            Arg.Any<CancellationToken>());
    }
}
