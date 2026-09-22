# Validation

Validation in FreshAPI happens at **two levels**, and keeping them separate is the
point:

1. **Input validation** — is the request well-formed? (FluentValidation, runs in the
   MediatR pipeline *before* the handler.)
2. **Domain invariants** — is the operation legal? (the entity itself, enforced via
   the `Result` pattern.)

## Level 1 — FluentValidation in the pipeline

Every command/query can have a validator: `CreateProductCommandValidator`
(`src/Application/Products/Commands/CreateProduct/CreateProductCommandValidator.cs`):

```csharp
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(50).WithMessage("SKU must not exceed 50 characters.")
            .Matches("^[A-Za-z0-9-]+$").WithMessage("SKU may only contain letters, digits and dashes.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Initial stock cannot be negative.");
    }
}
```

Validators are discovered automatically:
`services.AddValidatorsFromAssembly(assembly)` in
`src/Application/DependencyInjection.cs`. The naming convention
`<Operation><Entity>Validator` is all you need — no registration code.

Before the handler runs, `ValidationBehavior<TRequest, TResponse>`
(`src/Application/Common/Behaviors/ValidationBehavior.cs`) executes every
validator for the request, collects all failures, and short-circuits with a single
error:

```text
Code:    Validation.Failed
Message: Name is required.; Price must be greater than zero.
```

The handler never sees invalid input. Because the behavior constrains
`TResponse : Result`, this works for both `Result` and `Result<T>` responses.

To add validation to a new operation: write an
`AbstractValidator<TRequest>` next to the command. That's it.

## Level 2 — Domain invariants

Shape checks ("is the string non-empty?") also appear inside the domain factories,
and that's intentional. `Product.Create` re-checks the name and SKU even though the
validator already did — because **the domain can't trust its callers**. A validator
is an `Application`-layer convenience; the invariant belongs to the entity:

```csharp
public static Result<Product> Create(string name, string sku, Money price, int initialStock)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return Result<Product>.Failure(ProductErrors.InvalidName);
    }
    ...
}
```

Domain errors carry specific codes (`Product.InvalidName`, `Money.NegativeAmount`),
while pipeline failures always use the generic `Validation.Failed` code. In
practice: the pipeline catches 95% of bad input early with friendly messages; the
domain is the un-bypassable backstop.

## Where each check goes — cheat sheet

| Check | Where | Example |
|---|---|---|
| Request shape (required, lengths, formats) | Validator (`Application`) | `Name` ≤ 200 chars, SKU regex |
| Uniqueness / conflicts | Handler (`Application`) | `DuplicateSku` — needs the DB |
| Business invariant | Entity factory/method (`Domain`) | stock can't go negative, SKU normalized to upper case |
| Schema constraints | EF configuration (`Infrastructure`) | `HasMaxLength`, unique index |

The uniqueness check is worth a second look: `CreateProductCommandHandler` queries
for an existing SKU and returns `ProductErrors.DuplicateSku` — and
`ProductConfiguration` also declares `HasIndex(p => p.Sku).IsUnique()` so the
database enforces it against races. Belt and suspenders, at the right layers.
