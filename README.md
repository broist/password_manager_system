# Password Manager System – PAM Lite

Vállalati jelszókezelő és könnyített PAM (Privileged Access Management) rendszer
prototípusa szakdolgozati projekt keretében. A rendszer a fogyasztói
jelszókezelők egyszerűsége és a nagyvállalati PAM-megoldások komplexitása
között helyezkedik el: kisebb IT-csapatok számára kínál központi,
auditálható, szerepkör-alapú hitelesítő adat- és RDP-hozzáférés-kezelést.

**Verzió:** v0.1.0 (prototípus)
**Szerző:** Biró István
**Téma:** Szakdolgozat — jelszókezelő és PAM Lite rendszer tervezése és megvalósítása

---

## Fő funkciók

- **Partnercég-szintű hitelesítő adatkezelés** — credentialok cégekhez rendelve, soft delete támogatással
- **Credential CRUD** AES-256-GCM titkosítással (username + password mezők at-rest titkosítva)
- **Külön reveal endpoint** plaintext felhasználónév és jelszó megtekintésére, minden lekérdezés auditálva
- **Szerepkör- és felhasználó-alapú hozzáférés-vezérlés** (RBAC + ideiglenes user-szintű grantok lejárati idővel)
- **JWT alapú authentikáció** refresh token rotációval és single-active-refresh-token policyval
- **Hash-chain alapú audit napló** sértetlenség-ellenőrzéssel (`/api/Audit/verify-chain`)
- **Active Directory / LDAP integráció** előkészítve (Mock provider fejlesztéshez, LDAP éles használathoz)
- **Kriptográfiailag biztonságos jelszógenerátor** konfigurálható karakterkészletekkel
- **WPF kliens** MVVM mintára építve, DPAPI-titkosítással védett tokentárral
- **Egy-kattintásos RDP indítás** a Windows beépített `mstsc.exe` kliensével
- **Vágólap auto-clear** (alapból 12 másodperc) a vágólap-szivárgás kockázatának csökkentésére
- **Auto-mask** plaintext credentialokra reveal után (alapból 30 másodperc)
- **Credential usage session tracking** — ki, mikor, milyen credentialhoz fért hozzá
- **Health endpoint** backend és adatbázis-elérhetőség ellenőrzéshez

---

## Architektúra

Háromrétegű architektúra:

```
┌────────────────────────┐
│  WPF kliens (Windows)  │   .NET 8 WPF, MVVM, DPAPI tokentár
└──────────┬─────────────┘
           │  HTTPS + JWT Bearer
           ▼
┌────────────────────────┐
│   ASP.NET Core 8 API   │   JWT auth, FluentValidation, AES-GCM
└──────────┬─────────────┘
           │  EF Core (Pomelo MySQL provider)
           ▼
┌────────────────────────┐
│      MySQL 8           │   Docker konténerben (fejlesztői env)
└────────────────────────┘
```

---

## Technológiai stack

### Backend (`Backend_api/PasswordManagerSystem.Api`)

- ASP.NET Core 8 Web API + C#
- EF Core 8 + Pomelo.EntityFrameworkCore.MySql
- MySQL 8 (Docker konténer fejlesztéshez)
- JWT Bearer authentication
- AES-256-GCM titkosítás (`AesGcmEncryptionService`)
- FluentValidation alapú request validáció
- Swashbuckle (Swagger UI)
- System.DirectoryServices.Protocols (LDAP)

### Kliens (`PasswordManagerSystem.Client`)

- .NET 8 WPF (Windows)
- MVVM minta + Clean Architecture
- CommunityToolkit.Mvvm (ObservableObject, RelayCommand)
- Microsoft.Extensions.Hosting (DI + Configuration + Logging)
- HttpClient + DelegatingHandler-ek (Bearer token + 401-en auto refresh)
- DPAPI (`System.Security.Cryptography.ProtectedData`) a refresh token at-rest védelméhez

### Infrastruktúra

- Docker / Docker Compose (MySQL fejlesztői konténerhez)
- Dockerfile a backendhez

---

## Projektstruktúra

