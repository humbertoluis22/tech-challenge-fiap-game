using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl; // Garanta que esta linha exista!
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Shared.Models.Dtos.Responses;

namespace TechChallengeGame.Data.Repositories
{
    public class GameQueryRepository : IGameQueryRepository
    {
        private readonly ElasticsearchClient _elasticClient;

        public GameQueryRepository(ElasticsearchClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task<GameResponse> FindByIdAsync(Guid id)
        {
            var response = await _elasticClient.GetAsync<GameResponse>(id.ToString());
            return response.Source;
        }

        public async Task<IEnumerable<GameResponse>> SearchAsync(string query)
        {
            var response = await _elasticClient.SearchAsync<GameResponse>(s => s
                .Query(q => q
                    .MultiMatch(mm => mm
                        .Query(query)
                        .Fields(new[] { "name", "description" })
                        .Fuzziness(new Fuzziness("AUTO")) 
                        .Operator(Operator.And)
                    )
                )
            );
            return response.Documents;
        }

        public async Task<IEnumerable<GameResponse>> GetRecommendationsAsync(Guid gameId)
        {
            // ETAPA 1: Buscar o jogo original para descobrir seu gênero.
            var originalGameResponse = await _elasticClient.GetAsync<GameResponse>(gameId.ToString());

            // Se o jogo original não for encontrado, retorna uma lista vazia.
            if (!originalGameResponse.Found || string.IsNullOrWhiteSpace(originalGameResponse.Source.Genre))
            {
                return Enumerable.Empty<GameResponse>();
            }

            var originalGameGenre = originalGameResponse.Source.Genre;

            // ETAPA 2: Buscar outros jogos com o mesmo gênero, excluindo o original.
            var response = await _elasticClient.SearchAsync<GameResponse>(s => s
                .Query(q => q
                    .Bool(b => b
                        // O gênero DEVE ser o mesmo.
                        .Filter(f => f
                            .Term(t => t
                                .Field("genre.keyword")
                                .Value(originalGameGenre)
                            )
                        )
                        // O ID NÃO PODE ser o do jogo original.
                        .MustNot(mn => mn
                            .Ids(i => i
                                .Values(gameId.ToString())
                            )
                        )
                    )
                )
                .Size(5) // Limita a 5 recomendações
            );

            return response.Documents;
        }

        public async Task<object> GetGenreAggregationsAsync()
        {
            var response = await _elasticClient.SearchAsync<GameResponse>(s => s
                .Size(0)
                .Aggregations(a => a
                    .Terms("genres", t => t
                        .Field("genre.keyword")
                        .Size(20)
                    )
                )
            );

            if (response.IsValidResponse && response.Aggregations.TryGetValue("genres", out var aggregation))
            {
                if (aggregation is StringTermsAggregate termsAgg)
                {
                    // CORREÇÃO AQUI: Convertemos a chave para string com .ToString()
                    var result = termsAgg.Buckets.ToDictionary(
                        bucket => bucket.Key.ToString(), 
                        bucket => bucket.DocCount
                    );
                    return result;
                }
            }
            return null;
        }
    }
}