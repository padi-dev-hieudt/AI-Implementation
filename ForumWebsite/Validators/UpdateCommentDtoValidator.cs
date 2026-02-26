using FluentValidation;
using ForumWebsite.Models.DTOs.Comment;

namespace ForumWebsite.Validators
{
    public class UpdateCommentDtoValidator : AbstractValidator<UpdateCommentDto>
    {
        public UpdateCommentDtoValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required.")
                .MinimumLength(2).WithMessage("Comment must be at least 2 characters.")
                .MaximumLength(5000).WithMessage("Comment cannot exceed 5 000 characters.");
        }
    }
}
