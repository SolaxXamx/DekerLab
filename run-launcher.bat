@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Le launcher est en C# et ce script utilise "dotnet run".
  echo Sur un PC 32 bits, installe le SDK .NET 8 x86 ^(Windows x86^) depuis Microsoft,
  echo ou utilise plutot l'exe cree par publish-windows-x86.bat.
  echo.
  echo Si tu ne veux rien installer sur ce PC, genere l'exe win-x86 sur un autre PC,
  echo puis copie le dossier publish sur ton PC 32 bits.
  pause
  exit /b 1
)
dotnet run --project MonLauncherJeux.csproj
