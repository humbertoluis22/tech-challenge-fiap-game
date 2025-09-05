using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechChallengeGame.Domain.Interfaces;

namespace TechChallengeGame.Domain.Notifications
{
    public class Notifier : INotifier
    {
        private readonly List<Notification> _notifications = [];

        public bool HasNotification()
        {
            return _notifications.Count != 0;
        }

        public List<Notification> GetNotifications()
        {
            return _notifications;
        }

        public void Handle(Notification notification)
        {
            _notifications.Add(notification);
        }
    }
}
