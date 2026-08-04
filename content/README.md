# Content

Carsets — the data-driven championships the game plays. Each carset is a folder with a
`carset.json` (schema in `docs/content-schema.md`, validated by `ltf validate`).

```
content/
├─ carsets/          Shipped with the game
│  └─ global-prix/   Fictional sample — "Global Prix Series"
└─ mods/             User content, NOT shipped (see ADR-0007)
   └─ real-2025/     Real 2025 season (real names)
```

## `carsets/` — shipped

**global-prix** — a fully fictional championship (10 teams, 20 drivers) whose names come
from the UI mockup. Its calendar, circuit specs and performance spread are **calibrated
from the real 2024/2025 season** (24 rounds, real circuit characteristics, 2025 points),
but no real team or driver names appear. This is what a fresh install plays (ADR-0003).

## `mods/` — not shipped

Community content lives here and is **excluded from every release/package** (ADR-0007).
**real-2025** carries real 2025 team and driver names for offline personal use; it is not
distributed with the game. See `mods/real-2025/README.md`.
