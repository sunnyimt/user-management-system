# User Management Application

A simple React + .NET 8 application for managing users with login functionality.

## Project Structure

```
UserManagementApp/
├── Backend/
│   └── UserManagementAPI/    (.NET 8 WebAPI)
├── Frontend/
│   └── user-management-app/  (React application)
└── README.md
```

## Features

- **Login Page**: Authenticate with username and password
- **User Management**: Full CRUD operations (Create, Read, Update, Delete users)
- **File-Based Storage**: Users stored in CSV format
- **Simple UI**: Clean and responsive interface

## Default Credentials

- **Username**: admin
- **Password**: admin

## Backend Setup (.NET 8)

### Prerequisites
- .NET 8 SDK installed

### Running the Backend

```bash
cd Backend/UserManagementAPI
dotnet run
```

The API will run on: `https://localhost:7110`

### API Endpoints

- `POST /api/auth/login` - Login with username and password
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Data Storage

Users are stored in `data/users.csv` file relative to the backend project.

## Frontend Setup (React)

### Prerequisites
- Node.js (v16 or higher)
- npm

### Running the Frontend

```bash
cd Frontend/user-management-app
npm install  # If not already installed
npm start
```

The application will run on: `http://localhost:3000`

## How to Use

1. **Start the Backend**: Run the .NET API first
2. **Start the Frontend**: Then run the React app
3. **Login**: Use admin/admin to login
4. **Manage Users**: 
   - View all users in the table
   - Click "Add New User" to create a new user
   - Click "Edit" to update a user
   - Click "Delete" to remove a user

## Technologies Used

### Backend
- .NET 8
- ASP.NET Core
- CsvHelper (for CSV file handling)

### Frontend
- React 18
- CSS3
- Fetch API

## Notes

- Passwords are stored in plain text (for demo purposes only)
- User data is persisted in a CSV file
- The application requires both backend and frontend to be running
- CORS is configured to allow requests from the React frontend
# Deployment Test
