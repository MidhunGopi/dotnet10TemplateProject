using AutoMapper;
using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Products;
using DotNet10Template.Application.Interfaces;
using DotNet10Template.Domain.Entities;
using DotNet10Template.Domain.Interfaces;
using DotNet10Template.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNet10Template.Infrastructure.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IRepository<Product>> _mockProductRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IMessageBrokerService> _mockMessageBroker;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _mockProductRepository = new Mock<IRepository<Product>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockCacheService = new Mock<ICacheService>();
        _mockMessageBroker = new Mock<IMessageBrokerService>();
        _mockLogger = new Mock<ILogger<ProductService>>();

        _productService = new ProductService(
            _mockProductRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockCacheService.Object,
            _mockMessageBroker.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExistsInCache_ReturnsCachedProduct()
    {
        var productId = Guid.NewGuid();
        var cachedProduct = new ProductDto(
            productId,
            "Test Product",
            "Description",
            99.99m,
            10,
            "SKU123",
            true,
            null,
            null,
            DateTime.UtcNow,
            null
        );

        _mockCacheService
            .Setup(x => x.GetAsync<ProductDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedProduct);

        var result = await _productService.GetByIdAsync(productId);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(cachedProduct);
        _mockProductRepository.Verify(x => x.AsQueryable(), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var products = new List<Product>().AsQueryable();

        _mockCacheService
            .Setup(x => x.GetAsync<ProductDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        _mockProductRepository
            .Setup(x => x.AsQueryable())
            .Returns(products);

        var result = await _productService.GetByIdAsync(productId);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Product not found");
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsMappedProduct()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            StockQuantity = 10,
            SKU = "SKU123",
            IsAvailable = true
        };

        var productDto = new ProductDto(
            productId,
            "Test Product",
            "Test Description",
            99.99m,
            10,
            "SKU123",
            true,
            null,
            null,
            DateTime.UtcNow,
            null
        );

        var products = new List<Product> { product }.AsQueryable();

        _mockCacheService
            .Setup(x => x.GetAsync<ProductDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        _mockProductRepository
            .Setup(x => x.AsQueryable())
            .Returns(products);

        _mockMapper
            .Setup(x => x.Map<ProductDto>(It.IsAny<Product>()))
            .Returns(productDto);

        var result = await _productService.GetByIdAsync(productId);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(productDto);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), productDto, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenSkuAlreadyExists_ReturnsFailure()
    {
        var request = new CreateProductRequest(
            "New Product",
            "Description",
            99.99m,
            10,
            "DUPLICATE-SKU",
            true,
            null
        );

        _mockProductRepository
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _productService.CreateAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("A product with this SKU already exists");
        _mockProductRepository.Verify(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesProductSuccessfully()
    {
        var request = new CreateProductRequest(
            "New Product",
            "Description",
            99.99m,
            10,
            "SKU123",
            true,
            null
        );

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            SKU = request.SKU,
            IsAvailable = request.IsAvailable
        };

        var productDto = new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.SKU,
            product.IsAvailable,
            null,
            null,
            DateTime.UtcNow,
            null
        );

        _mockProductRepository
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockMapper
            .Setup(x => x.Map<Product>(request))
            .Returns(product);

        _mockMapper
            .Setup(x => x.Map<ProductDto>(product))
            .Returns(productDto);

        var result = await _productService.CreateAsync(request);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(productDto);
        result.Message.Should().Be("Product created successfully");
        _mockProductRepository.Verify(x => x.AddAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockMessageBroker.Verify(x => x.PublishAsync("product.created", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var request = new UpdateProductRequest(
            "Updated Product",
            "Updated Description",
            149.99m,
            20,
            "SKU456",
            true,
            null
        );

        _mockProductRepository
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _productService.UpdateAsync(productId, request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Product not found");
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenSkuDuplicate_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var request = new UpdateProductRequest(
            "Updated Product",
            "Updated Description",
            149.99m,
            20,
            "DUPLICATE-SKU",
            true,
            null
        );

        var existingProduct = new Product { Id = productId, Name = "Existing Product", Price = 99.99m };

        _mockProductRepository
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _mockProductRepository
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _productService.UpdateAsync(productId, request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("A product with this SKU already exists");
    }

    [Fact]
    public async Task UpdateAsync_WhenValidRequest_UpdatesProductSuccessfully()
    {
        var productId = Guid.NewGuid();
        var request = new UpdateProductRequest(
            "Updated Product",
            "Updated Description",
            149.99m,
            20,
            "SKU456",
            true,
            null
        );

        var existingProduct = new Product
        {
            Id = productId,
            Name = "Old Product",
            Price = 99.99m,
            StockQuantity = 10
        };

        var updatedDto = new ProductDto(
            productId,
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.SKU,
            request.IsAvailable,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _mockProductRepository
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _mockProductRepository
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockMapper
            .Setup(x => x.Map<ProductDto>(existingProduct))
            .Returns(updatedDto);

        var result = await _productService.UpdateAsync(productId, request);

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("Product updated successfully");
        _mockProductRepository.Verify(x => x.UpdateAsync(existingProduct, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMessageBroker.Verify(x => x.PublishAsync("product.updated", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ReturnsFailure()
    {
        var productId = Guid.NewGuid();

        _mockProductRepository
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _productService.DeleteAsync(productId);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Product not found");
        _mockProductRepository.Verify(x => x.SoftDeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_DeletesSuccessfully()
    {
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "Product to Delete", Price = 99.99m };

        _mockProductRepository
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _productService.DeleteAsync(productId);

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be("Product deleted successfully");
        _mockProductRepository.Verify(x => x.SoftDeleteAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMessageBroker.Verify(x => x.PublishAsync("product.deleted", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WhenSearchTermEmpty_ReturnsEmptyCollection()
    {
        var result = await _productService.SearchAsync("");

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty();
        _mockProductRepository.Verify(x => x.AsQueryable(), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WhenSearchTermProvided_ReturnsMatchingProducts()
    {
        var searchTerm = "laptop";
        var products = new List<Product>
        {
            new Product { Id = Guid.NewGuid(), Name = "Gaming Laptop", Price = 1299.99m },
            new Product { Id = Guid.NewGuid(), Name = "Business Laptop", Price = 999.99m }
        }.AsQueryable();

        var productDtos = new List<ProductDto>
        {
            new ProductDto(products.First().Id, "Gaming Laptop", null, 1299.99m, 5, null, true, null, null, DateTime.UtcNow, null),
            new ProductDto(products.Last().Id, "Business Laptop", null, 999.99m, 10, null, true, null, null, DateTime.UtcNow, null)
        };

        _mockProductRepository
            .Setup(x => x.AsQueryable())
            .Returns(products);

        _mockMapper
            .Setup(x => x.Map<IEnumerable<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(productDtos);

        var result = await _productService.SearchAsync(searchTerm);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }
}
