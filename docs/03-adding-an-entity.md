# Adding an Entity (Tutorial)

This is the flagship tutorial: we'll add a **`Customer`** aggregate end to end —
domain entity, application use cases, persistence, API endpoints, and a migration —
and along the way you'll see exactly which layer each piece belongs in and why.

The finished work mirrors how `Product` is already built. If you ever wonder
"what should this look like?", open the `Product` equivalent — that's your template.

**What we're adding:** customers with a name and an email address. Email must be
unique and look like an email. Operations: create, get by id, list (paged + search),
update, delete.

## Step 1 — The domain entity (`Domain`)

**Rule: every business rule lives in `Domain`, and `Domain` references nothing.**

Create `src/Domain/Entities/Customer.cs`:

```csharp
using FreshApi.Domain.Common;

namespace FreshApi.Domain.Entities;

/// <summary>
/// Customer aggregate root. Email is unique and validated on creation.
/// </summary>
public sealed class Customer : Entity
{
    public const int MaxNameLength = 200;
    public const int MaxEmailLength = 320;

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    // Required by EF Core.
    private Customer()
    {
    }

    private Customer(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public static Result<Customer> Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Customer>.Failure(CustomerErrors.InvalidName);
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return Result<Customer>.Failure(CustomerErrors.NameTooLong);
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<Customer>.Failure(CustomerErrors.InvalidEmail);
        }

        email = email.Trim().ToLowerInvariant();

        if (email.Length > MaxEmailLength || !email.Contains('@'))
        {
            return Result<Customer>.Failure(CustomerErrors.InvalidEmail);
        }

        return new Customer(name.Trim(), email);
    }

    public Result UpdateDetails(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(CustomerErrors.InvalidName);
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return Result.Failure(CustomerErrors.NameTooLong);
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Trim().Contains('@'))
        {
            return Result.Failure(CustomerErrors.InvalidEmail);
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();

        return Result.Success();
    }
}
```

Why a `Create` factory instead of a public constructor? Two reasons, both visible in
`Product.Create`:

1. **Invariants in one place.** Every caller gets the same validation — no one can
   construct an invalid `Customer`, and the checks can't drift between call sites.
2. **No exceptions for expected failures.** Bad input returns
   `Result<Customer>.Failure(...)` with a typed `Error`, exactly like the rest of
   the kit (see [06 — Error handling](06-error-handling.md)).

The setters are `private` so the only way to change state is through `Create` /
`UpdateDetails`. The parameterless private constructor exists only for EF Core.

Now the error catalog — create `src/Domain/Entities/CustomerErrors.cs`, following
`ProductErrors`:

```csharp
using FreshApi.Domain.Common;

namespace FreshApi.Domain.Entities;

public static class CustomerErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Customer.NotFound", $"Customer with id '{id}' was not found.");

    public static Error DuplicateEmail(string email) =>
        Error.Conflict("Customer.DuplicateEmail", $"A customer with email '{email}' already exists.");

    public static Error InvalidName =>
        Error.Validation("Customer.InvalidName", "Customer name must not be empty.");

    public static Error NameTooLong =>
        Error.Validation("Customer.NameTooLong", $"Customer name must not exceed {Customer.MaxNameLength} characters.");

    public static Error InvalidEmail =>
        Error.Validation("Customer.InvalidEmail", "Customer email must be a valid email address.");
}
```

`Error` is a `record` with a machine-readable `Code` — the controller maps
`Customer.NotFound` to HTTP 404 later (step 5).

> **Checkpoint:** `dotnet build src/Domain` should succeed.

## Step 2 — The application layer (`Application`)

**Rule: use cases orchestrate; they don't invent business rules.** Every operation is
one MediatR request (`IRequest<Result<…>>`) with one handler and one FluentValidation
validator. Handlers talk to the `IUnitOfWork` / `IApplicationDbContext` interfaces —
never to EF Core.

### The DTO

Create `src/Application/Customers/CustomerDto.cs`:

```csharp
namespace FreshApi.Application.Customers;

/// <summary>
/// Read model for customers. Kept as a record: immutable, cheap to construct.
/// </summary>
public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    DateTimeOffset CreatedAt);
```

And the mapping, `src/Application/Customers/Mappings/CustomerMapping.cs`
(mirroring `ProductMapping` — manual mapping, no extra dependency):

```csharp
using FreshApi.Domain.Entities;

namespace FreshApi.Application.Customers.Mappings;

public static class CustomerMapping
{
    public static CustomerDto ToDto(this Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        return new CustomerDto(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.CreatedAt);
    }
}
```

### Create command

Create `src/Application/Customers/Commands/CreateCustomer/CreateCustomerCommand.cs`:

```csharp
using FreshApi.Domain.Common;
using MediatR;

namespace FreshApi.Application.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(
    string Name,
    string Email) : IRequest<Result<Guid>>;
```

Handler — `CreateCustomerCommandHandler.cs`. Compare with
`CreateProductCommandHandler`: check the uniqueness rule, build the entity through
the domain factory, save through the unit of work:

