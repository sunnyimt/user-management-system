# Quick Start Guide

## Step 1: Start the Backend (.NET API)

Open a PowerShell terminal and run:

```powershell
cd Backend\UserManagementAPI
dotnet run
```

You should see:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7110
```

**Leave this terminal open!**

## Step 2: Start the Frontend (React)

Open a **new** PowerShell terminal and run:

```powershell
cd Frontend\user-management-app
npm start
```

This will automatically open your browser to `http://localhost:3000`

## Step 3: Login

Use these credentials:
- **Username**: admin
- **Password**: admin

Or you can try different usernames/passwords if they're registered in the system.

## Step 4: Use the App

Once logged in, you can:
- ✅ View all users in the table
- ✅ Click "Add New User" to create a new user
- ✅ Click "Edit" to modify a user's username/password
- ✅ Click "Delete" to remove a user
- ✅ Click "Logout" to return to the login page

## Troubleshooting

### "Connection refused" or "Cannot connect to server"
- Make sure the backend is running on https://localhost:7110
- Check that you started the backend BEFORE the frontend

### SSL Certificate Warning
- This is normal in development. The backend uses self-signed certificates.
- You can bypass this by allowing the connection in your browser

### Port Already in Use
- If port 3000 is already in use, React will ask to use a different port
- If port 7110 is already in use, change it in the backend appsettings.json

## Notes

- All user data is saved to `Backend/UserManagementAPI/data/users.csv`
- Passwords are stored in plain text (for demo purposes only)
- Both terminal windows must remain open while using the app
- To stop: Press Ctrl+C in each terminal
