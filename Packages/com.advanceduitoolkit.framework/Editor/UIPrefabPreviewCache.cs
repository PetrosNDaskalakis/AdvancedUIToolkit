using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Editor-only offscreen thumbnails for the normal appearance of RectTransform prefabs. Unlike Unity's AssetPreview,
    /// renders the actual uGUI Graphics on a temporary World Space Canvas in an isolated
    /// PreviewRenderUtility scene. Never writes to or instantiates objects in the user's scene.
    ///
    /// Captures are queued outside OnGUI (one per editor update) and kept only in memory.
    /// The window supplies an IMGUI approximation if capture is unsupported.
    /// </summary>
    [InitializeOnLoad]
    internal static class UIPrefabPreviewCache
    {
        /// <summary>Defines the preview width constant.</summary>
        private const int PreviewWidth = 1024;
        /// <summary>Defines the preview height constant.</summary>
        private const int PreviewHeight = 512;
        /// <summary>Defines the max pending constant.</summary>
        private const int MaxPending = 48;
        /// <summary>Defines the capture interval seconds constant.</summary>
        private const double CaptureIntervalSeconds = 0.12;

        /// <summary>Stores the cache entry reference or configuration value.</summary>
        private sealed class CacheEntry
        {
            /// <summary>Stores the texture reference or configuration value.</summary>
            internal Texture2D Texture;
            /// <summary>Stores the finished reference or configuration value.</summary>
            internal bool Finished;
        }

        /// <summary>Defines the preview request struct.</summary>
        private struct PreviewRequest
        {
            /// <summary>Stores the prefab reference or configuration value.</summary>
            internal GameObject Prefab;
            /// <summary>Stores the key reference or configuration value.</summary>
            internal string Key;
        }

        /// <summary>Stores the cache reference or configuration value.</summary>
        private static readonly Dictionary<string, CacheEntry> Cache =
            new Dictionary<string, CacheEntry>(StringComparer.Ordinal);
        /// <summary>Stores the requests reference or configuration value.</summary>
        private static readonly Queue<PreviewRequest> Requests = new Queue<PreviewRequest>();
        /// <summary>Stores the next capture time reference or configuration value.</summary>
        private static double nextCaptureTime;

        /// <summary>Notifies listeners when updated occurs.</summary>
        internal static event Action Updated;

        /// <summary>Performs the ui prefab preview cache operation.</summary>
        static UIPrefabPreviewCache()
        {
            EditorApplication.update += Update;
            EditorApplication.projectChanged += Clear;
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.quitting += Clear;
        }

        /// <summary>Gets value.</summary>
        internal static Texture Get(GameObject prefab)
        {
            if (prefab == null) return null;
            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path) || prefab.GetComponent<RectTransform>() == null)
                return null;

            string key = path;
            if (!Cache.TryGetValue(key, out CacheEntry entry))
            {
                // Do not queue hundreds of hidden gallery items. Retry on a later repaint.
                if (Requests.Count < MaxPending)
                {
                    Cache[key] = new CacheEntry();
                    Requests.Enqueue(new PreviewRequest { Prefab = prefab, Key = key });
                }
                return null;
            }
            return entry.Finished ? entry.Texture : null;
        }

        /// <summary>Performs the clear operation.</summary>
        internal static void Clear()
        {
            Requests.Clear();
            foreach (CacheEntry entry in Cache.Values)
                if (entry.Texture != null) UnityEngine.Object.DestroyImmediate(entry.Texture);
            Cache.Clear();
            Updated?.Invoke();
        }

        /// <summary>Performs the update operation.</summary>
        private static void Update()
        {
            if (Requests.Count == 0 || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.timeSinceStartup < nextCaptureTime)
                return;

            PreviewRequest request = Requests.Dequeue();
            if (!Cache.TryGetValue(request.Key, out CacheEntry entry)) return;

            try
            {
                if (request.Prefab != null)
                    entry.Texture = Render(request.Prefab);
            }
            catch (Exception exception)
            {
                // Preview generation should never prevent building or using a component.
                Debug.LogWarning("Advanced UI Toolkit: Could not render preview for '" +
                    (request.Prefab != null ? request.Prefab.name : "missing prefab") +
                    "'. A lightweight graphic preview will be shown instead. " + exception.Message);
            }
            finally
            {
                entry.Finished = true;
                nextCaptureTime = EditorApplication.timeSinceStartup + CaptureIntervalSeconds;
                Updated?.Invoke();
            }
        }

        /// <summary>Performs the render operation.</summary>
        private static Texture2D Render(GameObject prefab)
        {
            if (prefab.GetComponent<RectTransform>() == null) return null;

            PreviewRenderUtility preview = null;
            try
            {
                preview = new PreviewRenderUtility();
                Camera camera = preview.camera;
                camera.cameraType = CameraType.Game; // uGUI is not always rendered by Preview cameras
                camera.orthographic = true;
                camera.transform.position = new Vector3(0f, 0f, -50f);
                camera.transform.rotation = Quaternion.identity;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 200f;
                camera.cullingMask = ~0;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.13f, 0.15f, 0.17f, 1f);

                GameObject canvasObject = new GameObject("Advanced UI Toolkit Preview Canvas",
                    typeof(RectTransform), typeof(Canvas));
                preview.AddSingleGO(canvasObject);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                canvas.pixelPerfect = false;
                RectTransform canvasRect = (RectTransform)canvasObject.transform;
                canvasRect.sizeDelta = new Vector2(1600f, 900f);
                canvasRect.localScale = Vector3.one;

                // The instance belongs to PreviewRenderUtility's isolated preview scene.
                GameObject instance = preview.InstantiatePrefabInScene(prefab);
                if (instance == null) return null;
                instance.transform.SetParent(canvasRect, false);
                instance.SetActive(true);

                RectTransform uiRect = instance.GetComponent<RectTransform>();
                uiRect.anchorMin = new Vector2(.5f, .5f);
                uiRect.anchorMax = new Vector2(.5f, .5f);
                uiRect.pivot = new Vector2(.5f, .5f);
                uiRect.anchoredPosition = Vector2.zero;
                uiRect.localPosition = Vector3.zero;
                uiRect.localRotation = Quaternion.identity;
                uiRect.localScale = Vector3.one;

                // A prefab may carry a Canvas configured for Overlay mode. Override
                // the preview *instance* only, so the offscreen camera can render it.
                foreach (Canvas nested in instance.GetComponentsInChildren<Canvas>(true))
                {
                    nested.renderMode = RenderMode.WorldSpace;
                    nested.worldCamera = camera;
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(uiRect);
                Canvas.ForceUpdateCanvases();

                float width = Mathf.Max(80f, Mathf.Abs(uiRect.rect.width));
                float height = Mathf.Max(32f, Mathf.Abs(uiRect.rect.height));
                float aspect = (float)PreviewWidth / PreviewHeight;
                camera.orthographicSize = Mathf.Max(height, width / aspect) * 0.62f;

                preview.BeginStaticPreview(new Rect(0f, 0f, PreviewWidth, PreviewHeight));
                preview.Render(true);
                Texture2D result = preview.EndStaticPreview();

                if (result != null)
                {
                    result.filterMode = FilterMode.Bilinear;
                    result.wrapMode = TextureWrapMode.Clamp;
                    result.Apply(false, false);
                }

                // An entirely blank capture can occur with unsupported SRP previews.
                // In that case return null and let Unity's thumbnail be the fallback.
                if (result != null && IsBlank(result))
                {
                    UnityEngine.Object.DestroyImmediate(result);
                    return null;
                }
                return result;
            }
            finally
            {
                if (preview != null) preview.Cleanup();
            }
        }

        /// <summary>Determines whether blank.</summary>
        private static bool IsBlank(Texture2D image)
        {
            // Sample only a small grid; do not allocate a full pixel array per preview.
            Color baseColor = image.GetPixel(0, 0);
            for (int x = 15; x < image.width; x += 35)
                for (int y = 15; y < image.height; y += 29)
                {
                    Color pixel = image.GetPixel(x, y);
                    if (Mathf.Abs(pixel.r - baseColor.r) +
                        Mathf.Abs(pixel.g - baseColor.g) +
                        Mathf.Abs(pixel.b - baseColor.b) > 0.055f)
                        return false;
                }
            return true;
        }

    }
}
