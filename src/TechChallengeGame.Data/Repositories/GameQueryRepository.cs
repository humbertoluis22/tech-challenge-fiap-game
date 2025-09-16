using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Shared.Models.Dtos.Responses;
using Elastic.Clients.Elasticsearch.QueryDsl;

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
                    )
                )
            );
            return response.Documents;
        }

        public async Task<IEnumerable<GameResponse>> GetRecommendationsAsync(Guid gameId)
        {
            var response = await _elasticClient.SearchAsync<GameResponse>(s => s
                .Query(q => q
                    .MoreLikeThis(new MoreLikeThisQuery
                    {
                        Like = new List<Like>
                        {
                    new Like(new LikeDocument
                    {
                        Index = "games",
                        Id = gameId.ToString()
                    })
                        },
                        Fields = new Field[]
                        {
                    "description",
                    "genre"
                        },
                        MinTermFreq = 1,
                        MaxQueryTerms = 12
                    })
                )
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
                    return termsAgg.Buckets.ToDictionary(bucket => bucket.Key, bucket => bucket.DocCount);
                }
            }

            return null;
        }
    }
}