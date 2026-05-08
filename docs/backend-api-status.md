# Backend API aktuális állapot

A rendszer fő backend funkciói elkészültek és több körben manuálisan tesztelve lettek Swaggerből és MySQL lekérdezésekkel.

## Technológiai alap

A backend API ASP.NET Core 8 Web API alapon készült. Az adatbázis MySQL 8, amely fejlesztési környezetben Docker konténerben fut. Az adatbázis-elérést EF Core és Pomelo MySQL provider biztosítja.

A jelenlegi backend főbb technológiai elemei:

- ASP.NET Core 8 Web API
- MySQL 8 adatbázis
- Docker alapú fejlesztői adatbázis-környezet
- EF Core + Pomelo MySQL provider
- JWT Bearer authentication
- Refresh token alapú session kezelés
- AES-256-GCM alapú credential titkosítás
- Hash-chain alapú audit naplózás
- FluentValidation alapú request validáció
- kriptográfiailag biztonságos jelszógenerátor endpoint
- Mock és LDAP alapú authentikációs stratégia
- Health endpoint backend és adatbázis elérhetőséghez

## Authentikáció

A rendszer authentikációs rétege Strategy jellegű megoldással készült. A közös interfész az `IAdAuthenticationService`, amely mögött több implementáció használható.

Jelenlegi implementációk:

- `MockAdAuthenticationService` fejlesztési és otthoni tesztelési környezethez
- `LdapAuthenticationService` éles Active Directory / LDAP integráció előkészítéséhez

A használt authentikációs provider konfigurációból választható:

```json
"Authentication": {
  "Provider": "Mock"
}
```

vagy:

```json
"Authentication": {
  "Provider": "Ldap"
}
```

A login folyamat során a rendszer ellenőrzi a felhasználó hitelesítési adatait, lekéri a csoporttagságokat, majd ezek alapján feloldja a belső alkalmazásszerepkört.

## Szerepkörök

A rendszer jelenlegi szerepkörei:

- `ITAdmin`
- `IT`
- `Consultant`
- `Support`

A szerepkörök AD/mock csoportnév alapján kerülnek feloldásra. A rendszer a legmagasabb jogosultsági szintű szerepkört választja ki.

Szabályok:

- `ITAdmin` teljes jogosultságú adminisztrátori szerepkör
- `IT` credentialt létrehozhat és a számára engedélyezett bejegyzéseket kezelheti
- `IT` csak olyan credentialt módosíthat vagy törölhet, amelyet aktuálisan lát és amelyhez megfelelő joga van
- `IT` csak ideiglenes, user-alapú hozzáférést adhat tovább
- `Consultant` és `Support` csak a számukra engedélyezett bejegyzéseket láthatják
- `Consultant` és `Support` nem hozhat létre credentialt és nem kezelhet hozzáférési szabályokat

## User szinkronizáció

Sikeres authentikáció után a backend szinkronizálja a felhasználót a `users` táblába.

A folyamat:

1. AD/mock authentikáció
2. csoporttagságok lekérése
3. belső role feloldása
4. felhasználó keresése `ad_username` alapján
5. új user létrehozása vagy meglévő frissítése
6. `role_id`, `last_login_at`, `role_synced_at` mezők frissítése

Ez biztosítja, hogy az alkalmazás saját adatbázisában is nyilvántartsa az AD-ből érkező felhasználókat.

## Token kezelés

A rendszer JWT access tokent és refresh tokent használ.

Endpointok:

- `POST /api/Auth/login`
- `POST /api/Auth/refresh`
- `POST /api/Auth/logout`

A login sikeres authentikáció után visszaad:

- access token
- refresh token
- token típus
- access token élettartam
- user információk
- role információk
- AD/mock csoportok

Az access token rövid életű, konfigurációból olvasott értékkel működik. A refresh token hosszabb életű session kezelésre szolgál.

A rendszerben egy felhasználóhoz egyszerre csak egy aktív refresh token tartozhat. Új login esetén a korábbi aktív refresh tokenek visszavonásra kerülnek.

Refresh működés:

