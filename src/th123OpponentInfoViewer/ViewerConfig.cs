using System;
using System.Collections.Generic;
using System.IO;

namespace th123OpponentInfoViewer
{
    public class ViewerConfig
    {
        public string DatabaseFileName { get; private set; }

        public float ViewerFontSize { get; private set; }

        public float ProfileSearchFontSize { get; private set; }

        public bool DefaultCheckAsobby { get; private set; }

        public bool DefaultShowIpPort { get; private set; }

        public bool DefaultCheckMatchRecord { get; private set; }

        public bool DefaultShowOverlay { get; private set; }

        public int PlayerStatsRecentDays { get; private set; }

        public int PlayerStatsRecentMatches { get; private set; }

        public int PlayerStatsRankingMinMatches { get; private set; }

        /*
         * プレイヤー表のランキング条件。
         *
         * 0 = すべて表示
         * 1 = 全敗相手非表示
         * 2 = 全敗相手のみ表示
         */
        public int PlayerStatsRankingCondition { get; private set; }

        /*
         * exeが入っているフォルダ。
         *
         * 例：
         * th4_5888\additional_tool\
         */
        public string ToolDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        /*
         * iniファイルの場所。
         */
        public string IniPath
        {
            get
            {
                return Path.Combine(
                    ToolDirectory,
                    "th123OpponentInfoViewer.ini");
            }
        }

        /*
         * DBはツールフォルダの1つ上。
         *
         * 例：
         * th4_5888\Default.db
         */
        public string DatabasePath
        {
            get
            {
                string parentDirectory =
                    Directory.GetParent(
                        ToolDirectory.TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar))
                    .FullName;

                return Path.Combine(
                    parentDirectory,
                    DatabaseFileName);
            }
        }

        public ViewerConfig()
        {
            /*
             * iniが存在しない場合の初期値。
             */
            DatabaseFileName =
                "Default.db";

            ViewerFontSize =
                10.0f;

            ProfileSearchFontSize =
                10.0f;

            DefaultCheckAsobby =
                true;

            DefaultShowIpPort =
                true;

            DefaultCheckMatchRecord =
                true;

            DefaultShowOverlay =
                true;

            PlayerStatsRecentDays =
                30;

            PlayerStatsRecentMatches =
                100;

            PlayerStatsRankingMinMatches =
                10;

            PlayerStatsRankingCondition =
                0;

            Load();
        }

