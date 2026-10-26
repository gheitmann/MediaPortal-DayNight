# Skin integration contract 1

Place the capability marker at `Media/DayNight/skin-support.xml`. Use the example
marker and list every image used by the integration as an `<asset>` path relative
to Media. Assets must remain inside Media; every declared asset must exist.
Set `periods="morning,noon,day,night"` for four periods. A contract-1 marker
without periods keeps two-period compatibility: morning/noon resolve to day.
An invalid/missing marker or asset yields night/original fallback.

## Published properties

| Property | Values |
| --- | --- |
| `#DayNight.Period` | `morning`, `noon`, `day`, `night` |
| `#DayNight.Mode` | `automatic`, `morning`, `noon`, `day`, `night`, `disabled` |
| `#DayNight.Supported` | `true` or `false` |
| `#DayNight.Available` | Whether the native settings window can be opened |
| `#DayNight.Label.Title` | Title localized to the MediaPortal language |

Use the original texture as the final branch, so an empty property also falls
back safely. Example (paths are relative to the skin Media directory):

```xml
<texture>#(iif(string.equals(#DayNight.Period,'morning'),'Backgrounds/morning.jpg',iif(string.equals(#DayNight.Period,'noon'),'Backgrounds/noon.jpg',iif(string.equals(#DayNight.Period,'day'),'Backgrounds/day.jpg','Backgrounds/night.jpg'))))</texture>
```

The startup splash is outside this runtime expression path. Custom fanart need
not be replaced: apply this mapping only to backgrounds you explicitly support.

## Optional native settings entry

Adapt `examples/DayNight.xml` to your skin fonts/layout and place it in the skin
root. Its window ID **87950** must be free. Add a native button with a free
control ID, label `#DayNight.Label.Title`, hyperlink `87950`, and visibility
`string.equals(#DayNight.Available,true)`. Preserve your own navigation edges.
The plugin registers that window only when the XML exists and never replaces
another plugin window. Native MediaPortal menu dialogs supply mode and time
selection. Config remains available externally when no in-skin page exists.

Preferences use version 2 with `mode`, `morningStarts`, `noonStarts`, `dayStarts`
and `nightStarts` attributes. Use HH:mm values, distinct starts and the cyclic
order morning → noon → day → night. Windows local time drives the schedule;
there are no network, location, sunrise or sunset dependencies.
