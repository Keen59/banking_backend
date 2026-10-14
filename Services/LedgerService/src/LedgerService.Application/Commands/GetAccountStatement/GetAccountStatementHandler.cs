using LedgerService.Application.DTOs.Ledger;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using MediatR;

namespace LedgerService.Application.Commands.GetAccountStatement;

public class GetAccountStatementHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAccountStatementCommand, GetAccountStatementResponse>
{
    public async Task<GetAccountStatementResponse> Handle(
        GetAccountStatementCommand request,
        CancellationToken cancellationToken)
    {
        if (request.To < request.From)
            throw new InvalidOperationException("Ekstre bitiş tarihi başlangıçtan önce olamaz.");

        var account = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
            request.AccountId,
            cancellationToken) ?? throw new InvalidOperationException("Ledger hesabı bulunamadı.");

        if (!request.CanReadAny && account.CustomerId != request.RequestedCustomerId)
            throw new UnauthorizedAccessException("Bu hesaba erişim yetkiniz yok.");

        var lines = await unitOfWork.JournalEntries.GetLinesByLedgerAccountIdAsync(account.Id, cancellationToken);
        var openingLines = lines.Where(line => line.JournalEntry.BookedAt < request.From).ToList();
        var periodLines = lines
            .Where(line => line.JournalEntry.BookedAt >= request.From && line.JournalEntry.BookedAt <= request.To)
            .ToList();
        var closingLines = lines.Where(line => line.JournalEntry.BookedAt <= request.To).ToList();

        var (opening, _, _) = LedgerBalanceCalculator.Calculate(account.Kind, openingLines, []);
        var (closing, _, _) = LedgerBalanceCalculator.Calculate(account.Kind, closingLines, []);

        var movements = periodLines
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

        return new GetAccountStatementResponse
        {
            Message = "Hesap ekstresi.",
            Statement = new StatementDto
            {
                AccountId = account.SourceAccountId ?? Guid.Empty,
                CustomerId = account.CustomerId ?? Guid.Empty,
                Currency = account.Currency,
                From = request.From,
                To = request.To,
                OpeningLedger = opening,
                ClosingLedger = closing,
                Movements = movements
            }
        };
    }
}