```text
PasswordManagerSystem/
├── Backend_api/
│   └── PasswordManagerSystem.Api/
│       ├── Application/
│       │   ├── DTOs/                 # request/response DTO-k
│       │   ├── Interfaces/           # service kontraktok
│       │   ├── Services/             # üzleti logika
│       │   │   ├── AuditService.cs
│       │   │   ├── AuditChainVerifierService.cs
│       │   │   ├── CredentialAccessService.cs
│       │   │   ├── PasswordGeneratorService.cs
│       │   │   ├── RefreshTokenService.cs
│       │   │   ├── RoleResolverService.cs
│       │   │   └── UserSyncService.cs
│       │   └── Validators/           # FluentValidation szabályok
│       ├── Controllers/              # Auth, Companies, Credentials,
│       │                             # CredentialAccess, CredentialUsage,
│       │                             # Audit, Health, PasswordGenerator,
│       │                             # Users
│       ├── Domain/
│       │   ├── Entities/             # User, Role, Company, Credential,
│       │   │                         # CredentialAccess, AuditLog,
│       │   │                         # RefreshToken, CredentialUsageSession
│       │   └── Enums/
│       ├── Infrastructure/
│       │   ├── Authentication/       # MockAd + LDAP implementációk
│       │   ├── Data/                 # AppDbContext, EF konfigurációk
│       │   ├── Logging/
│       │   └── Security/             # JwtTokenService, AesGcmEncryptionService
│       ├── appsettings.example.json
│       ├── Dockerfile
│       └── Program.cs
│
├── PasswordManagerSystem.Client/
│   ├── App.xaml / App.xaml.cs        # DI bootstrap
│   ├── appsettings.json              # API URL, session beállítások
│   ├── Configuration/                # strongly-typed config
│   ├── Models/                       # DTO-k a backend kontraktokhoz
│   ├── Services/
│   │   ├── Api/                      # HttpClient wrapper + feature service-ek
│   │   ├── Auth/                     # AuthenticationService, TokenStore (DPAPI)
│   │   ├── Session/                  # aktuális user/role nyilvántartás
│   │   ├── Clipboard/                # auto-clear vágólap
│   │   ├── Notifications/            # toast service
│   │   ├── Navigation/
│   │   └── Dialogs/
│   ├── ViewModels/                   # Login, Shell, Companies, Credentials,
│   │                                 # Access, Audit, Settings, Common
│   ├── Views/                        # XAML ablakok és UserControl-ok
│   ├── Resources/                    # Themes (Colors, Typography, Icons), Styles
│   ├── Converters/
│   ├── Behaviors/                    # PasswordBoxBindingBehavior
│   └── INSTALL.md
│
├── docker/
│   ├── docker-compose.yml            # MySQL 8 konténer
│   └── mysql/
│       ├── init.sql                  # séma + roles seed
│       └── *.sql                     # tábla definíciók
│
├── docs/
│   └── backend-api-status.md         # részletes backend dokumentáció
│
├── PasswordManagerSystem.Client.sln
└── README.md
```

---

## Előfeltételek

- **.NET 8 SDK** (backend és kliens build-hez)
- **Visual Studio 2022** (Community kiadás is jó) — `.NET desktop development` és `ASP.NET and web development` workload-okkal
- **Docker Desktop** (MySQL konténerhez)
- **Windows 10/11** (a WPF kliens Windows-specifikus)
- Opcionálisan: MySQL Workbench vagy DBeaver az adatbázis vizsgálatához

---

## Telepítés és futtatás

### 1. Repó klónozása

```powershell
git clone https://github.com/broist/password_manager_system.git
cd password_manager_system
```

### 2. MySQL konténer indítása

```powershell
cd docker
docker compose up -d
```

Ez elindítja a `pms-mysql` konténert a `3306` porton, és lefuttatja az
`init.sql`-t, ami létrehozza a `password_manager` adatbázist, a táblákat,
és felveszi a négy alapértelmezett szerepkört.

Ellenőrzés:

```powershell
docker ps
```

A `pms-mysql` konténernek futnia kell.

### 3. Backend konfiguráció

A `Backend_api/PasswordManagerSystem.Api/appsettings.json` nem kerül
verziókövetésbe (érzékeny adatok miatt). Az `appsettings.example.json`
alapján kell létrehozni:

```powershell
cd Backend_api\PasswordManagerSystem.Api
copy appsettings.example.json appsettings.json
```

