using BLL.Dtos;
using BLL.Dtos.Appointment;
using BLL.Dtos.Schedule;
using BLL.Services.AbstractServices;
using BLL.Services.AbstractServices.AppointmentModule;
using DAL.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PL.Extention;
using PresentationLayer.Controller;
using System;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize]
    public class AppointmentController(
        IAppointmentService _appointmentService
                        ) : ApiControllerBase
    {
        #region Patient - Functionality

        [Authorize(Roles = "PATIENT")]
        [HttpPost("Book")]
        public async Task<IActionResult> BookAppointment([FromBody] CreateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var appointment = await _appointmentService.BookAppointmentAsync(User.GetUserId(), dto);
            return Ok(appointment);
        }

        [Authorize(Roles = "PATIENT")]
        [HttpGet("MyAppointments")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var appointments = await _appointmentService.GetMyAppointmentsAsync(User.GetUserId());
            return Ok(appointments);
        }

        [Authorize(Roles = "PATIENT")]
        [HttpPut("Update/{appointmentId}")]
        public async Task<IActionResult> UpdateAppointment(int appointmentId, [FromBody] UpdateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var appointment = await _appointmentService.UpdateAppointmentAsync(appointmentId, dto, User.GetUserId());
            return Ok(appointment);
        }

        #endregion

        #region Shared - Patient & Doctor

        [HttpGet("GetAppointment/{appointmentId}")]
        public async Task<IActionResult> GetAppointment(int appointmentId)
        {
            var appointment = await _appointmentService.GetAppointmentAsync(appointmentId, User.GetUserId());
            return Ok(appointment);
        }

        [HttpPost("Cancel/{appointmentId}")]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            var appointment = await _appointmentService.CancelAppointmentAsync(appointmentId, User.GetUserId());

            return Ok(appointment);
        }

        #endregion

        #region Doctor - Functionality

        [Authorize(Roles = "DOCTOR")]
        [HttpGet("DoctorAppointments")]
        public async Task<IActionResult> GetDoctorAppointments()
        {
            var appointments = await _appointmentService.GetDoctorAppointmentsAsync(User.GetUserId());
            return Ok(appointments);
        }

        [Authorize(Roles = "DOCTOR")]
        [HttpPost("Confirm/{appointmentId}")]
        public async Task<IActionResult> ConfirmAppointment(int appointmentId)
        {
            var appointment = await _appointmentService.ConfirmAppointmentAsync(appointmentId, User.GetUserId());
            return Ok(appointment);
        }

        [Authorize(Roles = "DOCTOR")]
        [HttpPost("Complete/{appointmentId}")]
        public async Task<IActionResult> CompleteAppointment(int appointmentId)
        {
            var appointment = await _appointmentService.CompleteAppointmentAsync(appointmentId, User.GetUserId());
            return Ok(appointment);
        }

        #endregion

        #region Public

        [AllowAnonymous]
        [HttpGet("AvailableSlots")]
        public async Task<IActionResult> GetAvailableSlots([FromQuery] int doctorId, [FromQuery] DateTime date)
        {
            var slots = await _appointmentService.GetAvailableSlotsAsync(doctorId, date);
            return Ok(slots);
        }

        #endregion
    }
}