1. a kliens elküldi a refresh tokent
2. a backend hash alapján megkeresi az aktív refresh tokent
3. ellenőrzi, hogy nem járt-e le és nincs-e visszavonva
4. új access tokent generál
5. új refresh tokent generál
6. a régi refresh tokent visszavonja
7. audit eseményt ír

Logout esetén a backend visszavonja az aktív refresh tokent.

## Refresh token biztonsági működés

A refresh token nem nyersen kerül tárolásra az adatbázisban, hanem SHA-256 hash formában.

A `refresh_tokens` tábla főbb mezői:

- `id`
- `user_id`
- `token_hash`
- `expires_at`
- `revoked_at`
- `replaced_by_token_hash`
- `created_by_ip`
- `revoked_by_ip`
- `created_at`

A token rotation miatt egy korábbi refresh token újrafelhasználása `401 Unauthorized` választ eredményez.

Egy felhasználóhoz egyszerre csak egy aktív refresh token tartozhat, ezért új login vagy új refresh token kiadása visszavonja a korábbi aktív tokent.

## Partnercégek kezelése

A partnercégek a credential bejegyzések csoportosítására szolgálnak. Egy credential mindig egy partnercéghez tartozik `company_id` alapján.

Endpointok:

- `GET /api/Companies`
- `GET /api/Companies/{id}`
- `POST /api/Companies`
- `PUT /api/Companies/{id}`
- `DELETE /api/Companies/{id}`

A törlés soft delete jellegű, vagyis a rekord fizikailag nem törlődik, hanem az `is_active` mező kerül hamis értékre.

A partnercégek később a kliensben KeePass-szerű fa nézetben jelennek meg.

## Credential kezelés

A credential bejegyzések az ügyfelekhez vagy partnercégekhez tartozó hozzáférési adatokat reprezentálják.

Endpointok:

- `GET /api/Credentials`
- `GET /api/Credentials/{id}`
- `POST /api/Credentials`
- `PUT /api/Credentials/{id}`
- `DELETE /api/Credentials/{id}`
- `POST /api/Credentials/{id}/reveal-username`
- `POST /api/Credentials/{id}/reveal-password`

A normál listázás és részletes lekérés nem ad vissza plaintext username vagy password értéket.

A plaintext adatok kizárólag külön reveal endpointon keresztül kérhetők le, jogosultságellenőrzés és auditálás után.

## Credential titkosítás

A credential username és password mezői AES-256-GCM algoritmussal kerülnek titkosításra.

Adatbázisban tárolt mezők:

- `encrypted_username`
- `username_iv`
- `username_tag`
- `encrypted_password`
- `password_iv`
- `password_tag`

Plaintext username és password nem kerül külön oszlopba.

A titkosítási logika az `IEncryptionService` interfészen keresztül érhető el. A jelenlegi implementáció az `AesGcmEncryptionService`.

A titkosításhoz használt master key konfigurációból érkezik:

```json
"Encryption": {
  "MasterKey": "CHANGE_ME_TO_BASE64_32_BYTE_KEY"
}
```

Éles környezetben ezt nem szabad közvetlenül az `appsettings.json` fájlban tárolni, hanem környezeti változóban, secret managerben vagy más biztonságos tárolóban kell kezelni.

## Credential hozzáféréskezelés

A credential hozzáférések a `credential_access` táblában vannak tárolva.

Fontos mezők:

- `credential_id`
- `role_id`
- `user_id`
- `can_view`
- `can_write`
- `can_delete`
- `expires_at`
- `created_by_user_id`
- `created_at`

A hozzáférési szabály lehet role-alapú vagy user-alapú.

Szabályok:

- `ITAdmin` minden credentialhez hozzáfér
- `ITAdmin` teljes hozzáféréskezelési jogosultsággal rendelkezik
- `IT` credentialt létrehozhat
- `IT` csak olyan credentialt módosíthat vagy törölhet, amelyet aktuálisan lát és amelyhez megfelelő joga van
- `IT` csak ideiglenes, user-alapú hozzáférést adhat tovább
- `IT` által adott hozzáférésnél az `expires_at` kötelező
- `IT` nem adhat korlátlan hozzáférést
- `Consultant` és `Support` nem kezelhet hozzáférési szabályokat

