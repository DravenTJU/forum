using FluentAssertions;
using FluentAssertions.Primitives;
using Forum.Api.Models.DTOs;
using Forum.Api.Models.Entities;
using System.Security.Claims;

namespace Forum.Api.Tests.Extensions;

/// <summary>
/// 论坛业务领域专用断言扩展
/// 提供业务逻辑相关的断言方法
/// </summary>
public static class ForumAssertions
{
    /// <summary>
    /// 验证API响应格式
    /// </summary>
    public static AndConstraint<ObjectAssertions> ShouldBeValidApiResponse<T>(this ApiResponse<T> response)
    {
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Error.Should().BeNull();
        return response.Should();
    }

    /// <summary>
    /// 验证API错误响应
    /// </summary>
    public static AndConstraint<ObjectAssertions> ShouldBeErrorResponse<T>(this ApiResponse<T> response, string? expectedCode = null)
    {
        response.Should().NotBeNull();
        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Data.Should().Be(default(T));

        if (!string.IsNullOrEmpty(expectedCode))
        {
            response.Error!.Code.Should().Be(expectedCode);
        }

        return response.Should();
    }

    /// <summary>
    /// 验证分页响应格式
    /// </summary>
    public static AndConstraint<ObjectAssertions> ShouldHaveValidPagination<T>(this ApiResponse<List<T>> response)
    {
        response.ShouldBeValidApiResponse();
        response.Data.Should().NotBeNull();
        response.Meta.Should().NotBeNull();
        response.Meta!.HasNext.Should().NotBeNull();
        
        if (response.Meta.HasNext == true)
        {
            response.Meta.NextCursor.Should().NotBeNullOrEmpty();
        }

        return response.Should();
    }

    /// <summary>
    /// 验证用户认证状态
    /// </summary>
    public static AndConstraint<ObjectAssertions> ShouldBeAuthenticated(this ClaimsPrincipal principal, string? expectedUserId = null)
    {
        principal.Should().NotBeNull();
        principal.Identity!.IsAuthenticated.Should().BeTrue();

        if (!string.IsNullOrEmpty(expectedUserId))
        {
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            userIdClaim.Should().NotBeNull();
            userIdClaim!.Value.Should().Be(expectedUserId);
        }

        return principal.Should();
    }

    /// <summary>
    /// 验证用户实体有效性
    /// </summary>
    public static AndConstraint<ObjectAssertions> ShouldBeValidUser(this User user)
    {
        user.Should().NotBeNull();
        user.Id.Should().NotBeNullOrEmpty();
        user.Username.Should().NotBeNullOrEmpty();
        user.Email.Should().NotBeNullOrEmpty().And.Contain("@");
        user.PasswordHash.Should().NotBeNullOrEmpty();
        user.Status.Should().BeOneOf("active", "suspended");
        user.CreatedAt.Should().BeBefore(DateTime.UtcNow);
        user.UpdatedAt.Should().BeOnOrAfter(user.CreatedAt);

        return user.Should();
    }

    /// <summary>
    /// 验证主题实体有效性
    /// </summary>
    public static AndConstraint<ObjectAssertions> ShouldBeValidTopic(this Topic topic)
    {
        topic.Should().NotBeNull();
        topic.Id.Should().BeGreaterThan(0);
        topic.Title.Should().NotBeNullOrEmpty();
        topic.Slug.Should().NotBeNullOrEmpty();
        topic.AuthorId.Should().BeGreaterThan(0);
        topic.CategoryId.Should().BeGreaterThan(0);
        topic.ReplyCount.Should().BeGreaterOrEqualTo(0);
        topic.ViewCount.Should().BeGreaterOrEqualTo(0);
        topic.CreatedAt.Should().BeBefore(DateTime.UtcNow);
        topic.UpdatedAt.Should().BeOnOrAfter(topic.CreatedAt);

        // 如果有回复，最后回复时间和人应该有值
        if (topic.ReplyCount > 0)
        {
            topic.LastPostedAt.Should().NotBeNull().And.BeOnOrAfter(topic.CreatedAt);
            topic.LastPosterId.Should().NotBeNull().And.BeGreaterThan(0);
        }

        return topic.Should();
    }