Majd `appsettings.json` szerkesztése:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=password_manager;user=pmsuser;password=pmsPassword123;"
  },
  "Authentication": {
    "Provider": "Mock"
  },
  "Jwt": {
    "Issuer": "PasswordManagerSystem.Api",
    "Audience": "PasswordManagerSystem.Client",
    "Secret": "<GENERÁLT_BASE64_64_BÁJTOS_KULCS>",
    "AccessTokenMinutes": 60,
    "RefreshTokenDays": 1
  },
  "Encryption": {
    "MasterKey": "<GENERÁLT_BASE64_32_BÁJTOS_KULCS>"
  }
}
```

#### JWT secret generálása (PowerShell)

```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }))
```

#### AES-256 master key generálása (PowerShell)

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

> Az AES kulcsnak **pontosan 32 bájtnak** kell lennie, mert a rendszer
> AES-256-GCM titkosítást használ. A base64-kódolt sztring 44 karakter hosszú lesz.

### 4. Backend indítása

```powershell
cd Backend_api\PasswordManagerSystem.Api
dotnet build
dotnet run
```

Várható kimenet:

```
Now listening on: http://localhost:5174
Now listening on: https://localhost:7050
```

Swagger UI: `https://localhost:7050/swagger`
Health: `GET /api/Health`

### 5. Kliens konfiguráció

A `PasswordManagerSystem.Client/appsettings.json` alapból a backend
lokális URL-jét használja:

```json
{
  "Api": {
    "BaseUrl": "https://localhost:7050/",
    "Timeout": "00:00:30",
    "AcceptInvalidCertificates": true
  },
  "Session": {
    "AutoRefreshLeadTimeSeconds": 60,
    "ClipboardClearSeconds": 12,
    "PasswordRevealAutoMaskSeconds": 30
  }
}
```

> **Megjegyzés:** az `AcceptInvalidCertificates: true` csak fejlesztéshez van!
> Éles környezetben ezt `false`-ra kell állítani és érvényes tanúsítványt használni.

Ha a backend még nem rendelkezik dev tanúsítvánnyal:

```powershell
dotnet dev-certs https --trust
```

### 6. Kliens indítása

Visual Studio-ban:

1. Nyisd meg a `PasswordManagerSystem.Client.sln`-t
2. NuGet csomagok automatikusan visszaállnak
3. `PasswordManagerSystem.Client` legyen a Startup Project
4. **F5**

Parancssorból:

```powershell
cd PasswordManagerSystem.Client
dotnet run
```

---

## Hitelesítés

A rendszer Strategy mintára építve támogat több authentikációs providert.
A `Authentication:Provider` konfigurációs érték alapján választódik ki:

- `"Provider": "Mock"` — fejlesztéshez, `MockAdAuthenticationService`
- `"Provider": "Ldap"` — éles AD/LDAP integráció, `LdapAuthenticationService`

### Mock authentikáció — teszt felhasználók

A `MockAdAuthenticationService` a felhasználónévben keresett kulcsszó
alapján rendeli hozzá a szerepkört.

| Példa felhasználónév | Felismert szó | Szerepkör |
|----------------------|---------------|-----------|
| `pista.admin` | `admin` | ITAdmin |
| `nora.it` | `it` | IT |
| `andras.consultant` | `consultant` | Consultant |
| `eva.support` | `support` | Support |

**Jelszó mindegyikhez:** `Test1234!`

A felhasználónévben csak tartalmaznia kell a kulcsszót — a vizsgálati
sorrend `admin` → `it` → `consultant` → `support`.

### LDAP konfiguráció (éles AD-hez)

```json
"Ldap": {
  "Server": "ad.example.local",
  "Port": 389,
  "UseSsl": false,
  "Domain": "DOMAIN",
  "BaseDn": "DC=example,DC=local",
  "UserSearchFilter": "(&(objectClass=user)(sAMAccountName={0}))",
  "GroupAttribute": "memberOf"
}
```

---

## Szerepkörök és jogosultságok

A rendszerben négy szerepkör van, AD-csoportnévhez kötve:

