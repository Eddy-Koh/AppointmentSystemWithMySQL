using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AppointmentBookingSystem.Models;
using MySql.Data.MySqlClient;

namespace AppointmentBookingSystem.Controllers
{
    public class AppointmentController : Controller
    {
        // ------------------------------------------------------
        // Add Appointment
        [HttpGet]
        public ActionResult Create()
        {
            // Prevent no login user or approver direct access via the link
            if (Session["Username"] == null)
                return RedirectToAction("Index", "Home");

            if (Session["Role"]?.ToString() != "Requester")
                return RedirectToAction("Index");

            // Set the initial appointment data
            var appointment = new Appointment
            {
                RequesterName = Session["Username"]?.ToString(),
                AppointmentDate = DateTime.Today,
                StartTime = TimeSpan.FromHours(8),  // 08:00
                EndTime = TimeSpan.FromHours(17)    // 17:00
            };
            return View(appointment);
        }

        [HttpPost]
        public ActionResult Create(Appointment appointment)
        {
            // Prevent no login user or approver direct access via the link
            if (Session["Username"] == null)
                return RedirectToAction("Index", "Home");

            if (Session["Role"]?.ToString() != "Requester")
                return RedirectToAction("Index");

            appointment.Id = Math.Abs(Guid.NewGuid().GetHashCode()); //Assign unique id
            //appointment.Status = "Pending";

            // Collect the the validation error to be display in view
            var validationErrors = ValidateAppointment(appointment);
            foreach (var error in validationErrors)
            { ModelState.AddModelError("", error); }

            // For return error
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    { System.Diagnostics.Debug.WriteLine($"Error in {key}: {error.ErrorMessage}"); }
                }
                return View(appointment);
            }

            // Store the data in the database
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"INSERT INTO appointments 
                        (Id, RequesterName, AppointmentDate, StartTime, EndTime, Reason, Status) 
                        VALUES (@Id, @RequesterName, @AppointmentDate, @StartTime, @EndTime, @Reason, @Status)", conn);

