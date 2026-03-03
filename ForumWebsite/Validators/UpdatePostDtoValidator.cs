using FluentValidation;
using ForumWebsite.Models.DTOs.Post;
using System.Text.RegularExpressions;

namespace ForumWebsite.Validators
{
    public class UpdatePostDtoValidator : AbstractValidator<UpdatePostDto>
    {
        public UpdatePostDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MinimumLength(5).WithMessage("Title must be at least 5 characters.")
                .MaximumLength(300).WithMessage("Title cannot exceed 300 characters.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required.")
                // Raw HTML can be much longer than visible text; cap the HTML payload
                .Must(c => c.Length <= 200_000).WithMessage("Content is too large.")
                // Strip tags to check visible text length
                .Must(c => StripHtml(c).Trim().Length >= 10)
                    .WithMessage("Content must be at least 10 characters.");
        }

        private static string StripHtml(string html) =>
            Regex.Replace(html ?? string.Empty, "<[^>]+>", string.Empty);
    }
}
