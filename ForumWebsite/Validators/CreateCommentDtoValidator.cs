using FluentValidation;
using ForumWebsite.Models.DTOs.Comment;

namespace ForumWebsite.Validators
{
    public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
    {
        public CreateCommentDtoValidator()
        {
            RuleFor(x => x.PostId)
                .GreaterThan(0).WithMessage("A valid post ID is required.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required.")
                .MinimumLength(2).WithMessage("Comment must be at least 2 characters.")
                .MaximumLength(5000).WithMessage("Comment cannot exceed 5 000 characters.");
        }
    }
}
