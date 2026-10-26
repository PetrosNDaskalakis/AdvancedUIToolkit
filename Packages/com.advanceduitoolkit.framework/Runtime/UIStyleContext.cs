using UnityEngine;

namespace AdvancedUIToolkit
{
    /// <summary>
    /// Optional component on a Canvas, screen, or station root. Advanced UI Toolkit Factory uses
    /// the nearest ancestor context when placing new controls beneath this hierarchy.
    /// The component has no per-frame or runtime processing cost.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIStyleContext : MonoBehaviour
    {
        /// <summary>Style profile assigned to the current UI hierarchy.</summary>
        [SerializeField] private UIStyleProfile profile;

        /// <summary>Gets the style profile applied to this screen or station.</summary>
        public UIStyleProfile Profile => profile;
    }
}
