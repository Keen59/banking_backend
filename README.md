# banking_backend

This repository is a learning project: a Turkish retail digital-banking backend, written with Cursor.

The architecture, folder layout, and business rules are mine. Cursor writes the code.

This is not a licensed bank product.

## Authorship

| | |
|---|---|
| Architecture, service boundaries, layers, what to build next | Author |
| Code (handlers, consumers, migrations, endpoints) | Cursor |

## Architecture

Each bounded context is its own service with its own PostgreSQL database. Services do not write to each other’s databases.

Each service uses Clean Architecture:

```
Services/<Service>/src/
  <Service>.Domain
  <Service>.Application
  <Service>.Infrastructure
  <Service>.Presentation
```

- **Domain:** entities, enums
- **Application:** MediatR commands/handlers, DTOs, repository interfaces
- **Infrastructure:** EF Core, PostgreSQL, JWT, MassTransit
- **Presentation:** ASP.NET Core API

Shared contracts live in `BuildingBlocks/src/Banking.Contracts` (events and permission names).

Inter-service communication is **MassTransit + RabbitMQ**. There is no synchronous HTTP between services. Auth completes registration locally; other services learn from the queue.

## Current status

Order: Auth → Customer (CIF/KYC) → Account → Ledger → Payment (internal transfer, test credit, outbound FAST/EFT, incoming FAST/EFT) → Notification → Card (virtual debit, token/last4, hold/capture/reverse) → Risk (post-transaction monitoring) → Loan (zero-interest installments) → Gateway (YARP) → FX (USD/EUR demand-deposit + stub-rate conversion) → Open banking (consent + AIS list + PIS virman). An account is not opened before KYC approval. Ledger holds balances. Payment, Card, Loan, and OpenBanking do not call Ledger over HTTP. Opening balance is 0 until ops posts a test credit, incoming FAST/EFT, or a loan disbursement. Licensed-bank extras (PCI processor, 3DS, a real MASAK engine) are out of this learning slice.

```
Register (Auth)
  → UserRegistered (outbox → RabbitMQ)
  → CustomerService CIF stub (Prospect, no TCKN)

Onboarding + documents + kyc:review
  → KycApproved (outbox → RabbitMQ)
  → AccountService demand-deposit TRY account + TR IBAN
  → AccountOpened (outbox → RabbitMQ)
  → LedgerService projection, opening balance 0
  → PaymentService account projection
  → CardService virtual debit (token + last4, no PAN)
  → NotificationService KYC/account rows

POST /api/payments/transfers (Idempotency-Key)
  → TransferRequested (outbox → RabbitMQ)
  → LedgerService hold + double-entry (debit source, credit dest)
  → TransferCompleted or TransferRejected (outbox → RabbitMQ)
  → PaymentService status update
  → NotificationRequested → NotificationService customer inbox

POST /api/payments/test-credits (ops, payments:credit, Idempotency-Key)
  → TestCreditRequested
  → LedgerService debit InternalClearing TRY, credit customer demand-deposit
  → TestCreditPosted or TestCreditRejected

POST /api/payments/fast (Idempotency-Key, TR IBAN)
  → FastPaymentRequested
  → LedgerService hold + debit source, credit InternalClearing TRY
  → FastPaymentCompleted or FastPaymentRejected

POST /api/payments/incoming-fast (ops, payments:credit, Idempotency-Key, TR IBAN)
  → IncomingFastPaymentRequested
  → LedgerService debit InternalClearing TRY, credit customer (no hold)
  → IncomingFastPaymentCompleted or IncomingFastPaymentRejected

POST /api/payments/incoming-eft (ops, payments:credit, Idempotency-Key, TR IBAN)
  → IncomingEftPaymentRequested
  → LedgerService accept (clearing hold only, no customer posting)
  → IncomingEftPaymentHeld or IncomingEftPaymentRejected
  → ops POST .../incoming-eft/{id}/settle (payments:settle)
      → IncomingEftSettlementRequested → debit clearing / credit customer → IncomingEftPaymentCompleted
  → ops POST .../incoming-eft/{id}/reject (payments:settle)
      → IncomingEftReturnRequested → release clearing hold → IncomingEftPaymentRejected

POST /api/payments/eft (Idempotency-Key, TR IBAN)
  → EftPaymentRequested
  → LedgerService hold only (no posting)
  → EftPaymentHeld or EftPaymentRejected
  → ops POST .../eft/{id}/settle (payments:settle)
      → EftSettlementRequested → debit source / credit clearing → EftPaymentCompleted
  → ops POST .../eft/{id}/reject (payments:settle)
      → EftReturnRequested → release hold → EftPaymentRejected

POST /api/cards/{id}/authorize (Idempotency-Key)
  → CardAuthorizationRequested
  → LedgerService hold only (card-auth-hold:{id})
  → CardAuthorizationHeld
  → POST .../authorizations/{authId}/capture → CardCaptureRequested → debit customer / credit clearing (card-auth:{id})
  → POST .../authorizations/{authId}/reverse → CardAuthorizationReverseRequested → release hold

Transfer/FAST/EFT/incoming/card requested events
  → RiskService screening (does not block posting)
  → alert if amount ≥ 25_000 (LargeAmount) or UTC-day outbound sum ≥ 80_000 (DailyVelocity)
  → ops GET /api/risk/alerts (risk:review), POST .../{id}/close

POST /api/loans (Idempotency-Key)
  → Applied (no posting)
  → ops POST .../loans/{id}/approve (loans:review)
      → LoanDisbursementRequested → debit clearing / credit customer (loan:{id}) → equal installments
  → POST .../loans/{id}/repay
      → LoanRepaymentRequested → debit customer / credit clearing (loan-repay:{installmentId})

POST /api/accounts { currency: USD|EUR }
  → AccountOpened (after KYC TRY exists)
POST /api/payments/fx-conversions (Idempotency-Key)
  → FxConversionRequested
  → Ledger fx-sell:{id} (debit source / credit source clearing) + fx-buy:{id} (debit dest clearing / credit dest)
  → FxConversionCompleted or FxConversionRejected

POST /api/open-banking/consents (JWT, Idempotency-Key)
  → Active consent, AccessToken 32 hex (TPP sandbox-tpp)
GET /api/open-banking/ais/accounts (X-Consent-Token)
  → IBAN/currency only (no live ledger)
POST /api/open-banking/pis/payments (X-Consent-Token, Idempotency-Key)
  → OpenBankingPaymentRequested (InitiationId = transfer id)
  → Payment virman → TransferCompleted / TransferRejected
```

