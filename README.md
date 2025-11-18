# Appointment Booking System
This is a C# appointment booking system with features like user registration, role-based approval, appointment scheduling and password reset via email.

---

## ▶️ Running the Project
- **Run without debugging**: Press `Ctrl + F5`
- **Stop/Build**: Press `Ctrl + Shift + B`

---

## ⚙️ Configuration
### 1. Database Connection
- Ensure connection details are the same in:
  - `Web.config` under `<connectionStrings>`
  - `Models/DatabaseHelper.cs`
- Update them to match your MySQL server settings.
 
### 2. Enable MySQL connectivity
- Go to Tools > NuGet Package Manager > Package Manager Console
- run: Install-Package MySql.Data

### 3. Use the Reset Password feature
- Before running the project, update the SendEmail function in Controllers/AccountController.cs (line 322 to 333) with your own Gmail credentials.

**Ensure MySQL server is running, connection strings are correct and email credentials are configured well before using the system.**

---

## ⚙️ Feature
### 1. Role-based registration (Requester auto-approved, Approver pending unless first)
### 2. Appointment scheduling with date/time validation
### 3. Password reset via email OTP
### 4. Automatic creation of database and tables if missing


