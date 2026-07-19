using AutoMapper;
using BLL.Dtos.Appointment;
using BLL.Services.AbstractServices;
using BLL.Services.AbstractServices.AppointmentModule;
using DAL.Exceptions;
using DAL.Exceptions.AppointmentModule;
using DAL.Models.AppointmentModule;
using DAL.Models.Users;
using DAL.Repository;
using DAL.Shared.Enums;
using DAL.Specifications.Appointment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.ImplementationService.AppointmentModule
{
    public class AppointmentService(IUnitOfWork _unitOfWork, IMapper _mapper, IUserRepository _userRepository, INotificationService _notificationService) : IAppointmentService
    {

        #region Public Methods
        
        public async Task<AppointmentDto> BookAppointmentAsync(int patientId, CreateAppointmentDto dto)
        {
            var patient = await ValidatePatientAsync(patientId);
            ValidateFutureDate(dto.AppointmentDate);

            var doctor = await _userRepository.GetDoctorByIdAsync(dto.DoctorId)
                         ?? throw new DoctorNotFoundException(dto.DoctorId);
            if (!doctor.IsActive)
                throw new BadRequestException(new List<string> { "This doctor is currently inactive." });

            var schedule = await ValidateScheduleAsync(dto.ScheduleId, dto.DoctorId);
            ValidateDayOfWeek(dto.AppointmentDate, schedule.DayOfWeek);
            await ValidateSlotAvailabilityAsync(dto.DoctorId, dto.AppointmentDate, dto.ScheduleId);

            var appointmentEntity = _mapper.Map<Appointment>(dto);
            appointmentEntity.PatientId = patientId;
            appointmentEntity.AppointmentTime = schedule.StartTime;
            appointmentEntity.Status = AppointmentStatus.Pending;
            appointmentEntity.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepository<Appointment>().AddAsync(appointmentEntity);
            await _unitOfWork.SaveChangesAsync();
            var appointment = await GetAppointmentOrThrowAsync(appointmentEntity.Id);

            await _notificationService.SendNotificationAsync($"Patient with name {patient.Fullname} has booked an appointment.", NotificationType.AppointmentBookRequest, appointment.DoctorId);
            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<AppointmentDto> CancelAppointmentAsync(int appointmentId, int userId)
        {
            var appointment = await GetAppointmentOrThrowAsync(appointmentId);
            ValidateOwnership(appointment, userId);

            if (appointment.Status == AppointmentStatus.Cancelled ||
                appointment.Status == AppointmentStatus.Completed)
                throw new AppointmentNotCancelableException(appointmentId);

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<Appointment>().Update(appointment);

            if (appointment.PatientId == userId)
            {
                await _notificationService.SendNotificationAsync($"Patient {appointment.Patient?.Fullname ?? ""} has canceled an appointment.", NotificationType.AppointmentCanceled, appointment.DoctorId);
            }
            else if (appointment.DoctorId == userId)
            {
                await _notificationService.SendNotificationAsync($"Doctor {appointment.Doctor?.Fullname ?? ""} has canceled an appointment.", NotificationType.AppointmentCanceled, appointment.PatientId);
            }

            await _unitOfWork.SaveChangesAsync();


            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<AppointmentDto> ConfirmAppointmentAsync(int appointmentId, int doctorId)
        {
            var appointment = await GetAppointmentOrThrowAsync(appointmentId);

            if (appointment.DoctorId != doctorId)
                throw new UnauthorizedAppointmentAccessException(doctorId, appointmentId);

            if (appointment.Status != AppointmentStatus.Pending)
                throw new AppointmentNotConfirmableException(appointmentId);

            appointment.Status = AppointmentStatus.Confirmed;
            appointment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<Appointment>().Update(appointment);

            await _notificationService.SendNotificationAsync($"Doctor {appointment.Doctor?.Fullname ?? ""} has confirmed an appointment.", NotificationType.AppointmentConfirmed, appointment.PatientId);


            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<AppointmentDto> CompleteAppointmentAsync(int appointmentId, int doctorId)
        {
            var appointment = await GetAppointmentOrThrowAsync(appointmentId);

            if (appointment.DoctorId != doctorId)
                throw new UnauthorizedAppointmentAccessException(doctorId, appointmentId);

            if (appointment.Status != AppointmentStatus.Confirmed)
                throw new AppointmentNotCompletableException(appointmentId);

            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<Appointment>().Update(appointment);

            await _notificationService.SendNotificationAsync($"Doctor {appointment.Doctor?.Fullname ?? ""} has completed an appointment.", NotificationType.AppointmentCompleted, appointment.PatientId);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<AppointmentDto> GetAppointmentAsync(int appointmentId, int userId)
        {
            var appointment = await GetAppointmentOrThrowAsync(appointmentId);
            ValidateOwnership(appointment, userId);
            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<IEnumerable<AvailableDoctorSlotDto>> GetAvailableSlotsAsync(int doctorId, DateTime date)
        {
            var spec = new GetAvailableSlotsSpecs(doctorId, date);
            var doctorSchedules = await _unitOfWork.GetRepository<DoctorSchedule>().GetAllAsync(spec);

            var appointmentSpec = new AppointmentNotCancelledSpec(doctorId, date);
            var bookedAppointments = await _unitOfWork.GetRepository<Appointment>().GetAllAsync(appointmentSpec);
            var bookedScheduleIds = bookedAppointments.Select(a => a.ScheduleId).ToHashSet();

            var freeSchedules = doctorSchedules.Where(s => !bookedScheduleIds.Contains(s.Id));
            var dtos = _mapper.Map<IEnumerable<AvailableDoctorSlotDto>>(freeSchedules);
            foreach (var dto in dtos)
            {
                dto.AvailableDates = new List<DateTime> { date.Date };
            }
            return dtos;
        }

        public async Task<IEnumerable<AppointmentDto>> GetDoctorAppointmentsAsync(int doctorId)
        {
            var spec = new AppointmentsDoctorSpec(doctorId);
            var appointments = await _unitOfWork.GetRepository<Appointment>().GetAllAsync(spec);
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<IEnumerable<AppointmentDto>> GetMyAppointmentsAsync(int userId)
        {
            var spec = new AppointmentsPatientSpec(userId);
            var appointments = await _unitOfWork.GetRepository<Appointment>().GetAllAsync(spec);
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<AppointmentDto> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto dto, int userId)
        {
            var appointment = await GetAppointmentOrThrowAsync(appointmentId);
            ValidateOwnership(appointment, userId);

            if (appointment.Status == AppointmentStatus.Cancelled)
                throw new AppointmentNotCancelableException(appointmentId);

            if (appointment.AppointmentDate < DateTime.Now)
                throw new AppointmentNotCancelableException(appointmentId);

            
            if (dto.ScheduleId.HasValue)
            {
                var schedule = await ValidateScheduleAsync(dto.ScheduleId.Value, appointment.DoctorId);
                appointment.AppointmentTime = schedule.StartTime;
            }

            
            if (dto.AppointmentDate.HasValue)
            {
                ValidateFutureDate(dto.AppointmentDate.Value);

                var scheduleId = dto.ScheduleId ?? appointment.ScheduleId;
                var currentSchedule = await _unitOfWork.GetRepository<DoctorSchedule>().GetByIdAsync(scheduleId);
                if (currentSchedule != null)
                    ValidateDayOfWeek(dto.AppointmentDate.Value, currentSchedule.DayOfWeek);
            }

            
            if (dto.ScheduleId.HasValue || dto.AppointmentDate.HasValue)
            {
                var checkDate = dto.AppointmentDate ?? appointment.AppointmentDate;
                var checkScheduleId = dto.ScheduleId ?? appointment.ScheduleId;
                await ValidateSlotAvailabilityAsync(appointment.DoctorId, checkDate, checkScheduleId, appointmentId);

                if (appointment.Status == AppointmentStatus.Confirmed)
                {
                    appointment.Status = AppointmentStatus.Pending;
                }
            }

            _mapper.Map(dto, appointment);
            appointment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GetRepository<Appointment>().Update(appointment);
            await _notificationService.SendNotificationAsync($"Patient {appointment.Patient!.Fullname ?? ""} has updated an appointment.", NotificationType.AppointmentUpdated, appointment.DoctorId);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<AppointmentDto>(appointment);
        }

        #endregion

        #region Private Validation Methods

        private async Task<Appointment> GetAppointmentOrThrowAsync(int appointmentId)
        {
            var appointment = (await _unitOfWork.GetRepository<Appointment>().GetAllAsync(new AppointmentWithIncludesSpecs(appointmentId))).FirstOrDefault();
            if (appointment == null)
                throw new AppointmentNotFoundException(appointmentId);
            return appointment;
        }

        private static void ValidateOwnership(Appointment appointment, int userId)
        {
            if (appointment.PatientId != userId && appointment.DoctorId != userId)
                throw new UnauthorizedAppointmentAccessException(userId, appointment.Id);
        }

        private async Task<Patient> ValidatePatientAsync(int patientId)
        {
            var patient = await _userRepository.GetPatientWithAppointmentAsync(patientId)
                ?? throw new PatientNotFoundException(patientId);
            if (patient.UserType != "Patient")
                throw new UnauthorizedAccessException("Only patients can book appointments.");
            return patient;
        }

        private async Task<DoctorSchedule> ValidateScheduleAsync(int scheduleId, int doctorId)
        {
            var schedule = await _unitOfWork.GetRepository<DoctorSchedule>().GetByIdAsync(scheduleId)
                ?? throw new DoctorScheduleNotFoundException(scheduleId);

            if (schedule.DoctorId != doctorId)
                throw new UnauthorizedAppointmentAccessException(doctorId, scheduleId);

            if (!schedule.IsAvailable)
                throw new AppointmentSlotUnavailableException(doctorId, DateTime.Today, schedule.StartTime);

            return schedule;
        }

        private async Task ValidateSlotAvailabilityAsync(int doctorId, DateTime date, int scheduleId, int? excludeAppointmentId = null)
        {
            var appointmentSpec = new AppointmentNotCancelledSpec(doctorId, date);
            var bookedAppointments = await _unitOfWork.GetRepository<Appointment>().GetAllAsync(appointmentSpec);
            var isSlotTaken = bookedAppointments.Any(a => a.ScheduleId == scheduleId && a.Id != excludeAppointmentId);
            if (isSlotTaken)
            {
                var schedule = await _unitOfWork.GetRepository<DoctorSchedule>().GetByIdAsync(scheduleId);
                throw new AppointmentSlotUnavailableException(doctorId, date, schedule?.StartTime ?? TimeSpan.Zero);
            }
        }

        private static void ValidateFutureDate(DateTime date)
        {
            if (date.Date < DateTime.Today)
                throw new BadRequestException(["Cannot book an appointment in the past."]);
        }

        private static void ValidateDayOfWeek(DateTime date, DayOfWeek expectedDay)
        {
            if (date.DayOfWeek != expectedDay)
                throw new BadRequestException([$"The appointment date must be on {expectedDay}."]);
        }

        #endregion
    }
}
