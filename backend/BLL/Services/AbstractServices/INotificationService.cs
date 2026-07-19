using BLL.Dtos;
using DAL.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.AbstractServices
{
    public interface INotificationService
    {
        public Task SendNotificationAsync(string message, NotificationType type , int UserId);

        public Task<List<NotificationDto>> GetNotificationsAsync(int UserId);
        Task MarkAsReadAsync(int notificationId);
        Task DeleteNotificationAsync(int notificationId);
        Task<int> GetUnreadCountAsync(int userId);

    }
}
