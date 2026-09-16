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

Order: Auth → Customer (CIF/KYC) → Account → Ledger → Payment (internal transfer, test credit, outbound FAST/EFT, incoming FAST). An account is not opened before KYC approval. Ledger holds balances. Payment does not call Ledger over HTTP. Opening balance is 0 until ops posts a test credit or incoming FAST. Cards and incoming EFT are out of this slice.

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

POST /api/payments/eft (Idempotency-Key, TR IBAN)
  → EftPaymentRequested
  → LedgerService hold only (no posting)
  → EftPaymentHeld or EftPaymentRejected
  → ops POST .../eft/{id}/settle (payments:settle)
      → EftSettlementRequested → debit source / credit clearing → EftPaymentCompleted
  → ops POST .../eft/{id}/reject (payments:settle)
      → EftReturnRequested → release hold → EftPaymentRejected
```

| Service | HTTP | Database |
|---|---|---|
| AuthService | `http://localhost:5229` | `BankingAuthServiceDb` |
| CustomerService | `http://localhost:5029` | `BankingCustomerServiceDb` |
| AccountService | `http://localhost:5039` | `BankingAccountServiceDb` |
| LedgerService | `http://localhost:5049` | `BankingLedgerServiceDb` |
| PaymentService | `http://localhost:5059` | `BankingPaymentServiceDb` |

JWT uses the same `JwtSettings` on every service. Claim: `customer_id`. Operations permissions: `kyc:review`, `customers:read`, `roles:assign`, `payments:credit`, `payments:settle`.

### AuthService

Refresh token, email OTP, 2FA, token rotation. Register publishes `UserRegistered` via the EF outbox (no password, token, or OTP on the event).

Examples: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/login/verify-otp`, `POST /api/auth/refresh`, `POST /api/auth/email-otp/send`, `POST /api/auth/2fa/enable`.

### CustomerService

The `UserRegistered` consumer opens a CIF stub. The same `CustomerId` is not inserted twice. TCKN/address/KVKK are filled with `POST /api/customers`. KYC documents are uploaded and submitted; review requires `kyc:review`. Approval publishes `KycApproved`.

### AccountService

Accounts are not created with HTTP POST. The `KycApproved` consumer inserts an `Active` demand-deposit TRY account and a TR IBAN (ISO 7064 mod-97, 26 characters) when `(CustomerId, DemandDeposit, TRY)` does not exist, then publishes `AccountOpened` via the EF outbox. Reads: `GET /api/accounts/me` (`customer_id`), `GET /api/accounts/{id}` (owner or `customers:read`).

### LedgerService

Balances are not stored on `Account`. The `AccountOpened` consumer opens a customer demand-deposit projection (liability) with ledger/hold/available = 0. Double-entry journals must have debit = credit and an idempotency key. Hold reduces available, not ledger. `TransferRequested` posts an internal transfer in one database transaction: hold on the source, debit source / credit destination, then release the hold. Journal key `transfer:{transferId}`, hold key `transfer-hold:{transferId}`. `TestCreditRequested` posts debit `InternalClearingTry` / credit customer (`test-credit:{creditId}`). `FastPaymentRequested` posts hold then debit source / credit clearing (`fast:{id}`, `fast-hold:{id}`). `IncomingFastPaymentRequested` posts debit clearing / credit customer with no hold (`incoming-fast:{id}`). `EftPaymentRequested` writes an active hold only (`eft-hold:{id}`) and publishes `EftPaymentHeld`; posting waits for `EftSettlementRequested` (debit source / credit clearing, `eft:{id}`, hold released) or `EftReturnRequested` (hold released, no journal). If the journal already exists, Ledger republishes the completed event. Business rejections publish the matching rejected event. No generic HTTP posting. Reads: `GET /api/ledger/me`, `GET /api/ledger/accounts/{accountId}`, `GET /api/ledger/accounts/{accountId}/movements` (`customer_id` or `customers:read`). `{accountId}` is the AccountService id.

### PaymentService

Payment stores a local `AccountOpened` projection and payment records. It does not hold balances and does not call Ledger over HTTP. `POST /api/payments/transfers` requires JWT `customer_id` and an `Idempotency-Key` header. `POST /api/payments/fast` and `POST /api/payments/eft` are the same for an outbound TR IBAN (ISO 7064 mod-97); an IBAN that already exists as an internal projection is rejected (use virman). FAST posts immediately. EFT stays `Held` until ops settle/return (fake clearing). `POST /api/payments/incoming-fast` requires `payments:credit`; destination IBAN must exist as an internal projection; an internal source IBAN is rejected (use virman). Incoming FAST posts immediately (clearing debit, customer credit, no hold) and does not count toward the outbound daily limit. Limits: max 50_000 per transfer/FAST/EFT/incoming FAST, 100_000 per UTC day shared across outbound virman+FAST+EFT (Initiated+Held+Completed; Rejected excluded). `POST /api/payments/test-credits` requires `payments:credit`; max 100_000 per credit; it funds a customer ledger (clearing debit, customer credit) so available is not stuck at 0. `POST /api/payments/eft/{id}/settle` and `POST /api/payments/eft/{id}/reject` require `payments:settle`. Reads: `GET /api/payments/transfers/me`, `GET /api/payments/transfers/{id}`, `GET /api/payments/fast/me`, `GET /api/payments/fast/{id}`, `GET /api/payments/incoming-fast/me`, `GET /api/payments/incoming-fast/{id}` (owner, `customers:read`, or `payments:credit`), `GET /api/payments/eft/me`, `GET /api/payments/eft/{id}` (owner, `customers:read`, or `payments:settle` for EFT), `GET /api/payments/test-credits/{id}` (`payments:credit`).

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
| `LedgerService.UnitTests` | `AccountOpened` projection is idempotent and posts nothing; debit must equal credit; duplicate idempotency key does not double-post; customer credit raises ledger/available; hold/release changes available only; internal transfer debit source / credit dest, hold released, same transfer id does not double-post; test credit debit clearing / credit customer; FAST debit source / credit clearing; incoming FAST debit clearing / credit customer with no hold; EFT hold stays active until settle (then debit source / credit clearing) or return (hold released, no journal) |
| `PaymentService.UnitTests` | Create publishes `TransferRequested` as `Initiated`; same `Idempotency-Key` returns the existing row; per-transfer and daily limits reject; source must belong to the caller; `AccountOpened` projection is idempotent; Completed/Rejected update status and do not overwrite Completed; FAST/EFT require a valid external TR IBAN; incoming FAST requires an internal destination IBAN and does not count toward the outbound daily limit; daily limit is shared virman+FAST+EFT and Held counts; test credit and incoming FAST require `payments:credit`; EFT settle/return require `payments:settle` |

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

Sıra: Auth → Customer (CIF/KYC) → Account → Ledger → Payment (iç virman, test kredisi, giden FAST/EFT, gelen FAST). Hesap, KYC onayı olmadan açılmaz. Bakiye ledger’dadır. Payment, Ledger’a HTTP atmaz. Opening balance 0’dır; available için ops test kredisi veya gelen FAST gerekir. Kart ve gelen EFT bu dilimde yok.

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

POST /api/payments/eft (Idempotency-Key, TR IBAN)
  → EftPaymentRequested
  → LedgerService yalnızca hold (posting yok)
  → EftPaymentHeld veya EftPaymentRejected
  → ops POST .../eft/{id}/settle (payments:settle)
      → EftSettlementRequested → debit kaynak / credit clearing → EftPaymentCompleted
  → ops POST .../eft/{id}/reject (payments:settle)
      → EftReturnRequested → hold release → EftPaymentRejected
```

