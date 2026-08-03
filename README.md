# Lights to Flag 2

A ground-up rewrite of the **Lights to Flag** F1 management / racing-simulation
game — **.NET 8 + WPF (MVVM)**, C#, Windows single-player career mode.

The original was a compiled .NET/VB.NET WinForms app. This project is a clean
reimplementation: mechanics are *freshly designed* (inspired by the original,
not ported line-for-line), and the game engine is deliberately split from the UI
so it can be built and unit-tested on any OS — including Linux CI.

## Solution layout

```
LightsToFlag.sln
├─ src/
│  ├─ LightsToFlag.Core/   net8.0, UI-free engine (Domain / Data / … )   ← testable everywhere
│  └─ LightsToFlag.App/    net8.0-windows, WPF shell (MVVM)              ← Windows only
└─ tests/
   └─ LightsToFlag.Tests/  net8.0, xUnit — references Core only          ← runs on Linux
```

- **`LightsToFlag.Core`** — domain models, carset loading, and (coming) the
  race/qualifying/practice simulation, season & career logic, and save/load.
  No UI or Windows dependencies; a unit test guards that invariant.
- **`LightsToFlag.App`** — the WPF desktop shell. Targets `net8.0-windows` and
  uses WPF, so it builds and runs **only on Windows**. It is excluded from the
  Linux CI build via the `LightsToFlag.CI.slnf` solution filter.
- **`LightsToFlag.Tests`** — xUnit tests for Core, runnable on any OS.

## Build & test

Engine + tests (any OS, no Windows needed):

```bash
dotnet test LightsToFlag.CI.slnf -c Release
```

Full game incl. the WPF app (Windows only):

```bash
dotnet build LightsToFlag.sln -c Release
dotnet run --project src/LightsToFlag.App
```

CI (`.github/workflows/ci.yml`) builds and tests **Core + Tests** on
`ubuntu-latest` using the solution filter, so the Windows-only App never blocks
Linux CI.

## Carsets (game data)

The game is data-driven by **carsets** — folders of underscore-separated text
files describing the series, teams, drivers, circuits and simulation tuning.
The classic format is kept as the canonical input for compatibility with
existing content; `LightsToFlag.Core.Data.LegacyTextCarsetLoader` parses it into
clean immutable domain records.

A real carset (`carsets/F1 2019/`) ships with the repo as test data and initial
playable content. Field schemas are documented per file:

| File | Contents |
|------|----------|
| `Rules.txt` | Series regulations, points formats, qualifying format |
| `Coefficients.txt` | Global simulation tuning (27 values) |
| `Teamdata.txt` | Teams / cars (26 fields each) |
| `Driverdata.txt` | Race drivers (24 fields) + reserves (abbreviated rookie records) |
| `Circuitdata.txt` | Circuits (variable-width: per-class laptimes + corner/overtaking strings) |

## Roadmap

Engine milestones (verifiable on Linux CI):

- [x] **M0** — solution scaffold, CI, solution filter
- [x] **M1** — Core domain model
- [x] **M2** — legacy carset loader + real-data tests
- [ ] **M3** — seedable RNG + lap-time core
- [ ] **M4** — qualifying + practice
- [ ] **M5** — full race simulator
- [ ] **M6** — career + season + save/load (JSON)

UI milestones (Windows):

- [ ] **M7** — WPF shell + DI + carset picker
- [ ] **M8** — race-weekend UI (live timing / strategy)
- [ ] **M9** — career UI (standings / calendar / offers / saves)
- [ ] **M10** — polish (assets, theme, balance)
