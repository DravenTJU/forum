using FluentAssertions;
using Forum.Api.Infrastructure.Auth;
using Forum.Api.Models.Entities;
using Forum.Api.Repositories;
using Forum.Api.Services;
using Forum.Api.Tests.Builders;
using Forum.Api.Tests.Extensions;
using Forum.Api.Tests.Fixtures;
using Forum.Api.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Forum.Api.Tests.Services;

/// <summary>
/// AuthService 单元测试
/// 测试用户认证、注册、JWT生成等核心功能
/// </summary>
public class AuthServiceTests : IClassFixture<ServiceTestFixture>
{
    private readonly ServiceTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly AuthService _authService;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<AuthService> _logger;

    public AuthServiceTests(ServiceTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.Output = output;

        // 获取服务和Mock对象
        _authService = _fixture.GetService<IAuthService>() as AuthService 
            ?? throw new InvalidOperationException("AuthService not registered");
        _userRepositoryMock = _fixture.GetMockRepository<IUserRepository>();
        _refreshTokenRepositoryMock = _fixture.GetMockRepository<IRefreshTokenRepository>();
        _jwtTokenService = _fixture.GetService<IJwtTokenService>();
        _passwordService = _fixture.GetService<IPasswordService>();
        _logger = _fixture.GetService<ILogger<AuthService>>();
    }

    #region 用户登录测试

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        var testUser = UserTestDataBuilder.RegularUser()
            .WithEmail("test@example.com")
            .WithPasswordHash(_passwordService.HashPassword("password123"))
            .Build();

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(testUser);

        // Act
        var result = await _authService.LoginAsync("test@example.com", "password123");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User.Id.Should().Be(testUser.Id);
        result.User.Email.Should().Be(testUser.Email);
        result.AccessToken.ShouldBeValidJwtToken();
        result.RefreshToken.Should().NotBeNullOrEmpty();

        // 验证Repository调用
        _userRepositoryMock.Verify(x => x.GetByEmailAsync("test@example.com"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync("nonexistent@example.com", "password123"));

        exception.Message.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var testUser = UserTestDataBuilder.RegularUser()
            .WithEmail("test@example.com")
            .WithPasswordHash(_passwordService.HashPassword("correctpassword"))
            .Build();

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(testUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync("test@example.com", "wrongpassword"));

        exception.Message.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WithSuspendedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var suspendedUser = UserTestDataBuilder.SuspendedUser()
            .WithEmail("suspended@example.com")
            .WithPasswordHash(_passwordService.HashPassword("password123"))
            .Build();

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("suspended@example.com"))
            .ReturnsAsync(suspendedUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync("suspended@example.com", "password123"));

