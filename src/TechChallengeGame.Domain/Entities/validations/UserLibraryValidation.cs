using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
