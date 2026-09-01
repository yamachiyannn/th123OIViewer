using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace th123OpponentInfoViewer
{
    /*
     * ================================================
     * CombinedPlayers.db
     * ================================================
     *
     * 天則観のDefault.dbとは完全に別のDB。
     *
     * Default.db：
     *     天則観が管理する。
     *     このツールではRead Only。
     *
     * CombinedPlayers.db：
     *     このツールが管理する。
     *
     * 「複数のプロファイル = 1人のプレイヤー」
     * という関係を保存する。
     */
    public class CombinedPlayersDatabase
    {
        private readonly string databasePath;

        public string DatabasePath
        {
            get
            {
                return databasePath;
            }
        }

        public CombinedPlayersDatabase()
            : this(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "CombinedPlayers.db"))
        {
        }

        public CombinedPlayersDatabase(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "CombinedPlayers.dbのパスが空です。",
                    "path");
            }

            databasePath =
                Path.GetFullPath(path);

            InitializeDatabase();
        }

        /*
         * ================================================
         * DB初期化
         * ================================================
         */
        private void InitializeDatabase()
        {
            string directory =
                Path.GetDirectoryName(
                    databasePath);

            if (!string.IsNullOrWhiteSpace(directory) &&
                !Directory.Exists(directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    @"
CREATE TABLE IF NOT EXISTS Players
(
    PlayerId INTEGER PRIMARY KEY AUTOINCREMENT,
    PlayerName TEXT NOT NULL DEFAULT '',
    RepresentativeProfile TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Profiles
(
    PlayerId INTEGER NOT NULL,
    ProfileName TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,

    PRIMARY KEY
    (
        PlayerId,
        ProfileName
    ),

    FOREIGN KEY
    (
        PlayerId
    )
    REFERENCES Players
    (
        PlayerId
    )
    ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_Profiles_ProfileName
ON Profiles(ProfileName);

CREATE INDEX IF NOT EXISTS IX_Profiles_PlayerId
ON Profiles(PlayerId);
";

                command.ExecuteNonQuery();
            }
        }

        /*
         * ================================================
         * DB接続
         * ================================================
         */
        private SQLiteConnection OpenConnection()
        {
            SQLiteConnection connection =
                new SQLiteConnection(
                    "Data Source=" +
                    databasePath +
                    ";" +
                    "Version=3;");

            connection.Open();

            using (SQLiteCommand pragma =
                connection.CreateCommand())
            {
                pragma.CommandText =
                    "PRAGMA foreign_keys = ON;";

                pragma.ExecuteNonQuery();
            }

            return connection;
        }

        /*
         * ================================================
         * 全プレイヤー取得
         * ================================================
         */
        public List<CombinedPlayer> GetPlayers()
        {
            List<CombinedPlayer> result =
                new List<CombinedPlayer>();

            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    @"
SELECT
    PlayerId,
    PlayerName,
    RepresentativeProfile
FROM Players
ORDER BY PlayerId;
";

                using (SQLiteDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CombinedPlayer player =
                            ReadPlayer(
                                reader);

                        player.Profiles =
                            GetProfiles(
                                connection,
                                player.PlayerId);

                        result.Add(
                            player);
                    }
                }
            }

            return result;
        }

        /*
         * ================================================
         * PlayerIdから取得
         * ================================================
         */
        public CombinedPlayer GetPlayer(
            int playerId)
        {
            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    @"
SELECT
    PlayerId,
    PlayerName,
    RepresentativeProfile
FROM Players
WHERE PlayerId = @PlayerId;
";

                command.Parameters.AddWithValue(
                    "@PlayerId",
                    playerId);

                using (SQLiteDataReader reader =
                    command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    CombinedPlayer player =
                        ReadPlayer(
                            reader);

                    player.Profiles =
                        GetProfiles(
                            connection,
                            player.PlayerId);

                    return player;
                }
            }
        }

        /*
         * ================================================
         * プロファイルからプレイヤー取得
         * ================================================
         */
        public CombinedPlayer GetPlayerByProfile(
            string profileName)
        {
            string normalized =
                NormalizeProfileName(
                    profileName);

            if (normalized.Length == 0)
            {
                return null;
            }

            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    @"
SELECT
    p.PlayerId,
    p.PlayerName,
    p.RepresentativeProfile
FROM Players p
INNER JOIN Profiles pr
    ON p.PlayerId = pr.PlayerId
WHERE pr.ProfileName = @ProfileName
LIMIT 1;
";

                command.Parameters.AddWithValue(
                    "@ProfileName",
                    normalized);

                using (SQLiteDataReader reader =
                    command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    CombinedPlayer player =
                        ReadPlayer(
                            reader);

                    player.Profiles =
                        GetProfiles(
                            connection,
                            player.PlayerId);

                    return player;
                }
            }
        }

        /*
         * ================================================
         * プレイヤー名で検索
         * ================================================
         *
         * 部分一致。
         *
         * ProfileSearchFormから利用する。
         */
        public List<CombinedPlayer> SearchPlayers(
            string searchText)
        {
            string keyword =
                NormalizeProfileName(
                    searchText);

            List<CombinedPlayer> players =
                GetPlayers();

            if (keyword.Length == 0)
            {
                return players;
            }

            return players
                .Where(
                    player =>
                        PlayerMatchesSearch(
                            player,
                            keyword))
                .ToList();
        }

        /*
         * ================================================
         * プレイヤーが検索条件に一致するか
         * ================================================
         *
         * プレイヤー名
         * または
         * 内包しているプロファイル
         * のどれか1つでも一致すればtrue。
         */
        public bool PlayerMatchesSearch(
            CombinedPlayer player,
            string searchText)
        {
            if (player == null)
            {
                return false;
            }

            string keyword =
                NormalizeProfileName(
                    searchText);

            if (keyword.Length == 0)
            {
                return true;
            }

            /*
             * プレイヤー名。
             */
            if (!string.IsNullOrWhiteSpace(
                player.PlayerName) &&
                player.PlayerName.IndexOf(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            /*
             * 全プロファイル。
             */
            foreach (string profile
                in player.Profiles ??
                     new List<string>())
            {
                if (profile.IndexOf(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /*
         * ================================================
         * 表示名取得
         * ================================================
         *
         * PlayerNameがあればPlayerName。
         *
         * なければ代表プロファイル。
         */
        public string GetDisplayName(
            CombinedPlayer player)
        {
            if (player == null)
            {
                return "";
            }

            if (!string.IsNullOrWhiteSpace(
                player.PlayerName))
            {
                return player.PlayerName.Trim();
            }

            return
                player.RepresentativeProfile ?? "";
        }

        /*
         * ================================================
         * プレイヤー作成
         * ================================================
         */
        public int CreatePlayer(
            string playerName,
            string representativeProfile,
            IEnumerable<string> profiles)
        {
            string representative =
                NormalizeProfileName(
                    representativeProfile);

            if (representative.Length == 0)
            {
                throw new ArgumentException(
                    "代表プロファイルを指定してください。",
                    "representativeProfile");
            }

            List<string> profileList =
                NormalizeProfiles(
                    profiles);

            /*
             * 代表プロファイルは必ず登録する。
             */
            if (!profileList.Contains(
                representative,
                StringComparer.Ordinal))
            {
                profileList.Insert(
                    0,
                    representative);
            }

            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteTransaction transaction =
                connection.BeginTransaction())
            {
                int playerId;

                using (SQLiteCommand command =
                    connection.CreateCommand())
                {
                    command.Transaction =
                        transaction;

                    command.CommandText =
                        @"
INSERT INTO Players
(
    PlayerName,
    RepresentativeProfile
)
VALUES
(
    @PlayerName,
    @RepresentativeProfile
);

SELECT last_insert_rowid();
";

                    command.Parameters.AddWithValue(
                        "@PlayerName",
                        playerName ?? "");

                    command.Parameters.AddWithValue(
                        "@RepresentativeProfile",
                        representative);

                    playerId =
                        Convert.ToInt32(
                            command.ExecuteScalar());
                }

                InsertProfiles(
                    connection,
                    transaction,
                    playerId,
                    profileList);

                transaction.Commit();

                return playerId;
            }
        }

        /*
         * ================================================
         * プレイヤー更新
         * ================================================
         */
        public void UpdatePlayer(
            int playerId,
            string playerName,
            string representativeProfile,
            IEnumerable<string> profiles)
        {
            string representative =
                NormalizeProfileName(
                    representativeProfile);

            if (representative.Length == 0)
            {
                throw new ArgumentException(
                    "代表プロファイルを指定してください。",
                    "representativeProfile");
            }

            List<string> profileList =
                NormalizeProfiles(
                    profiles);

            if (!profileList.Contains(
                representative,
                StringComparer.Ordinal))
            {
                profileList.Insert(
                    0,
                    representative);
            }

            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteTransaction transaction =
                connection.BeginTransaction())
            {
                using (SQLiteCommand command =
                    connection.CreateCommand())
                {
                    command.Transaction =
                        transaction;

                    command.CommandText =
                        @"
UPDATE Players
SET
    PlayerName = @PlayerName,
    RepresentativeProfile =
        @RepresentativeProfile
WHERE PlayerId = @PlayerId;
";

                    command.Parameters.AddWithValue(
                        "@PlayerName",
                        playerName ?? "");

                    command.Parameters.AddWithValue(
                        "@RepresentativeProfile",
                        representative);

                    command.Parameters.AddWithValue(
                        "@PlayerId",
                        playerId);

                    int affected =
                        command.ExecuteNonQuery();

                    if (affected == 0)
                    {
                        throw new InvalidOperationException(
                            "指定されたプレイヤーが存在しません。");
                    }
                }

                using (SQLiteCommand command =
                    connection.CreateCommand())
                {
                    command.Transaction =
                        transaction;

                    command.CommandText =
                        @"
DELETE FROM Profiles
WHERE PlayerId = @PlayerId;
";

                    command.Parameters.AddWithValue(
                        "@PlayerId",
                        playerId);

                    command.ExecuteNonQuery();
                }

                InsertProfiles(
                    connection,
                    transaction,
                    playerId,
                    profileList);

                transaction.Commit();
            }
        }

        /*
         * ================================================
         * プレイヤー削除
         * ================================================
         */
        public void DeletePlayer(
            int playerId)
        {
            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    @"
DELETE FROM Players
WHERE PlayerId = @PlayerId;
";

                command.Parameters.AddWithValue(
                    "@PlayerId",
                    playerId);

                command.ExecuteNonQuery();
            }
        }

        /*
         * ================================================
         * プロファイル追加
         * ================================================
         *
         * 既存プレイヤーへ1つ追加。
         */
        public bool AddProfileToPlayer(
            int playerId,
            string profileName)
        {
            string profile =
                NormalizeProfileName(
                    profileName);

            if (profile.Length == 0)
            {
                return false;
            }

            if (GetPlayer(playerId) == null)
            {
                return false;
            }

            /*
             * すでに別プレイヤーに所属している
             * プロファイルは追加しない。
             */
            CombinedPlayer owner =
                GetPlayerByProfile(
                    profile);

            if (owner != null &&
                owner.PlayerId != playerId)
            {
                return false;
            }

            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    @"
SELECT COUNT(*)
FROM Profiles
WHERE PlayerId = @PlayerId
AND ProfileName = @ProfileName;
";

                command.Parameters.AddWithValue(
                    "@PlayerId",
                    playerId);

                command.Parameters.AddWithValue(
                    "@ProfileName",
                    profile);

                int count =
                    Convert.ToInt32(
                        command.ExecuteScalar());

                if (count > 0)
                {
                    return false;
                }
            }

            int order =
                GetProfiles(
                    playerId).Count;

            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    @"
INSERT INTO Profiles
(
    PlayerId,
    ProfileName,
    DisplayOrder
)
VALUES
(
    @PlayerId,
    @ProfileName,
    @DisplayOrder
);
";

                command.Parameters.AddWithValue(
                    "@PlayerId",
                    playerId);

                command.Parameters.AddWithValue(
                    "@ProfileName",
                    profile);

                command.Parameters.AddWithValue(
                    "@DisplayOrder",
                    order);

                command.ExecuteNonQuery();
            }

            return true;
        }

        /*
         * ================================================
         * プロファイル削除
         * ================================================
         */
        public bool RemoveProfileFromPlayer(
            int playerId,
            string profileName)
        {
            string profile =
                NormalizeProfileName(
                    profileName);

            if (profile.Length == 0)
            {
                return false;
            }

            CombinedPlayer player =
                GetPlayer(playerId);

            if (player == null)
            {
                return false;
            }

            /*
             * 代表プロファイルは削除させない。
             *
             * 代表を変更してから削除する。
             */
            if (ProfileNameEquals(
                profile,
                player.RepresentativeProfile))
            {
                return false;
            }

            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    @"
DELETE FROM Profiles
WHERE PlayerId = @PlayerId
AND ProfileName = @ProfileName;
";

                command.Parameters.AddWithValue(
                    "@PlayerId",
                    playerId);

                command.Parameters.AddWithValue(
                    "@ProfileName",
                    profile);

                return
                    command.ExecuteNonQuery() > 0;
            }
        }

        /*
         * ================================================
         * 代表プロファイル変更
         * ================================================
         */
        public bool SetRepresentativeProfile(
            int playerId,
            string profileName)
        {
            string profile =
                NormalizeProfileName(
                    profileName);

            if (profile.Length == 0)
            {
                return false;
            }

            CombinedPlayer player =
                GetPlayer(playerId);

            if (player == null)
            {
                return false;
            }

            if (!player.Profiles.Contains(
                profile,
                StringComparer.Ordinal))
            {
                return false;
            }

            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    @"
UPDATE Players
SET RepresentativeProfile =
    @RepresentativeProfile
WHERE PlayerId = @PlayerId;
";

                command.Parameters.AddWithValue(
                    "@RepresentativeProfile",
                    profile);

                command.Parameters.AddWithValue(
                    "@PlayerId",
                    playerId);

                return
                    command.ExecuteNonQuery() > 0;
            }
        }

        /*
         * ================================================
         * プロファイル一覧取得
         * ================================================
         */
        public List<string> GetProfiles(
            int playerId)
        {
            using (SQLiteConnection connection =
                OpenConnection())
            {
                return GetProfiles(
                    connection,
                    playerId);
            }
        }

        private List<string> GetProfiles(
            SQLiteConnection connection,
            int playerId)
        {
            List<string> result =
                new List<string>();

            using (SQLiteCommand command =
                connection.CreateCommand())
            {
                command.CommandText =
                    @"
SELECT ProfileName
FROM Profiles
WHERE PlayerId = @PlayerId
ORDER BY DisplayOrder, ProfileName;
";

                command.Parameters.AddWithValue(
                    "@PlayerId",
                    playerId);

                using (SQLiteDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string profile =
                            Convert.ToString(
                                reader["ProfileName"]);

                        if (!string.IsNullOrWhiteSpace(
                            profile))
                        {
                            result.Add(
                                profile);
                        }
                    }
                }
            }

            return result;
        }

        /*
         * ================================================
         * 代表プロファイル取得
         * ================================================
         */
        public string GetRepresentativeProfile(
            int playerId)
        {
            CombinedPlayer player =
                GetPlayer(
                    playerId);

            if (player == null)
            {
                return "";
            }

            return
                player.RepresentativeProfile;
        }

        /*
         * ================================================
         * 登録済みか
         * ================================================
         */
        public bool IsProfileRegistered(
            string profileName)
        {
            return
                GetPlayerByProfile(
                    profileName) != null;
        }

        /*
         * ================================================
         * プロファイルの所属プレイヤーID
         * ================================================
         */
        public int? GetPlayerIdByProfile(
            string profileName)
        {
            CombinedPlayer player =
                GetPlayerByProfile(
                    profileName);

            if (player == null)
            {
                return null;
            }

            return player.PlayerId;
        }

        /*
         * ================================================
         * プロファイル検索対象
         * ================================================
         *
         * 登録済みならそのプレイヤーの全プロファイル。
         *
         * 未登録なら指定プロファイルだけ。
         */
        public List<string> GetSearchProfiles(
            string profileName)
        {
            string normalized =
                NormalizeProfileName(
                    profileName);

            if (normalized.Length == 0)
            {
                return new List<string>();
            }

            CombinedPlayer player =
                GetPlayerByProfile(
                    normalized);

            if (player == null)
            {
                return new List<string>
                {
                    normalized
                };
            }

            return
                new List<string>(
                    player.Profiles);
        }

        /*
         * ================================================
         * 左側リスト用
         * ================================================
         *
         * 旧UI互換。
         *
         * 今回のPlayerManagementFormでは
         * 直接GetPlayers()を使用する。
         */
        public List<string> GetRepresentativeProfiles(
            IEnumerable<string> defaultDbProfiles)
        {
            List<string> result =
                new List<string>();

            foreach (string profile
                in defaultDbProfiles ??
                     Enumerable.Empty<string>())
            {
                string normalized =
                    NormalizeProfileName(
                        profile);

                if (normalized.Length == 0)
                {
                    continue;
                }

                CombinedPlayer player =
                    GetPlayerByProfile(
                        normalized);

                if (player != null)
                {
                    if (ProfileNameEquals(
                        normalized,
                        player.RepresentativeProfile))
                    {
                        result.Add(
                            player.RepresentativeProfile);
                    }
                }
                else
                {
                    result.Add(
                        normalized);
                }
            }

            return
                result
                    .Distinct(
                        StringComparer.Ordinal)
                    .OrderBy(
                        x => x,
                        StringComparer.Ordinal)
                    .ToList();
        }

        /*
         * ================================================
         * プロファイル表示順変更
         * ================================================
         */
        public void UpdateProfileOrder(
            int playerId,
            IEnumerable<string> orderedProfiles)
        {
            List<string> profiles =
                NormalizeProfiles(
                    orderedProfiles);

            using (SQLiteConnection connection =
                OpenConnection())
            using (SQLiteTransaction transaction =
                connection.BeginTransaction())
            {
                for (int i = 0;
                     i < profiles.Count;
                     i++)
                {
                    using (SQLiteCommand command =
                        connection.CreateCommand())
                    {
                        command.Transaction =
                            transaction;

                        command.CommandText =
                            @"
UPDATE Profiles
SET DisplayOrder = @DisplayOrder
WHERE PlayerId = @PlayerId
AND ProfileName = @ProfileName;
";

                        command.Parameters.AddWithValue(
                            "@DisplayOrder",
                            i);

                        command.Parameters.AddWithValue(
                            "@PlayerId",
                            playerId);

                        command.Parameters.AddWithValue(
                            "@ProfileName",
                            profiles[i]);

                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }

        /*
         * ================================================
         * 内部：Player読み込み
         * ================================================
         */
        private CombinedPlayer ReadPlayer(
            SQLiteDataReader reader)
        {
            CombinedPlayer player =
                new CombinedPlayer();

            player.PlayerId =
                Convert.ToInt32(
                    reader["PlayerId"]);

            player.PlayerName =
                reader["PlayerName"] == DBNull.Value
                    ? ""
                    : Convert.ToString(
                        reader["PlayerName"]);

            player.RepresentativeProfile =
                reader["RepresentativeProfile"] ==
                    DBNull.Value
                        ? ""
                        : Convert.ToString(
                            reader["RepresentativeProfile"]);

            return player;
        }

        /*
         * ================================================
         * プロファイルINSERT
         * ================================================
         */
        private void InsertProfiles(
            SQLiteConnection connection,
            SQLiteTransaction transaction,
            int playerId,
            IEnumerable<string> profiles)
        {
            int displayOrder = 0;

            foreach (string profile
                in profiles)
            {
                using (SQLiteCommand command =
                    connection.CreateCommand())
                {
                    command.Transaction =
                        transaction;

                    command.CommandText =
                        @"
INSERT INTO Profiles
(
    PlayerId,
    ProfileName,
    DisplayOrder
)
VALUES
(
    @PlayerId,
    @ProfileName,
    @DisplayOrder
);
";

                    command.Parameters.AddWithValue(
                        "@PlayerId",
                        playerId);

                    command.Parameters.AddWithValue(
                        "@ProfileName",
                        profile);

                    command.Parameters.AddWithValue(
                        "@DisplayOrder",
                        displayOrder);

                    command.ExecuteNonQuery();
                }

                displayOrder++;
            }
        }

        /*
         * ================================================
         * プロファイル正規化
         * ================================================
         */
        private List<string> NormalizeProfiles(
            IEnumerable<string> profiles)
        {
            List<string> result =
                new List<string>();

            foreach (string profile
                in profiles ??
                     Enumerable.Empty<string>())
            {
                string normalized =
                    NormalizeProfileName(
                        profile);

                if (normalized.Length == 0)
                {
                    continue;
                }

                if (!result.Contains(
                    normalized,
                    StringComparer.Ordinal))
                {
                    result.Add(
                        normalized);
                }
            }

            return result;
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

        private bool ProfileNameEquals(
            string a,
            string b)
        {
            return string.Equals(
                NormalizeProfileName(a),
                NormalizeProfileName(b),
                StringComparison.Ordinal);
        }
    }

    /*
     * ================================================
     * プレイヤー情報
     * ================================================
     */
    public class CombinedPlayer
    {
        public int PlayerId
        {
            get;
            set;
        }

        public string PlayerName
        {
            get;
            set;
        }

        public string RepresentativeProfile
        {
            get;
            set;
        }

        public List<string> Profiles
        {
            get;
            set;
        }

        public CombinedPlayer()
        {
            PlayerName = "";
            RepresentativeProfile = "";

            Profiles =
                new List<string>();
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(
                    PlayerName))
                {
                    return
                        PlayerName.Trim();
                }

                return
                    RepresentativeProfile ?? "";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}