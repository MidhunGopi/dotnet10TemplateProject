using DotNet10Template.Application.DTOs.Categories;

namespace DotNet10Template.Application.DTOs.Categories;

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    int ProductCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCategoryRequest(
    string Name,
    string? Description,
    bool IsActive,
    Guid? ParentCategoryId
);

public record UpdateCategoryRequest(
    string Name,
    string? Description,
    bool IsActive,
    Guid? ParentCategoryId
);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? ProfilePictureUrl,
    IEnumerable<string> Roles
);

public record ForgotPasswordRequest(string Email);
