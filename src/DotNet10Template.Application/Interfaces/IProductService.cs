using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Products;

namespace DotNet10Template.Application.Interfaces;

/// <summary>
/// Interface for product service
/// </summary>
public interface IProductService
{
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<ProductDto>>> GetAllAsync(PaginationParams parameters, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProductDto>>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ProductDto>>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
}
