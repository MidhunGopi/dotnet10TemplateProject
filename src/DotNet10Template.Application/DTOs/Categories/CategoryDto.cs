namespace DotNet10Template.Application.DTOs.Categories;

/// <summary>
/// Category response DTO
/// </summary>
public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    int ProductCount,
    DateTime CreatedAt
);
