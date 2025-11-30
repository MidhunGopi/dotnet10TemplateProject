using DotNet10Template.Application.Common.Models;
using DotNet10Template.Application.DTOs.Products;
using DotNet10Template.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNet10Template.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products with pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams parameters, CancellationToken cancellationToken)
    {
        var result = await _productService.GetAllAsync(parameters, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetByIdAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return NotFound(new { message = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{categoryId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(Guid categoryId, CancellationToken cancellationToken)
    {
        var result = await _productService.GetByCategoryAsync(categoryId, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Search products
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string term, CancellationToken cancellationToken)
    {
        var result = await _productService.SearchAsync(term, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Errors.FirstOrDefault(), errors = result.Errors });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Update a product
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateAsync(id, request, cancellationToken);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
            {
                return NotFound(new { message = result.Errors.FirstOrDefault() });
            }
            return BadRequest(new { message = result.Errors.FirstOrDefault(), errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return NotFound(new { message = result.Errors.FirstOrDefault() });
        }

        return NoContent();
    }
}
