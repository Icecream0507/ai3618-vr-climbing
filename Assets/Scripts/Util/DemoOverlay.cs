using UnityEngine;
using UnityEngine.UI;
using VRClimb.Climbing;
using VRClimb.Gameplay;

namespace VRClimb.Util
{
    /// <summary>
    /// Builds and updates a lightweight on-screen overlay for the recorded demo: a title, the live
    /// timer + fall count, a balance meter (blue, turning red while slipping), and a big caption line
    /// driven by <see cref="SimulatedClimber.Caption"/>. Built from code with legacy UI + the built-in
    /// font so it needs no TMP font asset or prefab. The canvas is World-Space and parented to the
    /// spectator camera so it is captured by the frame recorder (which renders that camera directly).
    ///
    /// Demo-only; does not affect gameplay. The team's real in-headset HUD is <c>VRClimb.UI.GameHUD</c>.
    /// </summary>
    public class DemoOverlay : MonoBehaviour
    {
        public Camera spectator;
        public BalanceSystem balance;
        public SimulatedClimber sim;

        Text _timer, _caption, _title;
        Image _balanceFill;
        RectTransform _balanceFillRT;
        float _balanceMaxWidth;

        static Font BuiltinFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        void Start() { Build(); }

        void Build()
        {
            var font = BuiltinFont();

            var canvasGo = new GameObject("DemoOverlayCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = spectator;

            // Park the canvas in front of the camera and scale it to exactly fit the frustum at that
            // distance (with a small margin) so edge elements aren't clipped. 720 canvas units map to
            // the visible height = 2*d*tan(fov/2); a 0.92 factor keeps a safe margin.
            const float d = 1f, fitMargin = 0.92f;
            float visH = 2f * d * Mathf.Tan(spectator.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var crt = canvasGo.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(1280f, 720f);
            canvasGo.transform.SetParent(spectator.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, d);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * (visH / 720f * fitMargin);

            _title   = MakeText(crt, font, new Vector2(0f, 1f), new Vector2(20f, -16f),
                                new Vector2(900f, 40f), 26, TextAnchor.UpperLeft,
                                "Summit VR — Balance & Footwork (simulated playthrough)");
            _title.fontStyle = FontStyle.Bold;

            _timer   = MakeText(crt, font, new Vector2(1f, 1f), new Vector2(-28f, -18f),
                                new Vector2(420f, 40f), 26, TextAnchor.UpperRight, "00:00");

            _caption = MakeText(crt, font, new Vector2(0.5f, 0f), new Vector2(0f, 96f),
                                new Vector2(1100f, 80f), 30, TextAnchor.LowerCenter, "");
            _caption.fontStyle = FontStyle.Bold;

            // Balance meter (label + track + fill) bottom-left.
            MakeText(crt, font, new Vector2(0f, 0f), new Vector2(24f, 60f),
                     new Vector2(200f, 26f), 20, TextAnchor.LowerLeft, "BALANCE");
            var track = MakePanel(crt, new Vector2(0f, 0f), new Vector2(24f, 30f),
                                  new Vector2(320f, 24f), new Color(0f, 0f, 0f, 0.45f));
            _balanceMaxWidth = 312f;
            _balanceFill = MakePanel(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(4f, 0f),
                                     new Vector2(_balanceMaxWidth, 16f), new Color(0.3f, 0.7f, 1f, 0.95f));
            _balanceFillRT = _balanceFill.rectTransform;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && _timer != null)
            {
                int m = Mathf.FloorToInt(gm.ElapsedTime / 60f), s = Mathf.FloorToInt(gm.ElapsedTime % 60f);
                _timer.text = $"{m:00}:{s:00}   falls {gm.FallCount}";
            }
            if (sim != null && _caption != null) _caption.text = sim.Caption;
            if (balance != null && _balanceFillRT != null)
            {
                float w = Mathf.Clamp01(balance.Normalized) * _balanceMaxWidth;
                _balanceFillRT.sizeDelta = new Vector2(Mathf.Max(2f, w), _balanceFillRT.sizeDelta.y);
                _balanceFill.color = balance.IsSlipping
                    ? new Color(0.95f, 0.25f, 0.25f, 0.97f)
                    : new Color(0.3f, 0.7f, 1f, 0.95f);
            }
        }

        // ---- tiny UI builders ----

        static Text MakeText(RectTransform parent, Font font, Vector2 anchor, Vector2 anchoredPos,
                             Vector2 size, int fontSize, TextAnchor align, string text)
        {
            var go = new GameObject("Text", typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = fontSize; t.alignment = align; t.text = text;
            t.color = Color.white; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            // cheap drop shadow for legibility over the wall
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.8f); sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        static Image MakePanel(RectTransform parent, Vector2 anchor, Vector2 anchoredPos,
                              Vector2 size, Color color)
        {
            var go = new GameObject("Panel", typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0f, anchor.y);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }
    }
}