        /*
         * ini読み込み。
         */
        private void Load()
        {
            if (!File.Exists(IniPath))
            {
                return;
            }

            try
            {
                string[] lines =
                    File.ReadAllLines(
                        IniPath);

                bool inGeneral =
                    false;

                foreach (string rawLine in lines)
                {
                    string line =
                        rawLine.Trim();

                    if (line.Length == 0)
                    {
                        continue;
                    }

                    if (line.StartsWith(";") ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[") &&
                        line.EndsWith("]"))
                    {
                        string section =
                            line.Substring(
                                1,
                                line.Length - 2)
                            .Trim();

                        inGeneral =
                            string.Equals(
                                section,
                                "General",
                                StringComparison.OrdinalIgnoreCase);

                        continue;
                    }

                    if (!inGeneral)
                    {
                        continue;
                    }

                    int equalIndex =
                        line.IndexOf('=');

                    if (equalIndex <= 0)
                    {
                        continue;
                    }

                    string key =
                        line.Substring(
                            0,
                            equalIndex)
                        .Trim();

                    string value =
                        line.Substring(
                            equalIndex + 1)
                        .Trim();

                    if (string.Equals(
                        key,
                        "DatabaseFileName",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        if (value.Length > 0)
                        {
                            /*
                             * DBの場所は指定させない。
                             * ファイル名だけ許可。
                             */
                            DatabaseFileName =
                                Path.GetFileName(value);
                        }
                    }
                    else if (string.Equals(
                        key,
                        "ViewerFontSize",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        float size;

                        if (float.TryParse(
                            value,
                            out size) &&
                            size > 0)
                        {
                            ViewerFontSize =
                                size;
                        }
                    }
                    else if (string.Equals(
                        key,
                        "ProfileSearchFontSize",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        float size;

                        if (float.TryParse(
                            value,
                            out size) &&
                            size > 0)
                        {
                            ProfileSearchFontSize =
                                size;
                        }
                    }
                    else if (string.Equals(
                        key,
                        "DefaultCheckAsobby",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        bool valueBool;

                        if (bool.TryParse(
                            value,
                            out valueBool))
                        {
                            DefaultCheckAsobby =
                                valueBool;
                        }
                    }
                    else if (string.Equals(
                        key,
                        "DefaultShowIpPort",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        bool valueBool;

                        if (bool.TryParse(
                            value,
                            out valueBool))
                        {
                            DefaultShowIpPort =
                                valueBool;
                        }
                    }
                    else if (string.Equals(
                        key,
                        "DefaultCheckMatchRecord",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        bool valueBool;

                        if (bool.TryParse(
                            value,
                            out valueBool))
                        {
                            DefaultCheckMatchRecord =
                                valueBool;
                        }
                    }
                    else if (string.Equals(
                        key,
                        "DefaultShowOverlay",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        bool valueBool;

                        if (bool.TryParse(
                            value,
                            out valueBool))
                        {
                            DefaultShowOverlay =
                                valueBool;
                        }
                    }
                    else if (string.Equals(
                        key,
                        "PlayerStatsRecentDays",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        int number;

                        if (int.TryParse(
                            value,
                            out number) &&
                            number > 0)
                        {
                            PlayerStatsRecentDays =
                                number;
                        }
                    }
                    else if (string.Equals(
                        key,
                        "PlayerStatsRecentMatches",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        int number;

                        if (int.TryParse(
                            value,
                            out number) &&
                            number > 0)
                        {
                            PlayerStatsRecentMatches =
                                number;
                        }
                    }
                    else if (string.Equals(
                        key,
                        "PlayerStatsRankingMinMatches",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        int number;

                        if (int.TryParse(
                            value,
                            out number) &&
                            number > 0)
                        {
                            PlayerStatsRankingMinMatches =
                                number;
                        }
                    }
                    else if (string.Equals(
                        key,
                        "PlayerStatsRankingCondition",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        int number;

                        if (int.TryParse(
                            value,
                            out number) &&
                            number >= 0 &&
                            number <= 2)
                        {
                            PlayerStatsRankingCondition =
                                number;
                        }
                    }
                }
            }
            catch
            {
                /*
                 * ini読み込み失敗時は
                 * 初期値のまま使用。
                 */
            }
        }
        /*
         * --------------------------------
         * プレイヤー情報設定保存
         * --------------------------------
         */
        public void SavePlayerStatsSettings(
            int recentDays,
            int recentMatches,
            int rankingMinMatches)
        {
            SavePlayerStatsSettings(
                recentDays,
                recentMatches,
                rankingMinMatches,
                PlayerStatsRankingCondition);
        }

        /*
         * ランキング条件まで含めてプレイヤー情報設定を保存する。
         *
         * rankingCondition:
         *   0 = すべて表示
         *   1 = 全敗相手非表示
         *   2 = 全敗相手のみ表示
         */
        public void SavePlayerStatsSettings(
            int recentDays,
            int recentMatches,
            int rankingMinMatches,
            int rankingCondition)
        {
            if (recentDays <= 0)
            {
                recentDays = 30;
            }

            if (recentMatches <= 0)
            {
                recentMatches = 100;
            }

            if (rankingMinMatches <= 0)
            {
                rankingMinMatches = 10;
            }

            if (rankingCondition < 0 ||
                rankingCondition > 2)
            {
                rankingCondition = 0;
            }

            List<string> lines =
                new List<string>();

            if (File.Exists(IniPath))
            {
                try
                {
                    lines =
                        new List<string>(
                            File.ReadAllLines(
                                IniPath));
                }
                catch
                {
                    lines =
                        new List<string>();
                }
            }

            bool inGeneral = false;
            bool foundRecentDays = false;
            bool foundRecentMatches = false;
            bool foundMinMatches = false;
            bool foundRankingCondition = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string line =
                    lines[i].Trim();

                if (line.StartsWith("[") &&
                    line.EndsWith("]"))
                {
                    string section =
                        line.Substring(
                            1,
                            line.Length - 2)
                        .Trim();

                    inGeneral =
                        string.Equals(
                            section,
                            "General",
                            StringComparison.OrdinalIgnoreCase);

                    continue;
                }

                if (!inGeneral)
                {
                    continue;
                }

                int equalIndex =
                    line.IndexOf('=');

                if (equalIndex <= 0)
                {
                    continue;
                }

                string key =
                    line.Substring(
                        0,
                        equalIndex)
                    .Trim();

                if (string.Equals(
                    key,
                    "PlayerStatsRecentDays",
                    StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] =
                        "PlayerStatsRecentDays=" +
                        recentDays;

                    foundRecentDays =
                        true;
                }
                else if (string.Equals(
                    key,
                    "PlayerStatsRecentMatches",
                    StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] =
                        "PlayerStatsRecentMatches=" +
                        recentMatches;

                    foundRecentMatches =
                        true;
                }
                else if (string.Equals(
                    key,
                    "PlayerStatsRankingMinMatches",
                    StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] =
                        "PlayerStatsRankingMinMatches=" +
                        rankingMinMatches;

                    foundMinMatches =
                        true;
                }
                else if (string.Equals(
                    key,
                    "PlayerStatsRankingCondition",
                    StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] =
                        "PlayerStatsRankingCondition=" +
                        rankingCondition;

                    foundRankingCondition =
                        true;
                }
            }

            int generalSectionIndex = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                string line =
                    lines[i].Trim();

                if (string.Equals(
                    line,
                    "[General]",
                    StringComparison.OrdinalIgnoreCase))
                {
                    generalSectionIndex =
                        i;

                    break;
                }
            }

            if (generalSectionIndex < 0)
            {
                if (lines.Count > 0 &&
                    lines[lines.Count - 1].Trim().Length != 0)
                {
                    lines.Add("");
                }

                lines.Add("[General]");

                generalSectionIndex =
                    lines.Count - 1;
            }

            int insertIndex =
                generalSectionIndex + 1;

            while (insertIndex < lines.Count)
            {
                string line =
                    lines[insertIndex].Trim();

                if (line.StartsWith("[") &&
                    line.EndsWith("]"))
                {
                    break;
                }

                insertIndex++;
            }

            if (!foundRecentDays)
            {
                lines.Insert(
                    insertIndex,
                    "PlayerStatsRecentDays=" +
                    recentDays);

                insertIndex++;
            }

            if (!foundRecentMatches)
            {
                lines.Insert(
                    insertIndex,
                    "PlayerStatsRecentMatches=" +
                    recentMatches);

                insertIndex++;
            }

            if (!foundMinMatches)
            {
                lines.Insert(
                    insertIndex,
                    "PlayerStatsRankingMinMatches=" +
                    rankingMinMatches);

                insertIndex++;
            }

            if (!foundRankingCondition)
            {
                lines.Insert(
                    insertIndex,
                    "PlayerStatsRankingCondition=" +
                    rankingCondition);
            }

            try
            {
                File.WriteAllLines(
                    IniPath,
                    lines.ToArray());

                PlayerStatsRecentDays =
                    recentDays;

                PlayerStatsRecentMatches =
                    recentMatches;

                PlayerStatsRankingMinMatches =
                    rankingMinMatches;

                PlayerStatsRankingCondition =
                    rankingCondition;
            }
            catch
            {
            }
        }

    }
}

