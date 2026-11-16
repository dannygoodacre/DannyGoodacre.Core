# Changelog

All packages are released together at the same version.

## Unreleased

### Added

- **identity:** Add username accounts package on ASP.NET Core Identity (#48)

- **cqrs:** Register open generic validators (#51)


### Breaking

- **cqrs:** Replace handler base classes with a decorator pipeline (#47). - Handler and HandlerTestBase classes are removed; use the handler
interfaces, IValidator and IPostCommitHook
- Result/Result<T> factories return IResult/IResult<T> instead of the
concrete types
- ValidationState is now a class, and Error is no longer a positional
record
- DannyGoodacre.Testing no longer has TestBase or LoggerExtensions, and
no longer depends on Moq
- DbContextTransactionUnit is renamed to DbContextTransaction and sealed
- AddCqrsHandlers throws for post-commit hooks with no matching handler
- Cqrs.Testing requires the exact matching Cqrs version

- **cqrs:** Reject duplicate handlers and concrete validator and hook bases (#52). setups with two handlers for one request, or with a
validator or hook derived from a concrete one, now throw at startup.
Make the base abstract, or remove the inheritance.

- **identity:** Remove DannyGoodacre.Identity (#53). DannyGoodacre.Identity is no longer published from this
repository. Use DannyGoodacre.InviteOnly instead.

- **results:** Rename DannyGoodacre.Primitives to DannyGoodacre.Results (#54). the DannyGoodacre.Primitives package is replaced by
DannyGoodacre.Results. Consumers must change their package references
accordingly.


### Changed

- **cqrs:** Scan assemblies once in AddCqrsHandlers (#50)

