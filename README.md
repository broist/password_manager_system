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
- Refresh token alapú session kezelés
- AES-256-GCM titkosítás
- Hash-chain audit log
- FluentValidation alapú request validáció
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
│       │   ├── Services
│       │   └── Validators
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
```

## Konfiguráció

A valódi `appsettings.json` nem kerülhet GitHubra, mert érzékeny adatokat tartalmazhat.

Lokális futtatáshoz a mintaállomány alapján kell saját konfigurációt készíteni:

```text
Backend_api/PasswordManagerSystem.Api/appsettings.example.json
Backend_api/PasswordManagerSystem.Api/appsettings.json
```

Fontos konfigurációs blokkok:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=password_manager;user=pm_user;password=CHANGE_ME;"
  },

  "Authentication": {
    "Provider": "Mock"
  },

  "Ldap": {
    "Server": "ad.example.local",
    "Port": 389,
    "UseSsl": false,
    "Domain": "DOMAIN",
    "BaseDn": "DC=example,DC=local",
    "UserSearchFilter": "(&(objectClass=user)(sAMAccountName={0}))",
    "GroupAttribute": "memberOf"
  },

  "Jwt": {
    "Issuer": "PasswordManagerSystem.Api",
    "Audience": "PasswordManagerSystem.Client",
    "Secret": "CHANGE_ME_TO_LONG_RANDOM_SECRET",
    "AccessTokenMinutes": 60,
    "RefreshTokenDays": 1
  },

  "Encryption": {
    "MasterKey": "CHANGE_ME_TO_BASE64_32_BYTE_KEY"
  }
}
```

## JWT secret generálása

PowerShell:

```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }))
```

Ezt kell beállítani:

```json
"Secret": "GENERATED_BASE64_SECRET"
```

## AES master key generálása

PowerShell:

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

Ezt kell beállítani:

```json
"MasterKey": "GENERATED_BASE64_32_BYTE_KEY"
```

Az AES kulcsnak 32 bájtosnak kell lennie, mert a rendszer AES-256-GCM titkosítást használ.

## Backend indítása

Projektmappa:

```cmd
cd C:\Szakdolgozat\PasswordManagerSystem\Backend_api\PasswordManagerSystem.Api
```

Build:

```cmd
dotnet build
```

Indítás:

```cmd
dotnet run
```

Elvárt kimenet:

```text
Now listening on: http://localhost:5174
```

Swagger:

```text
http://localhost:5174/swagger
```

Health endpoint:

```text
GET /api/Health
```

Elvárt válasz:

```json
{
  "status": "Healthy",
  "api": "PasswordManagerSystem.Api",
  "database": "Available",
  "utcTime": "2026-..."
}
```

## Auth endpointok

```text
POST /api/Auth/login
POST /api/Auth/refresh
POST /api/Auth/logout
```

Login test body fejlesztői Mock providerrel:

```json
{
  "username": "DOMAIN\\admin",
  "password": "Test1234!"
}
```

Sikeres válasz tartalmazza:

- `accessToken`
- `refreshToken`
- `tokenType`
- `expiresInMinutes`
- `user`
- `role`
- `groups`

## Swagger Authorize használata

1. `POST /api/Auth/login`
2. válaszból `accessToken` kimásolása
3. Swagger jobb felső sarok: `Authorize`
4. csak a tokent kell beilleszteni
5. `Bearer` előtagot nem kell kézzel beírni

## Szerepkörök

Jelenlegi szerepkörök:

- `ITAdmin`
- `IT`
- `Consultant`
- `Support`

Fő szabályok:

- `ITAdmin` teljes hozzáféréssel rendelkezik
- `IT` credentialt létrehozhat
- `IT` csak olyan credentialt módosíthat vagy törölhet, amelyet lát és amelyhez joga van
- `IT` csak ideiglenes, user-alapú hozzáférést adhat tovább
- `Consultant` és `Support` nem hozhat létre credentialt
- `Consultant` és `Support` nem kezelhet hozzáférési szabályokat

## Companies endpointok

```text
GET    /api/Companies
GET    /api/Companies/{id}
POST   /api/Companies
PUT    /api/Companies/{id}
DELETE /api/Companies/{id}
```

Create body:

```json
{
  "name": "Aktív Teszt Partner Kft.",
  "description": "Credential teszteléshez"
}
```