Credential access endpointok:

- `POST /api/CredentialAccess`
- `GET /api/CredentialAccess/credential/{credentialId}`
- `DELETE /api/CredentialAccess/{id}`

## Credential létrehozási jogosultság

Credential bejegyzést jelenleg az alábbi szerepkörök hozhatnak létre:

- `ITAdmin`
- `IT`

A `Consultant` és `Support` szerepkör nem hozhat létre credential bejegyzést.

Új credential létrehozásakor a rendszer automatikusan létrehoz egy alap hozzáférési szabályt az aktuális szerepkörhöz.

Például ha egy `IT` szerepkörű felhasználó hoz létre bejegyzést, akkor az `IT` role hozzáférést kap az adott credentialhez.

Az `ITAdmin` ezt később módosíthatja vagy visszavonhatja. Ha az `IT` szerepkör már nem látja a credentialt, akkor nem is tudja módosítani vagy törölni.

## Jelszógenerátor

A backend tartalmaz egy beépített jelszógenerátor funkciót, amely új credential létrehozásakor használható. A célja, hogy a felhasználó ne kézzel adjon meg gyenge vagy újrahasznált jelszót, hanem az alkalmazás tudjon erős, véletlenszerű jelszót előállítani.

Endpoint:

- `POST /api/PasswordGenerator/generate`

Az endpoint JWT authentikációt igényel, tehát csak bejelentkezett felhasználó hívhatja.

Request példa:

```json
{
  "length": 20,
  "includeUppercase": true,
  "includeLowercase": true,
  "includeDigits": true,
  "includeSpecialCharacters": true
}
```

Sikeres válasz példa:

```json
{
  "password": "A9x!fK2#mP7qLz@vT4sB",
  "length": 20,
  "includesUppercase": true,
  "includesLowercase": true,
  "includesDigits": true,
  "includesSpecialCharacters": true
}
```

A jelszógenerátor az alábbi opciókat támogatja:

- jelszóhossz megadása
- nagybetűk engedélyezése
- kisbetűk engedélyezése
- számjegyek engedélyezése
- speciális karakterek engedélyezése

A generálás `RandomNumberGenerator` használatával történik, ezért kriptográfiailag biztonságosabb, mint a hagyományos `Random` alapú megoldás. A generált jelszó minden bekapcsolt karakterkészletből legalább egy karaktert tartalmaz.

Validációs szabályok:

- a jelszó hossza minimum 8, maximum 128 karakter lehet
- legalább egy karakterkészletet engedélyezni kell

A WPF kliens később ezt az endpointot használhatja az új credential létrehozási felületen.

## Reveal endpointok

A rendszer külön endpointokat használ a titkosított adatok visszafejtésére:

- `POST /api/Credentials/{id}/reveal-username`
- `POST /api/Credentials/{id}/reveal-password`

Ezek működése:

1. jogosultság ellenőrzése
2. credential aktív állapotának ellenőrzése
3. titkosított mezők ellenőrzése
4. AES-GCM visszafejtés
5. `last_accessed_at` frissítése
6. audit esemény írása
7. plaintext érték visszaadása

Normál `GET` endpointokban a username és password nem jelenik meg.

Ez illeszkedik a későbbi WPF kliens működéséhez, ahol a jelszó alapból maszkolva jelenik meg, és csak felhasználói műveletre történik reveal.

## WPF klienshez kapcsolódó elvárt működés

A kliensben a credential lista KeePass-szerű módon fog működni.

Elvárt viselkedés:

- a jelszó alapból maszkolva jelenik meg
- például `********` vagy pontok formájában
- `Ctrl+H` hatására a jogosult felhasználó számára megjeleníthető a plaintext jelszó
- dupla kattintással a felhasználónév vagy jelszó másolható
- a plaintext adat lekérése csak API reveal endpointon keresztül történhet
- a reveal művelet backend oldalon auditálásra kerül
- a normál lista és részletes lekérés nem tartalmaz plaintext secretet

Így a kliens nem tárolja előre az összes jelszót, hanem csak akkor kéri le, amikor a felhasználó ténylegesen látni vagy másolni akarja.

