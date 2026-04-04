# Software Authentication Keyloader GUI

WinForms GUI wrapper for `sakl.exe`, packaged for local use and redistribution.

## Credits

- Original `sakl.exe`, manual, and underlying SAKL project by Daniel Dugger
- Original upstream project: [SoftwareAuthKeyLoader](https://github.com/duggerd/SoftwareAuthKeyLoader)
- This repository adds a packaged Windows GUI wrapper around that CLI so it can be used directly without compiling

## Included Files

- `Software Authentication Keyloader GUI.exe`: current GUI launcher
- `sakl.exe`: underlying CLI used by the GUI
- `sakl.exe.config`: CLI config
- `sakl_manual.pdf`: bundled manual
- `P25RadioSerialModem.inf`: bundled driver file
- `src/Launcher/Program.cs`: full GUI source

## What Works

- `Load`
- `Read Status`
- `Zeroize`
- Active / Named workflows supported by the GUI
- decimal `Unit` in the GUI, converted to hex for named CLI operations
- 16-byte key editor with paste and auto-generate support

## Notes

- On the tested radio, `Read Status -> Active` works reliably.
- `Read Status -> Device` was not reliable and is intentionally not offered.
- `Zeroize` may be rejected by some radios or firmware even when reads work.
- The GUI is a wrapper around `sakl.exe`; without that executable the UI opens but cannot perform operations.
- End users do not need to build anything to use the packaged GUI. Building is only needed if you want to modify the source or rebuild the launcher.

## Build

Build with the .NET Framework C# compiler on Windows:

```powershell
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:winexe /out:'Software Authentication Keyloader GUI.exe' /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll '.\src\Launcher\Program.cs'
```

## Publish

If you publish this repo, keep the `LICENSE` file with the package and preserve attribution to Daniel Dugger as the original SAKL author.
