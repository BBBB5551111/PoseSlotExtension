# Changelog

## 2.3.0-ma-stable - 2026-08-16

- Added a saved local `Valid` flag per slot; loading an empty slot is now a no-op.
- Save stores `PE/Set`, `PE/Float`, and marks the slot valid.
- Removed the 0.35 second dwell and routed commands through Any State transitions so consecutive commands are not dropped.
- An explicit Avatar Reset returns all slots to the empty state, following VRChat parameter-default behavior.

## 2.2.0-ma-stable - 2026-08-16

- **Added a minimum dwell (0.35 s) after a Load.** Loading repeatedly in quick succession rewrites `PE/Set` faster than BUDDYWORKS can finish its own pose transition, leaving the avatar in a state that only an explicit Reset clears
  - Implementation: the Load state's return transition now has an Exit Time; a new Load is not accepted during the dwell
  - **No press is dropped.** The expression menu holds the command value while the button is held, so a command arriving during the dwell is applied when it ends. Only a press shorter than the dwell can be missed
  - Save is unchanged - it does not write `PE/Set`
- Calibrate the real dwell in game (`Evidence/pse_osc_settle.py`)

## 2.1.1-ma-stable - 2026-08-16

- **Follow-up to v2.1.0.** The return transitions were changed to `NotEqual`, but `PoseSlotBuildValidator` still required them to be `Command == 0`
  - Effect: running Setup on v2.1.0 reported `Save/Load return transitions: False` and **NDMF validation FAILED**
  - Fix: the check now requires "anything other than this slot's own command", which also makes it a regression guard against the old structure

## 2.1.0-ma-stable - 2026-08-16

- **Critical fix: a `PSE/Command` value arriving before the previous one returned to `0` was silently dropped**
  - Cause: the only way out of a Save/Load state was `Command == 0`, so a command that arrived first left the state machine stuck in the previous state
  - Symptom: VRChat's expression menu emits a button's value as the cursor passes over it, so brushing a neighbouring button made the intended press do nothing - or wrote the neighbouring slot instead
  - Fix: the return condition is now "anything other than this slot's own command" (NotEqual), so the state machine leaves as soon as the next command arrives
- This defect has been present **since v1.0.2**; it is unrelated to the move to `Packages/`
- The fixed contract (`PoseSlotFixedSpecification`) is unchanged

## 2.0.0-ma-stable - 2026-08-16

- **Install location moved from `Assets/` to `Packages/`**, following the BUDDYWORKS author's request to have the add-on live under `/Packages`
- Added an Editor-only Assembly Definition (code under `Packages/` is not compiled without one)
- Generated assets now go to `Packages/com.poseslotextension.ma/Generated`
- The fixed contract (`PoseSlotFixedSpecification`) is unchanged and identical to v1.0.4
- Note: Unity's Export Package still follows dependencies into `Packages/` and writes the real data. Living under `Packages/` does not prevent BUDDYWORKS assets from being exported

## 1.0.0-ma-stable - 2026-08-15

- Split into an independent package targeting the BUDDYWORKS Poses Extension `[MA]` variant only
- No VRCFury detection, no integration-choice UI: the Modular Avatar path is fixed
- Moves `Pose Slots` to the top level and `Dances` under `More` inside duplicated menus only, never editing the BUDDYWORKS original
- Recovers the MA identity of the BUDDYWORKS menu installer even when a previous generated menu reference has gone missing
- Reconnects an already-installed Merge Animator whose controller reference was invalidated by regeneration
- Independent asset root, namespace and NDMF plugin id
- Verified in game over OSC: all 50 slots saved, loaded, overwritten with a second pattern and re-loaded, plus pose persistence across a jump
