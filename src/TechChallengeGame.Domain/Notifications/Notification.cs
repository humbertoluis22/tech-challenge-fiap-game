using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Domain.Notifications
{
    public class Notification(string message)
    {
        public string Message { get; } = message;
    }
}
