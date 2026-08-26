using System;

namespace th123OpponentInfoViewer
{
    public class OpponentInfo
    {
        public bool IsCharacterSelect { get; set; }

        public bool IsWatching { get; set; }

        public uint SceneId { get; set; }

        // 通常対戦時の相手
        public string ProfileName { get; set; }

        // 観戦時の1P
        public string Player1ProfileName { get; set; }

        // 観戦時の2P
        public string Player2ProfileName { get; set; }

        // 1P側のIP
        // 現在は表示には使用しない
        public string IPAddress { get; set; }
    }
}