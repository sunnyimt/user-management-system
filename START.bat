@echo off
color 0B
echo.
echo ======================================================================
echo   User Management System - Starting Application
echo ======================================================================
echo.
echo Killing any existing processes...
taskkill /F /IM dotnet.exe >nul 2>&1
taskkill /F /IM node.exe >nul 2>&1
timeout /t 2 /nobreak

echo.
echo Starting Backend API on http://localhost:5278...
start "User Management - Backend" cmd /k "cd /d C:\Users\L\UserManagementApp\Backend\UserManagementAPI && dotnet run"

echo Waiting for backend to start...
timeout /t 3 /nobreak

echo.
echo Starting Frontend on http://localhost:3000...
start "User Management - Frontend" cmd /k "cd /d C:\Users\L\UserManagementApp\Frontend\user-management-app && npm start"

echo.
echo.
echo ======================================================================
echo   Application Starting!
echo ======================================================================
echo.
echo   Login Credentials:
echo   Username: user1
echo   Password: admin
echo.
echo   Frontend:  http://localhost:3000
echo   Backend:   http://localhost:5278
echo   API Docs:  http://localhost:5278/swagger
echo.
echo   Opening browser in 5 seconds...
echo.
timeout /t 5 /nobreak

start "" "http://localhost:3000"

echo.
echo Application is running! Check the two terminal windows for logs.
echo.
pause
