using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Shared.Models.Dtos.Requests
{
    public class AddGameToLibraryRequest
    {
        public Guid GameId { get; set; }
        //public decimal PurchasePrice { get; set; }
    }
}
