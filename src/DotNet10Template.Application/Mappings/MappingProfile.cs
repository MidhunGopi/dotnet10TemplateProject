using AutoMapper;
using DotNet10Template.Application.DTOs.Auth;
using DotNet10Template.Application.DTOs.Categories;
using DotNet10Template.Application.DTOs.Orders;
using DotNet10Template.Application.DTOs.Products;
using DotNet10Template.Domain.Entities;

namespace DotNet10Template.Application.Mappings;

/// <summary>
/// AutoMapper profile for application mappings
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(d => d.Roles, opt => opt.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name)));

        // Product mappings
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : null));
        CreateMap<CreateProductRequest, Product>();
        CreateMap<UpdateProductRequest, Product>();

        // Category mappings
        CreateMap<Category, CategoryDto>()
            .ForMember(d => d.ParentCategoryName, opt => opt.MapFrom(s => s.ParentCategory != null ? s.ParentCategory.Name : null))
            .ForMember(d => d.ProductCount, opt => opt.MapFrom(s => s.Products.Count));
        CreateMap<CreateCategoryRequest, Category>();
        CreateMap<UpdateCategoryRequest, Category>();

        // Order mappings
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User != null ? s.User.FullName : null))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.OrderItems));
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty));
        CreateMap<CreateOrderRequest, Order>();
        CreateMap<CreateOrderItemRequest, OrderItem>();
    }
}
