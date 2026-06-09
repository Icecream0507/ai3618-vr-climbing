using System;
using UnityEngine;

namespace VRClimb.Gameplay
{
    public enum GameState { Ready, Climbing, Summit, Fell }

    /// <summary>
    /// Lightweight game flow: tracks state, the climb timer and fall count, and raises events the
    /// UI / audio layers subscribe to. A simple singleton so triggers, holds and the HUD can reach
    /// it without wiring every reference in the Inspector.
    ///
    /// The run starts (timer begins) on the first hand grab — <see cref="ClimbController"/> calls
    /// <see cref="StartClimb"/> when it sees the first grip while still in <see cref="GameState.Ready"/>.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] GameState _state = GameState.Ready;
        public GameState State => _state;

        public float ElapsedTime { get; private set; }
        public int FallCount { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action PlayerFell;     // transient: fired once per fall (for HUD/audio feedback)
        public event Action<float> Finished; // final time, fired once on summit

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Update()
        {
            if (_state == GameState.Climbing) ElapsedTime += Time.deltaTime;
        }

        public void StartClimb()
        {
            if (_state == GameState.Climbing || _state == GameState.Summit) return;
            ElapsedTime = 0f;
            FallCount = 0;
            SetState(GameState.Climbing);
        }

        public void OnPlayerFell()
        {
            if (_state == GameState.Summit) return;   // a stray fall after topping out must not undo the win
            FallCount++;
            PlayerFell?.Invoke();                     // transient feedback; state stays Climbing
            if (_state != GameState.Climbing) SetState(GameState.Climbing);
        }

        public void OnSummitReached()
        {
            if (_state == GameState.Summit) return;
            SetState(GameState.Summit);
            Finished?.Invoke(ElapsedTime);
        }

        void SetState(GameState next)
        {
            _state = next;
            StateChanged?.Invoke(next);
        }
    }
}
