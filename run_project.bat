@echo off
title AcxiomCRM - Server Launcher
cd /d "%~dp0"
echo ====================================================
echo Starting AcxiomCRM Web Application...
echo Local App URL: http://localhost:5110
echo Swagger REST API: http://localhost:5110/swagger
echo ====================================================
echo.
dotnet run --launch-profile "http"
pause
