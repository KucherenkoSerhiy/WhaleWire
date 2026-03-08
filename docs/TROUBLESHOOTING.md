# Troubleshooting

## Tests blocked by Windows (0x800711C7)

**Error:** `System.IO.FileLoadException: An Application Control policy has blocked this file. (0x800711C7)`

Windows (Smart App Control, WDAC, or Defender) is blocking unsigned .NET assemblies from loading.

### Quick fix (run as Administrator)

```powershell
cd C:\repos\_other\WhaleWire
.\scripts\fix-windows-test-block.ps1 -AddExclusion
dotnet clean
dotnet build
dotnet test
```

### Manual steps

#### 1. Unblock files (removes "downloaded from internet" mark)

```powershell
Get-ChildItem -Path "C:\repos\_other\WhaleWire" -Recurse -File | Unblock-File
```

#### 2. Add Windows Defender exclusion (requires Admin)

```powershell
# Run PowerShell as Administrator
Add-MpPreference -ExclusionPath "C:\repos\_other\WhaleWire"
```

#### 3. Disable Smart App Control (Windows 11)

- **Settings** → **Privacy & security** → **Windows Security** → **App & browser control**
- **Smart App Control settings** → **Off**
- Restart, then run tests again

**Note:** On older Windows 11, turning Smart App Control off may prevent turning it back on without a reset.

#### 4. Allow blocked file (if Defender quarantined it)

- **Windows Security** → **Virus & threat protection** → **Protection history**
- Find the blocked `WhaleWire.Domain.dll` (or similar)
- Click **Actions** → **Allow on device**

#### 5. Alternative: run tests in WSL

```bash
wsl
cd /mnt/c/repos/_other/WhaleWire
dotnet test
```

### Root cause

- **Smart App Control** (Windows 11): Blocks unsigned/untrusted code
- **WDAC** (App Control for Business): Enterprise policy blocking DLLs
- **Zone.Identifier**: Files from git clone/download marked as "from internet"
