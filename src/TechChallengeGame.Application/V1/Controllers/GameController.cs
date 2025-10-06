using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecChallenge.Application.Controllers;
using TechChallengeGame.Shared.Models.Dtos.Requests;
using TechChallengeGame.Shared.Models.Dtos.Responses;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Shared.Models.Generics;
using TecChallenge.Application.Extensions;
using MicroserviceExample.Middleware;

namespace TecChallenge.Application.V1.Controllers;


//[Authorize(Roles = "Admin")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/games")]
[Produces("application/json")]
//[Authorize]
public class GameController(
    INotifier notifier,
    ILogger<GameController> logger,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    IGameRepository gameRepository,
    IGameQueryRepository gameQueryRepository,
    IGameService gameService
) : MainController(notifier, httpContextAccessor, webHostEnvironment)
{

    /// <summary>
    /// Get all available games
    /// </summary>
    /// <returns>List of all games</returns>
    /// <response code="200">Returns the list of games</response>
    [HttpGet]
    //[AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Root<IEnumerable<GameResponse>>>> GetAllGames()
    {
        if (!HttpContext.IsAuthenticated())
        {
            logger.LogWarning("Tentativa de acesso sem JWT válido");
            return Unauthorized(
                new { Message = "Token JWT válido é obrigatório para gerenciar usuários" }
            );
        }

        // Extrair informações do JWT usando os extensions methods
        var userId = HttpContext.GetUserId();

        logger.LogInformation("Chamando rota que recolhe todos os games!");
        var games = (await gameRepository.GetAllAsync()).Select(g => g.MapToDto());
        return CustomResponse(data: games);
    }


    /// <summary>
    /// Search for games using Elasticsearch with advanced filtering.
    /// </summary>
    /// <param name="searchTerm">The term to search for in game names and descriptions.</param>
    /// <returns>A list of games that match the search criteria.</returns>
    [HttpGet("search")] // Nova Rota: GET /v1/games/search
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Root<IEnumerable<GameResponse>>>> SearchGames([FromQuery] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            // Retorna uma lista vazia ou um bad request se o termo de busca for obrigatório
            NotifyError("Search term cannot be empty.");
            return CustomResponse<IEnumerable<GameResponse>>(statusCode: HttpStatusCode.BadRequest);
        }

        logger.LogInformation("Buscando jogos no Elasticsearch com o termo: {SearchTerm}", searchTerm);

        // Usando o gameQueryRepository que busca do Elasticsearch
        var games = await gameQueryRepository.SearchAsync(searchTerm);

        if (!games.Any())
        {

            NotifyError("No similar games found.");
            return CustomResponse<IEnumerable<GameResponse>>(statusCode: HttpStatusCode.BadRequest);

        }
        return CustomResponse(data: games);
    }

    // NOVA ROTA DE RECOMENDAÇÕES ABAIXO

    /// <summary>
    /// Get similar game recommendations for a specific game using Elasticsearch.
    /// </summary>
    /// <param name="id">The unique identifier of the game to get recommendations for.</param>
    /// <returns>A list of recommended games.</returns>
    /// <response code="200">Returns the list of recommended games.</response>
    [HttpGet("{id:guid}/recommendations")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Root<IEnumerable<GameResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Root<IEnumerable<GameResponse>>>> GetRecommendations(Guid id)
    {
        logger.LogInformation("Buscando recomendações para o jogo com ID: {GameId}", id);
        var recommendations = await gameQueryRepository.GetRecommendationsAsync(id);
        if (!recommendations.Any())
        {
            NotifyError("No similar games found.");
            return CustomResponse<IEnumerable<GameResponse>>(statusCode: HttpStatusCode.BadRequest);

        } 
        return CustomResponse(data: recommendations);
    }


    // NOVA ROTA DE AGREGAÇÕES ABAIXO

    /// <summary>
    /// Get a summary of game counts by genre from Elasticsearch.
    /// </summary>
    /// <returns>An aggregation of genres and their respective game counts.</returns>
    /// <response code="200">Returns the genre summary.</response>
    [HttpGet("genres/summary")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Root<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Root<object>>> GetGenreSummary()
    {
        logger.LogInformation("Buscando agregação de gêneros");
        var aggregations = await gameQueryRepository.GetGenreAggregationsAsync();
        return CustomResponse(data: aggregations);
    }



    /// <summary>
    /// Get a specific game by its identifier
    /// </summary>
    /// <param name="id">Game identifier</param>
    /// <returns>Game data</returns>
    /// <response code="200">Returns the requested game</response>
    /// <response code="404">Game not found</response>
    [HttpGet("{id:guid}")]
    //[AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<GameResponse>>> GetGameById(Guid id)
    {
        var game = await gameRepository.GetByIdAsync(id);

        if (game != null)
            return CustomResponse(data: game.MapToDto());

        NotifyError("Game not found");
        return CustomResponse<GameResponse>(statusCode: HttpStatusCode.NotFound);
    }



    /// <summary>
    /// Create a new game
    /// </summary>
    /// <param name="model">Game data</param>
    /// <returns>Created game data</returns>
    /// <response code="201">Game created successfully</response>
    /// <response code="400">Invalid input data</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<GameResponse>>> AddGame(GameAddRequest model)
    {
        logger.LogInformation("Realizando a criacao de um novo jogo!");

        if (!ModelState.IsValid)
            return CustomModelStateResponse<GameResponse>(ModelState);

        var entity = model.MapToEntity();

        var result = await gameService.AddAsync(entity);

        return result
            ? CustomResponse(data: entity.MapToDto(), statusCode: HttpStatusCode.Created)
            : CustomResponse<GameResponse>(statusCode: HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Update an existing game
    /// </summary>
    /// <param name="id">Game identifier</param>
    /// <param name="model">Updated game data</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Game updated successfully</response>
    /// <response code="400">Invalid input data or ID mismatch</response>
    /// <response code="404">Game not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<GameResponse>>> UpdateGame(Guid id, GameUpdateRequest model)
    {
        logger.LogInformation("Realizando a atualizacao de um  jogo!");

        if (id != model.Id)
        {
            NotifyError("The ids entered are not the same");
            return CustomResponse<GameResponse>();
        }

        if (!ModelState.IsValid)
            return CustomModelStateResponse<GameResponse>(ModelState);

        var result = await gameService.UpdateAsync(id, model.MapToEntity());

        if (result != null)
            return !result.Value
                ? CustomResponse<GameResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<GameResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<GameResponse>(statusCode: HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Delete a game
    /// </summary>
    /// <param name="id">Game identifier</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Game deleted successfully</response>
    /// <response code="404">Game not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<GameResponse>>> DeleteGame(Guid id)
    {
        logger.LogInformation("deletando  um  jogo pelo id!");

        var result = await gameService.DeleteAsync(id);

        if (result != null)
            return !result.Value
                ? CustomResponse<GameResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<GameResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<GameResponse>(statusCode: HttpStatusCode.NotFound);
    }
}