| Service | HTTP | Database |
|---|---|---|
| AuthService | `http://localhost:5229` | `BankingAuthServiceDb` |
| CustomerService | `http://localhost:5029` | `BankingCustomerServiceDb` |
| AccountService | `http://localhost:5039` | `BankingAccountServiceDb` |
| LedgerService | `http://localhost:5049` | `BankingLedgerServiceDb` |
| PaymentService | `http://localhost:5059` | `BankingPaymentServiceDb` |
| NotificationService | `http://localhost:5069` | `BankingNotificationServiceDb` |
| CardService | `http://localhost:5079` | `BankingCardServiceDb` |
| RiskService | `http://localhost:5089` | `BankingRiskServiceDb` |
| LoanService | `http://localhost:5099` | `BankingLoanServiceDb` |
| OpenBankingService | `http://localhost:5109` | `BankingOpenBankingServiceDb` |
| Gateway (YARP) | `http://localhost:5000` | — |

JWT uses the same `JwtSettings` on every service. Claim: `customer_id`. Operations permissions: `kyc:review`, `customers:read`, `roles:assign`, `payments:credit`, `payments:settle`, `risk:review`, `loans:review`. Clients may call each service port or the gateway at `:5000` (`/api/auth`, `/api/customers`, `/api/accounts`, `/api/ledger`, `/api/payments`, `/api/notifications`, `/api/cards`, `/api/risk`, `/api/loans`, `/api/open-banking`). The gateway does not validate JWT.

### AuthService

Refresh token, email OTP, 2FA, token rotation. Register publishes `UserRegistered` via the EF outbox (no password, token, or OTP on the event).

