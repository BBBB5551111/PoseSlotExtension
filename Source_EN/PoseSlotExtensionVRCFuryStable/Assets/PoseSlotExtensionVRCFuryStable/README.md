# Pose Slot Extension - VRCFury Stable

---

## ⚠ Unofficial add-on

**This is an UNOFFICIAL add-on for BUDDYWORKS Poses Extension. It is not developed, distributed or supported by BUDDYWORKS.**

**All bug reports, questions and feature requests about this tool must go to the distributor (GitHub Issues) — please do not contact BUDDYWORKS about it.**
Do not raise issues with this tool on BUDDYWORKS' support channels, Discord or store pages.

Support: https://github.com/BBBB5551111/PoseSlotExtension/issues

---


A fixed-scope tool that adds 50 pose save/load slots to the `[VRCF]` variant of
BUDDYWORKS Poses Extension. It has no MA detection, no MA menu replacement and
no "pick your integration" UI.

## Features

- `Save 01-50`: overwrite the selected slot with the current `PE/Set` and `PE/Float`
- `Load 01-50`: restore those two values back into BUDDYWORKS
- Seven folders each for Save and Load, no custom Next button, the last folder holds 43-50 (8 entries)
- The 100 stored values are saved and local only
- After Load the pose is shown to other players, because it goes back through BUDDYWORKS' own synced parameters
- Jumping, landing, moving and Emotes never clear the pose; only the explicit BUDDYWORKS Reset does
- `Pose Slots` is placed at the BUDDYWORKS top level and the original `Dances` entry moves under `More`

The only change this tool makes to the BUDDYWORKS menu is swapping the top-level
`Dances` and `Pose Slots` entries inside duplicated copies. The BUDDYWORKS
package original is never modified.

## Requirements

- Unity 2022.3.22f1
- VRChat SDK - Avatars 3.10.4
- Modular Avatar 1.18.1
- VRCFury 1.1417.0
- BUDDYWORKS Poses Extension 7.2.1 (the `[VRCF]` prefab is installed automatically during setup)

## Installation

1. Back up your Unity project.
2. Make sure BUDDYWORKS Poses Extension 7.2.1 is installed through VCC. The tool places the `[VRCF]` prefab on the target avatar for you.
3. Import this UnityPackage.
4. Open `Tools > Pose Slot Extension VRCFury > Setup`.
5. Pick the target Avatar Descriptor and press `Generate, Install and Validate`.
6. Confirm `PASS`, then upload.

`Generated` is produced inside your own project and is not part of the
distributed UnityPackage.

## Non-destructive policy

- The BUDDYWORKS package original is never edited.
- The Action Controller and Expression Menus are copied into `Assets/PoseSlotExtensionVRCFuryStable/Generated` and modified there.
- Your existing Expression Parameters are never modified in place; they are copied into PrivateParameters when a migration is needed.
- Only the required references and parameter entries are written to the avatar's VRCFury Full Controller.
- If more than one Locomotion (Base) is present the tool stops instead of deleting anything.

Do not install the MA stable edition and the VRCFury stable edition on the same avatar.

## Verification

Generated and installed on a real project avatar, then validated after
VRCFury + NDMF for 50 Save entries, 50 Load entries, 101 PSE parameters,
overwrite behaviour, pose persistence, Reset behaviour, top-level placement and
the 8-control menu limit. A VRChat SDK build and a real upload to an existing
blueprint were also completed, and the uploaded avatar was verified in game over
OSC (full 50-slot save/load/overwrite sweep and jump persistence).

Please read `TERMS.md` and `THIRD_PARTY_NOTICES.md` before use.
