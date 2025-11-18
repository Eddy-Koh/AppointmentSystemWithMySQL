using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AppointmentBookingSystem.Models;
using MySql.Data.MySqlClient;

namespace AppointmentBookingSystem.Controllers
{
    public class UserController : Controller
    {
        // For loading the user data
        private List<User> LoadUsers()
        {
            var users = new List<User>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM users", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Username = reader["Username"].ToString(),
                            MobilePhone = reader["MobilePhone"].ToString(),
                            Email = reader["Email"].ToString(),
                            Password = reader["Password"].ToString(),
                            Role = reader["Role"].ToString(),
                            ApprovalStatus = reader["ApprovalStatus"].ToString()
                        });
                    }
                }
            }

            return users;
        }

        // For getting user list
        public ActionResult UserList()
        {
            // Prevent no login user or requester direct access via the link
            if (Session["Username"] == null || Session["Role"]?.ToString() != "Approver")
            { return RedirectToAction("Index", "Home"); }

            var users = LoadUsers();
            return View(users);
        }

        // To approve the approver registration
        public ActionResult ApproveUser(string username)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("UPDATE users SET ApprovalStatus = 'Accepted' WHERE Username = @Username", conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("UserList");
        }

        // To reject the approver registration
        public ActionResult RejectUser(string username)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("UPDATE users SET ApprovalStatus = 'Rejected' WHERE Username = @Username", conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("UserList");
        }
    }
}
