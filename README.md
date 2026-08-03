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
- [x] **M3** — seedable RNG + lap-time core
- [x] **M4** — qualifying + practice
- [x] **M5** — full race simulator
- [x] **M6** — career + season + save/load (JSON)

The engine is complete and runs headlessly: load a carset → build the entry list
→ simulate a full season round-by-round (qualifying + race) → championship
standings → season rollover (ageing, retirements, rookie promotions, seat offers)
→ save/load as JSON. All of it is covered by deterministic unit tests (38 passing
on Linux).

UI milestones (Windows):

- [x] **M7** — WPF shell (custom branded frame) + DI + main menu + new-career + load
- [x] **M8** — race-weekend UI: practice → qualifying → **live-timing race playback** → results
- [x] **M9** — career UI: dashboard, standings, calendar, seat offers, season rollover, save
- [x] **M10** — dark theme + brand kit (logo, colours, fonts), app icon

## The app (Windows)

`LightsToFlag.App` is a WPF desktop game with a from-scratch **dark theme** built on
the Lights to Flag 2 brand kit (Track Black / Lights Out Red palette; Saira Condensed,
Chakra Petch and Archivo fonts bundled under `Assets/Fonts`). It has a custom branded
window frame, a main menu, a new-career flow (pick carset + driver), a career hub
(dashboard, championship standings, calendar, seat offers, season rollover) and a race
weekend with **live-timing playback** (practice → qualifying → an animated lap-by-lap
race you can speed up or skip → results).

Build & run on Windows:

```powershell
dotnet build LightsToFlag.sln -c Release
dotnet run --project src/LightsToFlag.App
```

CI: `.github/workflows/ci.yml` builds/tests the engine on Linux; `windows.yml` builds
the full solution (incl. the WPF app) on `windows-latest`. Player saves live under
`%AppData%/LightsToFlag/Saves`.

Bundled fonts are licensed under the SIL Open Font License 1.1 (see
`src/LightsToFlag.App/Assets/Fonts/OFL.txt`).
