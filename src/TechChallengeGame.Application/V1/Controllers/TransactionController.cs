using Microsoft.AspNetCore.Mvc;
using System.Net;
using TecChallenge.Application.Controllers;
using TecChallenge.Application.Extensions;
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
            IHistoryPaymentRepository historyPaymentRepository)
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
        public async Task<ActionResult<Root<HistoryPaymentResponse>>> Purchase(PurchaseRequest request)
        {
            if (!ModelState.IsValid)
            {
                return CustomModelStateResponse<HistoryPaymentResponse>(ModelState);
            }

            // A lógica de negócio foi movida para o serviço
            var historyPayment = await _transactionService.CreatePurchaseAsync(request);

            if (historyPayment is null)
            {
                // O serviço já notificou o erro, então apenas retornamos BadRequest
                return CustomResponse<HistoryPaymentResponse>(statusCode: HttpStatusCode.BadRequest);
            }

            return CustomResponse(data: historyPayment.MapToDto(), statusCode: HttpStatusCode.Created);
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
            var historyPayment = await _historyPaymentRepository.FirstOrDefaultAsync(x => x.Id == id, includes: x => x.TransactionGames);

            if (historyPayment is not null)
                return CustomResponse(data: historyPayment.MapToDto());

            NotifyError("Transaction not found");
            return CustomResponse<HistoryPaymentResponse>(statusCode: HttpStatusCode.NotFound);
        }
    }
}
