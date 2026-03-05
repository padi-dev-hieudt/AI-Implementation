using AutoMapper;
using FluentAssertions;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.User;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Implementations;
using ForumWebsite.Services.Interfaces;
using ForumWebsite.Tests.Helpers;
using Moq;

namespace ForumWebsite.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repoMock  = new();
    private readonly Mock<IJwtService>     _jwtMock   = new();

    private UserService CreateSut() => new(_repoMock.Object, _jwtMock.Object);

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_UniqueCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username        = "newuser",
            Email           = "new@example.com",
            Password        = "Password1!",
            ConfirmPassword = "Password1!"
        };

        _repoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>()))    .ReturnsAsync(false);
        _repoMock.Setup(r => r.UsernameExistsAsync(It.IsAny<string>())) .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                 .ReturnsAsync((User u) => u);

        _jwtMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("jwt.token");
        _jwtMock.Setup(j => j.GetTokenExpiry()).Returns(DateTime.UtcNow.AddHours(24));

        // Act
        var result = await CreateSut().RegisterAsync(dto);

        // Assert
        result.Username .Should().Be("newuser");
        result.Email    .Should().Be("new@example.com");
        result.Role     .Should().Be(UserRoles.User);
        result.Token    .Should().Be("jwt.token");
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmailToLowercase()
    {
        // Arrange
        User? captured = null;

        _repoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>()))    .ReturnsAsync(false);
        _repoMock.Setup(r => r.UsernameExistsAsync(It.IsAny<string>())) .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                 .Callback<User>(u => captured = u)
                 .ReturnsAsync((User u) => u);

        _jwtMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("t");
        _jwtMock.Setup(j => j.GetTokenExpiry()).Returns(DateTime.UtcNow.AddHours(1));

        var dto = new RegisterDto
        {
            Username = "user", Email = "USER@EXAMPLE.COM",
            Password = "P1!", ConfirmPassword = "P1!"
        };

        // Act
        await CreateSut().RegisterAsync(dto);

        // Assert
        captured!.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task RegisterAsync_TrimsWhitespaceFromUsername()
    {
        User? captured = null;

        _repoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>()))    .ReturnsAsync(false);
        _repoMock.Setup(r => r.UsernameExistsAsync(It.IsAny<string>())) .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                 .Callback<User>(u => captured = u)
                 .ReturnsAsync((User u) => u);

        _jwtMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("t");
        _jwtMock.Setup(j => j.GetTokenExpiry()).Returns(DateTime.UtcNow.AddHours(1));

        var dto = new RegisterDto
        {
            Username = "  trimmed  ", Email = "a@b.com",
            Password = "P1!", ConfirmPassword = "P1!"
        };

        await CreateSut().RegisterAsync(dto);

        captured!.Username.Should().Be("trimmed");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsBusinessRuleException()
    {
        _repoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var dto = new RegisterDto
        {
            Username = "u", Email = "existing@example.com",
            Password = "P1!", ConfirmPassword = "P1!"
        };

        await CreateSut().Invoking(s => s.RegisterAsync(dto))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ThrowsBusinessRuleException()
    {
        _repoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>()))    .ReturnsAsync(false);
        _repoMock.Setup(r => r.UsernameExistsAsync(It.IsAny<string>())) .ReturnsAsync(true);

        var dto = new RegisterDto
        {
            Username = "existinguser", Email = "new@example.com",
            Password = "P1!", ConfirmPassword = "P1!"
        };

        await CreateSut().Invoking(s => s.RegisterAsync(dto))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*already taken*");
    }

    [Fact]
    public async Task RegisterAsync_SetsRoleToUser()
    {
        User? captured = null;

        _repoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>()))    .ReturnsAsync(false);
        _repoMock.Setup(r => r.UsernameExistsAsync(It.IsAny<string>())) .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                 .Callback<User>(u => captured = u)
                 .ReturnsAsync((User u) => u);

        _jwtMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("t");
        _jwtMock.Setup(j => j.GetTokenExpiry()).Returns(DateTime.UtcNow.AddHours(1));

        var dto = new RegisterDto
        {
            Username = "u", Email = "a@b.com",
            Password = "P1!", ConfirmPassword = "P1!"
        };

        await CreateSut().RegisterAsync(dto);

        captured!.Role    .Should().Be(UserRoles.User);
        captured!.IsActive.Should().BeTrue();
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsAuthResponse()
    {
        // Use workFactor:4 for fast tests
        const string password = "Password1!";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var user = TestDataFactory.CreateUser(email: "user@example.com", passwordHash: hash);

        _repoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);
        _jwtMock.Setup(j => j.GenerateToken(user)).Returns("jwt.token");
        _jwtMock.Setup(j => j.GetTokenExpiry()).Returns(DateTime.UtcNow.AddHours(24));

        var result = await CreateSut().LoginAsync(
            new LoginDto { Email = "user@example.com", Password = password });

        result.Username.Should().Be(user.Username);
        result.Role    .Should().Be(UserRoles.User);
        result.Token   .Should().Be("jwt.token");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsAuthenticationException()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword1!", workFactor: 4);
        var user = TestDataFactory.CreateUser(email: "user@example.com", passwordHash: hash);

        _repoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        await CreateSut().Invoking(s =>
                s.LoginAsync(new LoginDto
                    { Email = "user@example.com", Password = "WrongPassword1!" }))
            .Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsAuthenticationException()
    {
        // Returns null → service uses _dummyHash for constant-time comparison
        _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Note: this test is intentionally slow (~250 ms) because the service calls
        // BCrypt.Verify against _dummyHash (workFactor:12) to prevent timing attacks.
        await CreateSut().Invoking(s =>
                s.LoginAsync(new LoginDto
                    { Email = "nobody@example.com", Password = "Password1!" }))
            .Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsForbiddenException()
    {
        const string password = "Password1!";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var user = TestDataFactory.CreateUser(
            email: "disabled@example.com", passwordHash: hash, isActive: false);

        _repoMock.Setup(r => r.GetByEmailAsync("disabled@example.com")).ReturnsAsync(user);

        await CreateSut().Invoking(s =>
                s.LoginAsync(new LoginDto
                    { Email = "disabled@example.com", Password = password }))
            .Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*disabled*");
    }

    [Fact]
    public async Task LoginAsync_NormalizesEmailBeforeLookup()
    {
        const string password = "Password1!";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4);
        var user = TestDataFactory.CreateUser(email: "user@example.com", passwordHash: hash);

        _repoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);
        _jwtMock.Setup(j => j.GenerateToken(user)).Returns("t");
        _jwtMock.Setup(j => j.GetTokenExpiry()).Returns(DateTime.UtcNow.AddHours(1));

        // Email supplied with uppercase → should be normalised before the lookup
        var result = await CreateSut().LoginAsync(
            new LoginDto { Email = "USER@EXAMPLE.COM", Password = password });

        result.Should().NotBeNull();
        _repoMock.Verify(r => r.GetByEmailAsync("user@example.com"), Times.Once);
    }

    // ── GetProfileAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_ExistingUser_ReturnsProfileWithCounts()
    {
        var user = TestDataFactory.CreateUser(id: 7);

        _repoMock.Setup(r => r.GetByIdAsync(7))             .ReturnsAsync(user);
        _repoMock.Setup(r => r.GetActivityCountsAsync(7))   .ReturnsAsync((3, 8));

        var profile = await CreateSut().GetProfileAsync(7);

        profile.Id          .Should().Be(7);
        profile.Username    .Should().Be(user.Username);
        profile.Email       .Should().Be(user.Email);
        profile.Role        .Should().Be(UserRoles.User);
        profile.PostCount   .Should().Be(3);
        profile.CommentCount.Should().Be(8);
    }

    [Fact]
    public async Task GetProfileAsync_UnknownUserId_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await CreateSut().Invoking(s => s.GetProfileAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    // ── GetAllUsersAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsersAsync_ReturnsPagedAdminUserDtos()
    {
        var users = new List<User>
        {
            TestDataFactory.CreateUser(id: 1),
            TestDataFactory.CreateUser(id: 2)
        };

        _repoMock.Setup(r => r.GetAllPagedAsync(1, 20))
                 .ReturnsAsync(((IEnumerable<User>)users, 2));

        var result = await CreateSut().GetAllUsersAsync(1, 20);

        result.TotalCount.Should().Be(2);
        result.Page      .Should().Be(1);
        result.PageSize  .Should().Be(20);
        result.Items     .Should().HaveCount(2);
    }

    // ── ToggleUserActiveAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ToggleUserActiveAsync_ActiveUser_DeactivatesUser()
    {
        var user = TestDataFactory.CreateUser(id: 5, isActive: true);

        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);

        await CreateSut().ToggleUserActiveAsync(targetUserId: 5, requestingUserId: 1);

        user.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ToggleUserActiveAsync_InactiveUser_ReactivatesUser()
    {
        var user = TestDataFactory.CreateUser(id: 6, isActive: false);

        _repoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);

        await CreateSut().ToggleUserActiveAsync(targetUserId: 6, requestingUserId: 1);

        user.IsActive.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ToggleUserActiveAsync_SelfDeactivation_ThrowsBusinessRuleException()
    {
        // Admin cannot deactivate their own account
        await CreateSut().Invoking(s => s.ToggleUserActiveAsync(targetUserId: 1, requestingUserId: 1))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task ToggleUserActiveAsync_UnknownTargetUser_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await CreateSut().Invoking(s => s.ToggleUserActiveAsync(targetUserId: 999, requestingUserId: 1))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }
}
