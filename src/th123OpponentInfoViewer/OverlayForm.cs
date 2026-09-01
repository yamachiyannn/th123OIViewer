using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace th123OpponentInfoViewer
{
    /*
     * --------------------------------
     * 天則フルスクリーン用オーバーレイ
     * --------------------------------
     *
     * 天則は古いゲームなので、
     *
     *   Windows画面全体
     *        ↓
     *   天則の表示領域を縦基準で拡大
     *        ↓
     *   左右に黒帯
     *
     * という状態になる。
     *
     * そのため、単純に
     *
     *   Location = (0, 0)
     *
     * にはしない。
     *
     * 天則ウィンドウの位置・サイズから
     * 実際のゲーム表示領域を計算する。
     */
    public class OverlayForm : Form
    {
        private readonly Label lblText;

        private readonly Timer positionTimer;

        private uint currentSceneId;

        /*
         * 天則の基準解像度。
         *
         * 天則のゲーム画面は4:3。
         *
         * 実際のウィンドウサイズに合わせて
         * 縦方向基準で拡大する。
         */
        private const int GAME_WIDTH = 640;
        private const int GAME_HEIGHT = 480;

        private static readonly string[] GAME_PROCESS_NAMES =
        {
            "th123"
        };

        /*
         * オーバーレイ文字。
         */
        private readonly Font overlayFont =
            new Font(
                "MS Gothic",
                8.0f,
                FontStyle.Regular,
                GraphicsUnit.Point);

        /*
         * 背景は通常のColor。
         *
         * 半透明にはBackColorのARGBを使わず、
         * Form.Opacityを使用する。
         */
        private readonly Color overlayBackColor =
            Color.Black;

        private readonly Color overlayForeColor =
            Color.White;

        /*
         * --------------------------------
         * Win32
         * --------------------------------
         */

        private const int GWL_EXSTYLE = -20;

        private const int WS_EX_LAYERED =
            0x00080000;

        private const int WS_EX_TRANSPARENT =
            0x00000020;

        private const int WS_EX_TOOLWINDOW =
            0x00000080;

        private const int WS_EX_NOACTIVATE =
            0x08000000;

        private const int WM_NCHITTEST =
            0x0084;

        private const int HTTRANSPARENT =
            -1;

        private const int SWP_NOSIZE =
            0x0001;

        private const int SWP_NOMOVE =
            0x0002;

        private const int SWP_NOACTIVATE =
            0x0010;

        private const int SWP_SHOWWINDOW =
            0x0040;

        [DllImport(
            "user32.dll",
            EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(
            IntPtr hWnd,
            int nIndex);

        [DllImport(
            "user32.dll",
            EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(
            IntPtr hWnd,
            int nIndex,
            int dwNewLong);

        [DllImport(
            "user32.dll")]
        private static extern bool IsWindowVisible(
            IntPtr hWnd);

        [DllImport(
            "user32.dll")]
        private static extern bool IsIconic(
            IntPtr hWnd);

        [DllImport(
            "user32.dll")]
        private static extern bool GetWindowRect(
            IntPtr hWnd,
            out RECT rect);

        [DllImport(
            "user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [StructLayout(
            LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static readonly IntPtr HWND_TOPMOST =
            new IntPtr(-1);

        public OverlayForm()
        {
            /*
             * --------------------------------
             * Form基本設定
             * --------------------------------
             */

            FormBorderStyle =
                FormBorderStyle.None;

            StartPosition =
                FormStartPosition.Manual;

            ShowInTaskbar =
                false;

            ShowIcon =
                false;

            TopMost =
                true;

            ControlBox =
                false;

            MinimizeBox =
                false;

            MaximizeBox =
                false;

            Text =
                "天則：対戦情報ビューワー";

            BackColor =
                overlayBackColor;

            ForeColor =
                overlayForeColor;

            /*
             * 半透明はForm全体に適用。
             *
             * BackColorにARGBを入れない。
             */
            Opacity =
                0.60;

            Padding =
                new Padding(4);

            AutoSize =
                true;

            AutoSizeMode =
                AutoSizeMode.GrowAndShrink;

            /*
             * --------------------------------
             * Label
             * --------------------------------
             */

            lblText =
                new Label();

            lblText.AutoSize =
                true;

            lblText.Font =
                overlayFont;

            lblText.ForeColor =
                overlayForeColor;

            lblText.BackColor =
                Color.Transparent;

            lblText.Margin =
                new Padding(0);

            lblText.Padding =
                new Padding(0);

            lblText.Text =
                "";

            Controls.Add(
                lblText);

            /*
             * 初期位置。
             *
             * 実際にはSetGameAreaPosition()
             * で更新する。
             */
            Location =
                new Point(
                    0,
                    0);

            /*
             * --------------------------------
             * 天則位置追従Timer
             * --------------------------------
             *
             * フルスクリーン切替や解像度変更に
             * 対応するため定期的に位置を更新。
             */
            positionTimer =
                new Timer();

            positionTimer.Interval =
                250;

            positionTimer.Tick +=
                delegate
                {
                    if (Visible)
                    {
                        SetGameAreaPosition();
                    }
                };

            positionTimer.Start();

            Visible =
                false;
        }

        /*
         * --------------------------------
         * クリック透過
         * --------------------------------
         */
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp =
                    base.CreateParams;

                cp.ExStyle |=
                    WS_EX_LAYERED;

                cp.ExStyle |=
                    WS_EX_TRANSPARENT;

                cp.ExStyle |=
                    WS_EX_TOOLWINDOW;

                cp.ExStyle |=
                    WS_EX_NOACTIVATE;

                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }

        /*
         * --------------------------------
         * マウス操作をゲームへ通す
         * --------------------------------
         */
        protected override void WndProc(
            ref Message m)
        {
            if (m.Msg ==
                WM_NCHITTEST)
            {
                m.Result =
                    new IntPtr(
                        HTTRANSPARENT);

                return;
            }

            base.WndProc(
                ref m);
        }

        /*
         * --------------------------------
         * SceneID + テキスト更新
         * --------------------------------
         */
        public void SetText(
            string text,
            uint sceneId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action(
                        delegate
                        {
                            SetText(
                                text,
                                sceneId);
                        }));

                return;
            }

            currentSceneId =
                sceneId;

            if (!IsOverlayScene(
                sceneId))
            {
                HideOverlay();
                return;
            }

            /*
             * 通常表示。
             */
            lblText.Text =
                text ?? "";

            ResizeToContent();

            /*
             * ゲーム表示領域へ移動。
             */
            if (!SetGameAreaPosition())
            {
                /*
                 * 天則ウィンドウがまだ見つからない場合。
                 *
                 * とりあえず非表示。
                 *
                 * これで画面の変な場所に
                 * オーバーレイが出るのを防ぐ。
                 */
                HideOverlay();
                return;
            }

            if (!Visible)
            {
                Show();
            }

            /*
             * Activateしない。
             */
        }

        /*
         * --------------------------------
         * 非表示
         * --------------------------------
         */
        public void HideOverlay()
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action(
                        HideOverlay));

                return;
            }

            Hide();
        }

        /*
         * --------------------------------
         * Overlay対象Scene
         * --------------------------------
         */
        private bool IsOverlayScene(
            uint sceneId)
        {
            return
                sceneId == 8 ||
                sceneId == 9 ||
                sceneId == 10 ||
                sceneId == 11 ||
                sceneId == 12 ||
                sceneId == 15;
        }

        /*
         * --------------------------------
         * 天則プロセス検索
         * --------------------------------
         */
        private Process FindGameProcess()
        {
            foreach (string name
                in GAME_PROCESS_NAMES)
            {
                try
                {
                    Process[] processes =
                        Process.GetProcessesByName(
                            name);

                    foreach (Process process
                        in processes)
                    {
                        try
                        {
                            if (process.HasExited)
                            {
                                process.Dispose();
                                continue;
                            }

                            if (process.MainWindowHandle ==
                                IntPtr.Zero)
                            {
                                process.Dispose();
                                continue;
                            }

                            if (!IsWindowVisible(
                                process.MainWindowHandle))
                            {
                                process.Dispose();
                                continue;
                            }

                            if (IsIconic(
                                process.MainWindowHandle))
                            {
                                process.Dispose();
                                continue;
                            }

                            return process;
                        }
                        catch
                        {
                            process.Dispose();
                        }
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        /*
         * --------------------------------
         * ゲーム表示領域計算
         * --------------------------------
         *
         * 天則のウィンドウが
         *
         * 640x480
         *
         * だと仮定し、
         *
         * 実ウィンドウ内で
         * 4:3になる最大矩形を作る。
         *
         * これによって左右の黒帯を除外する。
         */
        private Rectangle GetGameDisplayRectangle(
            Rectangle windowRect)
        {
            if (windowRect.Width <= 0 ||
                windowRect.Height <= 0)
            {
                return Rectangle.Empty;
            }

            double windowAspect =
                (double)windowRect.Width /
                windowRect.Height;

            double gameAspect =
                (double)GAME_WIDTH /
                GAME_HEIGHT;

            int gameWidth;
            int gameHeight;
            int offsetX;
            int offsetY;

            /*
             * 横が余る場合。
             *
             * 左右に黒帯がある。
             */
            if (windowAspect > gameAspect)
            {
                gameHeight =
                    windowRect.Height;

                gameWidth =
                    (int)Math.Round(
                        gameHeight *
                        gameAspect);

                offsetX =
                    (windowRect.Width -
                     gameWidth) / 2;

                offsetY =
                    0;
            }
            else
            {
                /*
                 * 縦が余る場合。
                 *
                 * 通常はこちらには
                 * ならない想定。
                 */
                gameWidth =
                    windowRect.Width;

                gameHeight =
                    (int)Math.Round(
                        gameWidth /
                        gameAspect);

                offsetX =
                    0;

                offsetY =
                    (windowRect.Height -
                     gameHeight) / 2;
            }

            return new Rectangle(
                windowRect.Left +
                    offsetX,

                windowRect.Top +
                    offsetY,

                gameWidth,
                gameHeight);
        }

        /*
         * --------------------------------
         * オーバーレイ位置更新
         * --------------------------------
         */
        private bool SetGameAreaPosition()
        {
            Process process =
                FindGameProcess();

            if (process == null)
            {
                return false;
            }

            try
            {
                RECT rect;

                if (!GetWindowRect(
                    process.MainWindowHandle,
                    out rect))
                {
                    return false;
                }

                Rectangle windowRect =
                    Rectangle.FromLTRB(
                        rect.Left,
                        rect.Top,
                        rect.Right,
                        rect.Bottom);

                Rectangle gameRect =
                    GetGameDisplayRectangle(
                        windowRect);

                if (gameRect.Width <= 0 ||
                    gameRect.Height <= 0)
                {
                    return false;
                }

                /*
                 * ゲーム表示領域の左上+(3, 5)px。
                 */
                Location =
                    new Point(
                        gameRect.Left + 3,
                        gameRect.Top + 10);

                /*
                 * オーバーレイは
                 * 内容サイズだけでよい。
                 */
                ResizeToContent();

                /*
                 * 天則より上へ。
                 */
                SetWindowPos(
                    Handle,
                    HWND_TOPMOST,
                    Location.X,
                    Location.Y,
                    Width,
                    Height,
                    SWP_NOACTIVATE |
                    SWP_SHOWWINDOW);

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                process.Dispose();
            }
        }

        /*
         * --------------------------------
         * 内容サイズ
         * --------------------------------
         */
        private void ResizeToContent()
        {
            Size preferred =
                lblText.GetPreferredSize(
                    new Size(
                        0,
                        0));

            int width =
                preferred.Width +
                Padding.Left +
                Padding.Right;

            int height =
                preferred.Height +
                Padding.Top +
                Padding.Bottom;

            if (width < 1)
            {
                width = 1;
            }

            if (height < 1)
            {
                height = 1;
            }

            Size =
                new Size(
                    width,
                    height);
        }

        /*
         * --------------------------------
         * Dispose
         * --------------------------------
         */
        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                if (positionTimer != null)
                {
                    positionTimer.Stop();
                    positionTimer.Dispose();
                }

                if (lblText != null)
                {
                    lblText.Dispose();
                }

                if (overlayFont != null)
                {
                    overlayFont.Dispose();
                }
            }

            base.Dispose(
                disposing);
        }
    }
}