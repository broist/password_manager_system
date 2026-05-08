# Backend API aktuális állapot

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
- Mock és LDAP alapú authentikációs stratégia

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

"Authentication": {
  "Provider": "Ldap"
}
