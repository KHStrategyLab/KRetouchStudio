# SCREEN_IO_AND_WORK_MODE_ARCHITECTURE.md

Last updated: 2026-06-17

## Purpose

This document fixes the current screen input/output architecture before more tool code is added.

The goal is to prevent UI state fragmentation, preview-routing confusion, and tool-state coupling inside `MainWindow`.

This document defines:

- screen-level runtime modes
- panel visibility / expand behavior
- layer and history ownership
- preview composition rules
- tool file separation policy
- first implementation order

---

## 1. Core Direction

KRetouchPro is not a direct-paint preview shell.

The preview must always be treated as a composed result, not as the next editing source.

Main rule:

```text
BaseImage
-> Layer Stack
-> Compose
-> Preview
```

The preview result must not become the new source image for later edits.

All tool actions and all right-panel adjustments must be represented as layer-backed operations or layer-backed object states.

---

## 2. Screen Runtime Modes

The screen runs in three runtime modes.

### 2.1 Viewer Mode

Condition:

- selected photo count = `0`

Meaning:

- pure viewer state
- no retouch-authoring operation is active

Allowed:

- photo list browsing
- zoom
- pan
- tethered capture auto-import and auto-focus
- split preview navigation
- work-area refresh
- passive information viewing

Blocked:

- right-panel adjustment editing
- tool creation
- layer editing
- history commit creation

UI rule:

- adjustment panel auto-collapses
- tool actions are disabled
- the app behaves as a clean viewer

### 2.2 Edit Mode

Condition:

- selected photo count = `1`
- one right retouch tab is expanded

Meaning:

- full editing mode

Allowed:

- right-panel editing
- tool creation and editing
- layer editing
- history creation

UI rule:

- adjustment panel opens only in single-photo state
- tool interactions are enabled
- layer and history panels are active

### 2.3 Multi Mode

Condition:

- selected photo count >= `2`

Meaning:

- compare-first preview state
- user-facing name: `Multi Mode`

Allowed:

- split preview
- zoom
- pan
- comparative viewing

Blocked or limited:

- single-photo retouch tools
- single-photo adjustment commits
- save

UI rule:

- comparison stays active
- single-photo editing panels auto-collapse or remain disabled

---

## 3. Mode Transition Rule

Runtime mode is resolved from selection count and right retouch tab state:

- `0 selected` -> `Viewer Mode`
- `1 selected` + no expanded right retouch tab -> `Viewer Mode`
- `1 selected` + expanded right retouch tab -> `Edit Mode`
- `2+ selected` -> `Multi Mode`

Safety rules:

- `Edit Mode` requires exactly one selected photo.
- `Multi Mode` cannot enter `Edit Mode`.
- `Multi Mode` auto-collapses or disables right retouch tabs.
- `Multi Mode` cannot save.
- Work-area/tether imports may auto-focus only in `Viewer Mode`.
- Work-area/tether imports must not steal focus in `Edit Mode` or `Multi Mode`.
- Edit history is keyed by normalized file path.
- Restarted sessions may restore history only for the same normalized file path.
- Persisted edit history is stored under local AppData, not in the repository.
- Work-area refresh prunes persisted history whose source path is no longer present in the current work folder.

---

## 4. Unified UI Section State

Every user-facing section must use the same state model.

This applies to:

- right adjustment panel
- adjustment tabs inside that panel
- Layers panel
- History panel
- future Info / Debug / Calculator panels

### 4.1 Required section state

Each section owns:

- `Visible`
- `UserExpandedPreference`
- `ActualExpanded`
- `Enabled`

### 4.2 Meaning

- `Visible`
  - user-level display preference
- `UserExpandedPreference`
  - whether the user wants the section open when allowed
- `ActualExpanded`
  - final expanded state after current mode rules are applied
- `Enabled`
  - whether the section can currently receive edits

### 4.3 Evaluation rule

```text
ActualVisible = Visible
ActualExpanded = Visible AND UserExpandedPreference AND ModeAllowsExpand
Enabled = ModeAllowsEdit
```

### 4.4 Important rule

`0 selected` must auto-collapse, not hard-hide, the adjustment side.

This avoids layout disappearance confusion and preserves user preference.

---

## 5. Right Adjustment Panel Policy

The right adjustment area is not a special case.

It uses the same section-state rules as all other major panels.

### 5.1 Panel-level policy

- `Viewer Mode` -> auto-collapse + disable
- `Edit Mode` -> keep only one right retouch tab expanded
- `Multi Mode` -> collapse or disable single-photo commit behavior

### 5.2 Tab-level policy

Each major adjustment tab also uses section-state logic.

Examples:

- `Tone`
- `Face`
- `Background`
- `Hair`
- later tabs

Each tab must support:

- `Visible`
- `UserExpandedPreference`
- `ActualExpanded`
- `Enabled`

This prevents one tab from using a different collapse model than the rest of the UI.

---

## 6. Layers And History

Layers and History must not be treated as the same thing.

### 6.1 Layers

Layers represent the current surviving result structure.

They are not time-order logs.

Examples:

- `Base Image`
- `Tone Adjustment 1`
- `Type Object 3`
- `Path Object 2`

### 6.2 History

History represents action order.

Examples:

- create type
- move type
- change tone exposure
- toggle visibility

### 6.3 Required UI direction

The screen should later support both:

- `Layers`
- `History`

Recommended first UI form:

