# Password Manager System – WPF kliens

Profi WPF kliens a `PasswordManagerSystem.Api` backendhez.

## Architektúra

- **Pattern**: MVVM + Clean Architecture + Dependency Injection
- **Tech stack**:
  - .NET 8 WPF
  - CommunityToolkit.Mvvm (ObservableObject, RelayCommand)
  - Microsoft.Extensions.Hosting (DI + Configuration + Logging)
  - HttpClient + DelegatingHandler-ek (Bearer token, 401-en auto refresh)
  - DPAPI (Windows beépített titkosítás) a token at-rest tárolásához

### Mappastruktúra

```
PasswordManagerSystem.Client/
├── App.xaml / App.xaml.cs                 # DI bootstrap, ablak választás
├── appsettings.json                       # API URL, session beállítások
├── Configuration/                         # Strongly-typed config
├── Models/                                # DTO-k a backend kontraktokhoz
├── Services/
│   ├── Api/                               # HttpClient wrapper + feature service-ek
│   ├── Auth/                              # AuthenticationService, TokenStore (DPAPI), AuthHeaderHandler
│   ├── Session/                           # Aktuális user / role nyilvántartás
│   ├── Clipboard/                         # Auto-clear vágólap kezelés
│   ├── Notifications/                     # Toast service
│   └── Dialogs/                           # MessageBox absztrakció
├── ViewModels/                            # MVVM ViewModelek
├── Views/                                 # XAML ablakok és UserControl-ok
├── Resources/
│   ├── Themes/                            # Colors, Typography, Icons
│   └── Styles/                            # Button, Input, ListBox, Window
├── Converters/                            # IValueConverter implementációk
└── Behaviors/                             # PasswordBoxBindingBehavior
```

## Indítás

### Előfeltételek

- Visual Studio 2022 (Community is jó) – `.NET desktop development` workload
- Backend API fut (`https://localhost:7050` vagy `http://localhost:5174`)
- MySQL Docker konténer fut

### Futás

1. Nyisd meg a `PasswordManagerSystem.Client.sln`-t Visual Studio-ban
2. NuGet csomagok automatikusan letöltődnek
3. Beállítások az `appsettings.json`-ben:
   ```json
   "Api": {
     "BaseUrl": "https://localhost:7050/",
     "AcceptInvalidCertificates": true
   }
   ```
4. F5 – fut.

### Mock AD bejelentkezés (fejlesztés)

A backend `MockAdAuthenticationService`-t használ alapból. Belépéshez:

- Felhasználónév: tartalmazzon `admin`/`it`/`consultant`/`support` szót (pl. `pista.admin`)
- Jelszó: `Test1234!`

Szerepkör automatikusan a felhasználónév alapján:
- `*admin*` → ITAdmin
- `*it*` → IT
- `*consultant*` → Consultant
- `*support*` → Support

## Implementált funkciók (v0.1.0)

- ✅ Login képernyő modern split-screen designnal
- ✅ JWT + refresh token tárolás DPAPI titkosítással
- ✅ Automata refresh 401-en (DelegatingHandler-ben)
- ✅ Shell ablak custom titlebar-ral
- ✅ Sidebar navigáció szerepkör-szűréssel
- ✅ Cégek lista (bal oldalt)
- ✅ Bejegyzések lista (középen, kapcsolódás-típus ikonokkal)
- ✅ Részletek panel (jobbra) maszkolt user/jelszóval
- ✅ Reveal username/password (Eye gomb, Ctrl+H, Ctrl+B)
- ✅ Auto-clear vágólap (12 mp után)
- ✅ Auto-mask jelszó (30 mp után reveal után)
- ✅ Kapcsolódás megnyitása (URL, RDP, UNC) – Ctrl+U
- ✅ Toast értesítések
- ✅ Logout megerősítéssel

## Tervezett funkciók (következő iteráció)

- 🚧 Új bejegyzés / cég dialógus
- 🚧 Jelszógenerátor dialógus
- 🚧 Hozzáférés-kezelés (ITAdmin)
- 🚧 Audit hash-chain validáló nézet (ITAdmin)
- 🚧 Light/Dark theme váltó
- 🚧 Beállítások (clipboard timeout, auto-mask idő, stb.)

## Billentyűkombinációk

A bejegyzések nézetben:

| Gomb | Funkció |
|------|---------|
| `Ctrl+H` | Jelszó feltárása |
| `Ctrl+C` | Jelszó vágólapra (auto-clear 12 mp után) |
| `Ctrl+B` | Felhasználónév vágólapra |
| `Ctrl+U` | Kapcsolódás megnyitása (URL/RDP) |

## Biztonsági megjegyzések

- A refresh token **DPAPI CurrentUser** scope-pal titkosítva tárolódik
  `%LOCALAPPDATA%\PasswordManagerSystem\Client\session.bin` alatt.
  Csak az aktuális Windows user fiókja tudja visszafejteni ugyanazon a gépen.
- Az access tokent NEM perzisztáljuk; minden indítás új sessiont kezd
  a refresh tokenből.
- Plaintext felhasználónév/jelszó **csak** a reveal endpointokon érkezik,
  csak akkor amikor a user kifejezetten kéri – minden ilyen kérés
  auditálva van a backenden.
- A vágólap auto-clear csak akkor töröl, ha még a mi értékünk van benne
  (a user azóta nem másolt mást).
- Dev környezetben az `AcceptInvalidCertificates` beállítás engedi a
  self-signed dev tanúsítványokat. **Élesben ezt ki kell kapcsolni!**
