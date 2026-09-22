# Testing

Two test projects, two levels:

| Project | Tests | Tools |
|---|---|---|
| `tests/Domain.Tests` | Business rules — factories, invariants, value objects | xUnit |
| `tests/Application.Tests` | Command/query handlers — orchestration logic | xUnit + NSubstitute |

Run everything from the repo root:

```bash
dotnet test
```

## Domain tests

`tests/Domain.Tests/ProductTests.cs` tests `Product` directly — no mocks, no
database, no DI. Because factories return `Result`, assertions check
`IsSuccess`/`IsFailure` and the error `Code`:

```csharp
[Fact]
public void Create_WithValidData_ReturnsSuccessWithNormalizedValues()
{
    var result = Product.Create("  Widget  ", "wgt-001", ValidPrice(), 10);

    Assert.True(result.IsSuccess);
    Assert.Equal("Widget", result.Value.Name);
    Assert.Equal("WGT-001", result.Value.Sku);   // normalized by the factory
    Assert.Equal(10, result.Value.StockQuantity);
    Assert.Single(result.Value.DomainEvents);    // ProductCreatedDomainEvent raised
}

[Fact]
public void Create_WithEmptyName_ReturnsFailure()
{
    var result = Product.Create("   ", "WGT-001", ValidPrice(), 10);

    Assert.True(result.IsFailure);
    Assert.Equal("Product.InvalidName", result.Error.Code);
}
```

Write one of these for every factory method and behavior method on a new entity
(`Customer.Create`, `Customer.UpdateDetails` — see the [03 tutorial](03-adding-an-entity.md)).
They're fast, they pin down the invariants, and they document the rules better than
comments do.

## Application tests

`tests/Application.Tests/CreateProductCommandHandlerTests.cs` tests the handler
with the database mocked out via NSubstitute:

```csharp
public sealed class CreateProductCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Product> _repository;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _repository = Substitute.For<IRepository<Product>>();
        _unitOfWork.Repository<Product>().Returns(_repository);
        _handler = new CreateProductCommandHandler(_unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesProductAndSaves()
    {
        _repository
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var result = await _handler.Handle(
            new CreateProductCommand("Widget", "WGT-001", 19.99m, "USD", 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _repository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateSku_ReturnsFailureWithoutSaving()
    {
        ...
        Assert.True(result.IsFailure);
        Assert.Equal("Product.DuplicateSku", result.Error.Code);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

The pattern: substitute `IUnitOfWork` and `IRepository<T>`, control what the
repository returns, assert on the `Result` **and** on the interactions
(`Received(1)`, `DidNotReceive()`). The duplicate-SKU test is the valuable one —
it proves the handler short-circuits *without* saving.

Note that these tests exercise the handler directly, not the FluentValidation
pipeline — `ValidationBehavior` runs in the MediatR pipeline at runtime, not when
you call `Handle` yourself. If you want to test a validator, instantiate it and
call `Validate` directly.

## What's not covered (yet)

- **No integration tests** against a real database — the kit ships unit tests only.
  A common next step is an `Infrastructure`-level test project using the EF Core
  in-memory provider or Testcontainers with SQL Server.
- **No API/controller tests** — controllers are intentionally thin
  (send + `ToActionResult()`), so the valuable coverage is at the handler level.

## Adding tests for a new entity

1. `tests/Domain.Tests/<Entity>Tests.cs` — mirror `ProductTests`.
2. `tests/Application.Tests/Create<Entity>CommandHandlerTests.cs` — mirror
   `CreateProductCommandHandlerTests` (valid path, duplicate path, invalid-input path).
3. `dotnet test` — both projects run; CI runs them on every push (see
   [09 — GitHub Actions](09-github-actions.md) and [10 — Azure DevOps](10-azure-devops.md)).
