namespace TechChallengeGame.Shared.Models.Dtos.Responses
{
    public class UserLibraryResponse
    {
        public Guid Id { get; set; }

        // public Guid UserId { get; set; }

        public IEnumerable<LibraryItemReponse> Items { get; set; } = [];
    }

}
