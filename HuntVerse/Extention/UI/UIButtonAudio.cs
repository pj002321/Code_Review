using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class UIButtonAudio : UIButtonControlBase
    {
        [SerializeField] private AudioType sfxType = AudioType.SFX_CHANNEL_SELECT;
        [SerializeField] private float volumeScale = 1.0f;
        protected override void OnClickEvent()
        {
            if (AudioHelper.Shared == null) return;
            string audioKey = AudioKeyConst.GetSfxKey(sfxType);
            if (string.IsNullOrEmpty(audioKey)) return;
            AudioHelper.Shared.PlaySfx(audioKey, volumeScale);
        }
    }
}

