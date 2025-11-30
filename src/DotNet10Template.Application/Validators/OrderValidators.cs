using DotNet10Template.Application.DTOs.Orders;
using FluentValidation;

namespace DotNet10Template.Application.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.ShippingAddress)
            .MaximumLength(500).WithMessage("Shipping address cannot exceed 500 characters");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemRequestValidator());
    }
}

public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");
    }
}
