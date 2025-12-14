using System.Net;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Mvc;
using TecChallenge.Application.Controllers;
using TecChallenge.Application.Extensions;
using TechChallengeGame.Application.Middlewares;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Shared.Models.Dtos.Requests;
using TechChallengeGame.Shared.Models.Dtos.Responses;
using TechChallengeGame.Shared.Models.Generics;

namespace TechChallengeGame.Application.V1.Controllers;

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
    /// <returns>The user's library containing their game collection</returns>
    /// <response code="200">Returns the user's game library</response>
    /// <response code="404">User library not found</response>
    [HttpGet()]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserLibraryResponse>>> GetUserLibrary()  
    {
        if (!HttpContext.IsAuthenticated())
        {
           return Unauthorized(
               new { Message = "Token JWT v�lido � obrigat�rio para gerenciar usu�rios" }
           );
        }

        // Extrair informa��es do JWT usando os extensions methods
        var userId_string = HttpContext.GetUserId();
        var userId = Guid.Parse(userId_string);

        logger.LogInformation("Obtendo biblioteca do usu�rio do banco de dados.");
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
    /// <returns>Create user library</returns>
    /// <response code="200">Game successfully added to user's library</response>
    /// <response code="404">User library not found</response>
    [HttpPost()]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserLibraryResponse>>> CreateALibraryForUser()
    {
        if (!HttpContext.IsAuthenticated())
        {
           return Unauthorized(
               new { Message = "Token JWT v�lido � obrigat�rio para gerenciar usu�rios" }
           );
        }

        // Extrair informa��es do JWT usando os extensions methods
        var userId_string = HttpContext.GetUserId();
        var userId = Guid.Parse(userId_string);

        logger.LogInformation("Criando biblioteca do usu�rio no banco de dados.");
        var userLibrary = UserLibrary.Create(userId);
        var result = await userLibraryService.AddAsync(userLibrary);

        return result
            ? CustomResponse(data: userLibrary.MapToDto(), statusCode: HttpStatusCode.Created)
            : CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// add game for user
    /// </summary>
    /// <returns>No content if successful</returns>
    /// <response code="200">Game successfully added to user's library</response>
    /// <response code="404">User library not found</response>
    [HttpPut()]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserLibraryResponse>>> AddGameToLibrary(
        AddGameToLibraryRequest resquest
    )
    {
        if (!HttpContext.IsAuthenticated())
        {
           return Unauthorized(
               new { Message = "Token JWT v�lido � obrigat�rio para gerenciar usu�rios" }
           );
        }

        // Extrair informa��es do JWT usando os extensions methods
        var userId_string = HttpContext.GetUserId();
        var userId = Guid.Parse(userId_string);

        logger.LogInformation("Adicionando jogo � biblioteca do usu�rio no banco de dados.");
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
    /// <param name="gameId">The unique identifier of the Game</param>
    /// <returns>No content if successful</returns>
    /// <response code="200">Game successfully added to user's library</response>
    /// <response code="404">User library not found</response>
    [HttpDelete()]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserLibraryResponse>>> RemoveGameFromLibrary(Guid gameId)
    {
        if (!HttpContext.IsAuthenticated())
        {
           return Unauthorized(
               new { Message = "Token JWT v�lido � obrigat�rio para gerenciar usu�rios" }
           );
        }

        // Extrair informa��es do JWT usando os extensions methods
        var userId_string = HttpContext.GetUserId();
        var userId = Guid.Parse(userId_string);

        logger.LogInformation("Removendo jogo da biblioteca do usu�rio no banco de dados.");
        var result = await userLibraryService.DeleteGameForUser(userId, gameId);

        if (result != null)
            return !result.Value
                ? CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.NotFound);
    }
}
