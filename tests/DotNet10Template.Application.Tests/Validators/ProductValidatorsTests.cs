using DotNet10Template.Application.DTOs.Products;
using DotNet10Template.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DotNet10Template.Application.Tests.Validators;

public class ProductValidatorsTests
{
    [Fact]
    public void CreateProductRequestValidator_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest("", "Description", 99.99m, 10, "SKU123");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name is required");
    }

    [Fact]
    public void CreateProductRequestValidator_WhenNameExceedsMaxLength_ShouldHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var longName = new string('A', 201);
        var request = new CreateProductRequest(longName, "Description", 99.99m, 10, "SKU123");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name cannot exceed 200 characters");
    }

    [Fact]
    public void CreateProductRequestValidator_WhenDescriptionExceedsMaxLength_ShouldHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var longDescription = new string('A', 2001);
        var request = new CreateProductRequest("Product", longDescription, 99.99m, 10, "SKU123");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 2000 characters");
    }

    [Fact]
    public void CreateProductRequestValidator_WhenPriceIsZero_ShouldHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest("Product", "Description", 0m, 10, "SKU123");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price must be greater than 0");
    }

    [Fact]
    public void CreateProductRequestValidator_WhenPriceIsNegative_ShouldHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest("Product", "Description", -10.50m, 10, "SKU123");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price must be greater than 0");
    }

    [Fact]
    public void CreateProductRequestValidator_WhenStockQuantityIsNegative_ShouldHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest("Product", "Description", 99.99m, -5, "SKU123");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.StockQuantity)
            .WithErrorMessage("Stock quantity cannot be negative");
    }

    [Fact]
    public void CreateProductRequestValidator_WhenStockQuantityIsZero_ShouldNotHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest("Product", "Description", 99.99m, 0, "SKU123");

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.StockQuantity);
    }

    [Fact]
    public void CreateProductRequestValidator_WhenSkuExceedsMaxLength_ShouldHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var longSku = new string('A', 51);
        var request = new CreateProductRequest("Product", "Description", 99.99m, 10, longSku);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SKU)
            .WithErrorMessage("SKU cannot exceed 50 characters");
    }

    [Fact]
    public void CreateProductRequestValidator_WhenSkuContainsInvalidCharacters_ShouldHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest("Product", "Description", 99.99m, 10, "SKU@123!");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SKU)
            .WithErrorMessage("SKU can only contain letters, numbers, hyphens, and underscores");
    }

    [Fact]
    public void CreateProductRequestValidator_WhenSkuIsValid_ShouldNotHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest("Product", "Description", 99.99m, 10, "SKU-123_ABC");

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.SKU);
    }

    [Fact]
    public void CreateProductRequestValidator_WhenSkuIsNull_ShouldNotHaveValidationError()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest("Product", "Description", 99.99m, 10, null);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.SKU);
    }

    [Fact]
    public void CreateProductRequestValidator_WhenAllFieldsValid_ShouldNotHaveValidationErrors()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest(
            "Valid Product Name",
            "Valid product description",
            99.99m,
            50,
            "SKU-12345",
            true,
            Guid.NewGuid()
        );

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateProductRequestValidator_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        var validator = new UpdateProductRequestValidator();
        var request = new UpdateProductRequest("", "Description", 99.99m, 10, "SKU123", true, null);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name is required");
    }

    [Fact]
    public void UpdateProductRequestValidator_WhenNameExceedsMaxLength_ShouldHaveValidationError()
    {
        var validator = new UpdateProductRequestValidator();
        var longName = new string('A', 201);
        var request = new UpdateProductRequest(longName, "Description", 99.99m, 10, "SKU123", true, null);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Product name cannot exceed 200 characters");
    }

    [Fact]
    public void UpdateProductRequestValidator_WhenPriceIsZero_ShouldHaveValidationError()
    {
        var validator = new UpdateProductRequestValidator();
        var request = new UpdateProductRequest("Product", "Description", 0m, 10, "SKU123", true, null);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price must be greater than 0");
    }

    [Fact]
    public void UpdateProductRequestValidator_WhenStockQuantityIsNegative_ShouldHaveValidationError()
    {
        var validator = new UpdateProductRequestValidator();
        var request = new UpdateProductRequest("Product", "Description", 99.99m, -1, "SKU123", true, null);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.StockQuantity)
            .WithErrorMessage("Stock quantity cannot be negative");
    }

    [Fact]
    public void UpdateProductRequestValidator_WhenSkuExceedsMaxLength_ShouldHaveValidationError()
    {
        var validator = new UpdateProductRequestValidator();
        var longSku = new string('A', 51);
        var request = new UpdateProductRequest("Product", "Description", 99.99m, 10, longSku, true, null);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SKU)
            .WithErrorMessage("SKU cannot exceed 50 characters");
    }

    [Fact]
    public void UpdateProductRequestValidator_WhenAllFieldsValid_ShouldNotHaveValidationErrors()
    {
        var validator = new UpdateProductRequestValidator();
        var request = new UpdateProductRequest(
            "Updated Product Name",
            "Updated description",
            149.99m,
            100,
            "SKU-67890",
            true,
            Guid.NewGuid()
        );

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
