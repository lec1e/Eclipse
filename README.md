<h1 align="center">Eclipse</h1>

<p align="center">
  <img src="./.resources/Eclipse.png" height="160" alt="Eclipse logo"/>
</p>

<p align="center">
  <strong>Eclipse</strong> is a Froststrap-based Roblox launcher with MrExLive / ExploitStrap features ported in —
  Versions Manager profiles, LIVE channel lock, BanAsync tools, multi-instance, and a black/purple multi-theme system.
</p>

## What’s new vs stock Froststrap

- **Versions Manager** — one profile per executor (WEAO / manual hash), dropdown to switch, optional picker on launch
- **LIVE channel lock** — Player launches forced to production (toggleable)
- **Eclipse themes** — default black/purple plus Purple Haze, Ocean, Emerald, Sunset, Mono, Blood Red
- **BanAsync** (Windows) — clean Roblox traces, MAC / MachineGuid helpers
- **Multi-instance + window tiling**
- **VIP / Server Browser / News** tabs
- **Privacy mode** — truncate `RobloxCookies.dat` before launch
- **Stream mode** — hide account-identifying UI while streaming

C# namespaces remain `Froststrap.*` for upstream compatibility; the product name, install folder, and branding are **Eclipse**.

## Build

```bash
# dependencies (if not already present)
git clone --depth 1 https://github.com/Froststrap/FluentAvalonia.git FluentAvalonia
git clone --depth 1 https://github.com/Froststrap/LucideAvalonia.git LucideAvalonia
git clone --depth 1 https://github.com/Froststrap/ColorPicker.git ColorPicker

dotnet build Froststrap/Froststrap.csproj -c Release
```

## License

Same multi-license model as Froststrap (AGPL-3.0 for Eclipse modifications; MIT for upstream Bloxstrap/Fishstrap code).
MrExLive / ExploitStrap ports inherit MIT from that fork.
