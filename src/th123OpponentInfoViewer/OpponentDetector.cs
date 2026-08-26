using System;
using System.Diagnostics;

namespace th123OpponentInfoViewer
{
    public class OpponentDetector
    {
        private readonly MemoryReader memoryReader =
            new MemoryReader();

        /*
         * true  = クライアント
         * false = ホスト
         *
         * SceneID 9を検知したらtrue。
         * SceneID 7以下になるまで保持する。
         */
        private bool isClientMode = false;

        public OpponentInfo GetOpponent()
        {
            OpponentInfo info =
                new OpponentInfo();

            Process process =
                memoryReader.GetGameProcess();

            if (process == null)
            {
                return info;
            }

            IntPtr handle =
                memoryReader.OpenGameProcess(process);

            if (handle == IntPtr.Zero)
            {
                return info;
            }

            uint sceneId =
                memoryReader.ReadUInt32(
                    handle,
                    Constants.SCENEID);

            info.SceneId =
                sceneId;

            /*
             * SceneID 7以下になったら
             * クライアント状態をリセット。
             */
            if (sceneId <= 7)
            {
                isClientMode = false;
            }

            /*
             * SceneID 9はクライアント側。
             *
             * 一度9を検知したら、
             * 7以下になるまでクライアント扱い。
             */
            if (sceneId == 9)
            {
                isClientMode = true;
            }

            /*
             * --------------------------------
             * 観戦
             * --------------------------------
             *
             * SceneID 12 = 観戦ロード中
             * SceneID 15 = 観戦中
             */
            if (sceneId == 12 ||
                sceneId == 15)
            {
                info.IsWatching = true;

                ReadWatchingProfiles(
                    handle,
                    info);

                return info;
            }

            /*
             * --------------------------------
             * 通常対戦
             * --------------------------------
             *
             * SceneID 8～11
             * SceneID 13～14
             *
             * SceneID 12は観戦なので除外。
             */
            if ((sceneId >= 8 &&
                 sceneId <= 11) ||
                (sceneId >= 13 &&
                 sceneId <= 14))
            {
                info.IsCharacterSelect = true;

                ReadBattleOpponent(
                    handle,
                    info);

                return info;
            }

            /*
             * その他は待機。
             */
            return info;
        }

        private void ReadBattleOpponent(
            IntPtr handle,
            OpponentInfo info)
        {
            uint pnet =
                memoryReader.ReadUInt32(
                    handle,
                    Constants.PNETOBJECT);

            if (pnet == 0)
            {
                return;
            }

            uint profileOffset;
            uint ipOffset;

            /*
             * クライアント
             *
             * 1P = 相手
             * 2P = 自分
             */
            if (isClientMode)
            {
                profileOffset =
                    Constants.LPROFOFS;

                ipOffset =
                    Constants.IP1OFS;
            }
            /*
             * ホスト
             *
             * 1P = 自分
             * 2P = 相手
             */
            else
            {
                profileOffset =
                    Constants.RPROFOFS;

                ipOffset =
                    Constants.IP2OFS;
            }

            info.ProfileName =
                memoryReader.ReadString(
                    handle,
                    pnet + profileOffset,
                    Constants.PROFSZ);

            /*
             * 現在IPは表示しないが、
             * OpponentInfoには保存。
             */
            uint ip =
                memoryReader.ReadUInt32(
                    handle,
                    pnet + ipOffset);

            info.IPAddress =
                ConvertIP(ip);
        }

        private void ReadWatchingProfiles(
            IntPtr handle,
            OpponentInfo info)
        {
            uint pnet =
                memoryReader.ReadUInt32(
                    handle,
                    Constants.PNETOBJECT);

            if (pnet == 0)
            {
                return;
            }

            /*
             * 観戦では
             *
             * L = 1P
             * R = 2P
             */
            info.Player1ProfileName =
                memoryReader.ReadString(
                    handle,
                    pnet + Constants.LPROFOFS,
                    Constants.PROFSZ);

            info.Player2ProfileName =
                memoryReader.ReadString(
                    handle,
                    pnet + Constants.RPROFOFS,
                    Constants.PROFSZ);
        }

        private string ConvertIP(
            uint ip)
        {
            byte[] bytes =
                BitConverter.GetBytes(ip);

            return
                bytes[0] + "." +
                bytes[1] + "." +
                bytes[2] + "." +
                bytes[3];
        }
    }
}