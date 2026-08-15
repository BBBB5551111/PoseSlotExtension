# Changelog

## 1.0.4-vrcfury-stable - 2026-08-15

- Critical fix: edits to VRCFury components (which serialize through SerializeReference) were silently dropped when the scene was saved if the component lived on a prefab instance, so the uploaded avatar ended up without the `Pose Slots` menu and without `PSE/Command`
- Restored a guarded unpack limited to the BUDDYWORKS `[VRCF]` prefab instance. Unrelated VRCFury products are never unpacked, and if the component is nested inside the avatar's own prefab the tool stops with an explicit error instead of failing silently
- Added a save -> reload -> re-validate gate to ONE CLICK, so an installation that only exists in memory now fails validation
- Improvements from 1.0.3 are kept: property-override handling for Modular Avatar components, `[VRCF]` prefab dependency check, no more overwriting of the bundled README on every generate, recursive 8-control menu limit checks

## 1.0.3-vrcfury-stable - 2026-08-15

- Ported the improvements that were already verified in the MA stable line
- Replaced the destructive full unpack of the BUDDYWORKS prefab with property overrides
- Persisted VRCFury reference changes on prefab instances with `RecordPrefabInstancePropertyModifications`
- Fixed the generate-time dependency check to look for the `[VRCF]` prefab instead of the `[MA]` prefab
- Stopped regenerating and overwriting the bundled README on every generate
- Extended the post-NDMF 8-control menu limit check to walk the whole integration menu graph
- No change to the core behaviour (50 slots, `PE/Set` and `PE/Float` only, pose persistence)

## 1.0.2-vrcfury-stable - 2026-08-15

- Auto-install the bundled BUDDYWORKS Poses Extension `[VRCF]` prefab when the target avatar does not have it
- Allow setup to start on avatars that only have the `[MA]` variant present
- Remove missing legacy Parameters and menu references and repair them to the current stable assets
- Verified end to end on a real avatar: generate, install, NDMF validation, official VRChat SDK build and a real upload to an existing blueprint

## 1.0.0-vrcfury-stable - 2026-08-15

- Split into an independent package based on the pre-MA v1.0.2 VRCFury line
- Fixed VRCFury-only path with no MA detection, MA menu replacement or integration-choice UI
- Moves `Pose Slots` to the top level and `Dances` under `More` inside duplicated menus only, never editing the BUDDYWORKS original
- Independent asset root, namespace and NDMF plugin id
- Two consecutive generate/install/validate runs on the same avatar both passed

## 1.0.2 - 2026-08-14

- Documented in the setup window and README that the target avatar needs the `[VRCF]` variant
- Show a specific "MA variant only" status when just the `[MA]` variant is present
- Replaced the generic install-failure message with one that names the failing step
- Regression tested on Unity 2022.3.22f1 starting from a fresh `[VRCF]` prefab

## 1.0.1 - 2026-08-14

- Fixed CS0246 in environments without Lyuma's Av3 Emulator by compiling the internal runtime test only when `PSE_LYUMA_RUNTIME_TEST` is defined

## 1.0.0 - 2026-08-13

- 50 overwrite save/load slots
- Fixed contract v1 storing only `PE/Set` and `PE/Float`
- Seven folders each for Save and Load, no custom Next button
- Stored values are local only; Load targets the BUDDYWORKS synced parameters
- Poses are no longer cleared by jumping, landing, moving or Emotes
- The explicit BUDDYWORKS Reset still clears the pose
