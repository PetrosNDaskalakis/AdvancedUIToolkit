using System;
using UnityEngine;

namespace AdvancedUIAuthoring.MoldTests
{
    /// <summary>Test-only unsupported managed-reference field used to verify explicit capture failure.</summary>
    public sealed class MoldUnsupportedProbe : MonoBehaviour
    {
        /// <summary>Managed payload fixture.</summary>
        [Serializable]
        public sealed class Payload
        {
            /// <summary>Sample payload value.</summary>
            public int number = 7;
        }
        /// <summary>Managed references must not be silently dropped.</summary>
        [SerializeReference] public Payload payload = new Payload();
    }
}
