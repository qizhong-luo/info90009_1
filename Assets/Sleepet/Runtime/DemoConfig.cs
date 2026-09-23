using UnityEngine;

namespace Sleepet
{
    public enum SleepBehaviour { StopWhenAsleep, FadeOutGently, PlayAllNight }
    public enum CompanionMode { Quiet, Occasional, Proactive }
    public enum SleepState { Awake, LowAttention, LikelyAsleep, Ended }
    public enum MorningResult { Calm, Difficult }

    [CreateAssetMenu(menuName = "Sleepet/Demo Config")]
    public sealed class DemoConfig : ScriptableObject
    {
        [Min(0.1f)] public float lowAttentionDelay = 30f;
        [Tooltip("Additional inactivity AFTER Low Attention, not total elapsed time.")]
        [Min(0.1f)] public float likelyAsleepDelay = 60f;
        [Min(0.1f)] public float fadeDuration = 8f;
        [Min(0.05f)] public float mockReplyDelay = 0.4f;
        [Min(0.2f)] public float endHoldDuration = 1.5f;
    }

    // Deliberately simulated inactivity, not a sleep measurement.
    public sealed class SleepDetector
    {
        readonly float lowDelay;
        readonly float asleepDelay;
        public SleepState State { get; private set; } = SleepState.Ended;
        public float InactiveSeconds { get; private set; }
        public bool Running => State != SleepState.Ended;
        public event System.Action<SleepState> Changed;

        public SleepDetector(float lowDelay, float asleepDelay)
        {
            this.lowDelay = Mathf.Max(0.1f, lowDelay);
            this.asleepDelay = Mathf.Max(0.1f, asleepDelay);
        }

        public void Start() { InactiveSeconds = 0; Set(SleepState.Awake); }
        public void Activity()
        {
            if (!Running) return;
            InactiveSeconds = 0;
            Set(SleepState.Awake);
        }
        public void Tick(float seconds)
        {
            if (!Running) return;
            InactiveSeconds += Mathf.Max(0, seconds);
            if (State == SleepState.Awake && InactiveSeconds >= lowDelay)
                Set(SleepState.LowAttention);
            if (State == SleepState.LowAttention && InactiveSeconds >= lowDelay + asleepDelay)
                Set(SleepState.LikelyAsleep);
        }
        public void Force(SleepState state)
        {
            if (!Running || state == SleepState.Ended) return;
            InactiveSeconds = state == SleepState.Awake ? 0 :
                state == SleepState.LowAttention ? lowDelay : lowDelay + asleepDelay;
            Set(state);
        }
        public void End() { if (Running) Set(SleepState.Ended); }
        void Set(SleepState next)
        {
            if (State == next) return;
            State = next;
            Changed?.Invoke(next);
        }
    }
}
