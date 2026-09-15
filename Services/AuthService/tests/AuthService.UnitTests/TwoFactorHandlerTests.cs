using AuthService.Application.Commands.DisableTwoFactor;
using AuthService.Application.Commands.EnableTwoFactor;
using AuthService.Application.Commands.VerifyDisableTwoFactor;
using AuthService.Application.Commands.VerifyEnableTwoFactor;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using NSubstitute;
using Xunit;

namespace AuthService.UnitTests;

public class TwoFactorHandlerTests
{
    [Fact]
    public async Task Enable_sends_setup_otp_when_two_factor_is_off()
    {
        var user = AuthFakes.ActiveUser(twoFactor: false);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(user.Id).Returns(user);

        var auditLogs = Substitute.For<IAuditLogRepository>();
        var unitOfWork = AuthFakes.UnitOfWork(users, auditLogs: auditLogs);
        var otp = Substitute.For<IEmailOtpService>();
        otp.IssueAsync(user, EOtpPurpose.TwoFactorSetup, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new EnableTwoFactorHandler(unitOfWork, otp);
        var response = await handler.Handle(new EnableTwoFactorCommand { UserId = user.Id }, CancellationToken.None);

        Assert.Contains("gönderildi", response.Message, StringComparison.OrdinalIgnoreCase);
        await otp.Received(1).IssueAsync(user, EOtpPurpose.TwoFactorSetup, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Enable_does_not_send_otp_when_already_on()
    {
        var user = AuthFakes.ActiveUser(twoFactor: true);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(user.Id).Returns(user);

        var otp = Substitute.For<IEmailOtpService>();
        var handler = new EnableTwoFactorHandler(AuthFakes.UnitOfWork(users), otp);

        var response = await handler.Handle(new EnableTwoFactorCommand { UserId = user.Id }, CancellationToken.None);

        Assert.Contains("zaten açık", response.Message, StringComparison.OrdinalIgnoreCase);
        await otp.DidNotReceive().IssueAsync(Arg.Any<User>(), Arg.Any<EOtpPurpose>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Verify_enable_sets_two_factor_true()
    {
        var user = AuthFakes.ActiveUser(twoFactor: false);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(user.Id).Returns(user);

        var otp = Substitute.For<IEmailOtpService>();
        var unitOfWork = AuthFakes.UnitOfWork(users);
        var handler = new VerifyEnableTwoFactorHandler(unitOfWork, otp);

        await handler.Handle(new VerifyEnableTwoFactorCommand { UserId = user.Id, Code = "123456" }, CancellationToken.None);

        Assert.True(user.IsTwoFactorEnabled);
        users.Received(1).Update(user);
        await otp.Received(1).VerifyAndConsumeAsync(
            user.Email,
            "123456",
            EOtpPurpose.TwoFactorSetup,
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Disable_wrong_password_throws()
    {
        var user = AuthFakes.ActiveUser(twoFactor: true);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(user.Id).Returns(user);

        var hasher = Substitute.For<IPasswordHasherService>();
        hasher.Verify("wrong", user.PasswordHash).Returns(false);

        var otp = Substitute.For<IEmailOtpService>();
        var handler = new DisableTwoFactorHandler(AuthFakes.UnitOfWork(users), hasher, otp);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new DisableTwoFactorCommand
            {
                UserId = user.Id,
                Password = "wrong"
            }, CancellationToken.None));

        await otp.DidNotReceive().IssueAsync(Arg.Any<User>(), Arg.Any<EOtpPurpose>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Disable_sends_disable_otp_when_password_matches()
    {
        var user = AuthFakes.ActiveUser(twoFactor: true);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(user.Id).Returns(user);

        var hasher = Substitute.For<IPasswordHasherService>();
        hasher.Verify("secret", user.PasswordHash).Returns(true);

        var otp = Substitute.For<IEmailOtpService>();
        otp.IssueAsync(user, EOtpPurpose.TwoFactorDisable, Arg.Any<CancellationToken>()).Returns(true);

        var unitOfWork = AuthFakes.UnitOfWork(users);
        var handler = new DisableTwoFactorHandler(unitOfWork, hasher, otp);

        var response = await handler.Handle(new DisableTwoFactorCommand
        {
            UserId = user.Id,
            Password = "secret"
        }, CancellationToken.None);

        Assert.Contains("gönderildi", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(user.IsTwoFactorEnabled);
        await otp.Received(1).IssueAsync(user, EOtpPurpose.TwoFactorDisable, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Verify_disable_sets_two_factor_false()
    {
        var user = AuthFakes.ActiveUser(twoFactor: true);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(user.Id).Returns(user);

        var otp = Substitute.For<IEmailOtpService>();
        var unitOfWork = AuthFakes.UnitOfWork(users);
        var handler = new VerifyDisableTwoFactorHandler(unitOfWork, otp);

        await handler.Handle(new VerifyDisableTwoFactorCommand { UserId = user.Id, Code = "654321" }, CancellationToken.None);

        Assert.False(user.IsTwoFactorEnabled);
        users.Received(1).Update(user);
        await otp.Received(1).VerifyAndConsumeAsync(
            user.Email,
            "654321",
            EOtpPurpose.TwoFactorDisable,
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }
}
