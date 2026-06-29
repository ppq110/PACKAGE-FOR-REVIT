# DynLock - Chạy script Dynamo ma hoa trong Revit

Bo cong cu cho phep **team lead** ma hoa file `.dyn` thanh `.dynx`, va **nhan vien**
chay script do tu mot nut tren ribbon Revit **ma khong bao gio nhin thay node hay
thong so ben trong** (Dynamo chay ngam, khong mo cua so).

> Luu y nguyen tac: khong the tao file ma Dynamo *mo duoc tren canvas* nhung
> *khong xem duoc*. Giai phap nay an noi dung bang cach **khong bao gio mo
> Dynamo UI** - script duoc giai ma trong bo nho va chay headless.

## Thanh phan

| Project | Vai tro | Ai dung |
|---|---|---|
| `DynLock.Core` | Thu vien ma hoa AES-256 + HMAC dung chung | (build chung) |
| `DynLock.AuthServer` | Server HTTP noi bo cu, khong can cho login Gmail don gian | Tuy chon |
| `DynLock.Encryptor` | `DynLockEncrypt.exe` - chuyen `.dyn` -> `.dynx` | Team lead |
| `DynLock.Addin` | Addin Revit: nut **BIMLab -> Run Tool**, form nhap thong so, chay ngam | Nhan vien |

Target hien tai: **Revit 2024 / Dynamo 2.19 / .NET Framework 4.8** (khop file
`FILE DYNAMO GUI TEAM IOT.dyn`). Addin nap `DynamoRevitDS.dll` bang reflection
nen thuong chay duoc tren 2021-2024 ma khong can doi code; voi Revit 2025+ can
doi TargetFramework sang `net8.0-windows` va package API sang `2025.*`/`2026.*`.

## Dang nhap Gmail qua DB trung tam trong LAN

Leader va Member khong can nhap server URL. App da nhung san dia chi Auth Server
noi bo, mac dinh:

```text
http://192.168.110.213:5050
```

Khi app hoi dang nhap, user chi can nhap Gmail dung dang:

```text
ten@gmail.com
```

Gmail do phai ton tai va dang active trong database trung tam ma Auth Server
dang dung. Co 2 cach:

- `sqlite`: file DB nam tren may chay Auth Server.
- `postgres`: database Postgres trung tam, nen Leader them email tu may nao cung
  dong bo ngay cho cac may khac, mien la tat ca app cung goi ve Auth Server
  `http://192.168.110.213:5050`.

Neu dung SQLite, file DB nam tren may server:

```text
C:\ProgramData\BIMLab\DynLock\auth.db
```

Tren may Fedora server `192.168.110.213`, tao config Auth Server:

```bash
chmod +x setup_fedora_auth_server.sh run_auth_server_fedora.sh install_fedora_auth_service.sh

POSTGRES_CONNECTION_STRING='Host=127.0.0.1;Port=5432;Database=bimlab_auth;Username=bimlab;Password=your_password' \
./setup_fedora_auth_server.sh
```

Chay Auth Server truc tiep tren Fedora:

```bash
./run_auth_server_fedora.sh
```

Hoac cai thanh user service:

```bash
./install_fedora_auth_service.sh
```

Neu dung Windows server thay vi Fedora, tao config va import/upsert du lieu Gmail tu
Supabase cu bang PowerShell:

```powershell
.\setup_local_auth_server.ps1 `
  -SuperAdminEmail "projectbim.bimlab@gmail.com" `
  -LegacySupabaseUrl "https://your-project.supabase.co" `
  -LegacySupabaseAnonKey "<old-supabase-anon-key>"
```

Neu dung Postgres, tao database/user truoc, roi setup nhu sau:

```powershell
.\setup_local_auth_server.ps1 `
  -SuperAdminEmail "projectbim.bimlab@gmail.com" `
  -DatabaseProvider postgres `
  -PostgresConnectionString "Host=192.168.110.213;Port=5432;Database=bimlab_auth;Username=bimlab;Password=your_password" `
  -LegacySupabaseUrl "https://your-project.supabase.co" `
  -LegacySupabaseAnonKey "<old-supabase-anon-key>"
```

Chay Auth Server tren Windows server:

```powershell
.\run_auth_server.ps1 -BindUrl "http://0.0.0.0:5050"
```

Tu may khac trong LAN test:

```powershell
Invoke-RestMethod "http://192.168.110.213:5050/api/health"
```

Neu can dong goi ban gui cho team, chay:

```powershell
.\rebuild_and_dist.ps1
```

Script se build va tao san 2 goi trong `dist`:

- `BIMLab_Studio_Leader_<ngay>.zip`
- `BIMLab_Player_Member_<ngay>.zip`

## Team lead can lam gi (checklist)

