using UnityEngine;
using VRClimb.Climbing;
using VRClimb.Gameplay;

namespace VRClimb.Util
{
    /// <summary>
    /// Minimal first-person HUD for the VR-controller scene (<c>VR.unity</c>): a balance bar, the
    /// climb timer / fall count, summit + fall banners, and a one-line reminder of the XR Device
    /// Simulator controls. Pure read-only overlay — it never writes gameplay state, so it can sit on
    /// the XR Origin alongside the real climbing stack without affecting it.
    ///
    /// Labels are English: IMGUI's default font has no CJK glyphs.
    /// </summary>
    public class VRHud : MonoBehaviour
    {
        [Tooltip("Balance to read for the bar. Auto-found on this object if left empty.")]
        public BalanceSystem balance;

        [Tooltip("One-line control hint shown along the bottom (XR Device Simulator keys).")]
        public string controlsHint =
            "Hold Left-Shift = aim LEFT hand / Space = aim RIGHT hand   |   move: mouse + WASD, Q/E = down/up   |   " +
            "G = grab / let go (latches)   |   hold right-mouse = look   |   pull a gripped hand DOWN to climb";

        Texture2D _px;
        GUIStyle _mid, _small, _rightLbl;
        string _flash;
        float _flashT;

        void Awake()
        {
            if (balance == null) balance = GetComponent<BalanceSystem>();
        }

        void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.PlayerFell += OnFell;
        }

        void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.PlayerFell -= OnFell;
        }

        void Update()
        {
            if (_flashT > 0f) _flashT -= Time.deltaTime;
        }

        void OnFell() { _flash = "Fell!"; _flashT = 1.6f; }

        void OnGUI()
        {
            if (_px == null)
            {
                _px = new Texture2D(1, 1); _px.SetPixel(0, 0, Color.white); _px.Apply();
            }
            if (_mid == null)
            {
                _mid = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 30, fontStyle = FontStyle.Bold };
                _small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
                _rightLbl = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 16 };
            }

            // Balance bar (top-left).
            float bw = 240f, bh = 18f, x = 16f, y = 16f;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, bw + 4f, bh + 4f), _px);
            float n = balance != null ? Mathf.Clamp01(balance.Normalized) : 1f;
            GUI.color = (balance != null && balance.IsSlipping) ? new Color(0.92f, 0.27f, 0.2f) : new Color(0.3f, 0.85f, 0.42f);
            GUI.DrawTexture(new Rect(x, y, bw * n, bh), _px);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + bh + 2f, 200f, 20f), "BALANCE");

            var gm = GameManager.Instance;
            if (gm != null)
                GUI.Label(new Rect(Screen.width - 230f, 14f, 214f, 22f), $"Time {gm.ElapsedTime:0.0}s    Falls {gm.FallCount}", _rightLbl);

            // Controls hint (bottom).
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.Label(new Rect(8f, Screen.height - 24f, Screen.width - 16f, 20f), controlsHint, _small);
            GUI.color = Color.white;

            // Centre banners.
            if (gm != null && gm.State == GameState.Summit)
                GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 50f), $"SUMMIT!  {gm.ElapsedTime:0.0}s", _mid);
            else if (_flashT > 0f)
            {
                GUI.color = new Color(1f, 0.5f, 0.45f);
                GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 46f), _flash, _mid);
                GUI.color = Color.white;
            }
        }
    }
}
