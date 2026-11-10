# DannyGoodacre.Core

A set of .NET 10 libraries for structuring application logic: a result type for expected outcomes, and a CQRS pipeline that handles logging, validation, transactions, and persistence around your handlers.

## Packages

| Package | Purpose | Documentation |
| --- | --- | --- |
| `DannyGoodacre.Primitives` | Result types, `Error`, and `ValidationState`. Methods return a result for each expected outcome instead of throwing. No dependencies. | [README](./src/DannyGoodacre.Primitives/README.md) |
| `DannyGoodacre.Cqrs` | Commands, queries, and the decorator pipeline that runs them. Handlers contain only domain logic. | [README](./src/DannyGoodacre.Cqrs/README.md) |
| `DannyGoodacre.Cqrs.EntityFrameworkCore` | The EF Core implementation of the pipeline's persistence interfaces. | [README](./src/DannyGoodacre.Cqrs.EntityFrameworkCore/README.md) |
| `DannyGoodacre.Testing` | xUnit assertions for results, `ValidationState`, and `Error`. | [README](./src/DannyGoodacre.Testing/README.md) |
| `DannyGoodacre.Cqrs.Testing` | Test contexts for the pipeline decorators, built on xUnit and Moq. | [README](./src/DannyGoodacre.Cqrs.Testing/README.md) |

`DannyGoodacre.Cqrs` builds on `DannyGoodacre.Primitives`, and each testing package builds on the package it tests. `DannyGoodacre.Primitives` can be used on its own.

## Installation

```shell
dotnet add package DannyGoodacre.Cqrs
dotnet add package DannyGoodacre.Cqrs.EntityFrameworkCore
```

The packages are currently published to GitHub Packages only; publication to nuget.org is planned. Until then, add the feed with a GitHub token that has the `read:packages` scope:

```shell
dotnet nuget add source https://nuget.pkg.github.com/dannygoodacre/index.json --name dannygoodacre --username <github-username> --password <token>
```

## Example

[`src/DannyGoodacre.Cqrs.Examples`](./src/DannyGoodacre.Cqrs.Examples) is a customer onboarding workflow on SQLite that uses every part of the pipeline, and [`tests/DannyGoodacre.Cqrs.Examples.Tests`](./tests/DannyGoodacre.Cqrs.Examples.Tests) tests it through the full pipeline. The [examples guide](./src/DannyGoodacre.Cqrs/docs/EXAMPLES.md) walks through both.

```shell
dotnet run --project src/DannyGoodacre.Cqrs.Examples
```

## Building from source

```shell
dotnet build
dotnet test
```

The solution is [`DannyGoodacre.Core.slnx`](./DannyGoodacre.Core.slnx), with the libraries and the example under `src/` and the tests under `tests/`. Building requires the .NET 10 SDK. The tests use xUnit v3 on Microsoft.Testing.Platform, which [`global.json`](./global.json) enables for `dotnet test`.

## License

[LGPL-3.0-or-later](./LICENSE).
