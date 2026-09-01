using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace th123OpponentInfoViewer
{
    /*
     * ============================================================
     * リバサルミネスカウンター
     * ============================================================
     *
     * ・指定キーを押すとカウントアップ
     * ・カウンターウィンドウが非アクティブでも有効
     * ・天則がフルスクリーンでも有効
     * ・天則がアクティブかどうかは確認しない
     * ・天則プロセスが存在している場合のみ有効
     * ・SceneID 8～15 のときだけ有効
     *
     * キー入力：
     *
     *   GetAsyncKeyState()
     *
     * を使用してWindows全体のキー状態を監視する。
     *
     * RegisterHotKey() は使用しない。
     *
     * これにより、
     *
     *   ・カウンターを選択していない
     *   ・天則がフルスクリーン
     *   ・天則がアクティブ
     *   ・カウンターが非アクティブ
     *
     * の状態でも指定キーを検出できる。
     *
     * カウント履歴：
     *
     *   2026/9/1_00:00:00_対戦相手
     *
     * の形式で表示・保存する。
     *
     * RLcounter.txt はEXEと同じフォルダ。
     *
     * .NET Framework 4.7.2対応。
     */
    public class ReversalLuminousStrikeCounter : Form
    {
        /*
         * --------------------------------------------------------
         * Win32
         * --------------------------------------------------------
         */

        /*
         * 指定したキーが現在押されているかを取得する。
         *
         * 戻り値の最上位ビットが1なら、
         * 現在キーが押されている。
         */
        [DllImport(
            "user32.dll")]
        private static extern short GetAsyncKeyState(
            int vKey);

        /*
         * --------------------------------------------------------
         * UI
         * --------------------------------------------------------
         */

        private Label lblCount;

        private Label lblKey;

        private TextBox txtKey;

        private Button btnSetKey;

        private ListBox lstHistory;

        private Label lblStatus;

        private Timer displayRefreshTimer;

        private string currentPlayer1Profile = "";
        private string currentPlayer2Profile = "";

        /*
         * --------------------------------------------------------
         * 状態
         * --------------------------------------------------------
         */

        private int count =
            0;

        private Keys assignedKey =
            Keys.F8;

        /*
         * 指定キーの前回状態。
         *
         * false = 押されていない
         * true  = 押されている
         *
         * これを使って、
         *
         * 「押した瞬間」
         *
         * だけを検出する。
         */
        private bool keyWasDown =
            false;

        /*
         * キー監視Timer。
         *
         * GetAsyncKeyState()を一定間隔で確認する。
         */
        private Timer keyPollingTimer;

        /*
         * 現在のゲーム情報。
         *
         * Form1からtimer1_Tickごとに更新する。
         */
        private uint currentSceneId =
            0;

        private string currentOpponentProfile =
            "";

        /*
         * カウント履歴。
         *
         * ウィンドウを開いている間、
         * すべて保持する。
         */
        private readonly List<string> history =
            new List<string>();

        /*
         * --------------------------------------------------------
         * ファイル
         * --------------------------------------------------------
         */

        private string CounterFilePath
        {
            get
            {
                return Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "RLcounter.txt");
            }
        }

        /*
         * --------------------------------------------------------
         * コンストラクタ
         * --------------------------------------------------------
         */

        public ReversalLuminousStrikeCounter()
        {
            InitializeForm();

            /*
             * ----------------------------------------------------
             * キー監視Timer
             * ----------------------------------------------------
             *
             * 10msごとにGetAsyncKeyState()を確認する。
             *
             * これはフォームのアクティブ状態とは
             * 関係なく動作する。
             */
            keyPollingTimer =
                new Timer();

            keyPollingTimer.Interval =
                10;

            keyPollingTimer.Tick +=
                KeyPollingTimer_Tick;

            keyPollingTimer.Start();
        }

        /*
         * ============================================================
         * フォーム初期化
         * ============================================================
         */

        private void InitializeForm()
        {
            Text =
                "リバサルミネスカウンター";

            StartPosition =
                FormStartPosition.CenterScreen;

            Width =
                500;

            Height =
                600;

            MinimumSize =
                new Size(
                    400,
                    450);

            Font =
                new Font(
                    "MS Gothic",
                    9.0f);

            /*
             * --------------------------------------------------------
             * カウント
             * --------------------------------------------------------
             */

            lblCount =
                new Label();

            lblCount.Dock =
                DockStyle.Top;

            lblCount.Height =
                150;

            lblCount.Text =
                "0";

            lblCount.TextAlign =
                ContentAlignment.MiddleCenter;

            lblCount.Font =
                new Font(
                    "MS Gothic",
                    72.0f,
                    FontStyle.Bold);

            /*
             * --------------------------------------------------------
             * キー設定Panel
             * --------------------------------------------------------
             */

            Panel keyPanel =
                new Panel();

            keyPanel.Dock =
                DockStyle.Top;

            keyPanel.Height =
                65;

            /*
             * --------------------------------------------------------
             * キー設定ラベル
             * --------------------------------------------------------
             */

            lblKey =
                new Label();

            lblKey.Text =
                "カウントキー";

            lblKey.Location =
                new Point(
                    10,
                    8);

            lblKey.AutoSize =
                true;

            keyPanel.Controls.Add(
                lblKey);

            /*
             * --------------------------------------------------------
             * キー表示TextBox
             * --------------------------------------------------------
             */

            txtKey =
                new TextBox();

            txtKey.Location =
                new Point(
                    10,
                    30);

            txtKey.Width =
                120;

            txtKey.ReadOnly =
                true;

            txtKey.Text =
                assignedKey.ToString();

            txtKey.KeyDown +=
                TxtKey_KeyDown;

            keyPanel.Controls.Add(
                txtKey);

            /*
             * --------------------------------------------------------
             * キー設定ボタン
             * --------------------------------------------------------
             */

            btnSetKey =
                new Button();

            btnSetKey.Text =
                "キー設定";

            btnSetKey.Location =
                new Point(
                    140,
                    29);

            btnSetKey.Width =
                90;

            btnSetKey.Click +=
                BtnSetKey_Click;

            keyPanel.Controls.Add(
                btnSetKey);

            /*
             * --------------------------------------------------------
             * 状態
             * --------------------------------------------------------
             */

            lblStatus =
                new Label();

            lblStatus.Dock =
                DockStyle.Top;

            lblStatus.Height =
                35;

            lblStatus.Padding =
                new Padding(
                    10,
                    0,
                    10,
                    0);

            lblStatus.TextAlign =
                ContentAlignment.MiddleLeft;

            lblStatus.Text =
                "SceneIDを待機中...";

            /*
             * --------------------------------------------------------
             * 履歴
             * --------------------------------------------------------
             */

            lstHistory =
                new ListBox();

            lstHistory.Dock =
                DockStyle.Fill;

            lstHistory.HorizontalScrollbar =
                true;

            /*
             * --------------------------------------------------------
             * コントロール配置
             * --------------------------------------------------------
             *
             * Fillを先に追加。
             *
             * その後Topを追加することで、
             * 履歴欄を残りの領域いっぱいにする。
             * --------------------------------------------------------
             */

            Controls.Add(
                lstHistory);

            Controls.Add(
                lblStatus);

            Controls.Add(
                keyPanel);

            Controls.Add(
                lblCount);

            displayRefreshTimer =
                new Timer();

            displayRefreshTimer.Interval =
                1000;

            displayRefreshTimer.Tick +=
                delegate
                {
                    if (!IsDisposed &&
                        lblCount != null &&
                        !lblCount.IsDisposed)
                    {
                        lblCount.Refresh();
                    }
                };

            displayRefreshTimer.Start();
        }

        /*
         * ============================================================
         * キー設定ボタン
         * ============================================================
         */

        private void BtnSetKey_Click(
            object sender,
            EventArgs e)
        {
            txtKey.Focus();

            txtKey.SelectAll();

            lblStatus.Text =
                "割り当てたいキーを押してください。";
        }

        /*
         * ============================================================
         * キー入力
         * ============================================================
         *
         * キー設定時だけはTextBoxのKeyDownを使用する。
         *
         * 実際のカウント監視は
         * GetAsyncKeyState()で行う。
         * ============================================================
         */

        private void TxtKey_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            /*
             * Alt / Ctrl / Shift単独などは
             * 今回の単一キー設定では使用しない。
             */
            if (e.KeyCode == Keys.None ||
                e.KeyCode == Keys.ControlKey ||
                e.KeyCode == Keys.ShiftKey ||
                e.KeyCode == Keys.Menu)
            {
                return;
            }

            assignedKey =
                e.KeyCode;

            txtKey.Text =
                assignedKey.ToString();

            /*
             * キーを変更した直後に、
             * 新しいキーがすでに押されている状態を
             * 「新しい押下」と誤認しないようにする。
             */
            keyWasDown =
                IsAssignedKeyDown();

            lblStatus.Text =
                "カウントキー : " +
                assignedKey;

            e.SuppressKeyPress =
                true;

            e.Handled =
                true;
        }

        /*
         * ============================================================
         * GetAsyncKeyState キー監視
         * ============================================================
         */

        private void KeyPollingTimer_Tick(
            object sender,
            EventArgs e)
        {
            /*
             * フォームが終了している場合。
             */
            if (IsDisposed ||
                Disposing)
            {
                return;
            }

            /*
             * 現在のキー状態。
             */
            bool keyDown =
                IsAssignedKeyDown();

            /*
             * ----------------------------------------------------
             * 「押した瞬間」だけ処理
             * ----------------------------------------------------
             *
             * keyDown == true
             * keyWasDown == false
             *
             * のときだけ新しいキー入力と判定する。
             */
            if (keyDown &&
                !keyWasDown)
            {
                HandleKeyPressed();
            }

            /*
             * 現在状態を保存。
             */
            keyWasDown =
                keyDown;
        }

        /*
         * ============================================================
         * 指定キーが押されているか
         * ============================================================
         */

        private bool IsAssignedKeyDown()
        {
            try
            {
                short state =
                    GetAsyncKeyState(
                        (int)assignedKey);

                /*
                 * 最上位ビットが1なら
                 * 現在キーが押されている。
                 */
                return
                    (state & 0x8000) != 0;
            }
            catch
            {
                return false;
            }
        }

        /*
         * ============================================================
         * キー押下処理
         * ============================================================
         */

        private void HandleKeyPressed()
        {
            /*
             * ----------------------------------------------------
             * 天則が起動しているか
             * ----------------------------------------------------
             *
             * アクティブかどうかは確認しない。
             *
             * フルスクリーンでも、
             * ウィンドウモードでも、
             * カウンターが非アクティブでもOK。
             */
            if (!IsTensokuRunning())
            {
                return;
            }

            /*
             * ----------------------------------------------------
             * SceneID確認
             * ----------------------------------------------------
             *
             * 8～15を許可。
             *
             * 8  キャラクター選択等
             * 9  キャラクター選択等
             * 10 ローディング
             * 11 ローディング
             * 12 観戦
             * 13 対戦
             * 14 対戦
             * 15 観戦
             */
            if (!IsCounterEnabledScene(
                currentSceneId))
            {
                return;
            }

            /*
             * 条件をすべて満たしたので
             * カウント。
             */
            IncrementCounter();
        }

        /*
         * ============================================================
         * SceneID判定
         * ============================================================
         */

        private bool IsCounterEnabledScene(
            uint sceneId)
        {
            return
                sceneId == 8 ||
                sceneId == 9 ||
                sceneId == 10 ||
                sceneId == 11 ||
                sceneId == 12 ||
                sceneId == 13 ||
                sceneId == 14 ||
                sceneId == 15;
        }

        /*
         * ============================================================
         * 天則が起動しているか
         * ============================================================
         *
         * ここではアクティブウィンドウを確認しない。
         *
         * 天則のプロセスが存在していればOK。
         *
         * これにより、
         *
         * ・ウィンドウモード
         * ・フルスクリーン
         * ・カウンターが非アクティブ
         *
         * のいずれでも、SceneIDが有効なら
         * カウントできる。
         * ============================================================
         */

        private bool IsTensokuRunning()
        {
            try
            {
                Process[] processes =
                    Process.GetProcessesByName(
                        "th123");

                try
                {
                    return
                        processes.Length > 0;
                }
                finally
                {
                    /*
                     * GetProcessesByName()で取得した
                     * Processオブジェクトを破棄。
                     */
                    foreach (Process process
                        in processes)
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /*
         * ============================================================
         * Form1からゲーム情報を渡す
         * ============================================================
         *
         * Form1のtimer1_Tickから呼び出す。
         * ============================================================
         */
        public void UpdateGameState(
            uint sceneId,
            string opponentProfile,
            string player1Profile,
            string player2Profile)
        {
            if (IsDisposed)
            {
                return;
            }

            currentSceneId = sceneId;

            currentOpponentProfile =
                opponentProfile ?? "";

            currentPlayer1Profile =
                player1Profile ?? "";

            currentPlayer2Profile =
                player2Profile ?? "";

            if (!IsCounterEnabledScene(sceneId))
            {
                lblStatus.Text =
                    "現在はカウント無効です。";
            }
            else
            {
                lblStatus.Text =
                    "カウント可能　SceneID : " +
                    sceneId;
            }
        }

        /*
         * ============================================================
         * カウントアップ
         * ============================================================
         */

        private void IncrementCounter()
        {
            DateTime now =
                DateTime.Now;

            string line;

            /*
             * 観戦Scene
             */
            if (currentSceneId == 12 ||
                currentSceneId == 15)
            {
                string player1 =
                    string.IsNullOrWhiteSpace(
                        currentPlayer1Profile)
                        ? "不明"
                        : currentPlayer1Profile;

                string player2 =
                    string.IsNullOrWhiteSpace(
                        currentPlayer2Profile)
                        ? "不明"
                        : currentPlayer2Profile;

                line =
                    now.ToString(
                        "yyyy/M/d_HH:mm:ss") +
                    "_watching_[" +
                    player1 +
                    "]_vs_[" +
                    player2 +
                    "]" ;
            }
            else
            {
                /*
                 * 通常対戦
                 */
                string opponent =
                    currentOpponentProfile;

                if (string.IsNullOrWhiteSpace(
                    opponent))
                {
                    opponent =
                        "不明";
                }

                line =
                    now.ToString(
                        "yyyy/M/d_HH:mm:ss") +
                    "_[" +
                    opponent +
                    "]";
            }

            /*
             * ファイルへ追記
             */
            try
            {
                File.AppendAllText(
                    CounterFilePath,
                    line +
                    Environment.NewLine,
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                lblStatus.Text =
                    "保存失敗 : " +
                    ex.Message;

                return;
            }

            /*
             * 保存成功後にカウント
             */
            count++;

            lblCount.Text =
                count.ToString();

            history.Add(
                line);

            lstHistory.Items.Add(
                line);

            if (lstHistory.Items.Count > 0)
            {
                lstHistory.SelectedIndex =
                    lstHistory.Items.Count - 1;
            }

            /*
             * 成功音
             */
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch
            {
            }

            lblStatus.Text =
                "カウントしました　" +
                line;
        }
        /*
         * ============================================================
         * フォーム表示時
         * ============================================================
         */

        protected override void OnShown(
            EventArgs e)
        {
            base.OnShown(e);

            txtKey.Text =
                assignedKey.ToString();

            txtKey.SelectAll();

            /*
             * 現在のキー状態を記録。
             *
             * フォーム表示時にすでにキーが押されていた場合、
             * それを新しい押下として誤認しない。
             */
            keyWasDown =
                IsAssignedKeyDown();

            /*
             * 念のためTimerを起動。
             */
            if (keyPollingTimer != null)
            {
                keyPollingTimer.Start();
            }
        }

        /*
         * ============================================================
         * フォーム終了
         * ============================================================
         */

        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            /*
             * キー監視Timer停止。
             */
            if (keyPollingTimer != null)
            {
                try
                {
                    keyPollingTimer.Stop();
                    keyPollingTimer.Dispose();
                }
                catch
                {
                }

                keyPollingTimer =
                    null;
            }

            base.OnFormClosed(e);
        }

        /*
         * ============================================================
         * Dispose
         * ============================================================
         */

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                if (keyPollingTimer != null)
                {
                    try
                    {
                        keyPollingTimer.Stop();
                        keyPollingTimer.Dispose();
                    }
                    catch
                    {
                    }

                    keyPollingTimer =
                        null;
                }
            }
            if (displayRefreshTimer != null)
            {
                displayRefreshTimer.Stop();
                displayRefreshTimer.Dispose();
            }

            base.Dispose(
                disposing);
        }
    }
}