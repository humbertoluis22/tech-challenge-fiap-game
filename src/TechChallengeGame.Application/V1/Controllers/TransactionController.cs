using System.Net;
using Microsoft.AspNetCore.Mvc;
using TecChallenge.Application.Controllers;
using TecChallenge.Application.Extensions;
using TechChallengeGame.Application.Middlewares;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Shared.Models.Dtos.Requests;
using TechChallengeGame.Shared.Models.Dtos.Responses;
using TechChallengeGame.Shared.Models.Generics;

namespace TechChallengeGame.Application.V1.Controllers
{
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/transactions")]
    [Produces("application/json")]
    public class TransactionController : MainController
    {
        private readonly ITransactionService _transactionService;
        private readonly IHistoryPaymentRepository _historyPaymentRepository;

        public TransactionController(
            INotifier notifier,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment webHostEnvironment,
            ITransactionService transactionService,
            IHistoryPaymentRepository historyPaymentRepository
        )
            : base(notifier, httpContextAccessor, webHostEnvironment)
        {
            _transactionService = transactionService;
            _historyPaymentRepository = historyPaymentRepository;
        }

        /// <summary>
        /// Creates a new purchase transaction.
        /// </summary>
        /// <param name="request">The purchase request details.</param>
        /// <returns>The created transaction.</returns>
        [HttpPost("purchase")]
        [ProducesResponseType(typeof(Root<HistoryPaymentResponse>), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(Root<HistoryPaymentResponse>), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<Root<HistoryPaymentResponse>>> Purchase(
            PurchaseRequest request
        )
        {
            if (!ModelState.IsValid)
            {
                return CustomModelStateResponse<HistoryPaymentResponse>(ModelState);
            }

            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(
                    new { Message = "Token JWT válido é obrigatório para gerenciar usuários" }
                );
            }

            // Extrair informações do JWT usando os extensions methods
            var userId = HttpContext.GetUserId();

            var historyPayment = await _transactionService.CreatePurchaseAsync(userId, request);

            if (historyPayment is null)
            {
                return CustomResponse<HistoryPaymentResponse>(
                    statusCode: HttpStatusCode.BadRequest
                );
            }

            return CustomResponse(
                data: historyPayment.MapToDto(),
                statusCode: HttpStatusCode.Created
            );
        }

        /// <summary>
        /// Gets a transaction by its ID.
        /// </summary>
        /// <param name="id">The transaction ID.</param>
        /// <returns>The transaction details.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Root<HistoryPaymentResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Root<HistoryPaymentResponse>), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<Root<HistoryPaymentResponse>>> GetById(Guid id)
        {
            var historyPayment = await _historyPaymentRepository.FirstOrDefaultAsync(
                x => x.Id == id,
                includes: x => x.TransactionGames
            );

            if (historyPayment is not null)
                return CustomResponse(data: historyPayment.MapToDto());

            NotifyError("Transaction not found");
            return CustomResponse<HistoryPaymentResponse>(statusCode: HttpStatusCode.NotFound);
        }

        // Adicione este novo endpoint dentro da classe TransactionController
        /// <summary>
        /// Creates a new refund transaction for a game.
        /// </summary>
        /// <param name="request">The refund request details.</param>
        /// <returns>The created refund transaction.</returns>
        [HttpPost("refund")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)] // Resposta de sucesso agora é 202
        [ProducesResponseType(typeof(Root<object>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(Root<object>), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<Root<object>>> Refund(RefundRequest request) // O tipo de retorno genérico pode ser <object>
        {
            if (!ModelState.IsValid)
            {
                return CustomModelStateResponse<object>(ModelState);
            }
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(
                    new { Message = "Token JWT válido é obrigatório para gerenciar usuários" }
                );
            }

            // Extrair informações do JWT usando os extensions methods
            var userId = HttpContext.GetUserId();

            var result = await _transactionService.CreateRefundAsync(userId, request);

            if (!result)
            {
                // Se o serviço retornou 'false', significa que uma notificação de erro foi adicionada.
                return CustomResponse<object>(statusCode: HttpStatusCode.BadRequest);
            }

            // Retorna 202 Accepted sem corpo de resposta, indicando que a solicitação foi aceita.
            return StatusCode(
                (int)HttpStatusCode.Accepted,
                new Root<object> { StatusCode = 202, Success = true }
            );
        }
    }
}
