using Cysharp.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Hunt
{
    public class AudioManager : MonoBehaviourSingleton<AudioManager>
    {
        [Header("AUDIO MIXER")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("AUDIO SETTINGS")]
        [SerializeField] private int maxSfxPoolCount = 10;
        [SerializeField] private float bgmVolume = 0.8f;
        [SerializeField] private float sfxVolume = 1.0f;

        private Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();
        private ConcurrentQueue<AudioSource> audioSfxPool = new ConcurrentQueue<AudioSource>();
        private List<AudioSource> activeSfxSources = new List<AudioSource>();
        private AudioSource bgmSource;
        private bool isPreloadComplete = false;

        protected override bool DontDestroy => true;
        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Init()
        {
            CreateBgmSource();
            for (int i = 0; i < maxSfxPoolCount; i++)
            {
                CreateSfxSource();
            }
            PreLoadAudioResources().Forget();
        }

        private void CreateBgmSource()
        {
            GameObject bgmObject = new GameObject("BGM");
            bgmObject.transform.SetParent(this.transform);
            bgmSource = bgmObject.AddComponent<AudioSource>();
            bgmSource.outputAudioMixerGroup = bgmGroup;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = bgmVolume;
        }
        private AudioSource CreateSfxSource()
        {
            GameObject sfxObject = new GameObject($"SFX");
            sfxObject.transform.SetParent(this.transform);
            var sfxsource = sfxObject.AddComponent<AudioSource>();
            sfxsource.outputAudioMixerGroup = sfxGroup;
            sfxsource.loop = false;
            sfxsource.playOnAwake = false;
            sfxsource.volume = sfxVolume;
            audioSfxPool.Enqueue(sfxsource);
            return sfxsource;
        }
        private async UniTask PreLoadAudioResources()
        {
            "🔊 [AudioHelper] AudioClip Preload Start...".DLog();

            await UniTask.WaitUntil(() => AbLoader.Shared != null);
            List<UniTask> loadTasks = new List<UniTask>();

            foreach (var audioKey in AudioKeyConst.GetAllSfxKeys())
            {
                if (!string.IsNullOrEmpty(audioKey))
                {
                    loadTasks.Add(LoadAudioClipAsync(audioKey).AsUniTask());
                }
            }

            await UniTask.WhenAll(loadTasks);
            isPreloadComplete = true;
            $"🔊 [AudioHelper] AudioClip Preload Success! {audioClipCache.Count}ea load".DLog();
        }

        private async UniTask<AudioClip> LoadAudioClipAsync(string audioKey)
        {
            if (AbLoader.Shared == null) return null;
            try
            {
                var audioClip = await AbLoader.Shared.LoadAssetAsync<AudioClip>(audioKey);
                if (audioClip != null)
                {
                    audioClipCache[audioKey] = audioClip;
                }
                return audioClip;
            }
            catch (System.Exception ex)
            {
                $"🔊 [AudioHelper] AudioClip Load Fail : {audioKey} - {ex.Message}".DError();
                return null;
            }
        }

        public void PlaySfx(string audioKey, float volumeScale = 1.0f)
        {
            if (string.IsNullOrEmpty(audioKey)) return;
            PlaySfxAsync(audioKey, volumeScale).Forget();
        }

        private async UniTaskVoid PlaySfxAsync(string audioKey, float volumeScale = 1.0f)
        {
            AudioClip clip = null;

            if (!audioClipCache.TryGetValue(audioKey, out clip))
            {
                if (!isPreloadComplete)
                {
                    await UniTask.WaitUntil(() => isPreloadComplete);
                    audioClipCache.TryGetValue(audioKey, out clip);
                }

                if (clip == null && AbLoader.Shared != null)
                {
                    clip = await LoadAudioClipAsync(audioKey);
                }

                if (clip == null)
                {
                    $"🔊 [AudioHelper] AudioClip not Find: {audioKey}".DError();
                    return;
                }
            }

            if (!audioSfxPool.TryDequeue(out var sfxSource))
            {
                sfxSource = CreateSfxSource();
            }

            if (sfxSource == null) return;

            if (sfxSource.isPlaying)
            {
                sfxSource.Stop();
            }

            sfxSource.clip = clip;
            sfxSource.volume = sfxVolume * volumeScale;
            sfxSource.Play();

            if (!activeSfxSources.Contains(sfxSource))
            {
                activeSfxSources.Add(sfxSource);
            }

            ReturnSfxSourceToPoolAfterPlay(sfxSource, clip.length).Forget();
        }

        private async UniTaskVoid ReturnSfxSourceToPoolAfterPlay(AudioSource source, float delay)
        {
            if (source == null) return;

            await UniTask.Delay(System.TimeSpan.FromSeconds(delay));

            if (source != null && source.isPlaying)
            {
                await UniTask.WaitUntil(() => source == null || !source.isPlaying);
            }

            if (source != null)
            {
                source.Stop();
                source.clip = null;
                if (activeSfxSources.Contains(source))
                {
                    activeSfxSources.Remove(source);
                }
                audioSfxPool.Enqueue(source);
            }
        }

        public void PlayBgm(string audioKey, bool loop = true, float fadeInDuration = 0f)
        {
            PlayBgmAsync(audioKey, loop, fadeInDuration).Forget();
        }

        private async UniTaskVoid PlayBgmAsync(string audioKey, bool loop = true, float fadeInDuration = 0f)
        {
            // 캐시에 없으면 Preload 완료 대기
            if (!audioClipCache.TryGetValue(audioKey, out var clip))
            {
                if (!isPreloadComplete)
                {
                    await UniTask.WaitUntil(() => isPreloadComplete);

                    if (!audioClipCache.TryGetValue(audioKey, out clip))
                    {
                        $"🔊 [AudioHelper] BGM AudioClip not found: {audioKey}".DError();
                        return;
                    }
                }
                else
                {
                    $"🔊 [AudioHelper] BGM AudioClip not found: {audioKey}".DError();
                    return;
                }
            }

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = fadeInDuration > 0 ? 0 : bgmVolume;
            bgmSource.Play();

            if (fadeInDuration > 0)
            {
                FadeInBgm(fadeInDuration).Forget();
            }
        }

        private async UniTaskVoid FadeInBgm(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0, bgmVolume, elapsed / duration);
                await UniTask.Yield();
            }
            bgmSource.volume = bgmVolume;
        }

        public void StopBgm(float fadeOutDuration = 0f)
        {
            if (fadeOutDuration > 0)
            {
                FadeOutBgm(fadeOutDuration).Forget();
            }
            else
            {
                bgmSource.Stop();
            }
        }

        private async UniTaskVoid FadeOutBgm(float duration)
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                await UniTask.Yield();
            }
            bgmSource.volume = 0;
            bgmSource.Stop();
        }

        public bool IsPreloadComplete() => isPreloadComplete;


    }
}
