using FluentAssertions;
using FluentValidation.TestHelper;
using ForumWebsite.Models.DTOs.User;
using ForumWebsite.Validators;

namespace ForumWebsite.Tests.Validators;

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _sut = new();

    // ── Helper ─────────────────────────────────────────────────────────────────

    private static RegisterDto Valid() => new()
    {
        Username        = "alice_99",
        Email           = "alice@example.com",
        Password        = "Password1!",
        ConfirmPassword = "Password1!"
    };

    // ── Username ───────────────────────────────────────────────────────────────

    [Fact]
    public void Username_Empty_FailsWithRequiredMessage()
    {
        var dto = Valid(); dto.Username = string.Empty;
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username is required.");
    }

    [Fact]
    public void Username_TwoChars_FailsMinLength()
    {
        var dto = Valid(); dto.Username = "ab";
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at least 3 characters.");
    }

    [Fact]
    public void Username_51Chars_FailsMaxLength()
    {
        var dto = Valid(); dto.Username = new string('a', 51);
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username cannot exceed 50 characters.");
    }

    [Theory]
    [InlineData("alice 99")]    // space
    [InlineData("alice-99")]    // hyphen
    [InlineData("alice@99")]    // @
    public void Username_InvalidChars_FailsRegex(string username)
    {
        var dto = Valid(); dto.Username = username;
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username can only contain letters, numbers, and underscores.");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("user_123")]
    [InlineData("Alice")]
    public void Username_ValidVariants_Passes(string username)
    {
        var dto = Valid(); dto.Username = username;
        _sut.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    // ── Email ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Email_Empty_FailsWithRequiredMessage()
    {
        var dto = Valid(); dto.Email = string.Empty;
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void Email_InvalidFormat_FailsEmailValidation(string email)
    {
        var dto = Valid(); dto.Email = email;
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("A valid email address is required.");
    }

    [Fact]
    public void Email_101Chars_FailsMaxLength()
    {
        var dto = Valid(); dto.Email = new string('a', 95) + "@x.com";   // 101 chars
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email cannot exceed 100 characters.");
    }

    // ── Password ───────────────────────────────────────────────────────────────

    [Fact]
    public void Password_Empty_FailsWithRequiredMessage()
    {
        var dto = Valid(); dto.Password = string.Empty; dto.ConfirmPassword = string.Empty;
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Fact]
    public void Password_FiveChars_FailsMinLength()
    {
        var dto = Valid(); dto.Password = "Abc1!"; dto.ConfirmPassword = "Abc1!";
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 6 characters.");
    }

    [Fact]
    public void Password_NoUppercase_FailsUppercaseRule()
    {
        var dto = Valid(); dto.Password = "password1!"; dto.ConfirmPassword = "password1!";
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Fact]
    public void Password_NoDigit_FailsDigitRule()
    {
        var dto = Valid(); dto.Password = "Password!"; dto.ConfirmPassword = "Password!";
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one digit.");
    }

    [Fact]
    public void Password_ValidStrong_Passes()
    {
        var dto = Valid();
        _sut.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    // ── ConfirmPassword ────────────────────────────────────────────────────────

    [Fact]
    public void ConfirmPassword_Mismatch_FailsEqualRule()
    {
        var dto = Valid(); dto.ConfirmPassword = "Different1!";
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Passwords do not match.");
    }

    [Fact]
    public void ConfirmPassword_Matching_Passes()
    {
        var dto = Valid();
        _sut.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    // ── Full valid object ──────────────────────────────────────────────────────

    [Fact]
    public void ValidDto_PassesAllRules()
    {
        _sut.TestValidate(Valid()).IsValid.Should().BeTrue();
    }
}
