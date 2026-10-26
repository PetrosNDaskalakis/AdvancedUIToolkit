# Settings Panel — Controls (importable source sample)

This sample demonstrates **one-way events from multiple standard uGUI control types** into an on-screen status label, using only the existing v0.9.8 runtime API. It is deliberately self-contained: **no JSON schema, no prefab assets, no prebuilt scene, and no generated game binding class required to compile the sample**.

## Import and assemble

1. In **Window > Package Manager > Advanced UI Toolkit > Samples**, import **Settings Panel — Controls**.
2. Create a Canvas and Panel with `UIBindingRoot`, then add these direct/descendant controls (the named GameObject must contain the expected component):

```text
Canvas
└── SettingsPanel  [UIBindingRoot, SettingsPanelExample]
    ├── SLD_Volume       [Slider]
    ├── TGL_Mute         [Toggle]
    ├── INP_PlayerName   [legacy InputField]
    ├── DDP_Quality      [legacy Dropdown]
    └── TXT_Status       [legacy Text]
```

3. For ease, create controls from **GameObject > Advanced UI Toolkit**. The Factory may use defaults from your selected station style profile, so verify the actual component type.
4. In Dropdown Inspector, add at least two options, e.g. `Low` and `High`, or leave the default options. Set Slider min/max to 0..1 for percentage formatting.
5. Open **Tools > Advanced UI Toolkit > Binder**. Scan and **generate/save the root manifest** so all keys exist: `Volume`, `Mute`, `PlayerName`, `Quality`, `Status`.
6. Attach imported `SettingsPanelExample` to the **same** root as `UIBindingRoot`.
7. Press Play. Moving slider, toggling mute, editing name and changing quality should update `TXT_Status` and Console.
8. Disable/re-enable SettingsPanel; each input should still trigger exactly one callback per interaction.

The sample uses a hand-authored `SettingsPanelBindingsSample` to remain compilable even before your first scan. Replace it with real generated bindings when building your production screen.

## Troubleshooting

- If compilation fails: ensure the package `AdvancedUIToolkit.Runtime` is installed and uGUI is present.
- If initialization throws: the manifest may not be generated or the root may not own all required keys.
- If a `TMP_Text` replaces `TXT_Status`: this specific sample expects **legacy** uGUI `Text`, matching Advanced UI Toolkit's default naming registry.
- If an input control doesn't change the status: check its interactable state, scene EventSystem and normal Unity UI setup.
