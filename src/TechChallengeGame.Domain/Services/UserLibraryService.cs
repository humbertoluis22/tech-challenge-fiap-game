using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Entities.validations;
using TechChallengeGame.Domain.Interfaces;

namespace TechChallengeGame.Domain.Services
{
    public class UserLibraryService(
    INotifier notifier,
    IUserLibraryRepository userLibraryRepository,
    IGameRepository gameRepository,
    IUnitOfWork unitOfWork
) : BaseService(notifier), IUserLibraryService
    {
        public async Task<bool> AddAsync(UserLibrary model, CancellationToken ct = default)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

            try
            {
                if (!ExecuteValidation(new UserLibraryValidation(), model))
                    return false;

                if (userLibraryRepository.WhereAsync(x => x.UserId == model.UserId).Result.Any())
                {
                    Notify("There is already a library created for this user");
                    return false;
                }

                await userLibraryRepository.AddAsync(model, ct);

                return await unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await unitOfWork.RollbackAsync(ct);
                throw;
            }
        }



        public async Task<bool?> AddGameForUser(Guid userID,Guid gameId, CancellationToken ct = default)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

            try
            {
                // busca a biblioteca do usuário junto com os itens
                var userLibrary = await userLibraryRepository.FirstOrDefaultAsync(
                    x => x.UserId == userID,
                    true,
                    x => x.Items
                );

                if (userLibrary == null)
                {
                    Notify("User library not found");
                    return null;
                }

                // valida se o jogo já existe na biblioteca
                if (userLibrary.Items.Any(i => i.GameId == gameId))
                {
                    Notify("This game is already in the user's library");
                    return false;
                }


                //  Verifica se o jogo existe
                var game = await gameRepository.GetByIdAsync(gameId, ct);
                if (game == null)
                {
                    Notify("Game does not exist");
                    return null;
                }

                // adiciona o jogo
                userLibrary.AddGame(game.Id, game.Price);

                userLibraryRepository.Update(userLibrary);

                return await unitOfWork.CommitAsync(ct);

            }
            catch
            {
                await unitOfWork.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool?> AddGameToLibraryFromEventAsync(Guid userId, Guid gameId, CancellationToken ct = default)
        {
            return await AddGameForUser(userId, gameId, ct);
        }

        public async Task<bool?> DeleteGameForUser(Guid userid, Guid gameId, CancellationToken ct = default)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

            try
            {
                // busca a biblioteca do usuário junto com os itens
                var userLibrary = await userLibraryRepository.FirstOrDefaultAsync(
                    x => x.UserId == userid,
                    true,
                    x => x.Items
                );

                if (userLibrary == null)
                {
                    Notify("User library not found");
                    return null;
                }

                var item = userLibrary.RemoveGame(gameId);

                if(item == null)
                {
                    Notify("This game is not in the user's library");
                    return null;
                }

                return await unitOfWork.CommitAsync(ct);

            }
            catch
            {
                await unitOfWork.RollbackAsync(ct);
                throw;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
