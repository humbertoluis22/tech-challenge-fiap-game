using TechChallengeGame.Domain.Notifications;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface INotifier
    {
        bool HasNotification();
        List<Notification> GetNotifications();
        void Handle(Notification notification);
    }
}
