param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [Parameter(Mandatory = $true)][string]$ShortcutName,
    [string]$ExeName = 'CardboardDungeon.exe',
    [string]$Description = 'Cardboard Dungeon'
)

$ErrorActionPreference = 'Stop'

$gameDir = (Resolve-Path $GameDir).Path
$exePath = Join-Path $gameDir $ExeName
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Executable not found: $exePath"
}

# Build a portable shortcut: the absolute target is only a hint, the relative
# path keeps the link working after the whole game folder is moved or unpacked
# on another computer.
$code = @'
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

[ComImport, Guid("00021401-0000-0000-C000-000000000046")]
public class ShellLinkCoClass { }

[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
public interface IShellLinkW {
    void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
    void GetIDList(out IntPtr ppidl);
    void SetIDList(IntPtr pidl);
    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
    void GetHotkey(out short pwHotkey);
    void SetHotkey(short wHotkey);
    void GetShowCmd(out int piShowCmd);
    void SetShowCmd(int iShowCmd);
    void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
    void Resolve(IntPtr hwnd, uint fFlags);
    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

public static class PortableLink {
    public static void Create(string lnkPath, string target, string workingDir, string relativeTarget, string description, string iconPath) {
        var link = (IShellLinkW)new ShellLinkCoClass();
        link.SetPath(target);
        link.SetWorkingDirectory(workingDir);
        link.SetDescription(description);
        link.SetIconLocation(iconPath, 0);
        link.SetRelativePath(relativeTarget, 0);
        ((IPersistFile)link).Save(lnkPath, true);
    }
}
'@

if (-not ('PortableLink' -as [type])) {
    Add-Type -TypeDefinition $code -Language CSharp
}

$lnkPath = Join-Path $gameDir $ShortcutName
if (Test-Path -LiteralPath $lnkPath) {
    Remove-Item -LiteralPath $lnkPath -Force
}

[PortableLink]::Create($lnkPath, $exePath, $gameDir, $ExeName, $Description, $exePath)

if (-not (Test-Path -LiteralPath $lnkPath)) {
    throw "Failed to create shortcut: $lnkPath"
}
Write-Host "Shortcut created: $lnkPath"
