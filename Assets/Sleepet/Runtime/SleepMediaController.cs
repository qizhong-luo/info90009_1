using System;
using UnityEngine;
namespace Sleepet
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SleepMediaController : MonoBehaviour
    {
        public AudioClip rain, ocean;
        public AudioSource source;
        public float selectedVolume = 0.35f;
        float fadeElapsed, fadeDuration, fadeStartVolume;
        public bool IsFading { get; private set; }
        public bool IsPlaying => source != null && source.isPlaying;
        public float Volume => source == null ? 0 : source.volume;
        public int Sound { get; private set; }
        public string SoundName => Sound == 0 ? "Rain" : Sound == 1 ? "Ocean" : "Silence";
        public event Action<string> MediaEvent;
        void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0;
            source.volume = selectedVolume;
            source.clip = rain;
        }
        public void Configure(int sound, float volume)
        {
            bool wasPlaying = IsPlaying, changed = Sound != sound;
            Sound = Mathf.Clamp(sound, 0, 2);
            selectedVolume = Mathf.Clamp01(volume);
            if (changed) Stop();
            source.clip = Sound == 0 ? rain : Sound == 1 ? ocean : null;
            if (!IsFading) source.volume = selectedVolume;
            if (changed && wasPlaying && source.clip != null) Play();
        }
        public void Play()
        {
            IsFading = false;
            source.volume = selectedVolume;
            if (source.clip == null) { MediaEvent?.Invoke("MEDIA_SILENCE_SELECTED"); return; }
            if (!source.isPlaying) source.Play();
            MediaEvent?.Invoke("MEDIA_STARTED");
        }
        public void Apply(SleepBehaviour behaviour, float duration)
        {
            if (behaviour == SleepBehaviour.StopWhenAsleep) Stop();
            else if (behaviour == SleepBehaviour.FadeOutGently && IsPlaying)
            {
                fadeElapsed = 0; fadeDuration = Mathf.Max(0.1f, duration);
                fadeStartVolume = source.volume; IsFading = true;
                MediaEvent?.Invoke("MEDIA_FADE_STARTED");
            }
            else if (behaviour == SleepBehaviour.PlayAllNight) MediaEvent?.Invoke("MEDIA_CONTINUED");
        }
        public void Wake()
        {
            if (!IsFading) return;
            IsFading = false; source.volume = selectedVolume;
            MediaEvent?.Invoke("MEDIA_FADE_CANCELLED");
        }
        public void Advance(float seconds)
        {
            if (!IsFading) return;
            fadeElapsed += Mathf.Max(0, seconds);
            source.volume = fadeStartVolume * (1 - Mathf.Clamp01(fadeElapsed / fadeDuration));
            if (fadeElapsed >= fadeDuration) Stop();
        }
        public void Stop()
        {
            bool wasActive = IsPlaying || IsFading;
            IsFading = false; source.Stop(); source.volume = 0;
            if (wasActive) MediaEvent?.Invoke("MEDIA_STOPPED");
        }
    }
}
