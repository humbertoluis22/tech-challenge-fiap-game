using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using TechChallengeGame.Domain.Entities.validations;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Shared.Models.Dtos.Responses;
using TechChallengeGame.Shared.Models.Dtos.Events;

namespace TechChallengeGame.Domain.Services
{
    public class GameService(INotifier notifier, IGameRepository gameRepository, IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    : BaseService(notifier),
        IGameService
    {
        public async Task<bool> AddAsync(Game model, CancellationToken ct = default)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

            try
            {
                if (!ExecuteValidation(new GameValidation(), model))
                    return false;

                if (await gameRepository.AnyAsync(x => x.Name == model.Name, ct))
                {
                    Notify("There is already a game with this name in the records");
                    return false;
                }

                await gameRepository.AddAsync(model, ct);

                var success =  await unitOfWork.CommitAsync(ct);

                // 1. SE SALVOU NO BANCO, publica evento para adicionar
                //if (success)
                //{
                //    var gameCreatedEvent = new GameCreatedEvent
                //    {
                //        Id = model.Id,
                //        Name = model.Name,
                //        Description = model.Description,
                //        Genre = model.Genre,
                //        Price = model.Price,
                //        IsActive = model.IsActive,
                //        ReleaseDate = model.ReleaseDate,
                //        CreatedAt = model.CreatedAt
                //    };
                //    await eventPublisher.PublishAsync(gameCreatedEvent, ct);
                //}

                return success;
            }
            catch
            {
                await unitOfWork.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool?> UpdateAsync(Guid id, Game model, CancellationToken ct = default)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

            try
            {
                if (!ExecuteValidation(new GameValidation(), model))
                    return false;

                var game = await gameRepository.GetByIdAsync(id, ct);

                if (game == null)
                {
                    Notify("Game not found");
                    return null;
                }

                if (await gameRepository.AnyAsync(x => x.Name == model.Name && x.Id != id, ct))
                {
                    Notify("There is already a game with this name in the records");
                    return false;
                }

                game.Name = model.Name;
                game.Price = model.Price;
                game.Genre = model.Genre;
                game.IsActive = model.IsActive;
                game.UpdatedAt = DateTime.UtcNow;

                gameRepository.Update(game);
                var success = await unitOfWork.CommitAsync(ct);

                // 2. SE ATUALIZOU NO BANCO, publica evento para atualizar
                //if (success)
                //{
                //    var gameUpdatedEvent = new GameUpdatedEvent
                //    {
                //        Id = game.Id,
                //        Name = game.Name,
                //        Description = game.Description,
                //        Genre = game.Genre,
                //        Price = game.Price,
                //        IsActive = game.IsActive,
                //        ReleaseDate = game.ReleaseDate,
                //        UpdatedAt = game.UpdatedAt
                //    };
                //    await eventPublisher.PublishAsync(gameUpdatedEvent, ct);
                //}

                return success;
            }
            catch
            {
                await unitOfWork.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool?> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var game = await gameRepository.GetByIdAsync(id, ct);

                if (game == null)
                {
                    Notify("Game not found");
                    return null;
                }

                game.IsActive = false;

                gameRepository.Delete(game);
                var success = await unitOfWork.CommitAsync(ct);

                // 3. SE DELETOU DO BANCO, publica evento para deletar
                //if (success)
                //{
                //    var gameDeletedEvent = new GameDeletedEvent { Id = id };
                //    await eventPublisher.PublishAsync(gameDeletedEvent, ct);
                //}

                return success;
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
