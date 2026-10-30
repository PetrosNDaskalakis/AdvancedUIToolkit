using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedUIAuthoring.MoldTests
{
    /// <summary>Test-only component exercising value serialization and hierarchy-local references.</summary>
    public sealed class MoldTestProbe : MonoBehaviour
    {
        /// <summary>Serializable nested array element.</summary>
        [Serializable]
        public sealed class Row
        {
            /// <summary>Value inside a nested array element.</summary>
            public int number;
            /// <summary>Internal or external object reference.</summary>
            public UnityEngine.Object link;
        }
        /// <summary>Signed scalar.</summary>
        public int number;
        /// <summary>Unsigned value above the signed range.</summary>
        public ulong unsignedNumber;
        /// <summary>Double precision scalar.</summary>
        public double precise;
        /// <summary>Ordinary value that resembles JSON and must remain literal text.</summary>
        public string text;
        /// <summary>Internal GameObject reference.</summary>
        public GameObject objectLink;
        /// <summary>Internal component reference, including duplicate component slots.</summary>
        public Component componentLink;
        /// <summary>External asset/reference that must be cleared.</summary>
        public UnityEngine.Object external;
        /// <summary>Array shape and nested references.</summary>
        public List<Row> rows = new List<Row>();
        /// <summary>Nontrivial Unity value type.</summary>
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        /// <summary>Another Unity value type handled without child iteration.</summary>
        public Gradient gradient = new Gradient();
    }
}
