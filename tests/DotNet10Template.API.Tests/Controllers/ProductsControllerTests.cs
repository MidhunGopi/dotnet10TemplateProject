using DotNet10Template.API.Controllers;
using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Products;
using DotNet10Template.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNet10Template.API.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<ILogger<ProductsController>> _mockLogger;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockProductService = new Mock<IProductService>();
        _mockLogger = new Mock<ILogger<ProductsController>>();
        _controller = new ProductsController(_mockProductService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_WhenProductsExist_ReturnsOkWithPaginatedList()
    {
        var paginationParams = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var products = new List<ProductDto>
        {
            new ProductDto(Guid.NewGuid(), "Product 1", "Description 1", 99.99m, 10, "SKU1", true, null, null, DateTime.UtcNow, null),
            new ProductDto(Guid.NewGuid(), "Product 2", "Description 2", 149.99m, 5, "SKU2", true, null, null, DateTime.UtcNow, null)
        };
        var paginatedList = new PaginatedList<ProductDto>(products, products.Count, 1, 10);

        _mockProductService
            .Setup(x => x.GetAllAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginatedList<ProductDto>>.Success(paginatedList));

        var result = await _controller.GetAll(paginationParams, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedData = okResult.Value.Should().BeOfType<PaginatedList<ProductDto>>().Subject;
        returnedData.Items.Should().HaveCount(2);
        returnedData.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetById_WhenProductExists_ReturnsOkWithProduct()
    {
        var productId = Guid.NewGuid();
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

        _mockProductService
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDto>.Success(productDto));

        var result = await _controller.GetById(productId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProduct = okResult.Value.Should().BeOfType<ProductDto>().Subject;
        returnedProduct.Id.Should().Be(productId);
        returnedProduct.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetById_WhenProductNotFound_ReturnsNotFound()
    {
        var productId = Guid.NewGuid();

        _mockProductService
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDto>.Failure("Product not found"));

        var result = await _controller.GetById(productId, CancellationToken.None);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetByCategory_WhenCategoryHasProducts_ReturnsOkWithProducts()
    {
        var categoryId = Guid.NewGuid();
        var products = new List<ProductDto>
        {
            new ProductDto(Guid.NewGuid(), "Product 1", "Description 1", 99.99m, 10, "SKU1", true, categoryId, "Category Name", DateTime.UtcNow, null)
        };

        _mockProductService
            .Setup(x => x.GetByCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<ProductDto>>.Success(products));

        var result = await _controller.GetByCategory(categoryId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProducts = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>().Subject;
        returnedProducts.Should().HaveCount(1);
    }

    [Fact]
    public async Task Search_WhenMatchingProductsFound_ReturnsOkWithProducts()
    {
        var searchTerm = "laptop";
        var products = new List<ProductDto>
        {
            new ProductDto(Guid.NewGuid(), "Gaming Laptop", "High performance laptop", 1299.99m, 5, "LAP001", true, null, null, DateTime.UtcNow, null),
            new ProductDto(Guid.NewGuid(), "Business Laptop", "Professional laptop", 999.99m, 10, "LAP002", true, null, null, DateTime.UtcNow, null)
        };

        _mockProductService
            .Setup(x => x.SearchAsync(searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<ProductDto>>.Success(products));

        var result = await _controller.Search(searchTerm, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProducts = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>().Subject;
        returnedProducts.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_WhenValidRequest_ReturnsCreatedAtAction()
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

        var createdProduct = new ProductDto(
            Guid.NewGuid(),
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.SKU,
            request.IsAvailable,
            null,
            null,
            DateTime.UtcNow,
            null
        );

        _mockProductService
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDto>.Success(createdProduct));

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ProductsController.GetById));
        createdResult.RouteValues!["id"].Should().Be(createdProduct.Id);
        var returnedProduct = createdResult.Value.Should().BeOfType<ProductDto>().Subject;
        returnedProduct.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task Create_WhenServiceFails_ReturnsBadRequest()
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

        _mockProductService
            .Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDto>.Failure("A product with this SKU already exists"));

        var result = await _controller.Create(request, CancellationToken.None);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Update_WhenValidRequest_ReturnsOkWithUpdatedProduct()
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

        var updatedProduct = new ProductDto(
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

        _mockProductService
            .Setup(x => x.UpdateAsync(productId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDto>.Success(updatedProduct));

        var result = await _controller.Update(productId, request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProduct = okResult.Value.Should().BeOfType<ProductDto>().Subject;
        returnedProduct.Name.Should().Be(request.Name);
        returnedProduct.Price.Should().Be(request.Price);
    }

    [Fact]
    public async Task Update_WhenProductNotFound_ReturnsNotFound()
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

        _mockProductService
            .Setup(x => x.UpdateAsync(productId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDto>.Failure("Product not found"));

        var result = await _controller.Update(productId, request, CancellationToken.None);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Update_WhenValidationFails_ReturnsBadRequest()
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

        _mockProductService
            .Setup(x => x.UpdateAsync(productId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDto>.Failure("A product with this SKU already exists"));

        var result = await _controller.Update(productId, request, CancellationToken.None);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Delete_WhenProductExists_ReturnsNoContent()
    {
        var productId = Guid.NewGuid();

        _mockProductService
            .Setup(x => x.DeleteAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Delete(productId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenProductNotFound_ReturnsNotFound()
    {
        var productId = Guid.NewGuid();

        _mockProductService
            .Setup(x => x.DeleteAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Product not found"));

        var result = await _controller.Delete(productId, CancellationToken.None);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }
}