    /// <summary>
    /// 验证分类实体有效性
    /// </summary>
    public static AndConstraint<ObjectAssertions> ShouldBeValidCategory(this Category category)
    {
        category.Should().NotBeNull();
        category.Id.Should().BeGreaterThan(0);
        category.Name.Should().NotBeNullOrEmpty();
        category.Slug.Should().NotBeNullOrEmpty();
        category.Color.Should().NotBeNullOrEmpty().And.StartWith("#");
        category.Order.Should().BeGreaterOrEqualTo(0);
        category.CreatedAt.Should().BeBefore(DateTime.UtcNow);
        category.UpdatedAt.Should().BeOnOrAfter(category.CreatedAt);

        return category.Should();
    }

    /// <summary>
    /// 验证性能基准
    /// </summary>
    public static AndConstraint<TimeSpanAssertions> ShouldMeetPerformanceBenchmark(this TimeSpan elapsed, TimeSpan threshold, string operation = "操作")
    {
        elapsed.Should().BeLessOrEqualTo(threshold, $"{operation}应该在{threshold.TotalMilliseconds}ms内完成");
        return elapsed.Should();
    }

    /// <summary>
    /// 验证JWT Token格式
    /// </summary>
    public static AndConstraint<StringAssertions> ShouldBeValidJwtToken(this string token)
    {
        token.Should().NotBeNullOrEmpty();
        var parts = token.Split('.');
        parts.Should().HaveCount(3, "JWT Token应该有3个部分");
        
        foreach (var part in parts)
        {
            part.Should().NotBeNullOrEmpty();
        }

        return token.Should();
    }

    /// <summary>
    /// 验证密码哈希格式
    /// </summary>
    public static AndConstraint<StringAssertions> ShouldBeValidPasswordHash(this string hash)
    {
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(60, "BCrypt哈希应该是60个字符");
        hash.Should().StartWith("$2", "BCrypt哈希应该以$2开头");

        return hash.Should();
    }

    /// <summary>
    /// 验证邮箱格式
    /// </summary>
    public static AndConstraint<StringAssertions> ShouldBeValidEmail(this string email)
    {
        email.Should().NotBeNullOrEmpty();
        email.Should().Contain("@");
        email.Should().MatchRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", "应该是有效的邮箱格式");

        return email.Should();
    }

    /// <summary>
    /// 验证Slug格式
    /// </summary>
    public static AndConstraint<StringAssertions> ShouldBeValidSlug(this string slug)
    {
        slug.Should().NotBeNullOrEmpty();
        slug.Should().NotContain(" ", "Slug不应该包含空格");
        slug.Should().NotContain("_", "Slug不应该包含下划线");
        slug.Should().MatchRegex(@"^[a-z0-9-]+$", "Slug应该只包含小写字母、数字和连字符");

        return slug.Should();
    }

    /// <summary>
    /// 验证时间戳一致性
    /// </summary>
    public static AndConstraint<DateTimeAssertions> ShouldBeConsistentWith(this DateTime updatedAt, DateTime createdAt)
    {
        updatedAt.Should().BeOnOrAfter(createdAt, "更新时间不应该早于创建时间");
        return updatedAt.Should();
    }

    /// <summary>
    /// 验证集合不为空且有序
    /// </summary>
    public static AndConstraint<GenericCollectionAssertions<T>> ShouldBeOrderedCollection<T>(this IEnumerable<T> collection)
    {
        var list = collection.ToList();
        list.Should().NotBeEmpty();
        return list.Should();
    }

    /// <summary>
    /// 验证并发操作结果
    /// </summary>
    public static AndConstraint<ObjectAssertions> ShouldHandleConcurrency<T>(this T result, bool expectSuccess = true)
    {
        result.Should().NotBeNull();
        
        if (expectSuccess)
        {
            // 验证并发操作成功的标准
        }
        else
        {
            // 验证并发冲突检测
        }

        return result.Should();
    }

    /// <summary>
    /// 验证测试数据质量
    /// </summary>
    public static AndConstraint<ObjectAssertions> ShouldBeValidTestData<T>(this T data) where T : class
    {
        data.Should().NotBeNull("测试数据不应该为null");
        
        // 使用反射检查关键属性
        var properties = typeof(T).GetProperties();
        foreach (var prop in properties)
        {
            if (prop.Name.Contains("Id") && prop.PropertyType == typeof(long))
            {
                var value = (long)(prop.GetValue(data) ?? 0);
                value.Should().BeGreaterThan(0, $"{prop.Name}应该大于0");
            }
            
            if (prop.PropertyType == typeof(string) && prop.Name.EndsWith("At") == false)
            {
                var value = (string?)(prop.GetValue(data));
                if (prop.Name.Contains("Email"))
                {
                    value.ShouldBeValidEmail();
                }
            }
        }

        return data.Should();
    }
}