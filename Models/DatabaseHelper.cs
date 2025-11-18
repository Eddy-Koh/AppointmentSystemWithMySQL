using MySql.Data.MySqlClient;
using System.Configuration;

namespace AppointmentBookingSystem.Models
{
    public static class DatabaseHelper
    {
        // Connection string from Web.config (points to appointment_db)
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

        // A separate connection string without database, for creating DB if missing
        private static readonly string serverConnectionString = "Server=localhost;Uid=root;Pwd=;";
        public static MySqlConnection GetConnection()
        {
            // Ensure database exists
            using (var serverConn = new MySqlConnection(serverConnectionString))
            {
                serverConn.Open();
                var createDbCmd = new MySqlCommand("CREATE DATABASE IF NOT EXISTS appointment_db", serverConn);
                createDbCmd.ExecuteNonQuery();
            }

            using (var dbConn = new MySqlConnection(connectionString))
            {
                dbConn.Open();

                // Ensure users table exists
                var createUsersTableCmd = new MySqlCommand(@"
                    CREATE TABLE IF NOT EXISTS users (
                        Username VARCHAR(50) PRIMARY KEY,
                        MobilePhone VARCHAR(15) NOT NULL,
                        Email VARCHAR(100) NOT NULL UNIQUE,
                        Password VARCHAR(255) NOT NULL,
                        Role VARCHAR(20) NOT NULL,
                        ApprovalStatus VARCHAR(20) NOT NULL
                    )", dbConn);
                createUsersTableCmd.ExecuteNonQuery();

                // Ensure appointments table exists
                var createAppointmentsTableCmd = new MySqlCommand(@"
                    CREATE TABLE IF NOT EXISTS appointments (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        RequesterName VARCHAR(50) NOT NULL,
                        AppointmentDate DATE NOT NULL,
                        StartTime TIME NOT NULL,
                        EndTime TIME NOT NULL,
                        Reason TEXT,
                        Status VARCHAR(20) NOT NULL DEFAULT 'Pending'
                    )", dbConn);
                createAppointmentsTableCmd.ExecuteNonQuery();
            }

            // Return connection to appointment_db
            return new MySqlConnection(connectionString);
        }
    }
}