Examples: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/login/verify-otp`, `POST /api/auth/refresh`, `POST /api/auth/email-otp/send`, `POST /api/auth/2fa/enable`.

### CustomerService

The `UserRegistered` consumer opens a CIF stub. The same `CustomerId` is not inserted twice. TCKN/address/KVKK are filled with `POST /api/customers`. KYC documents are uploaded and submitted; review requires `kyc:review`. Approval publishes `KycApproved`.

### AccountService

The first TRY demand-deposit is not created with HTTP POST. The `KycApproved` consumer inserts an `Active` demand-deposit TRY account and a TR IBAN (ISO 7064 mod-97, 26 characters) when `(CustomerId, DemandDeposit, TRY)` does not exist, then publishes `AccountOpened` via the EF outbox. After that TRY exists, `POST /api/accounts` with `USD` or `EUR` opens a second demand-deposit (same unique key `(CustomerId, ProductType, Currency)`); a repeat returns the existing row. Reads: `GET /api/accounts/me` (`customer_id`), `GET /api/accounts/{id}` (owner or `customers:read`).

### LedgerService

Balances are not stored on `Account`. The `AccountOpened` consumer opens a customer demand-deposit projection (liability) with ledger/hold/available = 0. Double-entry journals must have debit = credit and an idempotency key. Hold reduces available, not ledger. `TransferRequested` posts an internal transfer in one database transaction: hold on the source, debit source / credit destination, then release the hold. Journal key `transfer:{transferId}`, hold key `transfer-hold:{transferId}`. `TestCreditRequested` posts debit `InternalClearingTry` / credit customer (`test-credit:{creditId}`). `FastPaymentRequested` posts hold then debit source / credit clearing (`fast:{id}`, `fast-hold:{id}`). `IncomingFastPaymentRequested` posts debit clearing / credit customer with no hold (`incoming-fast:{id}`). `EftPaymentRequested` writes an active hold only (`eft-hold:{id}`) and publishes `EftPaymentHeld`; posting waits for `EftSettlementRequested` (debit source / credit clearing, `eft:{id}`, hold released) or `EftReturnRequested` (hold released, no journal). `IncomingEftPaymentRequested` writes a clearing hold only (`incoming-eft-hold:{id}`, customer available unchanged) and publishes `IncomingEftPaymentHeld`; posting waits for `IncomingEftSettlementRequested` (debit clearing / credit customer, `incoming-eft:{id}`) or `IncomingEftReturnRequested` (hold released, no journal). `CardAuthorizationRequested` writes an active customer hold only (`card-auth-hold:{id}`) and publishes `CardAuthorizationHeld`; posting waits for `CardCaptureRequested` (debit customer / credit clearing, `card-auth:{id}`, hold released) or `CardAuthorizationReverseRequested` (hold released, no journal). `FxConversionRequested` posts two same-currency journals in one SaveAsync: `fx-sell:{id}` debit source / credit source clearing, `fx-buy:{id}` debit dest clearing / credit dest (`InternalClearingUsd` / `InternalClearingEur` seeded). If the journals already exist, Ledger republishes the completed event. Business rejections publish the matching rejected event. No generic HTTP posting. Reads: `GET /api/ledger/me`, `GET /api/ledger/accounts/{accountId}`, `GET /api/ledger/accounts/{accountId}/movements`, `GET /api/ledger/accounts/{accountId}/statement?from=&to=` (`customer_id` or `customers:read`). Statement returns opening/closing ledger and period movements (holds are not on the statement). `{accountId}` is the AccountService id.

### PaymentService

Payment stores a local `AccountOpened` projection and payment records. It does not hold balances and does not call Ledger over HTTP. `POST /api/payments/transfers` requires JWT `customer_id` and an `Idempotency-Key` header. `POST /api/payments/fast` and `POST /api/payments/eft` are the same for an outbound TR IBAN (ISO 7064 mod-97); an IBAN that already exists as an internal projection is rejected (use virman). FAST posts immediately. EFT stays `Held` until ops settle/return (fake clearing). `POST /api/payments/incoming-fast` and `POST /api/payments/incoming-eft` require `payments:credit`; destination IBAN must exist as an internal projection; an internal source IBAN is rejected (use virman). Incoming FAST posts immediately (clearing debit, customer credit, no hold). Incoming EFT stays `Held` until ops settle/return. Incoming amounts do not count toward the outbound daily limit. Limits: max 50_000 per transfer/FAST/EFT/incoming FAST/incoming EFT, 100_000 per UTC day shared across outbound virman+FAST+EFT+FX TRY-equivalent (Initiated+Held+Completed; Rejected excluded). `POST /api/payments/fx-conversions` converts between the caller’s own TRY/USD/EUR accounts at stub rates (`UsdTry` 34, `EurTry` 37; not a TCMB feed). Same currency is rejected (use virman). Per-item cap is the TRY equivalent. `POST /api/payments/test-credits` requires `payments:credit`; max 100_000 per credit; it funds a customer ledger (clearing debit, customer credit) so available is not stuck at 0. `POST /api/payments/eft/{id}/settle`, `POST /api/payments/eft/{id}/reject`, `POST /api/payments/incoming-eft/{id}/settle` and `POST /api/payments/incoming-eft/{id}/reject` require `payments:settle`. When a payment actually changes to Held/Completed/Rejected, Payment publishes `NotificationRequested` in the same EF outbox transaction. Reads: `GET /api/payments/transfers/me`, `GET /api/payments/transfers/{id}`, `GET /api/payments/fast/me`, `GET /api/payments/fast/{id}`, `GET /api/payments/incoming-fast/me`, `GET /api/payments/incoming-fast/{id}` (owner, `customers:read`, or `payments:credit`), `GET /api/payments/incoming-eft/me`, `GET /api/payments/incoming-eft/{id}` (owner, `customers:read`, `payments:credit`, or `payments:settle`), `GET /api/payments/eft/me`, `GET /api/payments/eft/{id}` (owner, `customers:read`, or `payments:settle` for EFT), `GET /api/payments/test-credits/{id}` (`payments:credit`), `GET /api/payments/fx-conversions/me`, `GET /api/payments/fx-conversions/{id}` (owner or `customers:read`). `OpenBankingPaymentRequested` creates a virman whose transfer id equals the initiation id (`open-banking:{id}`).

### NotificationService

Notification stores customer inbox rows. It does not send SMS/email. It consumes `NotificationRequested` (from Payment/Card), `KycApproved`, and `AccountOpened`. Duplicate `(Type, SourceId)` is ignored. Reads: `GET /api/notifications/me` (`customer_id`). Mark read: `POST /api/notifications/{id}/read` (owner only).

### CardService

Card stores a virtual debit issued on `AccountOpened` (one card per account). PAN is not stored: `Token` is a Guid hex string, `Last4` is four digits, `Brand` is `Virtual`. `POST /api/cards/{id}/authorize` requires JWT owner and `Idempotency-Key` (max 50_000 TRY). Ledger hold follows the EFT pattern. Capture/reverse are owner HTTP calls that publish `CardCaptureRequested` / `CardAuthorizationReverseRequested`. Freeze: `POST /api/cards/{id}/freeze`. Reads: `GET /api/cards/me`, `GET /api/cards/{id}` (owner or `customers:read`), `GET /api/cards/{id}/authorizations`. Status updates also publish `NotificationRequested`. This is not a PCI processor and has no 3DS.

### RiskService

Risk is post-transaction monitoring, not a MASAK engine and not a payment gate. It does not call Payment or Ledger over HTTP and does not reject posting. `AccountOpened` builds an account projection so requested events (which have no `CustomerId`) can be attributed. Each Transfer/FAST/EFT/incoming FAST/incoming EFT/card authorization/FX/loan disbursement request is screened once (`Channel` + `SourceId`). Missing projection: skip. Amount ≥ 25_000 opens `LargeAmount`. UTC-day outbound sum (virman+FAST+EFT+card+FX TRY-equivalent+loan) ≥ 80_000 opens `DailyVelocity` (incoming does not count). Three or more outbound posts in `[20_000, 25_000)` on the UTC day open `Structuring`. `KycApproved` now carries name and TCKN; a stub watchlist (`99999999990`, name `YASAKLI TEST`) opens `WatchlistHit`. This is still not a MASAK engine. Ops: `GET /api/risk/alerts`, `GET /api/risk/alerts/{id}`, `POST /api/risk/alerts/{id}/close` require `risk:review`.

### LoanService

Simplified consumer loan: zero interest, equal principal installments. Not a licensed credit product. `POST /api/loans` requires JWT owner and `Idempotency-Key` (max 100_000 TRY, term 3–12 months). Status stays `Applied` until ops `POST /api/loans/{id}/approve` (`loans:review`) publishes `LoanDisbursementRequested`. Ledger posts debit clearing / credit customer (`loan:{id}`). `LoanDisbursementPosted` builds N installments (last row gets the remainder). `POST /api/loans/{id}/repay` pays the next pending installment (`loan-repay:{installmentId}`: debit customer / credit clearing). When none remain, status is `Completed`. Ops reject from `Applied`: `POST /api/loans/{id}/reject`. Reads: `GET /api/loans/me`, `GET /api/loans/{id}` (owner or `customers:read`).

### OpenBankingService

Not licensed PSD2 and not OAuth2. Customer `POST /api/open-banking/consents` (`Idempotency-Key`) grants TPP `sandbox-tpp` AIS (`1`) and/or PIS (`2`) on owned accounts (1–90 days). Token is Guid N (32 hex). TPP calls send `X-Consent-Token` (no JWT): `GET /api/open-banking/ais/accounts` returns IBAN/currency only (no live ledger). `POST /api/open-banking/pis/payments` publishes `OpenBankingPaymentRequested`; Payment opens a virman with `Transfer.Id = InitiationId`. `TransferCompleted` / `TransferRejected` / `OpenBankingPaymentRejected` update initiation status. Revoke: `POST /api/open-banking/consents/{id}/revoke`. Reads: `GET /api/open-banking/consents/me`, `GET /api/open-banking/pis/payments/{id}`.

### Gateway

YARP reverse proxy on `http://localhost:5000`. Path prefixes are forwarded to the matching service port (`/api/open-banking` → 5109). JWT is still validated by each service. `Authorization`, `Idempotency-Key`, and `X-Consent-Token` are forwarded. This is not production ingress.

## Run

PostgreSQL (`localhost:5432`) and RabbitMQ (`localhost:5672`) must be up. Apply each service’s migration, then run the Presentation project.

