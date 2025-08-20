using Dapper;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, name, slug, description, color, `order`, is_archived as IsArchived,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM categories 
            WHERE is_archived = 0
            ORDER BY `order`, name";

        return await connection.QueryAsync<Category>(sql);
    }

    public async Task<Category?> GetByIdAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, name, slug, description, color, `order`, is_archived as IsArchived,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM categories 
            WHERE id = @Id";

        return await connection.QuerySingleOrDefaultAsync<Category>(sql, new { Id = id });
    }

    public async Task<Category?> GetBySlugAsync(string slug)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, name, slug, description, color, `order`, is_archived as IsArchived,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM categories 
            WHERE slug = @Slug";

        return await connection.QuerySingleOrDefaultAsync<Category>(sql, new { Slug = slug });
    }

    public async Task<long> CreateAsync(Category category)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            INSERT INTO categories (name, slug, description, color, `order`, is_archived)
            VALUES (@Name, @Slug, @Description, @Color, @Order, @IsArchived);
            SELECT LAST_INSERT_ID();";

        return await connection.QuerySingleAsync<long>(sql, category);
    }

    public async Task UpdateAsync(Category category)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            UPDATE categories 
            SET name = @Name, slug = @Slug, description = @Description, 
                color = @Color, `order` = @Order, is_archived = @IsArchived
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, category);
    }

    public async Task DeleteAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = "DELETE FROM categories WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}