```csharp
using FreshApi.Application.Common.Interfaces;
using FreshApi.Domain.Common;
using FreshApi.Domain.Entities;
using MediatR;

namespace FreshApi.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Customer>();

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await repository.FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            return Result<Guid>.Failure(CustomerErrors.DuplicateEmail(normalizedEmail));
        }

        var customerResult = Customer.Create(request.Name, request.Email);
        if (customerResult.IsFailure)
        {
            return Result<Guid>.Failure(customerResult.Error);
        }

        await repository.AddAsync(customerResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return customerResult.Value.Id;
    }
}
```

Note where the uniqueness check lives: the **handler**, not the entity — it needs the
database, and `Domain` has no database access. The entity still enforces the shape
rules. This split is deliberate.

Validator — `CreateCustomerCommandValidator.cs` (runs in the pipeline *before* the
handler, see [05 — Validation](05-validation.md)):

```csharp
using FluentValidation;

namespace FreshApi.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(320).WithMessage("Email must not exceed 320 characters.");
    }
}
```

> **Naming convention:** `<Operation><Entity><Kind>` — command, handler, validator —
> each in its own file under `Commands/<Operation><Entity>/`. MediatR and the
> validators are auto-registered from the assembly in
> `src/Application/DependencyInjection.cs`, so new handlers need **no registration
> code**.

### Update and delete commands

`UpdateCustomerCommand.cs`:

```csharp
using FreshApi.Domain.Common;
using MediatR;

namespace FreshApi.Application.Customers.Commands.UpdateCustomer;

public sealed record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string Email) : IRequest<Result>;
```

`UpdateCustomerCommandHandler.cs` (mirrors `UpdateProductCommandHandler`):

```csharp
using FreshApi.Application.Common.Interfaces;
using FreshApi.Domain.Common;
using FreshApi.Domain.Entities;
using MediatR;

namespace FreshApi.Application.Customers.Commands.UpdateCustomer;

public sealed class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Customer>();

        var customer = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (customer is null)
        {
            return Result.Failure(CustomerErrors.NotFound(request.Id));
        }

        var updateResult = customer.UpdateDetails(request.Name, request.Email);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        repository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

`DeleteCustomerCommand.cs` — `public sealed record DeleteCustomerCommand(Guid Id) : IRequest<Result>;`
and a `DeleteCustomerCommandHandler` mirroring `DeleteProductCommandHandler`
(load → `NotFound` → `repository.Remove` → save). Add validators for both, matching
`UpdateProductCommandValidator` (note: `UpdateProductCommandValidator` validates
`x.Id` with `.NotEmpty()` — do the same for update/delete).

### Queries

**Rule: queries read through `IApplicationDbContext` directly** (no unit of work),
with `.AsNoTracking()` for speed, and project to DTOs.

`GetCustomerByIdQuery.cs`:

```csharp
using FreshApi.Application.Customers;
using FreshApi.Domain.Common;
using MediatR;

namespace FreshApi.Application.Customers.Queries.GetCustomerById;

public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDto>>;
```

`GetCustomerByIdQueryHandler.cs` (mirrors `GetProductByIdQueryHandler`):

```csharp
using FreshApi.Application.Common.Interfaces;
using FreshApi.Application.Customers;
using FreshApi.Application.Customers.Mappings;
using FreshApi.Domain.Common;
using FreshApi.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FreshApi.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCustomerByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CustomerDto>> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (customer is null)
        {
            return Result<CustomerDto>.Failure(CustomerErrors.NotFound(request.Id));
        }

        return customer.ToDto();
    }
}
```

For the list query, copy the `GetProducts` pattern: a
`GetCustomersQuery(string? SearchTerm, int Page = 1, int PageSize = 20)` returning
`Result<PagedResult<CustomerDto>>`, searching `Name` and `Email`, ordered by name —
same shape as `GetProductsQueryHandler` in
`src/Application/Products/Queries/GetProducts/`.

> **Checkpoint:** `dotnet build src/Application` succeeds — but `_context.Customers`
> doesn't exist yet. That's step 3.

## Step 3 — Persistence (`Infrastructure`)

**Rule: EF Core is an outside detail.** `Application` only declared the interface;
now `Infrastructure` makes it real.

1. **Expose the set** on the interface —
   `src/Application/Common/Interfaces/IApplicationDbContext.cs`:

   ```csharp
   public interface IApplicationDbContext
   {
       DbSet<Product> Products { get; }
       DbSet<Customer> Customers { get; }   // add this

       Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
   }
   ```

   (Yes, this file lives in `Application` — the interface belongs to the use-case
   layer; the implementation below belongs to `Infrastructure`.)

2. **Implement it** in `src/Infrastructure/Persistence/ApplicationDbContext.cs`:

   ```csharp
   public DbSet<Customer> Customers => Set<Customer>();
   ```

3. **Map the table** — create
   `src/Infrastructure/Persistence/Configurations/CustomerConfiguration.cs`
   (mirrors `ProductConfiguration`; `OnModelCreating` picks it up automatically via
   `ApplyConfigurationsFromAssembly`):

   ```csharp
   using FreshApi.Domain.Entities;
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;

   namespace FreshApi.Infrastructure.Persistence.Configurations;

   public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
   {
       public void Configure(EntityTypeBuilder<Customer> builder)
       {
           builder.ToTable("Customers");

           builder.HasKey(c => c.Id);

           builder.Property(c => c.Name)
               .IsRequired()
               .HasMaxLength(Customer.MaxNameLength);

           builder.Property(c => c.Email)
               .IsRequired()
               .HasMaxLength(Customer.MaxEmailLength);

           builder.HasIndex(c => c.Email).IsUnique();

           builder.Property(c => c.CreatedAt).IsRequired();
       }
   }
   ```

   The unique index on `Email` is the database-level backstop for the
   `DuplicateEmail` check in the handler (two concurrent creates could otherwise
   race).

> **Checkpoint:** `dotnet build` on the solution succeeds.

## Step 4 — The migration

**Rule: the schema is generated, not hand-written.** Run from the repo root:

```bash
dotnet ef migrations add AddCustomers --project src/Infrastructure --startup-project src/Api
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

