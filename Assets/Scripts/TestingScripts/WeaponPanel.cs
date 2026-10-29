using AdvancedUIToolkit;
using Game.UI.Generated;
using UnityEngine;

/// <summary>
/// Controls a simple weapon panel using generated UI bindings.
/// </summary>
public sealed class WeaponPanel : UIView<WeaponPanelBindings>
{
    private bool _isLoaded;
    private bool _isArmed;

    /// <summary>
    /// Registers button callbacks and initializes their states.
    /// </summary>
    protected override void OnInitialize()
    {
        Events.BindClick(UI.Load, OnLoadClicked);
        Events.BindClick(UI.Arm, OnArmClicked);
        Events.BindClick(UI.Fire, OnFireClicked);

        Events.BindValueChanged(UI.On, Toggle);
        Events.BindValueChanged(UI.Off, Toggle);

        UpdateButtonStates();
    }

    private void Toggle(bool isOn)
    {
        Debug.Log($"[Advanced UI Toolkit] Toggle is {(isOn ? "on" : "off")}", this);
    }

    /// <summary>Loads the weapon.</summary>
    private void OnLoadClicked()
    {
        _isLoaded = true;
        _isArmed = false;

        Debug.Log("[Advanced UI Toolkit] Loaded", this);
        UpdateButtonStates();
    }

    /// <summary>Arms the loaded weapon.</summary>
    private void OnArmClicked()
    {
        if (!_isLoaded) return;

        _isArmed = true;

        Debug.Log("[Advanced UI Toolkit] Armed", this);
        UpdateButtonStates();
    }

    /// <summary>Fires the weapon and resets the sequence.</summary>
    private void OnFireClicked()
    {
        if (!_isLoaded || !_isArmed) return;

        Debug.Log("[Advanced UI Toolkit] Fired!", this);

        _isLoaded = false;
        _isArmed = false;

        UpdateButtonStates();
    }

    /// <summary>
    /// Enables only the valid button for the current state.
    /// </summary>
    private void UpdateButtonStates()
    {
        UI.Load.interactable = !_isLoaded;
        UI.Arm.interactable = _isLoaded && !_isArmed;
        UI.Fire.interactable = _isLoaded && _isArmed;
    }
}
