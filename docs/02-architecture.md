# Architecture

FreshAPI follows Clean Architecture: the business rules at the center, use cases around
them, and all the infrastructure (database, web, auth) on the outside.

## The six projects

| Project | Namespace root | Depends on | Job |
|---|---|---|---|
| `FreshApi.Domain` | `FreshApi.Domain.*` | nothing | Entities, value objects, errors, domain events |
| `FreshApi.Application` | `FreshApi.Application.*` | Domain | Use cases: MediatR commands/queries, handlers, validators, DTOs |
| `FreshApi.Infrastructure` | `FreshApi.Infrastructure.*` | Application | EF Core `DbContext`, entity configurations, repository, unit of work |
| `FreshApi.Api` | `FreshApi.Api.*` | Application, Infrastructure | Controllers, middleware, Swagger, auth, `Program.cs` |
| `FreshApi.Domain.Tests` | `FreshApi.Domain.Tests` | Domain | xUnit tests of domain rules |
| `FreshApi.Application.Tests` | `FreshApi.Application.Tests` | Application | xUnit + NSubstitute handler tests |

## The dependency rule

```
Api → Infrastructure → Application → Domain
```

Dependencies point **inward only**. `Domain` references nothing. `Application`
references `Domain` only. Concretely:

- `Domain` never imports `Microsoft.EntityFrameworkCore`, `MediatR`, or
  `Microsoft.AspNetCore.*`. Business rules must not know about the database or the web.
- `Application` defines **interfaces** (`IRepository<T>`, `IUnitOfWork`,
  `IApplicationDbContext` in `src/Application/Common/Interfaces/`) and `Infrastructure`
  implements them. The use-case layer depends on abstractions, never on EF Core.
- `Api` is the composition root: `Program.cs` calls `AddApplication()` and
  `AddInfrastructure(configuration)` and wires everything together.

If you catch yourself adding a project reference that breaks this chain, that's a
design smell — put an interface in `Application` and implement it in `Infrastructure`
instead.

## How a request flows

```
HTTP → ProductsController → MediatR ISender
      → ValidationBehavior (FluentValidation)
      → Handler (commands: IUnitOfWork / queries: IApplicationDbContext)
      → Domain (Product.Create / UpdateDetails / AdjustStock return Result)
      → ResultExtensions → 200 / 201 / 204 / 400 / 404
```

Every operation is one MediatR request with its own handler:

- **Commands** change state and go through `IUnitOfWork`
  (e.g. `CreateProductCommand` → `CreateProductCommandHandler` in
  `src/Application/Products/Commands/CreateProduct/`).
- **Queries** read state and go straight at `IApplicationDbContext`
  (e.g. `GetProductsQuery` → `GetProductsQueryHandler`, using
  `.AsNoTracking()` for speed) in `src/Application/Products/Queries/`.

Handlers return `Result` / `Result<T>` — never throw for expected failures.
The controller is thin: `_sender.Send(command)` then `result.ToActionResult()`
(see `src/Api/Common/ResultExtensions.cs` and `src/Api/Controllers/ProductsController.cs`).

## The building blocks

- **`Entity`** (`src/Domain/Common/Entity.cs`) — base class for aggregates:
  `Guid` id, `CreatedAt`/`UpdatedAt` audit timestamps, and a domain-event list.
  The setters are `private`/`protected` so state only changes through behavior methods.
- **`ValueObject`** (`src/Domain/Common/ValueObject.cs`) — structural equality by
  components. `Money` (`src/Domain/ValueObjects/Money.cs`) is the sample: amount +
  ISO currency, non-negative, normalized, created via `Money.Create(...)` returning
  `Result<Money>`.
- **`Result` / `Error`** (`src/Domain/Common/Result.cs`, `Error.cs`) — the
  typed-failure pattern. `ProductErrors` (`src/Domain/Entities/ProductErrors.cs`)
  defines the catalog: `Product.NotFound`, `Product.DuplicateSku`,
  `Product.InvalidName`, etc.
- **Domain events** — `Product.Create` raises a `ProductCreatedDomainEvent`
  (`src/Domain/Events/`). There is no dispatcher wired up yet; events are collected
  on the entity and available via `entity.DomainEvents` for you to publish
  (e.g. from `SaveChangesAsync` in the future).
- **DTOs** — `ProductDto` is a plain record in `Application`; manual mapping lives in
  `src/Application/Products/Mappings/ProductMapping.cs` (no AutoMapper dependency).

## What lives where — the rule of thumb

| You are adding… | It belongs in… | Why |
|---|---|---|
| A business rule or invariant | `Domain` (entity/VO method) | It must hold no matter who calls |
| A use case ("create X") | `Application` (command + handler) | Orchestration, not business logic |
| A read model / query | `Application` (query + handler, DTO) | Queries can shape data freely |
| Input shape validation | `Application` (FluentValidation validator) | Runs in the pipeline before the handler |
| DB mapping / SQL | `Infrastructure` (configuration, context) | EF Core is an outside detail |
| HTTP concerns | `Api` (controller, middleware) | Thin: send + map the `Result` |

Work through [03 — Adding an entity](03-adding-an-entity.md) to see this in action.
