# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

_nothing changed yet_


## [v0.1.0] - 2025-08-28

_Inital release_

### Added

- `CallMonitorClient` as client to connect to the call monitor endpoint
- `CallMonitorEventArgs` are the custom arugments, when `OnEvent` is raised.
- Notifying about
  - `Ring`: An incoming call
  - `Call`: An outgoing call
  - `Connect`: The call is answered
  - `Disconnect`: One party has hung up
- An unknown caller means, the `CallerNumber` is empty



[Unreleased]: https://github.com/AM-WD/FritzCallMonitor/compare/v0.1.0...HEAD

[v0.1.0]: https://github.com/AM-WD/FritzCallMonitor/commits/v0.1.0
