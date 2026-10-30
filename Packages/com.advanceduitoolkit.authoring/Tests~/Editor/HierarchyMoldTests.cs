using System;
using System.Collections.Generic;
using AdvancedUIAuthoring.Integration.Editor;
using AdvancedUIToolkit;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdvancedUIAuthoring.MoldTests
{
    /// <summary>EditMode integration coverage; run in a disposable project with the supplied package pair.</summary>
    public sealed class HierarchyMoldTests
    {
        /// <summary>Isolated additive test scene.</summary>
        private Scene scene;
        /// <summary>Previously active scene, restored during cleanup.</summary>
        private Scene previous;
        /// <summary>Unique temporary asset folder.</summary>
        private string folder;
        /// <summary>Transient snapshots/assets to release.</summary>
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

        /// <summary>Create an isolated fixture scene and asset folder.</summary>
        [SetUp]
        public void SetUp()
        {
            previous = SceneManager.GetActiveScene();
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            folder = "Assets/MoldTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            Undo.IncrementCurrentGroup();
        }

        /// <summary>Remove only this fixture's temporary scene, snapshots and assets.</summary>
        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object item in owned)
                if (item != null && !AssetDatabase.Contains(item)) UnityEngine.Object.DestroyImmediate(item);
            owned.Clear();
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.DeleteAsset(folder);
            Undo.IncrementCurrentGroup();
        }

        /// <summary>Create an ordinary rectangular test node with optional parent.</summary>
        private static GameObject Node(string name, Transform parent = null)
        {
            var result = new GameObject(name, typeof(RectTransform));
            if (parent != null) result.transform.SetParent(parent, false);
            return result;
        }

        /// <summary>Capture and own a fixture snapshot, reporting the service's precise failure.</summary>
        private UIHierarchyMold Capture(GameObject root)
        {
            Assert.IsTrue(UIHierarchyMoldCapture.TryCapture(root, out UIHierarchyMold mold, out string error), error);
            owned.Add(mold);
            return mold;
        }

        /// <summary>Create a fixture instance under its explicit destination.</summary>
        private static GameObject Create(UIHierarchyMold mold, GameObject target)
        {
            Assert.IsTrue(UIHierarchyMoldBuilder.TryCreate(mold, (RectTransform)target.transform, "Copy", out GameObject result, out string error), error);
            return result;
        }

        /// <summary>Nested inactive nodes and repeated names survive without name-based identity or source edits.</summary>
        [Test]
        public void CaptureAndRestoreHierarchyValues()
        {
            GameObject source = Node("Source");
            GameObject child = Node("Repeated", source.transform);
            GameObject nested = Node("Nested", child.transform);
            Node("Repeated", source.transform);
            child.SetActive(false);
            var rect = (RectTransform)nested.transform;
            rect.anchorMin = new Vector2(.1f, .2f); rect.anchorMax = new Vector2(.8f, .9f);
            rect.pivot = new Vector2(.3f, .7f); rect.sizeDelta = new Vector2(120, 45);
            rect.anchoredPosition3D = new Vector3(12, -7, 2);
            UIHierarchyMold mold = Capture(source);
            Assert.AreEqual(4, mold.nodes.Count);
            Assert.AreEqual(1, mold.nodes[2].parent);
            Assert.IsFalse(child.activeSelf);
            Assert.AreEqual(2, source.transform.childCount);
            UnityEngine.Object.DestroyImmediate(source);
            GameObject instance = Create(mold, Node("Target"));
            var restored = (RectTransform)instance.transform.GetChild(0).GetChild(0);
            Assert.IsFalse(instance.transform.GetChild(0).gameObject.activeSelf);
            Assert.AreEqual(new Vector2(.1f, .2f), restored.anchorMin);
            Assert.AreEqual(new Vector2(120, 45), restored.sizeDelta);
            Assert.AreEqual(new Vector3(12, -7, 2), restored.anchoredPosition3D);
            Assert.AreEqual(instance.transform.GetChild(0).name, instance.transform.GetChild(1).name);
        }

        /// <summary>Internal links, duplicate component slots, nested arrays and large scalars round-trip without external pointers.</summary>
        [Test]
        public void RemapsComponentSlotsAndClearsExternalReferences()
        {
            GameObject source = Node("Source");
            GameObject child = Node("Child", source.transform);
            var first = source.AddComponent<MoldTestProbe>();
            var second = source.AddComponent<MoldTestProbe>();
            var outside = Node("Outside");
            first.number = -42; first.unsignedNumber = ulong.MaxValue; first.precise = 1.234567890123;
            first.text = "{\"instanceID\":123} must remain text";
            first.objectLink = child; first.componentLink = second; first.external = outside;
            first.rows.Add(new MoldTestProbe.Row { number = 7, link = child.transform });
            first.rows.Add(new MoldTestProbe.Row { number = 8, link = outside });
            second.componentLink = first;
            UIHierarchyMold mold = Capture(source);
            Assert.AreEqual(2, mold.omissions.Count);
            GameObject copy = Create(mold, Node("Target"));
            MoldTestProbe[] probes = copy.GetComponents<MoldTestProbe>();
            Assert.AreEqual(2, probes.Length);
            Assert.AreEqual(-42, probes[0].number); Assert.AreEqual(ulong.MaxValue, probes[0].unsignedNumber);
            Assert.AreEqual(first.precise, probes[0].precise); Assert.AreEqual(first.text, probes[0].text);
            Assert.AreSame(probes[1], probes[0].componentLink); Assert.AreSame(probes[0], probes[1].componentLink);
            Assert.AreSame(copy.transform.GetChild(0).gameObject, probes[0].objectLink);
            Assert.IsNull(probes[0].external); Assert.AreSame(outside, first.external);
            Assert.AreEqual(2, probes[0].rows.Count); Assert.AreEqual(7, probes[0].rows[0].number);
            Assert.AreSame(copy.transform.GetChild(0), probes[0].rows[0].link); Assert.IsNull(probes[0].rows[1].link);
            Assert.AreEqual(first.curve.Evaluate(.25f), probes[0].curve.Evaluate(.25f));
            Assert.AreEqual(first.gradient.Evaluate(.4f), probes[0].gradient.Evaluate(.4f));
        }

        /// <summary>Prefab branches are excluded while retained container values persist independently of the source hierarchy.</summary>
        [Test]
        public void ExcludedPrefabHasNoSavedMoldDependency()
        {
            GameObject original = Node("Source");
            original.AddComponent<Image>().color = Color.red;
            var texture = new Texture2D(2, 2);
            AssetDatabase.CreateAsset(texture, folder + "/Texture.asset");
            original.AddComponent<MoldTestProbe>().external = texture;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(original, folder + "/Source.prefab");
            GameObject source = Node("Panel");
            source.AddComponent<Image>().color = Color.red;
            source.AddComponent<MoldTestProbe>().external = texture;
            GameObject prefabChild = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            prefabChild.transform.SetParent(source.transform, false);
            UIHierarchyMold mold = Capture(source);
            Assert.AreEqual(1, mold.nodes.Count);
            var saved = ScriptableObject.CreateInstance<UIHierarchyMold>();
            EditorUtility.CopySerialized(mold, saved); saved.hideFlags = HideFlags.None;
            string path = folder + "/Mold.asset";
            AssetDatabase.CreateAsset(saved, path);
            string[] dependencies = AssetDatabase.GetDependencies(path);
            CollectionAssert.DoesNotContain(dependencies, folder + "/Source.prefab");
            CollectionAssert.DoesNotContain(dependencies, folder + "/Texture.asset");
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(prefabChild));
            Assert.AreSame(texture, source.GetComponent<MoldTestProbe>().external);
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(original);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            GameObject copy = Create(AssetDatabase.LoadAssetAtPath<UIHierarchyMold>(path), Node("Target"));
            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(copy));
            Assert.AreEqual(0, copy.transform.childCount);
            Assert.AreEqual(Color.red, copy.GetComponent<Image>().color);
            Assert.IsNull(copy.GetComponent<MoldTestProbe>().external);
        }

        /// <summary>Non-prefab objects follow eligibility, not a Button/Toggle type blacklist; required components still restore correctly.</summary>
        [Test]
        public void BuiltInUIComponentsReuseRequiredSlots()
        {
            GameObject source = Node("Panel");
            var layout = source.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 13; layout.padding = new RectOffset(1, 2, 3, 4);
            GameObject button = Node("Button", source.transform);
            Image image = button.AddComponent<Image>();
            Button control = button.AddComponent<Button>(); control.targetGraphic = image;
            GameObject copy = Create(Capture(source), Node("Target"));
            Assert.AreEqual(13, copy.GetComponent<VerticalLayoutGroup>().spacing);
            Assert.AreEqual(4, copy.GetComponent<VerticalLayoutGroup>().padding.bottom);
            GameObject newButton = copy.transform.GetChild(0).gameObject;
            Assert.AreEqual(1, newButton.GetComponents<CanvasRenderer>().Length);
            Assert.AreSame(newButton.GetComponent<Image>(), newButton.GetComponent<Button>().targetGraphic);
        }

        /// <summary>New screens never inherit the source Binder's manifest or output-file ownership.</summary>
        [Test]
        public void BindingConfigurationIsReset()
        {
            GameObject source = Node("Source");
            source.AddComponent<UIBindingRoot>().EditorSetGenerationProfile("SourceBindings", "Example", "Assets/Generated");
            UIHierarchyMold mold = Capture(source);
            Assert.AreEqual(1, mold.omissions.Count);
            GameObject copy = Create(mold, Node("Target"));
            copy.GetComponent<UIBindingRoot>().EditorGetGenerationProfile(out string name, out string ns, out string output);
            Assert.IsTrue(string.IsNullOrEmpty(name)); Assert.IsTrue(string.IsNullOrEmpty(output));
        }

        /// <summary>Unsupported managed references block capture and return no partial mold.</summary>
        [Test]
        public void UnsupportedSerializedFieldsAreReported()
        {
            GameObject source = Node("Source"); source.AddComponent<MoldUnsupportedProbe>();
            Assert.IsFalse(UIHierarchyMoldCapture.TryCapture(source, out UIHierarchyMold mold, out string error));
            Assert.IsNull(mold); StringAssert.Contains("payload", error);
            Assert.AreEqual(7, source.GetComponent<MoldUnsupportedProbe>().payload.number);
        }

        /// <summary>Missing types fail before creation; removed property paths roll back new objects.</summary>
        [Test]
        public void InvalidMoldsCannotLeavePartialHierarchies()
        {
            GameObject source = Node("Source"); source.AddComponent<MoldTestProbe>().number = 12;
            UIHierarchyMold mold = Capture(source);
            GameObject destination = Node("Target");
            string type = mold.nodes[0].components[0].typeName;
            mold.nodes[0].components[0].typeName = "Missing.Type, Missing.Assembly";
            Assert.IsFalse(UIHierarchyMoldBuilder.TryCreate(mold, (RectTransform)destination.transform, "Copy", out GameObject created, out string error));
            Assert.AreEqual(0, destination.transform.childCount); Assert.IsNull(created);
            mold.nodes[0].components[0].typeName = type;
            mold.nodes[0].components[0].properties[0].path = "fieldThatNoLongerExists";
            Assert.IsFalse(UIHierarchyMoldBuilder.TryCreate(mold, (RectTransform)destination.transform, "Copy", out created, out error));
            Assert.AreEqual(0, destination.transform.childCount); Assert.IsNull(created);
            Assert.AreEqual(12, source.GetComponent<MoldTestProbe>().number);
        }

        /// <summary>A destination name collision blocks without overwriting or renaming existing objects.</summary>
        [Test]
        public void RootCollisionPreservesExistingChild()
        {
            UIHierarchyMold mold = Capture(Node("Source"));
            GameObject target = Node("Target"); GameObject existing = Node("Copy", target.transform);
            Assert.IsFalse(UIHierarchyMoldBuilder.TryCreate(mold, (RectTransform)target.transform, "Copy", out GameObject created, out string error));
            Assert.IsNull(created); Assert.AreEqual(1, target.transform.childCount);
            Assert.AreSame(existing.transform, target.transform.GetChild(0));
        }

        /// <summary>One scene Undo removes the whole creation and Redo restores values and internal links.</summary>
        [Test]
        public void UndoRedoRestoresCompleteHierarchy()
        {
            GameObject source = Node("Source"); GameObject child = Node("Child", source.transform);
            var probe = source.AddComponent<MoldTestProbe>(); probe.number = 73; probe.objectLink = child;
            GameObject target = Node("Target");
            Create(Capture(source), target);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.AreEqual(0, target.transform.childCount);
            Undo.PerformRedo();
            Assert.AreEqual(1, target.transform.childCount);
            GameObject copy = target.transform.GetChild(0).gameObject;
            Assert.AreEqual(73, copy.GetComponent<MoldTestProbe>().number);
            Assert.AreSame(copy.transform.GetChild(0).gameObject, copy.GetComponent<MoldTestProbe>().objectLink);
        }

        /// <summary>The requested Panel/Header/Body/parents/Footer skeleton excludes complete prefab controls, including added overrides.</summary>
        [Test]
        public void CapturesOnlyTheSelectableContainerSkeleton()
        {
            GameObject panel = Node("Panel");
            Node("Header", panel.transform);
            GameObject body = Node("Body", panel.transform);
            GameObject buttons = Node("Buttons parent", body.transform);
            buttons.AddComponent<VerticalLayoutGroup>().spacing = 19;
            GameObject toggles = Node("Toggles parent", body.transform);
            toggles.SetActive(false);
            GameObject footer = Node("Footer", panel.transform);
            GameObject template = Node("Button prefab");
            template.AddComponent<Image>(); template.AddComponent<Button>();
            Node("Text", template.transform).AddComponent<Text>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(template, folder + "/Button.prefab");
            GameObject first = null;
            for (int i = 0; i < 3; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(buttons.transform, false);
                if (i == 0) first = instance;
            }
            // Added non-prefab overrides inside an excluded prefab must not leak into the skeleton.
            Node("Added override", first.transform).AddComponent<MoldUnsupportedProbe>();
            GameObject toggleTemplate = Node("Toggle prefab"); toggleTemplate.AddComponent<Toggle>();
            GameObject togglePrefab = PrefabUtility.SaveAsPrefabAsset(toggleTemplate, folder + "/Toggle.prefab");
            GameObject toggle = (GameObject)PrefabUtility.InstantiatePrefab(togglePrefab, scene);
            toggle.transform.SetParent(toggles.transform, false);
            var probe = panel.AddComponent<MoldTestProbe>();
            probe.objectLink = first;
            probe.componentLink = first.transform.GetChild(0).GetComponent<Text>();
            probe.rows.Add(new MoldTestProbe.Row { number = 9, link = footer.transform });

            UIHierarchyMold mold = Capture(panel);
            Assert.AreEqual(2, mold.version);
            CollectionAssert.AreEqual(new[] { "Panel", "Header", "Body", "Buttons parent", "Toggles parent", "Footer" }, mold.nodes.ConvertAll(n => n.name));
            CollectionAssert.AreEqual(new[] { -1, 0, 0, 2, 2, 0 }, mold.nodes.ConvertAll(n => n.parent));
            Assert.AreEqual(6, mold.omissions.Count); // Four skipped branches and two references into them.
            GameObject copy = Create(mold, Node("Target"));
            Transform newBody = copy.transform.GetChild(1);
            Assert.AreEqual(0, newBody.GetChild(0).childCount);
            Assert.AreEqual(0, newBody.GetChild(1).childCount);
            Assert.AreEqual(19, newBody.GetChild(0).GetComponent<VerticalLayoutGroup>().spacing);
            Assert.IsFalse(newBody.GetChild(1).gameObject.activeSelf);
            Assert.AreEqual(0, copy.GetComponentsInChildren<Button>(true).Length);
            Assert.AreEqual(0, copy.GetComponentsInChildren<Toggle>(true).Length);
            Assert.AreEqual(0, copy.GetComponentsInChildren<Text>(true).Length);
            Assert.IsNull(copy.GetComponent<MoldTestProbe>().objectLink);
            Assert.IsNull(copy.GetComponent<MoldTestProbe>().componentLink);
            Assert.AreSame(copy.transform.GetChild(2), copy.GetComponent<MoldTestProbe>().rows[0].link);
            Assert.AreSame(first, probe.objectLink);
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(first));
            Assert.AreEqual(3, buttons.transform.childCount);
        }

        /// <summary>Capture uses the same eligibility predicate as root selection and rejects prefab roots/descendants without fallback.</summary>
        [Test]
        public void SourceEligibilityMatchesRootSelection()
        {
            GameObject parent = Node("Panel");
            GameObject template = Node("Prefab"); Node("Child", template.transform);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(template, folder + "/Selection.prefab");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(parent.transform, false);
            GameObject added = Node("Override", instance.transform);
            var plain = new GameObject("Plain Transform");
            foreach (GameObject candidate in new[] { parent, template, prefab, instance, instance.transform.GetChild(0).gameObject, added, plain })
                Assert.AreEqual(AdvancedUIToolkit.Editor.AdvancedUIEditorIntegration.IsValidCreationParent(candidate.GetComponent<RectTransform>()),
                    UIHierarchyMoldCapture.IsValidSource(candidate), candidate.name);
            Assert.IsFalse(UIHierarchyMoldCapture.TryCapture(instance, out UIHierarchyMold mold, out string error));
            Assert.IsNull(mold); StringAssert.Contains("selectable", error);
            Assert.AreEqual(1, parent.transform.childCount);
        }

        /// <summary>Old flattened molds cannot recreate unwanted controls under the new contract or be silently rewritten.</summary>
        [Test]
        public void LegacyMoldRequiresRecaptureWithoutMutation()
        {
            UIHierarchyMold mold = Capture(Node("Source")); mold.version = 1;
            string before = JsonUtility.ToJson(mold);
            GameObject target = Node("Target");
            Assert.IsFalse(UIHierarchyMoldBuilder.TryCreate(mold, (RectTransform)target.transform, "Copy", out GameObject created, out string error));
            StringAssert.Contains("Capture a new mold from the original hierarchy", error);
            StringAssert.Contains("older capture format", error);
            StringAssert.DoesNotContain("includes flattened prefab contents", error);
            Assert.AreEqual(before, JsonUtility.ToJson(mold));
            Assert.IsNull(created); Assert.AreEqual(0, target.transform.childCount);
        }

        /// <summary>Skipped non-RectTransform branches are disclosed and their descendants are never silently reparented.</summary>
        [Test]
        public void NonSelectableBranchDoesNotPromoteDescendants()
        {
            GameObject source = Node("Panel");
            var branch = new GameObject("Non-UI group"); branch.transform.SetParent(source.transform, false);
            Node("Nested UI", branch.transform);
            Node("Kept", source.transform);
            UIHierarchyMold mold = Capture(source);
            CollectionAssert.AreEqual(new[] { "Panel", "Kept" }, mold.nodes.ConvertAll(n => n.name));
            Assert.AreEqual(1, mold.omissions.Count);
            StringAssert.Contains("Non-UI group", mold.omissions[0]);
        }

        /// <summary>Loaded data obeys the same depth boundary as capture, without allocating scene objects.</summary>
        [Test]
        public void LoadedMoldDepthLimitBlocksBeforeCreation()
        {
            UIHierarchyMold mold = Capture(Node("Source"));
            for (int i = 1; i <= UIHierarchyMold.MaximumDepth; i++)
                mold.nodes.Add(new UIMoldNode { name = "Nested" + i, parent = i - 1, rectTransform = true });
            Assert.IsTrue(UIHierarchyMoldBuilder.ValidateMold(mold, out string error), error);
            mold.nodes.Add(new UIMoldNode { name = "TooDeep", parent = mold.nodes.Count - 1, rectTransform = true });
            GameObject target = Node("Target");
            Assert.IsFalse(UIHierarchyMoldBuilder.TryCreate(mold, (RectTransform)target.transform, "Copy", out GameObject copy, out error));
            StringAssert.Contains("depth 256", error);
            Assert.IsNull(copy); Assert.AreEqual(0, target.transform.childCount);
        }

        /// <summary>Invalid numeric geometry is rejected without clamping valid negative offsets or sizes.</summary>
        [Test]
        public void NonFiniteGeometryBlocksBeforeCreation()
        {
            UIHierarchyMold mold = Capture(Node("Source"));
            GameObject target = Node("Target");
            mold.nodes[0].sizeDelta = new Vector2(-20, 40);
            Assert.IsTrue(UIHierarchyMoldBuilder.ValidateMold(mold, out string error), error);
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                mold.nodes[0].sizeDelta = new Vector2(invalid, 40);
                Assert.IsFalse(UIHierarchyMoldBuilder.TryCreate(mold, (RectTransform)target.transform, "Copy", out GameObject copy, out error));
                StringAssert.Contains("Non-finite transform", error);
                Assert.IsNull(copy); Assert.AreEqual(0, target.transform.childCount);
            }
        }

        /// <summary>Unsupported kinds, absent leaf values and malformed null links fail portable-data preflight.</summary>
        [Test]
        public void InvalidPropertyMetadataBlocksBeforeCreation()
        {
            GameObject source = Node("Source"); source.AddComponent<MoldTestProbe>();
            UIHierarchyMold mold = Capture(source);
            UIMoldProperty value = mold.nodes[0].components[0].properties.Find(p => p.path == "number");
            int originalKind = value.kind;
            GameObject target = Node("Target");
            foreach (int unsupported in new[] { (int)SerializedPropertyType.ManagedReference, int.MaxValue })
            {
                value.kind = unsupported;
                Assert.IsFalse(UIHierarchyMoldBuilder.TryCreate(mold, (RectTransform)target.transform, "Copy", out GameObject copy, out string error));
                StringAssert.Contains("Unsupported property kind", error); Assert.IsNull(copy);
            }
            value.kind = originalKind;
            string originalValue = value.value; value.value = " ";
            Assert.IsFalse(UIHierarchyMoldBuilder.ValidateMold(mold, out string missing));
            StringAssert.Contains("Missing mold value", missing);
            value.value = originalValue;
            UIMoldProperty link = mold.nodes[0].components[0].properties.Find(p => p.path == "objectLink");
            link.node = -1; link.component = 0;
            Assert.IsFalse(UIHierarchyMoldBuilder.ValidateMold(mold, out string badLink));
            StringAssert.Contains("Invalid internal reference", badLink);
            Assert.AreEqual(0, target.transform.childCount);
        }

        /// <summary>Source, stored snapshot and created hierarchy remain independent until an explicit recapture.</summary>
        [Test]
        public void CreatedHierarchyHasNoLiveMoldOrSourceLink()
        {
            GameObject source = Node("Source");
            Node("Body", source.transform).AddComponent<LayoutElement>().preferredWidth = 140;
            UIHierarchyMold mold = Capture(source);
            GameObject copy = Create(mold, Node("Target"));
            source.transform.GetChild(0).name = "SourceEdited";
            mold.nodes[1].name = "MoldEdited";
            Assert.AreEqual("Body", copy.transform.GetChild(0).name);
            copy.transform.GetChild(0).name = "CopyEdited";
            Assert.AreEqual("SourceEdited", source.transform.GetChild(0).name);
            Assert.AreEqual("MoldEdited", mold.nodes[1].name);
            UIHierarchyMold revised = Capture(copy);
            Assert.AreEqual("CopyEdited", revised.nodes[1].name);
            GameObject second = Create(revised, Node("SecondTarget"));
            Assert.AreEqual("CopyEdited", second.transform.GetChild(0).name);
            Assert.AreEqual(140, second.transform.GetChild(0).GetComponent<LayoutElement>().preferredWidth);
        }
    }
}
