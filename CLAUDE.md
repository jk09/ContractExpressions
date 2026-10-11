## Workflow

- Build and test: `dotnet build ContractExpressions.sln` and `dotnet test ContractExpressions.sln`.
- Non-trivial work starts from a spec in the [spec folder](./docs/specs/). No spec → use the [`spec`](.claude/skills/spec/) skill first.
- Finish every task with the [`ship`](.claude/skills/ship/) skill. Don't commit ad hoc.
- Commits: [Conventional Commits](https://www.conventionalcommits.org/), scope = feature folder name, `Spec: <spec-id>` line when a spec drove the change. Format in the `ship` skill.

## Features

- Docs are written by the [`document-feature`](.claude/skills/document-feature/) skill; the Stop hook blocks finishing while feature code or the active spec changed without its docs.
- Decisions go in `docs/adr/`, append-only: a changed decision gets a new ADR that supersedes the old one.
- Every feature flag has an owner and a removal date in its feature README. The [`audit-features`](.claude/skills/audit-features/) skill lists overdue flags and sprawl.

## Where instructions go

- Short rules that always apply → this file, a few lines. Feature-specific rules → that feature's `CLAUDE.md`.
- Step-by-step procedures → a skill in `.claude/skills/`.
- Steps that must run at a set moment → hooks in `.claude/settings.json` (scripts in `.claude/scripts/`, PowerShell 7).
