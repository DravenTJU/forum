using Forum.Api.Models.Entities;
using Forum.Api.Repositories;

namespace Forum.Api.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        return await _tagRepository.GetAllAsync();
    }

    public async Task<Tag?> GetByIdAsync(long id)
    {
        return await _tagRepository.GetByIdAsync(id);
    }

    public async Task<Tag?> GetBySlugAsync(string slug)
    {
        return await _tagRepository.GetBySlugAsync(slug);
    }

    public async Task<IEnumerable<Tag>> GetByTopicIdAsync(long topicId)
    {
        return await _tagRepository.GetByTopicIdAsync(topicId);
    }

    public async Task<Dictionary<long, IEnumerable<Tag>>> GetByTopicIdsAsync(IEnumerable<long> topicIds)
    {
        return await _tagRepository.GetByTopicIdsAsync(topicIds);
    }
}