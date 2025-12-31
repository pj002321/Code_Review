using Newtonsoft.Json;

namespace Hunt
{
    public class ChannelModel
    {
        public string channelName;
        public int congestion;
        public int myCharacterCount;
        
        /// <summary>
        /// 혼잡도를 문자열로 변환
        /// </summary>
        public string GetCongestionString()
        {
            return congestion switch
            {
                1 => "원활",
                2 => "보통",
                3 => "혼잡",
                _ => "보통"
            };
        }
    }
}

