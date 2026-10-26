using System;
using System.Collections.Generic;
using NUnit.Framework;
using AdvancedUIToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>
    /// Release-candidate regression checks for shared style profiles and deterministic
    /// typed binding generation. These tests run in EditMode without project scenes.
    /// </summary>
    public sealed class AdvancedUIToolkitReleaseCandidateTests
    {
        /// <summary>
        /// Confirms a station profile overrides a shared Button while retaining
        /// inherited components of other types.
        /// </summary>
        [Test]
        public void StationProfileOverridesOnlyMatchingControlKind()
        {
            UIStyleProfile baseProfile = ScriptableObject.CreateInstance<UIStyleProfile>();
            UIStyleProfile sonarProfile = ScriptableObject.CreateInstance<UIStyleProfile>();
            GameObject baseButton = new GameObject("BaseButton");
            GameObject sonarButton = new GameObject("SonarButton");
            GameObject baseSlider = new GameObject("BaseSlider");
            try
            {
                AddTemplate(baseProfile, UIComponentKind.Button, baseButton);
                AddTemplate(baseProfile, UIComponentKind.Slider, baseSlider);
                SetBaseProfile(sonarProfile, baseProfile);
                AddTemplate(sonarProfile, UIComponentKind.Button, sonarButton);

                Assert.IsTrue(sonarProfile.TryResolve(UIComponentKind.Button, out UIComponentTemplate button));
                Assert.AreSame(sonarButton, button.Prefab);
                Assert.IsTrue(sonarProfile.TryResolve(UIComponentKind.Slider, out UIComponentTemplate slider));
                Assert.AreSame(baseSlider, slider.Prefab);
                Assert.AreEqual(2, sonarProfile.GetEffectiveTemplates().Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseProfile);
                UnityEngine.Object.DestroyImmediate(sonarProfile);
                UnityEngine.Object.DestroyImmediate(baseButton);
                UnityEngine.Object.DestroyImmediate(sonarButton);
                UnityEngine.Object.DestroyImmediate(baseSlider);
            }
        }

        /// <summary>
        /// Confirms malformed cyclic inheritance does not cause an infinite loop.
        /// </summary>
        [Test]
        public void CyclicProfilesTerminateSafely()
        {
            UIStyleProfile first = ScriptableObject.CreateInstance<UIStyleProfile>();
            UIStyleProfile second = ScriptableObject.CreateInstance<UIStyleProfile>();
            GameObject button = new GameObject("Button");
            try
            {
                SetBaseProfile(first, second);
                SetBaseProfile(second, first);
                AddTemplate(second, UIComponentKind.Button, button);
                Assert.IsTrue(first.TryResolve(UIComponentKind.Button, out UIComponentTemplate found));
                Assert.AreSame(button, found.Prefab);
                Assert.AreEqual(1, first.GetEffectiveTemplates().Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(button);
            }
        }

        /// <summary>
        /// Confirms generated code is order-independent, documented, and cached.
        /// This method does not create or modify a .cs file.
        /// </summary>
        [Test]
        public void BindingGenerationIsDeterministicAndHasXmlComments()
        {
            var arm = new UIBindingRoot.Entry
            {
                Key = "Arm", HierarchyKey = "Arm", ComponentType = typeof(Button).FullName
            };
            var fire = new UIBindingRoot.Entry
            {
                Key = "Fire", HierarchyKey = "Fire", ComponentType = typeof(Button).FullName
            };
            string first = AdvancedUIToolkitCodeTemplate.Generate("WeaponBindings", "Game.UI.Generated",
                new[] { fire, arm }, null);
            string second = AdvancedUIToolkitCodeTemplate.Generate("WeaponBindings", "Game.UI.Generated",
                new[] { arm, fire }, null);
            Assert.AreEqual(first, second);
            StringAssert.Contains("/// <summary>Cached reference to Arm UI component.</summary>", first);
            StringAssert.Contains("/// <summary>Cached reference to Fire UI component.</summary>", first);
            StringAssert.Contains("Arm = root.Get<UnityEngine.UI.Button>(\"Arm\");", first);
            Assert.Less(first.IndexOf("public UnityEngine.UI.Button Arm", StringComparison.Ordinal),
                first.IndexOf("public UnityEngine.UI.Button Fire", StringComparison.Ordinal));
        }

        /// <summary>
        /// Sets a profile's base through its actual serialized field, matching Inspector editing.
        /// </summary>
        /// <param name="profile">Child style profile.</param>
        /// <param name="parent">Profile to inherit from.</param>
        private static void SetBaseProfile(UIStyleProfile profile, UIStyleProfile parent)
        {
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("baseProfile").objectReferenceValue = parent;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Adds a registered component through serialized profile data.
        /// </summary>
        /// <param name="profile">Profile being configured.</param>
        /// <param name="kind">Component category.</param>
        /// <param name="prefab">GameObject used as a reference by the test.</param>
        private static void AddTemplate(UIStyleProfile profile, UIComponentKind kind, GameObject prefab)
        {
            var serialized = new SerializedObject(profile);
            SerializedProperty list = serialized.FindProperty("templates");
            list.arraySize++;
            SerializedProperty item = list.GetArrayElementAtIndex(list.arraySize - 1);
            item.FindPropertyRelative("Kind").enumValueIndex = (int)kind;
            item.FindPropertyRelative("Id").stringValue = string.Empty;
            item.FindPropertyRelative("DefaultObjectName").stringValue = "BTN_Test";
            item.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
