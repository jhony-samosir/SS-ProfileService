# SS-ProfileService

## Overview

`SS-ProfileService` adalah microservice pengelola profil pelanggan dan buku alamat untuk platform SamStore. Layanan backend ini memisahkan secara jelas informasi demografis pengguna (seperti biodata, nomor HP) dari fungsi core autentikasi, serta mengelola alamat pengiriman (*shipping address*) pengguna.

Dibangun dengan **.NET 10.0 (C#)**, aplikasi ini menerapkan desain modern **Vertical Slice Architecture** dengan pola **CQRS (via MediatR)**. Desain ini memastikan independensi kode di tiap-tiap fitur tanpa hirarki folder controller/service klasik.

Layanan ini mengintegrasikan pola RabbitMQ Inbox untuk mendengarkan pesan pendaftaran user baru dari `SS-AuthService` agar profil awal langsung terbentuk secara *event-driven*.

---

## Tech Stack

| Kategori       | Teknologi                                      |
| -------------- | ---------------------------------------------- |
| Backend        | .NET 10.0 (C#) Minimal API                     |
| Architecture   | Vertical Slice Architecture (MediatR CQRS)     |
| Database       | PostgreSQL                                     |
| ORM            | Entity Framework Core (Npgsql)                 |
| Message Broker | RabbitMQ (.NET Client / MassTransit)           |
| Validation     | FluentValidation                               |
| Telemetry      | OpenTelemetry, Serilog                         |
| Security       | HMAC Signature Validation (Zero-Trust Gateway) |

---

## Arsitektur: Vertical Slice

Berbeda dengan Clean Architecture, seluruh perintah (*Commands*), kuiri (*Queries*), validasi (*Validators*), dan endpoints (*API Mappings*) untuk sebuah fitur disatukan dalam satu lokasi direktori (Slice).

```text
SS-ProfileService/
├── src/
│   └── SS.ProfileService.API/
│       ├── Features/
│       │   ├── Profiles/                 # Slice: Profil Pengguna
│       │   │   ├── CreateProfile/        # Handler/Endpoint untuk inisialisasi profil
│       │   │   ├── GetProfileById/       # Handler/Endpoint untuk membaca data profil
│       │   │   └── UpdateProfile/        # Handler/Endpoint untuk memperbarui bio, avatar
│       │   └── Addresses/                # Slice: Buku Alamat
│       │       ├── CreateAddress/        # Handler/Endpoint penambahan alamat baru
│       │       ├── UpdateAddress/        # Handler/Endpoint update data alamat
│       │       ├── DeleteAddress/        # Handler/Endpoint soft-delete alamat
│       │       ├── SetDefaultAddress/    # Mengganti alamat default primary
│       │       └── Shared/               # Entitas/Record yang dibagi antar operasi alamat
│       ├── Domain/                       # Entitas EF Core (UserProfile, Address, InboxEvent)
│       ├── Infrastructure/               # Database Context, RabbitMQ worker
│       ├── Middleware/                   # GatewaySignatureMiddleware (HMAC)
│       ├── Extensions/                   # OpenTelemetry DI setup
│       ├── Program.cs                    # Minimal API Registration
│       └── appsettings.json
├── test/
│   └── SS.ProfileService.Tests/          # xUnit integration & unit tests
└── SS-ProfileService.slnx
```

---

## Fitur Utama

- **Profile Management**: Update biodata, avatar upload (link image), dan nomor kontak.
- **Address Book Management**: Relasi *one-to-many* untuk alamat user. Mendukung logika `is_default`, di mana pemilihan alamat baru otomatis mereset flag alamat lama.
- **Zero-Trust Middleware**: Menggunakan `GatewaySignatureMiddleware` untuk menolak request API secara langsung yang tidak memiliki signature dari API Gateway (`GATEWAY_HMAC_SECRET`).
- **Inbox Idempotency**: Mencegah proses inisialisasi ganda pada profil saat service mendengarkan pesan pendaftaran akun dari message broker secara asinkron.
- **Soft Deletion**: Mencegah kehilangan historis data keranjang atau order (menggunakan kolom `deleted_at`).

---

## API Endpoints (Minimal API)

Endpoints didefinisikan dalam masing-masing slice di `Features/...`. Endpoint diproteksi via middleware.

| Kategori | Endpoint                               | HTTP Method | Auth Role | Deskripsi                               |
| -------- | -------------------------------------- | ----------- | --------- | --------------------------------------- |
| Profile  | `/api/profiles/me`                     | GET         | JWT User  | Dapatkan profil sendiri                 |
| Profile  | `/api/profiles/me`                     | PUT         | JWT User  | Perbarui profil sendiri                 |
| Address  | `/api/profiles/me/addresses`           | GET         | JWT User  | List semua buku alamat user             |
| Address  | `/api/profiles/me/addresses`           | POST        | JWT User  | Tambah alamat baru                      |
| Address  | `/api/profiles/me/addresses/{id}`      | PUT         | JWT User  | Update alamat spesifik                  |
| Address  | `/api/profiles/me/addresses/{id}`      | DELETE      | JWT User  | Hapus (soft-delete) alamat              |
| Address  | `/api/profiles/me/addresses/{id}/default` | PUT      | JWT User  | Set alamat menjadi *Default Shipping*   |
| Health   | `/health`                              | GET         | Anonim    | Liveness/Readiness probe                |

---

## Environment Variables

| Variable                               | Deskripsi                                                            | Wajib |
| -------------------------------------- | -------------------------------------------------------------------- | ----- |
| `ASPNETCORE_ENVIRONMENT`               | Status Environment (`Development`, `Production`, `Testing`)          | ✅    |
| `ConnectionStrings__DefaultConnection` | String koneksi PostgreSQL                                            | ✅    |
| `RabbitMQ__Host`                       | RabbitMQ server host                                                 | ✅    |
| `RabbitMQ__Port`                       | RabbitMQ connection port (default 5672)                              | ✅    |
| `RabbitMQ__Username`                   | RabbitMQ auth user                                                   | ✅    |
| `RabbitMQ__Password`                   | RabbitMQ auth password                                               | ✅    |
| `GATEWAY_HMAC_SECRET`                  | Kunci rahasia HMAC-SHA256 untuk memverifikasi identitas Reverse Proxy| ✅    |

> **Catatan Keamanan**: Akses client eksternal harus melalui SS-APIGateway. Apabila request HTTP dipanggil langsung dengan bypass Gateway, middleware `GatewaySignatureMiddleware` akan me-return *403 Forbidden*.

---

## Instalasi & Menjalankan

### Prasyarat

- .NET 10.0 SDK
- PostgreSQL instance (db: `ss_profile_db`)
- RabbitMQ instance

### Menjalankan Server Lokal

```bash
git clone <repository>
cd SamStore/SS-ProfileService

# Restore dependensi nuget
dotnet restore

# Run API secara lokal (Pastikan config connection diubah ke lokal)
dotnet run --project src/SS.ProfileService.API/SS.ProfileService.API.csproj
```

### Build

```bash
dotnet build
```

### Testing

Proyek pengujian (`test/SS.ProfileService.Tests`) dirancang untuk memutar container in-memory database atau test-server kustom.
```bash
dotnet test
```

---

## Integrasi Event Broker

**Consumer (InboxWorker)**:
Layanan melacak *user registrations* melalui antrian pesan RabbitMQ (exchange `samstore.events` rute `auth.user.registered`). Jika terdeteksi, layanan akan membuat baris *Profile* kosong yang siap digunakan pelanggan.

## Known Issues

Tidak ada issue yang teridentifikasi dari source code.
