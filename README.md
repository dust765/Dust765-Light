<div align="center">
  <img src="/logodust.png" alt="Dust765 Light Logo" width="200"/>

  [![.NET](https://img.shields.io/badge/.NET-10-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
  [![Cross-Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-orange.svg)](https://github.com/andreakarasho/ClassicUO)
  [![Discord](https://img.shields.io/badge/Discord-Join%20us-7289da.svg)](https://discord.gg/9Vh7aqqX)
  [![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)

</div>

## 🎭 **The Dust765 Project**

> **"This project was created to address a problem constructed within the toxicity of this community. This is to show the community that open source projects are not meant for cliques and high school drama but rather the expansion of something greater: innovation."**
>
> *- A penny for your thoughts, the adder that prays beneath the rose.*

### 🌑 **The Dark Truth**

Welcome to the **Dust765** project - where we don't just break barriers, we **obliterate them**. While others play nice in their little sandboxes, we're here to remind everyone that **true innovation doesn't come from playing favorites**.

[**Discord**](https://discord.gg/9Vh7aqqX) – We're on our own Discord server, join and have fun!

Dust765: 7 Link, 6 Gaechti, 5 Syrupz and jsebold666 (astraroth)

[**Website**](https://dust765.com/)

## Dust765 Light Version

A lightweight fork of [Dust765](https://github.com/dust765/ClassicUO) focused exclusively on **PvP gameplay**. This version strips down the full Dust765 feature set to only what matters in combat, keeping the client lean and easy to maintain.

> Built on top of [ClassicUO](https://github.com/andreakarasho/ClassicUO) — the open source Ultima Online client.

---

## Features

All options are available under **Options → Dust765** in-game.

### Movement
- **Avoid obstacles automatically** while walking

### UO Classic Combat — Swing Timer
- Visual **swing timer bar** showing weapon cooldown progress
- Toggle the bar on/off and lock its position on screen

### HP / Mana / Stamina Bars
- **HP/Mana/Stamina bars** displayed below your own character nameplate (no Ctrl+Shift needed)
- Adjustable **nameplate bar opacity**
- **Old-style underfoot HP lines** for all mobiles
- **HP / Mana / Stamina underlines** for self and party members
- Option for **larger underlines**
- Adjustable **underline transparency** (1–10 scale)

### Bandage Timer
- On-screen **bandage timer gump**
- Option to count **up** instead of down

### Casting Timer (OnCasting)
- On-screen **casting timer gump**
- Option to hide the gump (internal tracking only)
- Position the timer **below the player status bar**

### Houses & World Map
- **Transparent houses** — walls above a configurable Z level fade out (transparency 1–9)
- **Invisible houses** — walls above a configurable Z level disappear entirely; mobiles and players inside remain visible
- Configurable **ground clearance** to prevent floors from being affected
- **Show death location** on the world map for 5 minutes after dying

### Grid Container
- Replace the standard container gump with a **resizable item grid**
- Drag the corner to resize; border and resize handle always visible

---

## Building

```bash
git clone https://github.com/dust765/Dust765-Light.git
cd Dust765-Light
git submodule update --init --recursive
dotnet build src/ClassicUO.Client/ClassicUO.Client.csproj -c Release
```

---

## Credits

- **andreakarasho** — original ClassicUO creator
- **Dust765 team** — Gaechti, Syrupz, Link, Astraroth

---

## License

MIT — see [LICENSE.md](LICENSE.md)
