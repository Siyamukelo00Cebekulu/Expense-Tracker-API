# Expense Tracker API

A complete expense tracker application with a **.NET 8 backend** and a **React frontend**.

## Features

- User signup and login
- JWT authentication for protected routes
- User-specific expense data
- Add, edit, delete expenses
- Filter expenses by:
  - Past week
  - Past month
  - Last 3 months
  - Custom date range
- SQLite database for easy deployment

## Project Structure

- `backend/` - ASP.NET Core Web API
  - JWT authentication
  - SQLite storage
  - Expense CRUD operations
- `frontend/` - React application built with Vite
  - Login / signup UI
  - Expense list and filtering

## Backend Setup

1. Open a terminal in `backend`
2. Restore and build:
   ```powershell
   dotnet restore
   dotnet build
   ```
3. Run the backend API:
   ```powershell
   dotnet run
   ```
4. Default API base URL:
   - `http://localhost:5083`

## Frontend Setup

1. Open a terminal in `frontend`
2. Install dependencies:
   ```powershell
   npm install
   ```
3. Start the development server:
   ```powershell
   npm run dev
   ```
4. Open the frontend URL shown by Vite in your browser.

## API Endpoints

- `POST /api/auth/signup` - create a new user
- `POST /api/auth/login` - authenticate and receive a JWT
- `GET /api/expenses` - list authenticated user's expenses
- `POST /api/expenses` - create a new expense
- `PUT /api/expenses/{id}` - update an existing expense
- `DELETE /api/expenses/{id}` - delete an existing expense

## Notes

- The backend stores data in `backend/expenses.db`
- The frontend expects the backend at `http://localhost:5083`
- The JWT secret and issuer settings are configured in `backend/appsettings.json`

## Development Tips

- If you want to change the backend port, update `Properties/launchSettings.json` or set the `ASPNETCORE_URLS` environment variable.
- Use browser dev tools to inspect requests and make sure the `Authorization: Bearer <token>` header is included.

## License

This project is provided as-is for learning and demonstration purposes.

## URL
https://roadmap.sh/projects/expense-tracker-api