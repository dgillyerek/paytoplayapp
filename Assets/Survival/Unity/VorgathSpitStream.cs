#nullable enable
using UnityEngine;
using UnityEngine.Rendering;

namespace Survival.Unity
{
    /// <summary>
    /// Vorgath's spit fire stream (design/survival-theme-a-fantasy/enemies/anim/vorgath/vfx/DEV_NOTE_VORGATH_spit_fire.md).
    /// A single stretched strip along the parent socket's +Z, about 2.6 m, with a slight world-down droop, clipped at the
    /// ground. The 4x4 flipbook loops while the stream is up. One strip stays on for the whole window so the head sweep
    /// carries it; it is not a burst of particles. Drop the prefab under <c>VORGATH_mouth_socket</c> and call
    /// <see cref="ApplyClipTime"/> with the spit clip time. New Characters Preview samples clips without an Animator, so
    /// animation events would not fire there; the clip-time window is the FireStart (1.733 s) / FireStop (2.467 s) hook.
    /// </summary>
    public sealed class VorgathSpitStream : MonoBehaviour
    {
        public const string PrefabPath = "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/vorgath/vfx/VORGATH_spit_fire_stream.prefab";
        public const string FlipbookPath = "Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/vorgath/vfx/VORGATH_fire_stream_flipbook_4x4.png";

        /// <summary>FireStart. Spit clip frame 53 at 30 fps: (53-1)/30.</summary>
        public const float FireStartSeconds = 1.733f;

        /// <summary>FireStop. Last stream frame is 75; frame 76 (2.500 s) is off. (75-1)/30.</summary>
        public const float FireStopSeconds = 2.467f;

        /// <summary>Length grows in, then alpha fades and the stream shrinks out. About 0.17 s each side.</summary>
        public const float FadeSeconds = 0.17f;

        private const float StreamLength = 2.6f;
        private const float Droop = 0.12f;
        private const float WidthMouth = 0.18f;
        private const float WidthEnd = 0.9f;
        private const float GroundY = 0f;
        private const int Segments = 10;
        private const int Frames = 16;
        private const float FramesPerSecond = 24f;
        private const float Cell = 0.25f;

        private static readonly Vector2 CellScale = new Vector2(Cell, Cell);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private LineRenderer? _line;
        private Light? _light;
        private GameObject? _glow;
        private Material? _streamMat;
        private Material? _glowMat;
        private Texture2D? _flipbook;
        private readonly Vector3[] _points = new Vector3[Segments + 1];
        private readonly AnimationCurve _width = new AnimationCurve(
            new Keyframe(0f, WidthMouth, 0f, WidthEnd - WidthMouth),
            new Keyframe(1f, WidthEnd, WidthEnd - WidthMouth, 0f));
        private float _flip;
        private bool _built;

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
                // Stay on the socket. SampleAnimation must not walk the light off the mouth.
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

            _flip = Mathf.Repeat(_flip + (Mathf.Max(0f, deltaTime) * FramesPerSecond), Frames);
            Draw(intensity);
        }

        private void Awake() => Ensure();

        private void OnDestroy()
        {
            if (_streamMat != null)
            {
                Destroy(_streamMat);
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
            var loaded = PlayerAssets.Load<Texture2D>(FlipbookPath);
            if (loaded == null)
            {
                Debug.LogError("Vorgath spit: flipbook missing at " + FlipbookPath);
                loaded = Texture2D.whiteTexture;
            }
            else
            {
                loaded.wrapMode = TextureWrapMode.Clamp;
                loaded.filterMode = FilterMode.Bilinear;
            }

            _flipbook = loaded;
            _streamMat = FireMaterial(loaded);
            if (_streamMat == null)
            {
                return;
            }

            _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.loop = false;
            _line.positionCount = 0;
            _line.widthMultiplier = 1f;
            _line.widthCurve = _width;
            _line.numCornerVertices = 2;
            _line.numCapVertices = 0;
            _line.alignment = LineAlignment.View;
            _line.textureMode = LineTextureMode.Stretch;
            _line.sharedMaterial = _streamMat;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.enabled = false;

            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.416f, 0.051f, 1f);
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

        private void Draw(float intensity)
        {
            if (_line == null || _streamMat == null)
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
                    p.y = GroundY + 0.02f;
                    _points[count++] = p;
                    hit = true;
                    break;
                }

                _points[count++] = p;
                prev = p;
            }

