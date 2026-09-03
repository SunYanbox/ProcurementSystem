@echo off
setlocal

cd /d "%~dp0.."

start "ProcurementSystem Backend" cmd /k "dotnet run --project ProcurementSystem\ProcurementSystem.csproj"
start "ProcurementSystem Vue" cmd /k "pnpm --dir ProcurementSystem.Vue dev"

endlocal

exit