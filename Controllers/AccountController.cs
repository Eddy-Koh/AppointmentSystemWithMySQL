using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AppointmentBookingSystem.Models;
using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Mail;

namespace AppointmentBookingSystem.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public ActionResult Register()
        {
            // Prevent login user direct access via link
            if (Session["Username"] != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public ActionResult Register(User user)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // Check if username already exists 
                var checkUsernameCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE Username = @Username", conn);
                checkUsernameCmd.Parameters.AddWithValue("@Username", user.Username);
                int usernameCount = Convert.ToInt32(checkUsernameCmd.ExecuteScalar());

                if (usernameCount > 0)
                    ModelState.AddModelError("Username", "Username already exists. Please try another name.");

                // Check if email already exists
                var checkEmailCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE Email = @Email", conn);
                checkEmailCmd.Parameters.AddWithValue("@Email", user.Email);
                int emailCount = Convert.ToInt32(checkEmailCmd.ExecuteScalar());

                if (emailCount > 0)
                    ModelState.AddModelError("Email", "Email is already registered. Please use a different email.");
            }

            // Check mobile number
            if (string.IsNullOrWhiteSpace(user.MobilePhone) ||
                !(user.MobilePhone.Length == 10 || user.MobilePhone.Length == 11) ||
                !user.MobilePhone.All(char.IsDigit))
            { ModelState.AddModelError("MobilePhone", "Mobile number must be 10 or 11 digits and cannot be empty."); }

            // Check password length
            if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 3)
            { ModelState.AddModelError("Password", "Password must be at least 3 characters."); }

            // Return error if above data is not valid
            if (!ModelState.IsValid)
            { return View(user); }

            // Need to be approved by exist approver if register as approver
            // Requester pass directly
            if (user.Role == "Requester")
            { user.ApprovalStatus = "Accepted"; }
            else if (user.Role == "Approver")
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    // Check if any approver exists
                    var checkApproverCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE Role = 'Approver'", conn);
                    int approverCount = Convert.ToInt32(checkApproverCmd.ExecuteScalar());

                    if (approverCount == 0)
                    { user.ApprovalStatus = "Accepted"; }  // First approver - auto approve
                    else
                    { user.ApprovalStatus = "Pending"; }  // Other approver need to be approved by exist approver
                }
            }

            string hashedPassword = HashPassword(user.Password); // Hash password before storing

            // Save new user
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var insertCmd = new MySqlCommand(@"INSERT INTO users 
                    (Username, MobilePhone, Email, Password, Role, ApprovalStatus) 
                    VALUES (@Username, @MobilePhone, @Email, @Password, @Role, @ApprovalStatus)", conn);

                insertCmd.Parameters.AddWithValue("@Username", user.Username);
                insertCmd.Parameters.AddWithValue("@MobilePhone", user.MobilePhone);
                insertCmd.Parameters.AddWithValue("@Email", user.Email);
                insertCmd.Parameters.AddWithValue("@Password", hashedPassword);
                insertCmd.Parameters.AddWithValue("@Role", user.Role);
                insertCmd.Parameters.AddWithValue("@ApprovalStatus", user.ApprovalStatus);

                insertCmd.ExecuteNonQuery();
            }

            return RedirectToAction("Login"); // redirect to login page after success register
        }

        [HttpGet]
        public ActionResult Login()
        {
            // Prevent login user direct access via link
            if (Session["Username"] != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            // Load user details from database and check validity of the details input
            User user = null;
            string hashedInput = HashPassword(password);

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM users WHERE Username = @Username AND Password = @Password", conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", hashedInput);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new User
                        {
                            Username = reader["Username"].ToString(),
                            MobilePhone = reader["MobilePhone"].ToString(),
                            Email = reader["Email"].ToString(),
                            Password = reader["Password"].ToString(),
                            Role = reader["Role"].ToString(),
                            ApprovalStatus = reader["ApprovalStatus"].ToString()
                        };
                    }
                }
            }

            // Input details is not correct
            if (user == null)
            {
                ViewBag.Error = "Invalid credentials";
                return View();
            }

            // the approver registration still haven't approved by the exist approver
            if (user.Role == "Approver" && user.ApprovalStatus == "Pending")
            {
                ViewBag.Error = "Your approver registration is still pending approval.";
                return View();
            }

            // the approver registration is rejected
            if (user.Role == "Approver" && user.ApprovalStatus == "Rejected")
            {
                ViewBag.Error = "Your approver registration is rejected. Please register again.";
                return View();
            }

            // For session-based authentication and authorization purpose
            Session["Username"] = user.Username;
            Session["Role"] = user.Role;

            return RedirectToAction("Index", "Home"); // redirect to home page if success login
        }

        public ActionResult Logout()
        {
            // Pass the not login user to home page if they directly access via change link
            if (Session["Username"] == null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public ActionResult ConfirmLogout()
        {
            // Clear the session and redirect to no login home page
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Hashing password using SHA256
        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower(); // hex string
            }
        }

        // Return to Reset Password - Getting username/email
        [HttpGet]
        public ActionResult ResetPassword()
        {
            // Prevent login user direct access via link
            if (Session["Username"] != null)
                return RedirectToAction("Index", "Home");

            ViewBag.Step = 1; // Step 1: enter username/email
            return View();
        }

        [HttpPost]
        public ActionResult SendOtp(string identifier)
        {
            string username = null;
            string email = null;

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // Try match by username
                var cmdUser = new MySqlCommand("SELECT Email FROM users WHERE Username = @Identifier", conn);
                cmdUser.Parameters.AddWithValue("@Identifier", identifier);
                var resultUser = cmdUser.ExecuteScalar();

                if (resultUser != null)
                {
                    username = identifier;
                    email = resultUser.ToString();
                }
                else
                {
                    // Try match by email
                    var cmdEmail = new MySqlCommand("SELECT Username FROM users WHERE Email = @Identifier", conn);
                    cmdEmail.Parameters.AddWithValue("@Identifier", identifier);
                    var resultEmail = cmdEmail.ExecuteScalar();

                    if (resultEmail != null)
                    {
                        username = resultEmail.ToString();
                        email = identifier;
                    }
                }
            }

            if (username != null && email != null)
            {
                // Generate 6-digit OTP
                var rng = new Random();
                string otp = rng.Next(100000, 999999).ToString();

                Session["ResetOtp"] = otp;
                Session["ResetUser"] = username;

                SendEmail(email, "Password Reset OTP", $"Your OTP code is: {otp}");

                ViewBag.Step = 2; // Step 2: enter OTP
                ViewBag.Message = "OTP has been sent to your email.";
            }
            else
            {
                ViewBag.Step = 1;
                ViewBag.Error = "No matching user found.";
            }

            return View("ResetPassword");
        }

        [HttpPost]
        public ActionResult VerifyOtp(string otp)
        {
            if (Session["ResetOtp"] != null && otp == Session["ResetOtp"].ToString())
            {
                ViewBag.Step = 3; // Step 3: set new password
            }
            else
            {
                ViewBag.Step = 2;
                ViewBag.Error = "Invalid OTP.";
            }
            return View("ResetPassword");
        }

        // Setting new password
        [HttpPost]
        public ActionResult SetNewPassword(string newPassword)
        {
            // Check password length restriction
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 3)
            {
                ModelState.AddModelError("Password", "Password must be at least 3 characters.");
                ViewBag.Step = 3; // Stay on password reset step
                return View("ResetPassword");
            }

            if (Session["ResetUser"] == null)
                return RedirectToAction("ResetPassword");

            string username = Session["ResetUser"].ToString();
            string hashed = HashPassword(newPassword);

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("UPDATE users SET Password = @Password WHERE Username = @Username", conn);
                cmd.Parameters.AddWithValue("@Password", hashed);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.ExecuteNonQuery();
            }

            // Clear session
            Session.Remove("ResetOtp");
            Session.Remove("ResetUser");

            ViewBag.Step = 4; // Step 4: success
            return View("ResetPassword");
        }

        // Sending email
        public void SendEmail(string toEmail, string subject, string body)
        {
            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("your_email", "your_password"),
                EnableSsl = true
            };

            var message = new MailMessage("your_email", toEmail, subject, body);
            smtp.Send(message);
        }

    }
}
