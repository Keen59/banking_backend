using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.PostJournal;

public class PostJournalHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<PostJournalCommand, PostJournalResponse>
{
    public async Task<PostJournalResponse> Handle(PostJournalCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new InvalidOperationException("IdempotencyKey zorunludur.");

        if (request.Lines.Count < 2)
            throw new InvalidOperationException("Journal en az iki satır içermelidir.");

        if (request.Lines.Any(line => line.Amount <= 0))
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        var debit = request.Lines.Where(line => line.Side == EEntrySide.Debit).Sum(line => line.Amount);
        var credit = request.Lines.Where(line => line.Side == EEntrySide.Credit).Sum(line => line.Amount);
        if (debit != credit)
            throw new InvalidOperationException("Debit toplamı credit toplamına eşit olmalıdır.");

        var existing = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            return new PostJournalResponse
            {
                Message = "Journal zaten kayıtlı.",
                JournalEntryId = existing.Id
            };
        }

        var accountIds = request.Lines.Select(line => line.LedgerAccountId).Distinct().ToList();
        var accounts = await unitOfWork.LedgerAccounts.GetByIdsAsync(accountIds, cancellationToken);
        if (accounts.Count != accountIds.Count)
            throw new InvalidOperationException("Ledger hesabı bulunamadı.");

        if (accounts.Select(account => account.Currency).Distinct().Count() != 1)
            throw new InvalidOperationException("Journal satırları aynı para biriminde olmalıdır.");

        if (accounts.Any(account => account.Status != ELedgerAccountStatus.Active))
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = request.IdempotencyKey.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Journal" : request.Description.Trim(),
            BookedAt = DateTimeOffset.UtcNow,
            Lines = request.Lines.Select(line => new JournalLine
            {
                Id = Guid.NewGuid(),
                LedgerAccountId = line.LedgerAccountId,
                Side = line.Side,
                Amount = decimal.Round(line.Amount, 4, MidpointRounding.AwayFromZero)
            }).ToList()
        };

        await unitOfWork.JournalEntries.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new PostJournalResponse
        {
            Message = "Journal kaydedildi.",
            JournalEntryId = entry.Id
        };
    }
}
