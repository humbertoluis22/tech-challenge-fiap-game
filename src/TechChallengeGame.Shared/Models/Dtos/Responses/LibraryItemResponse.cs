using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Shared.Models.Dtos.Responses
{
    public class LibraryItemReponse
    {
        public Guid Id { get; set; }
        public Guid UserLibraryId { get; set; }
        public Guid GameId { get; set; }
        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
        public decimal PurchasePrice { get; set; }
    }
}
