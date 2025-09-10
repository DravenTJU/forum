using FluentAssertions;
using Forum.Api.Infrastructure.Auth;
using Forum.Api.Tests.Extensions;
using Forum.Api.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Forum.Api.Tests.Infrastructure;

/// <summary>
/// PasswordService 单元测试
/// 测试密码哈希、验证和强度检查功能
/// </summary>
public class PasswordServiceTests : IClassFixture<ServiceTestFixture>
{
    private readonly PasswordService _passwordService;
    private readonly ITestOutputHelper _output;

    public PasswordServiceTests(ServiceTestFixture fixture, ITestOutputHelper output)
    {
        _output = output;
        fixture.Output = output;
        _passwordService = fixture.GetService<IPasswordService>() as PasswordService 
            ?? throw new InvalidOperationException("PasswordService not registered");
    }

    #region 密码哈希测试

    [Fact]
    public void HashPassword_WithValidPassword_ShouldReturnValidHash()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        hash.ShouldBeValidPasswordHash();
        hash.Should().NotBe(password, "哈希值不应该等于原密码");
    }

    [Fact]
    public void HashPassword_WithSamePasswordTwice_ShouldReturnDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        hash1.ShouldBeValidPasswordHash();
        hash2.ShouldBeValidPasswordHash();
        hash1.Should().NotBe(hash2, "相同密码的两次哈希应该产生不同的结果（盐值不同）");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HashPassword_WithInvalidInput_ShouldThrowArgumentException(string? invalidPassword)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(invalidPassword!));
    }

    #endregion

    #region 密码验证测试

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "CorrectPassword123!";
        var hash = _passwordService.HashPassword(password);

        // Act
        var isValid = _passwordService.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue("正确的密码应该验证成功");
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _passwordService.HashPassword(correctPassword);

        // Act
        var isValid = _passwordService.VerifyPassword(wrongPassword, hash);

        // Assert
        isValid.Should().BeFalse("错误的密码应该验证失败");
    }

    [Theory]
    [InlineData("TestPassword123!", "")]
    [InlineData("TestPassword123!", " ")]
    [InlineData("TestPassword123!", null)]
    [InlineData("", "validhash")]
    [InlineData(null, "validhash")]
    public void VerifyPassword_WithInvalidInputs_ShouldReturnFalse(string? password, string? hash)
    {
        // Act
        var isValid = _passwordService.VerifyPassword(password!, hash!);

        // Assert
        isValid.Should().BeFalse("无效输入应该返回false");
    }

    [Fact]
    public void VerifyPassword_WithMalformedHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var malformedHash = "not-a-valid-bcrypt-hash";

        // Act
        var isValid = _passwordService.VerifyPassword(password, malformedHash);

        // Assert
        isValid.Should().BeFalse("格式错误的哈希应该验证失败");
    }

    #endregion

    #region 密码强度检查测试

    [Theory]
    [InlineData("StrongPassword123!", true)]
    [InlineData("MySecure@2024", true)]
    [InlineData("Complex#Pass9", true)]
    [InlineData("ValidP@ss1", true)]
    public void IsPasswordStrong_WithStrongPasswords_ShouldReturnTrue(string password, bool expected)
    {
        // Act
        var result = _passwordService.IsPasswordStrong(password);

        // Assert
        result.Should().Be(expected, $"密码 '{password}' 的强度检查应该返回 {expected}");
    }

    [Theory]
    [InlineData("", false)] // 空密码
    [InlineData("123", false)] // 太短
    [InlineData("password", false)] // 只有小写字母
    [InlineData("PASSWORD", false)] // 只有大写字母
    [InlineData("12345678", false)] // 只有数字
    [InlineData("!@#$%^&*", false)] // 只有特殊字符
    [InlineData("Password", false)] // 缺少数字和特殊字符
    [InlineData("password123", false)] // 缺少大写字母和特殊字符
    [InlineData("PASSWORD123", false)] // 缺少小写字母和特殊字符
    [InlineData("Password123", false)] // 缺少特殊字符
    [InlineData("Pass@123", false)] // 太短（小于8位）
    public void IsPasswordStrong_WithWeakPasswords_ShouldReturnFalse(string password, bool expected)
    {
        // Act
        var result = _passwordService.IsPasswordStrong(password);

        // Assert
        result.Should().Be(expected, $"密码 '{password}' 的强度检查应该返回 {expected}");
    }

    [Fact]
    public void IsPasswordStrong_WithNullPassword_ShouldReturnFalse()
    {
        // Act
        var result = _passwordService.IsPasswordStrong(null!);

        // Assert
        result.Should().BeFalse("null密码应该被认为是弱密码");
    }

    #endregion

    #region 性能测试

    [Fact]
    public void HashPassword_Performance_ShouldCompleteWithin500Ms()
    {
        // Arrange
        var password = "TestPassword123!";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        _passwordService.HashPassword(password);

        // Assert
        stopwatch.Stop();
        stopwatch.Elapsed.ShouldMeetPerformanceBenchmark(
            TimeSpan.FromMilliseconds(500), 
            "密码哈希操作");
    }

    [Fact]
    public void VerifyPassword_Performance_ShouldCompleteWithin100Ms()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _passwordService.HashPassword(password);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        _passwordService.VerifyPassword(password, hash);

        // Assert
        stopwatch.Stop();
        stopwatch.Elapsed.ShouldMeetPerformanceBenchmark(
            TimeSpan.FromMilliseconds(100), 
            "密码验证操作");
    }

    #endregion

    #region 安全性测试

    [Fact]
    public void HashPassword_MultipleThreads_ShouldBeThreadSafe()
    {
        // Arrange
        var password = "TestPassword123!";
        var tasks = new List<Task<string>>();
        var taskCount = 10;

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(() => _passwordService.HashPassword(password)));
        }

        var results = Task.WhenAll(tasks).GetAwaiter().GetResult();

        // Assert
        results.Should().HaveCount(taskCount);
        results.Should().OnlyContain(hash => !string.IsNullOrEmpty(hash), "所有哈希都应该有效");
        results.Should().OnlyHaveUniqueItems("每个哈希都应该是唯一的");
        
        foreach (var hash in results)
        {
            hash.ShouldBeValidPasswordHash();
            _passwordService.VerifyPassword(password, hash).Should().BeTrue("每个哈希都应该能验证原密码");
        }
    }

    [Fact]
    public void HashPassword_WithTimingAttack_ShouldHaveConsistentTiming()
    {
        // 这个测试检查是否存在时序攻击漏洞
        // 不同长度和复杂度的密码，哈希时间应该相对一致

        // Arrange
        var passwords = new[]
        {
            "Short1!",
            "MediumLength123!",
            "VeryLongPasswordWithManyCharacters123456789!@#$%^&*()"
        };

        var timings = new List<TimeSpan>();

        // Act
        foreach (var password in passwords)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _passwordService.HashPassword(password);
            stopwatch.Stop();
            timings.Add(stopwatch.Elapsed);
        }

        // Assert
        var maxTiming = timings.Max();
        var minTiming = timings.Min();
        var timingDifference = maxTiming - minTiming;

        // 时序差异不应该超过100ms（这个阈值可能需要根据实际情况调整）
        timingDifference.Should().BeLessThan(TimeSpan.FromMilliseconds(100), 
            "不同密码的哈希时间差异不应该太大，以防止时序攻击");
    }

    #endregion
}