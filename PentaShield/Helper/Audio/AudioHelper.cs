using UnityEngine;

namespace chaos
{
    /// <summary>
    /// 오디오 재생을 위한 정적 헬퍼 클래스
    /// 간단한 호출로 오디오를 재생할 수 있습니다
    /// </summary>
    public static class AudioHelper
    {
        /// <summary>
        /// SFX 재생 (2D)
        /// </summary>
        /// <param name="clipName">클립 이름</param>
        /// <param name="volume">볼륨 배율</param>
        public static AudioSource PlaySFX(string clipName, float volume = 1f)
        {
            return AudioManager.Shared?.PlaySound(clipName, volume);
        }

        /// <summary>
        /// 3D SFX 재생
        /// </summary>
        /// <param name="clipName">클립 이름</param>
        /// <param name="position">재생 위치</param>
        /// <param name="volume">볼륨 배율</param>
        public static AudioSource PlaySFX3D(string clipName, Vector3 position, float volume = 1f)
        {
            return AudioManager.Shared?.PlaySound(clipName, position, volume, true);
        }

        /// <summary>
        /// UI 사운드 재생
        /// </summary>
        /// <param name="clipName">클립 이름</param>
        /// <param name="volume">볼륨 배율</param>
        public static AudioSource PlayUI(string clipName, float volume = 1f)
        {
            return AudioManager.Shared?.PlaySound(clipName, volume);
        }

        /// <summary>
        /// BGM 재생
        /// </summary>
        /// <param name="clipName">BGM 클립 이름</param>
        /// <param name="fadeInTime">페이드인 시간</param>
        public static void PlayBGM(string clipName, float fadeInTime = 1f)
        {
            AudioManager.Shared?.PlayBGM(clipName, fadeInTime);
        }

        /// <summary>
        /// BGM 중지
        /// </summary>
        /// <param name="fadeOutTime">페이드아웃 시간</param>
        public static void StopBGM(float fadeOutTime = 1f)
        {
            AudioManager.Shared?.StopBGM(fadeOutTime);
        }

        /// <summary>
        /// 이전 BGM으로 복원
        /// </summary>
        /// <param name="fadeInTime">페이드인 시간</param>
        public static void RestorePreviousBGM(float fadeInTime = 1f)
        {
            AudioManager.Shared?.RestorePreviousBGM(fadeInTime);
        }

        /// <summary>
        /// 현재 재생 중인 BGM 이름 반환
        /// </summary>
        /// <returns>현재 BGM 클립 이름</returns>
        public static string GetCurrentBGM()
        {
            return AudioManager.Shared?.GetCurrentBGM() ?? "";
        }

        /// <summary>
        /// 모든 SFX 중지
        /// </summary>
        public static void StopAllSFX()
        {
            AudioManager.Shared?.StopAllSFX();
        }

        /// <summary>
        /// Transform 위치에서 3D 사운드 재생
        /// </summary>
        /// <param name="clipName">클립 이름</param>
        /// <param name="transform">재생할 Transform</param>
        /// <param name="volume">볼륨 배율</param>
        public static AudioSource PlaySFXAtTransform(string clipName, Transform transform, float volume = 1f)
        {
            if (transform == null) return null;
            return PlaySFX3D(clipName, transform.position, volume);
        }

        /// <summary>
        /// GameObject 위치에서 3D 사운드 재생
        /// </summary>
        /// <param name="clipName">클립 이름</param>
        /// <param name="gameObject">재생할 GameObject</param>
        /// <param name="volume">볼륨 배율</param>
        public static AudioSource PlaySFXAtGameObject(string clipName, GameObject gameObject, float volume = 1f)
        {
            if (gameObject == null) return null;
            return PlaySFX3D(clipName, gameObject.transform.position, volume);
        }
    }

    /// <summary>
    /// 오디오 상수 정의
    /// 클립 이름을 상수로 관리하여 오타를 방지합니다
    /// </summary>
    public static class AudioConst
    {
        // SFX
        public const string HIT_IMPACT = "hit_impact@sound";
        public const string ENEMY_DEATH = "enemy_death";
       
        // ...
        
    }
}
