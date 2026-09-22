# Error Handling

FreshAPI treats errors as **data, not exceptions**. Expected failures (validation,
not found, conflicts) travel through the `Result` pattern all the way to HTTP.
Only truly unexpected failures become exceptions, and those are caught by global
middleware.

## The Result pattern

`src/Domain/Common/Result.cs` and `Error.cs`:

```csharp
// Operation without a return value
Result result = product.AdjustStock(-5);
if (result.IsFailure)
{
    // result.Error is an Error(Code, Message) record
}

// Operation with a return value
Result<Product> result = Product.Create(name, sku, price, stock);
```

`Error` is a `sealed record Error(string Code, string Message)` with factory
methods: `Error.NotFound(...)`, `Error.Validation(...)`, `Error.Conflict(...)`.
Each aggregate keeps its own catalog — `ProductErrors`
(`src/Domain/Entities/ProductErrors.cs`):

```text
Product.NotFound        → "Product with id '…' was not found."
Product.DuplicateSku    → "A product with SKU '…' already exists."
Product.InvalidName     → "Product name must not be empty."
Product.NameTooLong     → "Product name must not exceed 200 characters."
Product.InvalidSku / Product.SkuTooLong
Product.NegativeStock / Product.InsufficientStock
```

Conventions to follow for new aggregates: `<Entity>.<Reason>` codes, one
`*Errors` static class per aggregate, messages written for API consumers
(not developers).

## Result → HTTP

Handlers return `Result`; controllers map it with
`src/Api/Common/ResultExtensions.cs`:

```csharp
return error.Code switch
{
    "Product.NotFound" => new NotFoundObjectResult(body),
    _ => new BadRequestObjectResult(body),
};
```

So:

| Result | HTTP |
|---|---|
| `Result<T>` success | `200 OK` with the value |
| `Result` success | `204 No Content` |
| `Result<Guid>` from create | controller returns `201 Created` with the GUID (see `ProductsController.Create`) |
| `*.NotFound` | `404 Not Found` |
| anything else (`Validation.Failed`, `Product.DuplicateSku`, …) | `400 Bad Request` |

The response body is `{ "code": "…", "message": "…" }` — machine-readable codes for
clients, human-readable messages for people.

> **Gotcha when adding entities:** the switch is hard-coded per aggregate. Add your
> `"Customer.NotFound"` case or it will fall through to 400. The
> [03 — Adding an entity](03-adding-an-entity.md) tutorial shows exactly where.

## The exception middleware

`src/Api/Middleware/ExceptionHandlingMiddleware.cs` is registered first in
`Program.cs` (`app.UseMiddleware<ExceptionHandlingMiddleware>()`), so it catches
everything that escapes the handlers:

- `DomainException` → **422 Unprocessable Entity** — "A domain rule was violated."
- anything else → **500 Internal Server Error** — "An unexpected error occurred."

Both are returned as **RFC 7807 problem details** (`application/problem+json`).
Stack traces are included only in Development (`exception.ToString()`); production
gets `Detail: null`, with the full exception logged via Serilog.

`DomainException` (`src/Domain/Exceptions/DomainException.cs`) exists for the
rare case where an invariant is violated somewhere the `Result` pattern can't
reach — e.g. inside infrastructure code. Prefer returning `Result` everywhere else;
you'll almost never throw it.

## The whole picture

```text
Expected failure      → Result.Failure(Error) → controller → 200/201/204/400/404
Validation failure    → Validation.Failed      → controller → 400
Domain invariant hit  → DomainException        → middleware → 422
Bug / outage          → Exception              → middleware → 500 (logged, no leak)
```

No `try/catch` in handlers, no `throw` for business failures, no stack traces
leaking to clients. Add new error cases by adding codes to your `*Errors` class —
never by inventing new HTTP mappings in controllers.
