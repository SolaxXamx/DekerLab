@echo off
cd /d "%~dp0.."

REM Build for x64
dotnet publish RocketStats.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o .\publish\win-x64

REM Build for x86
dotnet publish RocketStats.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o .\publish\win-x86

REM Build for AnyCPU
dotnet publish RocketStats.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=true -o .\publish\AnyCPU

echo Publication terminee!
pause
