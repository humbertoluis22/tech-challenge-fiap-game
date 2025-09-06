using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface IUserLibraryRepository : IRepository<UserLibrary>
    {
        void RemoveLibraryItem(LibraryItem item, CancellationToken ct = default);
    }
}
