using System;
using UnityEngine;

namespace ThePrimeGame
{
    public enum AudioCue { Ready, Music, Pickup, Snap, Success, Muted, Enabled }

    /// <summary>Plays local music and effects while publishing the same events for visible audio descriptions.</summary>
    public sealed class AudioToggle : MonoBehaviour
    {
        private const string MuteKey = "ThePrimeGame.Muted";
        private AudioSource music;
        private AudioSource effects;
        private PrimeGameAssets assets;
        public bool IsMuted { get; private set; }
        public bool MusicStarted { get; private set; }
        public event Action<AudioCue> CueChanged;

        /// <summary>Creates separate sources and restores the player's saved sound preference.</summary>
        public void Initialize(PrimeGameAssets resources)
        {
            assets = resources;
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0.3f;
            music.spatialBlend = 0f;
            music.clip = assets.ambient;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0f;
            IsMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            ApplyVolume();
        }

        // Music starts after intentional interaction, never on a silent loading screen.
        public void BeginMusic()
        {
            if (MusicStarted || music == null || music.clip == null) return;
            MusicStarted = true;
            music.Play();
            CueChanged?.Invoke(AudioCue.Music);
        }

        /// <summary>Persists the mute choice and publishes a caption even when no sound can be heard.</summary>
        public void Toggle()
        {
            IsMuted = !IsMuted;
            ApplyVolume();
            PlayerPrefs.SetInt(MuteKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            CueChanged?.Invoke(IsMuted ? AudioCue.Muted : MusicStarted ? AudioCue.Music : AudioCue.Enabled);
        }

        public void Pickup() => Play(assets.pickup, 0.65f, AudioCue.Pickup);
        public void Snap() => Play(assets.snap, 0.75f, AudioCue.Snap);
        public void Success() => Play(assets.success, 0.7f, AudioCue.Success);

        /// <summary>Starts music after interaction and emits a visual equivalent before optionally playing the effect.</summary>
        private void Play(AudioClip clip, float volume, AudioCue cue)
        {
            BeginMusic();
            // Captions remain available when muted; hearing an effect is never required to understand an action.
            CueChanged?.Invoke(cue);
            if (clip != null && !IsMuted) effects.PlayOneShot(clip, volume);
        }

        private void ApplyVolume()
        {
            // Local source muting avoids changing other scenes' global listener volume.
            if (music != null) music.mute = IsMuted;
            if (effects != null) effects.mute = IsMuted;
        }
    }
}
