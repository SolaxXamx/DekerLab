@echo off
cd /d "%~dp0.."
dotnet build RocketStats.csproj -c Release
pause
