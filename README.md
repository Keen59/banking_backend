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

Order: Auth → Customer (CIF/KYC) → Account → Ledger → Payment (internal transfer). An account is not opened before KYC approval. Ledger holds balances. Payment does not call Ledger over HTTP. FAST/EFT and cards are out of this slice.

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

POST /api/payments/transfers (Idempotency-Key)
  → TransferRequested (outbox → RabbitMQ)
  → LedgerService hold + double-entry (debit source, credit dest)
  → TransferCompleted or TransferRejected (outbox → RabbitMQ)
  → PaymentService status update
```

| Service | HTTP | Database |
|---|---|---|
| AuthService | `http://localhost:5229` | `BankingAuthServiceDb` |
| CustomerService | `http://localhost:5029` | `BankingCustomerServiceDb` |
| AccountService | `http://localhost:5039` | `BankingAccountServiceDb` |
| LedgerService | `http://localhost:5049` | `BankingLedgerServiceDb` |
| PaymentService | `http://localhost:5059` | `BankingPaymentServiceDb` |

JWT uses the same `JwtSettings` on every service. Claim: `customer_id`. Operations permissions: `kyc:review`, `customers:read`, `roles:assign`.

### AuthService

Refresh token, email OTP, 2FA, token rotation. Register publishes `UserRegistered` via the EF outbox (no password, token, or OTP on the event).

Examples: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/login/verify-otp`, `POST /api/auth/refresh`, `POST /api/auth/email-otp/send`, `POST /api/auth/2fa/enable`.

### CustomerService

The `UserRegistered` consumer opens a CIF stub. The same `CustomerId` is not inserted twice. TCKN/address/KVKK are filled with `POST /api/customers`. KYC documents are uploaded and submitted; review requires `kyc:review`. Approval publishes `KycApproved`.

### AccountService

Accounts are not created with HTTP POST. The `KycApproved` consumer inserts an `Active` demand-deposit TRY account and a TR IBAN (ISO 7064 mod-97, 26 characters) when `(CustomerId, DemandDeposit, TRY)` does not exist, then publishes `AccountOpened` via the EF outbox. Reads: `GET /api/accounts/me` (`customer_id`), `GET /api/accounts/{id}` (owner or `customers:read`).

### LedgerService

Balances are not stored on `Account`. The `AccountOpened` consumer opens a customer demand-deposit projection (liability) with ledger/hold/available = 0. Double-entry journals must have debit = credit and an idempotency key. Hold reduces available, not ledger. `TransferRequested` posts an internal transfer in one database transaction: hold on the source, debit source / credit destination, then release the hold. Journal key `transfer:{transferId}`, hold key `transfer-hold:{transferId}`. If the journal already exists, Ledger republishes `TransferCompleted`. Business rejections publish `TransferRejected`. No HTTP posting, FAST, or EFT in this slice. Reads: `GET /api/ledger/me`, `GET /api/ledger/accounts/{accountId}`, `GET /api/ledger/accounts/{accountId}/movements` (`customer_id` or `customers:read`). `{accountId}` is the AccountService id.

### PaymentService

Payment stores a local `AccountOpened` projection and transfer records. It does not hold balances and does not call Ledger over HTTP. `POST /api/payments/transfers` requires JWT `customer_id` and an `Idempotency-Key` header. Limits: max 50_000 per transfer, 100_000 per UTC day (Initiated + Completed; Rejected is excluded). The source account must belong to the caller and both projections must be Active with matching currency. Create inserts `Initiated` and publishes `TransferRequested` via the EF outbox. `TransferCompleted` / `TransferRejected` update status. Reads: `GET /api/payments/transfers/me`, `GET /api/payments/transfers/{id}` (owner or `customers:read`).

## Run

PostgreSQL (`localhost:5432`) and RabbitMQ (`localhost:5672`) must be up. Apply each service’s migration, then run the Presentation project.

```powershell
dotnet ef database update --project Services/AuthService/src/AuthService.Infrastructure --startup-project Services/AuthService/src/AuthService.Presentation
dotnet ef database update --project Services/CustomerService/src/CustomerService.Infrastructure --startup-project Services/CustomerService/src/CustomerService.Presentation
dotnet ef database update --project Services/AccountService/src/AccountService.Infrastructure --startup-project Services/AccountService/src/AccountService.Presentation
dotnet ef database update --project Services/LedgerService/src/LedgerService.Infrastructure --startup-project Services/LedgerService/src/LedgerService.Presentation
dotnet ef database update --project Services/PaymentService/src/PaymentService.Infrastructure --startup-project Services/PaymentService/src/PaymentService.Presentation
```

Connection strings in `appsettings.json` are for local development, not production secrets.

## Tests

xUnit + NSubstitute. No HTTP or RabbitMQ. Run:

```powershell
dotnet test
```

| Project | What is locked |
|---|---|
| `AuthService.UnitTests` | Refresh rotation; reused refresh token outside grace → 401 and family revoke; login OTP success/fail; 2FA enable/disable |
| `CustomerService.UnitTests` | `UserRegistered` does not insert the same `CustomerId` twice; `POST .../kyc/review` requires `kyc:review` |
| `AccountService.UnitTests` | TR IBAN ISO 7064 mod-97; `KycApproved` does not open a second `(CustomerId, DemandDeposit, TRY)` account; `AccountOpened` is published on create |
| `LedgerService.UnitTests` | `AccountOpened` projection is idempotent and posts nothing; debit must equal credit; duplicate idempotency key does not double-post; customer credit raises ledger/available; hold/release changes available only; internal transfer debit source / credit dest, hold released, same transfer id does not double-post |
| `PaymentService.UnitTests` | Create publishes `TransferRequested` as `Initiated`; same `Idempotency-Key` returns the existing row; per-transfer and daily limits reject; source must belong to the caller; `AccountOpened` projection is idempotent; Completed/Rejected update status and do not overwrite Completed |

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

Sıra: Auth → Customer (CIF/KYC) → Account → Ledger → Payment (iç virman). Hesap, KYC onayı olmadan açılmaz. Bakiye ledger’dadır. Payment, Ledger’a HTTP atmaz. FAST/EFT ve kart bu dilimde yok.

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

POST /api/payments/transfers (Idempotency-Key)
  → TransferRequested (outbox → RabbitMQ)
  → LedgerService hold + çift kayıt (debit kaynak, credit hedef)
  → TransferCompleted veya TransferRejected (outbox → RabbitMQ)
  → PaymentService durum güncellemesi
```