## Audit log

Az audit napló hash-chain alapú.

A `audit_log` tábla minden rekordja tartalmazza az előző rekord hash értékét:

- `previous_hash`
- `hash`

Ez lehetővé teszi annak kimutatását, ha egy korábbi audit rekordot utólag módosítottak.

Auditált események például:

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

A hash-chain működését MySQL lekérdezésekkel és a `GET /api/Audit/verify-chain` endpointtal is ellenőriztük.

## Audit hash-chain validálás

A backend tartalmaz audit hash-chain ellenőrző funkciót.

Endpoint:

- `GET /api/Audit/verify-chain`

Csak `ITAdmin` szerepkörrel érhető el.

Sértetlen lánc esetén a válasz:

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

A backend tartalmaz health endpointot.

Endpoint:

- `GET /api/Health`

Ez JWT nélkül hívható. A WPF kliens később induláskor ellenőrizheti vele, hogy az API és az adatbázis elérhető-e.

Példa válasz:

```json
{
  "status": "Healthy",
  "api": "PasswordManagerSystem.Api",
  "database": "Available",
  "utcTime": "2026-05-08T15:00:00Z"
}
```

## FluentValidation

A rendszer FluentValidation alapú request validációt használ.

Jelenlegi validátorok:

- `LoginRequestValidator`
- `RefreshTokenRequestValidator`
- `LogoutRequestValidator`
- `CreateCompanyRequestValidator`
- `UpdateCompanyRequestValidator`
- `CreateCredentialRequestValidator`
- `UpdateCredentialRequestValidator`
- `CreateCredentialAccessRequestValidator`
- `GeneratePasswordRequestValidator`

A controllerekből a duplikált kézi validációk jelentős része eltávolításra került. A controllerekben főként az üzleti logika, jogosultságellenőrzés, adatbázis műveletek és auditálás maradt.

Tesztelt validációs hibák:

- üres login username/password
- üres refresh token
- hibás company create
- hibás credential create
- hibás credential access request

## LDAP előkészítés

A rendszer tartalmaz LDAP authentikációs előkészítést.

Főbb elemek:

- `LdapOptions`
- `LdapAuthenticationService`
- `System.DirectoryServices.Protocols`
- konfigurációból választható provider

LDAP konfigurációs példa:

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

A jelenlegi LDAP implementáció feladata:

1. LDAP kapcsolat létrehozása
2. felhasználói bind végrehajtása
3. user keresése `sAMAccountName` alapján
4. display name, email és group adatok lekérése
5. AD csoportnév kinyerése `memberOf` mezőből

Éles környezetben az LDAP konfigurációt konkrét AD adatokkal kell tesztelni.

## Konfiguráció

A valódi `appsettings.json` nem kerülhet verziókezelésbe, mert érzékeny adatokat tartalmazhat.

Például:

- adatbázis jelszó
- JWT secret
- AES master key
- LDAP szerveradatok

A repo-ban ezért `appsettings.example.json` található, amely mintaértékeket tartalmaz.

## Jelenlegi tesztelt működés

A következő működések tesztelve lettek:

- Docker MySQL indulása
- adatbázis séma megléte
- role-ok megléte
- login működés Mock AD-val
- user szinkronizáció
- JWT token kiadás
- JWT protected endpoint
- Swagger Authorize működés
- Companies CRUD
- Credentials CRUD
- AES-GCM titkosítás
- reveal-username
- reveal-password
- audit log írás
- hash-chain működés
- audit hash-chain validáló endpoint
- health endpoint
- credential access rule létrehozás, listázás és törlés
- ITAdmin és IT credential létrehozási jogosultság
- Consultant és Support tiltása credential létrehozásnál
- refresh token kiadás
- refresh token rotation
- régi refresh token újrahasználásának tiltása
- logout utáni refresh token tiltása
- egy felhasználóhoz csak egy aktív refresh token engedélyezése
- FluentValidation validációs hibák
- jelszógenerátor endpoint
- jelszógenerátor validáció
- kriptográfiailag biztonságos jelszógenerálás