                    cmd.Parameters.AddWithValue("@Id", appointment.Id);
                    cmd.Parameters.AddWithValue("@RequesterName", appointment.RequesterName);
                    cmd.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);
                    cmd.Parameters.AddWithValue("@StartTime", appointment.StartTime);
                    cmd.Parameters.AddWithValue("@EndTime", appointment.EndTime);
                    cmd.Parameters.AddWithValue("@Reason", appointment.Reason ?? "");
                    cmd.Parameters.AddWithValue("@Status", appointment.Status);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SQL Error: " + ex.Message);
                ModelState.AddModelError("", "Database error: " + ex.Message);
                return View(appointment);
            }

            return RedirectToAction("Index");
        }
        // End: Add Appointment
        // ------------------------------------------------------

        // ------------------------------------------------------
        // View Appointment
        public ActionResult Index()
        {
            // Prevent no login user direct access via the link
            if (Session["Username"] == null)
                return RedirectToAction("Index", "Home");

            var appointments = LoadAppointments();

            // if role = requester, display only his booking
            // else display all the booking (approver)
            string role = Session["Role"]?.ToString();
            string username = Session["Username"]?.ToString();

            if (role == "Requester")
            {
                appointments = appointments
                    .Where(a => a.RequesterName.Equals(username, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Sort by AppointmentDate, then StartTime, then EndTime
            appointments = appointments
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ThenBy(a => a.EndTime)
                .ToList();

            return View(appointments);
        }
        // End: View Appointment
        // ------------------------------------------------------

        // ------------------------------------------------------
        // Update Appointment
        [HttpGet]
        public ActionResult Edit(int id)
        {
            // Prevent no login user direct access via the link
            if (Session["Username"] == null)
                return RedirectToAction("Index", "Home");

            // Load appointment via id
            var appointment = LoadAppointments().FirstOrDefault(a => a.Id == id);

            // Prevent the person other than the appointment requester to get the data
            if (Session["Role"]?.ToString() != "Requester"
                || appointment?.Status != "Pending"
                || appointment?.RequesterName != Session["Username"]?.ToString())
                return RedirectToAction("Index");

            return View(appointment);
        }

        [HttpPost]
        public ActionResult Edit(Appointment updated)
        {
            // Prevent no login user direct access via the link
            if (Session["Username"] == null)
                return RedirectToAction("Index", "Home");

            var appointments = LoadAppointments(); // Load appointment via id
            var existing = appointments.FirstOrDefault(a => a.Id == updated.Id);

            // Prevent the person other than the appointment requester to access the data
            if (existing == null ||
                Session["Role"]?.ToString() != "Requester" ||
                existing.Status != "Pending" ||
                existing.RequesterName != Session["Username"]?.ToString())
            { return RedirectToAction("Index"); }

            // Error Validation
            var validationErrors = ValidateAppointment(updated);
            foreach (var error in validationErrors)
            { ModelState.AddModelError("", error); }

            if (!ModelState.IsValid)
            { return View(updated); }

            // Update the appointment in the database
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand(@"UPDATE appointments SET 
                    AppointmentDate = @AppointmentDate,
                    StartTime = @StartTime,
                    EndTime = @EndTime,
                    Reason = @Reason
                    WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@AppointmentDate", updated.AppointmentDate);
                cmd.Parameters.AddWithValue("@StartTime", updated.StartTime);
                cmd.Parameters.AddWithValue("@EndTime", updated.EndTime);
                cmd.Parameters.AddWithValue("@Reason", updated.Reason ?? "");
                cmd.Parameters.AddWithValue("@Id", updated.Id);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
        // End: Update Appointment
        // ------------------------------------------------------

        // ------------------------------------------------------
        // Delete Appointment
        public ActionResult Delete(int id)
        {
            // Prevent no login user direct access via the link
            if (Session["Username"] == null)
                return RedirectToAction("Index", "Home");

            var appointment = LoadAppointments().FirstOrDefault(a => a.Id == id);

            // Only allow requester to delete if status is Pending or Rejected
            if (Session["Role"]?.ToString() != "Requester" ||
                appointment == null ||
                appointment.RequesterName != Session["Username"]?.ToString() ||
                (appointment.Status != "Pending" && appointment.Status != "Rejected"))
            { return RedirectToAction("Index"); }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM appointments WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
        // End: Delete Appointment
        // ------------------------------------------------------

        public ActionResult Approve(int id)
        {
            // Prevent no login user or requester direct access via the link
            if (Session["Username"] == null)
                return RedirectToAction("Index", "Home");

            if (Session["Role"]?.ToString() != "Approver")
                return RedirectToAction("Index");

            var appointments = LoadAppointments();
            var approved = appointments.FirstOrDefault(a => a.Id == id && a.Status == "Pending");

            if (approved != null)
            {
                approved.Status = "Approved";
                // Auto reject others appointment that has crash time if one approved
                foreach (var a in appointments)
                {
                    if (a.Id != approved.Id &&
                        a.Status == "Pending" &&
                        a.AppointmentDate.Date == approved.AppointmentDate.Date &&
                        a.StartTime < approved.EndTime &&
                        a.EndTime > approved.StartTime)
                    { a.Status = "Rejected"; }
                }

                SaveAppointments(appointments); // Save the appointment
            }

            return RedirectToAction("Index");
        }

        public ActionResult Reject(int id)
        {
            // Prevent no login user or requester direct access via the link
            if (Session["Username"] == null)
                return RedirectToAction("Index", "Home");

            if (Session["Role"]?.ToString() != "Approver")
                return RedirectToAction("Index");

            var appointments = LoadAppointments();
            var target = appointments.FirstOrDefault(a => a.Id == id && a.Status == "Pending");

            if (target != null)
            {
                target.Status = "Rejected";
                SaveAppointments(appointments);
            }

            return RedirectToAction("Index");
        }

        // For loading all the appointment in database
        private List<Appointment> LoadAppointments()
        {
            var appointments = new List<Appointment>();

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM appointments", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new Appointment
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            RequesterName = reader["RequesterName"].ToString(),
                            AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]),
                            StartTime = TimeSpan.Parse(reader["StartTime"].ToString()),
                            EndTime = TimeSpan.Parse(reader["EndTime"].ToString()),
                            Reason = reader["Reason"].ToString(),
                            Status = reader["Status"].ToString()
                        });
                    }
                }
            }

            return appointments;
        }

        // For save an appointment
        private void SaveAppointments(List<Appointment> appointments)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // Clear all existing appointments
                var deleteCmd = new MySqlCommand("DELETE FROM appointments", conn);
                deleteCmd.ExecuteNonQuery();

                // Re-insert all appointments
                foreach (var a in appointments)
                {
                    var insertCmd = new MySqlCommand(@"INSERT INTO appointments 
                        (Id, RequesterName, AppointmentDate, StartTime, EndTime, Reason, Status) 
                        VALUES (@Id, @RequesterName, @AppointmentDate, @StartTime, @EndTime, @Reason, @Status)", conn);

                    insertCmd.Parameters.AddWithValue("@Id", a.Id);
                    insertCmd.Parameters.AddWithValue("@RequesterName", a.RequesterName);
                    insertCmd.Parameters.AddWithValue("@AppointmentDate", a.AppointmentDate);
                    insertCmd.Parameters.AddWithValue("@StartTime", a.StartTime);
                    insertCmd.Parameters.AddWithValue("@EndTime", a.EndTime);
                    insertCmd.Parameters.AddWithValue("@Reason", a.Reason ?? "");
                    insertCmd.Parameters.AddWithValue("@Status", a.Status);

                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        // Check for approved appointment date time to prevent requester choose the appointment time that has already approved
        private bool IsTimeSlotAvailable(DateTime date, TimeSpan start, TimeSpan end)
        {
            var approved = LoadAppointments().Where(a => a.Status == "Approved");

            return !approved.Any(a =>
                a.AppointmentDate.Date == date.Date &&
                start < a.EndTime &&
                end > a.StartTime);
        }

        // For input details validation
        private List<string> ValidateAppointment(Appointment appointment)
        {
            var errors = new List<string>();

            if (appointment.AppointmentDate.Date <= DateTime.Today)
                errors.Add("Appointment date must be in the future.");

            if (appointment.AppointmentDate.DayOfWeek == DayOfWeek.Saturday ||
                appointment.AppointmentDate.DayOfWeek == DayOfWeek.Sunday)
                errors.Add("Appointments cannot be scheduled on weekends.");

            if (appointment.StartTime < TimeSpan.FromHours(8))
                errors.Add("Start time must be at or after 08:00.");

            if (appointment.EndTime > TimeSpan.FromHours(17))
                errors.Add("End time must be at or before 17:00.");

            if (appointment.EndTime <= appointment.StartTime)
                errors.Add("End time must be after start time.");

            if (!IsTimeSlotAvailable(appointment.AppointmentDate, appointment.StartTime, appointment.EndTime))
                errors.Add("This time slot overlaps with an approved appointment.");

            return errors;
        }
    }
}
