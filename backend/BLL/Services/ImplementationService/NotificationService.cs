using BLL.Dtos;
using BLL.Hubs;
using BLL.Services.AbstractServices;
using DAL.Exceptions;
using DAL.Models;
using DAL.Repository;
using DAL.Shared.Enums;
using DAL.Specifications.NotificationSpecs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.ImplementationService
{
    public class NotificationService(IUnitOfWork _unitOfWork , IHubContext<NotificationHub> hubContext) : INotificationService
    {
        public async Task DeleteNotificationAsync(int notificationId)
        {
            var notification = await _unitOfWork.GetRepository<Notification>().GetByIdAsync(notificationId)
                ?? throw new NotificationNotFoundException(notificationId);

            _unitOfWork.GetRepository<Notification>().Delete(notification);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(int UserId)
        {
            var notifications = await _unitOfWork.GetRepository<Notification>().GetAllAsync(new NotificationsByUserIdSpecs(UserId));
            return notifications
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto 
            { 
                Id = n.Id, 
                Message = n.Message, 
                IsRead = n.IsRead, 
                CreatedAt = n.CreatedAt 
            }).ToList();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            var spec = new UnreadNotificationsForUserSpecification(userId);

            var unreadNotifications = await _unitOfWork.GetRepository<Notification>().GetAllAsync(spec);
            return unreadNotifications.Count();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _unitOfWork.GetRepository<Notification>().GetByIdAsync(notificationId)
                ?? throw new NotificationNotFoundException(notificationId);
            notification.IsRead = true;
            _unitOfWork.GetRepository<Notification>().Update(notification);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task SendNotificationAsync(string message, NotificationType Type, int UserId)
        {
            var notification = new Notification
            {
                UserId = UserId,
                Message = message,
                Type = Type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            try { 
                await hubContext.Clients.User(UserId.ToString()).SendAsync("NewNotification", new NotificationDto
                {
                    Id = notification.Id,
                    Message = message,
                    IsRead = false,
                    CreatedAt = notification.CreatedAt
                });
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error sending notification via SignalR: {ex.Message}");
            }
            
        }
    }
}
