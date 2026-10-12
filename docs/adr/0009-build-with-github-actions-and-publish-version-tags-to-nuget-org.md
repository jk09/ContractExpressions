# 0009. Build with GitHub Actions and publish version tags to nuget.org

- Status: Proposed
- Date: 2026-10-12
- Features: –
- Spec: canyon-tundra-ymf1vj

## Context
Nothing built or tested the repository automatically, the libraries target .NET 8, 9 and 10 while the tests ran on
.NET 10 only, and the three packages had no release path.

## Options considered
1. **Manual `dotnet pack` / `dotnet nuget push`** – no setup; unrepeatable, easy to publish an untested build.
2. **GitHub Actions: CI on pushes and pull requests, release on `v*` tags with a `NUGET_API_KEY` secret** – simple,
   versions come from tags; a long-lived API key to create and rotate.
3. **The same with nuget.org trusted publishing (OIDC)** – no long-lived key; needs a trusted-publishing policy on
   nuget.org first.

## Decision
Option 2, chosen by the owner on 2026-10-11. `ci.yml` builds Debug and Release, runs the tests on net8.0, net9.0 and
net10.0 (`TestAllFrameworks=true`), packs and uploads the packages, and runs the spec/feature-docs check on pull
requests. `release.yml` tests and packs a `vX.Y.Z` tag as version X.Y.Z and pushes the packages and symbol packages to
nuget.org. `Directory.Build.props` holds the shared metadata, Source Link and deterministic CI builds.

## Consequences
- The owner creates the `NUGET_API_KEY` secret before the first tag, and rotates it.
- Moving to trusted publishing later only changes the push step (new ADR).
- CI runs every test six times (three frameworks, two configurations).