| Servis | HTTP | Veritabanı |
|---|---|---|
| AuthService | `http://localhost:5229` | `BankingAuthServiceDb` |
| CustomerService | `http://localhost:5029` | `BankingCustomerServiceDb` |
| AccountService | `http://localhost:5039` | `BankingAccountServiceDb` |
| LedgerService | `http://localhost:5049` | `BankingLedgerServiceDb` |
| PaymentService | `http://localhost:5059` | `BankingPaymentServiceDb` |

JWT tüm servislerde aynı `JwtSettings`. Claim: `customer_id`. Operasyon: `permission` (`kyc:review`, `customers:read`, `roles:assign`).

### AuthService

Refresh token, e-posta OTP, 2FA, token rotation. Kayıtta `UserRegistered` outbox ile yayınlanır (şifre/token/OTP event’te yok).

Örnek: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/login/verify-otp`, `POST /api/auth/refresh`, `POST /api/auth/email-otp/send`, `POST /api/auth/2fa/enable`.

### CustomerService

`UserRegistered` consumer CIF iskeleti açar. Aynı `CustomerId` ikinci kez gelirse yeni kayıt açılmaz. TCKN/adres/KVKK `POST /api/customers` ile dolar. KYC belgesi yüklenir, submit edilir, review `kyc:review` ister. Onayda `KycApproved` yayınlanır.

### AccountService

Hesap HTTP POST ile açılmaz. `KycApproved` consumer `(CustomerId, DemandDeposit, TRY)` yoksa `Active` vadesiz TRY hesap ve TR IBAN (ISO 7064 mod-97, 26 karakter) yazar, ardından `AccountOpened` outbox ile yayınlanır. Okuma: `GET /api/accounts/me` (`customer_id`), `GET /api/accounts/{id}` (sahip veya `customers:read`).

### LedgerService

Bakiye `Account` tablosunda tutulmaz. `AccountOpened` consumer müşteri vadesiz yükümlülük projeksiyonunu ledger/hold/available = 0 ile açar. Çift kayıt journal: debit = credit, idempotency key. Hold available’ı düşürür, ledger’ı düşürmez. `TransferRequested` iç virmanı tek veritabanı işleminde post eder: kaynakta hold, debit kaynak / credit hedef, hold release. Journal key `transfer:{transferId}`, hold key `transfer-hold:{transferId}`. Journal varsa Ledger `TransferCompleted` yeniden yayınlar. İş kuralı reddi `TransferRejected` yayınlar. HTTP posting, FAST, EFT yok bu dilimde. Okuma: `GET /api/ledger/me`, `GET /api/ledger/accounts/{accountId}`, `GET /api/ledger/accounts/{accountId}/movements` (`customer_id` veya `customers:read`). `{accountId}` AccountService id’sidir.

### PaymentService

Payment, `AccountOpened` projeksiyonu ve virman kayıtlarını tutar. Bakiye tutmaz; Ledger’a HTTP atmaz. `POST /api/payments/transfers` JWT `customer_id` ve `Idempotency-Key` ister. Limit: işlem başına 50_000, UTC gününde 100_000 (Initiated + Completed; Rejected sayılmaz). Kaynak hesap çağırana ait olmalı; her iki projeksiyon Active ve aynı para biriminde olmalı. Create `Initiated` yazar ve `TransferRequested` outbox ile yayınlanır. `TransferCompleted` / `TransferRejected` durumu günceller. Okuma: `GET /api/payments/transfers/me`, `GET /api/payments/transfers/{id}` (sahip veya `customers:read`).

## Çalıştırma

PostgreSQL (`localhost:5432`) ve RabbitMQ (`localhost:5672`) ayakta olsun. Her servis kendi DB’sine migration uygular, sonra Presentation projesinden koşar.

```powershell
dotnet ef database update --project Services/AuthService/src/AuthService.Infrastructure --startup-project Services/AuthService/src/AuthService.Presentation
dotnet ef database update --project Services/CustomerService/src/CustomerService.Infrastructure --startup-project Services/CustomerService/src/CustomerService.Presentation
dotnet ef database update --project Services/AccountService/src/AccountService.Infrastructure --startup-project Services/AccountService/src/AccountService.Presentation
dotnet ef database update --project Services/LedgerService/src/LedgerService.Infrastructure --startup-project Services/LedgerService/src/LedgerService.Presentation
dotnet ef database update --project Services/PaymentService/src/PaymentService.Infrastructure --startup-project Services/PaymentService/src/PaymentService.Presentation
```

Geliştirme bağlantı bilgileri `appsettings.json` içindedir; üretim sırrı değildir.

## Testler

xUnit + NSubstitute. HTTP ve RabbitMQ yok. Çalıştırma:

```powershell
dotnet test
```

| Proje | Kilitlenen kural |
|---|---|
| `AuthService.UnitTests` | Refresh rotation; grace dışı reuse → 401 ve family revoke; login OTP; 2FA aç/kapa |
| `CustomerService.UnitTests` | `UserRegistered` aynı `CustomerId` ikinci kayıt açmaz; `POST .../kyc/review` `kyc:review` ister |
| `AccountService.UnitTests` | TR IBAN ISO 7064 mod-97; `KycApproved` ikinci `(CustomerId, DemandDeposit, TRY)` hesap açmaz; açılışta `AccountOpened` yayınlanır |
| `LedgerService.UnitTests` | `AccountOpened` projeksiyonu idempotent ve posting yok; debit = credit; aynı idempotency key ikinci kayıt yazmaz; müşteri credit ledger/available artırır; hold/release yalnızca available değiştirir; iç virman debit kaynak / credit hedef, hold release, aynı transfer id ikinci posting yazmaz |
| `PaymentService.UnitTests` | Create `TransferRequested` yayınlar (`Initiated`); aynı `Idempotency-Key` mevcut kaydı döner; işlem ve günlük limit reddeder; kaynak çağırana ait olmalı; `AccountOpened` projeksiyonu idempotent; Completed/Rejected durumu günceller, Completed üzerine yazmaz |
