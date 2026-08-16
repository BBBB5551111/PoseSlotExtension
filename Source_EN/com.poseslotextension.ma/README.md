# Pose Slot Extension - MA Stable

---

## ⚠ Unofficial add-on

**This is an UNOFFICIAL add-on for BUDDYWORKS Poses Extension. It is not developed, distributed or supported by BUDDYWORKS.**

**All bug reports, questions and feature requests about this tool must go to the distributor (GitHub Issues) — please do not contact BUDDYWORKS about it.**
Do not raise issues with this tool on BUDDYWORKS' support channels, Discord or store pages.

Support: https://github.com/BBBB5551111/PoseSlotExtension/issues

---


A fixed-scope tool that adds 50 pose save/load slots to the `[MA]` variant of
BUDDYWORKS Poses Extension. There is no auto-detection of, or switching to, the
VRCFury variant.

## Features

- `Save 01-50`: overwrite the selected slot with the current `PE/Set` and `PE/Float`
- `Load 01-50`: restore those two values back into BUDDYWORKS
- Seven folders each for Save and Load, no custom Next button, the last folder holds 43-50 (8 entries)
- Each slot's `Set`, `Pose` and `Valid` values (150 total) are saved and local only
- Loading a slot that has never been saved is a no-op
- After Load the pose is shown to other players, because it goes back through BUDDYWORKS' own synced parameters
- Jumping, landing, moving and Emotes never clear the pose; only the explicit BUDDYWORKS Reset does
- `Pose Slots` is placed at the BUDDYWORKS top level and the original `Dances` entry moves under `More`

## Requirements

- Unity 2022.3.22f1
- VRChat SDK - Avatars 3.10.4
- Modular Avatar 1.18.1
- NDMF 1.14.4
- `BUDDYWORKS Poses Extension [MA]` from BUDDYWORKS Poses Extension 7.2.1

VRCFury is not part of this edition's install path. If VRCFury is present for
other gimmicks it is never treated as BUDDYWORKS `[VRCF]`.

## Installation

1. Back up your Unity project.
2. Add the `[MA]` variant of BUDDYWORKS Poses Extension to the target avatar.
3. **If v1.x is already installed, delete the `Assets/PoseSlotExtensionMAStable` folder first.** v2.0 installs into `Packages/` instead, so keeping both would duplicate the classes.
4. Import this UnityPackage. It unpacks into `Packages/com.poseslotextension.ma` and appears in Unity's Package Manager as `Pose Slot Extension (Modular Avatar)`. Nothing is added under `Assets/`.
5. Open `Tools > Pose Slot Extension MA > Setup`.
6. Pick the target Avatar Descriptor and press `Generate, Install and Validate`.
7. Confirm `PASS`, then upload.

`Generated` is produced inside your own project and is not part of the
distributed UnityPackage.

To uninstall, delete the `Packages/com.poseslotextension.ma` folder.

## Non-destructive policy

- The BUDDYWORKS package original is never edited.
- The Action Controller and Expression Menus are copied into `Packages/com.poseslotextension.ma/Generated` and modified there.
- Your existing Expression Parameters are never modified in place; they are copied into PrivateParameters when a migration is needed.
- Only prefab overrides are written to the avatar's MA Menu Installer and MA Merge Animator.

Do not install the MA stable edition and the VRCFury stable edition on the same avatar.

## Verification

Generated and installed twice in a row on a real BUDDYWORKS `[MA]` avatar, with
post-NDMF validation of 50 Save entries, 50 Load entries, 151 PSE parameters,
overwrite behaviour, pose persistence, Reset behaviour, top-level placement and
the 8-control menu limit passing each time. The uploaded avatar was then
verified in game over OSC: all 50 slots saved, loaded, overwritten with a second
pattern and re-loaded, plus pose persistence across a jump.

Please read `TERMS.md` and `THIRD_PARTY_NOTICES.md` before use.
