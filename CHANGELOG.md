# Changelog

All packages are released together at the same version.

## [1.0.0-alpha.2] - 2026-11-17

### Added

- **results:** Add ValidationState.AddErrorIf (#64)

### Breaking

- **cqrs:** Log elapsed time on request completion (#66). The message text of log events 1002, 1004, 1005 and 1006 has changed to include the elapsed time, for example "'X' failed after 8.6ms: {Result}". Anything that matches on the old text must be updated; match on the event ID instead.

## [1.0.0-alpha.1] - 2026-11-16

First release of the 1.0 line.

Upgrading from 0.x: `DannyGoodacre.Primitives` is now `DannyGoodacre.Results`, and `DannyGoodacre.Identity` has been replaced by `DannyGoodacre.InviteOnly`, found in its [own repository](https://github.com/dannygoodacre/DannyGoodacre.InviteOnly).

Changes before 1.0.0-alpha.1 are not listed here. See the [commit history](https://github.com/dannygoodacre/DannyGoodacre.Core/commits/main).
