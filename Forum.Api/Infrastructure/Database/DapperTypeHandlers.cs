using Dapper;
using Forum.Api.Models.Entities;
using System.Data;

namespace Forum.Api.Infrastructure.Database;

public class UserStatusTypeHandler : SqlMapper.TypeHandler<UserStatus>
{
    public override void SetValue(IDbDataParameter parameter, UserStatus value)
    {
        parameter.Value = value.ToString().ToLowerInvariant();
    }

    public override UserStatus Parse(object value)
    {
        return Enum.Parse<UserStatus>(value.ToString()!, true);
    }
}

public class UserRoleTypeHandler : SqlMapper.TypeHandler<UserRole>
{
    public override void SetValue(IDbDataParameter parameter, UserRole value)
    {
        parameter.Value = value.ToString().ToLowerInvariant();
    }

    public override UserRole Parse(object value)
    {
        return Enum.Parse<UserRole>(value.ToString()!, true);
    }
}

public static class DapperConfiguration
{
    public static void Configure()
    {
        SqlMapper.AddTypeHandler(new UserStatusTypeHandler());
        SqlMapper.AddTypeHandler(new UserRoleTypeHandler());
    }
}