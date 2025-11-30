using AutoMapper;
using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Categories;
using DotNet10Template.Application.Interfaces;
using DotNet10Template.Domain.Entities;
using DotNet10Template.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotNet10Template.Infrastructure.Services;

/// <summary>
/// Category service implementation
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CategoryService> _logger;

    private const string CacheKeyPrefix = "category_";
    private const string AllCategoriesCacheKey = "categories_all";

    public CategoryService(
        IRepository<Category> categoryRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";
        var cached = await _cacheService.GetAsync<CategoryDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return Result<CategoryDto>.Success(cached);
        }

        var category = await _categoryRepository.AsQueryable()
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
        {
            return Result<CategoryDto>.Failure("Category not found");
        }

        var dto = _mapper.Map<CategoryDto>(category);
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30), cancellationToken);

        return Result<CategoryDto>.Success(dto);
    }

    public async Task<Result<IEnumerable<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<IEnumerable<CategoryDto>>(AllCategoriesCacheKey, cancellationToken);
        if (cached != null)
        {
            return Result<IEnumerable<CategoryDto>>.Success(cached);
        }

        var categories = await _categoryRepository.AsQueryable()
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
        await _cacheService.SetAsync(AllCategoriesCacheKey, dtos, TimeSpan.FromMinutes(30), cancellationToken);

        return Result<IEnumerable<CategoryDto>>.Success(dtos);
    }

    public async Task<Result<IEnumerable<CategoryDto>>> GetSubCategoriesAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.AsQueryable()
            .Include(c => c.Products)
            .Where(c => c.ParentCategoryId == parentId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
        return Result<IEnumerable<CategoryDto>>.Success(dtos);
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        // Check for duplicate name
        var exists = await _categoryRepository.AnyAsync(c => c.Name == request.Name, cancellationToken);
        if (exists)
        {
            return Result<CategoryDto>.Failure("A category with this name already exists");
        }

        // Validate parent category if provided
        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await _categoryRepository.AnyAsync(c => c.Id == request.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
            {
                return Result<CategoryDto>.Failure("Parent category not found");
            }
        }

        var category = _mapper.Map<Category>(request);
        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync(AllCategoriesCacheKey, cancellationToken);

        _logger.LogInformation("Category {CategoryId} created", category.Id);

        var dto = _mapper.Map<CategoryDto>(category);
        return Result<CategoryDto>.Success(dto, "Category created successfully");
    }

    public async Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return Result<CategoryDto>.Failure("Category not found");
        }

        // Check for duplicate name (excluding current)
        var exists = await _categoryRepository.AnyAsync(c => c.Name == request.Name && c.Id != id, cancellationToken);
        if (exists)
        {
            return Result<CategoryDto>.Failure("A category with this name already exists");
        }

        // Prevent circular reference
        if (request.ParentCategoryId == id)
        {
            return Result<CategoryDto>.Failure("A category cannot be its own parent");
        }

        _mapper.Map(request, category);
        await _categoryRepository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}", cancellationToken);
        await _cacheService.RemoveAsync(AllCategoriesCacheKey, cancellationToken);

        _logger.LogInformation("Category {CategoryId} updated", category.Id);

        var dto = _mapper.Map<CategoryDto>(category);
        return Result<CategoryDto>.Success(dto, "Category updated successfully");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.AsQueryable()
            .Include(c => c.Products)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
        {
            return Result.Failure("Category not found");
        }

        // Check if category has products
        if (category.Products.Any())
        {
            return Result.Failure("Cannot delete category with associated products");
        }

        // Check if category has subcategories
        if (category.SubCategories.Any())
        {
            return Result.Failure("Cannot delete category with subcategories");
        }

        await _categoryRepository.SoftDeleteAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}", cancellationToken);
        await _cacheService.RemoveAsync(AllCategoriesCacheKey, cancellationToken);

        _logger.LogInformation("Category {CategoryId} deleted", id);

        return Result.Success("Category deleted successfully");
    }
}
