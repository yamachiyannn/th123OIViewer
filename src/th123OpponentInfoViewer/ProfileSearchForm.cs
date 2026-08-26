using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace th123OpponentInfoViewer
{
    public class ProfileSearchForm : Form
    {
        private readonly TskDatabaseReader database;

        /*
         * --------------------------------
         * ViewerConfig
         * --------------------------------
         */
        private readonly ViewerConfig config;

        /*
         * --------------------------------
         * コントロール
         * --------------------------------
         */

        /*
         * 左側の検索フォーム。
         */
        private TextBox txtSearch;

        /*
         * 単数 / 複数 切替ボタン。
         */
        private Button btnMode;

        /*
         * 上側リスト。
         *
         * 未選択かつ、
         * 現在の検索条件に該当する
         * プロファイルを表示。
         */
        private ListBox lstAvailable;

        /*
         * 下側リスト。
         *
         * 選択済みプロファイルを保持。
         */
        private ListBox lstSelected;

        /*
         * 右側の詳細表示。
         */
        private TextBox txtResult;

        /*
         * 詳細表示の右クリックメニュー。
         */
        private ContextMenuStrip resultContextMenu;

        /*
         * --------------------------------
         * フォントサイズ
         * --------------------------------
         *
         * 初期値はViewerConfigから取得。
         *
         * 右クリックメニューで変更した場合は
         * この値を上書きする。
         */
        private float resultFontSize;

        private float listFontSize;

        /*
         * --------------------------------
         * レイアウト固定値
         * --------------------------------
         */
        private const int LEFT_MARGIN = 15;

        private const int LEFT_WIDTH = 310;

        private const int RESULT_LEFT = 340;

        private const int TOP_MARGIN = 15;

        private const int SEARCH_HEIGHT = 25;

        private const int LIST_TOP = 55;

        private const int BOTTOM_MARGIN = 15;

        /*
         * --------------------------------
         * プロファイルデータ
         * --------------------------------
         */

        /*
         * 全プロファイル一覧。
         */
        private List<string> allProfileNames =
            new List<string>();

        /*
         * 現在選択済みのプロファイル。
         *
         * 検索条件とは独立して保持する。
         */
        private List<string> selectedProfileNames =
            new List<string>();

        /*
         * true  = 複数モード
         * false = 単数モード
         *
         * デフォルトは単数。
         */
        private bool multipleMode =
            false;

        /*
         * リスト更新中か。
         */
        private bool updatingLists =
            false;

        /*
         * --------------------------------
         * コンストラクタ
         * --------------------------------
         */
        public ProfileSearchForm(
            TskDatabaseReader database)
        {
            this.database =
                database;

            /*
             * ViewerConfigを読み込む。
             *
             * ProfileSearchFontSizeは
             * iniの値が使用される。
             */
            config =
                new ViewerConfig();

            resultFontSize =
                config.ProfileSearchFontSize;

            listFontSize =
                config.ProfileSearchFontSize;

            InitializeForm();

            LoadProfileNames();
        }

        /*
         * --------------------------------
         * フォーム初期化
         * --------------------------------
         */
        private void InitializeForm()
        {
            /*
             * --------------------------------
             * フォーム
             * --------------------------------
             */
            this.Text =
                "プロファイル検索";

            this.StartPosition =
                FormStartPosition.CenterParent;

            this.Size =
                new Size(
                    1000,
                    750);

            this.MinimumSize =
                new Size(
                    750,
                    550);

            /*
             * --------------------------------
             * 検索フォーム
             * --------------------------------
             */
            txtSearch =
                new TextBox();

            txtSearch.Location =
                new Point(
                    LEFT_MARGIN,
                    TOP_MARGIN);

            txtSearch.Size =
                new Size(
                    230,
                    SEARCH_HEIGHT);

            txtSearch.Font =
                CreateFont(
                    listFontSize);

            txtSearch.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Left;

            txtSearch.KeyDown +=
                TxtSearch_KeyDown;

            /*
             * --------------------------------
             * 単数 / 複数ボタン
             * --------------------------------
             */
            btnMode =
                new Button();

            btnMode.Text =
                "単数";

            btnMode.Location =
                new Point(
                    255,
                    13);

            btnMode.Size =
                new Size(
                    70,
                    30);

            btnMode.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Left;

            btnMode.Click +=
                BtnMode_Click;

            /*
             * --------------------------------
             * 上側リスト
             * --------------------------------
             */
            lstAvailable =
                new ListBox();

            lstAvailable.Location =
                new Point(
                    LEFT_MARGIN,
                    LIST_TOP);

            lstAvailable.Size =
                new Size(
                    LEFT_WIDTH,
                    500);

            lstAvailable.SelectionMode =
                SelectionMode.One;

            lstAvailable.Font =
                CreateFont(
                    listFontSize);

            lstAvailable.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Left |
                AnchorStyles.Bottom;

            lstAvailable.Click +=
                LstAvailable_Click;

            /*
             * --------------------------------
             * 下側リスト
             * --------------------------------
             */
            lstSelected =
                new ListBox();

            lstSelected.Location =
                new Point(
                    LEFT_MARGIN,
                    565);

            lstSelected.Size =
                new Size(
                    LEFT_WIDTH,
                    140);

            lstSelected.SelectionMode =
                SelectionMode.One;

            lstSelected.Font =
                CreateFont(
                    listFontSize);

            lstSelected.Anchor =
                AnchorStyles.Left |
                AnchorStyles.Bottom;

            lstSelected.Click +=
                LstSelected_Click;

            /*
             * --------------------------------
             * 右側詳細表示
             * --------------------------------
             */
            txtResult =
                new TextBox();

            txtResult.Location =
                new Point(
                    RESULT_LEFT,
                    TOP_MARGIN);

            txtResult.Size =
                new Size(
                    Math.Max(
                        100,
                        ClientSize.Width -
                        RESULT_LEFT -
                        BOTTOM_MARGIN),
                    ClientSize.Height -
                    TOP_MARGIN -
                    BOTTOM_MARGIN);

            txtResult.Multiline =
                true;

            txtResult.ReadOnly =
                true;

            txtResult.ScrollBars =
                ScrollBars.Both;

            txtResult.WordWrap =
                false;

            txtResult.Font =
                CreateFont(
                    resultFontSize);

            txtResult.BackColor =
                Color.White;

            txtResult.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left |
                AnchorStyles.Right;

            /*
             * --------------------------------
             * 右クリックメニュー
             * --------------------------------
             */
            CreateResultContextMenu();

            /*
             * --------------------------------
             * コントロール追加
             * --------------------------------
             */
            Controls.Add(
                txtSearch);

            Controls.Add(
                btnMode);

            Controls.Add(
                lstAvailable);

            Controls.Add(
                lstSelected);

            Controls.Add(
                txtResult);

            /*
             * --------------------------------
             * 初期レイアウト
             * --------------------------------
             */
            UpdateListLayout();
        }

        /*
         * --------------------------------
         * フォント生成
         * --------------------------------
         */
        private Font CreateFont(
            float size)
        {
            return new Font(
                "MS Gothic",
                size);
        }

        /*
         * --------------------------------
         * リスト配置更新
         * --------------------------------
         */
        private void UpdateListLayout()
        {
            int top =
                LIST_TOP;

            int bottomMargin =
                BOTTOM_MARGIN;

            int totalHeight =
                ClientSize.Height -
                top -
                bottomMargin;

            if (totalHeight < 100)
            {
                totalHeight =
                    100;
            }

            /*
             * 上側リストを約4/5。
             */
            int availableHeight =
                (int)(
                    totalHeight *
                    0.8);

            /*
             * 下側リストを約1/5。
             */
            int selectedHeight =
                totalHeight -
                availableHeight;

            /*
             * 最低高さを確保。
             */
            if (selectedHeight < 60)
            {
                selectedHeight =
                    60;

                availableHeight =
                    totalHeight -
                    selectedHeight;
            }

            if (availableHeight < 40)
            {
                availableHeight =
                    40;
            }

            /*
             * --------------------------------
             * 上側リスト
             * --------------------------------
             */
            lstAvailable.Location =
                new Point(
                    LEFT_MARGIN,
                    top);

            lstAvailable.Size =
                new Size(
                    LEFT_WIDTH,
                    availableHeight);

            /*
             * --------------------------------
             * 下側リスト
             * --------------------------------
             */
            lstSelected.Location =
                new Point(
                    LEFT_MARGIN,
                    top +
                    availableHeight);

            lstSelected.Size =
                new Size(
                    LEFT_WIDTH,
                    selectedHeight);
        }

        /*
         * --------------------------------
         * フォームサイズ変更
         * --------------------------------
         */
        protected override void OnResize(
            EventArgs e)
        {
            base.OnResize(e);

            if (IsHandleCreated)
            {
                UpdateListLayout();
            }
        }

        /*
         * --------------------------------
         * 右クリックメニュー
         * --------------------------------
         */
        private void CreateResultContextMenu()
        {
            resultContextMenu =
                new ContextMenuStrip();

            ToolStripMenuItem menu8 =
                new ToolStripMenuItem(
                    "8 px");

            menu8.Click +=
                delegate
                {
                    SetResultFontSize(
                        8.0f);
                };

            ToolStripMenuItem menu10 =
                new ToolStripMenuItem(
                    "10 px");

            menu10.Click +=
                delegate
                {
                    SetResultFontSize(
                        10.0f);
                };

            ToolStripMenuItem menu14 =
                new ToolStripMenuItem(
                    "14 px");

            menu14.Click +=
                delegate
                {
                    SetResultFontSize(
                        14.0f);
                };

            ToolStripMenuItem menu20 =
                new ToolStripMenuItem(
                    "20 px");

            menu20.Click +=
                delegate
                {
                    SetResultFontSize(
                        20.0f);
                };

            resultContextMenu.Items.Add(
                menu8);

            resultContextMenu.Items.Add(
                menu10);

            resultContextMenu.Items.Add(
                menu14);

            resultContextMenu.Items.Add(
                menu20);

            txtResult.ContextMenuStrip =
                resultContextMenu;
        }

        /*
         * --------------------------------
         * 詳細表示フォントサイズ変更
         * --------------------------------
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
         * --------------------------------
         * プロファイル一覧読み込み
         * --------------------------------
         */
        private void LoadProfileNames()
        {
            try
            {
                List<string> names =
                    database.GetP2ProfileNames();

                allProfileNames =
                    names
                        .Distinct()
                        .OrderBy(
                            x => x)
                        .ToList();

                selectedProfileNames.Clear();

                RefreshProfileLists();

                txtResult.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "プロファイル一覧を読み込めませんでした。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * --------------------------------
         * 現在の検索条件取得
         * --------------------------------
         */
        private IEnumerable<string>
            GetFilteredProfileNames()
        {
            string keyword =
                txtSearch.Text;

            IEnumerable<string> filtered =
                allProfileNames;

            if (!string.IsNullOrWhiteSpace(
                keyword))
            {
                filtered =
                    filtered.Where(
                        x =>
                            x.IndexOf(
                                keyword,
                                StringComparison.OrdinalIgnoreCase) >=
                            0);
            }

            return filtered;
        }

        /*
         * --------------------------------
         * リスト更新
         * --------------------------------
         */
        private void RefreshProfileLists()
        {
            updatingLists =
                true;

            try
            {
                /*
                 * --------------------------------
                 * 上側
                 * --------------------------------
                 */
                lstAvailable.BeginUpdate();

                try
                {
                    lstAvailable.Items.Clear();

                    IEnumerable<string> filtered =
                        GetFilteredProfileNames();

                    foreach (string name in
                        filtered)
                    {
                        if (selectedProfileNames.Contains(
                            name))
                        {
                            continue;
                        }

                        lstAvailable.Items.Add(
                            name);
                    }
                }
                finally
                {
                    lstAvailable.EndUpdate();
                }

                /*
                 * --------------------------------
                 * 下側
                 * --------------------------------
                 */
                lstSelected.BeginUpdate();

                try
                {
                    lstSelected.Items.Clear();

                    foreach (string name in
                        selectedProfileNames)
                    {
                        lstSelected.Items.Add(
                            name);
                    }
                }
                finally
                {
                    lstSelected.EndUpdate();
                }
            }
            finally
            {
                updatingLists =
                    false;
            }
        }

        /*
         * --------------------------------
         * 検索文字入力
         * --------------------------------
         */
        private void TxtSearch_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.KeyCode !=
                Keys.Enter)
            {
                return;
            }

            e.SuppressKeyPress =
                true;

            PerformSearch();
        }

        /*
         * --------------------------------
         * 検索
         * --------------------------------
         */
        private void PerformSearch()
        {
            RefreshProfileLists();

            if (selectedProfileNames.Count > 0)
            {
                ShowProfiles(
                    selectedProfileNames);
            }
        }

        /*
         * --------------------------------
         * 単数 / 複数切替
         * --------------------------------
         */
        private void BtnMode_Click(
            object sender,
            EventArgs e)
        {
            multipleMode =
                !multipleMode;

            if (multipleMode)
            {
                btnMode.Text =
                    "複数";
            }
            else
            {
                btnMode.Text =
                    "単数";

                if (selectedProfileNames.Count > 1)
                {
                    string keep =
                        selectedProfileNames[0];

                    selectedProfileNames =
                        new List<string>
                        {
                            keep
                        };
                }
            }

            RefreshProfileLists();

            if (selectedProfileNames.Count > 0)
            {
                ShowProfiles(
                    selectedProfileNames);
            }
            else
            {
                txtResult.Clear();
            }
        }

        /*
         * --------------------------------
         * 上側リストクリック
         * --------------------------------
         */
        private void LstAvailable_Click(
            object sender,
            EventArgs e)
        {
            if (updatingLists)
            {
                return;
            }

            if (lstAvailable.SelectedItem == null)
            {
                return;
            }

            string name =
                lstAvailable.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            /*
             * 単数モード。
             */
            if (!multipleMode)
            {
                selectedProfileNames.Clear();

                selectedProfileNames.Add(
                    name);

                RefreshProfileLists();

                ShowProfiles(
                    selectedProfileNames);

                return;
            }

            /*
             * 複数モード。
             */
            if (!selectedProfileNames.Contains(
                name))
            {
                selectedProfileNames.Add(
                    name);
            }

            RefreshProfileLists();

            ShowProfiles(
                selectedProfileNames);
        }

        /*
         * --------------------------------
         * 下側リストクリック
         * --------------------------------
         */
        private void LstSelected_Click(
            object sender,
            EventArgs e)
        {
            if (updatingLists)
            {
                return;
            }

            if (lstSelected.SelectedItem == null)
            {
                return;
            }

            string name =
                lstSelected.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            /*
             * 単数モード。
             */
            if (!multipleMode)
            {
                selectedProfileNames.Clear();

                selectedProfileNames.Add(
                    name);

                RefreshProfileLists();

                ShowProfiles(
                    selectedProfileNames);

                return;
            }

            /*
             * 複数モード。
             *
             * 下側から削除。
             */
            selectedProfileNames.Remove(
                name);

            RefreshProfileLists();

            if (selectedProfileNames.Count > 0)
            {
                ShowProfiles(
                    selectedProfileNames);
            }
            else
            {
                txtResult.Clear();
            }
        }

        /*
         * --------------------------------
         * プロファイル詳細表示
         * --------------------------------
         */
        private void ShowProfiles(
            List<string> profileNames)
        {
            try
            {
                int totalMatches =
                    0;

                int totalSelfWins =
                    0;

                int totalOpponentWins =
                    0;

                int last30SelfWins =
                    0;

                int last30OpponentWins =
                    0;

                int last100SelfWins =
                    0;

                int last100OpponentWins =
                    0;

                int lastMonthSelfWins =
                    0;

                int lastMonthOpponentWins =
                    0;

                DateTime firstMatchDate =
                    DateTime.MaxValue;

                DateTime lastMatchDate =
                    DateTime.MinValue;

                DateTime lastWinDate =
                    DateTime.MinValue;

                DateTime lastLossDate =
                    DateTime.MinValue;

                bool hasLastWinDate =
                    false;

                bool hasLastLossDate =
                    false;

                Dictionary<int, TskCharacterStats>
                    combinedCharacterStats =
                        new Dictionary<int, TskCharacterStats>();

                bool hasAnyRecords =
                    false;

                foreach (string profileName in
                    profileNames)
                {
                    TskOpponentStats stats =
                        database.GetPlayerStats(
                            profileName);

                    if (!stats.HasRecords)
                    {
                        continue;
                    }

                    hasAnyRecords =
                        true;

                    totalMatches +=
                        stats.TotalMatches;

                    totalSelfWins +=
                        stats.TotalLosses;

                    totalOpponentWins +=
                        stats.TotalWins;

                    last30SelfWins +=
                        stats.Last30Losses;

                    last30OpponentWins +=
                        stats.Last30Wins;

                    last100SelfWins +=
                        stats.Last100Losses;

                    last100OpponentWins +=
                        stats.Last100Wins;

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

                    if (stats.LastWinDate.HasValue)
                    {
                        if (!hasLastWinDate ||
                            stats.LastWinDate.Value >
                            lastWinDate)
                        {
                            lastWinDate =
                                stats.LastWinDate.Value;

                            hasLastWinDate =
                                true;
                        }
                    }

                    if (stats.LastLossDate.HasValue)
                    {
                        if (!hasLastLossDate ||
                            stats.LastLossDate.Value >
                            lastLossDate)
                        {
                            lastLossDate =
                                stats.LastLossDate.Value;

                            hasLastLossDate =
                                true;
                        }
                    }

                    Dictionary<int, TskCharacterStats>
                        characterStats =
                            database.GetP2CharacterStats(
                                profileName);

                    foreach (var item in
                        characterStats)
                    {
                        int characterId =
                            item.Key;

                        TskCharacterStats source =
                            item.Value;

                        if (!combinedCharacterStats.ContainsKey(
                            characterId))
                        {
                            combinedCharacterStats[
                                characterId] =
                                new TskCharacterStats();
                        }

                        TskCharacterStats destination =
                            combinedCharacterStats[
                                characterId];

                        destination.Matches +=
                            source.Matches;

                        destination.Wins +=
                            source.Wins;

                        destination.Losses +=
                            source.Losses;
                    }
                }

                /*
                 * --------------------------------
                 * 記録なし
                 * --------------------------------
                 */
                if (!hasAnyRecords)
                {
                    txtResult.Text =
                        CreateProfileHeader(
                            profileNames) +
                        "\r\n" +
                        "対戦記録がありません。";

                    return;
                }

                /*
                 * --------------------------------
                 * 本文
                 * --------------------------------
                 */
                string text =
                    CreateProfileHeader(
                        profileNames);

                text +=
                    "\r\n";

                /*
                 * --------------------------------
                 * 基本情報
                 * --------------------------------
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
                    "\r\n\r\n";

                /*
                 * --------------------------------
                 * 最近の戦績
                 * --------------------------------
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
                 * --------------------------------
                 * キャラクター使用状況
                 * --------------------------------
                 */
                text +=
                    "【キャラクター使用状況】\r\n";

                var orderedCharacters =
                    combinedCharacterStats
                        .OrderByDescending(
                            x => x.Value.Matches)
                        .ThenBy(
                            x => x.Key);

                foreach (var item in
                    orderedCharacters)
                {
                    int characterId =
                        item.Key;

                    TskCharacterStats charStats =
                        item.Value;

                    string characterName =
                        GetCharacterName(
                            characterId);

                    text +=
                        FormatCharacterLine(
                            characterName,
                            charStats) +
                        "\r\n";
                }

                /*
                 * --------------------------------
                 * 対戦日時
                 * --------------------------------
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
                 * --------------------------------
                 * メインキャラ
                 * --------------------------------
                 */
                text +=
                    "\r\n";

                text +=
                    "【メインキャラ】\r\n";

                if (combinedCharacterStats.Count > 0)
                {
                    /*
                     * 対戦回数最多。
                     */
                    var mainCharacter =
                        combinedCharacterStats
                            .OrderByDescending(
                                x => x.Value.Matches)
                            .ThenBy(
                                x => x.Key)
                            .First();

                    int mainId =
                        mainCharacter.Key;

                    TskCharacterStats mainStats =
                        mainCharacter.Value;

                    string mainName =
                        GetCharacterName(
                            mainId);

                    text +=
                        "対戦回数 : " +
                        mainName +
                        " (" +
                        mainStats.Matches +
                        "戦)\r\n";

                    /*
                     * P2視点の相手勝率。
                     */
                    var opponentCharacter =
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

                                    Rate =
                                        x.Value.Wins *
                                        100.0 /
                                        x.Value.Matches
                                })
                            .OrderByDescending(
                                x => x.Rate)
                            .ThenByDescending(
                                x => x.Stats.Matches)
                            .ThenBy(
                                x => x.CharacterId)
                            .First();

                    string opponentCharacterName =
                        GetCharacterName(
                            opponentCharacter.CharacterId);

                    text +=
                        "相手勝率 : " +
                        opponentCharacterName +
                        " (" +
                        opponentCharacter.Rate.ToString(
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
         * --------------------------------
         * プロファイル名ヘッダー
         * --------------------------------
         */
        private string CreateProfileHeader(
            List<string> profileNames)
        {
            string text =
                "【プロファイル詳細】\r\n\r\n";

            if (profileNames.Count == 0)
            {
                return text;
            }

            text +=
                "プロファイル : " +
                profileNames[0] +
                "\r\n";

            for (int i = 1;
                 i < profileNames.Count;
                 i++)
            {
                text +=
                    new string(
                        ' ',
                        15) +
                    profileNames[i] +
                    "\r\n";
            }

            return text;
        }

        /*
         * --------------------------------
         * 戦績行
         * --------------------------------
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
                total.ToString()
                    .PadLeft(6) +
                "戦 " +
                wins.ToString()
                    .PadLeft(6) +
                "勝 " +
                losses.ToString()
                    .PadLeft(6) +
                "敗 (" +
                FormatRate(
                    rate) +
                ")";
        }

        /*
         * --------------------------------
         * キャラクター行
         * --------------------------------
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

            return
                name +
                "：" +
                " " +
                stats.Matches
                    .ToString()
                    .PadLeft(6) +
                "戦 " +
                stats.Losses
                    .ToString()
                    .PadLeft(6) +
                "勝 " +
                stats.Wins
                    .ToString()
                    .PadLeft(6) +
                "敗";
        }

        /*
         * --------------------------------
         * P2視点の勝率
         * --------------------------------
         */
        private double CalculateP2WinRate(
            TskCharacterStats stats)
        {
            if (stats.Matches <= 0)
            {
                return 0.0;
            }

            return
                stats.Wins *
                100.0 /
                stats.Matches;
        }

        /*
         * --------------------------------
         * 勝率計算
         * --------------------------------
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
         * --------------------------------
         * 勝率表示
         * --------------------------------
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
         * --------------------------------
         * 日時
         * --------------------------------
         */
        private string FormatDate(
            DateTime dateTime)
        {
            return
                dateTime.ToString(
                    "yyyy/MM/dd HH:mm:ss");
        }

        /*
         * --------------------------------
         * キャラクター名
         * --------------------------------
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
    }
}