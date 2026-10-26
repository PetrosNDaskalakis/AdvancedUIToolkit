using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using AdvancedUIToolkit.Editor;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Tests
{
    /// <summary>Validates read-only diffs and custom generated XML summaries.</summary>
    public sealed class AdvancedUIToolkitGenerationPreviewTests
    {
        /// <summary>Confirms unchanged generated code produces no diff.</summary>
        [Test]
        public void DiffReportsUnchangedSource()
        {
            Assert.AreEqual("No source changes detected.",
                AdvancedUIToolkitGenerationDiff.Compare("one\ntwo\n", "one\ntwo\n"));
        }

        /// <summary>Ensures additions and removals appear with readable line prefixes.</summary>
        [Test]
        public void DiffShowsAddedAndRemovedLines()
        {
            string diff = AdvancedUIToolkitGenerationDiff.Compare("a\nb\nc\n", "a\nnew\nc\n");
            StringAssert.Contains("- b", diff);
            StringAssert.Contains("+ new", diff);
            StringAssert.Contains("  c", diff);
        }

        /// <summary>Ensures comparing against a missing generated file doesn't fail.</summary>
        [Test]
        public void DiffShowsNewFile()
        {
            string diff = AdvancedUIToolkitGenerationDiff.Compare(null, "using AdvancedUIToolkit;\n");
            StringAssert.Contains("New file", diff);
            StringAssert.Contains("+ using AdvancedUIToolkit;", diff);
        }

        /// <summary>Checks the optional example directive controls property documentation.</summary>
        [Test]
        public void TemplateCustomizesDocumentationAndEscapesXml()
        {
            string path = "Assets/__AdvancedUIToolkit096Template_" + Guid.NewGuid().ToString("N") + ".cs";
            try
            {
                File.WriteAllText(path,
                    "using AdvancedUIToolkit;\nnamespace Template.Example\n{\n" +
                    "    /// <summary>Example class.</summary>\n" +
                    "    public sealed class ExampleScript : IUIBindings\n    {\n" +
                    "        // <ADVANCEDUITOOLKIT:PROPERTY_DOC>Bound {name} from {hierarchy} ({type}).\n" +
                    "        // <ADVANCEDUITOOLKIT:PROPERTIES>\n" +
                    "        /// <summary>Initialize references.</summary>\n" +
                    "        /// <param name=\"root\">Binding root.</param>\n" +
                    "        public void Initialize(UIBindingRoot root)\n        {\n" +
                    "            // <ADVANCEDUITOOLKIT:ASSIGNMENTS>\n        }\n    }\n}\n");

                Assert.IsTrue(AdvancedUIToolkitCodeTemplate.Validate(path, out string error), error);
                string generated = AdvancedUIToolkitCodeTemplate.Generate("PanelBindings", "Game.Generated",
                    new List<UIBindingRoot.Entry>
                    {
                        new UIBindingRoot.Entry
                        {
                            Key = "Arm", HierarchyKey = "Arm & <Status>",
                            ComponentType = typeof(Button).FullName
                        }
                    }, path);

                StringAssert.Contains("Bound Arm from Arm &amp; &lt;Status&gt; " +
                    "(UnityEngine.UI.Button).", generated);
                Assert.IsFalse(generated.Contains("// <ADVANCEDUITOOLKIT:PROPERTY_DOC>"));
                StringAssert.Contains("public UnityEngine.UI.Button Arm", generated);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        /// <summary>Prevents accidental duplicate documentation directives in a template.</summary>
        [Test]
        public void DuplicatePropertyDocDirectiveIsInvalid()
        {
            string path = "Assets/__AdvancedUIToolkit096Duplicate_" + Guid.NewGuid().ToString("N") + ".cs";
            try
            {
                File.WriteAllText(path,
                    "using AdvancedUIToolkit;\nnamespace Template.Example\n{\n" +
                    "    /// <summary>Example class.</summary>\n" +
                    "    public sealed class ExampleScript : IUIBindings\n    {\n" +
                    "        // <ADVANCEDUITOOLKIT:PROPERTY_DOC>First {name}\n" +
                    "        // <ADVANCEDUITOOLKIT:PROPERTY_DOC>Second {name}\n" +
                    "        // <ADVANCEDUITOOLKIT:PROPERTIES>\n" +
                    "        /// <summary>Initialize references.</summary>\n" +
                    "        public void Initialize(UIBindingRoot root)\n        {\n" +
                    "            // <ADVANCEDUITOOLKIT:ASSIGNMENTS>\n        }\n    }\n}\n");
                Assert.IsFalse(AdvancedUIToolkitCodeTemplate.Validate(path, out string error));
                StringAssert.Contains("PROPERTY_DOC", error);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
