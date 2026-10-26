using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using AdvancedUIToolkit.Editor;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>
    /// Ensures a visual prefab may use a wrapper GameObject while the actual
    /// bindable uGUI component lives on a unique child of that prefab.
    /// </summary>
    public sealed class AdvancedUIToolkitNestedTemplateTests
    {
        /// <summary>Root-level Image always takes precedence over nested Images.</summary>
        [Test]
        public void ComponentOnRootTakesPrecedenceOverNestedImage()
        {
            GameObject wrapper = new GameObject("IMG_Icon", typeof(RectTransform), typeof(Image));
            try
            {
                GameObject child = new GameObject("Decoration", typeof(RectTransform), typeof(Image));
                child.transform.SetParent(wrapper.transform, false);
                Component resolved = AdvancedUIToolkitNaming.ResolveNamedComponent(
                    wrapper.transform, typeof(Image), out string problem);
                Assert.AreSame(wrapper.GetComponent<Image>(), resolved);
                Assert.IsNull(problem);
            }
            finally { UnityEngine.Object.DestroyImmediate(wrapper); }
        }

        /// <summary>Inactive descendants also qualify as the binding target.</summary>
        [Test]
        public void UniqueNestedImageCanBeBoundFromNamedWrapper()
        {
            GameObject panel = new GameObject("Screen", typeof(RectTransform), typeof(UIBindingRoot));
            UIAutoBinderWindow binder = null;
            try
            {
                GameObject wrapper = new GameObject("IMG_Icon", typeof(RectTransform));
                wrapper.transform.SetParent(panel.transform, false);
                GameObject nested = new GameObject("IconGraphic", typeof(RectTransform), typeof(Image));
                nested.transform.SetParent(wrapper.transform, false);
                nested.SetActive(false);
                binder = ScriptableObject.CreateInstance<UIAutoBinderWindow>();
                binder.SetWorkspaceTarget(panel);

                Assert.AreEqual(0, binder.WorkspaceErrorCount,
                    string.Join(" | ", binder.GetWorkspaceIssues(true)));
                Assert.IsTrue(binder.WorkspaceCanGenerate);
                Assert.AreEqual(1, binder.GetWorkspaceBindings().Count);
                Assert.AreEqual("Icon", binder.GetWorkspaceBindings()[0].ApiKey);
                Assert.AreSame(nested.GetComponent<Image>(),
                    binder.GetWorkspaceBindings()[0].Component);
            }
            finally
            {
                if (binder != null) UnityEngine.Object.DestroyImmediate(binder);
                UnityEngine.Object.DestroyImmediate(panel);
            }
        }

        /// <summary>Ambiguous child images must not produce an arbitrary binding.</summary>
        [Test]
        public void MultipleNestedImagesAreReportedAsAmbiguous()
        {
            GameObject panel = new GameObject("Screen", typeof(RectTransform), typeof(UIBindingRoot));
            UIAutoBinderWindow binder = null;
            try
            {
                GameObject wrapper = new GameObject("IMG_Icon", typeof(RectTransform));
                wrapper.transform.SetParent(panel.transform, false);
                for (int i = 0; i < 2; i++)
                {
                    GameObject image = new GameObject("Decoration" + i,
                        typeof(RectTransform), typeof(Image));
                    image.transform.SetParent(wrapper.transform, false);
                }
                binder = ScriptableObject.CreateInstance<UIAutoBinderWindow>();
                binder.SetWorkspaceTarget(panel);
                Assert.Greater(binder.WorkspaceErrorCount, 0);
                Assert.IsFalse(binder.WorkspaceCanGenerate);
                Assert.That(binder.GetWorkspaceIssues(true),
                    Has.Some.Contains("multiple Image components"));
            }
            finally
            {
                if (binder != null) UnityEngine.Object.DestroyImmediate(binder);
                UnityEngine.Object.DestroyImmediate(panel);
            }
        }

        /// <summary>A component from a nested, independent binding root cannot be stolen.</summary>
        [Test]
        public void NestedBindingRootImageCannotSatisfyWrapperBinding()
        {
            GameObject panel = new GameObject("Screen", typeof(RectTransform), typeof(UIBindingRoot));
            try
            {
                GameObject wrapper = new GameObject("IMG_Outer", typeof(RectTransform));
                wrapper.transform.SetParent(panel.transform, false);
                GameObject nestedRoot = new GameObject("Widget", typeof(RectTransform), typeof(UIBindingRoot));
                nestedRoot.transform.SetParent(wrapper.transform, false);
                GameObject image = new GameObject("IconGraphic", typeof(RectTransform), typeof(Image));
                image.transform.SetParent(nestedRoot.transform, false);
                Component resolved = AdvancedUIToolkitNaming.ResolveNamedComponent(
                    wrapper.transform, typeof(Image), out string problem);
                Assert.IsNull(resolved);
                Assert.That(problem, Does.Contain("no Image"));
            }
            finally { UnityEngine.Object.DestroyImmediate(panel); }
        }

        /// <summary>Saved manifests and diagnostics accept nested component references.</summary>
        [Test]
        public void DiagnosticsAcceptsSavedNestedImageReference()
        {
            GameObject panel = new GameObject("Screen", typeof(RectTransform), typeof(UIBindingRoot));
            try
            {
                GameObject wrapper = new GameObject("IMG_Icon", typeof(RectTransform));
                wrapper.transform.SetParent(panel.transform, false);
                GameObject child = new GameObject("Graphic", typeof(RectTransform), typeof(Image));
                child.transform.SetParent(wrapper.transform, false);
                UIBindingRoot bindingRoot = panel.GetComponent<UIBindingRoot>();
                bindingRoot.EditorSetEntries(new List<UIBindingRoot.Entry>
                {
                    new UIBindingRoot.Entry
                    {
                        Key = "Icon", HierarchyKey = "Icon", StableId = Guid.NewGuid().ToString("N"),
                        ComponentType = typeof(Image).FullName, Reference = child.GetComponent<Image>()
                    }
                });
                AdvancedUIToolkitDiagnostics.Report report = new AdvancedUIToolkitDiagnostics.Report();
                AdvancedUIToolkitDiagnostics.ValidateRoot(bindingRoot, "scene", report);
                Assert.IsFalse(report.Findings.Any(f =>
                    f.Level == AdvancedUIToolkitDiagnostics.Severity.Error ||
                    f.Description.Contains("prefix") || f.Description.Contains("name changed")),
                    string.Join(" | ", report.Findings.Select(f => f.Description)));
            }
            finally { UnityEngine.Object.DestroyImmediate(panel); }
        }

        /// <summary>Factory accepts a prefab with Image on a child, retaining its prefab link.</summary>
        [Test]
        public void FactoryInstantiatesPrefabWithNestedImage()
        {
            string folder = "Assets/AUIToolkitNestedTest_" + Guid.NewGuid().ToString("N");
            GameObject assetSource = null;
            GameObject panel = null;
            try
            {
                string leaf = folder.Substring("Assets/".Length);
                Assert.IsNotEmpty(AssetDatabase.CreateFolder("Assets", leaf));
                assetSource = new GameObject("ImageContainer", typeof(RectTransform));
                GameObject graphic = new GameObject("Graphic", typeof(RectTransform), typeof(Image));
                graphic.transform.SetParent(assetSource.transform, false);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(assetSource, folder + "/NestedImage.prefab");
                Assert.IsNotNull(prefab);

                panel = new GameObject("Screen", typeof(RectTransform), typeof(UIBindingRoot));
                UIComponentTemplate template = new UIComponentTemplate
                {
                    Kind = UIComponentKind.Image,
                    Prefab = prefab,
                    DefaultObjectName = "IMG_Image"
                };
                GameObject created = UIStyleFactory.CreatePrefab(
                    (RectTransform)panel.transform, template, "IMG_Image");
                Assert.IsNotNull(created);
                Assert.IsNull(created.GetComponent<Image>());
                Assert.IsNotNull(created.GetComponentInChildren<Image>(true));
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(created));

                UIAutoBinderWindow binder = ScriptableObject.CreateInstance<UIAutoBinderWindow>();
                try
                {
                    binder.SetWorkspaceTarget(panel);
                    Assert.AreEqual(0, binder.WorkspaceErrorCount,
                        string.Join(" | ", binder.GetWorkspaceIssues(true)));
                    Assert.AreEqual("Image", binder.GetWorkspaceBindings()[0].ApiKey);
                    Assert.AreSame(created.GetComponentInChildren<Image>(true),
                        binder.GetWorkspaceBindings()[0].Component);
                }
                finally { UnityEngine.Object.DestroyImmediate(binder); }
            }
            finally
            {
                Selection.activeObject = null;
                if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                if (assetSource != null) UnityEngine.Object.DestroyImmediate(assetSource);
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
