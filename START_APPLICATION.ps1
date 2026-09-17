# User Management System - Startup Script
# This script starts both the backend and frontend in separate windows

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   User Management System - Starting Application                ║" -ForegroundColor Cyan
Write-Host "║   Backend: http://localhost:5278                               ║" -ForegroundColor Cyan
Write-Host "║   Frontend: http://localhost:3000                              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Kill any existing processes
Write-Host "🧹 Cleaning up old processes..." -ForegroundColor Yellow
Get-Process | Where-Object {
    $_.ProcessName -like "*dotnet*" -or
    $_.ProcessName -like "*npm*"
} | Stop-Process -Force -ErrorAction SilentlyContinue

Start-Sleep -Seconds 1

# Backend
Write-Host ""
Write-Host "🚀 Starting Backend API..." -ForegroundColor Green
$backendPath = "C:\Users\L\UserManagementApp\Backend\UserManagementAPI"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$backendPath'; Write-Host 'Backend starting...' -ForegroundColor Cyan; Write-Host ''; dotnet run" -WindowStyle Normal

# Wait for backend to start
Write-Host "   Waiting for backend to initialize..." -ForegroundColor Gray
Start-Sleep -Seconds 3

# Frontend
Write-Host ""
Write-Host "🎨 Starting Frontend..." -ForegroundColor Green
$frontendPath = "C:\Users\L\UserManagementApp\Frontend\user-management-app"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$frontendPath'; Write-Host 'Frontend starting...' -ForegroundColor Cyan; Write-Host ''; npm start" -WindowStyle Normal

Write-Host ""
Write-Host "✅ Application starting in separate windows..." -ForegroundColor Green
Write-Host ""
Write-Host "📝 Default Credentials:" -ForegroundColor Yellow
Write-Host "   Username: user1" -ForegroundColor Cyan
Write-Host "   Password: admin" -ForegroundColor Cyan
Write-Host ""
Write-Host "🌐 Open in browser: http://localhost:3000" -ForegroundColor Cyan
Write-Host ""
Write-Host "⏳ Frontend will open automatically in 5 seconds..." -ForegroundColor Gray
Write-Host ""

Start-Sleep -Seconds 5

# Open browser
Write-Host "🌍 Opening browser..." -ForegroundColor Cyan
Start-Process "http://localhost:3000"

Write-Host ""
Write-Host "✨ Application ready! Check the backend and frontend windows for logs." -ForegroundColor Green
Write-Host ""
