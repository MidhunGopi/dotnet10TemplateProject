namespace DotNet10Template.Application.DTOs.Products;

/// <summary>
/// Create product request DTO
/// </summary>
public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? SKU,
    bool IsAvailable = true,
    Guid? CategoryId = null
);