- right-bottom area
- tabbed or stacked
- `Layers` and `History` may each be shown or hidden
- each may also be collapsed or expanded

---

## 7. Layer Types

The current design should support at least these layer categories.

### 7.1 Base Layer

- the original image
- always present

### 7.2 Adjustment Layer

- right-panel adjustments
- tone, curves, exposure, color, skin, background operations

### 7.3 Object Layer

- text
- rectangle
- path
- future vector-style screen objects

### 7.4 Pixel Edit Layer

- brush
- stamp
- healing
- liquify

### 7.5 Mask Attachment

- apply region
- protect region
- block region

Masks are attached to a layer or operation, not treated as unrelated floating state.

---

## 8. Layer Editing Rule

The user must be able to:

- use one tool
- switch to another tool
- come back later
- select the previous result
- continue editing it

Therefore:

- layer-backed operations must remain selectable
- previous results must not be flattened into an anonymous preview state

### 8.1 Recommended editing rule

If an existing compatible layer is selected:

- edit that layer

If no compatible layer is selected:

- create a new layer

This rule is the safest practical equivalent of Photoshop behavior for this project stage.

---

## 9. History Commit Rule

The history system must commit meaningful operations, not noise.

### 9.1 Commit timing

- brush family: one stroke = one commit
- liquify: one stroke = one commit
- text: create, move, edit confirm = separate commits
- rectangle/path: create confirm, later edit confirm = separate commits
- sliders: drag movement is temporary, commit when released
- button adjustments: one click = one commit

### 9.2 History data requirement

Each history record should carry at least:

- `Id`
- `Timestamp`
- `ActionType`
- `TargetLayerId`
- `Before`
- `After`
- `IsUndoable`
- `SummaryText`

This is required for future undo, replay, and recomposition.

---

## 10. Screen Input Routing

All screen input must be routed by mode and by active tool.

### 10.0 Toolbox And Retouch Panel Boundary

The toolbox owns target definition.

It decides:

- work area
- selection geometry
- mask source
- brush stroke
- path geometry
- sample point
- crop rectangle

The right retouch panel owns parameter dispatch.

It decides:

- amount
- strength
- radius
- opacity
- color
- blend mode
- preset
- engine operation values

The right panel should not silently invent hidden target areas.

If a slider needs a target, the target must come from:

- current tool input
- current user selection
- an explicit detector result
- a documented full-image default

The detailed contract is defined in:

- `TOOLBOX_RETOUCH_PANEL_CONTRACT.md`

### 10.1 Input ownership order

```text
App Mode
-> Active Section
-> Active Tool
-> Tool Target Input
-> Retouch Panel Parameters
-> Selected Layer or New Layer Policy
-> Preview Interaction
-> History Commit
```

### 10.2 Input rule

`MainWindow` should route input.

Tool-specific logic should not permanently live inside `MainWindow`.

`MainWindow` should own:

- mode resolution
- active-photo state
- panel state routing
- preview-surface event routing
- final compose requests

It should not own detailed tool behavior for every tool forever.

---

## 11. Screen Output Routing

The screen must expose three main output classes.

### 11.1 Preview Output

- composed visible result for the current mode

### 11.2 UI State Output

- panel visibility
- expand/collapse state
- enable/disable state
- mode status text

### 11.3 Operation Output

- layer creation
- layer update
- layer selection
- history record append

---

## 12. Tool File Separation Policy

Each tool must be implemented in its own file.

This is now a design rule, not a later cleanup wish.

### 12.1 Reason

If tools stay inside one growing `MainWindow.xaml.cs`, the project will lose:

- ownership clarity
- testability
- reusable state boundaries
- safe future layer integration

### 12.2 Required policy

Each tool should own its own file and its own operational state block.

Examples:

- `TypeToolController.cs`
- `PathToolController.cs`
- `RectangleToolController.cs`
- `BrushToolController.cs`
- `LiquifyToolController.cs`

The exact naming may change, but the file-separation rule stays.

### 12.3 MainWindow responsibility after separation

`MainWindow` should remain the screen coordinator, not the permanent owner of every tool algorithm.

`MainWindow` should:

- know which tool is active
- know which panel is active
- route input to the right tool owner
- request preview recomposition

The tool file should own:

- tool-local state
- interaction behavior
- object/layer update logic
- commit-ready output payload

---

## 13. Window Visibility Policy

Future windows and panels should be governed from a common settings path.

Recommended settings path:

- `Prefs -> Window`

This path should later control show/hide state for:

- Layers
- History
- Info
- Debug
- future work panels

Menu-level control should handle `Visible`.

Panel header control should handle `Expanded`.

---

## 14. First Safe Implementation Order

Implementation should follow this order.

1. document this architecture
2. add mode resolver: `Viewer / Edit / Multi`
3. route right-tab expansion through the mode resolver
4. connect right adjustment panel auto-collapse
5. connect toolbox enable/disable from mode
6. add `Layers` and `History` panel shells
7. start moving tools into separate files one by one
8. connect layer-backed object tools first:
   - Type
   - Rectangle
   - Path
9. connect adjustment-layer commits
10. connect pixel-edit layers later

---

## 15. Current Decision Summary

- `0 selected` means pure viewer behavior when auto mode is active
- adjustment UI should auto-collapse, not disappear
- layers and history are separate concepts
- preview must always be composed from base + layer stack
- existing work must remain selectable and editable later
- every tool must move toward its own file

This is the active screen input/output architecture direction for KRetouchPro.