```powershell
dotnet ef database update --project Services/AuthService/src/AuthService.Infrastructure --startup-project Services/AuthService/src/AuthService.Presentation
dotnet ef database update --project Services/CustomerService/src/CustomerService.Infrastructure --startup-project Services/CustomerService/src/CustomerService.Presentation
dotnet ef database update --project Services/AccountService/src/AccountService.Infrastructure --startup-project Services/AccountService/src/AccountService.Presentation
dotnet ef database update --project Services/LedgerService/src/LedgerService.Infrastructure --startup-project Services/LedgerService/src/LedgerService.Presentation
dotnet ef database update --project Services/PaymentService/src/PaymentService.Infrastructure --startup-project Services/PaymentService/src/PaymentService.Presentation
dotnet ef database update --project Services/NotificationService/src/NotificationService.Infrastructure --startup-project Services/NotificationService/src/NotificationService.Presentation
dotnet ef database update --project Services/CardService/src/CardService.Infrastructure --startup-project Services/CardService/src/CardService.Presentation
dotnet ef database update --project Services/RiskService/src/RiskService.Infrastructure --startup-project Services/RiskService/src/RiskService.Presentation
dotnet ef database update --project Services/LoanService/src/LoanService.Infrastructure --startup-project Services/LoanService/src/LoanService.Presentation
dotnet ef database update --project Services/OpenBankingService/src/OpenBankingService.Infrastructure --startup-project Services/OpenBankingService/src/OpenBankingService.Presentation
```

Connection strings in `appsettings.json` are for local development, not production secrets. Gateway has no database; run `Services/Gateway` on `http://localhost:5000` after the other Presentation projects.

## Tests

xUnit + NSubstitute. No HTTP or RabbitMQ. Run:

```powershell
dotnet test
```

| Project | What is locked |
|---|---|
| `AuthService.UnitTests` | Refresh rotation; reused refresh token outside grace → 401 and family revoke; login OTP success/fail; 2FA enable/disable |
| `CustomerService.UnitTests` | `UserRegistered` does not insert the same `CustomerId` twice; `POST .../kyc/review` requires `kyc:review` |
| `AccountService.UnitTests` | TR IBAN ISO 7064 mod-97; `KycApproved` does not open a second `(CustomerId, DemandDeposit, TRY)` account; `AccountOpened` is published on create; USD opens after TRY; repeat USD does not publish again; TRY is rejected on `POST /api/accounts` |
| `LedgerService.UnitTests` | `AccountOpened` projection is idempotent and posts nothing; debit must equal credit; duplicate idempotency key does not double-post; customer credit raises ledger/available; hold/release changes available only; internal transfer debit source / credit dest, hold released, same transfer id does not double-post; test credit debit clearing / credit customer; FAST debit source / credit clearing; incoming FAST debit clearing / credit customer with no hold; EFT hold stays active until settle (then debit source / credit clearing) or return (hold released, no journal); incoming EFT holds clearing until settle (then debit clearing / credit customer) or return (no journal, customer available unchanged); period statement splits opening/period/closing; card authorize hold then capture (debit customer / credit clearing) or reverse (no journal); loan disbursement debit clearing / credit customer; loan repay debit customer / credit clearing; FX sell TRY / buy USD two journals, same conversion id does not double-post |
| `PaymentService.UnitTests` | Create publishes `TransferRequested` as `Initiated`; same `Idempotency-Key` returns the existing row; per-transfer and daily limits reject; source must belong to the caller; `AccountOpened` projection is idempotent; Completed/Rejected update status and do not overwrite Completed; FAST/EFT require a valid external TR IBAN; incoming FAST/EFT require an internal destination IBAN and do not count toward the outbound daily limit; daily limit is shared virman+FAST+EFT and Held counts; test credit and incoming FAST/EFT create require `payments:credit`; EFT and incoming EFT settle/return require `payments:settle`; FX TRY→USD uses stub rate 34, same `Idempotency-Key` returns the existing row, same currency and foreign destination reject; OpenBanking PIS uses initiation id as transfer id |
| `NotificationService.UnitTests` | `(Type, SourceId)` does not insert twice; `GET me` is scoped to `customer_id`; mark-read rejects another customer |
| `CardService.UnitTests` | `AccountOpened` issues one virtual card per account; authorize publishes `CardAuthorizationRequested`; same `Idempotency-Key` returns the existing row; frozen card and amount over 50_000 reject; capture/reverse require Held; Completed is not overwritten; token is 32 hex chars and last4 is four digits |
| `RiskService.UnitTests` | `AccountOpened` projection is idempotent; 25_000 opens `LargeAmount`, 24_999 does not; same source id is not screened twice; missing account skips; UTC-day outbound 3×30_000 opens `DailyVelocity`; incoming large amount does not open daily; FX TRY-equivalent 25_000 opens `LargeAmount`; 3×24_000 opens `Structuring`; watchlist TCKN opens `WatchlistHit`; loan 25_000 opens `LargeAmount`; close requires `risk:review` |
| `LoanService.UnitTests` | Apply stays `Applied` and does not disburse; same `Idempotency-Key` returns the existing row; amount/term/ownership reject; approve publishes `LoanDisbursementRequested`; reject from `Applied` only; posted installments sum to principal; repay requires `Disbursed`; last payment sets `Completed`; approve/reject require `loans:review` |
| `OpenBankingService.UnitTests` | Consent stays Active with a 32-hex token; same `Idempotency-Key` returns the existing row; unknown TPP and unowned account reject; AIS lists consented accounts; revoked/expired/PIS-only reject AIS; PIS publishes `OpenBankingPaymentRequested`; PIS without PIS scope rejects; `TransferCompleted` does not overwrite Completed; revoke then PIS rejects |

---

# Türkçe

Bu repo, bankacılık uygulamaları geliştirmeyi öğrenmek için Cursor ile yazılmış bir çalışma alanı. Türkiye perakende dijital banka backend’i.

Mimariyi, klasör düzenini ve iş kurallarını ben kurdum. Kodu Cursor yazdı.

Lisanslı bir banka ürünü değil.

## Kim ne yaptı

| | |
|---|---|
| Mimari, servis sınırları, katmanlar, sıradaki iş | Yazar |
| Kod (handler, consumer, migration, endpoint) | Cursor |

## Mimari

Her bounded context ayrı servis, ayrı PostgreSQL veritabanı. Servisler birbirinin DB’sine yazmaz.

Her serviste Clean Architecture katmanları:

```
Services/<Servis>/src/
  <Servis>.Domain
  <Servis>.Application
  <Servis>.Infrastructure
  <Servis>.Presentation
```

- **Domain:** entity, enum
- **Application:** MediatR command/handler, DTO, repository arayüzleri
- **Infrastructure:** EF Core, PostgreSQL, JWT, MassTransit
- **Presentation:** ASP.NET Core API

Ortak sözleşme: `BuildingBlocks/src/Banking.Contracts` (event ve permission isimleri).

Servisler arası iletişim: **MassTransit + RabbitMQ**. Senkron HTTP yok. Kayıt Auth’ta biter; diğer servisler kuyruktan öğrenir.

