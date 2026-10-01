# WeatherOverhaul

WeatherOverhaul replaces The Long Dark's usual island-wide weather flow with a persistent regional weather simulation.

Each supported region receives its own evolving weather timeline, severity profile and transitions. The simulation persists across scene changes and saves and maintains a rolling 336-hour / 14-day forecast.

WeatherOverhaul can either apply that regional simulation to the region you are currently playing in, or leave vanilla weather in control while keeping the World Map weather display available for the currently loaded region.

The weather stages icons currently use a **high-quality** placeholder, this will be replaced by actual icons in the future.

---

### Disclaimer
- This mod was designed based on the weather severity of **Stalker** difficulty. Consequently, the "Normal" preset actually corresponds to **Stalker** weather severity. **There is no dynamic adjustment based on your chosen difficulty**.  

- Developer console commands related to the weather will no longer work, given that WO almost entirely overrides the vanilla system.  

- This may change in the future, but for now it remains as is, so please do not request such a feature for now.  
It’s the same with Solstice, this mod is effectively incompatible. I also plan to make WO compatible with this mod at some point, but not right now.  

- One final point: this mod requires a specific uninstallation procedure, see the end of the readme for more information.

---

[**WindMovementFix**](https://github.com/EtherSystem/WindMovementFix) is highly recommended when playing with WeatherOverhaul for the intended experience with its most extreme weather.

WindMovementFix was originally intended to be part of WeatherOverhaul, but was ultimately released as a standalone mod so its movement fix could also be used independently.

Its main benefit with WeatherOverhaul is removing the vanilla limitation on wind-based player movement slowdown. In vanilla, the maximum intended movement penalty is reached at around **65 MPH (105 km/h)** winds. WindMovementFix raises that limit to **85 MPH (137 km/h)**, allowing the movement penalty to continue scaling into the much stronger winds that can occur during WeatherOverhaul's **Violent Blizzard** stage.

It also fixes the underlying vanilla wind movement bug, where the slowdown behaves incorrectly as wind strength increases. As a result, strong winds and blizzards become meaningfully more dangerous to move through.

## Features

* Independent persistent weather simulation for every supported region.
* Rolling 336-hour / 14-day regional forecast.
* Individual regional severity settings, from **Forgiving** to **Who Wants to Play Like This?**.
* Configurable weather-stage duration presets.
* Optional generic simulation profile for unknown or modded outdoor regions.
* Custom weather stages and global night events.
* Integrated World Map weather display and regional forecast panel.
* Optional transmitter-based forecast progression.

## Regional Weather Simulation

By default, WeatherOverhaul applies the simulated forecast to the currently loaded outdoor region while every other region continues advancing independently in the background.

Changing regions immediately synchronizes the loaded scene with that region's current simulated WeatherStage, avoiding a visible transition from the previous region's weather.

The **Apply global weather to loaded region** setting controls whether WeatherOverhaul takes control of the actual weather:

**Enabled**
WeatherOverhaul applies its regional simulation to the loaded region. The World Map displays the WeatherOverhaul forecast for supported regions.

**Disabled**
Vanilla remains responsible for the actual weather. The World Map no longer presents the WeatherOverhaul simulation as the real forecast and instead displays the current vanilla WeatherStage and remaining phase time for the region that is currently loaded.

Vanilla does not maintain a future weather timeline for unloaded regions, so this mode does **not** provide a multi-stage vanilla forecast for other regions.

## Custom Weather

WeatherOverhaul expands the normal weather pool with several custom stages:

* Low Overcast
* Heavy Overcast
* Ashfall
* Whiteout
* Windy Light Snow
* Very Heavy Snow
* Very Dense Fog
* Freezing Fog
* Violent Blizzard

These stages are built from vanilla weather systems but use their own combinations of precipitation, cloud cover, fog, wind, temperature and atmosphere.

## Night & Regional Events

WeatherOverhaul can also schedule special events as part of the simulated forecast:

* Clear Aurora
* Cloudy Aurora
* Snowy Aurora
* Foggy Aurora
* Blood Moon
* Snow Blood Moon
* Glimmer Fog in supported Far Territory regions

Aurora and Blood Moon chances are configurable. Blood Moons can optionally require a full moon.

Blood Moons also alter the behavior of certain predators while active: after a fight, you will have 25% less time to recover before the predator can attack you again.  
Blood Moons also alter bleeding, basically wildlife cant bleed anymore during a blood moon.

## Custom Stages Showcase

[![WeatherOverhaul Showcase](https://img.youtube.com/vi/wa5jHLF3HCk/maxresdefault.jpg)](https://www.youtube.com/watch?v=wa5jHLF3HCk&t)

## World Map Forecast

Weather information is integrated directly into the vanilla World Map.

Supported region labels can display the current WeatherStage as:

* Name
* Icons
* Name + Icons

Region labels **can** also be made clickable to open a detailed forecast panel.

When WeatherOverhaul controls the loaded weather, the panel displays the simulated regional forecast across the available forecast horizon.

When vanilla weather control is enabled instead, the panel only displays the currently observable vanilla WeatherStage and its remaining phase time for the loaded region.

### Forecast Access

Two access modes are available while using the WeatherOverhaul regional forecast:

**Permanent**
Regional weather and the complete forecast horizon are always available.

**Transmitter Network**
Forecast access is tied to the vanilla transmitter network. Repaired transmitters provide regional coverage and increase forecast depth by 48 hours each, with all six transmitters unlocking the complete horizon.

An optional aurora-powered mode requires the transmitter network to synchronize while an aurora is active. After the aurora ends, the synchronized forecast window gradually expires until the next refresh.

These forecast-access options are hidden when **Apply global weather to loaded region** is disabled because WeatherOverhaul's simulated future forecast is no longer presented to the player.

## Unmapped / Modded Regions

Unknown outdoor regions can use one of two behaviours:

**Default Profile**  
The region receives its own WeatherOverhaul simulation using a generic climate profile.

**Vanilla Weather**  
WeatherOverhaul leaves weather and auroras in that region entirely under vanilla control.

A custom region can therefore remain playable without requiring a dedicated WeatherOverhaul profile. World Map labels and clickable forecast anchors may still require explicit support for custom maps.

## Configuration

Most systems can be configured through Mod Settings, including:

* regional severity
* stage duration
* transition duration
* custom WeatherStages
* Aurora, Blood Moon and Glimmer Fog chances
* transmitter forecast access
* World Map stage names/icons
* forecast panel appearance

Changing simulation-related settings rebuilds the regional forecast when the settings are confirmed.

## Compatibility

### General Rule

Mods that only **read or react to** weather, temperature, wind or aurora state are generally **expected** to work.

Mods that directly **force WeatherStages**, replace or repeatedly override `WeatherTransition`, independently schedule auroras, or provide their own regional climate simulation have a high chance of conflicting while **Apply global weather to loaded region** is enabled.

Wildlife mods are usually compatible with the weather simulation itself, but any mod that substantially replaces `BaseAi` behavior can overlap with WeatherOverhaul's Blood Moon predator rules.

World Map mods are generally compatible when they preserve the vanilla `Panel_Map` structure. Mods that replace map region anchors or heavily restructure the panel can require dedicated overlay support.

---

The table below is an **estimated compatibility matrix** based on the systems and game methods modified by WeatherOverhaul and by the listed mods. A green entry means no conflicting ownership has been identified, it is not a guarantee for every version or load order.

| Mod | Estimated compatibility | Why |
|---|---|---|
| **Stormfront** | 🔴 **Incompatible with WO weather control** | Stormfront has its own regional climate cycle and forces near-permanent blizzard conditions across non-stable regions during a White Front. WeatherOverhaul also owns regional weather selection, persistence and application, so both systems try to be the authoritative regional weather controller. With **Apply global weather to loaded region** disabled, they can technically coexist because WO stops applying its simulation, but WO's regional forecast is no longer the authoritative weather forecast. |
| **Aurora Monitor** | 🔴 **Incompatible with its aurora-control features** | Its aurora-control functionality competes with WO's own AuroraManager authority and planned aurora windows. Passive monitoring is a different matter, but both mods should not independently schedule or force auroras. |
| **Sky Co-op** | 🔴 **Unsupported / high desync risk** | Multiplayer synchronization is especially sensitive to mods that modify weather and animal behavior. WO heavily modifies both, including regional weather authority and Blood Moon predator behavior, so weather or wildlife state can diverge between players. |
| **Solstice** | 🟠 **Important timing conflict** | Solstice alters solar positioning, time-of-day behavior, Weather.Update and temperature/day-length logic. WO's special night events are still scheduled from fixed world-clock windows, so extreme seasonal day lengths can make a Blood Moon or aurora window occur while the visual daylight state says otherwise. |
| **Better Night Sky** | 🟠/🟡 **Important visual conflict** | Better Night Sky uses its own stars/moon presentation while WO's Blood Moon visuals modify vanilla moon, moon-light, star and glow values. The weather simulation itself should continue to work, but Blood Moon visuals may target different celestial objects from those actually rendered. So that mod is compatible with WO if you disable Blood Moons. |
| **Bleeding Revamped** | 🟠 **Conflict during Blood Moon** | Both mods touch `BaseAi.ApplyDamage(...)` and `BaseAi.UpdateWounds(float)`. WO scales animal bleed progression with Blood Moon illumination and can effectively stop new bleed progression at maximum illumination, which can contradict Bleeding Revamped's own behavior. |
| **Expanded AI Framework** | 🟠 **Partial compatibility** | EAF takes substantial control of BaseAi, including `Start`, `SetAiMode`, damage handling and its own Update path. WO's normal weather simulation is independent, but Blood Moon predator rules assume much of the vanilla BaseAi pipeline and may be incomplete on custom AI. |
| **Improved Cougar** | 🟠 **Partial compatibility** | It uses Expanded AI Framework and a custom cougar AI. WO supports the vanilla cougar's Blood Moon behavior, but the custom AI path may bypass or replace the BaseAi behavior WO expects. |
| **Dynamic Temperature** | 🟡 **Compatible, thermal models may disagree** | Both mods touch temperature calculation. WO applies weather-stage-specific temperature effects such as Freezing Fog, while Dynamic Temperature has its own global/interior model. They can stack, but indoor temperatures may not represent WO's final outdoor offset exactly. |
| **Extreme Temperature Drop** | 🟡/🟢 **Compatible, effects stack** | It changes long-term/global temperature decline while WO adds stage-specific temperature behavior. No competing weather authority was identified, so the result should mainly be cumulative cold. |
| **Seamless Interiors** | 🟢/🟡 **Very likely compatible** | Seamless Interiors identifies its cloned interiors through `Weather.IsIndoorEnvironment` and adjusts indoor temperature/windchill. WO also relies on indoor-environment detection to suppress outdoor-only custom effects, which makes the two systems naturally cooperate. |
| **Improved Clothing** | 🟡 **Compatible with stacking** | Both can affect clothing wetness, but with different responsibilities. Improved Clothing changes wetness based on clothing/gameplay state while WO applies weather-related wetness behavior. No direct weather-authority conflict is expected. |
| **AfflictionsAndBuffs** | 🟡 **Compatible, animal effects stack** | Lunar Syndrome modifies animal senses/behavior under lunar conditions while WO modifies Blood Moon fear, flee, bleed and struggle behavior. The systems target overlapping animals but mostly different fields, so a clear Blood Moon is worth regression-testing. |
| **Nonlethal Flares** | 🟡 **Compatible with gameplay interaction** | No weather conflict exists. During Blood Moons, WO can suppress predator fleeing from flare-based threats, so combining it with nonlethal flares can make those tools significantly less effective. |
| **Ambient Lights** | 🟡/🟢 **Generally compatible** | It owns many interior lighting/window effects but does not control WeatherTransition. WO defers its outdoor special-event visuals while indoors, so no direct weather-authority conflict is expected; unusual lighting near interior/exterior transitions is still worth testing. |
| **WeatherDisplay** | 🟡 **Compatible / partially redundant** | It observes the active vanilla WeatherStage and transition timing rather than controlling weather. It can coexist with WO, but custom WO stages use vanilla base stages internally, so WeatherDisplay may show `Light Fog`, `Heavy Snow`, etc. instead of the custom WO stage name. |
| **Survival HUD** | 🟢/🟡 **Compatible, passive observer** | Survival HUD only reads live weather, wind and temperature values for display; it does not control weather or auroras. It should therefore follow WO's applied conditions correctly. The main overlap is visual, since both UIs can occupy the same areas. |
| **MajorMiseries** | 🟢 **Supported integration** | It does not own WeatherTransition. Existing testing/logs show its weather- and aurora-reactive systems following WO's applied game state correctly. This is a compatibility worth preserving through regression testing. |
| **Tiny Tweaks: Wakeup Call** | 🟢 **Compatible expected** | It reacts to an aurora beginning. WO exposes planned auroras through the game's AuroraManager state, which is the appropriate interface for a passive consumer. |
| **Afraid of the Dark** | 🟢 **Compatible expected** | It reacts to darkness/night conditions but does not control weather. WO may change ambient conditions during Blood Moons, but this is normal gameplay stacking rather than competing authority. |
| **Dynamic Trees** | 🟢 **Compatible expected** | It reacts to environmental snow rather than choosing weather. WO's snow-based custom stages still use real snow-capable vanilla weather bases and particles. |
| **Randomizer: Transitions** | 🟢/🟡 **Compatible, transition testing recommended** | It changes scene destinations, but WO resolves the region that is actually loaded after transition. Teleport/transition edge cases are worth testing because WO immediately synchronizes weather on region entry. |
| **Fast Travel** | 🟢/🟡 **Compatible, transition testing recommended** | It does not own weather, but instantaneous region changes should be regression-tested to ensure WO immediately applies the destination region's current forecast without a visible stale-weather frame. |
| **The Long Development / TLDev Regions** | 🟡 **Supported through fallback, map UI will not exist** | This used to be a major coverage problem, but current WO can dynamically register unknown outdoor regions and give them the generic **Default Profile**, or leave them under vanilla weather through **Unmapped region behavior**. Dedicated World Map labels/clickable anchors are still not guaranteed for custom TLDev maps, and generic regions do not have hand-tuned climate profiles. |

## Installation

1. Install MelonLoader.
2. Install the required dependencies:
    [ModSettings](https://github.com/DigitalzombieTLD/ModSettings/) and [ModData](https://github.com/dommrogers/ModData)
3. Place `WeatherOverhaul.dll` inside your `Mods` folder.

## Uninstallation

Before removing WeatherOverhaul, use the **Prepare save for uninstall** option in ModSettings and confirm the settings. Then **save the game, quit, and only then remove `WeatherOverhaul.dll`**.

This cleanup restores WeatherOverhaul-controlled weather timing/state and rebuilds the forecast before the mod is removed. Skipping this step may leave altered weather data serialized in the save, which can result in abnormal or extremely long weather after uninstalling, including weather that appears to be stuck (thanks to Mako for spotting this).

## AI Notice

This mod was partially developed with the assistance of AI tools.

AI support was used for structural guidance, debugging assistance and documentation refinement.  

Coding, core design decisions, system architecture, balancing, and implementation logic remain fully human-driven.