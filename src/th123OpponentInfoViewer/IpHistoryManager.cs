using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace th123OpponentInfoViewer
{
    /*
     * ============================================================
     * IP履歴管理
     * ============================================================
     *
     * 接続検知時の
     *
     *   ・日時
     *   ・相手プロファイル
     *   ・その時点のクリップボード内容
     *
     * を OIViewerIpData.txt へ追記する。
     *
     * 同じIPでも接続イベントごとに必ず追記する。
     * 表示時だけ重複排除する。
     * ============================================================
     */
    public class IpHistoryManager
    {
        private readonly string filePath;

        public string FilePath
        {
            get
            {
                return filePath;
            }
        }

        public IpHistoryManager()
        {
            filePath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "OIViewerIpData.txt");
        }

        /*
         * ============================================================
         * 接続イベント追加
         * ============================================================
         *
         * 重複チェックは行わない。
         * 同じ内容でも接続イベントごとに1行追加する。
         */
        public void AddConnection(
            DateTime timestamp,
            string profileName,
            string clipboardText)
        {
            string profile =
                NormalizeValue(
                    profileName);

            string clipboard =
                NormalizeClipboard(
                    clipboardText);

            string line =
                timestamp.ToString(
                    "yyyy-MM-dd HH:mm:ss.fff") +
                "\t" +
                EscapeField(profile) +
                "\t" +
                EscapeField(clipboard);

            try
            {
                File.AppendAllText(
                    filePath,
                    line + Environment.NewLine,
                    new UTF8Encoding(false));
            }
            catch
            {
                /*
                 * 履歴保存失敗で
                 * 本体の動作を止めない。
                 */
            }
        }

        /*
         * ============================================================
         * 指定プロファイルの履歴
         * ============================================================
         *
         * 新しい記録から順番に返す。
         * 同じクリップボード内容は表示上1件にまとめる。
         */
        public List<IpHistoryEntry> GetHistory(
            string profileName)
        {
            List<IpHistoryEntry> result =
                new List<IpHistoryEntry>();

            if (string.IsNullOrWhiteSpace(profileName))
            {
                return result;
            }

            return GetHistory(
                    new List<string>
                    {
                        profileName
                    });
        }

        /*
         * ============================================================
         * 複数プロファイルの履歴
         * ============================================================
         *
         * 同じプレイヤーに属する複数プロファイルをまとめて扱う。
         *
         * 表示上の重複排除キーは
         * 「プロファイル」ではなく「クリップボード内容」。
         *
         * そのため、別プロファイルで同じIPが記録されていても
         * プレイヤー画面では1件だけ表示される。
         * 最新の接続が残る。
         */
        public List<IpHistoryEntry> GetHistory(
            IEnumerable<string> profileNames)
        {
            List<IpHistoryEntry> result =
                new List<IpHistoryEntry>();

            if (profileNames == null)
            {
                return result;
            }

            HashSet<string> profiles =
                new HashSet<string>(
                    profileNames
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(x))
                        .Select(
                            NormalizeValue),
                    StringComparer.Ordinal);

            if (profiles.Count == 0 ||
                !File.Exists(filePath))
            {
                return result;
            }

            try
            {
                string[] lines =
                    File.ReadAllLines(
                        filePath,
                        new UTF8Encoding(false));

                HashSet<string> seenClipboard =
                    new HashSet<string>(
                        StringComparer.Ordinal);

                foreach (string line in lines.Reverse())
                {
                    IpHistoryEntry entry;

                    if (!TryParseLine(
                        line,
                        out entry))
                    {
                        continue;
                    }

                    if (!profiles.Contains(entry.ProfileName))
                    {
                        continue;
                    }

                    /*
                     * 空のクリップボードはIP表示・コピー対象にしない。
                     * ファイル上の接続イベント自体は保存されている。
                     */
                    if (string.IsNullOrWhiteSpace(entry.ClipboardText))
                    {
                        continue;
                    }

                    if (!seenClipboard.Add(entry.ClipboardText))
                    {
                        continue;
                    }

                    result.Add(entry);
                }
            }
            catch
            {
            }

            return result;
        }

        /*
         * ============================================================
         * 最新IP取得
         * ============================================================
         *
         * GetHistory()の先頭が最新のため、それを返す。
         */
        public string GetLatestIp(
            IEnumerable<string> profileNames)
        {
            List<IpHistoryEntry> history =
                GetHistory(profileNames);

            if (history.Count == 0)
            {
                return "";
            }

            return history[0].ClipboardText;
        }

        /*
         * ============================================================
         * 1行解析
         * ============================================================
         */
        private bool TryParseLine(
            string line,
            out IpHistoryEntry entry)
        {
            entry = null;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            string[] parts =
                line.Split(
                    new char[]
                    {
                        '\t'
                    },
                    3);

            if (parts.Length < 3)
            {
                return false;
            }

            DateTime timestamp;

            if (!DateTime.TryParse(
                parts[0],
                out timestamp))
            {
                return false;
            }

            entry =
                new IpHistoryEntry();

            entry.Timestamp =
                timestamp;

            entry.ProfileName =
                UnescapeField(
                    parts[1]);

            entry.ClipboardText =
                UnescapeField(
                    parts[2]);

            return true;
        }

        /*
         * ============================================================
         * 値の正規化
         * ============================================================
         */
        private string NormalizeValue(
            string value)
        {
            if (value == null)
            {
                return "";
            }

            return value.Trim();
        }

        private string NormalizeClipboard(
            string value)
        {
            if (value == null)
            {
                return "";
            }

            return value.Trim();
        }

        /*
         * ============================================================
         * TSV用エスケープ
         * ============================================================
         */
        private string EscapeField(
            string value)
        {
            if (value == null)
            {
                return "";
            }

            return value
                .Replace(
                    "\\",
                    "\\\\")
                .Replace(
                    "\r",
                    "\\r")
                .Replace(
                    "\n",
                    "\\n")
                .Replace(
                    "\t",
                    "\\t");
        }

        /*
         * ============================================================
         * TSV用アンエスケープ
         * ============================================================
         */
        private string UnescapeField(
            string value)
        {
            if (value == null)
            {
                return "";
            }

            StringBuilder result =
                new StringBuilder();

            bool escaped =
                false;

            foreach (char c in value)
            {
                if (escaped)
                {
                    if (c == 't')
                    {
                        result.Append('\t');
                    }
                    else if (c == 'r')
                    {
                        result.Append('\r');
                    }
                    else if (c == 'n')
                    {
                        result.Append('\n');
                    }
                    else if (c == '\\')
                    {
                        result.Append('\\');
                    }
                    else
                    {
                        result.Append('\\');
                        result.Append(c);
                    }

                    escaped =
                        false;

                    continue;
                }

                if (c == '\\')
                {
                    escaped =
                        true;

                    continue;
                }

                result.Append(c);
            }

            if (escaped)
            {
                result.Append('\\');
            }

            return result.ToString();
        }
    }

    public class IpHistoryEntry
    {
        public DateTime Timestamp { get; set; }

        public string ProfileName { get; set; }

        public string ClipboardText { get; set; }
    }
}

