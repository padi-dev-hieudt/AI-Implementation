using FluentAssertions;
using FluentValidation.TestHelper;
using ForumWebsite.Models.DTOs.Comment;
using ForumWebsite.Validators;

namespace ForumWebsite.Tests.Validators;

public class CommentDtoValidatorTests
{
    // ── CreateCommentDtoValidator ──────────────────────────────────────────────

    public class CreateCommentDtoValidatorTests
    {
        private readonly CreateCommentDtoValidator _sut = new();

        private static CreateCommentDto Valid() => new()
        {
            PostId  = 1,
            Content = "A valid comment."
        };

        // PostId
        [Fact]
        public void PostId_Zero_FailsGreaterThanRule()
        {
            var dto = Valid(); dto.PostId = 0;
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.PostId)
                .WithErrorMessage("A valid post ID is required.");
        }

        [Fact]
        public void PostId_Negative_FailsGreaterThanRule()
        {
            var dto = Valid(); dto.PostId = -5;
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.PostId)
                .WithErrorMessage("A valid post ID is required.");
        }

        [Fact]
        public void PostId_Positive_Passes()
        {
            var dto = Valid();
            _sut.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.PostId);
        }

        // Content
        [Fact]
        public void Content_Empty_FailsWithRequiredMessage()
        {
            var dto = Valid(); dto.Content = string.Empty;
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Comment content is required.");
        }

        [Fact]
        public void Content_OneChar_FailsMinLength()
        {
            var dto = Valid(); dto.Content = "x";
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Comment must be at least 2 characters.");
        }

        [Fact]
        public void Content_5001Chars_FailsMaxLength()
        {
            var dto = Valid(); dto.Content = new string('a', 5001);
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Comment cannot exceed 5 000 characters.");
        }

        [Fact]
        public void ValidDto_PassesAllRules()
        {
            _sut.TestValidate(Valid()).IsValid.Should().BeTrue();
        }
    }

    // ── UpdateCommentDtoValidator ──────────────────────────────────────────────

    public class UpdateCommentDtoValidatorTests
    {
        private readonly UpdateCommentDtoValidator _sut = new();

        private static UpdateCommentDto Valid() => new() { Content = "Updated content." };

        [Fact]
        public void Content_Empty_FailsWithRequiredMessage()
        {
            var dto = Valid(); dto.Content = string.Empty;
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Comment content is required.");
        }

        [Fact]
        public void Content_OneChar_FailsMinLength()
        {
            var dto = Valid(); dto.Content = "x";
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Comment must be at least 2 characters.");
        }

        [Fact]
        public void Content_5001Chars_FailsMaxLength()
        {
            var dto = Valid(); dto.Content = new string('a', 5001);
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Comment cannot exceed 5 000 characters.");
        }

        [Fact]
        public void ValidDto_PassesAllRules()
        {
            _sut.TestValidate(Valid()).IsValid.Should().BeTrue();
        }
    }
}
