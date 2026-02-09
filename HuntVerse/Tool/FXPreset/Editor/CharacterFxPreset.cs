using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 캐릭터별 FX/SFX 프리셋. HuntPreset 에디터 툴로 세팅.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterFxPreset", menuName = "Hunt/CharacterFxPreset")]
    public class CharacterFxPreset : ScriptableObject
    {
        [Header("액터 정보")]
        public ActorCategory actorCategory;
        public GameObject characterPrefab;
        public Sprite previewSprite;
        public ClassType classType;

        [Header("클립별 FX 설정")]
        public List<ClipFxData> clipFxDataList = new List<ClipFxData>();

        [Header("Hit 시 FX 설정")]
        public HitFxData hitFxData = new HitFxData();
    }

    [Serializable]
    public class ClipFxData
    {
        public string clipName;
        public List<FxTiming> fxTimings = new List<FxTiming>();
    }

    [Serializable]
    public class FxTiming
    {
        [Tooltip("초 단위 시간")]
        public float timeInSeconds;
        public VfxType vfxType;
        public AudioType audioType = AudioType.SFX_HOVER;
        public bool attachHit;
    }

    [Serializable]
    public class HitFxData
    {
        public List<string> vfxKeys = new List<string>();
        public List<AudioType> audioTypes = new List<AudioType>();
    }
}
