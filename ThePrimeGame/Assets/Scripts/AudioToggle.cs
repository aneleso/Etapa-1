using UnityEngine;

namespace ThePrimeGame
{
    public sealed class AudioToggle : MonoBehaviour
    {
        private const string MuteKey = "ThePrimeGame.Muted";
        private AudioSource music;
        private AudioSource effects;
        private PrimeGameAssets assets;
        public bool IsMuted { get; private set; }
        public bool MusicStarted { get; private set; }

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
        }

        public void Toggle()
        {
            IsMuted = !IsMuted;
            ApplyVolume();
            PlayerPrefs.SetInt(MuteKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Pickup() => Play(assets.pickup, 0.65f);
        public void Snap() => Play(assets.snap, 0.75f);
        public void Success() => Play(assets.success, 0.7f);

        private void Play(AudioClip clip, float volume)
        {
            BeginMusic();
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