| Szerepkör | AD csoport | Szint | Leírás |
|-----------|------------|-------|--------|
| `ITAdmin` | `erp_kp_itadm` | 100 | Teljes hozzáférés minden funkcióhoz és minden credentialhoz |
| `IT` | `erp_kp_it` | 75 | Credential létrehozás, az engedélyezett bejegyzések kezelése, ideiglenes user-grant adása |
| `Consultant` | `erp_kp_erpconsultant` | 50 | Csak az engedélyezett bejegyzésekhez fér hozzá (olvasás) |
| `Support` | `erp_kp_erpsupport` | 25 | Korlátozott hozzáférés, csak kifejezetten megosztott bejegyzésekhez |

### Fő szabályok

- `ITAdmin` minden jogosultsággal rendelkezik
- `IT` létrehozhat credentialt; csak azt módosíthatja vagy törölheti, amelyhez joga van
- `IT` csak **user-szintű, ideiglenes** (lejárati idős) hozzáférést adhat — role-alapút nem
- `Consultant` és `Support` nem hozhat létre credentialt, és nem kezelhet hozzáférési szabályokat

### Hozzáférési modell

A `credential_access` táblán két típusú grant lehet:

1. **Role-alapú grant** — `roleId` kitöltve, `userId` null. Csak `ITAdmin` adhat.
2. **User-szintű grant** — `userId` kitöltve, `roleId` null. `IT` is adhat, de `expiresAt` kötelező.

Mind a kettőn finomszemcsés flagek: `canView`, `canWrite`, `canDelete`.

---

## REST API endpointok

### Auth

```
POST   /api/Auth/login
POST   /api/Auth/refresh
POST   /api/Auth/logout
```

A login válasz: `accessToken`, `refreshToken`, `tokenType`,
`expiresInMinutes`, `user`, `role`, `groups`.

### Companies

```
GET    /api/Companies
GET    /api/Companies/{id}
POST   /api/Companies
PUT    /api/Companies/{id}
DELETE /api/Companies/{id}
```

A törlés **soft delete**: az `is_active` flag áll hamisra, a kapcsolt
credentialok láthatatlanná válnak.

### Credentials

```
GET    /api/Credentials
GET    /api/Credentials/{id}
POST   /api/Credentials
PUT    /api/Credentials/{id}
DELETE /api/Credentials/{id}
POST   /api/Credentials/{id}/reveal-username
POST   /api/Credentials/{id}/reveal-password
```

A listázás és a részletes lekérés **nem ad vissza plaintext** felhasználónevet
vagy jelszót. Csak a két `reveal-*` endpoint dekriptál, és minden ilyen hívást
külön auditál a rendszer.

### CredentialAccess

```
POST   /api/CredentialAccess
GET    /api/CredentialAccess/credential/{credentialId}
DELETE /api/CredentialAccess/{id}
```

### CredentialUsage

```
POST   /api/CredentialUsage/start
POST   /api/CredentialUsage/{sessionId}/end
GET    /api/CredentialUsage
```

A kliens minden RDP/connection indításnál session-t nyit, amit a felhasználó
befejezésekor zár — így nyomon követhető, hogy ki, mikor, melyik credentialt
használt aktívan.

### Audit

```
GET    /api/Audit/verify-chain
```

Csak `ITAdmin`-nak elérhető. Visszaadja, hogy a hash-chain sértetlen-e,
és ha nem, melyik audit log bejegyzésnél tört meg.

### Password generator

```
POST   /api/PasswordGenerator/generate
```

Konfigurálható: hossz (8–128), nagybetű, kisbetű, szám, speciális karakter.
Kriptográfiailag biztonságos `RandomNumberGenerator`-t használ, és minden
bekapcsolt karakterkészletből garantáltan kerül legalább egy karakter
a generált jelszóba.

### Health

```
GET    /api/Health
```

JWT nélkül hívható. A kliens induláskor backend- és adatbázis-elérhetőség
ellenőrzésre használja.

---

## Auditált események

A `audit_log` táblába hash-chainnel írt események:

```
LOGIN_SUCCESS                LOGIN_FAILED                ROLE_SYNCED
TOKEN_REFRESHED              LOGOUT
COMPANY_CREATED              COMPANY_UPDATED             COMPANY_DEACTIVATED
CREDENTIAL_CREATED           CREDENTIAL_UPDATED          CREDENTIAL_DEACTIVATED
CREDENTIAL_USERNAME_REVEALED CREDENTIAL_PASSWORD_REVEALED
CREDENTIAL_ACCESS_GRANTED    CREDENTIAL_ACCESS_UPDATED   CREDENTIAL_ACCESS_REVOKED
CREDENTIAL_USAGE_STARTED     CREDENTIAL_USAGE_ENDED
```

