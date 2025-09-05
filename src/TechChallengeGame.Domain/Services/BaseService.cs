using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Domain.Notifications;

namespace TechChallengeGame.Domain.Services
{
    public abstract class BaseService(INotifier notifier)
    {
        private void Notify(FluentValidation.Results.ValidationResult validationResult)
        {
            foreach (var error in validationResult.Errors) Notify(error.ErrorMessage);
        }

        protected void Notify(string message)
        {
            notifier.Handle(new Notification(message));
        }

        protected bool ExecuteValidation<TV, TE>(TV validation, TE entity)
            where TV : AbstractValidator<TE>
            where TE : Entity
        {
            var validator = validation.Validate(entity);

            if (validator.IsValid)
                return true;

            Notify(validator);

            return false;
        }
    }
}
