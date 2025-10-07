using FluentValidation;

namespace TechChallengeGame.Domain.Entities.validations
{
    public class UserLibraryValidation : AbstractValidator<UserLibrary>
    {
        public UserLibraryValidation()
        {
            RuleFor(c => c.UserId)
                .NotEmpty()
                .WithMessage("The {PropertyName} field needs to be supplied");
        }
    }
}
