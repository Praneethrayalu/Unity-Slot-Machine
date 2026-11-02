using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlotGame.Audio
{
    /// <summary>
    /// Plays one-shot and looping sound effects from a <see cref="SoundLibrary"/>.
    /// <para>
    /// One-shots use a small round-robin pool of AudioSources so overlapping sounds (three reel
    /// stops in quick succession, coin ticks) never cut each other off. Mute state is persisted.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        private const string MutedPrefsKey = "SlotGame.Muted";

        [SerializeField] private SoundLibrary library;

        [Tooltip("How many one-shot sounds can overlap.")]
        [Range(1, 16)] [SerializeField] private int voiceCount = 8;

        [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.9f;

        private readonly List<AudioSource> _voices = new List<AudioSource>();
        private readonly Dictionary<SoundId, AudioSource> _loops = new Dictionary<SoundId, AudioSource>();
        private int _nextVoice;
        private bool _muted;

        /// <summary>Raised when the player toggles sound.</summary>
        public event Action<bool> MuteChanged;

        public bool IsMuted => _muted;

        private void Awake()
        {
            for (int i = 0; i < voiceCount; i++)
            {
                _voices.Add(CreateSource($"Voice {i}", false));
            }

            SetMuted(PlayerPrefs.GetInt(MutedPrefsKey, 0) == 1, false);
        }

        /// <summary>Plays a one-shot sound. <paramref name="pitch"/> is multiplied with the entry's random variance.</summary>
        public void Play(SoundId id, float pitch = 1f, float volumeScale = 1f)
        {
            if (!TryGetEntry(id, out SoundEntry entry))
            {
                return;
            }

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Count;

            voice.Stop();
            voice.clip = entry.Clip;
            voice.volume = entry.Volume * volumeScale * masterVolume;
            voice.pitch = pitch * (1f + UnityEngine.Random.Range(-entry.PitchVariance, entry.PitchVariance));
            voice.Play();
        }

        /// <summary>Starts a looping sound (no-op when it is already playing).</summary>
        public void StartLoop(SoundId id)
        {
            if (!TryGetEntry(id, out SoundEntry entry))
            {
                return;
            }

            if (!_loops.TryGetValue(id, out AudioSource source))
            {
                source = CreateSource($"Loop {id}", true);
                _loops.Add(id, source);
            }

            source.clip = entry.Clip;
            source.volume = entry.Volume * masterVolume;
            source.pitch = 1f;
            if (!source.isPlaying)
            {
                source.Play();
            }
        }

        /// <summary>Fades out and stops a looping sound.</summary>
        public void StopLoop(SoundId id, float fadeOut = 0.15f)
        {
            if (_loops.TryGetValue(id, out AudioSource source) && source.isPlaying)
            {
                StartCoroutine(FadeOutRoutine(source, fadeOut));
            }
        }

        public void ToggleMute()
        {
            SetMuted(!_muted, true);
        }

        public void SetMuted(bool muted, bool persist)
        {
            _muted = muted;

            // Silencing the listener mutes everything, including sounds already playing.
            AudioListener.volume = muted ? 0f : 1f;

            if (persist)
            {
                PlayerPrefs.SetInt(MutedPrefsKey, muted ? 1 : 0);
                PlayerPrefs.Save();
            }

            MuteChanged?.Invoke(muted);
        }

        private bool TryGetEntry(SoundId id, out SoundEntry entry)
        {
            entry = null;
            return library != null && library.TryGet(id, out entry);
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private static IEnumerator FadeOutRoutine(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;
        }
    }
}
