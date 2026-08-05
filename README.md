# Lights to Flag 2

A motorsport **career simulation** — inspired by the original *Lights to Flag* —
built ground-up in **C# / .NET 9** with an **Avalonia** UI. It ships for **Windows and
macOS**; Linux is used as a CI/build harness, not a release target.

The engine is data-driven and deterministic, and is deliberately split from the UI so
every layer — including the interface — builds and is tested on CI (Linux, plus Windows
and macOS, the ship targets).

> **Status:** Phase 0 / M0 — solution skeleton. The engine and UI are being built
> milestone by milestone. See [`ROADMAP.md`](ROADMAP.md) (Turkish) for the full plan
> and [`docs/adr/`](docs/adr/) for the architecture decisions behind it.

## What it is

You play as a **Team Principal** — run the budget, sponsors, staff, R&D and driver
transfers, and call both cars' strategy from the pit wall, under board and media
pressure. A Driver Career mode is shelved for now (a possible post-1.0 addition).

Online 4-player co-op (Football-Manager style) is planned as a post-1.0 phase.

## Solution layout

```
LightsToFlag2.sln
├─ src/
│  ├─ LTF.Domain/        Pure model. No I/O, no dependencies.
│  ├─ LTF.Content/       Carset format: schema, loader, validator.
│  ├─ LTF.Simulation/    Practice / qualifying / race. Deterministic.
│  ├─ LTF.Career/        Season, career, economy, contracts, R&D, transfers.
│  ├─ LTF.Persistence/   Save / load + schema migration.
│  ├─ LTF.App/           Avalonia UI. All platforms.
│  └─ LTF.Tools/         CLI: validator, balance sweep, tooling.
└─ tests/                One xUnit project per engine layer + the app.
```

Dependencies flow one way: `App → Career → Simulation → Content → Domain`.

## Build & test

Requires the **.NET 9 SDK**. Everything builds and tests on any OS:

```bash
dotnet build LightsToFlag2.sln -c Release
dotnet test  LightsToFlag2.sln -c Release
```

Run the desktop app:

```bash
dotnet run --project src/LTF.App
```

CI: `.github/workflows/ci.yml` builds and tests the **whole solution** on Linux (fast
harness); `build-matrix.yml` builds and tests it on **Windows and macOS**, the ship
targets.

## Conventions

Enforced by `Directory.Build.props` and guard tests:

- `Nullable` enabled, warnings treated as errors, latest C#.
- `Domain`, `Content`, `Simulation`, `Career` stay UI-free and I/O-free.
- The simulation is **deterministic**: no wall-clock, no shared RNG — same seed,
  same result, bit for bit.
- `InvariantGlobalization` is never enabled (it crashed the previous version's
  New Career screen with a `CultureNotFoundException`).

## History

This is a from-scratch rebuild. The previous .NET 8 + WPF version is preserved in
git history (commit `a83ebcc`); see [ADR-0001](docs/adr/0001-ground-up-rebuild.md)
for why it was replaced.
