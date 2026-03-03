using FluentAssertions;
using FluentValidation.TestHelper;
using ForumWebsite.Models.DTOs.Post;
using ForumWebsite.Validators;

namespace ForumWebsite.Tests.Validators;

public class PostDtoValidatorTests
{
    // ── CreatePostDtoValidator ─────────────────────────────────────────────────

    public class CreatePostDtoValidatorTests
    {
        private readonly CreatePostDtoValidator _sut = new();

        private static CreatePostDto Valid() => new()
        {
            Title   = "A valid forum title",
            Content = "Content that is long enough."
        };

        // Title
        [Fact]
        public void Title_Empty_FailsWithRequiredMessage()
        {
            var dto = Valid(); dto.Title = string.Empty;
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required.");
        }

        [Fact]
        public void Title_FourChars_FailsMinLength()
        {
            var dto = Valid(); dto.Title = "abcd";
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must be at least 5 characters.");
        }

        [Fact]
        public void Title_301Chars_FailsMaxLength()
        {
            var dto = Valid(); dto.Title = new string('x', 301);
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title cannot exceed 300 characters.");
        }

        // Content
        [Fact]
        public void Content_Empty_FailsWithRequiredMessage()
        {
            var dto = Valid(); dto.Content = string.Empty;
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Content is required.");
        }

        [Fact]
        public void Content_NineChars_FailsMinLength()
        {
            var dto = Valid(); dto.Content = "123456789";
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Content must be at least 10 characters.");
        }

        [Fact]
        public void ValidDto_PassesAllRules()
        {
            _sut.TestValidate(Valid()).IsValid.Should().BeTrue();
        }
    }

    // ── UpdatePostDtoValidator ─────────────────────────────────────────────────

    public class UpdatePostDtoValidatorTests
    {
        private readonly UpdatePostDtoValidator _sut = new();

        private static UpdatePostDto Valid() => new()
        {
            Title   = "Updated forum title",
            Content = "Updated content that is definitely long enough."
        };

        [Fact]
        public void Title_Empty_FailsWithRequiredMessage()
        {
            var dto = Valid(); dto.Title = string.Empty;
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required.");
        }

        [Fact]
        public void Title_FourChars_FailsMinLength()
        {
            var dto = Valid(); dto.Title = "abcd";
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must be at least 5 characters.");
        }

        [Fact]
        public void Title_301Chars_FailsMaxLength()
        {
            var dto = Valid(); dto.Title = new string('x', 301);
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title cannot exceed 300 characters.");
        }

        [Fact]
        public void Content_Empty_FailsWithRequiredMessage()
        {
            var dto = Valid(); dto.Content = string.Empty;
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Content is required.");
        }

        [Fact]
        public void Content_NineChars_FailsMinLength()
        {
            var dto = Valid(); dto.Content = "123456789";
            _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Content)
                .WithErrorMessage("Content must be at least 10 characters.");
        }

        [Fact]
        public void ValidDto_PassesAllRules()
        {
            _sut.TestValidate(Valid()).IsValid.Should().BeTrue();
        }
    }
}
