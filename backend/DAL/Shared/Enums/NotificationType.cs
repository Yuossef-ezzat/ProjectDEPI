using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Shared.Enums
{
    public enum NotificationType
    {
        AppointmentReminder = 1,
        AppointmentBookRequest,
        AppointmentCanceled,
        AppointmentConfirmed,
        AppointmentCompleted,
        AppointmentUpdated,
        OrderUpdate,
        ConsultationUpdate,
        ConsultationRequest,
        ConsultationReview,
        Message,
        System
    }
}