## Mevcut durum

Sıra: Auth → Customer (CIF/KYC) → Account → Ledger → Payment (iç virman, test kredisi, giden FAST/EFT, gelen FAST/EFT) → Notification → Card (sanal debit, token/last4, hold/capture/reverse) → Risk (işlem sonrası izleme) → Loan (faizsiz taksit) → Gateway (YARP) → FX (USD/EUR vadesiz + sabit kur çevrimi) → açık bankacılık (rıza + AIS liste + PIS virman). Hesap, KYC onayı olmadan açılmaz. Bakiye ledger’dadır. Payment, Card, Loan ve OpenBanking, Ledger’a HTTP atmaz. Opening balance 0’dır; available için ops test kredisi, gelen FAST/EFT veya kredi kullandırım gerekir. Lisanslı banka ekleri (PCI processor, 3DS, gerçek MASAK motoru) bu öğrenme diliminin dışında.

```
Kayıt (Auth)
  → UserRegistered (outbox → RabbitMQ)
  → CustomerService CIF iskeleti (Prospect, TCKN yok)

Onboarding + belge + kyc:review
  → KycApproved (outbox → RabbitMQ)
  → AccountService vadesiz TRY hesap + TR IBAN
  → AccountOpened (outbox → RabbitMQ)
  → LedgerService projeksiyon, opening balance 0
  → PaymentService hesap projeksiyonu
  → CardService sanal debit (token + last4, PAN yok)
  → NotificationService KYC/hesap satırları

POST /api/payments/transfers (Idempotency-Key)
  → TransferRequested (outbox → RabbitMQ)
  → LedgerService hold + çift kayıt (debit kaynak, credit hedef)
  → TransferCompleted veya TransferRejected (outbox → RabbitMQ)
  → PaymentService durum güncellemesi
  → NotificationRequested → NotificationService müşteri kutusu

POST /api/payments/test-credits (ops, payments:credit, Idempotency-Key)
  → TestCreditRequested
  → LedgerService debit InternalClearing TRY, credit müşteri vadesiz
  → TestCreditPosted veya TestCreditRejected

POST /api/payments/fast (Idempotency-Key, TR IBAN)
  → FastPaymentRequested
  → LedgerService hold + debit kaynak, credit InternalClearing TRY
  → FastPaymentCompleted veya FastPaymentRejected

POST /api/payments/incoming-fast (ops, payments:credit, Idempotency-Key, TR IBAN)
  → IncomingFastPaymentRequested
  → LedgerService debit InternalClearing TRY, credit müşteri (hold yok)
  → IncomingFastPaymentCompleted veya IncomingFastPaymentRejected

POST /api/payments/incoming-eft (ops, payments:credit, Idempotency-Key, TR IBAN)
  → IncomingEftPaymentRequested
  → LedgerService kabul (yalnızca clearing hold, müşteri posting yok)
  → IncomingEftPaymentHeld veya IncomingEftPaymentRejected
  → ops POST .../incoming-eft/{id}/settle (payments:settle)
      → IncomingEftSettlementRequested → debit clearing / credit müşteri → IncomingEftPaymentCompleted
  → ops POST .../incoming-eft/{id}/reject (payments:settle)
      → IncomingEftReturnRequested → clearing hold release → IncomingEftPaymentRejected

POST /api/payments/eft (Idempotency-Key, TR IBAN)
  → EftPaymentRequested
  → LedgerService yalnızca hold (posting yok)
  → EftPaymentHeld veya EftPaymentRejected
  → ops POST .../eft/{id}/settle (payments:settle)
      → EftSettlementRequested → debit kaynak / credit clearing → EftPaymentCompleted
  → ops POST .../eft/{id}/reject (payments:settle)
      → EftReturnRequested → hold release → EftPaymentRejected

POST /api/cards/{id}/authorize (Idempotency-Key)
  → CardAuthorizationRequested
  → LedgerService yalnızca hold (card-auth-hold:{id})
  → CardAuthorizationHeld
  → POST .../authorizations/{authId}/capture → CardCaptureRequested → debit müşteri / credit clearing (card-auth:{id})
  → POST .../authorizations/{authId}/reverse → CardAuthorizationReverseRequested → hold release

Virman/FAST/EFT/gelen/kart requested event’leri
  → RiskService tarama (posting’i durdurmaz)
  → tutar ≥ 25_000 ise LargeAmount, UTC günü giden toplam ≥ 80_000 ise DailyVelocity
  → ops GET /api/risk/alerts (risk:review), POST .../{id}/close

POST /api/loans (Idempotency-Key)
  → Applied (posting yok)
  → ops POST .../loans/{id}/approve (loans:review)
      → LoanDisbursementRequested → debit clearing / credit müşteri (loan:{id}) → eşit taksit
  → POST .../loans/{id}/repay
      → LoanRepaymentRequested → debit müşteri / credit clearing (loan-repay:{installmentId})

POST /api/accounts { currency: USD|EUR }
  → AccountOpened (KYC TRY varsa)
POST /api/payments/fx-conversions (Idempotency-Key)
  → FxConversionRequested
  → Ledger fx-sell:{id} (debit kaynak / credit kaynak clearing) + fx-buy:{id} (debit hedef clearing / credit hedef)
  → FxConversionCompleted veya FxConversionRejected

POST /api/open-banking/consents (JWT, Idempotency-Key)
  → Active rıza, AccessToken 32 hex (TPP sandbox-tpp)
GET /api/open-banking/ais/accounts (X-Consent-Token)
  → yalnızca IBAN/döviz (canlı ledger yok)
POST /api/open-banking/pis/payments (X-Consent-Token, Idempotency-Key)
  → OpenBankingPaymentRequested (InitiationId = transfer id)
  → Payment virman → TransferCompleted / TransferRejected
```

| Servis | HTTP | Veritabanı |
|---|---|---|
| AuthService | `http://localhost:5229` | `BankingAuthServiceDb` |
| CustomerService | `http://localhost:5029` | `BankingCustomerServiceDb` |
| AccountService | `http://localhost:5039` | `BankingAccountServiceDb` |
| LedgerService | `http://localhost:5049` | `BankingLedgerServiceDb` |
| PaymentService | `http://localhost:5059` | `BankingPaymentServiceDb` |
| NotificationService | `http://localhost:5069` | `BankingNotificationServiceDb` |
| CardService | `http://localhost:5079` | `BankingCardServiceDb` |
| RiskService | `http://localhost:5089` | `BankingRiskServiceDb` |
| LoanService | `http://localhost:5099` | `BankingLoanServiceDb` |
| OpenBankingService | `http://localhost:5109` | `BankingOpenBankingServiceDb` |
| Gateway (YARP) | `http://localhost:5000` | — |

