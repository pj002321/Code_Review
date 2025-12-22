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
            if (!IsActive) return;
            string audioKey = AudioKeyConst.GetSfxKey(sfxType);
            AudioHelper.Shared.PlaySfx(audioKey, volumeScale);
        }
    }
}

