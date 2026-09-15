using LedgerService.Application.DTOs.Ledger;
using LedgerService.Application.Interfaces.Repositories;
using MediatR;

namespace LedgerService.Application.Commands.GetAccountMovements;

public class GetAccountMovementsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAccountMovementsCommand, GetAccountMovementsResponse>
{
    public async Task<GetAccountMovementsResponse> Handle(
        GetAccountMovementsCommand request,
        CancellationToken cancellationToken)
    {
        var account = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
            request.AccountId,
            cancellationToken) ?? throw new InvalidOperationException("Ledger hesabı bulunamadı.");

        if (!request.CanReadAny && account.CustomerId != request.RequestedCustomerId)
            throw new UnauthorizedAccessException("Bu hesaba erişim yetkiniz yok.");

        var lines = await unitOfWork.JournalEntries.GetLinesByLedgerAccountIdAsync(account.Id, cancellationToken);
        var movements = lines
            .OrderBy(line => line.JournalEntry.BookedAt)
            .ThenBy(line => line.CreatedAt)
            .Select(line => new MovementDto
            {
                JournalEntryId = line.JournalEntryId,
                BookedAt = line.JournalEntry.BookedAt,
                Description = line.JournalEntry.Description,
                Side = line.Side,
                Amount = line.Amount
            })
            .ToList();

        return new GetAccountMovementsResponse
        {
            Message = "Hareket listesi.",
            Movements = movements
        };
    }
}