1. **Build** (can may Windows + Visual Studio 2022 hoac .NET SDK):
   ```
   dotnet build DynLock.sln -c Release
   ```
2. **Ma hoa script**:
   ```
   DynLockEncrypt.exe "FILE DYNAMO GUI TEAM IOT.dyn"
   ```
   -> tao `FILE DYNAMO GUI TEAM IOT.dynx`. Xóa/cat file `.dyn` goc, chi phat `.dynx`.
3. **Cai addin cho nhan vien** (moi may):
   - Cach de nhat: chay `Cai dat BIMLab DynLock.exe` (project `DynLock.Installer`).
   - Hoac tay: copy `DynLock.Addin.dll` + `DynLock.Core.dll` + `Newtonsoft.Json.dll`
     vao `C:\ProgramData\BIMLab\DynLock\`, copy `install\DynLock.addin` vao
     `C:\ProgramData\Autodesk\Revit\Addins\2024\`.
   - **Package Dynamo**: addin tu go cac node package "mo coi" (khong noi day)
     khoi graph truoc khi chay - graph hien tai nho do **khong can bimorphNodes**
     nua (node `CAD.CurvesFromCADLayers` da duoc thay bang Python node va chi con
     sot lai khong noi day). Neu mot cong cu khac *thuc su* dung node package,
     addin se tu bao ten package con thieu cho nhan vien truoc khi chay
     (package cai trong `%AppData%\Dynamo\Dynamo Revit\<phien ban>\packages`).
4. **Phan phoi file .dynx** - chon 1 trong 2:
   - Tha vao folder share roi tao `C:\ProgramData\BIMLab\DynLock\config.json`:
     ```json
     { "ScriptFolders": [ "\\\\server\\share\\dynx" ] }
     ```
   - Hoac tao folder `Scripts` canh `DynLock.Addin.dll` va bo file `.dynx` vao.
5. **Test**: mo Revit -> tab **BIMLab** -> **Run Tool** -> chon script -> dien
   5 thong so (layer coc, level, quet chon CAD, family coc, ten parameter) ->
   Chạy. Kiem tra coc duoc model dung nhu khi chay bang Dynamo thuong.

## Nhan vien thay gi

Chay **BIMLab Player.exe** -> nhap Gmail dang `ten@gmail.com` -> app tu kiem tra
Gmail trong DB local tren `192.168.110.213` -> cai add-in cho Revit 2024/2025/2026.
Mo Revit -> tab **BIMLab** ban dau co panel
**Manager** voi nut **Login** va **Load**. Dang nhap Gmail trong Revit, bam
**Load** de chon file `.dynx` leader gui, add-in se tao nut plugin tu metadata
trong file `.dynx`. Khi bam plugin, member chi thay form nhap thong so/chon CAD,
khong thay Dynamo graph hay file JSON doc duoc.

## Gioi han can biet

- **Key ma hoa nam san trong app**: nguoi dung Leader/Member khong can cau hinh key.
  Nguoi biet decompile (dnSpy) co the moi key ra. Voi muc
  dich noi bo la du; muon chac hon thi obfuscate DLL (ConfuserEx/Dotfuscator)
  hoac chuyen key sang server cap phat.
- **File tam**: luc chay, graph giai ma duoc ghi ra `%TEMP%` voi ten ngau nhien
  trong vai giay roi xoa ngay. Nguoi dung thuong khong can thiep kip.
- **Node input ho tro tren form**: String, Number, Bool, dropdown Level,
  dropdown Family Type, chon element (1 hoac nhieu). Kieu input khac se giu
  nguyen gia tri da luu trong graph.
- Neu doi key nang cao bang env/secrets.json, moi file `.dynx` cu phai ma hoa lai.

## Cach addin chay graph ngam (ky thuat)

- Dung journal `dynAutomation=true` -> Dynamo chay **dong bo, test mode**, khong UI.
  Trong che do nay Dynamo **bo qua `dynPathExecute`** va khong bao gio tu goi
  `Run()` - graph chi chay neu workspace o **RunType Automatic**. Vi vay addin
  luon ep `RunType=Automatic` + `HasRunWithoutCrash=true` vao JSON truoc khi chay
  (team lead luu graph Manual hay Automatic deu duoc).
- Package van duoc Dynamo nap binh thuong trong automation mode
  (tu `%AppData%\Dynamo\Dynamo Revit\<ver>\packages`). Luu y: may chua tung mo
  Dynamo UI lan nao se chua co thu muc package (test mode khong migrate) -
  mo Dynamo mot lan neu can cai package.
- Sau khi chay, addin doc trang thai tung node (qua reflection) de bao node
  thieu package (DummyNode) va canh bao/loi cua node, roi xoa workspace
  (graph da giai ma) khoi bo nho Dynamo.
