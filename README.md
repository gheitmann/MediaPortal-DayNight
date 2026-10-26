# MediaPortal Day/Night Backgrounds

A lightweight process plugin for **MediaPortal 1** that selects morning, noon,
day and night backgrounds in cooperating skins. It works independently of the
Backdrop menu editor. First public preview: **v0.2.0-alpha.1**.

## Features

- Automatic local-time switching, with separate morning/noon/day/night starts.
- Six modes: Automatic, Morning, Noon, Day / afternoon, Night and Off (original).
- Default schedule: 06:00, 12:00, 15:00 and 19:00; custom overnight schedules.
- Configuration inside MediaPortal when the skin exposes the optional page.
- A standalone configuration dialog in MediaPortal Configuration.
- 37 translated UI catalogues, English fallback and minute-precision times.
- One minute-aligned timer, GUI-thread property updates only when values change.
- Safe fallback for unsupported skins or missing assets, without replacing files.
- One AnyCPU DLL for x86 and x64 clients; no MediaPortal binaries bundled.

![Morning TV background](https://raw.githubusercontent.com/gheitmann/Backdrop/feat/lucide-refresh/skin/Backdrop/Media/Modern/Morning/tv.png)

The artwork above belongs to Backdrop. This plugin download contains no skin
artwork and does not automatically convert arbitrary skins.

## Installation

1. Download `MediaPortal-DayNight-v0.2.0-alpha.1.zip` from Releases and extract it.
2. Close MediaPortal and MediaPortal Configuration. Back up an existing plugin DLL.
3. Copy `DayNight.Backgrounds.dll` to `plugins/process/` inside the **actual**
   MediaPortal installation directory. Do not assume x86 or x64 Program Files.
4. Open MediaPortal Configuration > Plugins > Process plugins and enable
   **Day/Night Backgrounds**, then open Config and save your preferences.
5. Use a cooperating skin, for example [Backdrop ALPHA27](https://github.com/gheitmann/Backdrop/releases).
   In Backdrop: Settings > GUI > Skin > Background lighting.

Backdrop ALPHA27 already includes the same plugin; a second installation is not
needed. Do not install duplicate DLL copies under different names.

Preferences are stored separately as `DayNight.xml` in MediaPortal's configuration
directory (normally `%ProgramData%/Team MediaPortal/MediaPortal`). In-skin changes
apply on Save; Cancel discards staged changes. External edits are read on the
next minute check or window activation. Off selects the skin's original image.
To uninstall, close MediaPortal and remove only `plugins/process/DayNight.Backgrounds.dll`.
The separate preference file may be retained for reinstallation.

## Skin authors

See [the skin contract](docs/SKIN-INTEGRATION.md) for properties, asset capability
declaration, expressions and the optional native configuration entry.
The plugin cannot invent missing day images or retrofit unmodified skins.
Original backgrounds remain visible when the plugin is absent or disabled.
The native startup splash is not switched by this plugin.

## Build and validation

Requires Windows, the .NET Framework C# compiler and a local MediaPortal client.
Run `./build.ps1 -MediaPortalDirectory 'C:/path/to/MediaPortal'`.
The output is `dist/DayNight.Backgrounds.dll`. References are read locally and
never redistributed. Run `python tools/validate.py` and `./test.ps1`.

Validated against MediaPortal 1.39 native assemblies on x86 and x64. Full-client
interaction was exercised on x64 with Backdrop. This is an **alpha preview**;
full x86 interaction, suspend/resume and prolonged playback remain unverified.
No blanket compatibility claim is made for other MediaPortal versions or skins.
Translation completeness has been checked; every language has not had a native
speaker review. See [verification](docs/VALIDATION.md) and [changelog](CHANGELOG.md).

## Development

This repository is the canonical source for the independent plugin. Backdrop
keeps a pinned source/binary snapshot so its complete installer remains
reproducible. Report plugin issues here; report Backdrop artwork/layout issues
in the Backdrop repository. Include client version, architecture, skin, selected
mode, schedule and relevant log excerpts, after removing personal data.

Plugin source is MIT licensed. MediaPortal and third-party dependencies retain
their own licenses and are not included.
