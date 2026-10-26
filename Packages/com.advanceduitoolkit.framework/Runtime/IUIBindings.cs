namespace AdvancedUIToolkit
{
    /// <summary>
    /// Implemented by classes produced by the AutoBinder code generator.
    /// View bindings are resolved once, when the screen initializes.
    /// </summary>
    public interface IUIBindings
    {
        /// <summary>Resolves the required UI component references once for a view.</summary>
        /// <param name="root">Serialized component registry owned by the view.</param>
        void Initialize(UIBindingRoot root);
    }
}
