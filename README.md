# Password Manager System / PAM Lite

## Projekt célja

A projekt célja egy vállalati környezetben használható, központi jelszókezelő és PAM Lite rendszer megvalósítása.

A rendszer fő céljai:

- partnercégekhez tartozó credential bejegyzések központi kezelése
- Active Directory / LDAP alapú authentikáció előkészítése
- szerepkör alapú jogosultságkezelés
- credential username/password mezők titkosított tárolása
- auditálható jelszó-megtekintés
- hash-chain alapú audit naplózás
- refresh token alapú session kezelés
- WPF kliens kiszolgálása REST API-n keresztül

## Technológiai stack

Backend:

- ASP.NET Core 8 Web API
- C#
- EF Core
- Pomelo.EntityFrameworkCore.MySql
- MySQL 8
- JWT Bearer authentication
- AES-256-GCM titkosítás
- System.DirectoryServices.Protocols LDAP előkészítés

Adatbázis:

- MySQL 8
- Docker alapú fejlesztői környezet

## Projektstruktúra

```text
PasswordManagerSystem
├── Backend_api
│   └── PasswordManagerSystem.Api
│       ├── Application
│       │   ├── DTOs
│       │   ├── Interfaces
│       │   └── Services
│       ├── Controllers
│       ├── Domain
│       │   └── Entities
│       ├── Infrastructure
│       │   ├── Authentication
│       │   ├── Data
│       │   └── Security
│       ├── appsettings.example.json
│       └── PasswordManagerSystem.Api.csproj
├── docker
└── docs