JWT tüm servislerde aynı `JwtSettings`. Claim: `customer_id`. Operasyon: `permission` (`kyc:review`, `customers:read`, `roles:assign`, `payments:credit`, `payments:settle`, `risk:review`, `loans:review`). İstemci her servis portunu veya `:5000` gateway’i kullanabilir (`/api/open-banking` dahil). Gateway JWT doğrulamaz.

### AuthService

Refresh token, e-posta OTP, 2FA, token rotation. Kayıtta `UserRegistered` outbox ile yayınlanır (şifre/token/OTP event’te yok).

Örnek: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/login/verify-otp`, `POST /api/auth/refresh`, `POST /api/auth/email-otp/send`, `POST /api/auth/2fa/enable`.

### CustomerService

`UserRegistered` consumer CIF iskeleti açar. Aynı `CustomerId` ikinci kez gelirse yeni kayıt açılmaz. TCKN/adres/KVKK `POST /api/customers` ile dolar. KYC belgesi yüklenir, submit edilir, review `kyc:review` ister. Onayda `KycApproved` yayınlanır.

### AccountService

İlk TRY vadesiz HTTP POST ile açılmaz. `KycApproved` consumer `(CustomerId, DemandDeposit, TRY)` yoksa `Active` vadesiz TRY hesap ve TR IBAN (ISO 7064 mod-97, 26 karakter) yazar, ardından `AccountOpened` outbox ile yayınlanır. TRY varsa `POST /api/accounts` `USD` veya `EUR` ikinci vadesiz açar (aynı tekil anahtar `(CustomerId, ProductType, Currency)`); tekrar mevcut kaydı döner. Okuma: `GET /api/accounts/me` (`customer_id`), `GET /api/accounts/{id}` (sahip veya `customers:read`).

### LedgerService

Bakiye `Account` tablosunda tutulmaz. `AccountOpened` consumer müşteri vadesiz yükümlülük projeksiyonunu ledger/hold/available = 0 ile açar. Çift kayıt journal: debit = credit, idempotency key. Hold available’ı düşürür, ledger’ı düşürmez. `TransferRequested` iç virmanı tek veritabanı işleminde post eder: kaynakta hold, debit kaynak / credit hedef, hold release. Journal key `transfer:{transferId}`, hold key `transfer-hold:{transferId}`. `TestCreditRequested` debit `InternalClearingTry` / credit müşteri (`test-credit:{creditId}`). `FastPaymentRequested` hold sonra debit kaynak / credit clearing (`fast:{id}`, `fast-hold:{id}`). `IncomingFastPaymentRequested` debit clearing / credit müşteri, hold yok (`incoming-fast:{id}`). `EftPaymentRequested` yalnızca aktif hold yazar (`eft-hold:{id}`) ve `EftPaymentHeld` yayınlar; posting `EftSettlementRequested` ile gelir (debit kaynak / credit clearing, `eft:{id}`, hold release) veya `EftReturnRequested` hold’u serbest bırakır (journal yok). `IncomingEftPaymentRequested` yalnızca clearing hold yazar (`incoming-eft-hold:{id}`, müşteri available değişmez) ve `IncomingEftPaymentHeld` yayınlar; posting `IncomingEftSettlementRequested` ile gelir (debit clearing / credit müşteri, `incoming-eft:{id}`) veya `IncomingEftReturnRequested` hold’u serbest bırakır (journal yok). `CardAuthorizationRequested` yalnızca aktif müşteri hold yazar (`card-auth-hold:{id}`) ve `CardAuthorizationHeld` yayınlar; posting `CardCaptureRequested` ile gelir (debit müşteri / credit clearing, `card-auth:{id}`, hold release) veya `CardAuthorizationReverseRequested` hold’u serbest bırakır (journal yok). `FxConversionRequested` tek SaveAsync’te iki aynı para birimli journal yazar: `fx-sell:{id}` debit kaynak / credit kaynak clearing, `fx-buy:{id}` debit hedef clearing / credit hedef (`InternalClearingUsd` / `InternalClearingEur` seed). Journal varsa Ledger completed event’i yeniden yayınlar. İş kuralı reddi matching rejected event yayınlar. Genel HTTP posting yok. Okuma: `GET /api/ledger/me`, `GET /api/ledger/accounts/{accountId}`, `GET /api/ledger/accounts/{accountId}/movements`, `GET /api/ledger/accounts/{accountId}/statement?from=&to=` (`customer_id` veya `customers:read`). Ekstre opening/closing ledger ve dönem hareketini döner (hold ekstrede yok). `{accountId}` AccountService id’sidir.

### PaymentService

Payment, `AccountOpened` projeksiyonu ve ödeme kayıtlarını tutar. Bakiye tutmaz; Ledger’a HTTP atmaz. `POST /api/payments/transfers` JWT `customer_id` ve `Idempotency-Key` ister. `POST /api/payments/fast` ve `POST /api/payments/eft` giden TR IBAN (ISO 7064 mod-97) içindir; dahili projeksiyonda olan IBAN reddedilir (virman kullanılır). FAST hemen post eder. EFT `Held` kalır; ops settle/return sahte clearing’dir. `POST /api/payments/incoming-fast` ve `POST /api/payments/incoming-eft` `payments:credit` ister; hedef IBAN dahili projeksiyonda olmalı; dahili kaynak IBAN reddedilir (virman). Gelen FAST hemen post eder (clearing debit, müşteri credit, hold yok). Gelen EFT `Held` kalır; ops settle/return bekler. Gelen tutar giden günlük limite sayılmaz. Limit: işlem/FAST/EFT/gelen FAST/gelen EFT başına 50_000, UTC gününde giden virman+FAST+EFT+FX TRY karşılığı toplam 100_000 (Initiated + Held + Completed; Rejected sayılmaz). `POST /api/payments/fx-conversions` çağıranın kendi TRY/USD/EUR hesapları arasında sabit kurla çevirir (`UsdTry` 34, `EurTry` 37; TCMB yayını değil). Aynı para birimi reddedilir (virman kullan). İşlem tavanı TRY karşılığıdır. `POST /api/payments/test-credits` `payments:credit` ister; kredi başına en fazla 100_000; müşteri ledger’ını fonlar (clearing debit, müşteri credit) ki available 0’da kalmasın. `POST /api/payments/eft/{id}/settle`, `POST /api/payments/eft/{id}/reject`, `POST /api/payments/incoming-eft/{id}/settle` ve `POST /api/payments/incoming-eft/{id}/reject` `payments:settle` ister. Ödeme gerçekten Held/Completed/Rejected olduğunda Payment aynı EF outbox işleminde `NotificationRequested` yayınlar. Okuma: `GET /api/payments/transfers/me`, `GET /api/payments/transfers/{id}`, `GET /api/payments/fast/me`, `GET /api/payments/fast/{id}`, `GET /api/payments/incoming-fast/me`, `GET /api/payments/incoming-fast/{id}` (sahip, `customers:read` veya `payments:credit`), `GET /api/payments/incoming-eft/me`, `GET /api/payments/incoming-eft/{id}` (sahip, `customers:read`, `payments:credit` veya `payments:settle`), `GET /api/payments/eft/me`, `GET /api/payments/eft/{id}` (sahip, `customers:read` veya EFT için `payments:settle`), `GET /api/payments/test-credits/{id}` (`payments:credit`), `GET /api/payments/fx-conversions/me`, `GET /api/payments/fx-conversions/{id}` (sahip veya `customers:read`). `OpenBankingPaymentRequested` virman açar; transfer id initiation id’dir (`open-banking:{id}`).

### NotificationService

Notification müşteri kutusu satırlarını tutar. SMS/e-posta göndermez. `NotificationRequested` (Payment/Card), `KycApproved` ve `AccountOpened` tüketir. Aynı `(Type, SourceId)` ikinci kez yazılmaz. Okuma: `GET /api/notifications/me` (`customer_id`). Okundu: `POST /api/notifications/{id}/read` (yalnızca sahip).

### CardService

Kart, `AccountOpened` ile sanal debit açar (hesap başına bir kart). PAN tutulmaz: `Token` Guid hex, `Last4` dört hane, `Brand` `Virtual`. `POST /api/cards/{id}/authorize` JWT sahip ve `Idempotency-Key` ister (en fazla 50_000 TRY). Ledger hold EFT kalıbındadır. Capture/reverse sahip HTTP çağrılarıdır; `CardCaptureRequested` / `CardAuthorizationReverseRequested` yayınlar. Dondurma: `POST /api/cards/{id}/freeze`. Okuma: `GET /api/cards/me`, `GET /api/cards/{id}` (sahip veya `customers:read`), `GET /api/cards/{id}/authorizations`. Durum güncellemesi de `NotificationRequested` yayınlar. Bu bir PCI processor değildir; 3DS yoktur.

### RiskService

Risk işlem sonrası izlemedir; MASAK motoru değildir ve ödemeyi durdurmaz. Payment/Ledger’a HTTP atmaz. `AccountOpened` hesap projeksiyonu kurar (requested event’te `CustomerId` yok). Her virman/FAST/EFT/gelen FAST/gelen EFT/kart yetkisi/FX/kredi kullandırım bir kez taranır (`Channel` + `SourceId`). Projeksiyon yoksa atlanır. Tutar ≥ 25_000 `LargeAmount` açar. UTC günü giden toplam (virman+FAST+EFT+kart+FX TRY karşılığı+kredi) ≥ 80_000 `DailyVelocity` açar (gelen sayılmaz). UTC günü `[20_000, 25_000)` bandında 3 veya daha fazla giden `Structuring` açar. `KycApproved` ad ve TCKN taşır; sahte izleme listesi (`99999999990`, ad `YASAKLI TEST`) `WatchlistHit` açar. Bu hâlâ MASAK motoru değildir. Ops: `GET /api/risk/alerts`, `GET /api/risk/alerts/{id}`, `POST /api/risk/alerts/{id}/close` `risk:review` ister.

### LoanService

Sade tüketici kredisi: faiz yok, eşit anapara taksiti. Lisanslı kredi ürünü değil. `POST /api/loans` JWT sahip ve `Idempotency-Key` ister (en fazla 100_000 TRY, vade 3–12 ay). Durum `Applied` kalır; ops `POST /api/loans/{id}/approve` (`loans:review`) `LoanDisbursementRequested` yayınlar. Ledger debit clearing / credit müşteri (`loan:{id}`). `LoanDisbursementPosted` N taksit yazar (son satır kalanı alır). `POST /api/loans/{id}/repay` sıradaki Pending taksiti öder (`loan-repay:{installmentId}`: debit müşteri / credit clearing). Pending kalmayınca `Completed`. `Applied` iken ops red: `POST /api/loans/{id}/reject`. Okuma: `GET /api/loans/me`, `GET /api/loans/{id}` (sahip veya `customers:read`).

### OpenBankingService

Lisanslı PSD2 ve OAuth2 değildir. Müşteri `POST /api/open-banking/consents` (`Idempotency-Key`) ile TPP `sandbox-tpp`’ye AIS (`1`) ve/veya PIS (`2`) verir (1–90 gün, sahip hesaplar). Token Guid N (32 hex). TPP `X-Consent-Token` gönderir (JWT yok): `GET /api/open-banking/ais/accounts` yalnızca IBAN/döviz döner (canlı ledger yok). `POST /api/open-banking/pis/payments` `OpenBankingPaymentRequested` yayınlar; Payment virman açar (`Transfer.Id = InitiationId`). `TransferCompleted` / `TransferRejected` / `OpenBankingPaymentRejected` initiation durumunu günceller. İptal: `POST /api/open-banking/consents/{id}/revoke`. Okuma: `GET /api/open-banking/consents/me`, `GET /api/open-banking/pis/payments/{id}`.

### Gateway

YARP reverse proxy `http://localhost:5000`. Path öneki ilgili servis portuna gider (`/api/open-banking` → 5109). JWT’yi her servis doğrular. `Authorization`, `Idempotency-Key` ve `X-Consent-Token` ileri gider. Üretim ingress değildir.

