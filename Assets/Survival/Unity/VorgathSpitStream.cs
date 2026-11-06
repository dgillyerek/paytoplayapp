#nullable enable
using UnityEngine;
using UnityEngine.Rendering;

namespace Survival.Unity
{
    /// <summary>
    /// Vorgath spit stream. URP 17 Particles/Unlit samples the mesh UV and ignores <c>_BaseMap</c> scale and offset
    /// (<c>UnityBuildTexture2DStructNoScaleNoTexelSize</c>), so a 4x4 flipbook window never selects a cell: Stretch
    /// shows the entire sheet, sixteen yellow-white slugs. These eight strips are each one whole flame. Playback
    /// swaps the texture. It does not animate tiling. Body is alpha blend so the red/orange folds stay; a child line
    /// adds the same strip at 0.55. Socket, droop, length and FireStart/FireStop are unchanged.
    /// </summary>
    public sealed class VorgathSpitStream : MonoBehaviour
    {
        public const string PrefabPath = "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/vorgath/vfx/VORGATH_spit_fire_stream.prefab";

        /// <summary>FIX5 4x4 sheet. Not assigned. Stretching it is the pellet row.</summary>
        public const string FlipbookPath = "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/vorgath/vfx/VORGATH_fire_stream_flipbook_4x4.png";

        public const string StripFolder = "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/vorgath/vfx/strips/";

        /// <summary>The only shader. A Sprites fallback would hide the URP UV bug instead of fixing it.</summary>
        public const string ShaderName = "Universal Render Pipeline/Particles/Unlit";

        public const int StripFrames = 8;
        public const float FramesPerSecond = 24f;

        /// <summary>Additive core strength. Below this the hot pixels flood white.</summary>
        public const float CoreAdd = 0.55f;

        /// <summary>FireStart. Spit clip frame 53 at 30 fps: (53-1)/30.</summary>
        public const float FireStartSeconds = 1.733f;

        /// <summary>FireStop. Last stream frame is 75; frame 76 (2.500 s) is off. (75-1)/30.</summary>
        public const float FireStopSeconds = 2.467f;

        /// <summary>Length grows in, then alpha fades and the stream shrinks out. About 0.17 s each side.</summary>
        public const float FadeSeconds = 0.17f;

        private const float StreamLength = 2.6f;
        private const float Droop = 0.12f;
        private const float WidthMouth = 0.55f;
        private const float WidthEnd = 1.25f;

        /// <summary>Stop 0.12 m above the ground so the flame tip is not sliced off.</summary>
        private const float GroundY = 0.12f;

        private const int Segments = 10;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId2 = Shader.PropertyToID("_Color");

        private LineRenderer? _body;
        private LineRenderer? _core;
        private Light? _light;
        private GameObject? _glow;
        private Material? _bodyMat;
        private Material? _coreMat;
        private Material? _glowMat;
        private readonly Texture2D[] _strips = new Texture2D[StripFrames];
        private readonly Vector3[] _points = new Vector3[Segments + 1];
        private readonly AnimationCurve _width = new AnimationCurve(
            new Keyframe(0f, WidthMouth, 0f, WidthEnd - WidthMouth),
            new Keyframe(1f, WidthEnd, WidthEnd - WidthMouth, 0f));
        private float _flip;
        private int _shownFrame = -1;
        private bool _built;

        public static string StripPath(int frame)
        {
            return StripFolder + "VORGATH_fire_f" + frame.ToString("00") + ".png";
        }

        /// <summary>0 outside the spit window, otherwise the smoothed fade (1 while both ramps are done).</summary>
        public static float Intensity(float clipSeconds)
        {
            if (clipSeconds < FireStartSeconds || clipSeconds >= FireStopSeconds)
            {
                return 0f;
            }

            var fadeIn = Smooth01((clipSeconds - FireStartSeconds) / FadeSeconds);
            var fadeOut = Smooth01((FireStopSeconds - clipSeconds) / FadeSeconds);
            return Mathf.Min(fadeIn, fadeOut);
        }

