namespace penta
{
    /// <summary> 오디오 카테고리 </summary>
    public enum AudioCategory
    {
        BGM,       
        SFX,        
        UI,         
    }

    /// <summary> 오디오 클립 데이터 </summary>
    [System.Serializable]
    public class AudioClipData
    {
        public string clipName;
        public AudioClip clip;
        public AudioCategory category;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
        public bool is3D = false;
        public float spatialBlend = 0f; // 0 = 2D, 1 = 3D
    }

    /// <summary> 오디오 매니저 </summary>
    public class AudioManager : MonoBehaviourSingleton<AudioManager>
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup bgmMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup uiMixerGroup;

        [Header("Audio Sources Pool")]
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private int maxPoolSize = 50;

        [Header("Audio Clips Database")]
        [SerializeField] private List<AudioClipData> audioClips = new List<AudioClipData>();

        [Header("Volume Settings")]
        [Range(0f, 3f)] public float masterVolume = 1f;
        [Range(0f, 3f)] public float bgmVolume = 1f;
        [Range(0f, 3f)] public float sfxVolume = 1f;
        [Range(0f, 3f)] public float uiVolume = 1f;

        private ConcurrentQueue<AudioSource> audioSourcePool = new ConcurrentQueue<AudioSource>();
        private List<AudioSource> activeAudioSources = new List<AudioSource>();
        
        private AudioSource bgmAudioSource;
        
        private Stack<string> bgmStack = new Stack<string>();
        private string currentBGM = "";
        
        private Dictionary<string, AudioClipData> clipDatabase = new Dictionary<string, AudioClipData>();

        protected override void Awake()
        {
            base.Awake();
            InitializeAudioSystem();
            LoadVolumeSettings();
        }

        /// <summary> 오디오 시스템 초기화 </summary>
        private void InitializeAudioSystem()
        {

            CreateBGMAudioSource();
            
            CreateAudioSourcePool();
            
            BuildClipDatabase();
            
        }

        /// <summary> BGM 오디오 소스 생성 </summary>
        private void CreateBGMAudioSource()
        {
            GameObject bgmObject = new GameObject("BGM_AudioSource");
            bgmObject.transform.SetParent(transform);
            bgmAudioSource = bgmObject.AddComponent<AudioSource>();
            bgmAudioSource.outputAudioMixerGroup = bgmMixerGroup;
            bgmAudioSource.loop = true;
            bgmAudioSource.playOnAwake = false;
        }

