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

        private static readonly Encoding ShiftJis =
            Encoding.GetEncoding(932);

        /*
         * --------------------------------
         * 複数プロファイルの対戦記録取得
         * --------------------------------
         *
         * 指定されたプロファイルをまとめて取得する。
         *
         * 戻り値はDB上の対戦記録そのもの。
         */
        public List<TskMatchRecord>
            GetP2MatchRecords(
                List<string> profileNames)
        {
            List<TskMatchRecord> records =
                new List<TskMatchRecord>();

            if (profileNames == null ||
                profileNames.Count == 0)
            {
                return records;
            }

            if (!File.Exists(databasePath))
            {
                throw new FileNotFoundException(
                    "Default.dbが見つかりません。",
                    databasePath);
            }

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

                            bool matched =
                                false;

                            foreach (string profileName
                                in profileNames)
                            {
                                if (string.IsNullOrWhiteSpace(
                                    profileName))
                                {
                                    continue;
                                }

                                if (ProfileNameEquals(
                                    p2Name,
                                    profileName))
                                {
                                    matched = true;
                                    break;
                                }
                            }

                            if (!matched)
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

            return records
                .OrderBy(
                    x => x.DateTime)
                .ToList();
        }

        /*
         * 指定プロファイルの対戦記録を取得する。
         *
         * P2側のプロファイル名として検索する。
         *
         * 返すTskMatchRecordは読み取り専用用途で使用し、
         * Default.dbには何も書き込まない。
         */
        public List<TskMatchRecord> GetPlayerMatchRecords(
            string profileName)
        {
            List<TskMatchRecord> records =
                new List<TskMatchRecord>();

            if (string.IsNullOrWhiteSpace(
                profileName))
            {
                return records;
            }

            if (!File.Exists(databasePath))
            {
                throw new FileNotFoundException(
                    "Default.dbが見つかりません。",
                    databasePath);
            }

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

            return
                records
                    .OrderBy(
                        x => x.DateTime)
                    .ToList();
        }

        public TskDatabaseReader()
        {
            ViewerConfig config =
                new ViewerConfig();

            databasePath =
                config.DatabasePath;
        }

        public TskDatabaseReader(
            string path)
        {
            databasePath =
                path;
        }

        public bool DatabaseExists
        {
            get
            {
                return File.Exists(databasePath);
            }
        }

        public string DatabasePath
        {
            get
            {
                return databasePath;
            }
        }

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
                        "SELECT COUNT(*) FROM trackrecord123",
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

                            if (string.IsNullOrWhiteSpace(name))
                            {
                                continue;
                            }

                            if (!names.Contains(
                                name,
                                StringComparer.Ordinal))
                            {
                                names.Add(name);
                            }
                        }
                    }
                }
            }

            return names
                .OrderBy(
                    x => x,
                    StringComparer.CurrentCulture)
                .ToList();
        }

        public Dictionary<int, int>
            GetP2CharacterUsage(
                string profileName)
        {
            Dictionary<int, int> result =
                new Dictionary<int, int>();

            if (string.IsNullOrWhiteSpace(profileName) ||
                !File.Exists(databasePath))
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

        public Dictionary<int, TskCharacterStats>
            GetP2CharacterStats(
                string profileName)
        {
            Dictionary<int, TskCharacterStats> result =
                new Dictionary<int, TskCharacterStats>();

            if (string.IsNullOrWhiteSpace(profileName) ||
                !File.Exists(databasePath))
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
                             * P2 = 検索対象プロファイル。
                             *
                             * p2win >= 2
                             * → 検索対象の勝利。
                             */
                            if (p2win >= 2)
                            {
                                stats.Wins++;
                            }

                            /*
                             * p1win >= 2
                             * → 検索対象の敗北。
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

        public TskOpponentStats GetOpponentStats(
            string opponentProfileName)
        {
            return GetPlayerStats(
                opponentProfileName);
        }

        public TskOpponentStats GetPlayerStats(
            string profileName)
        {
            TskOpponentStats stats =
                new TskOpponentStats();

            stats.ProfileName =
                profileName ?? "";

            if (string.IsNullOrWhiteSpace(profileName))
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

            if (records.Count == 0)
            {
                stats.HasRecords = false;
                return stats;
            }

            records =
                records
                    .OrderBy(
                        x => x.DateTime)
                    .ToList();

            stats.HasRecords = true;

            stats.TotalMatches =
                records.Count;

            /*
             * 検索対象 = P2。
             *
             * ここを明確にする。
             */
            stats.TotalWins =
                records.Count(
                    x => x.P2RoundCount >= 2);

            stats.TotalLosses =
                records.Count(
                    x => x.P1RoundCount >= 2);

            /*
             * 既存DBとの互換用。
             */
            stats.IsHasUnrecordedWinningRound =
                records.Count > 0 &&
                records.All(
                    x =>
                        x.P1RoundCount == 0 &&
                        x.P2RoundCount == 2);

            DateTime today =
                DateTime.Now.Date;

            stats.FirstMatchDate =
                records.First().DateTime;

            TskMatchRecord lastBeforeToday =
                records
                    .Where(
                        x =>
                            x.DateTime.Date <
                            today)
                    .OrderByDescending(
                        x => x.DateTime)
                    .FirstOrDefault();

            if (lastBeforeToday != null)
            {
                stats.LastMatchDate =
                    lastBeforeToday.DateTime;
            }

            TskMatchRecord lastWin =
                records
                    .Where(
                        x =>
                            x.P2RoundCount >= 2)
                    .OrderByDescending(
                        x => x.DateTime)
                    .FirstOrDefault();

            if (lastWin != null)
            {
                stats.LastWinDate =
                    lastWin.DateTime;
            }

            TskMatchRecord lastLoss =
                records
                    .Where(
                        x =>
                            x.P1RoundCount >= 2)
                    .OrderByDescending(
                        x => x.DateTime)
                    .FirstOrDefault();

            if (lastLoss != null)
            {
                stats.LastLossDate =
                    lastLoss.DateTime;
            }

            /*
             * 直近30戦
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
             * 直近100戦
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
             * 直近1か月
             */
            DateTime oneMonthAgo =
                DateTime.Now.AddMonths(-1);

            List<TskMatchRecord> lastMonth =
                records
                    .Where(
                        x =>
                            x.DateTime >=
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
             * メインキャラ用。
             *
             * 100戦未満 → 全戦
             * 100戦以上 → 直近100戦
             */
            List<TskMatchRecord> characterRecords;

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

        private bool ProfileNameEquals(
            string a,
            string b)
        {
            return string.Equals(
                NormalizeProfileName(a),
                NormalizeProfileName(b),
                StringComparison.Ordinal);
        }

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
                return DateTime.FromFileTime(
                    value);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }

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

    public class TskOpponentStats
    {
        public string ProfileName { get; set; }

        public bool HasRecords { get; set; }

        /*
         * 常に「検索対象プロファイル視点」。
         */
        public int TotalMatches { get; set; }

        public int TotalWins { get; set; }

        public int TotalLosses { get; set; }

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

        public DateTime? LastWinDate { get; set; }

        public DateTime? LastLossDate { get; set; }

        public double WinRate
        {
            get
            {
                int total =
                    TotalWins +
                    TotalLosses;

                if (total <= 0)
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

                if (total <= 0)
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

                if (total <= 0)
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

                if (total <= 0)
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

    public class TskCharacterStats
    {
        public int Matches { get; set; }

        /*
         * そのキャラクターを使った側の勝利。
         */
        public int Wins { get; set; }

        /*
         * そのキャラクターを使った側の敗北。
         */
        public int Losses { get; set; }
    }
}