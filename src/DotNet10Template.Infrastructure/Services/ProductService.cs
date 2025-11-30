using AutoMapper;
using DotNet10Template.Application.Common.Exceptions;
using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Products;
using DotNet10Template.Application.Interfaces;
using DotNet10Template.Domain.Entities;
using DotNet10Template.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotNet10Template.Infrastructure.Services;

/// <summary>
/// Product service implementation
/// </summary>
public class ProductService : IProductService
{
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageBrokerService _messageBroker;
    private readonly ILogger<ProductService> _logger;

    private const string CacheKeyPrefix = "product_";

    public ProductService(
        IRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IMessageBrokerService messageBroker,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cacheKey = $"{CacheKeyPrefix}{id}";
        var cached = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return Result<ProductDto>.Success(cached);
        }

        var product = await _productRepository.AsQueryable()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
        {
            return Result<ProductDto>.Failure("Product not found");
        }

        var dto = _mapper.Map<ProductDto>(product);
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30), cancellationToken);

        return Result<ProductDto>.Success(dto);
    }

    public async Task<Result<PaginatedList<ProductDto>>> GetAllAsync(PaginationParams parameters, CancellationToken cancellationToken = default)
    {
        var query = _productRepository.AsQueryable()
            .Include(p => p.Category)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchTerm) ||
                                     (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                                     (p.SKU != null && p.SKU.ToLower().Contains(searchTerm)));
        }

        // Sort
        query = parameters.SortBy?.ToLower() switch
        {
            "name" => parameters.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => parameters.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "createdat" => parameters.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<ProductDto>>(items);
        var result = new PaginatedList<ProductDto>(dtos, totalCount, parameters.PageNumber, parameters.PageSize);

        return Result<PaginatedList<ProductDto>>.Success(result);
    }

    public async Task<Result<IEnumerable<ProductDto>>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.AsQueryable()
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<ProductDto>>(products);
        return Result<IEnumerable<ProductDto>>.Success(dtos);
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        // Check for duplicate SKU
        if (!string.IsNullOrEmpty(request.SKU))
        {
            var exists = await _productRepository.AnyAsync(p => p.SKU == request.SKU, cancellationToken);
            if (exists)
            {
                return Result<ProductDto>.Failure("A product with this SKU already exists");
            }
        }

        var product = _mapper.Map<Product>(request);
        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event
        await _messageBroker.PublishAsync("product.created", new { ProductId = product.Id, product.Name }, cancellationToken);

        _logger.LogInformation("Product {ProductId} created", product.Id);

        var dto = _mapper.Map<ProductDto>(product);
        return Result<ProductDto>.Success(dto, "Product created successfully");
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return Result<ProductDto>.Failure("Product not found");
        }

        // Check for duplicate SKU (excluding current product)
        if (!string.IsNullOrEmpty(request.SKU))
        {
            var exists = await _productRepository.AnyAsync(p => p.SKU == request.SKU && p.Id != id, cancellationToken);
            if (exists)
            {
                return Result<ProductDto>.Failure("A product with this SKU already exists");
            }
        }

        _mapper.Map(request, product);
        await _productRepository.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}", cancellationToken);

        // Publish event
        await _messageBroker.PublishAsync("product.updated", new { ProductId = product.Id, product.Name }, cancellationToken);

        _logger.LogInformation("Product {ProductId} updated", product.Id);

        var dto = _mapper.Map<ProductDto>(product);
        return Result<ProductDto>.Success(dto, "Product updated successfully");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return Result.Failure("Product not found");
        }

        await _productRepository.SoftDeleteAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync($"{CacheKeyPrefix}{id}", cancellationToken);

        // Publish event
        await _messageBroker.PublishAsync("product.deleted", new { ProductId = id }, cancellationToken);

        _logger.LogInformation("Product {ProductId} deleted", id);

        return Result.Success("Product deleted successfully");
    }

    public async Task<Result<IEnumerable<ProductDto>>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Result<IEnumerable<ProductDto>>.Success(Enumerable.Empty<ProductDto>());
        }

        var searchLower = searchTerm.ToLower();
        var products = await _productRepository.AsQueryable()
            .Include(p => p.Category)
            .Where(p => p.Name.ToLower().Contains(searchLower) ||
                       (p.Description != null && p.Description.ToLower().Contains(searchLower)))
            .Take(50)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<ProductDto>>(products);
        return Result<IEnumerable<ProductDto>>.Success(dtos);
    }
}
