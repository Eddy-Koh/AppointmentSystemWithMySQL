# Appointment Booking System
This is a C# appointment booking system with features like user registration, role-based approval, appointment scheduling and password reset via email.
The system contain 2 types of user - Approver and Requester. Each user has its own accessibility as described in Section `Features`.

---

## ▶️ Running the Project
- **Debug**: Press `F5`
- **Run without debugging**: Press `Ctrl + F5`
- **Stop/Build**: Press `Ctrl + Shift + B`

---

## ⚙️ Configuration
### 1. Database Connection
- Ensure connection details are the same in:
  - `Web.config` under `<connectionStrings>`
  - `Models/DatabaseHelper.cs`
- Update them to match own MySQL server settings.
 
### 2. Enable MySQL connectivity
- Go to `Tools` > `NuGet Package Manager` > `Package Manager Console`
- run: `Install-Package MySql.Data`

### 3. Use the Reset Password feature
- Before running the project, update the `SendEmail` function in `Controllers/AccountController.cs`(line 322 to 333) with your own Gmail credentials.

**Ensure MySQL server is running, connection strings are correct and email credentials are configured well before using the system.**

---

## ⚙️ Features
### 1. Role-based registration (Requester auto-approved, Approver pending unless first)
- For Requester, the user can direct login to the system after register account, the system will direct approve user registration.
- For Approver, the user registration need to be accepted by exist approver before can login the system, unless the approver is the first user register as approver.

### 2. User Registration Management 
- The feature is only available for Approver, Requester cannot access.
- The Approver can view all the user details in this feature.
- The Approver can accept or reject the Approver registration as well as view the details, but for Requester, Approver only can view the details. 

### 3. Appointment scheduling with date/time validation
Requester
- Can create, view, edit and delete appointment

Approver
- Can view, approve/reject the appointment

Appointment
- After one appointment is approved, other appointments which has crashed time with the approved appointment will auto-reject.
- The timeslot for the approved appointment will also be locked to prevent future crash booking.
- Can only be made on working hour - Monday to Friday from 8am to 5pm. (Cannot make appointment on weekend)
- Can only be made on the day after current day.
- The end time must be after the start time.

### 4. Password reset via email OTP
- Step 1: User provide their username or email.
- Step 2: OTP will send to the email.
- Step 3: Provide the OTP for reset password.
- Step 4: Provide the new password.

### 5. Automatic creation of database and tables if missing
- Do not need to create database and tables manually.
- The system will self-create database (appointment_db) and tables (users and appointments) when run the system.

### 6. URL accessibility
- https://localhost:44334/ - All accessible
- https://localhost:44334/Account/Login - Only accessible for unlogin user
- https://localhost:44334/Account/Register - Only accessible for unlogin user
- https://localhost:44334/Account/ResetPassword - Only accessible for unlogin user
- https://localhost:44334/Account/Logout - Only accessible for login user
- https://localhost:44334/Appointment - Only accessible for login user. View for approver and requester is different.
- https://localhost:44334/Appointment/Create - Only accessible for requester.
- https://localhost:44334/Appointment/Edit/:id - Only accessible for requester (Booking Owner).
- https://localhost:44334/User/UserList - Only accessible for approver.