A törlés soft delete jellegű, az `is_active` mező áll hamisra.

## Credentials endpointok

```text
GET    /api/Credentials
GET    /api/Credentials/{id}
POST   /api/Credentials
PUT    /api/Credentials/{id}
DELETE /api/Credentials/{id}
POST   /api/Credentials/{id}/reveal-username
POST   /api/Credentials/{id}/reveal-password
```

Credential create body:

```json
{
  "companyId": 2,
  "title": "RDP - APP01 admin",
  "username": "appadmin",
  "password": "AppSecret123!",
  "connectionValue": "rdp://app01.aktiv-teszt.local",
  "notes": "Teszt credential bejegyzés"
}
```

A normál listázás és részletes lekérés nem ad vissza plaintext username vagy password mezőt.

A plaintext érték csak külön reveal endpointon keresztül kérhető le.

## Credential access endpointok

```text
POST   /api/CredentialAccess
GET    /api/CredentialAccess/credential/{credentialId}
DELETE /api/CredentialAccess/{id}
```

ITAdmin role-alapú access példa:

```json
{
  "credentialId": 5,
  "roleId": 2,
  "userId": null,
  "canView": true,
  "canWrite": true,
  "canDelete": false,
  "expiresAt": null
}
```

IT által adható ideiglenes user-hozzáférés példa:

```json
{
  "credentialId": 5,
  "roleId": null,
  "userId": 3,
  "canView": true,
  "canWrite": false,
  "canDelete": false,
  "expiresAt": "2026-05-09T18:00:00Z"
}
```

IT szerepkör esetén az `expiresAt` kötelező.

## Audit endpoint

```text
GET /api/Audit/verify-chain
```

Csak `ITAdmin` role-lal érhető el.

Elvárt válasz sértetlen lánc esetén:

```json
{
  "isValid": true,
  "checkedRecords": 12,
  "brokenAtAuditLogId": null,
  "expectedPreviousHash": null,
  "actualPreviousHash": null,
  "expectedHash": null,
  "actualHash": null,
  "message": "Audit hash-chain is valid."
}
```

## Health endpoint

```text
GET /api/Health
```

JWT nélkül hívható. A WPF kliens induláskor használhatja backend- és adatbázis-elérhetőség ellenőrzésre.

## FluentValidation

A request DTO-k validációja FluentValidation alapon működik.

Jelenlegi validátorok:

- `LoginRequestValidator`
- `RefreshTokenRequestValidator`
- `LogoutRequestValidator`
- `CreateCompanyRequestValidator`
- `UpdateCompanyRequestValidator`
- `CreateCredentialRequestValidator`
- `UpdateCredentialRequestValidator`
- `CreateCredentialAccessRequestValidator`

A controllerekből a duplikált kézi validációk jelentős része eltávolításra került. A controllerek főként üzleti logikát, jogosultságellenőrzést, adatbázis műveleteket és auditálást tartalmaznak.

## Auditált események

Példák:

- `LOGIN_SUCCESS`
- `LOGIN_FAILED`
- `ROLE_SYNCED`
- `TOKEN_REFRESHED`
- `LOGOUT`
- `COMPANY_CREATED`
- `COMPANY_UPDATED`
- `COMPANY_DEACTIVATED`
- `CREDENTIAL_CREATED`
- `CREDENTIAL_UPDATED`
- `CREDENTIAL_DEACTIVATED`
- `CREDENTIAL_USERNAME_REVEALED`
- `CREDENTIAL_PASSWORD_REVEALED`
- `CREDENTIAL_ACCESS_GRANTED`
- `CREDENTIAL_ACCESS_UPDATED`
- `CREDENTIAL_ACCESS_REVOKED`

## Tesztelt fő működések

A jelenlegi backendben tesztelve lett:

- Mock login
- JWT token kiadás
- Swagger Authorize
- Companies CRUD
- Credentials CRUD
- AES-GCM credential titkosítás
- reveal username/password endpoint
- credential access szabály létrehozás, listázás, törlés
- ITAdmin és IT credential létrehozás
- Consultant és Support tiltása credential létrehozásnál
- refresh token kiadás
- refresh token rotation
- logout refresh token revoke
- userenként egy aktív refresh token
- audit hash-chain validálás
- health endpoint
- FluentValidation alapú validációs hibák