        /// <summary> 오디오 소스 풀 생성 </summary>
        private void CreateAudioSourcePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewAudioSource();
            }
        }

        /// <summary> 새로운 오디오 소스 생성 </summary>
        private AudioSource CreateNewAudioSource()
        {

            GameObject audioObject = new GameObject($"PooledAudioSource");
            audioObject.transform.SetParent(transform);
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.gameObject.SetActive(false);
    
            return audioSource;
        }

        /// <summary> 클립 데이터베이스 빌드 </summary>
        private void BuildClipDatabase()
        {
            clipDatabase.Clear();
            foreach (var clipData in audioClips)
            {
                if (!string.IsNullOrEmpty(clipData.clipName))
                {
                    clipDatabase[clipData.clipName.ToLower()] = clipData;
                }
            }
           
        }


        /// <summary> 볼륨 설정 로드 </summary>
        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("Master", 1f);
            bgmVolume = PlayerPrefs.GetFloat("BGM", 1f);
            sfxVolume = PlayerPrefs.GetFloat("SFX", 1f);
            uiVolume = PlayerPrefs.GetFloat("UI", 1f);
            
            ApplyVolumeSettings();
        }


        /// <summary> 볼륨 설정 적용 </summary>
        private void ApplyVolumeSettings()
        {
            if (audioMixer != null)
            {
                // 볼륨을 데시벨로 변환 (0~1 -> -80dB~0dB)
                float masterDB = masterVolume <= 0 ? -80f : Mathf.Log10(masterVolume) * 20f;
                float bgmDB = bgmVolume <= 0 ? -80f : Mathf.Log10(bgmVolume) * 20f;
                float sfxDB = sfxVolume <= 0 ? -80f : Mathf.Log10(sfxVolume) * 20f;
                float uiDB = uiVolume <= 0 ? -80f : Mathf.Log10(uiVolume) * 20f;
                
                // Master 볼륨은 별도로 설정하지 않음 (AudioMixer에서 처리)
                audioMixer.SetFloat("BGM", bgmDB);
                audioMixer.SetFloat("SFX", sfxDB);
                audioMixer.SetFloat("UI", uiDB);
                
                $"[AudioManager] 볼륨 설정 적용 - BGM: {bgmVolume} ({bgmDB}dB), SFX: {sfxVolume} ({sfxDB}dB), UI: {uiVolume} ({uiDB}dB)".DLog();
            }
            else
            {
                "[AudioManager] AudioMixer가 설정되지 않았습니다.".DWarning();
            }
        }

        #region Public Method

        /// <summary> 오디오 재생 </summary>
        public AudioSource PlaySound(string clipName, float volumeScale = 1f)
        {
            return PlaySound(clipName, Vector3.zero, volumeScale, false);
        }

        /// <summary> 3D 구분 오디오 재생 </summary>
        public AudioSource PlaySound(string clipName, Vector3 position, float volumeScale = 1f, bool is3D = false)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            if (!clipDatabase.TryGetValue(clipName.ToLower(), out AudioClipData clipData))
            {
                return null;
            }

            AudioSource audioSource = GetPooledAudioSource();
            if (audioSource == null)
            {
                Debug.LogWarning("[AudioManager] 사용 가능한 AudioSource가 없습니다.");
                return null;
            }

            ConfigureAudioSource(audioSource, clipData, volumeScale, is3D);
            
            if (is3D || clipData.is3D)
            {
                audioSource.transform.position = position;
            }

            audioSource.Play();
            StartCoroutine(ReturnToPoolWhenFinished(audioSource, clipData.clip.length));

            return audioSource;
        }

        
        /// <summary> BGM 재생 </summary>
        public void PlayBGM(string clipName, float fadeInDuration = 1f)
        {
            if (!clipDatabase.TryGetValue(clipName.ToLower(), out AudioClipData clipData))
            {
                Debug.LogWarning($"[AudioManager] BGM 클립을 찾을 수 없습니다: {clipName}");
                return;
            }

            // 현재 BGM이 있다면 스택에 푸시
            if (!string.IsNullOrEmpty(currentBGM) && currentBGM != clipName)
            {
                bgmStack.Push(currentBGM);
            }

            currentBGM = clipName;

            if (bgmAudioSource.isPlaying)
            {
                StartCoroutine(CrossfadeBGM(clipData, fadeInDuration));
            }
            else
            {
                bgmAudioSource.clip = clipData.clip;
                bgmAudioSource.volume = clipData.volume;
                bgmAudioSource.Play();
                bgmAudioSource.loop = true;
            }
        }

        /// <summary> BGM 중지 </summary>
        public void StopBGM(float fadeOutDuration = 1f)
        {
            if (bgmAudioSource.isPlaying)
            {
                StartCoroutine(FadeOutAndStop(bgmAudioSource, fadeOutDuration));
            }
            currentBGM = "";
        }

        /// <summary> 이전 BGM으로 복원 </summary>
        public void RestorePreviousBGM(float fadeInDuration = 1f)
        {
            if (bgmStack.Count > 0)
            {
                string previousBGM = bgmStack.Pop();
                currentBGM = "";  
                PlayBGM(previousBGM, fadeInDuration);
            }
            else
            {
                StopBGM(fadeInDuration);
            }
        }

        /// <summary> 현재 재생 중인 BGM의 이름을 반환 </summary>
        public string GetCurrentBGM()
        {
            return currentBGM;
        }

        /// <summary> BGM 스택을 초기화 </summary>
        public void ClearBGMStack()
        {
            bgmStack.Clear();
        }

        /// <summary> 모든 SFX 중지 </summary>
        public void StopAllSFX()
        {
            foreach (var audioSource in activeAudioSources)
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                    ReturnToPool(audioSource);
                }
            }
            activeAudioSources.Clear();
        }

        /// <summary> 카테고리 볼륨 설정 </summary>
        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            volume = Mathf.Clamp01(volume);
            
            switch (category)
            {
                case AudioCategory.BGM:
                    bgmVolume = volume;
                    PlayerPrefs.SetFloat("BGM", volume);
                    break;
                case AudioCategory.SFX:
                    sfxVolume = volume;
                    PlayerPrefs.SetFloat("SFX", volume);
                    break;
                case AudioCategory.UI:
                    uiVolume = volume;
                    PlayerPrefs.SetFloat("UI", volume);
                    break;
                default:
                    $"[AudioManager] 알 수 없는 오디오 카테고리: {category}".DWarning();
                    return;
            }
            
            PlayerPrefs.Save();
            ApplyVolumeSettings(); // AudioMixer를 통해 볼륨 적용
        }

        /// <summary> 마스터 볼륨 설정 </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("Master", masterVolume);
            PlayerPrefs.Save();
            ApplyVolumeSettings();
        }

        /// <summary> 오디오 리스너 토글 </summary>
        public void ToggleAudioListener()
        {
            AudioListener.pause = !AudioListener.pause;
        }

        #endregion

        #region Private Method

        /// <summary> 오디오 소스 풀에서 오디오 소스 가져오기 </summary>
        private AudioSource GetPooledAudioSource()
        {
            AudioSource audioSource = null;

            if (audioSourcePool.TryDequeue(out audioSource))
            {
                audioSource.gameObject.SetActive(true);
                activeAudioSources.Add(audioSource);
                return audioSource;
            }

   
            if (activeAudioSources.Count < maxPoolSize)
            {
                audioSource = CreateNewAudioSource();
                audioSource.gameObject.SetActive(true);
                activeAudioSources.Add(audioSource);
            }
            else
            {
                "[AudioManager] 최대 AudioSource 개수에 도달했습니다.".DWarning();
            }

            return audioSource;
        }

        /// <summary> 오디오 소스 설정 </summary>
        private void ConfigureAudioSource(AudioSource audioSource, AudioClipData clipData, float volumeScale, bool force3D)
        {
            audioSource.clip = clipData.clip;
            audioSource.loop = clipData.loop;
            audioSource.pitch = clipData.pitch;
            
            // 3D/2D 설정
            bool is3D = force3D || clipData.is3D;
            audioSource.spatialBlend = is3D ? 1f : clipData.spatialBlend;
            
            float categoryVolume = GetCategoryVolume(clipData.category);
            float finalVolume = clipData.volume * categoryVolume * masterVolume * volumeScale;
            audioSource.volume = finalVolume;
            
            audioSource.outputAudioMixerGroup = GetMixerGroup(clipData.category);
        }
        private float GetCategoryVolume(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.BGM => bgmVolume,
                AudioCategory.SFX => sfxVolume,
                AudioCategory.UI => uiVolume,
                _ => 1f
            };
        }

        private AudioMixerGroup GetMixerGroup(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.BGM => bgmMixerGroup,
                AudioCategory.SFX => sfxMixerGroup,
                AudioCategory.UI => uiMixerGroup, 
                _ => null
            };
        }

        /// <summary> 오디오 소스 풀로 반환 </summary>
        private void ReturnToPool(AudioSource audioSource)
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
                audioSource.gameObject.SetActive(false);
                activeAudioSources.Remove(audioSource);
                audioSourcePool.Enqueue(audioSource);
            }
        }

        #endregion

        #region Coroutine

        /// <summary> 오디오 소스 풀 반환  </summary>
        private IEnumerator ReturnToPoolWhenFinished(AudioSource audioSource, float clipLength)
        {
            yield return new WaitForSeconds(clipLength + 0.1f);
            ReturnToPool(audioSource);
        }

        /// <summary> 페이드인/아웃 </summary>
        private IEnumerator FadeIn(AudioSource audioSource, float targetVolume, float duration)
        {
            float currentTime = 0f;
            float startVolume = audioSource.volume;

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
                yield return null;
            }

            audioSource.volume = targetVolume;
        }

        private IEnumerator FadeOutAndStop(AudioSource audioSource, float duration)
        {
            float startVolume = audioSource.volume;
            float currentTime = 0f;

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / duration);
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = startVolume;
        }

        /// <summary> BGM 크로스페이드 </summary>
        private IEnumerator CrossfadeBGM(AudioClipData newClipData, float duration)
        {
            float startVolume = bgmAudioSource.volume;
            
            yield return StartCoroutine(FadeOutAndStop(bgmAudioSource, duration * 0.5f));
            
            // 새 BGM 설정 및 재생
            bgmAudioSource.clip = newClipData.clip;
            bgmAudioSource.volume = newClipData.volume;
            bgmAudioSource.Play();
        }

        #endregion
        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopAllCoroutines();
        }
    }
}
