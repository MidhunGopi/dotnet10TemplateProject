namespace DotNet10Template.Application.DTOs.Products;

/// <summary>
/// Update product request DTO
/// </summary>
public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? SKU,
    bool IsAvailable,
    Guid? CategoryId
);
