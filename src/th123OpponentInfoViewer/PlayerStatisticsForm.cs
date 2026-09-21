using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace th123OpponentInfoViewer
{
    /*
     * ============================================================
     * プレイヤー表
     * ============================================================
     *
     * 1つのDataGridViewに、左・中央・右の3つの情報を横並びで表示する。
     *
     * A   rank
     * B   player
     * C   battle
     * D   player
     * E   win
     * F   los
     * G   rate
     * H   last
     * I   rank
     * J   player
     * K   rate
     *
     * ヘッダーは表示せず、上部のLabelで3区画のタイトルを表示する。
     * 横スクロールは使用せず、必要な場合だけ縦スクロールする。
     *
     * 右側のランキングはプレイヤー単位。
     * 「そのプレイヤーを相手にしたとき、自分が一度も勝っていない」
     * 場合は、勝率ではなく総対戦数を表示する。
     * ============================================================
     */
    public class PlayerStatisticsForm : Form
    {
        private readonly TskDatabaseReader database;
        private readonly CombinedPlayersDatabase combinedDatabase;
        private readonly ViewerConfig config;

        private NumericUpDown nudRecentDays;
        private NumericUpDown nudRecentMatches;
        private NumericUpDown nudRankingMinMatches;
        private ComboBox cmbStrongestFilter;
        private DataGridView dgvPlayerTable;

        private Dictionary<string, PlayerAggregate> playersByKey =
            new Dictionary<string, PlayerAggregate>(StringComparer.Ordinal);

        private Dictionary<string, PlayerAggregate> playersByProfile =
            new Dictionary<string, PlayerAggregate>(StringComparer.Ordinal);

        /*
         * スクロールバー分の20px横に伸ばす
         */
        private const int FORM_WIDTH = 750;
        private const int FORM_HEIGHT = 600;

        private const int SETTING_PANEL_HEIGHT = 32;
        private const int TABLE_TITLE_HEIGHT = 22;

        /*
         * A～Kの列幅。
         * 合計697px。
         * 730pxフォーム内で縦スクロールバーが出ても
         * 横スクロールが発生しないように余裕を残す。
         */
        private const int WIDTH_A = 34;
        private const int WIDTH_B = 102;
        private const int WIDTH_C = 52;
        private const int WIDTH_D = 106;
        private const int WIDTH_E = 34;
        private const int WIDTH_F = 34;
        private const int WIDTH_G = 56;
        private const int WIDTH_H = 78;
        private const int WIDTH_I = 34;
        private const int WIDTH_J = 106;
        private const int WIDTH_K = 61;

        private const int LEFT_GROUP_WIDTH =
            WIDTH_A + WIDTH_B + WIDTH_C;

        private const int CENTER_GROUP_WIDTH =
            WIDTH_D + WIDTH_E + WIDTH_F + WIDTH_G + WIDTH_H;

        private const int RIGHT_GROUP_WIDTH =
            WIDTH_I + WIDTH_J + WIDTH_K;

        public PlayerStatisticsForm(
            TskDatabaseReader database,
            CombinedPlayersDatabase combinedDatabase)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            if (combinedDatabase == null)
            {
                throw new ArgumentNullException("combinedDatabase");
            }

            this.database = database;
            this.combinedDatabase = combinedDatabase;
            config = new ViewerConfig();

            InitializeForm();
            LoadStatistics();
        }

        /*
         * ============================================================
         * フォーム初期化
         * ============================================================
         */
        private void InitializeForm()
        {
            Text = "プレイヤー表";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(FORM_WIDTH, FORM_HEIGHT);
            MinimumSize = new Size(FORM_WIDTH, FORM_HEIGHT);
            MaximumSize = new Size(FORM_WIDTH, FORM_HEIGHT);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ShowIcon = false;
            Font = new Font("MS Gothic", 9.0f);
            BackColor = Color.White;
            AutoScroll = false;

            CreateTableArea();
            CreateSettingPanel();
        }

        /*
         * ============================================================
         * 上部設定
         * ============================================================
         */
        private void CreateSettingPanel()
        {
            Panel settingPanel = new Panel();
            settingPanel.Dock = DockStyle.Top;
            settingPanel.Height = SETTING_PANEL_HEIGHT;
            settingPanel.BackColor = Color.White;
            settingPanel.Margin = new Padding(0);
            settingPanel.Padding = new Padding(6, 0, 6, 0);

            Controls.Add(settingPanel);

            int x = 6;

            AddNumberSetting(
                settingPanel,
                "過去",
                config.PlayerStatsRecentDays,
                1,
                3650,
                "日間",
                ref x,
                out nudRecentDays);

            AddNumberSetting(
                settingPanel,
                "直近",
                config.PlayerStatsRecentMatches,
                1,
                10000,
                "戦",
                ref x,
                out nudRecentMatches);

            AddNumberSetting(
                settingPanel,
                "ランキング最低",
                config.PlayerStatsRankingMinMatches,
                1,
                10000,
                "戦",
                ref x,
                out nudRankingMinMatches);

            Label filterLabel = new Label();
            filterLabel.Text = "ランキング条件";
            filterLabel.AutoSize = true;
            filterLabel.Location = new Point(x + 2, 8);
            filterLabel.Margin = new Padding(0);
            settingPanel.Controls.Add(filterLabel);
            x += filterLabel.PreferredWidth + 4;

            cmbStrongestFilter = new ComboBox();
            cmbStrongestFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStrongestFilter.FlatStyle = FlatStyle.Standard;
            cmbStrongestFilter.Font = new Font("MS Gothic", 9.0f);
            cmbStrongestFilter.Items.Add("すべて表示");
            cmbStrongestFilter.Items.Add("全敗相手非表示");
            cmbStrongestFilter.Items.Add("全敗相手のみ表示");
            int rankingCondition =
                config.PlayerStatsRankingCondition;

            if (rankingCondition < 0 ||
                rankingCondition > 2)
            {
                rankingCondition = 0;
            }

            /*
             * INIに保存されているランキング条件を
             * 起動時の初期選択へ反映する。
             *
             * 0 = すべて表示
             * 1 = 全敗相手非表示
             * 2 = 全敗相手のみ表示
             */
            cmbStrongestFilter.SelectedIndex =
                rankingCondition;

            cmbStrongestFilter.Size = new Size(122, 24);
            cmbStrongestFilter.Location = new Point(x, 4);
            cmbStrongestFilter.Margin = new Padding(0);
            cmbStrongestFilter.SelectedIndexChanged +=
                CmbStrongestFilter_SelectedIndexChanged;
            settingPanel.Controls.Add(cmbStrongestFilter);
            x += 128;

            Button refreshButton = new Button();
            refreshButton.Text = "更新";
            refreshButton.Size = new Size(56, 24);
            refreshButton.Location = new Point(x, 4);
            refreshButton.Margin = new Padding(0);
            refreshButton.Click += delegate
            {
                SaveSettings();
                LoadStatistics();
            };
            settingPanel.Controls.Add(refreshButton);
        }

        private void AddNumberSetting(
            Panel panel,
            string prefix,
            int currentValue,
            int minimum,
            int maximum,
            string suffix,
            ref int x,
            out NumericUpDown number)
        {
            Label prefixLabel = new Label();
            prefixLabel.Text = prefix;
            prefixLabel.AutoSize = true;
            prefixLabel.Location = new Point(x, 8);
            prefixLabel.Margin = new Padding(0);
            panel.Controls.Add(prefixLabel);
            x += prefixLabel.PreferredWidth + 2;

            number = new NumericUpDown();
            number.Minimum = minimum;
            number.Maximum = maximum;
            number.Value = Math.Max(
                minimum,
                Math.Min(maximum, currentValue));
            number.DecimalPlaces = 0;
            number.Increment = 1;
            number.Size = new Size(55, 24);
            number.Location = new Point(x, 4);
            number.Margin = new Padding(0);
            panel.Controls.Add(number);
            x += 58;

            Label suffixLabel = new Label();
            suffixLabel.Text = suffix;
            suffixLabel.AutoSize = true;
            suffixLabel.Location = new Point(x, 8);
            suffixLabel.Margin = new Padding(0);
            panel.Controls.Add(suffixLabel);
            x += suffixLabel.PreferredWidth + 8;
        }

        /*
         * ============================================================
         * 表エリア
         * ============================================================
         */
        private void CreateTableArea()
        {
            Panel tableArea = new Panel();
            tableArea.Dock = DockStyle.Fill;
            tableArea.BackColor = Color.White;
            tableArea.Padding = new Padding(6, 0, 6, 6);
            tableArea.Margin = new Padding(0);

            Controls.Add(tableArea);

            Panel titlePanel = new Panel();
            titlePanel.Dock = DockStyle.Top;
            titlePanel.Height = TABLE_TITLE_HEIGHT;
            titlePanel.BackColor = Color.White;
            titlePanel.Margin = new Padding(0);
            titlePanel.Padding = new Padding(0);

            Label leftTitle = CreateSectionTitle(
                "過去N日間の対戦数ランキング",
                LEFT_GROUP_WIDTH,
                0);

            Label centerTitle = CreateSectionTitle(
                "直近N戦の連続対戦",
                CENTER_GROUP_WIDTH,
                LEFT_GROUP_WIDTH);

            Label rightTitle = CreateSectionTitle(
                "プレイヤーランキング",
                RIGHT_GROUP_WIDTH,
                LEFT_GROUP_WIDTH + CENTER_GROUP_WIDTH);

            titlePanel.Controls.Add(leftTitle);
            titlePanel.Controls.Add(centerTitle);
            titlePanel.Controls.Add(rightTitle);

            tableArea.Controls.Add(dgvPlayerTable = CreateGrid());
            tableArea.Controls.Add(titlePanel);
        }

        private Label CreateSectionTitle(
            string text,
            int width,
            int left)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.Size = new Size(width, TABLE_TITLE_HEIGHT);
            label.Location = new Point(left, 0);
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.BackColor = Color.White;
            label.ForeColor = Color.Black;
            label.Margin = new Padding(0);
            label.Padding = new Padding(0);
            return label;
        }

        /*
         * ============================================================
         * DataGridView
         * ============================================================
         */
        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();

            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            grid.GridColor = Color.Silver;

            grid.RowHeadersVisible = false;
            grid.ColumnHeadersVisible = false;

            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;

            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.AutoGenerateColumns = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.ScrollBars = ScrollBars.Vertical;
            grid.ShowCellToolTips = true;
            grid.RowTemplate.Height = 18;

            grid.EnableHeadersVisualStyles = false;

            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.Black;
            grid.DefaultCellStyle.SelectionBackColor = Color.White;
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.DefaultCellStyle.Font = new Font("MS Gothic", 8.0f);
            grid.DefaultCellStyle.Padding = new Padding(1, 0, 1, 0);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.DefaultCellStyle.NullValue = "";

            grid.RowHeadersDefaultCellStyle.BackColor = Color.White;
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = Color.White;

            AddTableColumn(
                grid,
                "A",
                WIDTH_A,
                DataGridViewContentAlignment.MiddleCenter);

            AddTableColumn(
                grid,
                "B",
                WIDTH_B,
                DataGridViewContentAlignment.MiddleLeft);

            AddTableColumn(
                grid,
                "C",
                WIDTH_C,
                DataGridViewContentAlignment.MiddleRight,
                new Padding(1, 0, 6, 0));

            AddTableColumn(
                grid,
                "D",
                WIDTH_D,
                DataGridViewContentAlignment.MiddleLeft,
                new Padding(4, 0, 1, 0));

            AddTableColumn(
                grid,
                "E",
                WIDTH_E,
                DataGridViewContentAlignment.MiddleRight);

            AddTableColumn(
                grid,
                "F",
                WIDTH_F,
                DataGridViewContentAlignment.MiddleRight);

            AddTableColumn(
                grid,
                "G",
                WIDTH_G,
                DataGridViewContentAlignment.MiddleRight);

            AddTableColumn(
                grid,
                "H",
                WIDTH_H,
                DataGridViewContentAlignment.MiddleRight,
                new Padding(1, 0, 6, 0));

            AddTableColumn(
                grid,
                "I",
                WIDTH_I,
                DataGridViewContentAlignment.MiddleCenter,
                new Padding(4, 0, 1, 0));

            AddTableColumn(
                grid,
                "J",
                WIDTH_J,
                DataGridViewContentAlignment.MiddleLeft);

            AddTableColumn(
                grid,
                "K",
                WIDTH_K,
                DataGridViewContentAlignment.MiddleRight);

            grid.CellDoubleClick += Grid_CellDoubleClick;

            return grid;
        }

        private void AddTableColumn(
            DataGridView grid,
            string name,
            int width,
            DataGridViewContentAlignment alignment)
        {
            AddTableColumn(
                grid,
                name,
                width,
                alignment,
                new Padding(1, 0, 1, 0));
        }

        private void AddTableColumn(
            DataGridView grid,
            string name,
            int width,
            DataGridViewContentAlignment alignment,
            Padding padding)
        {
            DataGridViewTextBoxColumn column =
                new DataGridViewTextBoxColumn();

            column.Name = name;
            column.HeaderText = "";
            column.Width = width;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.Resizable = DataGridViewTriState.False;
            column.DefaultCellStyle.Alignment = alignment;
            column.DefaultCellStyle.Padding = padding;

            grid.Columns.Add(column);
        }

        /*
         * ============================================================
         * 全体再計算
         * ============================================================
         */
        private void LoadStatistics()
        {
            try
            {
                BuildPlayerAggregates();
                LoadAllTableData();
                ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "プレイヤー表の読み込みに失敗しました。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadAllTableData()
        {
            List<RecentDaysRow> recentDays =
                BuildRecentDaysRanking();

            List<MatchRun> recentRuns =
                BuildRecentRuns();

            List<StrongestRow> strongest =
                BuildStrongestRanking();

            PopulateTable(
                recentDays,
                recentRuns,
                strongest);
        }

        /*
         * ============================================================
         * プレイヤー単位へ統合
         * ============================================================
         */
        private void BuildPlayerAggregates()
        {
            playersByKey =
                new Dictionary<string, PlayerAggregate>(
                    StringComparer.Ordinal);

            playersByProfile =
                new Dictionary<string, PlayerAggregate>(
                    StringComparer.Ordinal);

            List<CombinedPlayer> players =
                combinedDatabase.GetPlayers();

            foreach (CombinedPlayer player in players)
            {
                if (player == null)
                {
                    continue;
                }

                string key =
                    "P:" + player.PlayerId.ToString();

                PlayerAggregate aggregate =
                    new PlayerAggregate();

                aggregate.Key = key;
                aggregate.PlayerId = player.PlayerId;
                aggregate.Player = player;
                aggregate.DisplayName =
                    GetPlayerDisplayName(player);

                if (player.Profiles != null)
                {
                    aggregate.Profiles =
                        player.Profiles
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Select(x => NormalizeProfile(x))
                            .Distinct(StringComparer.Ordinal)
                            .ToList();
                }

                if (!string.IsNullOrWhiteSpace(
                    player.RepresentativeProfile))
                {
                    string representative =
                        NormalizeProfile(player.RepresentativeProfile);

                    if (!aggregate.Profiles.Contains(
                        representative,
                        StringComparer.Ordinal))
                    {
                        aggregate.Profiles.Insert(
                            0,
                            representative);
                    }
                }

                playersByKey[key] = aggregate;

                foreach (string profile in aggregate.Profiles)
                {
                    playersByProfile[profile] = aggregate;
                }
            }

            List<TskMatchRecord> allRecords =
                database.GetAllMatchRecords();

            foreach (TskMatchRecord record in allRecords)
            {
                string profile =
                    NormalizeProfile(record.P2Name);

                if (profile.Length == 0)
                {
                    continue;
                }

                PlayerAggregate aggregate;

                if (!playersByProfile.TryGetValue(
                    profile,
                    out aggregate))
                {
                    string key = "U:" + profile;

                    if (!playersByKey.TryGetValue(
                        key,
                        out aggregate))
                    {
                        aggregate = new PlayerAggregate();
                        aggregate.Key = key;
                        aggregate.PlayerId = -1;
                        aggregate.DisplayName = profile;
                        aggregate.Player = null;
                        aggregate.Profiles.Add(profile);
                        playersByKey[key] = aggregate;
                    }

                    playersByProfile[profile] = aggregate;
                }

                aggregate.Records.Add(record);

                if (!aggregate.Profiles.Contains(
                    profile,
                    StringComparer.Ordinal))
                {
                    aggregate.Profiles.Add(profile);
                }
            }

            foreach (PlayerAggregate aggregate in playersByKey.Values)
            {
                aggregate.DisplayName =
                    string.IsNullOrWhiteSpace(aggregate.DisplayName)
                        ? "不明"
                        : aggregate.DisplayName.Trim();

                aggregate.Profiles =
                    aggregate.Profiles
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                aggregate.Records =
                    aggregate.Records
                        .OrderBy(x => x.DateTime)
                        .ToList();
            }
        }

        private string GetPlayerDisplayName(
            CombinedPlayer player)
        {
            if (player == null)
            {
                return "不明";
            }

            string displayName =
                combinedDatabase.GetDisplayName(player);

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(
                player.RepresentativeProfile))
            {
                return player.RepresentativeProfile.Trim();
            }

            if (player.Profiles != null)
            {
                string firstProfile =
                    player.Profiles.FirstOrDefault(
                        x => !string.IsNullOrWhiteSpace(x));

                if (!string.IsNullOrWhiteSpace(firstProfile))
                {
                    return firstProfile.Trim();
                }
            }

            return "不明";
        }

        private string NormalizeProfile(string value)
        {
            if (value == null)
            {
                return "";
            }

            return value.Trim();
        }

        /*
         * ============================================================
         * 左：過去N日間の対戦数ランキング
         * ============================================================
         */
        private List<RecentDaysRow> BuildRecentDaysRanking()
        {
            int days =
                Decimal.ToInt32(nudRecentDays.Value);

            DateTime from =
                DateTime.Now.AddDays(-days);

            List<RecentDaysRow> result =
                playersByKey.Values
                    .Select(
                        x => new RecentDaysRow
                        {
                            Aggregate = x,
                            BattleCount = x.Records.Count(
                                r => r.DateTime >= from)
                        })
                    .Where(x => x.BattleCount > 0)
                    .OrderByDescending(x => x.BattleCount)
                    .ThenBy(
                        x => x.Aggregate.DisplayName,
                        StringComparer.CurrentCulture)
                    .ToList();

            for (int i = 0; i < result.Count; i++)
            {
                result[i].Rank = i + 1;
            }

            return result;
        }

        /*
         * ============================================================
         * 中央：直近N戦の連続対戦
         * ============================================================
         *
         * 日付が変わっても、P2プロファイル名が同じなら同じrun。
         * P2プロファイルが変わった時だけrunを分ける。
         * 新しいrunを上へ並べる。
         */
        private List<MatchRun> BuildRecentRuns()
        {
            int count =
                Decimal.ToInt32(nudRecentMatches.Value);

            List<TskMatchRecord> records =
                database.GetRecentMatchRecords(count);

            List<MatchRun> runs =
                new List<MatchRun>();

            if (records.Count == 0)
            {
                return runs;
            }

            MatchRun current = null;

            foreach (TskMatchRecord record in records)
            {
                string profile =
                    NormalizeProfile(record.P2Name);

                if (current == null ||
                    !string.Equals(
                        current.Profile,
                        profile,
                        StringComparison.Ordinal))
                {
                    current = new MatchRun();
                    current.Profile = profile;
                    current.Start = record.DateTime;
                    current.End = record.DateTime;
                    runs.Add(current);
                }

                current.End = record.DateTime;
                current.Matches++;

                /*
                 * DBはP1/P2のラウンド数。
                 * 本アプリの自分はP1、相手はP2として扱う。
                 * P1が2 → 自分の勝ち
                 * P2が2 → 自分の負け
                 */
                if (record.P1RoundCount >= 2)
                {
                    current.Wins++;
                }
                else if (record.P2RoundCount >= 2)
                {
                    current.Losses++;
                }
            }

            foreach (MatchRun run in runs)
            {
                PlayerAggregate aggregate;

                if (playersByProfile.TryGetValue(
                    run.Profile,
                    out aggregate))
                {
                    run.Aggregate = aggregate;
                }
            }

            return runs
                .OrderByDescending(x => x.End)
                .ToList();
        }

        /*
         * ============================================================
         * 右：最強プレイヤーランキング
         * ============================================================
         *
         * 勝率は「そのプレイヤー自身の勝率」。
         * P2 = 相手なので、P2の勝利数を分子にする。
         *
         * 特例：自分の勝ちが1回もない相手
         *        （P1勝利数が0）は勝率ではなく総対戦数を表示。
         */
        private List<StrongestRow> BuildStrongestRanking()
        {
            int minimumMatches =
                Decimal.ToInt32(nudRankingMinMatches.Value);

            int filter =
                cmbStrongestFilter == null
                    ? 0
                    : cmbStrongestFilter.SelectedIndex;

            IEnumerable<PlayerAggregate> source =
                playersByKey.Values
                    .Where(x => x.Records.Count >= minimumMatches);

            if (filter == 1)
            {
                /* 全敗相手非表示 = 自分の勝ちが0の相手を除外 */
                source = source.Where(x => GetSelfWins(x) > 0);
            }
            else if (filter == 2)
            {
                /* 全敗相手のみ表示 = 自分の勝ちが0の相手だけ */
                source = source.Where(x => GetSelfWins(x) == 0);
            }

            List<PlayerAggregate> ranking =
                source
                    .OrderByDescending(x => GetOpponentWinRate(x))
                    .ThenByDescending(x => x.Records.Count)
                    .ThenBy(
                        x => x.DisplayName,
                        StringComparer.CurrentCulture)
                    .ToList();

            List<StrongestRow> result =
                new List<StrongestRow>();

            for (int i = 0; i < ranking.Count; i++)
            {
                PlayerAggregate aggregate = ranking[i];

                StrongestRow row = new StrongestRow();
                row.Rank = i + 1;
                row.Aggregate = aggregate;
                row.ShowBattleCount =
                    GetSelfWins(aggregate) == 0;

                row.DisplayValue =
                    row.ShowBattleCount
                        ? aggregate.Records.Count.ToString() + "戦"
                        : FormatRate(
                            GetOpponentWins(aggregate),
                            GetSelfWins(aggregate));

                result.Add(row);
            }

            return result;
        }

        private int GetSelfWins(PlayerAggregate aggregate)
        {
            if (aggregate == null)
            {
                return 0;
            }

            return aggregate.Records.Count(
                x => x.P1RoundCount >= 2);
        }

        private int GetOpponentWins(PlayerAggregate aggregate)
        {
            if (aggregate == null)
            {
                return 0;
            }

            return aggregate.Records.Count(
                x => x.P2RoundCount >= 2);
        }

        private double GetOpponentWinRate(
            PlayerAggregate aggregate)
        {
            int wins = GetOpponentWins(aggregate);
            int losses = GetSelfWins(aggregate);
            int total = wins + losses;

            if (total <= 0)
            {
                return 0.0;
            }

            return wins * 100.0 / total;
        }

        /*
         * ============================================================
         * 表へ投入
         * ============================================================
         */
        private void PopulateTable(
            List<RecentDaysRow> recentDays,
            List<MatchRun> recentRuns,
            List<StrongestRow> strongest)
        {
            dgvPlayerTable.Rows.Clear();

            int rowCount = Math.Max(
                recentDays.Count,
                Math.Max(
                    recentRuns.Count,
                    strongest.Count));

            if (rowCount == 0)
            {
                int emptyRow = dgvPlayerTable.Rows.Add();
                dgvPlayerTable.Rows[emptyRow].Cells[1].Value = "該当なし";
                dgvPlayerTable.Rows[emptyRow].Cells[4].Value = "該当なし";
                dgvPlayerTable.Rows[emptyRow].Cells[9].Value = "該当なし";
                return;
            }

            for (int i = 0; i < rowCount; i++)
            {
                int rowIndex = dgvPlayerTable.Rows.Add();
                DataGridViewRow row = dgvPlayerTable.Rows[rowIndex];

                if (i < recentDays.Count)
                {
                    RecentDaysRow left = recentDays[i];

                    row.Cells[0].Value =
                        left.Rank.ToString();

                    SetPlayerNameCell(
                        row.Cells[1],
                        left.Aggregate.DisplayName);

                    row.Cells[2].Value =
                        left.BattleCount.ToString();

                    SetSectionTag(
                        row,
                        0,
                        2,
                        left.Aggregate);
                }

                if (i < recentRuns.Count)
                {
                    MatchRun center = recentRuns[i];

                    PlayerAggregate centerAggregate =
                        center.Aggregate;

                    string centerDisplayName =
                        centerAggregate == null
                            ? center.Profile
                            : centerAggregate.DisplayName;

                    SetPlayerNameCell(
                        row.Cells[3],
                        centerDisplayName);

                    row.Cells[4].Value =
                        center.Wins.ToString();

                    row.Cells[5].Value =
                        center.Losses.ToString();

                    row.Cells[6].Value =
                        FormatRate(
                            center.Wins,
                            center.Losses);

                    row.Cells[7].Value =
                        FormatLastDate(center.End);

                    if (centerAggregate != null)
                    {
                        SetSectionTag(
                            row,
                            3,
                            7,
                            centerAggregate);
                    }
                }

                if (i < strongest.Count)
                {
                    StrongestRow right = strongest[i];

                    row.Cells[8].Value =
                        right.Rank.ToString();

                    SetPlayerNameCell(
                        row.Cells[9],
                        right.Aggregate.DisplayName);

                    row.Cells[10].Value =
                        right.DisplayValue;

                    SetSectionTag(
                        row,
                        8,
                        10,
                        right.Aggregate);
                }
            }

            ClearSelection();
        }

        /*
         * ============================================================
         * プレイヤー名セル
         * ============================================================
         *
         * 表示は6文字まで。
         * 7文字以上は先頭5文字 + ".." に省略する。
         *
         * 省略された場合はセルへマウスを重ねたときに
         * 元のプレイヤー名をツールチップで表示する。
         */
        private void SetPlayerNameCell(
            DataGridViewCell cell,
            string fullName)
        {
            if (cell == null)
            {
                return;
            }

            string name =
                fullName ?? "";

            cell.Value =
                ShortenPlayerName(name);

            if (name.Length > 6)
            {
                cell.ToolTipText = name;
            }
            else
            {
                cell.ToolTipText = "";
            }
        }

        private void SetSectionTag(
            DataGridViewRow row,
            int startColumn,
            int endColumn,
            PlayerAggregate aggregate)
        {
            for (int i = startColumn; i <= endColumn; i++)
            {
                row.Cells[i].Tag = aggregate;
            }
        }

        /*
         * ============================================================
         * 名前短縮
         * ============================================================
         *
         * 6文字まではそのまま。
         * 7文字以上は先頭5文字 + ".."。
         * 例：ABCDEFG → ABCDE..
         */
        private string ShortenPlayerName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (value.Length <= 6)
            {
                return value;
            }

            return value.Substring(0, 5) + "..";
        }

        private string FormatLastDate(DateTime value)
        {
            if (value == DateTime.MinValue)
            {
                return "";
            }

            return value.AddHours(-9).ToString("MMddHHmm");
        }

        private string FormatRate(
            int wins,
            int losses)
        {
            int total = wins + losses;

            if (total <= 0)
            {
                return "---";
            }

            return (
                wins * 100.0 / total)
                .ToString("0.0") +
                "%";
        }

        /*
         * ============================================================
         * ダブルクリック
         * ============================================================
         *
         * 表をダブルクリックしたときだけ、
         * その行に対応するプレイヤー詳細を開く。
         */
        private void Grid_CellDoubleClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 ||
                e.ColumnIndex < 0 ||
                e.RowIndex >= dgvPlayerTable.Rows.Count)
            {
                return;
            }

            DataGridViewCell cell =
                dgvPlayerTable.Rows[e.RowIndex]
                    .Cells[e.ColumnIndex];

            PlayerAggregate aggregate =
                cell.Tag as PlayerAggregate;

            if (aggregate != null)
            {
                OpenPlayerDetail(aggregate);
            }

            ClearSelection();
        }

        private void OpenPlayerDetail(
            PlayerAggregate aggregate)
        {
            if (aggregate == null)
            {
                return;
            }

            List<string> profiles =
                aggregate.Profiles
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

            if (profiles.Count == 0 &&
                aggregate.Player != null &&
                aggregate.Player.Profiles != null)
            {
                profiles =
                    aggregate.Player.Profiles
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.Ordinal)
                        .ToList();
            }

            if (profiles.Count == 0)
            {
                return;
            }

            try
            {
                ProfileSearchForm form =
                    new ProfileSearchForm(
                        database,
                        combinedDatabase,
                        aggregate.DisplayName,
                        profiles);

                form.Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "プレイヤー詳細を開けませんでした。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * ============================================================
         * フィルター
         * ============================================================
         */
        private void CmbStrongestFilter_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            try
            {
                /*
                 * 選択変更をその場でINIへ保存する。
                 */
                SaveSettings();

                if (dgvPlayerTable == null)
                {
                    return;
                }

                LoadAllTableData();
                ClearSelection();
            }
            catch
            {
            }
        }

        private void ClearSelection()
        {
            if (dgvPlayerTable == null)
            {
                return;
            }

            dgvPlayerTable.ClearSelection();
            dgvPlayerTable.CurrentCell = null;
        }

        /*
         * ============================================================
         * 設定保存
         * ============================================================
         */
        private void SaveSettings()
        {
            try
            {
                int rankingCondition =
                    cmbStrongestFilter == null
                        ? config.PlayerStatsRankingCondition
                        : cmbStrongestFilter.SelectedIndex;

                if (rankingCondition < 0 ||
                    rankingCondition > 2)
                {
                    rankingCondition = 0;
                }

                config.SavePlayerStatsSettings(
                    Decimal.ToInt32(nudRecentDays.Value),
                    Decimal.ToInt32(nudRecentMatches.Value),
                    Decimal.ToInt32(nudRankingMinMatches.Value),
                    rankingCondition);
            }
            catch
            {
            }
        }

        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            try
            {
                SaveSettings();

                if (dgvPlayerTable != null)
                {
                    dgvPlayerTable.CellDoubleClick -= Grid_CellDoubleClick;
                }
            }
            catch
            {
            }

            base.OnFormClosed(e);
        }

        /*
         * ============================================================
         * 表示用クラス
         * ============================================================
         */
        private class RecentDaysRow
        {
            public int Rank;
            public int BattleCount;
            public PlayerAggregate Aggregate;
        }

        private class StrongestRow
        {
            public int Rank;
            public bool ShowBattleCount;
            public string DisplayValue;
            public PlayerAggregate Aggregate;
        }

        private class MatchRun
        {
            public string Profile;
            public int Matches;
            public int Wins;
            public int Losses;
            public DateTime Start;
            public DateTime End;
            public PlayerAggregate Aggregate;
        }

        private class PlayerAggregate
        {
            public string Key;
            public int PlayerId;
            public string DisplayName;
            public CombinedPlayer Player;
            public List<string> Profiles =
                new List<string>();
            public List<TskMatchRecord> Records =
                new List<TskMatchRecord>();
        }
    }
}
