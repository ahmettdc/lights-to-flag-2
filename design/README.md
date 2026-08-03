# Design reference

The brand kit and UI mockups the project owner authored in **Claude Design**. Per
[ADR-0002](../docs/adr/0002-dotnet9-avalonia.md) these are the **visual source of truth**:
the HTML/CSS is a reference, not shipped — the interface is re-implemented in Avalonia
during **Phase 3 (M19–M25)**.

## Contents

| Path | What |
|------|------|
| `brand/` | Logos (SVG + PNG), app icon, brand `README.txt` (usage rules) |
| `mockups/ui.dc.html` | Full UI mockup — menus, career hub, driver/team screens, race weekend |
| `mockups/logo.dc.html` | Logo construction mockup |
| `mockups/support.js` | Claude Design runtime for the exports (reference only) |

## Design tokens

Locked by the brand kit (`brand/README.txt`). These become the Avalonia theme in M19.

**Palette**

| Token | Hex | Use |
|-------|-----|-----|
| Track Black | `#08090B` | app background |
| Garage | `#14171C` | cards / panels |
| Lights Out Red | `#FF3B2F` | accent, the "2", CTAs |
| Flag White | `#F2F4F6` | typography |

**Type** (all Google Fonts, OFL, free for commercial use)

- **Saira Condensed 900** — wordmark, headings, driver names, timing tower
- **Chakra Petch 700** — numbers, lap times, telemetry, the "2"
- **Archivo 500** — body text, tooltips, contract copy

**Logo rules** (excerpt): clear space = one light diameter; first 3 lights lit, last 2
out (the lights-out moment); never restyle the red or stretch the wordmark.

## What the mockup tells the engine

The mockup fixes vocabulary the domain model (M1) and later phases follow:

- **Driver attributes:** Pace, Racecraft, Consistency, Tyre Management, Wet Weather,
  Feedback — plus dynamic Morale and a derived Rating.
- **Staff roles:** Technical Director, Chief Aerodynamicist, Chief Strategist.
- **Management:** Budget, Salary, Contract (with interest / at-risk states),
  Correlation (wind-tunnel ↔ track).
- **Fictional content** to reuse in the M2 sample carset — teams: Talon Racing,
  Kuro Dynamics, Kestrel Racing, Sable GP, Nordwind, Aurelia Corse, Marchetti Corse;
  drivers: Mateo Ferreira, Idris Whitlock, Freya Nilsen, H. Bergström, P. Okonkwo.
- **Tagline:** *Motorsport Manager*.
