using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRClimb.UI;
using VRClimb.Util;
using VRClimb.Gameplay;
using VRClimb.Climbing;

namespace VRClimb.EditorTools
{
    /// <summary>
    /// P5 (UX · audio) convenience: <c>VRClimb ▸ Set Up HUD + Audio</c>. Builds the world-space HUD
    /// Canvas (timer + status text, stamina + balance bars) and a ClimbAudio object, then auto-wires
    /// every reference it can find in the scene (BalanceSystem / StaminaSystem / the two ClimbingHands)
    /// and loads the placeholder SFX from <c>Assets/Audio</c>. Re-running it is safe — it reuses the
    /// existing HUD / Audio objects instead of duplicating them.
    ///
    /// Run this AFTER "Set Up Test Scene" and the climber wiring, so the BalanceSystem and hands exist
    /// to link against. Anything it can't find is logged and left for you to assign in the Inspector.
    /// </summary>
    public static class ClimbUIAudioSetup
    {
        const string AudioDir = "Assets/Audio";

        [MenuItem("VRClimb/Set Up HUD + Audio")]
        public static void SetUpHudAndAudio()
        {
            var balance = Object.FindObjectOfType<BalanceSystem>();
            var stamina = Object.FindObjectOfType<StaminaSystem>();
            ClimbingHand left = null, right = null;
            if (balance != null) { left = balance.leftHand; right = balance.rightHand; }
            if (left == null || right == null) FindHands(ref left, ref right);

            BuildHud(balance, stamina);
            BuildAudio(balance, left, right);

            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            Debug.Log("[VRClimb] HUD + Audio set up. If any ref is missing, run this after the climber " +
                      "is wired (BalanceSystem + hands must exist), then re-run — or assign in the Inspector.");
        }

        // ---- HUD ---------------------------------------------------------------------------------

        static void BuildHud(BalanceSystem balance, StaminaSystem stamina)
        {
            var existing = Object.FindObjectOfType<GameHUD>();
            GameObject canvasGo;
            if (existing != null)
            {
                canvasGo = existing.gameObject;
            }
            else
            {
                canvasGo = new GameObject("HUD Canvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameHUD));
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create HUD Canvas");

                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                // A small panel floating beside the wall start; tweak in-scene to taste.
                var crt = canvas.GetComponent<RectTransform>();
                crt.sizeDelta = new Vector2(400f, 220f);
                crt.localScale = Vector3.one * 0.0015f;       // ~0.6m wide in world space
                crt.position = new Vector3(0.5f, 1.5f, 0.4f);

                BuildHudContents(canvasGo, out _, out _, out _, out _);
            }

            var hud = canvasGo.GetComponent<GameHUD>();
            // (Re)link content refs by name so re-runs and hand-built canvases both resolve.
            hud.timerLabel   = FindText(canvasGo, "Timer");
            hud.statusLabel  = FindText(canvasGo, "Status");
            hud.staminaBar   = FindImage(canvasGo, "StaminaFill");
            hud.balanceBar   = FindImage(canvasGo, "BalanceFill");
            hud.balance      = balance;
            hud.stamina      = stamina;

            if (balance == null)
                Debug.LogWarning("[VRClimb] HUD: no BalanceSystem found — balance bar won't update until assigned.");
            EditorUtility.SetDirty(hud);
        }

        static void BuildHudContents(GameObject canvasGo,
            out TMP_Text timer, out TMP_Text status, out Image staminaFill, out Image balanceFill)
        {
            // Background panel
            var bg = NewUI("Panel", canvasGo.transform);
            Stretch(bg.GetComponent<RectTransform>());
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);

            timer  = NewLabel("Timer", canvasGo.transform,  new Vector2(0, 80),  64, "00:00");
            status = NewLabel("Status", canvasGo.transform, new Vector2(0, -80), 30, "Reach the top!");

            staminaFill = NewBar("Stamina", canvasGo.transform, new Vector2(0, 20),
                                 new Color(0.3f, 0.7f, 1f), "STAMINA");
            balanceFill = NewBar("Balance", canvasGo.transform, new Vector2(0, -25),
                                 new Color(0.3f, 0.7f, 1f), "BALANCE");
        }

        static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static TMP_Text NewLabel(string name, Transform parent, Vector2 anchoredPos, float size, string text)
        {
            var go = NewUI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380, 70);
            rt.anchoredPosition = anchoredPos;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            return t;
        }

        // A labelled bar: track + fill (Image.fillAmount, horizontal). Returns the fill image.
        static Image NewBar(string name, Transform parent, Vector2 anchoredPos, Color fillColor, string caption)
        {
            var track = NewUI(name + "Track", parent);
            var trt = track.GetComponent<RectTransform>();
            trt.sizeDelta = new Vector2(320, 26);
            trt.anchoredPosition = anchoredPos;
            var trackImg = track.AddComponent<Image>();
            trackImg.color = new Color(1f, 1f, 1f, 0.15f);

            var fill = NewUI(name + "Fill", track.transform);
            Stretch(fill.GetComponent<RectTransform>());
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 1f;
            // a flat sprite so Filled works without an imported texture
            fillImg.sprite = null;

            var cap = NewLabel(name + "Caption", track.transform, new Vector2(-150, 0), 14, caption);
            cap.alignment = TextAlignmentOptions.Left;
            cap.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 26);

            return fillImg;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---- Audio -------------------------------------------------------------------------------

        static void BuildAudio(BalanceSystem balance, ClimbingHand left, ClimbingHand right)
        {
            var existing = Object.FindObjectOfType<ClimbAudio>();
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("ClimbAudio", typeof(AudioSource), typeof(ClimbAudio));
                Undo.RegisterCreatedObjectUndo(go, "Create ClimbAudio");
            }

            var audio = go.GetComponent<ClimbAudio>();
            audio.balance   = balance;
            audio.leftHand  = left;
            audio.rightHand = right;
            audio.grab   = LoadClip("grab.wav");
            audio.slip   = LoadClip("slip.wav");
            audio.fall   = LoadClip("fall.wav");
            audio.summit = LoadClip("summit.wav");
            EditorUtility.SetDirty(audio);

            if (audio.grab == null)
                Debug.LogWarning("[VRClimb] Audio: SFX not found in " + AudioDir +
                                 ". Run Assets/Audio/_generate_placeholder_sfx.py or drop your own clips.");
        }

        static AudioClip LoadClip(string file) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{file}");

        // ---- helpers -----------------------------------------------------------------------------

        static TMP_Text FindText(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                if (t.gameObject.name == name) return t;
            return null;
        }

        static Image FindImage(GameObject root, string name)
        {
            foreach (var im in root.GetComponentsInChildren<Image>(true))
                if (im.gameObject.name == name) return im;
            return null;
        }

        static void FindHands(ref ClimbingHand left, ref ClimbingHand right)
        {
            foreach (var h in Object.FindObjectsOfType<ClimbingHand>())
            {
                if (h.hapticNode == UnityEngine.XR.XRNode.LeftHand && left == null) left = h;
                else if (h.hapticNode == UnityEngine.XR.XRNode.RightHand && right == null) right = h;
            }
        }
    }
}
