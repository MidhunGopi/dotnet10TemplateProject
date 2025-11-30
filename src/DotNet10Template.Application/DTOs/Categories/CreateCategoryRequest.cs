namespace DotNet10Template.Application.DTOs.Categories;

/// <summary>
/// Create category request DTO
/// </summary>
public record CreateCategoryRequest(
    string Name,
    string? Description,
    bool IsActive = true,
    Guid? ParentCategoryId = null
);
