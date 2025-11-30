namespace DotNet10Template.Application.DTOs.Categories;

/// <summary>
/// Update category request DTO
/// </summary>
public record UpdateCategoryRequest(
    string Name,
    string? Description,
    bool IsActive,
    Guid? ParentCategoryId
);
