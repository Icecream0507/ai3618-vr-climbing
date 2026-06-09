using UnityEngine;
using TMPro;
using VRClimb.Gameplay;
using VRClimb.Climbing;

namespace VRClimb.UI
{
    /// <summary>
    /// World-space HUD. Subscribes to <see cref="GameManager"/> and reads
    /// <see cref="StaminaSystem"/> / <see cref="BalanceSystem"/> to update TextMeshPro labels and
    /// two bars (stamina, balance). Attach to a world-space Canvas on the player's wrist or floating
    /// near the wall. The balance bar turns red while the climber is slipping; falling shows a brief
    /// message that clears itself.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        public TMP_Text timerLabel;
        public TMP_Text statusLabel;
        public StaminaSystem stamina;
        public UnityEngine.UI.Image staminaBar;
        public BalanceSystem balance;
        public UnityEngine.UI.Image balanceBar;

        [Tooltip("Seconds the 'you fell' message stays on screen.")]
        public float fellMessageSeconds = 2f;
        float _fellTimer;

        void OnEnable()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.StateChanged += OnStateChanged;
            GameManager.Instance.PlayerFell += OnPlayerFell;
            GameManager.Instance.Finished += OnFinished;
        }

        void OnDisable()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.StateChanged -= OnStateChanged;
            GameManager.Instance.PlayerFell -= OnPlayerFell;
            GameManager.Instance.Finished -= OnFinished;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && timerLabel != null) timerLabel.text = FormatTime(gm.ElapsedTime);
            if (stamina != null && staminaBar != null) staminaBar.fillAmount = stamina.Normalized;
            if (balance != null && balanceBar != null)
            {
                balanceBar.fillAmount = balance.Normalized;
                balanceBar.color = balance.IsSlipping ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.3f, 0.7f, 1f);
            }

            if (_fellTimer > 0f)
            {
                _fellTimer -= Time.deltaTime;
                if (_fellTimer <= 0f && statusLabel != null &&
                    gm != null && gm.State == GameState.Climbing)
                    statusLabel.text = "";
            }
        }

        void OnStateChanged(GameState s)
        {
            if (statusLabel == null) return;
            switch (s)
            {
                case GameState.Ready:    statusLabel.text = "Reach the top!"; break;
                case GameState.Climbing: if (_fellTimer <= 0f) statusLabel.text = ""; break;
                case GameState.Summit:   statusLabel.text = "Summit!"; break;
            }
        }

        void OnPlayerFell()
        {
            if (statusLabel == null) return;
            statusLabel.text = "You fell — keep going!";
            _fellTimer = fellMessageSeconds;
        }

        void OnFinished(float time)
        {
            if (statusLabel == null) return;
            int falls = GameManager.Instance != null ? GameManager.Instance.FallCount : 0;
            statusLabel.text = $"Summit!  Time {FormatTime(time)}  Falls {falls}";
            _fellTimer = 0f;
        }

        static string FormatTime(float t)
        {
            int m = Mathf.FloorToInt(t / 60f);
            int s = Mathf.FloorToInt(t % 60f);
            return $"{m:00}:{s:00}";
        }
    }
}
