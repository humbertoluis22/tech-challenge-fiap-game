using System.Net;
using Microsoft.AspNetCore.Mvc;
using TecChallenge.Application.Controllers;
using TecChallenge.Application.Extensions;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Domain.Services;
using TechChallengeGame.Shared.Models.Dtos.Requests;
using TechChallengeGame.Shared.Models.Dtos.Responses;
using TechChallengeGame.Shared.Models.Generics;

namespace TecChallenge.Application.V1.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/user-libraries")]
[Produces("application/json")]
public class UserLibraryController(
    INotifier notifier,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    IUserLibraryRepository userLibraryRepository,
    ILogger<UserLibraryController> logger,
    IUserLibraryService userLibraryService
    ) : MainController(notifier, httpContextAccessor, webHostEnvironment)
{
    /// <summary>
    /// Get a user's game library
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>The user's library containing their game collection</returns>
    /// <response code="200">Returns the user's game library</response>
    /// <response code="404">User library not found</response>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserLibraryResponse>>> GetUserLibrary(Guid userId)
    {
        logger.LogInformation("Obtendo biblioteca do usuário do banco de dados.");
        var userLibrary = await userLibraryRepository.FirstOrDefaultAsync(
            x => x.UserId == userId,
            false,
            x => x.Items
        );

        if (userLibrary != null)
            return CustomResponse(data: userLibrary.MapToDto());

        NotifyError("User library not found");

        return CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.NotFound);
    }


    /// <summary>
    /// Create a library for user
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>Create user library</returns>
    /// <response code="200">Game successfully added to user's library</response>
    /// <response code="404">User library not found</response>
    [HttpPost("{userId:guid}")]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserLibraryResponse>>> CreateALibraryForUser(
        Guid userId
    )
    {
        logger.LogInformation("Criando biblioteca do usuário no banco de dados.");
        var userLibrary = UserLibrary.Create(userId);
        var result = await userLibraryService.AddAsync(userLibrary);

        return result
            ? CustomResponse(data: userLibrary.MapToDto(), statusCode: HttpStatusCode.Created)
            : CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.BadRequest);
    }


    /// <summary>
    /// add game for user
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>No content if successful</returns>
    /// <response code="200">Game successfully added to user's library</response>
    /// <response code="404">User library not found</response>
    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserLibraryResponse>>> AddGameToLibrary(
        Guid userId,
        AddGameToLibraryRequest resquest
    )
    {
        logger.LogInformation("Adicionando jogo à biblioteca do usuário no banco de dados.");
        var result = await userLibraryService.AddGameForUser(userId, resquest.GameId);

        if (result != null)
            return !result.Value
                ? CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.NotFound);
    }


    // <summary>
    /// remove game to user
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="gameId">The unique identifier of the Game</param>
    /// <returns>No content if successful</returns>
    /// <response code="200">Game successfully added to user's library</response>
    /// <response code="404">User library not found</response>
    [HttpDelete("{userId:guid}/games/{gameId:guid}")]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserLibraryResponse>>> RemoveGameFromLibrary(
        Guid userId,
        Guid gameId
    )
    {
        logger.LogInformation("Removendo jogo da biblioteca do usuário no banco de dados.");
        var result = await userLibraryService.DeleteGameForUser(userId, gameId);

        if (result != null)
            return !result.Value
                ? CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.NotFound);
    }



}