| Servis | HTTP | Veritabanı |
|---|---|---|
| AuthService | `http://localhost:5229` | `BankingAuthServiceDb` |
| CustomerService | `http://localhost:5029` | `BankingCustomerServiceDb` |
| AccountService | `http://localhost:5039` | `BankingAccountServiceDb` |
| LedgerService | `http://localhost:5049` | `BankingLedgerServiceDb` |
| PaymentService | `http://localhost:5059` | `BankingPaymentServiceDb` |

JWT tüm servislerde aynı `JwtSettings`. Claim: `customer_id`. Operasyon: `permission` (`kyc:review`, `customers:read`, `roles:assign`, `payments:credit`, `payments:settle`).

### AuthService

Refresh token, e-posta OTP, 2FA, token rotation. Kayıtta `UserRegistered` outbox ile yayınlanır (şifre/token/OTP event’te yok).

Örnek: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/login/verify-otp`, `POST /api/auth/refresh`, `POST /api/auth/email-otp/send`, `POST /api/auth/2fa/enable`.

### CustomerService

`UserRegistered` consumer CIF iskeleti açar. Aynı `CustomerId` ikinci kez gelirse yeni kayıt açılmaz. TCKN/adres/KVKK `POST /api/customers` ile dolar. KYC belgesi yüklenir, submit edilir, review `kyc:review` ister. Onayda `KycApproved` yayınlanır.

### AccountService

Hesap HTTP POST ile açılmaz. `KycApproved` consumer `(CustomerId, DemandDeposit, TRY)` yoksa `Active` vadesiz TRY hesap ve TR IBAN (ISO 7064 mod-97, 26 karakter) yazar, ardından `AccountOpened` outbox ile yayınlanır. Okuma: `GET /api/accounts/me` (`customer_id`), `GET /api/accounts/{id}` (sahip veya `customers:read`).

### LedgerService

Bakiye `Account` tablosunda tutulmaz. `AccountOpened` consumer müşteri vadesiz yükümlülük projeksiyonunu ledger/hold/available = 0 ile açar. Çift kayıt journal: debit = credit, idempotency key. Hold available’ı düşürür, ledger’ı düşürmez. `TransferRequested` iç virmanı tek veritabanı işleminde post eder: kaynakta hold, debit kaynak / credit hedef, hold release. Journal key `transfer:{transferId}`, hold key `transfer-hold:{transferId}`. `TestCreditRequested` debit `InternalClearingTry` / credit müşteri (`test-credit:{creditId}`). `FastPaymentRequested` hold sonra debit kaynak / credit clearing (`fast:{id}`, `fast-hold:{id}`). `IncomingFastPaymentRequested` debit clearing / credit müşteri, hold yok (`incoming-fast:{id}`). `EftPaymentRequested` yalnızca aktif hold yazar (`eft-hold:{id}`) ve `EftPaymentHeld` yayınlar; posting `EftSettlementRequested` ile gelir (debit kaynak / credit clearing, `eft:{id}`, hold release) veya `EftReturnRequested` hold’u serbest bırakır (journal yok). Journal varsa Ledger completed event’i yeniden yayınlar. İş kuralı reddi matching rejected event yayınlar. Genel HTTP posting yok. Okuma: `GET /api/ledger/me`, `GET /api/ledger/accounts/{accountId}`, `GET /api/ledger/accounts/{accountId}/movements` (`customer_id` veya `customers:read`). `{accountId}` AccountService id’sidir.

### PaymentService

Payment, `AccountOpened` projeksiyonu ve ödeme kayıtlarını tutar. Bakiye tutmaz; Ledger’a HTTP atmaz. `POST /api/payments/transfers` JWT `customer_id` ve `Idempotency-Key` ister. `POST /api/payments/fast` ve `POST /api/payments/eft` giden TR IBAN (ISO 7064 mod-97) içindir; dahili projeksiyonda olan IBAN reddedilir (virman kullanılır). FAST hemen post eder. EFT `Held` kalır; ops settle/return sahte clearing’dir. `POST /api/payments/incoming-fast` `payments:credit` ister; hedef IBAN dahili projeksiyonda olmalı; dahili kaynak IBAN reddedilir (virman). Gelen FAST hemen post eder (clearing debit, müşteri credit, hold yok) ve giden günlük limite sayılmaz. Limit: işlem/FAST/EFT/gelen FAST başına 50_000, UTC gününde giden virman+FAST+EFT toplam 100_000 (Initiated + Held + Completed; Rejected sayılmaz). `POST /api/payments/test-credits` `payments:credit` ister; kredi başına en fazla 100_000; müşteri ledger’ını fonlar (clearing debit, müşteri credit) ki available 0’da kalmasın. `POST /api/payments/eft/{id}/settle` ve `POST /api/payments/eft/{id}/reject` `payments:settle` ister. Okuma: `GET /api/payments/transfers/me`, `GET /api/payments/transfers/{id}`, `GET /api/payments/fast/me`, `GET /api/payments/fast/{id}`, `GET /api/payments/incoming-fast/me`, `GET /api/payments/incoming-fast/{id}` (sahip, `customers:read` veya `payments:credit`), `GET /api/payments/eft/me`, `GET /api/payments/eft/{id}` (sahip, `customers:read` veya EFT için `payments:settle`), `GET /api/payments/test-credits/{id}` (`payments:credit`).

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
| `LedgerService.UnitTests` | `AccountOpened` projeksiyonu idempotent ve posting yok; debit = credit; aynı idempotency key ikinci kayıt yazmaz; müşteri credit ledger/available artırır; hold/release yalnızca available değiştirir; iç virman debit kaynak / credit hedef, hold release, aynı transfer id ikinci posting yazmaz; test kredisi debit clearing / credit müşteri; FAST debit kaynak / credit clearing; gelen FAST debit clearing / credit müşteri, hold yok; EFT hold settle’a kadar açık kalır (sonra debit kaynak / credit clearing) veya iade hold’u serbest bırakır (journal yok) |
| `PaymentService.UnitTests` | Create `TransferRequested` yayınlar (`Initiated`); aynı `Idempotency-Key` mevcut kaydı döner; işlem ve günlük limit reddeder; kaynak çağırana ait olmalı; `AccountOpened` projeksiyonu idempotent; Completed/Rejected durumu günceller, Completed üzerine yazmaz; FAST/EFT geçerli dış TR IBAN ister; gelen FAST dahili hedef IBAN ister ve giden günlük limite sayılmaz; günlük limit virman+FAST+EFT paylaşılır ve Held sayılır; test kredisi ve gelen FAST `payments:credit` ister; EFT settle/return `payments:settle` ister |
