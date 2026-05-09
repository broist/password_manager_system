# Telepítés és első futtatás

## 1. Kicsomagolás

A zip fájlt csomagold ki a `C:\Szakdolgozat\PasswordManagerSystem\` mappába
úgy, hogy a következő szerkezet jöjjön létre:

```
C:\Szakdolgozat\PasswordManagerSystem\
├── Backend_api\                          (már létezik)
│   └── PasswordManagerSystem.Api\
├── docker\                               (már létezik)
├── docs\                                 (már létezik)
├── PasswordManagerSystem.Client.sln      ← ÚJ
└── PasswordManagerSystem.Client\         ← ÚJ
    ├── App.xaml
    ├── App.xaml.cs
    ├── PasswordManagerSystem.Client.csproj
    ├── README.md
    ├── appsettings.json
    └── ... (a többi mappa)
```

> Megjegyzés: a kliens egy különálló projekt és egy különálló `.sln` fájllal
> indul. Ha a backendet és a klienst **egy** solution-ben akarod látni,
> később hozzáadhatod a kliens projektet a backend solution-jébe is.

## 2. Visual Studio megnyitása

1. Indítsd el a Visual Studio Community 2022-t
2. **File → Open → Project/Solution**
3. Tallózd ki:
   `C:\Szakdolgozat\PasswordManagerSystem\PasswordManagerSystem.Client.sln`
4. Megnyitja a Solution Explorer-t a kliens projekttel

## 3. NuGet csomagok visszaállítása

A Visual Studio első megnyitáskor automatikusan letölti a csomagokat.
Ha valamiért nem teszi:

- **Solution Explorer-ben jobb klikk a Solution-ön → Restore NuGet Packages**

Vagy parancsból (Developer PowerShell):
```powershell
cd C:\Szakdolgozat\PasswordManagerSystem
dotnet restore PasswordManagerSystem.Client.sln
```

## 4. Backend indítása

Mielőtt a klienst elindítanád, a backendnek futnia kell:

1. Indítsd el a Docker Desktop-ot, ha még nem fut
2. Ellenőrizd hogy a MySQL Docker konténer fut:
   ```powershell
   docker ps
   ```
   Látnod kell a `pms-mysql` (vagy hasonló nevű) konténert.

3. Indítsd el a Backend API-t Visual Studio-ban (külön példányban):
   - Nyisd meg a backend solution-t (`PasswordManagerSystem.Api.sln`)
   - **F5**
   - A Swagger UI nyíljon meg `https://localhost:7050/swagger`-en

## 5. Kliens elindítása

A backend fut a háttérben. Most a kliens Visual Studio ablakában:

1. Solution Explorer-ben legyen kiválasztva a `PasswordManagerSystem.Client`
   mint **Startup Project** (jobb klikk → Set as Startup Project, ha még nem az)
2. **F5** vagy a zöld nyíl

## 6. Bejelentkezés

Ha a backend fut, megjelenik a login képernyő.

**Mock AD adatok (fejlesztéshez):**

| Felhasználónév | Szerepkör |
|----------------|-----------|
| `pista.admin` | ITAdmin (minden joggal) |
| `nora.it` | IT (létrehozhat, módosíthat) |
| `andras.consultant` | Consultant (csak olvasás, jogosulttal) |
| `eva.support` | Support (csak olvasás, jogosulttal) |

**Jelszó mind a négyhez:** `Test1234!`

> A felhasználónév csak tartalmazza valamelyik kulcsszót — nem kell pontosan
> megfelelnie, pl. `kovacsi-it-admin` is működik (admin-ként, mert előbb
> találja meg az "admin" szót).

## 7. Hibaelhárítás

### "Nem sikerült csatlakozni a backendhez"

- Fut-e a backend? `https://localhost:7050/swagger` elérhető-e böngészőből?
- Stimmel-e a `BaseUrl` az `appsettings.json`-ben?
- Ha a backend egy másik portot használ, írd át `appsettings.json`-ben

### "Connection error: SSL ..."

- Dev tanúsítvány: ASP.NET Core dev cert kell hogy telepítve legyen
  ```powershell
  dotnet dev-certs https --trust
  ```
- Vagy az `appsettings.json`-ben legyen `"AcceptInvalidCertificates": true`
  (alapból igen). Ez DEV-ben OK, élesben NEM!

### "401 Unauthorized" minden hívásnál

- Ellenőrizd hogy a Mock AD ténylegesen aktív-e a backendnél
  (`appsettings.json` → `Authentication.Provider`: `"Mock"`)
- Töröld a tárolt sessiont:
  ```powershell
  Remove-Item "$env:LOCALAPPDATA\PasswordManagerSystem\Client\session.bin" -Force
  ```

### "View not found" futás közben

- Build → Clean Solution
- Build → Rebuild Solution
- Ha továbbra is, töröld a `bin` és `obj` mappákat manuálisan, majd build

## 8. Fejlesztési tippek

- A teljes kódbázis MVVM-re épül, ViewModel-ekben írd a logikát, ne code-behind-ban
- Új feature service: `Services/Api/` alá és regisztráld az `App.xaml.cs`-ben
  `ConfigureServices` metódusban
- Új nézet: `ViewModels/` alá ViewModel, `Views/` alá UserControl, és a
  `ShellWindow.xaml` `Window.Resources` részében regisztrálj DataTemplate-et
- Új ikon: `Resources/Themes/Icons.xaml`-be Path data, és használat
  `<Path Data="{StaticResource Icon.UjIkon}" .../>`-vel
