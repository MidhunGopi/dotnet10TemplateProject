using AutoMapper;
using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Categories;
using DotNet10Template.Application.Interfaces;
using DotNet10Template.Domain.Entities;
using DotNet10Template.Domain.Interfaces;
using DotNet10Template.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNet10Template.Infrastructure.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<IRepository<Category>> _mockCategoryRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<CategoryService>> _mockLogger;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _mockCategoryRepository = new Mock<IRepository<Category>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CategoryService>>();

        _categoryService = new CategoryService(
            _mockCategoryRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockCacheService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryExistsInCache_ReturnsCachedCategory()
    {
        var categoryId = Guid.NewGuid();
        var cachedCategory = new CategoryDto(
            categoryId,
            "Electronics",
            "Electronic devices",
            true,
            null,
            null,
            0,
            DateTime.UtcNow,
            null
        );

        _mockCacheService
            .Setup(x => x.GetAsync<CategoryDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedCategory);

        var result = await _categoryService.GetByIdAsync(categoryId);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(cachedCategory);
        _mockCategoryRepository.Verify(x => x.AsQueryable(), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryNotFound_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();
        var categories = new List<Category>().AsQueryable();

        _mockCacheService
            .Setup(x => x.GetAsync<CategoryDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryDto?)null);

        _mockCategoryRepository
            .Setup(x => x.AsQueryable())
            .Returns(categories);

        var result = await _categoryService.GetByIdAsync(categoryId);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Category not found");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryExists_ReturnsMappedCategory()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Electronics",
            Description = "Electronic devices",
            IsActive = true
        };

        var categoryDto = new CategoryDto(
            categoryId,
            "Electronics",
            "Electronic devices",
            true,
            null,
            null,
            0,
            DateTime.UtcNow,
            null
        );

        var categories = new List<Category> { category }.AsQueryable();

        _mockCacheService
            .Setup(x => x.GetAsync<CategoryDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryDto?)null);

        _mockCategoryRepository
            .Setup(x => x.AsQueryable())
            .Returns(categories);

        _mockMapper
            .Setup(x => x.Map<CategoryDto>(It.IsAny<Category>()))
            .Returns(categoryDto);

        var result = await _categoryService.GetByIdAsync(categoryId);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(categoryDto);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), categoryDto, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenCategoriesExistInCache_ReturnsCachedCategories()
    {
        var cachedCategories = new List<CategoryDto>
        {
            new CategoryDto(Guid.NewGuid(), "Electronics", "Electronic devices", true, null, null, 0, DateTime.UtcNow, null),
            new CategoryDto(Guid.NewGuid(), "Books", "Books and magazines", true, null, null, 0, DateTime.UtcNow, null)
        };

        _mockCacheService
            .Setup(x => x.GetAsync<IEnumerable<CategoryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedCategories);

        var result = await _categoryService.GetAllAsync();

        result.Succeeded.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        _mockCategoryRepository.Verify(x => x.AsQueryable(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ReturnsFailure()
    {
        var request = new CreateCategoryRequest("Electronics", "Electronic devices", true, null);

        _mockCategoryRepository
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _categoryService.CreateAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("A category with this name already exists");
        _mockCategoryRepository.Verify(x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenParentCategoryNotFound_ReturnsFailure()
    {
        var parentId = Guid.NewGuid();
        var request = new CreateCategoryRequest("Laptops", "Laptop computers", true, parentId);

        _mockCategoryRepository
            .SetupSequence(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(false);

        var result = await _categoryService.CreateAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Parent category not found");
    }

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesCategorySuccessfully()
    {
        var request = new CreateCategoryRequest("Electronics", "Electronic devices", true, null);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive
        };

        var categoryDto = new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.IsActive,
            null,
            null,
            0,
            DateTime.UtcNow,
            null
        );

        _mockCategoryRepository
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockMapper
            .Setup(x => x.Map<Category>(request))
            .Returns(category);

        _mockMapper
            .Setup(x => x.Map<CategoryDto>(category))
            .Returns(categoryDto);

        var result = await _categoryService.CreateAsync(request);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(categoryDto);
        result.Message.Should().Be("Category created successfully");
        _mockCategoryRepository.Verify(x => x.AddAsync(category, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryNotFound_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();
        var request = new UpdateCategoryRequest("Updated Category", "Updated Description", true, null);

        _mockCategoryRepository
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var result = await _categoryService.UpdateAsync(categoryId, request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Category not found");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameDuplicate_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();
        var request = new UpdateCategoryRequest("Electronics", "Updated Description", true, null);

        var existingCategory = new Category { Id = categoryId, Name = "Old Name" };

        _mockCategoryRepository
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _mockCategoryRepository
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _categoryService.UpdateAsync(categoryId, request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("A category with this name already exists");
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryIsOwnParent_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();
        var request = new UpdateCategoryRequest("Electronics", "Updated Description", true, categoryId);

        var existingCategory = new Category { Id = categoryId, Name = "Electronics" };

        _mockCategoryRepository
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _mockCategoryRepository
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _categoryService.UpdateAsync(categoryId, request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("A category cannot be its own parent");
    }

    [Fact]
    public async Task UpdateAsync_WhenValidRequest_UpdatesCategorySuccessfully()
    {
        var categoryId = Guid.NewGuid();
        var request = new UpdateCategoryRequest("Updated Electronics", "Updated Description", true, null);

        var existingCategory = new Category
        {
            Id = categoryId,
            Name = "Electronics",
            Description = "Old Description"
        };

        var updatedDto = new CategoryDto(
            categoryId,
            request.Name,
            request.Description,
            request.IsActive,
            null,
            null,
            0,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _mockCategoryRepository
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _mockCategoryRepository
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockMapper
            .Setup(x => x.Map<CategoryDto>(existingCategory))
            .Returns(updatedDto);

        var result = await _categoryService.UpdateAsync(categoryId, request);

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("Category updated successfully");
        _mockCategoryRepository.Verify(x => x.UpdateAsync(existingCategory, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryNotFound_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();
        var categories = new List<Category>().AsQueryable();

        _mockCategoryRepository
            .Setup(x => x.AsQueryable())
            .Returns(categories);

        var result = await _categoryService.DeleteAsync(categoryId);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Category not found");
        _mockCategoryRepository.Verify(x => x.SoftDeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryHasProducts_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Electronics",
            Products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Laptop" }
            }
        };

        var categories = new List<Category> { category }.AsQueryable();

        _mockCategoryRepository
            .Setup(x => x.AsQueryable())
            .Returns(categories);

        var result = await _categoryService.DeleteAsync(categoryId);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Cannot delete category with associated products");
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryHasSubcategories_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Electronics",
            SubCategories = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "Laptops" }
            }
        };

        var categories = new List<Category> { category }.AsQueryable();

        _mockCategoryRepository
            .Setup(x => x.AsQueryable())
            .Returns(categories);

        var result = await _categoryService.DeleteAsync(categoryId);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Cannot delete category with subcategories");
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryIsValid_DeletesSuccessfully()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Electronics",
            Products = new List<Product>(),
            SubCategories = new List<Category>()
        };

        var categories = new List<Category> { category }.AsQueryable();

        _mockCategoryRepository
            .Setup(x => x.AsQueryable())
            .Returns(categories);

        var result = await _categoryService.DeleteAsync(categoryId);

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("Category deleted successfully");
        _mockCategoryRepository.Verify(x => x.SoftDeleteAsync(category, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
