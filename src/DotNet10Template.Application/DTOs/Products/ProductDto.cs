namespace DotNet10Template.Application.DTOs.Products;

/// <summary>
/// Product response DTO
/// </summary>
public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? SKU,
    bool IsAvailable,
    Guid? CategoryId,
    string? CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