## Çalıştırma

PostgreSQL (`localhost:5432`) ve RabbitMQ (`localhost:5672`) ayakta olsun. Her servis kendi DB’sine migration uygular, sonra Presentation projesinden koşar.

```powershell
dotnet ef database update --project Services/AuthService/src/AuthService.Infrastructure --startup-project Services/AuthService/src/AuthService.Presentation
dotnet ef database update --project Services/CustomerService/src/CustomerService.Infrastructure --startup-project Services/CustomerService/src/CustomerService.Presentation
dotnet ef database update --project Services/AccountService/src/AccountService.Infrastructure --startup-project Services/AccountService/src/AccountService.Presentation
dotnet ef database update --project Services/LedgerService/src/LedgerService.Infrastructure --startup-project Services/LedgerService/src/LedgerService.Presentation
dotnet ef database update --project Services/PaymentService/src/PaymentService.Infrastructure --startup-project Services/PaymentService/src/PaymentService.Presentation
dotnet ef database update --project Services/NotificationService/src/NotificationService.Infrastructure --startup-project Services/NotificationService/src/NotificationService.Presentation
dotnet ef database update --project Services/CardService/src/CardService.Infrastructure --startup-project Services/CardService/src/CardService.Presentation
dotnet ef database update --project Services/RiskService/src/RiskService.Infrastructure --startup-project Services/RiskService/src/RiskService.Presentation
dotnet ef database update --project Services/LoanService/src/LoanService.Infrastructure --startup-project Services/LoanService/src/LoanService.Presentation
dotnet ef database update --project Services/OpenBankingService/src/OpenBankingService.Infrastructure --startup-project Services/OpenBankingService/src/OpenBankingService.Presentation
```

