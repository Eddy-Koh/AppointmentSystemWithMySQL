# Appointment Booking System
This is a C# appointment booking system with features like user registration, role-based approval, appointment scheduling and password reset via email.

To run the project, press Ctrl + F5. To stop or build the project, press Ctrl + Shift + B.

Make sure your database connection details are same in both Web.config under <connectionStrings> and Models/DatabaseHelper.cs. 
Update them to match your MySQL server settings. 
Example connection string: 
<connectionStrings>
    <add name="MySqlConnection" connectionString="Server=127.0.0.1;Database=appointment_db;Uid=root;Pwd=;" providerName="MySql.Data.MySqlClient" />
</connectionStrings>

To enable MySQL connectivity, go to Tools > NuGet Package Manager > Package Manager Console and run: Install-Package MySql.Data

To use the Reset Password feature, before running the project, update the SendEmail method in Controllers/AccountController.cs (line 322 to 333) with your own Gmail credentials.

Features include: role-based registration (Requester auto-approved, Approver pending unless first), appointment scheduling with date/time validation, password reset via email OTP, and automatic creation of database and tables if missing.

Testers should ensure MySQL server is running, connection strings are correct, and email credentials are configured before using the system.
