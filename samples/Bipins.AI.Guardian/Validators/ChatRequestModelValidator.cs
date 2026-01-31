using Bipins.AI.Guardian.Models;
using FluentValidation;

namespace Bipins.AI.Guardian.Validators;

public class ChatRequestModelValidator : AbstractValidator<ChatRequestModel>
{
    public ChatRequestModelValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message cannot be empty")
            .MinimumLength(1).WithMessage("Message must have at least 1 character")
            .MaximumLength(1000).WithMessage("Message cannot exceed 1000 characters")
            .Must(BeSafeContent).WithMessage("Message contains potentially unsafe content");
    }

    private bool BeSafeContent(string message)
    {
        // Simple validation - reject obviously unsafe content
        var unsafeKeywords = new[] { "hack", "exploit", "bypass" };
        return !unsafeKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
