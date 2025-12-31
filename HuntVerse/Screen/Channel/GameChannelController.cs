using System.Collections.Generic;
using UnityEngine;
namespace Hunt
{
    public class GameChannelController : MonoBehaviourSingleton<GameChannelController>
    {
        [Header("Channel Field")]
        [SerializeField] private List<GameChannelField> gameChannelFields;

        protected override bool DontDestroy => false;
        
        protected override void Awake()
        {
            base.Awake(); 
        }

        public void OnRecvChannelViewUpdate(ChannelListRequest res)
        {
            $"[Channel] OnRecvChannelViewUpdate".DLog();
            
            if (res?.channels == null || gameChannelFields == null) return;
            
            for (int i = 0; i < res.channels.Count && i < gameChannelFields.Count; i++)
            {
                if (gameChannelFields[i] == null) continue;
                var model = res.channels[i];
                $"[Channel] model: {model.channelName}, Count: {model.myCharacterCount}".DLog();
                gameChannelFields[i].Bind(model);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