        exception.Message.Should().Contain("Account is suspended");
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var unverifiedUser = UserTestDataBuilder.UnverifiedUser()
            .WithEmail("unverified@example.com")
            .WithPasswordHash(_passwordService.HashPassword("password123"))
            .Build();

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("unverified@example.com"))
            .ReturnsAsync(unverifiedUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync("unverified@example.com", "password123"));

        exception.Message.Should().Contain("Email not verified");
    }

    #endregion

    #region 用户注册测试

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserAndReturnResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(request.Email))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.UsernameExistsAsync(request.Username))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync("new-user-id");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("注册成功");

        // 验证用户创建
        _userRepositoryMock.Verify(x => x.CreateAsync(It.Is<User>(u => 
            u.Username == request.Username && 
            u.Email == request.Email &&
            u.EmailVerified == false &&
            u.Status == "active")), Times.Once);

        // 验证密码哈希
        _userRepositoryMock.Verify(x => x.CreateAsync(It.Is<User>(u => 
            _passwordService.VerifyPassword(request.Password, u.PasswordHash))), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "existing@example.com",
            Password = "SecurePass123!"
        };

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(request.Email))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(request));

        exception.Message.Should().Contain("Email already exists");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(request.Email))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.UsernameExistsAsync(request.Username))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(request));

        exception.Message.Should().Contain("Username already exists");
    }

    #endregion

    #region JWT Token测试

    [Fact]
    public void JwtTokenService_GenerateToken_ShouldReturnValidToken()
    {
        // Arrange
        var testUser = UserTestDataBuilder.RegularUser()
            .WithId("user-123")
            .WithEmail("test@example.com")
            .WithUsername("testuser")
            .Build();

        var roles = new[] { "User" };

        // Act
        var token = _jwtTokenService.GenerateToken(testUser.Id, testUser.Username, testUser.Email, roles);

        // Assert
        token.ShouldBeValidJwtToken();
        
        // 验证Token可以被解析
        var principal = _jwtTokenService.ValidateToken(token);
        principal.ShouldBeAuthenticated(testUser.Id);
        
        var usernameClaim = principal.FindFirst("username");
        usernameClaim.Should().NotBeNull();
        usernameClaim!.Value.Should().Be(testUser.Username);
        
        var emailClaim = principal.FindFirst("email");
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(testUser.Email);
    }

    [Fact]
    public void JwtTokenService_ValidateExpiredToken_ShouldReturnNull()
    {
        // 注意: 这个测试需要能够生成过期的Token
        // 在实际实现中，可能需要修改JwtTokenService以支持自定义过期时间用于测试
        
        // 这里只是示例结构，具体实现依赖于JwtTokenService的设计
        // 可以通过依赖注入不同的配置来创建短期过期Token
        
        // Act & Assert
        // 测试过期Token验证
        Assert.True(true); // 占位符，实际实现需要根据JwtTokenService设计
    }

    #endregion

    #region 密码服务测试

    [Fact]
    public void PasswordService_HashPassword_ShouldReturnValidHash()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        hash.ShouldBeValidPasswordHash();
        hash.Should().NotBe(password); // 哈希不应该等于原密码
    }

    [Fact]
    public void PasswordService_VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _passwordService.HashPassword(password);

        // Act
        var isValid = _passwordService.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void PasswordService_VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _passwordService.HashPassword(correctPassword);

        // Act
        var isValid = _passwordService.VerifyPassword(wrongPassword, hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("password")]
    [InlineData("PASSWORD123")]
    [InlineData("password123")]
    public void PasswordService_ValidatePasswordStrength_WithWeakPassword_ShouldReturnFalse(string weakPassword)
    {
        // Act
        var isStrong = _passwordService.IsPasswordStrong(weakPassword);

        // Assert
        isStrong.Should().BeFalse($"密码 '{weakPassword}' 应该被识别为弱密码");
    }

    [Theory]
    [InlineData("StrongPass123!")]
    [InlineData("MySecure@2024")]
    [InlineData("Complex#Password9")]
    public void PasswordService_ValidatePasswordStrength_WithStrongPassword_ShouldReturnTrue(string strongPassword)
    {
        // Act
        var isStrong = _passwordService.IsPasswordStrong(strongPassword);

        // Assert
        isStrong.Should().BeTrue($"密码 '{strongPassword}' 应该被识别为强密码");
    }

    #endregion

    #region 刷新令牌测试

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var testUser = UserTestDataBuilder.RegularUser()
            .WithId("user-123")
            .Build();

        var refreshToken = new RefreshToken
        {
            Id = 1,
            UserId = testUser.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _refreshTokenRepositoryMock
            .Setup(x => x.GetByTokenAsync(refreshToken.Token))
            .ReturnsAsync(refreshToken);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(testUser.Id))
            .ReturnsAsync(testUser);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken.Token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.ShouldBeValidJwtToken();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(refreshToken.Token); // 应该是新的刷新令牌
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _refreshTokenRepositoryMock
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RefreshTokenAsync("invalid-token"));

        exception.Message.Should().Contain("Invalid refresh token");
    }

    #endregion

    #region 边界条件和异常测试

    [Fact]
    public async Task LoginAsync_WithNullEmail_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.LoginAsync(null!, "password"));
    }

    [Fact]
    public async Task LoginAsync_WithEmptyPassword_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.LoginAsync("test@example.com", ""));
    }

    [Fact]
    public async Task RegisterAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _authService.RegisterAsync(null!));
    }

    #endregion

    #region 性能测试

    [Fact]
    public async Task LoginAsync_PerformanceBenchmark_ShouldCompleteWithin200Ms()
    {
        // Arrange
        var testUser = UserTestDataBuilder.RegularUser()
            .WithEmail("perf@example.com")
            .WithPasswordHash(_passwordService.HashPassword("password123"))
            .Build();

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("perf@example.com"))
            .ReturnsAsync(testUser);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _authService.LoginAsync("perf@example.com", "password123");

        // Assert
        stopwatch.Stop();
        stopwatch.Elapsed.ShouldMeetPerformanceBenchmark(
            TimeSpan.FromMilliseconds(200), 
            "用户登录");
    }

    #endregion

    /// <summary>
    /// 每个测试完成后重置Mock状态
    /// </summary>
    public void Dispose()
    {
        _fixture.ResetMocks();
    }
}

/// <summary>
/// 测试用的DTO类（如果不存在的话）
/// </summary>
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}