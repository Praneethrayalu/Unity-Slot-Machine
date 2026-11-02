using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotGame.Audio
{
    /// <summary>Clip + mix settings for one <see cref="SoundId"/>.</summary>
    [Serializable]
    public sealed class SoundEntry
    {
        [SerializeField] private SoundId id;
        [SerializeField] private AudioClip clip;
        [Range(0f, 1f)] [SerializeField] private float volume = 1f;

        [Tooltip("Random pitch variation (+/-) so repeated sounds do not feel robotic.")]
        [Range(0f, 0.3f)] [SerializeField] private float pitchVariance;

        public SoundEntry(SoundId id, AudioClip clip, float volume, float pitchVariance)
        {
            this.id = id;
            this.clip = clip;
            this.volume = volume;
            this.pitchVariance = pitchVariance;
        }

        public SoundId Id => id;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float PitchVariance => pitchVariance;
    }

    /// <summary>Maps sound ids to clips so audio can be re-skinned without touching code.</summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Slot Game/Sound Library", order = 10)]
    public sealed class SoundLibrary : ScriptableObject
    {
        [SerializeField] private List<SoundEntry> sounds = new List<SoundEntry>();

        private Dictionary<SoundId, SoundEntry> _lookup;

        public bool TryGet(SoundId id, out SoundEntry entry)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<SoundId, SoundEntry>();
                foreach (SoundEntry sound in sounds)
                {
                    if (sound?.Clip != null)
                    {
                        _lookup[sound.Id] = sound;
                    }
                }
            }

            return _lookup.TryGetValue(id, out entry);
        }

        private void OnValidate()
        {
            _lookup = null;
        }

#if UNITY_EDITOR
        public void EditorSetSounds(IEnumerable<SoundEntry> entries)
        {
            sounds = new List<SoundEntry>(entries);
            _lookup = null;
        }
#endif
    }
}
