# Mods (not shipped)

User-authored carsets. **Nothing in this folder is distributed with the game** — release
and packaging steps exclude `content/mods/**` (ADR-0007). It exists so real-name or
licensed-style content can be created and played locally without baking any of it into
the product.

Drop a carset folder here (same format as `content/carsets/`) and load it from the game
or validate it with `ltf validate content/mods/<name>`.
