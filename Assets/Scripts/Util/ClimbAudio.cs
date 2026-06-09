using UnityEngine;
using VRClimb.Gameplay;
using VRClimb.Climbing;

namespace VRClimb.Util
{
    /// <summary>
    /// Plays simple one-shot SFX in response to game events: grabbing a hold, the onset of a balance
    /// slip, falling, and topping out. Assign clips in the Inspector — missing clips are skipped, so
    /// you can wire up only the ones you have. Subscriptions to the GameManager are made in Start so
    /// the singleton is guaranteed to exist.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ClimbAudio : MonoBehaviour
    {
        [Header("Refs")]
        public ClimbingHand leftHand;
        public ClimbingHand rightHand;
        public BalanceSystem balance;

        [Header("Clips (optional)")]
        public AudioClip grab;
        public AudioClip slip;     // fired once when you start slipping
        public AudioClip fall;
        public AudioClip summit;

        AudioSource _src;
        bool _wasSlipping;
        bool _subscribed;

        void Awake()
        {
            _src = GetComponent<AudioSource>();
            _src.playOnAwake = false;
        }

        void Start()
        {
            if (leftHand != null) leftHand.Grabbed += OnGrab;
            if (rightHand != null) rightHand.Grabbed += OnGrab;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerFell += OnFell;
                GameManager.Instance.Finished += OnFinished;
            }
            _subscribed = true;
        }

        void OnDestroy()
        {
            if (!_subscribed) return;
            if (leftHand != null) leftHand.Grabbed -= OnGrab;
            if (rightHand != null) rightHand.Grabbed -= OnGrab;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerFell -= OnFell;
                GameManager.Instance.Finished -= OnFinished;
            }
        }

        void Update()
        {
            if (balance == null) return;
            if (balance.IsSlipping && !_wasSlipping) Play(slip);
            _wasSlipping = balance.IsSlipping;
        }

        void OnGrab(ClimbingHand h) => Play(grab);
        void OnFell() => Play(fall);
        void OnFinished(float t) => Play(summit);

        void Play(AudioClip c) { if (c != null) _src.PlayOneShot(c); }
    }
}
