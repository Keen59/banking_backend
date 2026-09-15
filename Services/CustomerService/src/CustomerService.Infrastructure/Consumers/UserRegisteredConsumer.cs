using Banking.Contracts.Events;
using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Interfaces.Services;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Enums;
using MassTransit;

namespace CustomerService.Infrastructure.Consumers;

public sealed class UserRegisteredConsumer(
    IUnitOfWork unitOfWork,
    ICifNumberService cifNumberService) : IConsumer<UserRegistered>
{
    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var message = context.Message;
        var existing = await unitOfWork.CustomerRepository.GetByIdAsync(message.CustomerId);
        if (existing is not null)
            return;

        var customer = new Customer
        {
            Id = message.CustomerId,
            CifNumber = await cifNumberService.GenerateAsync(context.CancellationToken),
            Type = ECustomerType.Individual,
            Status = ECustomerStatus.Prospect,
            KycStatus = EKycStatus.NotStarted,
            KycLevel = EKycLevel.None,
            Email = message.Email,
            PhoneNumber = message.PhoneNumber,
            FirstName = message.Username,
            LastName = string.Empty,
            Nationality = "TR"
        };

        await unitOfWork.CustomerRepository.AddAsync(customer, context.CancellationToken);
        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            CustomerId = customer.Id,
            Action = EAuditAction.CustomerCreated,
            Resource = "Customer"
        }, context.CancellationToken);

        await unitOfWork.SaveAsync(context.CancellationToken);
    }
}
