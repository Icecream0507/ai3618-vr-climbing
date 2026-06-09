using System;
using UnityEngine;

namespace VRClimb.Gameplay
{
    public enum GameState { Ready, Climbing, Summit, Fell }

    /// <summary>
    /// Lightweight game flow: tracks state, the climb timer and fall count, and raises events the
    /// UI / audio layers subscribe to. A simple singleton so triggers, holds and the HUD can reach
    /// it without wiring every reference in the Inspector.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] GameState _state = GameState.Ready;
        public GameState State => _state;

        public float ElapsedTime { get; private set; }
        public int FallCount { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action<float> Finished;   // final time, fired once on summit

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
            ElapsedTime = 0f;
            FallCount = 0;
            SetState(GameState.Climbing);
        }

        public void OnPlayerFell()
        {
            FallCount++;
            SetState(GameState.Fell);
            SetState(GameState.Climbing);   // auto-resume after respawn
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
