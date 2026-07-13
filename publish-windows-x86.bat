@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo SDK .NET introuvable.
  echo Pour creer l'exe compatible PC 32 bits, installe le SDK .NET 8 x86 ^(Windows x86^) depuis Microsoft.
  echo Tu peux aussi lancer ce script sur un autre PC Windows avec le SDK, puis copier l'exe genere.
  pause
  exit /b 1
)
echo Publication x86 autonome...
dotnet publish MonLauncherJeux.csproj -c Release -r win-x86 --self-contained true -p:PlatformTarget=x86 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 (
  echo.
  echo La publication a echoue. Lance build-windows-x86.bat pour afficher l'erreur de build plus clairement.
  pause
  exit /b 1
)
echo.
echo EXE 32 bits cree dans: bin\Release\net8.0-windows\win-x86\publish\DekerLab.exe
echo Copie ce fichier sur ton PC 32 bits et double-clique dessus.
pause