        /// <summary>
        /// Shows the stream for <paramref name="clipSeconds"/> inside the FireStart/FireStop window.
        /// Pass a negative time to force it off (any other clip, or the frozen pose).
        /// </summary>
        public void ApplyClipTime(float clipSeconds, float deltaTime)
        {
            Ensure();
            if (transform.parent != null)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }

            var intensity = Intensity(clipSeconds);
            if (intensity <= 0.001f)
            {
                Hide();
                return;
            }

            _flip = Mathf.Repeat(_flip + (Mathf.Max(0f, deltaTime) * FramesPerSecond), StripFrames);
            Draw(intensity);
        }

        private void Awake() => Ensure();

        private void OnDestroy()
        {
            if (_bodyMat != null)
            {
                Destroy(_bodyMat);
            }

            if (_coreMat != null)
            {
                Destroy(_coreMat);
            }

            if (_glowMat != null)
            {
                Destroy(_glowMat);
            }
        }

        private void Ensure()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("Vorgath spit: " + ShaderName + " is missing. No sprite fallback — that would not match the URP UV path.");
                return;
            }

            var missing = false;
            for (var i = 0; i < StripFrames; i++)
            {
                var tex = PlayerAssets.Load<Texture2D>(StripPath(i));
                if (tex == null)
                {
                    Debug.LogError("Vorgath spit: strip missing at " + StripPath(i));
                    missing = true;
                    continue;
                }

                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                _strips[i] = tex;
            }

            if (missing)
            {
                return;
            }

            _bodyMat = MakeMaterial(shader, false);
            _coreMat = MakeMaterial(shader, true);
            _body = MakeLine("FireBody", _bodyMat);
            var coreGo = new GameObject("FireCore");
            coreGo.transform.SetParent(transform, false);
            _core = coreGo.AddComponent<LineRenderer>();
            SetupLine(_core, _coreMat);

            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.42f, 0.08f, 1f);
            _light.range = 3f;
            _light.intensity = 0f;
            _light.shadows = LightShadows.None;
            _light.enabled = false;

            _glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _glow.name = "GroundGlow";
            _glow.transform.SetParent(transform, false);
            var collider = _glow.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            _glowMat = new Material(RunnerFireArt.Glow) { name = "VorgathSpitGlow" };
            var glowRenderer = _glow.GetComponent<MeshRenderer>();
            glowRenderer.sharedMaterial = _glowMat;
            glowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;
            _glow.SetActive(false);
        }

        private LineRenderer MakeLine(string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            SetupLine(line, material);
            return line;
        }

        private void SetupLine(LineRenderer line, Material material)
        {
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = 0;
            line.widthMultiplier = 1f;
            line.widthCurve = _width;
            line.numCornerVertices = 2;
            line.numCapVertices = 0;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.sharedMaterial = material;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
        }

        private void Draw(float intensity)
        {
            if (_body == null || _core == null || _bodyMat == null || _coreMat == null)
            {
                return;
            }

            var aim = transform.parent != null ? transform.parent : transform;
            var origin = aim.position;
            var forward = aim.forward;
            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            var shown = StreamLength * intensity;
            var count = 0;
            var hit = false;
            var prev = origin;
            _points[count++] = origin;
            for (var i = 1; i <= Segments; i++)
            {
                var dist = shown * (i / (float)Segments);
                var t = dist / StreamLength;
                var p = origin + (forward * dist) + (Vector3.down * (Droop * t * t));
                if (p.y <= GroundY)
                {
                    var dy = prev.y - p.y;
                    var u = dy > 1e-5f ? (prev.y - GroundY) / dy : 1f;
                    p = Vector3.Lerp(prev, p, Mathf.Clamp01(u));
                    p.y = GroundY;
                    _points[count++] = p;
                    hit = true;
                    break;
                }

                _points[count++] = p;
                prev = p;
            }

            var reached = count >= 2 ? Vector3.Distance(origin, _points[count - 1]) : 0f;
            var tip = Mathf.Lerp(WidthMouth, WidthEnd, Mathf.Clamp01(reached / StreamLength));
            var slope = tip - WidthMouth;
            _width.MoveKey(0, new Keyframe(0f, WidthMouth, 0f, slope));
            _width.MoveKey(1, new Keyframe(1f, tip, slope, 0f));
            ApplyLine(_body, count);
            ApplyLine(_core, count);
            var frame = (int)_flip % StripFrames;
            if (frame < 0)
            {
                frame += StripFrames;
            }

            if (frame != _shownFrame)
            {
                _shownFrame = frame;
                var tex = _strips[frame];
                Assign(_bodyMat, tex);
                Assign(_coreMat, tex);
            }

            var body = new Color(1f, 1f, 1f, intensity);
            _body.startColor = body;
            _body.endColor = body;
            var core = new Color(1f, 0.92f, 0.75f, CoreAdd * intensity);
            _core.startColor = core;
            _core.endColor = core;

            if (_light != null)
            {
                _light.enabled = true;
                _light.intensity = 2.2f * intensity;
            }

            if (_glow != null && _glowMat != null)
            {
                if (!hit)
                {
                    _glow.SetActive(false);
                }
                else
                {
                    _glow.SetActive(true);
                    _glow.transform.SetPositionAndRotation(_points[count - 1], Quaternion.Euler(90f, 0f, 0f));
                    var size = Mathf.Lerp(0.28f, 0.7f, intensity);
                    var denom = Mathf.Abs(transform.lossyScale.x) > 1e-4f ? transform.lossyScale.x : 1f;
                    var local = size / denom;
                    _glow.transform.localScale = new Vector3(local, local, 1f);
                    var glow = new Color(0.85f, 0.28f, 0.05f, 0.45f * intensity);
                    _glowMat.color = glow;
                    if (_glowMat.HasProperty(ColorId))
                    {
                        _glowMat.SetColor(ColorId, glow);
                    }
                }
            }
        }

        private void ApplyLine(LineRenderer line, int count)
        {
            line.enabled = true;
            line.positionCount = count;
            line.widthCurve = _width;
            for (var i = 0; i < count; i++)
            {
                line.SetPosition(i, _points[i]);
            }
        }

        /// <summary>Whole strip on <c>_BaseMap</c>. Scale and offset stay at identity; this shader ignores them.</summary>
        private static void Assign(Material mat, Texture tex)
        {
            mat.SetTexture(BaseMapId, tex);
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", tex);
            }

            mat.mainTexture = tex;
        }

        private void Hide()
        {
            if (_body != null)
            {
                _body.enabled = false;
            }

            if (_core != null)
            {
                _core.enabled = false;
            }

            if (_light != null)
            {
                _light.enabled = false;
                _light.intensity = 0f;
            }

            if (_glow != null)
            {
                _glow.SetActive(false);
            }
        }

        private static Material MakeMaterial(Shader shader, bool additive)
        {
            var mat = new Material(shader) { name = additive ? "VorgathSpitCore" : "VorgathSpitBody" };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetFloat("_BlendOp", (float)BlendOp.Add);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_FlipbookBlending", 0f);
            mat.DisableKeyword("_FLIPBOOKBLENDING_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHAMODULATE_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = additive ? 3001 : 3000;
            var tint = additive ? new Color(1f, 0.92f, 0.75f, CoreAdd) : Color.white;
            mat.SetColor(ColorId, tint);
            if (mat.HasProperty(ColorId2))
            {
                mat.SetColor(ColorId2, tint);
            }

            mat.color = tint;
            return mat;
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - (2f * t));
        }
    }
}
