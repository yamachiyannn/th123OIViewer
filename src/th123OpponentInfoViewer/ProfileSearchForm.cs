using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace th123OpponentInfoViewer
{
    /*
    * ============================================================
    * プレイヤー検索
    * ============================================================
    *
    * 左：
    *   Default.db に存在する全プロファイル。
    *
    *   プレイヤー登録済み：
    *       プレイヤー名を表示。
    *
    *   プレイヤー名が未設定：
    *       プロファイル名を表示。
    *
    *   未登録プロファイル：
    *       プロファイル名を表示。
    *
    * 検索：
    *   ・プレイヤー名
    *   ・所属している全プロファイル
    *
    *   のいずれかに検索文字列が含まれていれば表示。
    *
    * 右：
    *   選択したプレイヤー / プロファイルの戦績。
    *
    * 「代表プロファイルのみの戦績を表示」
    *
    *   OFF：
    *       プレイヤーに所属する全プロファイルを統合。
    *
    *   ON：
    *       代表プロファイルのみ。
    *
    * 戦績：
    *   TskDatabaseReader は P2 視点のデータを返すため、
    *   この画面では自分視点へ変換する。
    *
    *   P2 Wins   → 自分の Losses
    *   P2 Losses → 自分の Wins
    *
    * Designerは使用しない。
    * .NET Framework 4.7.2対応。
    */
    public class ProfileSearchForm : Form
    {
        /*
        * --------------------------------------------------------
        * DB
        * --------------------------------------------------------
        */
        private readonly TskDatabaseReader database;

        private readonly CombinedPlayersDatabase combinedDatabase;

        private readonly ViewerConfig config;

        /*
         * --------------------------------------------------------
         * UI
         * --------------------------------------------------------
         */
        private TextBox txtSearch;

        private ListBox lstPlayers;

        private CheckBox chkRepresentativeOnly;

        private TextBox txtResult;

        private ContextMenuStrip resultContextMenu;

        /*
         * --------------------------------------------------------
         * フォント
         * --------------------------------------------------------
         */
        private float resultFontSize;

        private float listFontSize;

        /*
         * --------------------------------------------------------
         * レイアウト定数
         * --------------------------------------------------------
         */
        private const int LEFT_MARGIN = 15;

        private const int LEFT_WIDTH = 310;

        private const int RESULT_LEFT = 340;

        private const int TOP_MARGIN = 15;

        private const int SEARCH_HEIGHT = 25;

        private const int LIST_TOP = 55;

        private const int RESULT_TOP = 55;

        private const int BOTTOM_MARGIN = 15;

        /*
         * --------------------------------------------------------
         * データ
         * --------------------------------------------------------
         */
        private List<string> allProfileNames =
            new List<string>();

        private List<CombinedPlayer> allPlayers =
            new List<CombinedPlayer>();

        private List<SearchPlayerItem> searchItems =
            new List<SearchPlayerItem>();

        private bool updatingList;

        /*
         * ============================================================
         * コンストラクタ
         * ============================================================
         */
        public ProfileSearchForm(
            TskDatabaseReader database,
            CombinedPlayersDatabase combinedDatabase)
        {
            if (database == null)
            {
                throw new ArgumentNullException(
                    "database");
            }

            if (combinedDatabase == null)
            {
                throw new ArgumentNullException(
                    "combinedDatabase");
            }

            this.database =
                database;

            this.combinedDatabase =
                combinedDatabase;

            config =
                new ViewerConfig();

            resultFontSize =
                config.ProfileSearchFontSize;

            listFontSize =
                config.ProfileSearchFontSize;

            InitializeForm();

            LoadData();
        }

        /*
         * ============================================================
         * フォーム初期化
         * ============================================================
         */
        private void InitializeForm()
        {
            Text =
                "プレイヤー検索";

            StartPosition =
                FormStartPosition.CenterScreen;

            Size =
                new Size(
                    1100,
                    750);

            MinimumSize =
                new Size(
                    800,
                    550);

            Font =
                new Font(
                    "MS Gothic",
                    9.0f);

            /*
             * ============================================================
             * 上部Panel
             * ============================================================
             */
            Panel topPanel =
                new Panel();

            topPanel.Location =
                new Point(
                    0,
                    0);

            topPanel.Size =
                new Size(
                    ClientSize.Width,
                    55);

            topPanel.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Left |
                AnchorStyles.Right;

            /*
             * ============================================================
             * 検索欄
             *
             * 必ず new TextBox() してから
             * Location / Size / Font / Event を設定する。
             * ============================================================
             */
            txtSearch =
                new TextBox();

            txtSearch.Location =
                new Point(
                    LEFT_MARGIN,
                    TOP_MARGIN);

            txtSearch.Size =
                new Size(
                    310,
                    SEARCH_HEIGHT);

            txtSearch.Font =
                CreateFont(
                    listFontSize);

            txtSearch.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Left;

            txtSearch.TextChanged +=
                TxtSearch_TextChanged;

            topPanel.Controls.Add(
                txtSearch);

            /*
             * ============================================================
             * 代表プロファイルのみ
             *
             * これも必ず new CheckBox() してから
             * プロパティを設定する。
             * ============================================================
             */
            chkRepresentativeOnly =
                new CheckBox();

            chkRepresentativeOnly.Text =
                "代表プロファイルのみの戦績を表示";

            chkRepresentativeOnly.AutoSize =
                true;

            chkRepresentativeOnly.Location =
                new Point(
                    345,
                    17);

            chkRepresentativeOnly.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Left;

            chkRepresentativeOnly.CheckedChanged +=
                ChkRepresentativeOnly_CheckedChanged;

            topPanel.Controls.Add(
                chkRepresentativeOnly);

            /*
             * ============================================================
             * 上部Panelをフォームへ追加
             * ============================================================
             */
            Controls.Add(
                topPanel);

            /*
             * ============================================================
             * 左側：プレイヤー一覧
             * ============================================================
             */
            lstPlayers =
                new ListBox();

            lstPlayers.Location =
                new Point(
                    LEFT_MARGIN,
                    LIST_TOP);

            lstPlayers.Size =
                new Size(
                    LEFT_WIDTH,
                    Math.Max(
                        100,
                        ClientSize.Height -
                        LIST_TOP -
                        BOTTOM_MARGIN));

            lstPlayers.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left;

            lstPlayers.Font =
                CreateFont(
                    listFontSize);

            lstPlayers.HorizontalScrollbar =
                true;

            lstPlayers.SelectionMode =
                SelectionMode.One;

            lstPlayers.SelectedIndexChanged +=
                LstPlayers_SelectedIndexChanged;

            Controls.Add(
                lstPlayers);

            /*
             * ============================================================
             * 右側：結果表示
             * ============================================================
             */
            txtResult =
                new TextBox();

            txtResult.Location =
                new Point(
                    RESULT_LEFT,
                    RESULT_TOP);

            txtResult.Size =
                new Size(
                    Math.Max(
                        100,
                        ClientSize.Width -
                        RESULT_LEFT -
                        BOTTOM_MARGIN),
                    Math.Max(
                        100,
                        ClientSize.Height -
                        RESULT_TOP -
                        BOTTOM_MARGIN));

            txtResult.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left |
                AnchorStyles.Right;

            txtResult.Multiline =
                true;

            txtResult.ReadOnly =
                true;

            txtResult.ScrollBars =
                ScrollBars.Both;

            txtResult.WordWrap =
                false;

            txtResult.BackColor =
                Color.White;

            txtResult.Font =
                CreateFont(
                    resultFontSize);

            Controls.Add(
                txtResult);

            /*
             * ============================================================
             * 右クリックメニュー
             * ============================================================
             */
            CreateResultContextMenu();
        }
        /*
         * ============================================================
         * リサイズ
         * ============================================================
         *
         * 検索欄は固定幅。
         * 左リストは高さだけ伸縮。
         * 右結果欄は上下左右に追従。
         *
         * これにより右下からリサイズしても
         * 検索フォームが縮まらない。
         */
        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            if (!IsHandleCreated)
            {
                return;
            }

            if (lstPlayers != null)
            {
                lstPlayers.Height =
                    Math.Max(
                        100,
                        ClientSize.Height -
                        LIST_TOP -
                        BOTTOM_MARGIN);
            }

            if (txtResult != null)
            {
                txtResult.Width =
                    Math.Max(
                        100,
                        ClientSize.Width -
                        RESULT_LEFT -
                        BOTTOM_MARGIN);

                txtResult.Height =
                    Math.Max(
                        100,
                        ClientSize.Height -
                        RESULT_TOP -
                        BOTTOM_MARGIN);
            }
        }

        /*
         * ============================================================
         * データ読み込み
         * ============================================================
         */
        private void LoadData()
        {
            try
            {
                allProfileNames =
                    database
                        .GetP2ProfileNames()
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(
                                    x))
                        .Distinct(
                            StringComparer.Ordinal)
                        .OrderBy(
                            x => x,
                            StringComparer.CurrentCulture)
                        .ToList();

                allPlayers =
                    combinedDatabase
                        .GetPlayers();

                BuildSearchItems();

                RefreshSearchList();

                txtResult.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "プレイヤー検索の読み込みに失敗しました。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * ============================================================
         * 検索項目作成
         * ============================================================
         *
         * Default.dbに存在する全プロファイルを基準にする。
         *
         * そのため、
         *
         *   ・プレイヤー登録済み
         *   ・プレイヤー未登録
         *
         * の両方が左側に表示される。
         */
        private void BuildSearchItems()
        {
            searchItems =
                new List<SearchPlayerItem>();

            /*
             * 同一プレイヤーを何度も追加しないため、
             * PlayerId → SearchPlayerItem を保持する。
             */
            Dictionary<int, SearchPlayerItem>
                playerItems =
                    new Dictionary<int, SearchPlayerItem>();

            foreach (string profile
                in allProfileNames)
            {
                CombinedPlayer player =
                    FindPlayerByProfile(
                        profile);

                /*
                 * ------------------------------------------------
                 * 未登録
                 * ------------------------------------------------
                 */
                if (player == null)
                {
                    SearchPlayerItem item =
                        new SearchPlayerItem();

                    item.DisplayName =
                        profile;

                    item.ProfileName =
                        profile;

                    item.Profiles =
                        new List<string>
                        {
                        profile
                        };

                    item.Player =
                        null;

                    searchItems.Add(
                        item);

                    continue;
                }

                /*
                 * ------------------------------------------------
                 * 登録済み
                 * ------------------------------------------------
                 */
                SearchPlayerItem playerItem;

                if (!playerItems.TryGetValue(
                    player.PlayerId,
                    out playerItem))
                {
                    playerItem =
                        new SearchPlayerItem();

                    /*
                     * プレイヤー名がある場合は
                     * プレイヤー名を表示。
                     *
                     * 無い場合は代表プロファイル、
                     * それも無ければ現在のプロファイル。
                     */
                    playerItem.DisplayName =
                        GetPlayerDisplayName(
                            player);

                    playerItem.ProfileName =
                        profile;

                    playerItem.Profiles =
                        new List<string>();

                    playerItem.Player =
                        player;

                    playerItems.Add(
                        player.PlayerId,
                        playerItem);

                    searchItems.Add(
                        playerItem);
                }

                /*
                 * DB上の所属プロファイルを
                 * 全て検索対象にする。
                 */
                if (player.Profiles != null)
                {
                    foreach (string playerProfile
                        in player.Profiles)
                    {
                        if (string.IsNullOrWhiteSpace(
                            playerProfile))
                        {
                            continue;
                        }

                        if (!playerItem.Profiles.Contains(
                            playerProfile,
                            StringComparer.Ordinal))
                        {
                            playerItem.Profiles.Add(
                                playerProfile);
                        }
                    }
                }

                /*
                 * 念のため現在のプロファイルも追加。
                 */
                if (!playerItem.Profiles.Contains(
                    profile,
                    StringComparer.Ordinal))
                {
                    playerItem.Profiles.Add(
                        profile);
                }
            }

            /*
             * Player DBには存在するが、
             * Default.db側に何らかの理由で
             * プロファイルが出てこない場合も
             * 検索対象として追加する。
             */
            foreach (CombinedPlayer player
                in allPlayers)
            {
                if (player == null)
                {
                    continue;
                }

                if (playerItems.ContainsKey(
                    player.PlayerId))
                {
                    continue;
                }

                SearchPlayerItem item =
                    new SearchPlayerItem();

                item.Player =
                    player;

                item.DisplayName =
                    GetPlayerDisplayName(
                        player);

                item.ProfileName =
                    GetFirstProfile(
                        player);

                item.Profiles =
                    player.Profiles == null
                        ? new List<string>()
                        : new List<string>(
                            player.Profiles);

                searchItems.Add(
                    item);

                playerItems.Add(
                    player.PlayerId,
                    item);
            }
        }

        /*
         * ============================================================
         * プレイヤー表示名
         * ============================================================
         */
        private string GetPlayerDisplayName(
            CombinedPlayer player)
        {
            if (player == null)
            {
                return "";
            }

            /*
             * 既存DBクラスの表示名処理を優先。
             */
            string displayName =
                combinedDatabase.GetDisplayName(
                    player);

            if (!string.IsNullOrWhiteSpace(
                displayName))
            {
                return displayName;
            }

            string firstProfile =
                GetFirstProfile(
                    player);

            return firstProfile;
        }

        /*
         * ============================================================
         * 最初のプロファイル
         * ============================================================
         */
        private string GetFirstProfile(
            CombinedPlayer player)
        {
            if (player == null ||
                player.Profiles == null)
            {
                return "";
            }

            foreach (string profile
                in player.Profiles)
            {
                if (!string.IsNullOrWhiteSpace(
                    profile))
                {
                    return profile;
                }
            }

            return "";
        }

        /*
         * ============================================================
         * プロファイルからプレイヤー検索
         * ============================================================
         */
        private CombinedPlayer FindPlayerByProfile(
            string profileName)
        {
            if (string.IsNullOrWhiteSpace(
                profileName))
            {
                return null;
            }

            foreach (CombinedPlayer player
                in allPlayers)
            {
                if (player == null ||
                    player.Profiles == null)
                {
                    continue;
                }

                if (player.Profiles.Contains(
                    profileName,
                    StringComparer.Ordinal))
                {
                    return player;
                }
            }

            return null;
        }

        /*
         * ============================================================
         * 検索リスト更新
         * ============================================================
         */
        private void RefreshSearchList()
        {
            if (txtSearch == null ||
                lstPlayers == null)
            {
                return;
            }

            string keyword =
                txtSearch.Text.Trim();

            IEnumerable<SearchPlayerItem> filtered =
                searchItems;

            if (!string.IsNullOrWhiteSpace(
                keyword))
            {
                filtered =
                    filtered.Where(
                        x =>
                            x.Matches(
                                keyword));
            }

            updatingList =
                true;

            try
            {
                lstPlayers.BeginUpdate();

                lstPlayers.Items.Clear();

                foreach (SearchPlayerItem item
                    in filtered)
                {
                    lstPlayers.Items.Add(
                        item);
                }
            }
            finally
            {
                lstPlayers.EndUpdate();

                updatingList =
                    false;
            }
        }

        /*
         * ============================================================
         * 検索
         * ============================================================
         */
        private void TxtSearch_TextChanged(
            object sender,
            EventArgs e)
        {
            RefreshSearchList();
        }

        /*
         * ============================================================
         * プレイヤー選択
         * ============================================================
         */
        private void LstPlayers_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            if (updatingList)
            {
                return;
            }

            SearchPlayerItem item =
                lstPlayers.SelectedItem
                    as SearchPlayerItem;

            if (item == null)
            {
                txtResult.Clear();

                return;
            }

            ShowPlayer(
                item);
        }

        /*
         * ============================================================
         * 代表のみ切り替え
         * ============================================================
         */
        private void ChkRepresentativeOnly_CheckedChanged(
            object sender,
            EventArgs e)
        {
            SearchPlayerItem item =
                lstPlayers.SelectedItem
                    as SearchPlayerItem;

            if (item == null)
            {
                return;
            }

            ShowPlayer(
                item);
        }

        /*
         * ============================================================
         * プレイヤー表示
         * ============================================================
         */
        private void ShowPlayer(
            SearchPlayerItem item)
        {
            try
            {
                List<string> profiles;

                /*
                 * ------------------------------------------------
                 * 未登録
                 * ------------------------------------------------
                 */
                if (item.Player == null)
                {
                    profiles =
                        new List<string>
                        {
                        item.ProfileName
                        };
                }
                else
                {
                    /*
                     * ------------------------------------------------
                     * 代表のみ
                     * ------------------------------------------------
                     */
                    if (chkRepresentativeOnly.Checked)
                    {
                        profiles =
                            new List<string>();

                        if (!string.IsNullOrWhiteSpace(
                            item.Player.RepresentativeProfile))
                        {
                            profiles.Add(
                                item.Player.RepresentativeProfile);
                        }
                    }
                    else
                    {
                        profiles =
                            GetOrderedProfiles(
                                item.Player,
                                item.Profiles);
                    }
                }

                
                
                profiles =
                    profiles
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(
                                    x))
                        .Distinct(
                            StringComparer.Ordinal)
                        .ToList();

                ShowProfiles(
                    item.DisplayName,
                    profiles);
            }
            catch (Exception ex)
            {
                txtResult.Text =
                    "戦績の読み込みに失敗しました。\r\n\r\n" +
                    ex.Message;
            }
        }

        /*
         * ============================================================
         * プロファイル表示順
         * ============================================================
         *
         * 代表プロファイルを必ず先頭にする。
         * それ以外の所属プロファイルは元の順番を維持する。
         */
        private List<string> GetOrderedProfiles(
            CombinedPlayer player,
            List<string> profiles)
        {
            List<string> result =
                new List<string>();

            if (player == null)
            {
                return result;
            }

            string representative =
                player.RepresentativeProfile;

            if (!string.IsNullOrWhiteSpace(
                representative))
            {
                result.Add(
                    representative);
            }

            if (profiles != null)
            {
                foreach (string profile
                    in profiles)
                {
                    if (string.IsNullOrWhiteSpace(
                        profile))
                    {
                        continue;
                    }

                    if (result.Contains(
                        profile,
                        StringComparer.Ordinal))
                    {
                        continue;
                    }

                    result.Add(
                        profile);
                }
            }

            return result;
        }

        /*
         * ============================================================
         * 戦績表示
         * ============================================================
         */
        private void ShowProfiles(
            string displayName,
            List<string> profileNames)
        {
            try
            {
                int totalMatches = 0;

                // 自分視点
                int totalSelfWins = 0;
                int totalOpponentWins = 0;

                int last30SelfWins = 0;
                int last30OpponentWins = 0;

                int last100SelfWins = 0;
                int last100OpponentWins = 0;

                int lastMonthSelfWins = 0;
                int lastMonthOpponentWins = 0;

                DateTime firstMatchDate =
                    DateTime.MaxValue;

                DateTime lastMatchDate =
                    DateTime.MinValue;

                DateTime lastWinDate =
                    DateTime.MinValue;

                DateTime lastLossDate =
                    DateTime.MinValue;

                bool hasLastWinDate = false;
                bool hasLastLossDate = false;

                bool hasAnyRecords = false;
                bool hasUnrecordedWinningRound = false;

                Dictionary<int, TskCharacterStats>
                    combinedCharacterStats =
                        new Dictionary<int, TskCharacterStats>();

                foreach (string profileName in profileNames)
                {
                    if (string.IsNullOrWhiteSpace(profileName))
                    {
                        continue;
                    }

                    TskOpponentStats stats =
                        database.GetPlayerStats(
                            profileName);

                    if (!stats.HasRecords)
                    {
                        continue;
                    }

                    hasAnyRecords = true;

                    if (stats.IsHasUnrecordedWinningRound)
                    {
                        hasUnrecordedWinningRound = true;
                    }

                    totalMatches +=
                        stats.TotalMatches;

                    /*
                     * Default.db の GetPlayerStats は
                     * P2プロフィール視点なので、
                     *
                     *   P2 Wins   = 相手の勝利
                     *   P2 Losses = 自分の勝利
                     *
                     * として自分視点に変換する。
                     */
                    totalSelfWins +=
                        stats.TotalLosses;

                    totalOpponentWins +=
                        stats.TotalWins;

                    lastMonthSelfWins +=
                        stats.LastMonthLosses;

                    lastMonthOpponentWins +=
                        stats.LastMonthWins;

                    if (stats.FirstMatchDate !=
                        DateTime.MinValue)
                    {
                        if (stats.FirstMatchDate <
                            firstMatchDate)
                        {
                            firstMatchDate =
                                stats.FirstMatchDate;
                        }
                    }

                    if (stats.LastMatchDate !=
                        DateTime.MinValue)
                    {
                        if (stats.LastMatchDate >
                            lastMatchDate)
                        {
                            lastMatchDate =
                                stats.LastMatchDate;
                        }
                    }

                    /*
                     * GetPlayerStatsのLastWinDate / LastLossDateも
                     * P2視点なので、自分視点に入れ替える。
                     */
                    if (stats.LastLossDate.HasValue)
                    {
                        if (!hasLastWinDate ||
                            stats.LastLossDate.Value >
                            lastWinDate)
                        {
                            lastWinDate =
                                stats.LastLossDate.Value;

                            hasLastWinDate = true;
                        }
                    }

                    if (stats.LastWinDate.HasValue)
                    {
                        if (!hasLastLossDate ||
                            stats.LastWinDate.Value >
                            lastLossDate)
                        {
                            lastLossDate =
                                stats.LastWinDate.Value;

                            hasLastLossDate = true;
                        }
                    }

                    /*
                     * キャラクター使用状況
                     *
                     * P2CharacterStatsもP2視点なので、
                     * Wins/Lossesは表示時に自分視点へ変換する。
                     */
                    Dictionary<int, TskCharacterStats>
                        characterStats =
                            database.GetP2CharacterStats(
                                profileName);

                    foreach (var characterEntry
                        in characterStats)
                    {
                        int characterId =
                            characterEntry.Key;

                        TskCharacterStats source =
                            characterEntry.Value;

                        if (!combinedCharacterStats.ContainsKey(
                            characterId))
                        {
                            combinedCharacterStats.Add(
                                characterId,
                                new TskCharacterStats());
                        }

                        TskCharacterStats destination =
                            combinedCharacterStats[
                                characterId];

                        destination.Matches +=
                            source.Matches;

                        /*
                         * P2のLosses = 自分の勝利
                         */
                        destination.Wins +=
                            source.Losses;

                        /*
                         * P2のWins = 自分の敗北
                         */
                        destination.Losses +=
                            source.Wins;
                    }
                }

                /*
                 * --------------------------------------------------------
                 * 複数プロファイルを統合した最近の戦績
                 * --------------------------------------------------------
                 */
                List<TskMatchRecord> combinedRecords =
                    database.GetP2MatchRecords(
                        profileNames);

                List<TskMatchRecord> last30Records =
                    combinedRecords
                        .OrderByDescending(
                            x => x.DateTime)
                        .Take(30)
                        .ToList();

                List<TskMatchRecord> last100Records =
                    combinedRecords
                        .OrderByDescending(
                            x => x.DateTime)
                        .Take(100)
                        .ToList();

                last30SelfWins =
                    last30Records.Count(
                        x => x.P1RoundCount >= 2);

                last30OpponentWins =
                    last30Records.Count(
                        x => x.P2RoundCount >= 2);

                last100SelfWins =
                    last100Records.Count(
                        x => x.P1RoundCount >= 2);

                last100OpponentWins =
                    last100Records.Count(
                        x => x.P2RoundCount >= 2);

                /*
                 * --------------------------------------------------------
                 * 記録なし
                 * --------------------------------------------------------
                 */
                if (!hasAnyRecords)
                {
                    string noRecordText =
                        "【プロファイル詳細】\r\n\r\n";

                    if (!string.IsNullOrWhiteSpace(
                        displayName))
                    {
                        noRecordText +=
                            "表示名： " +
                            displayName +
                            "\r\n";
                    }

                    noRecordText +=
                        "プロファイル：";

                    for (int i = 0;
                         i < profileNames.Count;
                         i++)
                    {
                        if (i == 0)
                        {
                            noRecordText +=
                                profileNames[i];
                        }
                        else
                        {
                            noRecordText +=
                                "\r\n             " +
                                profileNames[i];
                        }
                    }

                    noRecordText +=
                        "\r\n\r\n" +
                        "対戦記録がありません。";

                    txtResult.Text =
                        noRecordText;

                    return;
                }

                /*
                 * --------------------------------------------------------
                 * ヘッダー
                 * --------------------------------------------------------
                 */
                string text =
                    "【プロファイル詳細】\r\n\r\n";

                if (!string.IsNullOrWhiteSpace(
                    displayName))
                {
                    text +=
                        "表示名： " +
                        displayName +
                        "\r\n\r\n";
                }

                text +=
                    "プロファイル：";

                for (int i = 0;
                     i < profileNames.Count;
                     i++)
                {
                    if (i == 0)
                    {
                        text +=
                            profileNames[i];
                    }
                    else
                    {
                        text +=
                            "\r\n              " +
                            profileNames[i];
                    }
                }

                text +=
                    "\r\n\r\n";

                /*
                 * --------------------------------------------------------
                 * 基本情報
                 * --------------------------------------------------------
                 */
                text +=
                    "【基本情報】\r\n";

                text +=
                    "通算対戦数 : " +
                    totalMatches +
                    "戦\r\n";

                text +=
                    "自分勝利　 : " +
                    totalSelfWins +
                    "勝\r\n";

                text +=
                    "相手勝利　 : " +
                    totalOpponentWins +
                    "勝\r\n";

                text +=
                    "通算勝率　 : " +
                    FormatRate(
                        CalculateWinRate(
                            totalSelfWins,
                            totalOpponentWins)) +
                    "\r\n";

                if (hasUnrecordedWinningRound)
                {
                    text +=
                        "[ラウンド未取得]\r\n";
                }

                text +=
                    "\r\n";

                /*
                 * --------------------------------------------------------
                 * 最近の戦績
                 * --------------------------------------------------------
                 */
                text +=
                    "【最近の戦績】\r\n";

                text +=
                    FormatRecordLine(
                        "過去30戦",
                        last30SelfWins,
                        last30OpponentWins,
                        3) +
                    "\r\n";

                text +=
                    FormatRecordLine(
                        "過去100戦",
                        last100SelfWins,
                        last100OpponentWins,
                        2) +
                    "\r\n";

                text +=
                    FormatRecordLine(
                        "過去1か月",
                        lastMonthSelfWins,
                        lastMonthOpponentWins,
                        2) +
                    "\r\n\r\n";

                /*
                 * --------------------------------------------------------
                 * キャラクター使用状況
                 * --------------------------------------------------------
                 */
                text +=
                    "【キャラクター使用状況】\r\n";

                var orderedCharacters =
                    combinedCharacterStats
                        .OrderByDescending(
                            x => x.Value.Matches)
                        .ThenBy(
                            x => x.Key);

                foreach (var characterEntry
                    in orderedCharacters)
                {
                    text +=
                        FormatCharacterLine(
                            GetCharacterName(
                                characterEntry.Key),
                            characterEntry.Value) +
                        "\r\n";
                }

                /*
                 * --------------------------------------------------------
                 * 対戦日時
                 * --------------------------------------------------------
                 */
                text +=
                    "\r\n";

                text +=
                    "【対戦日時】\r\n";

                if (firstMatchDate !=
                    DateTime.MaxValue)
                {
                    text +=
                        "初対戦　　　 ： " +
                        FormatDate(
                            firstMatchDate) +
                        "\r\n";
                }

                if (lastMatchDate !=
                    DateTime.MinValue)
                {
                    text +=
                        "前回対戦　　 ： " +
                        FormatDate(
                            lastMatchDate) +
                        "\r\n";
                }

                if (hasLastWinDate)
                {
                    text +=
                        "最後に勝った ： " +
                        FormatDate(
                            lastWinDate) +
                        "\r\n";
                }

                if (hasLastLossDate)
                {
                    text +=
                        "最後に負けた ： " +
                        FormatDate(
                            lastLossDate) +
                        "\r\n";
                }

                /*
                 * --------------------------------------------------------
                 * メインキャラ
                 * --------------------------------------------------------
                 */
                text +=
                    "\r\n";

                text +=
                    "【メインキャラ】\r\n";

                if (combinedCharacterStats.Count > 0)
                {
                    /*
                     * 第一メイン：
                     * 最も使用回数が多いキャラクター。
                     */
                    var mainCharacter =
                        combinedCharacterStats
                            .OrderByDescending(
                                x => x.Value.Matches)
                            .ThenBy(
                                x => x.Key)
                            .First();

                    string mainName =
                        GetCharacterName(
                            mainCharacter.Key);

                    text +=
                        "対戦回数 : " +
                        mainName +
                        " (" +
                        mainCharacter.Value.Matches +
                        "戦)\r\n";

                    /*
                     * 第二メイン：
                     * 自分視点で最も敗北率が高いキャラクター。
                     *
                     * ここで使っているLossesは、
                     * 上の集計でP2 Winsから自分視点へ
                     * 変換済み。
                     */
                    var secondCharacter =
                        combinedCharacterStats
                            .Where(
                                x =>
                                    x.Value.Matches > 0)
                            .Select(
                                x => new
                                {
                                    CharacterId =
                                        x.Key,

                                    Stats =
                                        x.Value,

                                    LossRate =
                                        x.Value.Losses *
                                        100.0 /
                                        x.Value.Matches
                                })
                            .OrderByDescending(
                                x => x.LossRate)
                            .ThenByDescending(
                                x => x.Stats.Matches)
                            .ThenBy(
                                x => x.CharacterId)
                            .First();

                    string secondName =
                        GetCharacterName(
                            secondCharacter.CharacterId);

                    /*
                     * 2行目の勝率は相手視点。
                     *
                     * つまり「そのキャラに対して相手が
                     * どれだけ勝っているか」を表示する。
                     */
                    double opponentWinRate =
                        secondCharacter.Stats.Losses *
                        100.0 /
                        secondCharacter.Stats.Matches;

                    text +=
                        "相手勝率 : " +
                        secondName +
                        " (" +
                        opponentWinRate.ToString(
                            "0.0") +
                        "%)";
                }
                else
                {
                    text +=
                        "対戦回数 : ---\r\n" +
                        "相手勝率 : ---";
                }

                txtResult.Text =
                    text;
            }
            catch (Exception ex)
            {
                txtResult.Text =
                    "【エラー】\r\n\r\n" +
                    ex.Message;
            }
        }
        /*
         * ============================================================
         * キャラクター集計
         * ============================================================
         *
         * P2視点のWins/Lossesをそのまま保持する。
         *
         * 表示時に
         *
         *   Losses → 自分の勝ち
         *   Wins   → 自分の負け
         *
         * として扱う。
         */
        private Dictionary<int, TskCharacterStats>
            GetCombinedCharacterStats(
                List<string> profileNames)
        {
            Dictionary<int, TskCharacterStats>
                result =
                    new Dictionary<int, TskCharacterStats>();

            foreach (string profile
                in profileNames)
            {
                if (string.IsNullOrWhiteSpace(
                    profile))
                {
                    continue;
                }

                Dictionary<int, TskCharacterStats>
                    source =
                        database.GetP2CharacterStats(
                            profile);

                if (source == null)
                {
                    continue;
                }

                foreach (var item
                    in source)
                {
                    if (!result.ContainsKey(
                        item.Key))
                    {
                        result.Add(
                            item.Key,
                            new TskCharacterStats());
                    }

                    result[item.Key].Matches +=
                        item.Value.Matches;

                    result[item.Key].Wins +=
                        item.Value.Wins;

                    result[item.Key].Losses +=
                        item.Value.Losses;
                }
            }

            return result;
        }

        /*
         * ============================================================
         * 右クリックメニュー
         * ============================================================
         */
        private void CreateResultContextMenu()
        {
            resultContextMenu =
                new ContextMenuStrip();

            AddFontMenu(
                8.0f,
                "8 px");

            AddFontMenu(
                10.0f,
                "10 px");

            AddFontMenu(
                14.0f,
                "14 px");

            AddFontMenu(
                20.0f,
                "20 px");

            txtResult.ContextMenuStrip =
                resultContextMenu;
        }

        /*
         * ============================================================
         * フォントメニュー
         * ============================================================
         */
        private void AddFontMenu(
            float size,
            string text)
        {
            ToolStripMenuItem item =
                new ToolStripMenuItem(
                    text);

            item.Click +=
                delegate
                {
                    SetResultFontSize(
                        size);
                };

            resultContextMenu.Items.Add(
                item);
        }

        /*
         * ============================================================
         * フォント変更
         * ============================================================
         */
        private void SetResultFontSize(
            float size)
        {
            resultFontSize =
                size;

            txtResult.Font =
                new Font(
                    txtResult.Font.FontFamily,
                    resultFontSize,
                    txtResult.Font.Style);
        }

        /*
         * ============================================================
         * Font
         * ============================================================
         */
        private Font CreateFont(
            float size)
        {
            return new Font(
                "MS Gothic",
                size);
        }

        /*
         * ============================================================
         * 勝率
         * ============================================================
         */
        private double CalculateWinRate(
            int wins,
            int losses)
        {
            int total =
                wins +
                losses;

            if (total <= 0)
            {
                return 0.0;
            }

            return
                wins *
                100.0 /
                total;
        }

        /*
         * ============================================================
         * 勝率表示
         * ============================================================
         */
        private string FormatRate(
            double rate)
        {
            return
                rate.ToString(
                    "0.0") +
                "%";
        }

        /*
         * ============================================================
         * 戦績行
         * ============================================================
         *
         * GitHub v0.0.2の形式をベースにする。
         *
         * 例：
         *
         * 過去30戦   ：     30戦     20勝     10敗 (66.7%)
         *
         * 「戦」「勝」「負」の位置を
         * PadLeftで揃える。
         */
        private string FormatRecordLine(
            string label,
            int wins,
            int losses,
            int spaceCount)
        {
            int total =
                wins +
                losses;

            double rate =
                CalculateWinRate(
                    wins,
                    losses);

            return
                label +
                new string(
                    ' ',
                    spaceCount) +
                "：" +
                " " +
                total
                    .ToString()
                    .PadLeft(6) +
                "戦 " +
                wins
                    .ToString()
                    .PadLeft(6) +
                "勝 " +
                losses
                    .ToString()
                    .PadLeft(6) +
                "敗 (" +
                FormatRate(
                    rate) +
                ")";
        }

        /*
         * ============================================================
         * キャラクター行
         * ============================================================
         *
         * キャラクター名の後ろに
         *
         *   6 - キャラクター名の文字数
         *
         * 個の全角スペースを入れる。
         *
         * これで「：」の位置を揃える。
         *
         * 戦績は自分視点。
         *
         *   P2 Losses → 自分の勝ち
         *   P2 Wins   → 自分の負け
         */
        private string FormatCharacterLine(
            string characterName,
            TskCharacterStats stats)
        {
            int fullWidthPadding =
                Math.Max(
                    0,
                    6 -
                    characterName.Length);

            string name =
                characterName +
                new string(
                    '　',
                    fullWidthPadding);

            int selfWins =
                stats.Wins;

            int selfLosses =
                stats.Losses;

            return
                name +
                "：" +
                " " +
                stats.Matches
                    .ToString()
                    .PadLeft(6) +
                "戦 " +
                selfWins
                    .ToString()
                    .PadLeft(6) +
                "勝 " +
                selfLosses
                    .ToString()
                    .PadLeft(6) +
                "敗";
        }

        /*
         * ============================================================
         * 日付
         * ============================================================
         */
        private string FormatDate(
            DateTime dateTime)
        {
            return
                dateTime
                    .AddHours(-9)
                    .ToString(
                        "yyyy/MM/dd HH:mm:ss");
        }

        /*
         * ============================================================
         * プロファイルヘッダー
         * ============================================================
         */
        private string CreateProfileHeader(
            List<string> profileNames)
        {
            string text =
                "プロファイル：";

            if (profileNames == null ||
                profileNames.Count == 0)
            {
                return text +
                    "---\r\n";
            }

            text +=
                profileNames[0] +
                "\r\n";

            for (int i = 1;
                 i < profileNames.Count;
                 i++)
            {
                text +=
                    new string(
                        ' ',
                        13) +
                    profileNames[i] +
                    "\r\n";
            }

            return text;
        }

        /*
         * ============================================================
         * キャラクター名
         * ============================================================
         */
        private string GetCharacterName(
            int characterId)
        {
            switch (characterId)
            {
                case 0:
                    return "霊夢";

                case 1:
                    return "魔理沙";

                case 2:
                    return "咲夜";

                case 3:
                    return "アリス";

                case 4:
                    return "パチュリー";

                case 5:
                    return "妖夢";

                case 6:
                    return "レミリア";

                case 7:
                    return "幽々子";

                case 8:
                    return "紫";

                case 9:
                    return "萃香";

                case 10:
                    return "鈴仙";

                case 11:
                    return "文";

                case 12:
                    return "小町";

                case 13:
                    return "衣玖";

                case 14:
                    return "天子";

                case 15:
                    return "早苗";

                case 16:
                    return "チルノ";

                case 17:
                    return "美鈴";

                case 18:
                    return "空";

                case 19:
                    return "諏訪子";

                default:
                    return
                        "不明(" +
                        characterId +
                        ")";
            }
        }

        /*
         * ============================================================
         * 検索項目
         * ============================================================
         */
        private class SearchPlayerItem
        {
            /*
             * 左リストに表示する名前。
             */
            public string DisplayName
            {
                get;
                set;
            }

            /*
             * Default.db側から来たプロファイル。
             *
             * 未登録プレイヤーの場合はこれを
             * 戦績対象として使用する。
             */
            public string ProfileName
            {
                get;
                set;
            }

            /*
             * プレイヤーが持つ全プロファイル。
             *
             * 検索対象にもなる。
             */
            public List<string> Profiles
            {
                get;
                set;
            }

            /*
             * CPdbのプレイヤー。
             *
             * 未登録の場合はnull。
             */
            public CombinedPlayer Player
            {
                get;
                set;
            }

            /*
             * --------------------------------------------------------
             * 検索一致
             * --------------------------------------------------------
             *
             * 例えば、
             *
             * プレイヤー名：
             *   あいうえお
             *
             * プロファイル：
             *   かきくけこ
             *   さしすせそ
             *
             * の場合、
             *
             *   いう
             *   くけこ
             *   さ
             *
             * などでこのプレイヤーが検索結果に残る。
             */
            public bool Matches(
                string keyword)
            {
                if (string.IsNullOrWhiteSpace(
                    keyword))
                {
                    return true;
                }

                /*
                 * プレイヤー名。
                 */
                if (!string.IsNullOrWhiteSpace(
                    DisplayName))
                {
                    if (DisplayName.IndexOf(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) >=
                        0)
                    {
                        return true;
                    }
                }

                /*
                 * 所属プロファイル全て。
                 */
                if (Profiles != null)
                {
                    foreach (string profile
                        in Profiles)
                    {
                        if (string.IsNullOrWhiteSpace(
                            profile))
                        {
                            continue;
                        }

                        if (profile.IndexOf(
                            keyword,
                            StringComparison.OrdinalIgnoreCase) >=
                            0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }

        /*
         * ============================================================
         * 終了処理
         * ============================================================
         */
        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            try
            {
                if (lstPlayers != null)
                {
                    lstPlayers.SelectedIndexChanged -=
                        LstPlayers_SelectedIndexChanged;
                }

                if (txtSearch != null)
                {
                    txtSearch.TextChanged -=
                        TxtSearch_TextChanged;
                }

                if (chkRepresentativeOnly != null)
                {
                    chkRepresentativeOnly.CheckedChanged -=
                        ChkRepresentativeOnly_CheckedChanged;
                }
            }
            catch
            {
            }

            base.OnFormClosed(
                e);
        }
    }

}
