using UnityEngine;
using TMPro;
using VRClimb.Gameplay;

namespace VRClimb.UI
{
    /// <summary>
    /// World-space HUD. Subscribes to <see cref="GameManager"/> / <see cref="StaminaSystem"/> and
    /// updates a couple of TextMeshPro labels (climb timer, status) plus a stamina bar. Attach to a
    /// world-space Canvas that sits on the player's wrist or floats near the wall.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        public TMP_Text timerLabel;
        public TMP_Text statusLabel;
        public StaminaSystem stamina;
        public UnityEngine.UI.Image staminaBar;

        void OnEnable()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.StateChanged += OnStateChanged;
            GameManager.Instance.Finished += OnFinished;
        }

        void OnDisable()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.StateChanged -= OnStateChanged;
            GameManager.Instance.Finished -= OnFinished;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && timerLabel != null) timerLabel.text = FormatTime(gm.ElapsedTime);
            if (stamina != null && staminaBar != null) staminaBar.fillAmount = stamina.Normalized;
        }

        void OnStateChanged(GameState s)
        {
            if (statusLabel == null) return;
            statusLabel.text = s switch
            {
                GameState.Ready    => "Reach the top!",
                GameState.Climbing => "",
                GameState.Fell     => "You fell — keep going!",
                GameState.Summit   => "Summit!",
                _                  => ""
            };
        }

        void OnFinished(float time)
        {
            if (statusLabel == null) return;
            int falls = GameManager.Instance != null ? GameManager.Instance.FallCount : 0;
            statusLabel.text = $"Summit!  Time {FormatTime(time)}  Falls {falls}";
        }

        static string FormatTime(float t)
        {
            int m = Mathf.FloorToInt(t / 60f);
            int s = Mathf.FloorToInt(t % 60f);
            return $"{m:00}:{s:00}";
        }
    }
}