This works without running the API because
`src/Infrastructure/Persistence/ApplicationDbContextFactory.cs` implements
`IDesignTimeDbContextFactory<ApplicationDbContext>`. See [04 — EF Core](04-ef-core.md).

## Step 5 — The controller (`Api`)

**Rule: controllers are thin.** One action = send one MediatR request, map the
`Result`. Create `src/Api/Controllers/CustomersController.cs`, mirroring
`ProductsController`:

```csharp
using FreshApi.Api.Common;
using FreshApi.Application.Common.Models;
using FreshApi.Application.Customers;
using FreshApi.Application.Customers.Commands.CreateCustomer;
using FreshApi.Application.Customers.Commands.DeleteCustomer;
using FreshApi.Application.Customers.Commands.UpdateCustomer;
using FreshApi.Application.Customers.Queries.GetCustomerById;
using FreshApi.Application.Customers.Queries.GetCustomers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FreshApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public sealed class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetCustomersQuery(search, page, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateCustomerCommand(id, request.Name, request.Email),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCustomerCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    public sealed record UpdateCustomerRequest(string Name, string Email);
}
```

One more thing: `src/Api/Common/ResultExtensions.cs` maps errors to HTTP status
codes with a hard-coded switch:

```csharp
return error.Code switch
{
    "Product.NotFound" => new NotFoundObjectResult(body),
    _ => new BadRequestObjectResult(body),
};
```

`"Customer.NotFound"` would fall through to 400 — wrong. Add a case:

```csharp
"Customer.NotFound" => new NotFoundObjectResult(body),
```

(If you add several aggregates, consider replacing the switch with
`error.Code.EndsWith(".NotFound")` → 404.)

> **Checkpoint:** `dotnet build` succeeds.

## Step 6 — Try it

```bash
dotnet run --project src/Api
```

```bash
# Create
curl -X POST http://localhost:8080/api/customers \
  -H "Content-Type: application/json" \
  -d '{"name":"Ada Lovelace","email":"ada@example.com"}'
# → 201 Created with the new GUID

# Create with a bad email
curl -X POST http://localhost:8080/api/customers \
  -H "Content-Type: application/json" \
  -d '{"name":"Ada","email":"not-an-email"}'
# → 400 BadRequest, "Validation.Failed: Email must be a valid email address."

# Duplicate email
curl -X POST http://localhost:8080/api/customers \
  -H "Content-Type: application/json" \
  -d '{"name":"Ada Again","email":"ada@example.com"}'
# → 400 BadRequest, "Customer.DuplicateEmail"

# Get a missing customer
curl http://localhost:8080/api/customers/00000000-0000-0000-0000-000000000000
# → 404 NotFound
```

## Step 7 — Test it (optional but recommended)

Follow the existing test layout ([07 — Testing](07-testing.md)):

- `tests/Domain.Tests/CustomerTests.cs` — `Customer.Create` with valid data succeeds
  and normalizes the email; empty name and bad email return the right error codes
  (mirrors `ProductTests`).
- `tests/Application.Tests/CreateCustomerCommandHandlerTests.cs` — mock
  `IUnitOfWork`/`IRepository<Customer>` with NSubstitute; duplicate email returns
  `Customer.DuplicateEmail` without saving (mirrors
  `CreateProductCommandHandlerTests`).

## Recap — what went where, and why

| Piece | Layer | Why |
|---|---|---|
| `Customer`, `CustomerErrors` | `Domain` | Business rules; no dependencies |
| `CustomerDto`, commands/queries, handlers, validators, mapping | `Application` | Use cases; depends on `Domain` only |
| `DbSet` on `IApplicationDbContext` | `Application` (interface) | Use cases declare what they need |
| `ApplicationDbContext`, `CustomerConfiguration`, repository impl | `Infrastructure` | EF Core is an outside detail |
| `CustomersController`, `ResultExtensions` case | `Api` | Thin HTTP; send + map `Result` |
| Migration | tooling (`dotnet ef`) | Schema generated from the model |

That's the whole pattern. Every future aggregate follows these same seven steps.