Minden bejegyzés tartalmazza az előző sor hash-ét és a saját hash-ét, így
a `/api/Audit/verify-chain` egyszerű iterációval ki tudja mutatni a lánc
sértetlenségét vagy a törést.

---

## Biztonsági jellemzők

### Backend oldalon

- **AES-256-GCM** a credential username/password mezőkre (authenticated encryption)
- **JWT Bearer** access token, rövid élettartammal (alapból 60 perc)
- **Refresh token rotáció** — minden refreshnél új tokent állít ki, a régi azonnal érvénytelenné válik
- **Single active refresh token per user** — új login esetén a korábbi aktív refresh token revoke-olódik
- **Hash-chain audit log** sértetlenség-ellenőrzéssel
- **FluentValidation** minden bejövő DTO-ra (méret, formátum, kötelező mezők)
- **HTTPS redirect** beállítva

### Kliens oldalon

- A **refresh token DPAPI CurrentUser scope-pal** titkosítva tárolódik:
  `%LOCALAPPDATA%\PasswordManagerSystem\Client\session.bin`. Csak az
  aktuális Windows felhasználó tudja visszafejteni, ugyanazon a gépen.
- Az **access tokent nem perzisztáljuk**; minden indítás új sessiont kezd a refresh tokenből.
- Plaintext felhasználónév/jelszó **csak** a reveal endpointokon érkezik, és csak akkor,
  ha a user kifejezetten kéri — minden ilyen kérés auditálva van a backenden.
- **Vágólap auto-clear** — csak akkor töröl, ha még a mi értékünk van benne
  (a user azóta nem másolt mást).
- **Auto-mask** plaintext credentialokra reveal után, alapból 30 másodperc múlva.

---

## Kliens billentyűkombinációk

A credential nézetben:

| Gomb | Funkció |
|------|---------|
| `Ctrl+H` | Jelszó feltárása |
| `Ctrl+C` | Jelszó vágólapra (auto-clear 12 mp után) |
| `Ctrl+B` | Felhasználónév vágólapra |
| `Ctrl+U` | Kapcsolódás indítása (URL, RDP, UNC) |

---

## Swagger használata

1. `https://localhost:7050/swagger`
2. `POST /api/Auth/login` — mock providerrel pl.:
   ```json
   { "username": "pista.admin", "password": "Test1234!" }
   ```
3. A válaszból másold ki az `accessToken` értéket
4. Jobb felső sarokban **Authorize** gomb
5. Csak a tokent illeszd be — a `Bearer` előtag automatikus

---

## Fejlesztői megjegyzések

- A teljes kódbázis **MVVM**-re épül a kliens oldalon — minden logika
  ViewModel-ben legyen, ne code-behind-ban
- Új feature service: `Services/Api/` alá és regisztráld az `App.xaml.cs`
  `ConfigureServices` metódusában
- Új nézet: ViewModel a `ViewModels/` alá, UserControl a `Views/` alá,
  DataTemplate a `ShellWindow.xaml` `Window.Resources` szekciójába
- Új ikon: `Resources/Themes/Icons.xaml`-be Path data, használat
  `<Path Data="{StaticResource Icon.UjIkon}" .../>`-vel
- A backend Clean Architecture-szerű rétegezést követ:
  `Domain` → `Application` → `Infrastructure` + `Controllers`

---

## Továbbfejlesztési irányok

A szakdolgozat 10. fejezete részletesebben:

- Multi-faktor authentikáció (MFA / TOTP)
- Munkamenet-rögzítés (session recording RDP/SSH-hoz)
- SSH és VNC kapcsolódás-támogatás
- Automatizált jelszórotáció
- SIEM-integráció (Syslog / CEF export)
- Cross-platform kliens (MAUI vagy Avalonia)
- Offline mód lokális cache-sel
- Jóváhagyási munkafolyamatok (4-eyes principle)

---

## Licenc és felhasználás

Szakdolgozati projekt, nyilvános demonstrációs céllal.
Éles, kritikus rendszerekben való felhasználás előtt biztonsági audit szükséges.