            _line.enabled = true;
            _line.positionCount = count;
            for (var i = 0; i < count; i++)
            {
                _line.SetPosition(i, _points[i]);
            }

            var reached = count >= 2 ? Vector3.Distance(origin, _points[count - 1]) : 0f;
            var tip = Mathf.Lerp(WidthMouth, WidthEnd, Mathf.Clamp01(reached / StreamLength));
            var slope = tip - WidthMouth;
            _width.MoveKey(0, new Keyframe(0f, WidthMouth, 0f, slope));
            _width.MoveKey(1, new Keyframe(1f, tip, slope, 0f));
            _line.widthCurve = _width;
            var color = new Color(1f, 1f, 1f, intensity);
            _line.startColor = color;
            _line.endColor = color;
            SetFrame();

            if (_light != null)
            {
                _light.enabled = true;
                _light.intensity = 2.5f * intensity;
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
                    var size = Mathf.Lerp(0.35f, 1.15f, intensity);
                    var denom = Mathf.Abs(transform.lossyScale.x) > 1e-4f ? transform.lossyScale.x : 1f;
                    var local = size / denom;
                    _glow.transform.localScale = new Vector3(local, local, 1f);
                    var glow = new Color(1f, 0.5f, 0.12f, 0.85f * intensity);
                    _glowMat.color = glow;
                    if (_glowMat.HasProperty(BaseColorId))
                    {
                        _glowMat.SetColor(BaseColorId, glow);
                    }
                }
            }
        }

        private void Hide()
        {
            if (_line != null)
            {
                _line.enabled = false;
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

        private void SetFrame()
        {
            if (_streamMat == null || _flipbook == Texture2D.whiteTexture)
            {
                return;
            }

            var frame = ((int)_flip) % Frames;
            if (frame < 0)
            {
                frame += Frames;
            }

            var col = frame % 4;
            var rowFromTop = frame / 4;
            var offset = new Vector2(col * Cell, (3 - rowFromTop) * Cell);
            SetSheet("_BaseMap", offset);
            SetSheet("_MainTex", offset);
        }

        private void SetSheet(string property, Vector2 offset)
        {
            if (_streamMat != null && _streamMat.HasProperty(property))
            {
                _streamMat.SetTextureScale(property, CellScale);
                _streamMat.SetTextureOffset(property, offset);
            }
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - (2f * t));
        }

        /// <summary>Same additive Particles/Unlit setup as the other Theme A attack fire, with this flipbook on it.</summary>
        private static Material? FireMaterial(Texture tex)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                Debug.LogError("Vorgath spit: no unlit shader for the fire stream.");
                return null;
            }

            var mat = new Material(shader) { name = "VorgathSpitFire" };
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
            }

            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", tex);
            }

            mat.mainTexture = tex;
            if (mat.HasProperty(BaseColorId))
            {
                mat.SetColor(BaseColorId, Color.white);
            }

            mat.color = Color.white;
            SetFloat(mat, "_Surface", 1f);
            SetFloat(mat, "_Blend", 2f);
            SetFloat(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(mat, "_DstBlend", (float)BlendMode.One);
            SetFloat(mat, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloat(mat, "_DstBlendAlpha", (float)BlendMode.One);
            SetFloat(mat, "_ZWrite", 0f);
            SetFloat(mat, "_Cull", (float)CullMode.Off);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3003;
            return mat;
        }

        private static void SetFloat(Material mat, string name, float value)
        {
            if (mat.HasProperty(name))
            {
                mat.SetFloat(name, value);
            }
        }
    }
}
