using System;
using Grove.Domain.Layout;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Grove.Unity
{
    /// <summary>
    /// Shared Play Mode helpers. World sprites come from <see cref="GroveArt"/> (DES-001/002).
    /// UI white is a generated 8×8 — not the old Resources/Grove/Art programmer pack.
    /// </summary>
    internal static class GroveVisuals
    {
        private static Font? _font;
        private static Shader? _spriteShader;

        /// <summary>Warm maple fill if a pixel of camera clear ever shows — never the old grey/green grid.</summary>
        public static readonly Color GardenClear = new Color(0.89f, 0.84f, 0.72f, 1f);

        /// <summary>DES-003 dock cream — solid plate so the 5-slot PNG is not stretched over orders.</summary>
        public static readonly Color DockCream = new Color(0.976f, 0.953f, 0.886f, 1f);

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                            ?? Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans", "DejaVu Sans" }, 16);
                }

                return _font;
            }
        }

        public static Sprite WhiteSprite => GroveArt.WhiteSprite();

        public static Material MakeMaterial(Color color, Texture? texture = null)
        {
            var shader = SpriteShader();
            var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            ApplyColor(mat, color);
            if (texture != null)
            {
                mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", texture);
                }
            }

            return mat;
        }

        private static Shader? SpriteShader()
        {
            if (_spriteShader != null)
            {
                return _spriteShader;
            }

            _spriteShader = Shader.Find("Sprites/Default")
                            ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Unlit/Transparent")
                            ?? Shader.Find("Unlit/Color");
            return _spriteShader;
        }

        public static void ApplyColor(Material mat, Color color)
        {
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
        }

        public static GameObject SpriteObject(
            string name,
            Transform parent,
            Vector3 worldPos,
            float worldSize,
            Color color,
            Sprite sprite,
            int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.sharedMaterial = MakeMaterial(color, sprite != null ? sprite.texture : null);
            FitSprite(go.transform, sprite, worldSize);
            return go;
        }

        public static void FitSprite(Transform target, Sprite? sprite, float worldSize)
        {
            if (sprite == null)
            {
                return;
            }

            var size = sprite.bounds.size;
            if (size.x > 0.0001f && size.y > 0.0001f)
            {
                var max = Mathf.Max(size.x, size.y);
                target.localScale = new Vector3(worldSize / max, worldSize / max, 1f);
            }
        }

        /// <summary>Non-uniform scale so the sprite fills a world rectangle (BoardSurface under the 7×5).</summary>
        public static void StretchToRect(Transform target, Sprite? sprite, Vector3 center, float worldW, float worldH)
        {
            if (target == null || sprite == null)
            {
                return;
            }

            target.position = new Vector3(center.x, center.y, target.position.z);
            var size = sprite.bounds.size;
            target.localScale = new Vector3(
                worldW / Mathf.Max(0.001f, size.x),
                worldH / Mathf.Max(0.001f, size.y),
                1f);
        }

        /// <summary>Uniform scale to cover a world rectangle (backdrop over the camera frustum).</summary>
        public static void CoverRect(Transform target, Sprite? sprite, Vector3 center, float worldW, float worldH)
        {
            if (target == null || sprite == null)
            {
                return;
            }

            target.position = new Vector3(center.x, center.y, target.position.z);
            var size = sprite.bounds.size;
            var s = Mathf.Max(
                worldW / Mathf.Max(0.001f, size.x),
                worldH / Mathf.Max(0.001f, size.y));
            target.localScale = new Vector3(s, s, 1f);
        }

        public static void FrameBoard(BoardView view)
        {
            EnsurePlayCamera();
            var cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam == null || view == null)
            {
                return;
            }

            view.GetWorldBounds(out var minX, out var minY, out var maxX, out var maxY);
            var aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : PlayLayout.PortraitAspect;
            var frame = PlayLayout.FitBoard(minX, minY, maxX, maxY, aspect);
            cam.transform.position = new Vector3(frame.CenterX, frame.CenterY, -10f);
            cam.orthographicSize = frame.OrthographicSize;
        }

        public static TextMesh Label(string name, Transform parent, Vector3 localPos, string text, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.text = text;
            tm.fontSize = fontSize;
            tm.characterSize = 0.04f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.richText = false;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.sortingOrder = 20;
                if (Font != null && Font.material != null)
                {
                    meshRenderer.sharedMaterial = Font.material;
                }
            }

            return tm;
        }

        public static Color PieceColor(string id)
        {
            if (id.StartsWith("wildflower"))
            {
                var tier = ParseTier(id);
                return Color.Lerp(new Color(0.95f, 0.55f, 0.75f), new Color(0.75f, 0.15f, 0.45f), (tier - 1) / 7f);
            }

            if (id.StartsWith("herb"))
            {
                return new Color(0.35f, 0.75f, 0.35f);
            }

            if (id.StartsWith("tool"))
            {
                return new Color(0.35f, 0.55f, 0.9f);
            }

            if (id.StartsWith("twig"))
            {
                return new Color(0.62f, 0.42f, 0.22f);
            }

            return id switch
            {
                "pebble" => new Color(0.55f, 0.55f, 0.58f),
                "sprout" => new Color(0.45f, 0.72f, 0.4f),
                "sapling" => new Color(0.35f, 0.6f, 0.32f),
                "tree" => new Color(0.2f, 0.45f, 0.22f),
                "grove" => new Color(0.12f, 0.35f, 0.18f),
                "coin" => new Color(0.95f, 0.8f, 0.25f),
                "puff" => new Color(0.9f, 0.92f, 1f),
                _ => new Color(0.7f, 0.7f, 0.75f)
            };
        }

        public static string ShortLabel(string id, int count)
        {
            var label = id switch
            {
                "pebble" => "Pebble",
                "sprout" => "Sprout",
                "sapling" => "Sapling",
                "tree" => "Tree",
                "grove" => "Grove",
                "coin" => "Coin",
                "puff" => "Puff",
                "wildflower_t2" => "Sprout",
                "wildflower_t3" => "Bud",
                "wildflower_t5" => "Bouquet",
                "herb_t2" => "Herb Pot",
                "tool_t1" => "Twig",
                "tool_t2" => "Stick",
                _ when id.StartsWith("wildflower_t") => "WF T" + ParseTier(id),
                _ when id.StartsWith("herb_t") => "Herb T" + ParseTier(id),
                _ when id.StartsWith("tool_t") => "Tool T" + ParseTier(id),
                _ => id
            };
            return count > 1 ? $"{label} ×{count}" : label;
        }

        private static int ParseTier(string id)
        {
            var idx = id.LastIndexOf('t');
            if (idx >= 0 && idx + 1 < id.Length && int.TryParse(id.Substring(idx + 1), out var tier))
            {
                return Mathf.Max(1, tier);
            }

            return 1;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            EnsurePlayCamera();
        }

        public static void EnsurePlayCamera()
        {
            var cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            if (!cam.CompareTag("MainCamera"))
            {
                cam.tag = "MainCamera";
            }

            cam.enabled = true;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GardenClear;
            cam.cullingMask = ~0;
            cam.depth = -1;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            if (cam.orthographicSize < 5.2f)
            {
                cam.orthographicSize = 5.5f;
            }

            var pos = cam.transform.position;
            if (Mathf.Abs(pos.z) < 0.01f)
            {
                cam.transform.position = new Vector3(pos.x, pos.y, -10f);
            }

            EnsureUrpCameraData(cam);
        }

        public static void ShowDiagnostic(string message)
        {
            EnsureEventSystem();
            var canvasGo = new GameObject("GroveDiagnosticCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            var panel = UiImage(canvasGo.transform, "Panel", new Color(0.05f, 0.08f, 0.06f, 0.92f));
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.06f, 0.35f);
            panelRect.anchorMax = new Vector2(0.94f, 0.65f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var text = UiText(
                panel.transform,
                "Message",
                message,
                28,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.92f, 0.55f));
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 24f);
            textRect.offsetMax = new Vector2(-24f, -24f);
        }

        private static void EnsureUrpCameraData(Camera cam)
        {
            const string dataTypeName = "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData";
            const string extTypeName = "UnityEngine.Rendering.Universal.CameraExtensions";

            var dataType = FindLoadedType(dataTypeName);
            if (dataType != null && cam.GetComponent(dataType) != null)
            {
                return;
            }

            var extType = FindLoadedType(extTypeName);
            var getter = extType != null
                ? extType.GetMethod("GetUniversalAdditionalCameraData", new[] { typeof(Camera) })
                : null;
            if (getter != null)
            {
                getter.Invoke(null, new object[] { cam });
                return;
            }

            if (dataType != null)
            {
                cam.gameObject.AddComponent(dataType);
            }
        }

        private static Type? FindLoadedType(string fullName)
        {
            var qualified = Type.GetType(fullName + ", Unity.RenderPipelines.Universal.Runtime");
            if (qualified != null)
            {
                return qualified;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public static Text UiText(Transform parent, string name, string content, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            if (Font != null)
            {
                text.font = Font;
            }
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Image UiImage(
            Transform parent,
            string name,
            Color color,
            Sprite? sprite = null,
            bool preserveAspect = false,
            bool raycastTarget = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : WhiteSprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.preserveAspect = preserveAspect && sprite != null;
            image.raycastTarget = raycastTarget;
            return image;
        }

        public static Button UiButton(Transform parent, string name, string label, Color bg, Vector2 size, Sprite? sprite = null)
        {
            var image = UiImage(parent, name, bg, sprite, preserveAspect: sprite != null, raycastTarget: true);
            var rect = image.rectTransform;
            rect.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.2f);
            button.colors = colors;
            if (!string.IsNullOrEmpty(label))
            {
                var text = UiText(image.transform, "Label", label, 26, TextAnchor.MiddleCenter, Color.white);
                var textRect = text.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                text.raycastTarget = false;
            }

            return button;
        }
    }
}
