using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using AdvancedUIToolkit.Editor;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Exercises prefab ownership and malformed binding manifest checks.</summary>
    public sealed class UIBindingPrefabSafetyTests
    {
        /// <summary>Ensures an inner UIBindingRoot owns its children independently.</summary>
        [Test]
        public void NestedBindingRootsDoNotLeakReferencesToParent()
        {
            var parent = new GameObject("Parent", typeof(RectTransform), typeof(UIBindingRoot));
            var nested = new GameObject("Nested", typeof(RectTransform), typeof(UIBindingRoot));
            var button = new GameObject("BTN_Fire", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                nested.transform.SetParent(parent.transform, false);
                button.transform.SetParent(nested.transform, false);
                var nestedRoot = nested.GetComponent<UIBindingRoot>();
                var parentRoot = parent.GetComponent<UIBindingRoot>();
                Assert.IsFalse(UIBindingSafety.IsInScope(parentRoot, button.GetComponent<Button>()));
                Assert.IsTrue(UIBindingSafety.IsInScope(nestedRoot, button.GetComponent<Button>()));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        /// <summary>Ensures duplicate stable IDs, duplicate keys and missing references are reported.</summary>
        [Test]
        public void MalformedManifestReportsAllIdentityErrorsEvenWhenReferenceMissing()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform), typeof(UIBindingRoot));
            var child = new GameObject("BTN_Arm", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                child.transform.SetParent(rootObject.transform, false);
                var root = rootObject.GetComponent<UIBindingRoot>();
                var entries = new List<UIBindingRoot.Entry>
                {
                    new UIBindingRoot.Entry
                    {
                        Key = "Arm", StableId = "same-id", ComponentType = typeof(Button).FullName,
                        Reference = child.GetComponent<Button>()
                    },
                    new UIBindingRoot.Entry
                    {
                        Key = "Arm", StableId = "same-id", ComponentType = typeof(Button).FullName,
                        Reference = null
                    }
                };
                var errors = new List<string>();
                UIBindingSafety.ValidateManifest(root, entries, errors.Add);
                Assert.That(errors, Has.Some.Contains("Duplicate generated API key"));
                Assert.That(errors, Has.Some.Contains("Duplicate stable binding ID"));
                Assert.That(errors, Has.Some.Contains("Missing component reference"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        /// <summary>Ensures references outside the owner root and incompatible types are rejected.</summary>
        [Test]
        public void WrongOwnerAndTypeAreReported()
        {
            var rootObject = new GameObject("Root", typeof(UIBindingRoot));
            var external = new GameObject("BTN_External", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var root = rootObject.GetComponent<UIBindingRoot>();
                var entries = new List<UIBindingRoot.Entry>
                {
                    new UIBindingRoot.Entry
                    {
                        Key = "External", StableId = "external-id",
                        ComponentType = typeof(Slider).FullName,
                        Reference = external.GetComponent<Button>()
                    }
                };
                var errors = new List<string>();
                UIBindingSafety.ValidateManifest(root, entries, errors.Add);
                Assert.That(errors, Has.Some.Contains("outside its UI root"));
                Assert.That(errors, Has.Some.Contains("expects"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                UnityEngine.Object.DestroyImmediate(external);
            }
        }

        /// <summary>Rejects overwriting an existing generated file owned by an unregistered root.</summary>
        [Test]
        public void ExistingGeneratedFileRequiresSavedOwnership()
        {
            string path = Path.GetTempFileName();
            var go = new GameObject("Root", typeof(UIBindingRoot));
            try
            {
                Assert.IsNotNull(UIBindingSafety.GetExistingFileError(null, path));
                Assert.IsNotNull(UIBindingSafety.GetExistingFileError(
                    go.GetComponent<UIBindingRoot>(), path));
            }
            finally
            {
                File.Delete(path);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>Stops two unrelated scene UI roots from claiming one generated source path.</summary>
        [Test]
        public void DifferentSceneRootsCannotShareGeneratedClassOutput()
        {
            var first = new GameObject("First", typeof(UIBindingRoot));
            var second = new GameObject("Second", typeof(UIBindingRoot));
            try
            {
                var root = first.GetComponent<UIBindingRoot>();
                root.EditorSetGenerationProfile("UniqueSafetyBindings", "Game.Test", "Assets/Generated");
                string error = UIBindingSafety.GetOtherOwnerError(
                    second, "Assets/Generated/UniqueSafetyBindings.cs");
                Assert.That(error, Does.Contain("already owned"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        /// <summary>Allows normal scene-owned, unpacked UI roots to be edited.</summary>
        [Test]
        public void NormalSceneRootIsEditable()
        {
            var go = new GameObject("Screen", typeof(UIBindingRoot));
            try
            {
                Assert.IsNull(UIBindingSafety.GetTargetError(go));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
