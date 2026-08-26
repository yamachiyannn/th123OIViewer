using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace th123OpponentInfoViewer
{
    public partial class Form1 : Form
    {
        private readonly OpponentDetector detector =
            new OpponentDetector();

        private readonly ViewerConfig config;

        private readonly TskDatabaseReader tskDatabase;

        private uint previousSceneId = 0;

        /*
         * --------------------------------
         * 前回の対戦相手
         * --------------------------------
         *
         * 対戦中に最後に取得できた
         * 相手プロファイルを保持する。
         *
         * 対戦終了後、SceneIDが7以下に
         * なったときも表示する。
         */
        private string lastOpponentProfileName =
            "";

        /*
         * 起動直後のDB読み込み表示を
         * 1秒間保持するための時刻。
         */
        private DateTime dbLoadedDisplayUntil =
            DateTime.MinValue;

        /*
         * tsk.exe警告の点滅状態。
         */
        private bool tskWarningFlashState =
            false;

        private DateTime lastTskWarningFlash =
            DateTime.MinValue;

        /*
         * --------------------------------
         * 対戦セッション
         * --------------------------------
         */
        private int matchRecordCountBefore =
            -1;

        private bool checkingMatchRecord =
            false;

        private DateTime matchRecordCheckAt =
            DateTime.MinValue;

        /*
         * --------------------------------
         * 記録未追加警告
         * --------------------------------
         */
        private bool matchRecordWarning =
            false;

        private bool matchRecordWarningFlashState =
            false;

        private DateTime lastMatchRecordWarningFlash =
            DateTime.MinValue;

        private DateTime lastMatchRecordWarningSound =
            DateTime.MinValue;

        private bool matchRecordWarningSoundActive =
            false;

        /*
         * --------------------------------
         * ESC終了申告
         * --------------------------------
         */
        private bool escEndDeclared =
            false;

        /*
         * --------------------------------
         * 表示設定
         * --------------------------------
         *
         * 初期状態はINIから読み込む。
         *
         * DefaultCheckAsobby
         * DefaultShowIpPort
         */
        private bool asobbyCheckEnabled;

        private bool ipPortEnabled;

        /*
         * --------------------------------
         * 右クリックメニュー
         * --------------------------------
         *
         * メニュー項目をフィールドとして保持する。
         *
         * CheckOnClickには頼らず、
         * Clickイベントで設定値とCheckedを
         * 明示的に同期する。
         */
        private ToolStripMenuItem asobbyCheckMenu;

        private ToolStripMenuItem ipPortMenu;

        /*
         * --------------------------------
         * 通常時の色
         * --------------------------------
         */
        private Color normalFormBackColor;

        private Color normalOutputBackColor;

        private Color normalOutputForeColor;

        public Form1()
        {
            InitializeComponent();

            /*
             * --------------------------------
             * 設定読み込み
             * --------------------------------
             *
             * th123OpponentInfoViewer.ini
             * をEXEと同じフォルダから読み込む。
             */
            config =
                new ViewerConfig();

            /*
             * ウィンドウタイトル。
             */
            this.Text =
                "天則：対戦情報ビューワー";

            timer1.Interval =
                100;

            /*
             * 通常時の色を保存。
             */
            normalFormBackColor =
                BackColor;

            normalOutputBackColor =
                txtOutput.BackColor;

            normalOutputForeColor =
                txtOutput.ForeColor;

            /*
             * --------------------------------
             * フォントサイズ
             * --------------------------------
             *
             * 情報ビューワーの初期値を
             * INIから読み込む。
             */
            float viewerFontSize =
                config.ViewerFontSize;

            if (viewerFontSize <= 0)
            {
                viewerFontSize =
                    10.0f;
            }

            txtOutput.Font =
                new Font(
                    "MS Gothic",
                    viewerFontSize);

            /*
             * --------------------------------
             * IP表示・asobby確認の初期値
             * --------------------------------
             *
             * INIの値を使用。
             */
            asobbyCheckEnabled =
                config.DefaultCheckAsobby;

            ipPortEnabled =
                config.DefaultShowIpPort;

            /*
             * --------------------------------
             * 右クリックメニュー
             * --------------------------------
             */
            CreateOutputContextMenu();

            /*
             * IP入力欄の初期表示を反映。
             */
            ApplyIpPortSetting();

            /*
             * --------------------------------
             * IP入力欄
             * --------------------------------
             */
            txtIpInput.KeyDown +=
                TxtIpInput_KeyDown;

            /*
             * --------------------------------
             * ESC終了申告ボタン
             * --------------------------------
             */
            btnEscEnded.Visible =
                false;

            btnEscEnded.Enabled =
                false;

            btnEscEnded.Click +=
                BtnEscEnded_Click;

            /*
             * --------------------------------
             * DB読み込み
             * --------------------------------
             *
             * ViewerConfigが
             * EXEと同じフォルダにある
             * 指定DBファイルを返す。
             */
            tskDatabase =
                new TskDatabaseReader(
                    config.DatabasePath);

            /*
             * DBの存在確認。
             */
            if (!tskDatabase.DatabaseExists)
            {
                txtOutput.Text =
                    "【警告】DBファイルが見つかりません。\r\n\r\n" +
                    "探している場所：\r\n" +
                    tskDatabase.DatabasePath;

                return;
            }

            /*
             * --------------------------------
             * DB読み込み確認
             * --------------------------------
             *
             * この画面を1秒間保持。
             */
            try
            {
                int count =
                    tskDatabase.GetMatchCount();

                string databaseFileName =
                    Path.GetFileName(
                        tskDatabase.DatabasePath);

                txtOutput.Text =
                    "【待機中】\r\n\r\n" +
                    databaseFileName +
                    "を読み込みました。\r\n" +
                    "対戦記録 : " +
                    count +
                    "件\r\n\r\n" +
                    "キャラセレを待機中...";

                dbLoadedDisplayUntil =
                    DateTime.Now.AddSeconds(1);
            }
            catch (Exception ex)
            {
                txtOutput.Text =
                    "【警告】DB読み込みエラー\r\n\r\n" +
                    ex.Message;
            }
        }

        /*
         * --------------------------------
         * 右クリックメニュー作成
         * --------------------------------
         *
         * 機能：
         *
         * 8 px
         * 10 px
         * 14 px
         * 20 px
         *
         * asobby起動確認
         * IP:Port
         *
         * プロファイル検索
         */
        private void CreateOutputContextMenu()
        {
            ContextMenuStrip menu =
                new ContextMenuStrip();

            /*
             * チェックマーク表示領域を
             * 明示的に有効化。
             */
            menu.ShowCheckMargin =
                true;

            /*
             * --------------------------------
             * フォントサイズ
             * --------------------------------
             */
            ToolStripMenuItem fontSize8 =
                new ToolStripMenuItem(
                    "8 px");

            ToolStripMenuItem fontSize10 =
                new ToolStripMenuItem(
                    "10 px");

            ToolStripMenuItem fontSize14 =
                new ToolStripMenuItem(
                    "14 px");

            ToolStripMenuItem fontSize20 =
                new ToolStripMenuItem(
                    "20 px");

            fontSize8.Click +=
                delegate
                {
                    SetOutputFontSize(8.0f);
                };

            fontSize10.Click +=
                delegate
                {
                    SetOutputFontSize(10.0f);
                };

            fontSize14.Click +=
                delegate
                {
                    SetOutputFontSize(14.0f);
                };

            fontSize20.Click +=
                delegate
                {
                    SetOutputFontSize(20.0f);
                };

            menu.Items.Add(
                fontSize8);

            menu.Items.Add(
                fontSize10);

            menu.Items.Add(
                fontSize14);

            menu.Items.Add(
                fontSize20);

            menu.Items.Add(
                new ToolStripSeparator());

            /*
             * --------------------------------
             * asobby起動確認
             * --------------------------------
             */
            asobbyCheckMenu =
                new ToolStripMenuItem(
                    "asobby起動確認");

            /*
             * CheckOnClickは使用しない。
             *
             * Clickイベントで
             * asobbyCheckEnabledとCheckedを
             * 明示的に同期する。
             */
            asobbyCheckMenu.CheckOnClick =
                false;

            /*
             * INIの初期値をそのまま
             * メニューのチェック状態へ反映。
             */
            asobbyCheckMenu.Checked =
                asobbyCheckEnabled;

            asobbyCheckMenu.Click +=
                delegate
                {
                    asobbyCheckEnabled =
                        !asobbyCheckEnabled;

                    asobbyCheckMenu.Checked =
                        asobbyCheckEnabled;
                };

            menu.Items.Add(
                asobbyCheckMenu);

            /*
             * --------------------------------
             * IP:Port
             * --------------------------------
             */
            ipPortMenu =
                new ToolStripMenuItem(
                    "IP:Port");

            /*
             * CheckOnClickは使用しない。
             */
            ipPortMenu.CheckOnClick =
                false;

            /*
             * INIの初期値をそのまま
             * メニューのチェック状態へ反映。
             */
            ipPortMenu.Checked =
                ipPortEnabled;

            ipPortMenu.Click +=
                delegate
                {
                    ipPortEnabled =
                        !ipPortEnabled;

                    ipPortMenu.Checked =
                        ipPortEnabled;

                    ApplyIpPortSetting();
                };

            menu.Items.Add(
                ipPortMenu);

            menu.Items.Add(
                new ToolStripSeparator());

            /*
             * --------------------------------
             * プロファイル検索
             * --------------------------------
             */
            ToolStripMenuItem searchProfile =
                new ToolStripMenuItem(
                    "プロファイル検索");

            searchProfile.Click +=
                delegate
                {
                    OpenProfileSearch();
                };

            menu.Items.Add(
                searchProfile);

            /*
             * txtOutputへ設定。
             */
            txtOutput.ContextMenuStrip =
                menu;
        }

        /*
         * --------------------------------
         * IP:Port表示設定
         * --------------------------------
         *
         * ON：
         *   表示
         *   使用可能
         *
         * OFF：
         *   非表示
         *   操作不可
         */
        private void ApplyIpPortSetting()
        {
            if (txtIpInput == null)
            {
                return;
            }

            txtIpInput.Visible =
                ipPortEnabled;

            txtIpInput.Enabled =
                ipPortEnabled;
        }

        /*
         * txtOutputのフォントサイズを変更。
         */
        private void SetOutputFontSize(
            float size)
        {
            txtOutput.Font =
                new Font(
                    "MS Gothic",
                    size);
        }

        /*
         * --------------------------------
         * IP入力欄
         * --------------------------------
         *
         * Enterで入力されたIPを
         * クリップボードへコピー。
         */
        private void TxtIpInput_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            string ip =
                txtIpInput.Text.Trim();

            if (ip.Length > 0)
            {
                try
                {
                    Clipboard.SetText(ip);
                }
                catch
                {
                }
            }

            e.SuppressKeyPress =
                true;

            e.Handled =
                true;
        }

        /*
         * --------------------------------
         * ESC終了申告ボタン
         * --------------------------------
         *
         * ここではDBに一切書き込まない。
         */
        private void BtnEscEnded_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                int currentCount =
                    tskDatabase.GetMatchCount();

                escEndDeclared =
                    true;

                matchRecordWarning =
                    false;

                matchRecordWarningFlashState =
                    false;

                matchRecordWarningSoundActive =
                    false;

                lastMatchRecordWarningFlash =
                    DateTime.MinValue;

                lastMatchRecordWarningSound =
                    DateTime.MinValue;

                checkingMatchRecord =
                    false;

                matchRecordCountBefore =
                    currentCount;

                RestoreNormalColors();

                HideEscButton();

                txtOutput.Text =
                    AppendAsobbyWarning(
                        CreateWaitingText());
            }
            catch (Exception ex)
            {
                escEndDeclared =
                    true;

                matchRecordWarning =
                    false;

                matchRecordWarningFlashState =
                    false;

                matchRecordWarningSoundActive =
                    false;

                checkingMatchRecord =
                    false;

                matchRecordCountBefore =
                    -1;

                RestoreNormalColors();

                HideEscButton();

                txtOutput.Text =
                    AppendAsobbyWarning(
                        CreateWaitingText() +
                        "\r\n\r\n" +
                        "ESC終了として処理しました。\r\n\r\n" +
                        "DBの再読み込みに失敗しました。\r\n" +
                        ex.Message);
            }
        }

        /*
         * --------------------------------
         * プロファイル検索画面
         * --------------------------------
         */
        private void OpenProfileSearch()
        {
            try
            {
                using (ProfileSearchForm form =
                    new ProfileSearchForm(
                        tskDatabase))
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "プロファイル検索を開けませんでした。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * ESC終了申告ボタンを表示。
         */
        private void ShowEscButton()
        {
            btnEscEnded.Visible =
                true;

            btnEscEnded.Enabled =
                true;
        }

        /*
         * ESC終了申告ボタンを隠す。
         */
        private void HideEscButton()
        {
            btnEscEnded.Visible =
                false;

            btnEscEnded.Enabled =
                false;
        }

        private void timer1_Tick(
            object sender,
            EventArgs e)
        {
            /*
             * -------------------------
             * 起動直後
             * -------------------------
             */
            if (DateTime.Now <
                dbLoadedDisplayUntil)
            {
                return;
            }

            /*
             * -------------------------
             * tsk.exe未起動
             * -------------------------
             */
            if (!IsTskRunning())
            {
                matchRecordWarning =
                    false;

                checkingMatchRecord =
                    false;

                matchRecordWarningSoundActive =
                    false;

                HideEscButton();

                ShowTskNotRunningWarning();

                previousSceneId =
                    0;

                return;
            }

            /*
             * tsk.exeが起動している場合。
             */
            if (!matchRecordWarning)
            {
                RestoreNormalColors();
            }

            /*
             * -------------------------
             * DB記録追加確認
             * -------------------------
             */
            if (checkingMatchRecord)
            {
                if (DateTime.Now >=
                    matchRecordCheckAt)
                {
                    checkingMatchRecord =
                        false;

                    try
                    {
                        int currentCount =
                            tskDatabase.GetMatchCount();

                        if (matchRecordCountBefore >= 0 &&
                            currentCount <=
                            matchRecordCountBefore)
                        {
                            matchRecordWarning =
                                true;

                            matchRecordWarningFlashState =
                                false;

                            lastMatchRecordWarningFlash =
                                DateTime.MinValue;

                            lastMatchRecordWarningSound =
                                DateTime.MinValue;

                            matchRecordWarningSoundActive =
                                true;

                            ShowEscButton();
                        }
                    }
                    catch
                    {
                    }
                }
            }

            /*
             * -------------------------
             * 現在のゲーム情報取得
             * -------------------------
             */
            OpponentInfo info =
                detector.GetOpponent();

            /*
             * -------------------------
             * 前回の相手を記録
             * -------------------------
             *
             * 通常対戦中に取得できた
             * 相手プロファイルを保持する。
             *
             * SceneID：
             * 8～11 = キャラセレ・ロード
             * 13～14 = 対戦中
             *
             * 観戦（12・15）は対象外。
             */
            if ((info.SceneId >= 8 &&
                 info.SceneId <= 11) ||
                (info.SceneId >= 13 &&
                 info.SceneId <= 14))
            {
                if (!string.IsNullOrWhiteSpace(
                    info.ProfileName))
                {
                    lastOpponentProfileName =
                        info.ProfileName;
                }
            }

            /*
             * SceneID 8以上へ移行したら
             * IP入力欄を消す。
             */
            if (previousSceneId < 8 &&
                info.SceneId >= 8)
            {
                txtIpInput.Clear();
            }

            /*
             * -------------------------
             * 新しい対戦セッション開始
             * -------------------------
             */
            if (previousSceneId <= 7 &&
                (info.SceneId == 8 ||
                 info.SceneId == 9))
            {
                try
                {
                    matchRecordCountBefore =
                        tskDatabase.GetMatchCount();
                }
                catch
                {
                    matchRecordCountBefore =
                        -1;
                }

                matchRecordWarning =
                    false;

                matchRecordWarningFlashState =
                    false;

                matchRecordWarningSoundActive =
                    false;

                checkingMatchRecord =
                    false;

                lastMatchRecordWarningFlash =
                    DateTime.MinValue;

                lastMatchRecordWarningSound =
                    DateTime.MinValue;

                escEndDeclared =
                    false;

                RestoreNormalColors();

                HideEscButton();
            }

            /*
             * -------------------------
             * 対戦終了 → 次の対戦
             * -------------------------
             */
            if ((previousSceneId == 13 ||
                 previousSceneId == 14) &&
                (info.SceneId == 8 ||
                 info.SceneId == 9))
            {
                if (!escEndDeclared)
                {
                    if (matchRecordCountBefore >= 0)
                    {
                        checkingMatchRecord =
                            true;

                        matchRecordCheckAt =
                            DateTime.Now.AddMilliseconds(500);
                    }
                }
                else
                {
                    escEndDeclared =
                        false;
                }
            }

            /*
             * -------------------------
             * 記録未追加警告表示
             * -------------------------
             */
            if (matchRecordWarning)
            {
                if (info.SceneId == 10 ||
                    info.SceneId == 11)
                {
                    matchRecordWarningSoundActive =
                        false;

                    ShowMatchRecordWarningYellowOnly();
                }
                else
                {
                    ShowMatchRecordWarning();
                }
            }

            previousSceneId =
                info.SceneId;

            /*
             * -------------------------
             * 観戦
             * -------------------------
             */
            if (info.SceneId == 12 ||
                info.SceneId == 15)
            {
                ShowWatchingInfo(
                    info);

                return;
            }

            /*
             * -------------------------
             * 対戦
             * -------------------------
             */
            if ((info.SceneId >= 8 &&
                 info.SceneId <= 11) ||
                (info.SceneId >= 13 &&
                 info.SceneId <= 14))
            {
                ShowOpponentInfo(
                    info);

                return;
            }

            /*
             * -------------------------
             * 待機中
             * -------------------------
             */
            txtOutput.Text =
                AppendAsobbyWarning(
                    CreateWaitingText());
        }

        /*
         * --------------------------------
         * 待機中表示
         * --------------------------------
         *
         * 前回の対戦相手が存在する場合は
         * そのプロファイルを表示する。
         */
        private string CreateWaitingText()
        {
            string text =
                "【待機中】";

            if (!string.IsNullOrWhiteSpace(
                lastOpponentProfileName))
            {
                text +=
                    "\r\n\r\n" +
                    "前回の相手 : " +
                    lastOpponentProfileName;
            }

            return text;
        }

        /*
         * tsk.exeが起動しているか。
         */
        private bool IsTskRunning()
        {
            try
            {
                Process[] processes =
                    Process.GetProcessesByName(
                        "tsk");

                return
                    processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /*
         * asobby.exeが起動しているか。
         */
        private bool IsAsobbyRunning()
        {
            try
            {
                Process[] processes =
                    Process.GetProcessesByName(
                        "asobby");

                return
                    processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /*
         * tsk.exe未起動警告。
         */
        private void ShowTskNotRunningWarning()
        {
            DateTime now =
                DateTime.Now;

            if (lastTskWarningFlash ==
                DateTime.MinValue)
            {
                tskWarningFlashState =
                    false;

                lastTskWarningFlash =
                    now;
            }

            if ((now -
                 lastTskWarningFlash).TotalMilliseconds >=
                500)
            {
                tskWarningFlashState =
                    !tskWarningFlashState;

                lastTskWarningFlash =
                    now;
            }

            if (tskWarningFlashState)
            {
                BackColor =
                    Color.Yellow;

                txtOutput.BackColor =
                    Color.Yellow;

                txtOutput.ForeColor =
                    Color.Black;
            }
            else
            {
                BackColor =
                    Color.Red;

                txtOutput.BackColor =
                    Color.Red;

                txtOutput.ForeColor =
                    Color.White;
            }

            txtOutput.Text =
                "【警告】\r\n\r\n" +
                "天則観（tsk.exe）が検知できません。\r\n\r\n" +
                "tsk.exeを起動してください。";
        }

        /*
         * --------------------------------
         * 記録未追加警告
         * --------------------------------
         */
        private void ShowMatchRecordWarning()
        {
            DateTime now =
                DateTime.Now;

            if (lastMatchRecordWarningFlash ==
                DateTime.MinValue)
            {
                matchRecordWarningFlashState =
                    false;

                lastMatchRecordWarningFlash =
                    now;
            }

            if ((now -
                 lastMatchRecordWarningFlash).TotalMilliseconds >=
                500)
            {
                matchRecordWarningFlashState =
                    !matchRecordWarningFlashState;

                lastMatchRecordWarningFlash =
                    now;
            }

            /*
             * 1秒ごとに警告音。
             */
            if (matchRecordWarningSoundActive)
            {
                if (lastMatchRecordWarningSound ==
                    DateTime.MinValue ||
                    (now -
                     lastMatchRecordWarningSound).TotalMilliseconds >=
                    1000)
                {
                    try
                    {
                        SystemSounds.Exclamation.Play();
                    }
                    catch
                    {
                    }

                    lastMatchRecordWarningSound =
                        now;
                }
            }

            if (matchRecordWarningFlashState)
            {
                BackColor =
                    Color.Yellow;

                txtOutput.BackColor =
                    Color.Yellow;

                txtOutput.ForeColor =
                    Color.Black;
            }
            else
            {
                BackColor =
                    Color.Red;

                txtOutput.BackColor =
                    Color.Red;

                txtOutput.ForeColor =
                    Color.White;
            }

            txtOutput.Text =
                "【警告】\r\n\r\n" +
                "対戦終了を検出しましたが、\r\n" +
                GetDatabaseFileName() +
                "の対戦記録が増えていません。\r\n\r\n" +
                "天則観（tsk.exe）が書き込めない\r\n" +
                "状態である可能性があります。\r\n" +
                "非想天則、天則観、ビューワーの\r\n" +
                "再起動を試みてください。";

            ShowEscButton();
        }

        /*
         * --------------------------------
         * 記録未追加警告
         * 対戦中
         * --------------------------------
         */
        private void ShowMatchRecordWarningYellowOnly()
        {
            BackColor =
                Color.Yellow;

            txtOutput.BackColor =
                Color.Yellow;

            txtOutput.ForeColor =
                Color.Black;

            txtOutput.Text =
                "【警告】\r\n\r\n" +
                "対戦終了を検出しましたが、\r\n" +
                GetDatabaseFileName() +
                "の対戦記録が増えていません。\r\n\r\n" +
                "天則観（tsk.exe）が書き込めない\r\n" +
                "状態である可能性があります。\r\n" +
                "非想天戦、天則観、ビューワーの\r\n" +
                "再起動を試みてください。";

            ShowEscButton();
        }

        /*
         * tsk.exeが起動したら通常色へ戻す。
         */
        private void RestoreNormalColors()
        {
            BackColor =
                normalFormBackColor;

            txtOutput.BackColor =
                normalOutputBackColor;

            txtOutput.ForeColor =
                normalOutputForeColor;

            lastTskWarningFlash =
                DateTime.MinValue;

            tskWarningFlashState =
                false;
        }

        /*
         * --------------------------------
         * DBファイル名取得
         * --------------------------------
         */
        private string GetDatabaseFileName()
        {
            try
            {
                return Path.GetFileName(
                    tskDatabase.DatabasePath);
            }
            catch
            {
                return "DB";
            }
        }

        /*
         * --------------------------------
         * asobby.exe警告
         * --------------------------------
         *
         * asobbyCheckEnabledがOFFなら
         * 一切確認しない。
         */
        private string AppendAsobbyWarning(
            string text)
        {
            if (!asobbyCheckEnabled)
            {
                return text;
            }

            if (!IsAsobbyRunning())
            {
                text +=
                    "\r\n\r\n" +
                    "[警告] asobby.exeが起動していません。";
            }

            return text;
        }

        /*
         * -------------------------
         * 通常対戦情報
         * -------------------------
         */
        private void ShowOpponentInfo(
            OpponentInfo info)
        {
            string profileName =
                info.ProfileName;

            if (string.IsNullOrWhiteSpace(
                profileName))
            {
                txtOutput.Text =
                    AppendAsobbyWarning(
                        "【対戦中】\r\n\r\n" +
                        "対戦相手を取得中...");

                return;
            }

            TskOpponentStats stats;

            try
            {
                stats =
                    tskDatabase.GetOpponentStats(
                        profileName);
            }
            catch (Exception ex)
            {
                txtOutput.Text =
                    AppendAsobbyWarning(
                        "【対戦中】\r\n\r\n" +
                        GetDatabaseFileName() +
                        "読み込みエラー\r\n\r\n" +
                        ex.Message);

                return;
            }

            string text =
                "【対戦中】\r\n\r\n";

            text +=
                "対戦相手 : " +
                profileName +
                "\r\n";

            if (!stats.HasRecords)
            {
                text +=
                    "\r\n" +
                    "対戦記録がありません";

                txtOutput.Text =
                    AppendAsobbyWarning(
                        text);

                return;
            }

            text +=
                "メインキャラ : " +
                GetCharacterName(
                    stats.MainCharacterId) +
                "\r\n";

            text +=
                "通算 : " +
                stats.TotalMatches +
                "戦\r\n";

            int selfWins =
                stats.TotalLosses;

            int opponentWins =
                stats.TotalWins;

            double selfWinRate =
                CalculateWinRate(
                    selfWins,
                    opponentWins);

            text +=
                "通算勝率 : " +
                FormatRate(
                    selfWinRate) +
                "\r\n\r\n";

            text +=
                FormatRecordLine(
                    "過去 30戦",
                    stats.Last30Losses,
                    stats.Last30Wins,
                    CalculateWinRate(
                        stats.Last30Losses,
                        stats.Last30Wins)) +
                "\r\n";

            text +=
                FormatRecordLine(
                    "過去100戦",
                    stats.Last100Losses,
                    stats.Last100Wins,
                    CalculateWinRate(
                        stats.Last100Losses,
                        stats.Last100Wins)) +
                "\r\n";

            text +=
                FormatRecordLine(
                    "過去1か月",
                    stats.LastMonthLosses,
                    stats.LastMonthWins,
                    CalculateWinRate(
                        stats.LastMonthLosses,
                        stats.LastMonthWins)) +
                "\r\n\r\n";

            if (stats.TotalLosses == 0)
            {
                text +=
                    "[ラウンド未取得]\r\n";
            }

            if (stats.FirstMatchDate !=
                DateTime.MinValue)
            {
                text +=
                    "初対戦　　　 : " +
                    FormatDate(
                        stats.FirstMatchDate) +
                    "\r\n";
            }

            if (stats.LastMatchDate !=
                DateTime.MinValue)
            {
                text +=
                    "前回対戦　　 : " +
                    FormatDate(
                        stats.LastMatchDate) +
                    "\r\n";
            }

            if (stats.LastWinDate.HasValue)
            {
                text +=
                    "最後に勝った : " +
                    FormatDate(
                        stats.LastWinDate.Value) +
                    "\r\n";
            }

            if (stats.LastLossDate.HasValue)
            {
                text +=
                    "最後に負けた : " +
                    FormatDate(
                        stats.LastLossDate.Value);
            }

            txtOutput.Text =
                AppendAsobbyWarning(
                    text);
        }

        /*
         * -------------------------
         * 観戦情報
         * -------------------------
         */
        private void ShowWatchingInfo(
            OpponentInfo info)
        {
            string player1 =
                info.Player1ProfileName;

            string player2 =
                info.Player2ProfileName;

            string text =
                "【観戦中】\r\n\r\n";

            if (string.IsNullOrWhiteSpace(
                player1) ||
                string.IsNullOrWhiteSpace(
                player2))
            {
                text +=
                    "1P / 2Pのプロファイルを取得中...";

                txtOutput.Text =
                    AppendAsobbyWarning(
                        text);

                return;
            }

            TskOpponentStats stats1;
            TskOpponentStats stats2;

            try
            {
                stats1 =
                    tskDatabase.GetPlayerStats(
                        player1);

                stats2 =
                    tskDatabase.GetPlayerStats(
                        player2);
            }
            catch (Exception ex)
            {
                txtOutput.Text =
                    AppendAsobbyWarning(
                        "【観戦中】\r\n\r\n" +
                        GetDatabaseFileName() +
                        "読み込みエラー\r\n\r\n" +
                        ex.Message);

                return;
            }

            text +=
                CreateWatchingPlayerText(
                    "1P",
                    player1,
                    stats1);

            text +=
                "\r\n\r\n";

            text +=
                CreateWatchingPlayerText(
                    "2P",
                    player2,
                    stats2);

            text +=
                "\r\n\r\n";

            text +=
                FormatWatchingRateLine(
                    "通算",
                    stats1,
                    stats2,
                    0) +
                "\r\n";

            text +=
                FormatWatchingRateLine(
                    "過去 30戦",
                    stats1,
                    stats2,
                    30) +
                "\r\n";

            text +=
                FormatWatchingRateLine(
                    "過去100戦",
                    stats1,
                    stats2,
                    100) +
                "\r\n";

            text +=
                FormatWatchingRateLine(
                    "過去1か月",
                    stats1,
                    stats2,
                    -1);

            txtOutput.Text =
                AppendAsobbyWarning(
                    text);
        }

        /*
         * 観戦時の1P/2P情報。
         */
        private string CreateWatchingPlayerText(
            string side,
            string profileName,
            TskOpponentStats stats)
        {
            string text =
                side +
                " : " +
                profileName +
                "\r\n";

            if (!stats.HasRecords)
            {
                text +=
                    "メインキャラ : ---\r\n" +
                    "通算 : 0戦";

                return text;
            }

            text +=
                "メインキャラ : " +
                GetCharacterName(
                    stats.MainCharacterId) +
                "\r\n";

            text +=
                "通算 : " +
                stats.TotalMatches +
                "戦";

            if (stats.TotalLosses == 0)
            {
                text +=
                    "\r\n[ラウンド未取得]";
            }

            return text;
        }

        /*
         * 観戦時の勝率表示。
         */
        private string FormatWatchingRateLine(
            string label,
            TskOpponentStats stats1,
            TskOpponentStats stats2,
            int period)
        {
            double rate1 =
                GetPlayerOneWinRate(
                    stats1,
                    period);

            double rate2 =
                GetPlayerOneWinRate(
                    stats2,
                    period);

            string rateText1 =
                FormatWatchingRate(
                    rate1);

            string rateText2 =
                FormatWatchingRate(
                    rate2);

            rateText1 =
                rateText1.PadLeft(6);

            rateText2 =
                rateText2.PadLeft(6);

            return
                rateText1 +
                " /" +
                rateText2 +
                " ： " +
                label;
        }

        private double GetPlayerOneWinRate(
            TskOpponentStats stats,
            int period)
        {
            if (!stats.HasRecords)
            {
                return -1;
            }

            int p1win;
            int p2win;

            if (period == 30)
            {
                p1win =
                    stats.Last30Losses;

                p2win =
                    stats.Last30Wins;
            }
            else if (period == 100)
            {
                p1win =
                    stats.Last100Losses;

                p2win =
                    stats.Last100Wins;
            }
            else if (period == -1)
            {
                p1win =
                    stats.LastMonthLosses;

                p2win =
                    stats.LastMonthWins;
            }
            else
            {
                p1win =
                    stats.TotalLosses;

                p2win =
                    stats.TotalWins;
            }

            return CalculateWinRate(
                p1win,
                p2win);
        }

        private double CalculateWinRate(
            int wins,
            int losses)
        {
            int total =
                wins +
                losses;

            if (total <= 0)
            {
                return -1;
            }

            return
                wins * 100.0 /
                total;
        }

        private string FormatWatchingRate(
            double rate)
        {
            if (rate < 0)
            {
                return "---.-%";
            }

            return
                rate.ToString(
                    "0.0") +
                "%";
        }

        private string FormatRate(
            double rate)
        {
            if (rate < 0)
            {
                return "---.-%";
            }

            return
                rate.ToString(
                    "0.0") +
                "%";
        }

        private string FormatRecordLine(
            string label,
            int wins,
            int losses,
            double rate)
        {
            return
                label +
                "： " +
                wins.ToString().PadLeft(3) +
                "勝 " +
                losses.ToString().PadLeft(3) +
                "敗  (" +
                FormatRate(
                    rate) +
                ")";
        }

        private string FormatDate(
            DateTime dateTime)
        {
            return dateTime.ToString(
                "yyyy/MM/dd HH:mm:ss");
        }

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
         * Designer側から呼ばれている場合に備えて
         * 既存メソッドを残す。
         */
        private void txtIpInput_TextChanged(
            object sender,
            EventArgs e)
        {
        }

        /*
         * Designer側から呼ばれている場合に備えて
         * 既存メソッドを残す。
         */
        private void btnEscEnded_Click_1(
            object sender,
            EventArgs e)
        {
        }
    }
}