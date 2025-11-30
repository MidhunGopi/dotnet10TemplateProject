using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Categories;

namespace DotNet10Template.Application.Interfaces;

/// <summary>
/// Interface for category service
/// </summary>
public interface ICategoryService
{
    Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CategoryDto>>> GetSubCategoriesAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