Geliştirme bağlantı bilgileri `appsettings.json` içindedir; üretim sırrı değildir. Gateway’in veritabanı yoktur; diğer Presentation’lardan sonra `Services/Gateway` `http://localhost:5000` üzerinde çalışır.

## Testler

xUnit + NSubstitute. HTTP ve RabbitMQ yok. Çalıştırma:

```powershell
dotnet test
```

| Proje | Kilitlenen kural |
|---|---|
| `AuthService.UnitTests` | Refresh rotation; grace dışı reuse → 401 ve family revoke; login OTP; 2FA aç/kapa |
| `CustomerService.UnitTests` | `UserRegistered` aynı `CustomerId` ikinci kayıt açmaz; `POST .../kyc/review` `kyc:review` ister |
| `AccountService.UnitTests` | TR IBAN ISO 7064 mod-97; `KycApproved` ikinci `(CustomerId, DemandDeposit, TRY)` hesap açmaz; açılışta `AccountOpened` yayınlanır; USD TRY sonrası açılır; tekrar USD yayınlamaz; `POST /api/accounts` TRY reddeder |
| `LedgerService.UnitTests` | `AccountOpened` projeksiyonu idempotent ve posting yok; debit = credit; aynı idempotency key ikinci kayıt yazmaz; müşteri credit ledger/available artırır; hold/release yalnızca available değiştirir; iç virman debit kaynak / credit hedef, hold release, aynı transfer id ikinci posting yazmaz; test kredisi debit clearing / credit müşteri; FAST debit kaynak / credit clearing; gelen FAST debit clearing / credit müşteri, hold yok; EFT hold settle’a kadar açık kalır (sonra debit kaynak / credit clearing) veya iade hold’u serbest bırakır (journal yok); gelen EFT clearing hold settle’a kadar açık (sonra debit clearing / credit müşteri) veya iade (journal yok, müşteri available değişmez); dönem ekstresi opening/period/closing ayırır; kart authorize hold sonra capture (debit müşteri / credit clearing) veya reverse (journal yok); kredi kullandırım debit clearing / credit müşteri; taksit debit müşteri / credit clearing; FX satış TRY / alış USD iki journal, aynı conversion id ikinci posting yazmaz |
| `PaymentService.UnitTests` | Create `TransferRequested` yayınlar (`Initiated`); aynı `Idempotency-Key` mevcut kaydı döner; işlem ve günlük limit reddeder; kaynak çağırana ait olmalı; `AccountOpened` projeksiyonu idempotent; Completed/Rejected durumu günceller, Completed üzerine yazmaz; FAST/EFT geçerli dış TR IBAN ister; gelen FAST/EFT dahili hedef IBAN ister ve giden günlük limite sayılmaz; günlük limit virman+FAST+EFT paylaşılır ve Held sayılır; test kredisi ve gelen FAST/EFT create `payments:credit` ister; EFT ve gelen EFT settle/return `payments:settle` ister; FX TRY→USD sabit kur 34, aynı `Idempotency-Key` mevcut kaydı döner, aynı para birimi ve yabancı hedef reddeder; OpenBanking PIS initiation id’yi transfer id olarak kullanır |
| `NotificationService.UnitTests` | `(Type, SourceId)` ikinci kayıt açmaz; `GET me` `customer_id` ile sınırlıdır; okundu işareti başka müşteriyi reddeder |
| `CardService.UnitTests` | `AccountOpened` hesap başına bir sanal kart açar; authorize `CardAuthorizationRequested` yayınlar; aynı `Idempotency-Key` mevcut kaydı döner; dondurulmuş kart ve 50_000 üstü tutar reddeder; capture/reverse Held ister; Completed üzerine yazılmaz; token 32 hex, last4 dört hanedir |
| `RiskService.UnitTests` | `AccountOpened` projeksiyonu idempotent; 25_000 `LargeAmount` açar, 24_999 açmaz; aynı source id ikinci tarama yazmaz; hesap yoksa atlar; UTC günü giden 3×30_000 `DailyVelocity` açar; gelen büyük tutar günlük hız açmaz; FX TRY karşılığı 25_000 `LargeAmount` açar; 3×24_000 `Structuring` açar; izleme listesi TCKN `WatchlistHit` açar; kredi 25_000 `LargeAmount` açar; kapatma `risk:review` ister |
| `LoanService.UnitTests` | Apply `Applied` kalır ve kullandırım yazmaz; aynı `Idempotency-Key` mevcut kaydı döner; tutar/vade/sahiplik reddeder; approve `LoanDisbursementRequested` yayınlar; red yalnızca `Applied`; posted taksit toplamı anaparaya eşit; repay `Disbursed` ister; son ödeme `Completed`; approve/reject `loans:review` ister |
| `OpenBankingService.UnitTests` | Consent Active kalır, token 32 hex; aynı `Idempotency-Key` mevcut kaydı döner; bilinmeyen TPP ve sahip olunmayan hesap reddeder; AIS rızalı hesapları listeler; iptal/süresi dolmuş/yalnızca PIS AIS reddeder; PIS `OpenBankingPaymentRequested` yayınlar; PIS kapsamı yoksa reddeder; `TransferCompleted` Completed üzerine yazmaz; iptal sonrası PIS reddeder |
