using System;
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
    }
}