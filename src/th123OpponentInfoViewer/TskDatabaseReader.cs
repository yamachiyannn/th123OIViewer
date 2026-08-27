using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace th123OpponentInfoViewer
{
    public class TskDatabaseReader
    {
        private readonly string databasePath;

        /*
         * --------------------------------
         * Shift-JIS
         * --------------------------------
         *
         * .NET Framework 4.7.2
         *
         * Default.dbのプロファイル名は
         * Shift-JISで保存されているため使用する。
         */
        private static readonly Encoding ShiftJis =
            Encoding.GetEncoding(932);

        /*
         * --------------------------------
         * コンストラクタ
         * --------------------------------
         *
         * DBの場所はViewerConfigに任せる。
         *
         * ViewerConfigでは
         *
         * exeの入っているフォルダ
         *     ↓
         * 1つ上
         *     ↓
         * DatabaseFileName
         *
         * となる。
         *
         * 例：
         *
         * th4_5888\
         *     Default.db
         *
         *     additional_tool\
         *         th123OpponentInfoViewer.exe
         */
        public TskDatabaseReader()
        {
            ViewerConfig config =
                new ViewerConfig();

            databasePath =
                config.DatabasePath;
        }

        /*
         * --------------------------------
         * 任意のDBを指定する場合
         * --------------------------------
         *
         * テストや特殊用途用。
         *
         * 通常のViewerでは
         * 上のデフォルトコンストラクタを使用する。
         */
        public TskDatabaseReader(
            string path)
        {
            databasePath =
                path;
        }

        /*
         * --------------------------------
         * DB存在確認
         * --------------------------------
         */
        public bool DatabaseExists
        {
            get
            {
                return File.Exists(
                    databasePath);
            }
        }

        /*
         * --------------------------------
         * DBパス
         * --------------------------------
         */
        public string DatabasePath
        {
            get
            {
                return databasePath;
            }
        }

        /*
         * --------------------------------
         * 対戦記録件数
         * --------------------------------
         */
        public int GetMatchCount()
        {
            if (!File.Exists(databasePath))
            {
                return -1;
            }

            using (SQLiteConnection connection =
                OpenConnection())
            {
                using (SQLiteCommand command =
                    new SQLiteCommand(
                        "SELECT COUNT(*) " +
                        "FROM trackrecord123",
                        connection))
                {
                    object value =
                        command.ExecuteScalar();

                    if (value == null ||
                        value == DBNull.Value)
                    {
                        return 0;
                    }

                    return Convert.ToInt32(value);
                }
            }
        }

        /*
         * --------------------------------
         * P2プロファイル一覧
         * --------------------------------
         *
         * Default.dbのp2nameのみ取得。
         *
         * 同じ名前は1回だけ。
         */
        public List<string> GetP2ProfileNames()
        {
            List<string> names =
                new List<string>();

            if (!File.Exists(databasePath))
            {
                return names;
            }

            using (SQLiteConnection connection =
                OpenConnection())
            {
                const string sql =
                    "SELECT CAST(p2name AS BLOB) " +
                    "FROM trackrecord123";

                using (SQLiteCommand command =
                    new SQLiteCommand(
                        sql,
                        connection))
                {
                    using (SQLiteDataReader reader =
                        command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name =
                                ReadShiftJisBlob(
                                    reader,
                                    0);

                            if (string.IsNullOrWhiteSpace(
                                name))
                            {
                                continue;
                            }

                            if (!names.Contains(name))
                            {
                                names.Add(name);
                            }
                        }
                    }
                }
            }

            return
                names
                    .OrderBy(x => x)
                    .ToList();
        }

        /*
         * --------------------------------
         * P2キャラクター使用回数
         * --------------------------------
         *
         * 互換用。
         *
         * Key   = キャラクターID
         * Value = 使用回数
         */
        public Dictionary<int, int>
            GetP2CharacterUsage(
                string profileName)
        {
            Dictionary<int, int> result =
                new Dictionary<int, int>();

            if (string.IsNullOrWhiteSpace(
                profileName))
            {
                return result;
            }

            if (!File.Exists(databasePath))
            {
                return result;
            }

            using (SQLiteConnection connection =
                OpenConnection())
            {
                const string sql =
                    "SELECT " +
                    "CAST(p2name AS BLOB), " +
                    "p2id " +
                    "FROM trackrecord123";

                using (SQLiteCommand command =
                    new SQLiteCommand(
                        sql,
                        connection))
                {
                    using (SQLiteDataReader reader =
                        command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string p2Name =
                                ReadShiftJisBlob(
                                    reader,
                                    0);

                            if (!ProfileNameEquals(
                                p2Name,
                                profileName))
                            {
                                continue;
                            }

                            int characterId =
                                ReadInt(
                                    reader,
                                    1);

                            if (!result.ContainsKey(
                                characterId))
                            {
                                result.Add(
                                    characterId,
                                    0);
                            }

                            result[characterId]++;
                        }
                    }
                }
            }

            return result;
        }

        /*
         * --------------------------------
         * P2キャラクター詳細集計
         * --------------------------------
         *
         * キャラクターごとに
         *
         * Matches = 使用回数
         * Wins    = そのキャラでP2が勝利
         * Losses  = そのキャラでP2が敗北
         */
        public Dictionary<int, TskCharacterStats>
            GetP2CharacterStats(
                string profileName)
        {
            Dictionary<int, TskCharacterStats> result =
                new Dictionary<int, TskCharacterStats>();

            if (string.IsNullOrWhiteSpace(
                profileName))
            {
                return result;
            }

            if (!File.Exists(databasePath))
            {
                return result;
            }

            using (SQLiteConnection connection =
                OpenConnection())
            {
                const string sql =
                    "SELECT " +
                    "CAST(p2name AS BLOB), " +
                    "p2id, " +
                    "p1win, " +
                    "p2win " +
                    "FROM trackrecord123";

                using (SQLiteCommand command =
                    new SQLiteCommand(
                        sql,
                        connection))
                {
                    using (SQLiteDataReader reader =
                        command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string p2Name =
                                ReadShiftJisBlob(
                                    reader,
                                    0);

                            if (!ProfileNameEquals(
                                p2Name,
                                profileName))
                            {
                                continue;
                            }

                            int characterId =
                                ReadInt(
                                    reader,
                                    1);

                            int p1win =
                                ReadInt(
                                    reader,
                                    2);

                            int p2win =
                                ReadInt(
                                    reader,
                                    3);

                            TskCharacterStats stats;

                            if (!result.TryGetValue(
                                characterId,
                                out stats))
                            {
                                stats =
                                    new TskCharacterStats();

                                result.Add(
                                    characterId,
                                    stats);
                            }

                            stats.Matches++;

                            /*
                             * P2が2ラウンド取った
                             * → プロファイル側の勝利。
                             */
                            if (p2win >= 2)
                            {
                                stats.Wins++;
                            }

                            /*
                             * P1が2ラウンド取った
                             * → プロファイル側の敗北。
                             */
                            if (p1win >= 2)
                            {
                                stats.Losses++;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /*
         * --------------------------------
         * 通常対戦用
         * --------------------------------
         *
         * 相手プロファイルを
         * DBのp2nameと照合。
         */
        public TskOpponentStats GetOpponentStats(
            string opponentProfileName)
        {
            return GetPlayerStats(
                opponentProfileName);
        }

        /*
         * --------------------------------
         * 指定プロファイルの対戦記録
         * --------------------------------
         *
         * DB上のp2nameとして
         * 記録されている対戦を取得。
         *
         * このツールでは
         *
         * p1 = 自分
         * p2 = 検索対象プロファイル
         *
         * として扱う。
         */
        public TskOpponentStats GetPlayerStats(
            string profileName)
        {
            TskOpponentStats stats =
                new TskOpponentStats();

            stats.ProfileName =
                profileName ?? "";

            if (string.IsNullOrWhiteSpace(
                profileName))
            {
                return stats;
            }

            if (!File.Exists(databasePath))
            {
                throw new FileNotFoundException(
                    "Default.dbが見つかりません。",
                    databasePath);
            }

            List<TskMatchRecord> records =
                new List<TskMatchRecord>();

            /*
             * 毎回DBを開き直す。
             *
             * Read Onlyなので
             * Default.dbには書き込まない。
             */
            using (SQLiteConnection connection =
                OpenConnection())
            {
                const string sql =
                    "SELECT " +
                    "timestamp, " +
                    "CAST(p1name AS BLOB), " +
                    "p1id, " +
                    "p1win, " +
                    "CAST(p2name AS BLOB), " +
                    "p2id, " +
                    "p2win " +
                    "FROM trackrecord123";

                using (SQLiteCommand command =
                    new SQLiteCommand(
                        sql,
                        connection))
                {
                    using (SQLiteDataReader reader =
                        command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string p2Name =
                                ReadShiftJisBlob(
                                    reader,
                                    4);

                            if (!ProfileNameEquals(
                                p2Name,
                                profileName))
                            {
                                continue;
                            }

                            TskMatchRecord record =
                                new TskMatchRecord();

                            record.DateTime =
                                ReadTimestamp(
                                    reader,
                                    0);

                            if (record.DateTime ==
                                DateTime.MinValue)
                            {
                                continue;
                            }

                            record.P1Name =
                                ReadShiftJisBlob(
                                    reader,
                                    1);

                            record.P1CharacterId =
                                ReadInt(
                                    reader,
                                    2);

                            record.P1RoundCount =
                                ReadInt(
                                    reader,
                                    3);

                            record.P2Name =
                                p2Name;

                            record.P2CharacterId =
                                ReadInt(
                                    reader,
                                    5);

                            record.P2RoundCount =
                                ReadInt(
                                    reader,
                                    6);

                            records.Add(
                                record);
                        }
                    }
                }
            }

            /*
             * --------------------------------
             * 該当記録なし
             * --------------------------------
             */
            if (records.Count == 0)
            {
                stats.HasRecords =
                    false;

                return stats;
            }

            records =
                records
                    .OrderBy(
                        x => x.DateTime)
                    .ToList();

            stats.HasRecords =
                true;

            /*
             * --------------------------------
             * 通算
             * --------------------------------
             */
            stats.TotalMatches =
                records.Count;

            /*
             * 検索対象の勝利。
             */
            stats.TotalWins =
                records.Count(
                    x => x.P2RoundCount >= 2);

            /*
             * 自分の勝利。
             */
            stats.TotalLosses =
                records.Count(
                    x => x.P1RoundCount >= 2);

            /*
             * --------------------------------
             * ラウンド情報取得確認
             * --------------------------------
             *
             * p1win / p2win のどちらかに
             * 1以上の値が存在すれば、
             * ラウンド情報は取得できている。
             *
             * p1win=0 / p2win=2
             * のような「一度も勝っていない相手」は
             * 正常なラウンド情報なので警告しない。
             */
            stats.IsHasUnrecordedWinningRound =
                records.Count > 0 &&
                records.All(
                    x => x.P1RoundCount == 0 &&
                         x.P2RoundCount == 2);

            /*
             * --------------------------------
             * 初対戦
             * --------------------------------
             */
            stats.FirstMatchDate =
                records.First().DateTime;

            /*
             * --------------------------------
             * 最終対戦
             * --------------------------------
             *
             * 今日の記録は除外。
             */
            DateTime today =
                DateTime.Now.Date;

            TskMatchRecord lastBeforeToday =
                records
                    .Where(
                        x => x.DateTime.Date < today)
                    .OrderByDescending(
                        x => x.DateTime)
                    .FirstOrDefault();

            if (lastBeforeToday != null)
            {
                stats.LastMatchDate =
                    lastBeforeToday.DateTime;
            }

            /*
             * --------------------------------
             * 最後に自分が勝った日時
             * --------------------------------
             */
            TskMatchRecord lastWin =
                records
                    .Where(
                        x => x.P1RoundCount >= 2)
                    .OrderByDescending(
                        x => x.DateTime)
                    .FirstOrDefault();

            if (lastWin != null)
            {
                stats.LastWinDate =
                    lastWin.DateTime;
            }

            /*
             * --------------------------------
             * 最後に自分が負けた日時
             * --------------------------------
             */
            TskMatchRecord lastLoss =
                records
                    .Where(
                        x => x.P2RoundCount >= 2)
                    .OrderByDescending(
                        x => x.DateTime)
                    .FirstOrDefault();

            if (lastLoss != null)
            {
                stats.LastLossDate =
                    lastLoss.DateTime;
            }

            /*
             * --------------------------------
             * 過去30戦
             * --------------------------------
             */
            List<TskMatchRecord> last30 =
                records
                    .OrderByDescending(
                        x => x.DateTime)
                    .Take(30)
                    .ToList();

            stats.Last30Matches =
                last30.Count;

            stats.Last30Wins =
                last30.Count(
                    x => x.P2RoundCount >= 2);

            stats.Last30Losses =
                last30.Count(
                    x => x.P1RoundCount >= 2);

            /*
             * --------------------------------
             * 過去100戦
             * --------------------------------
             */
            List<TskMatchRecord> last100 =
                records
                    .OrderByDescending(
                        x => x.DateTime)
                    .Take(100)
                    .ToList();

            stats.Last100Matches =
                last100.Count;

            stats.Last100Wins =
                last100.Count(
                    x => x.P2RoundCount >= 2);

            stats.Last100Losses =
                last100.Count(
                    x => x.P1RoundCount >= 2);

            /*
             * --------------------------------
             * 過去1か月
             * --------------------------------
             */
            DateTime oneMonthAgo =
                DateTime.Now.AddMonths(-1);

            List<TskMatchRecord> lastMonth =
                records
                    .Where(
                        x => x.DateTime >=
                             oneMonthAgo)
                    .ToList();

            stats.LastMonthMatches =
                lastMonth.Count;

            stats.LastMonthWins =
                lastMonth.Count(
                    x => x.P2RoundCount >= 2);

            stats.LastMonthLosses =
                lastMonth.Count(
                    x => x.P1RoundCount >= 2);

            /*
             * --------------------------------
             * メインキャラ
             * --------------------------------
             *
             * 100戦未満
             * → 全対戦
             *
             * 100戦以上
             * → 直近100戦
             */
            List<TskMatchRecord>
                characterRecords;

            if (records.Count < 100)
            {
                characterRecords =
                    records
                        .OrderByDescending(
                            x => x.DateTime)
                        .ToList();
            }
            else
            {
                characterRecords =
                    records
                        .OrderByDescending(
                            x => x.DateTime)
                        .Take(100)
                        .ToList();
            }

            var mainCharacter =
                characterRecords
                    .GroupBy(
                        x => x.P2CharacterId)
                    .OrderByDescending(
                        x => x.Count())
                    .ThenBy(
                        x => x.Key)
                    .FirstOrDefault();

            if (mainCharacter != null)
            {
                stats.MainCharacterId =
                    mainCharacter.Key;

                stats.MainCharacterCount =
                    mainCharacter.Count();
            }

            return stats;
        }

        /*
         * --------------------------------
         * SQLite接続
         * --------------------------------
         *
         * Read Only=True。
         *
         * Default.dbを書き換えない。
         */
        private SQLiteConnection OpenConnection()
        {
            SQLiteConnection connection =
                new SQLiteConnection(
                    "Data Source=" +
                    databasePath +
                    ";" +
                    "Version=3;" +
                    "Read Only=True;");

            connection.Open();

            return connection;
        }

        /*
         * --------------------------------
         * Shift-JISの生バイト列を読む
         * --------------------------------
         */
        private string ReadShiftJisBlob(
            SQLiteDataReader reader,
            int index)
        {
            if (reader.IsDBNull(index))
            {
                return "";
            }

            try
            {
                long length =
                    reader.GetBytes(
                        index,
                        0,
                        null,
                        0,
                        0);

                if (length <= 0)
                {
                    return "";
                }

                byte[] bytes =
                    new byte[length];

                reader.GetBytes(
                    index,
                    0,
                    bytes,
                    0,
                    (int)length);

                /*
                 * NULL終端を切る。
                 */
                int actualLength =
                    Array.IndexOf(
                        bytes,
                        (byte)0);

                if (actualLength < 0)
                {
                    actualLength =
                        bytes.Length;
                }

                return NormalizeProfileName(
                    ShiftJis.GetString(
                        bytes,
                        0,
                        actualLength));
            }
            catch
            {
                try
                {
                    return NormalizeProfileName(
                        reader.GetString(index));
                }
                catch
                {
                    return "";
                }
            }
        }

        /*
         * --------------------------------
         * プロファイル名比較
         * --------------------------------
         */
        private bool ProfileNameEquals(
            string a,
            string b)
        {
            string left =
                NormalizeProfileName(a);

            string right =
                NormalizeProfileName(b);

            return string.Equals(
                left,
                right,
                StringComparison.Ordinal);
        }

        /*
         * --------------------------------
         * プロファイル名正規化
         * --------------------------------
         */
        private string NormalizeProfileName(
            string value)
        {
            if (value == null)
            {
                return "";
            }

            return value
                .Trim(
                    '\0',
                    ' ',
                    '\t',
                    '\r',
                    '\n')
                .Normalize(
                    NormalizationForm.FormC);
        }

        /*
         * --------------------------------
         * int読み込み
         * --------------------------------
         */
        private int ReadInt(
            SQLiteDataReader reader,
            int index)
        {
            if (reader.IsDBNull(index))
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(
                    reader.GetValue(index));
            }
            catch
            {
                return 0;
            }
        }

        /*
         * --------------------------------
         * timestamp読み込み
         * --------------------------------
         *
         * Default.dbのtimestampは
         * Windows FILETIME。
         * --------------------------------
         */
        private DateTime ReadTimestamp(
            SQLiteDataReader reader,
            int index)
        {
            if (reader.IsDBNull(index))
            {
                return DateTime.MinValue;
            }

            long value;

            try
            {
                value =
                    Convert.ToInt64(
                        reader.GetValue(index));
            }
            catch
            {
                return DateTime.MinValue;
            }

            if (value <= 0)
            {
                return DateTime.MinValue;
            }

            try
            {
                return
                    DateTime
                        .FromFileTime(
                            value);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }

    /*
     * --------------------------------
     * 1件の対戦記録
     * --------------------------------
     */
    public class TskMatchRecord
    {
        public DateTime DateTime { get; set; }

        public string P1Name { get; set; }

        public int P1CharacterId { get; set; }

        public int P1RoundCount { get; set; }

        public string P2Name { get; set; }

        public int P2CharacterId { get; set; }

        public int P2RoundCount { get; set; }
    }

    /*
     * --------------------------------
     * 1人分の集計結果
     * --------------------------------
     */
    public class TskOpponentStats
    {
        public string ProfileName { get; set; }

        public bool HasRecords { get; set; }

        /*
         * 通算対戦数。
         */
        public int TotalMatches { get; set; }

        /*
         * DB上のp2の勝利数。
         *
         * 検索対象プロファイルの勝利数。
         */
        public int TotalWins { get; set; }

        /*
         * DB上のp1の勝利数。
         *
         * 検索対象プロファイルから見た
         * 自分側の勝利数。
         */
        public int TotalLosses { get; set; }

        /*
         * ラウンド未取得用。
         */
        public bool IsHasUnrecordedWinningRound { get; set; }


        public int Last30Matches { get; set; }

        public int Last30Wins { get; set; }

        public int Last30Losses { get; set; }

        public int Last100Matches { get; set; }

        public int Last100Wins { get; set; }

        public int Last100Losses { get; set; }

        public int LastMonthMatches { get; set; }

        public int LastMonthWins { get; set; }

        public int LastMonthLosses { get; set; }

        public int MainCharacterId { get; set; }

        public int MainCharacterCount { get; set; }

        public DateTime FirstMatchDate { get; set; }

        public DateTime LastMatchDate { get; set; }

        /*
         * 自分が最後に勝った日時。
         */
        public DateTime? LastWinDate { get; set; }

        /*
         * 自分が最後に負けた日時。
         */
        public DateTime? LastLossDate { get; set; }

        /*
         * --------------------------------
         * 勝率
         * --------------------------------
         *
         * 検索対象プロファイル側の勝率。
         */
        public double WinRate
        {
            get
            {
                int total =
                    TotalWins +
                    TotalLosses;

                if (total == 0)
                {
                    return 0.0;
                }

                return
                    TotalWins *
                    100.0 /
                    total;
            }
        }

        public double Last30WinRate
        {
            get
            {
                int total =
                    Last30Wins +
                    Last30Losses;

                if (total == 0)
                {
                    return 0.0;
                }

                return
                    Last30Wins *
                    100.0 /
                    total;
            }
        }

        public double Last100WinRate
        {
            get
            {
                int total =
                    Last100Wins +
                    Last100Losses;

                if (total == 0)
                {
                    return 0.0;
                }

                return
                    Last100Wins *
                    100.0 /
                    total;
            }
        }

        public double LastMonthWinRate
        {
            get
            {
                int total =
                    LastMonthWins +
                    LastMonthLosses;

                if (total == 0)
                {
                    return 0.0;
                }

                return
                    LastMonthWins *
                    100.0 /
                    total;
            }
        }
    }

    /*
     * --------------------------------
     * キャラクター別集計
     * --------------------------------
     */
    public class TskCharacterStats
    {
        /*
         * 使用回数。
         */
        public int Matches { get; set; }

        /*
         * そのキャラクターでの勝利数。
         */
        public int Wins { get; set; }

        /*
         * そのキャラクターでの敗北数。
         */
        public int Losses { get; set; }
    }
}