# Changelog

## 1.0.0-ma-stable - 2026-08-15

- Split into an independent package targeting the BUDDYWORKS Poses Extension `[MA]` variant only
- No VRCFury detection, no integration-choice UI: the Modular Avatar path is fixed
- Moves `Pose Slots` to the top level and `Dances` under `More` inside duplicated menus only, never editing the BUDDYWORKS original
- Recovers the MA identity of the BUDDYWORKS menu installer even when a previous generated menu reference has gone missing
- Reconnects an already-installed Merge Animator whose controller reference was invalidated by regeneration
- Independent asset root, namespace and NDMF plugin id
- Verified in game over OSC: all 50 slots saved, loaded, overwritten with a second pattern and re-loaded, plus pose persistence across a jump
