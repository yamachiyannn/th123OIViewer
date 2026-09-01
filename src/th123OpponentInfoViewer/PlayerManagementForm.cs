using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace th123OpponentInfoViewer
{
    public class PlayerManagementForm : Form
    {
        private readonly CombinedPlayersDatabase database;
        private readonly TskDatabaseReader tskDatabase;

        private List<CombinedPlayer> allPlayers =
            new List<CombinedPlayer>();

        private List<string> allProfileNames =
            new List<string>();

        private CombinedPlayer selectedPlayer;

        private ListBox lstPlayers;
        private TextBox txtPlayerName;
        private Label lblPlayerId;
        private Label lblRepresentative;
        private ListBox lstPlayerProfiles;

        private Button btnRenamePlayer;
        private Button btnDeletePlayer;
        private Button btnRemoveProfile;
        private Button btnSetRepresentative;

        private TextBox txtProfileSearch;
        private ListBox lstProfiles;

        private Button btnAllProfiles;
        private Button btnUnregisteredProfiles;
        private Button btnRegisteredProfiles;

        private Label lblStatus;

        private enum ProfileFilter
        {
            All,
            Unregistered,
            Registered
        }

        private ProfileFilter currentProfileFilter =
            ProfileFilter.All;

        private bool updatingUi;

        public PlayerManagementForm(
            CombinedPlayersDatabase database,
            TskDatabaseReader tskDatabase)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            if (tskDatabase == null)
            {
                throw new ArgumentNullException("tskDatabase");
            }

            this.database =
                database;

            this.tskDatabase =
                tskDatabase;

            InitializeForm();

            LoadData();
        }

        private void InitializeForm()
        {
            Text =
                "プレイヤー管理";

            StartPosition =
                FormStartPosition.CenterParent;

            Width =
                1100;

            Height =
                700;

            MinimumSize =
                new Size(
                    900,
                    550);

            Font =
                new Font(
                    "MS Gothic",
                    9.0f);

            TableLayoutPanel root =
                new TableLayoutPanel();

            root.Dock =
                DockStyle.Fill;

            root.ColumnCount =
                2;

            root.RowCount =
                2;

            root.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    45.0f));

            root.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    55.0f));

            root.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    100.0f));

            root.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    32.0f));

            Controls.Add(
                root);

            root.Controls.Add(
                CreatePlayerPanel(),
                0,
                0);

            root.Controls.Add(
                CreateProfilePanel(),
                1,
                0);

            lblStatus =
                new Label();

            lblStatus.Dock =
                DockStyle.Fill;

            lblStatus.Padding =
                new Padding(
                    8,
                    0,
                    8,
                    0);

            lblStatus.TextAlign =
                ContentAlignment.MiddleLeft;

            root.Controls.Add(
                lblStatus,
                0,
                1);

            root.SetColumnSpan(
                lblStatus,
                2);
        }

        private Control CreatePlayerPanel()
        {
            GroupBox group =
                new GroupBox();

            group.Text =
                "プレイヤー";

            group.Dock =
                DockStyle.Fill;

            group.Padding =
                new Padding(8);

            TableLayoutPanel layout =
                new TableLayoutPanel();

            layout.Dock =
                DockStyle.Fill;

            layout.ColumnCount =
                1;

            layout.RowCount =
                8;

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    26));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    32));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    26));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    28));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    24));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    24));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    68));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    40));

            group.Controls.Add(
                layout);

            /*
             * ========================================================
             * 登録済みプレイヤー
             * ========================================================
             */

            Label lblPlayers =
                new Label();

            lblPlayers.Text =
                "登録済みプレイヤー";

            lblPlayers.Dock =
                DockStyle.Fill;

            lblPlayers.TextAlign =
                ContentAlignment.MiddleLeft;

            layout.Controls.Add(
                lblPlayers,
                0,
                0);

            lstPlayers =
                new ListBox();

            lstPlayers.Dock =
                DockStyle.Fill;

            lstPlayers.HorizontalScrollbar =
                true;

            lstPlayers.SelectionMode =
                SelectionMode.One;

            lstPlayers.SelectedIndexChanged +=
                LstPlayers_SelectedIndexChanged;

            layout.Controls.Add(
                lstPlayers,
                0,
                1);

            /*
             * ========================================================
             * プレイヤー名
             * ========================================================
             */

            Label lblName =
                new Label();

            lblName.Text =
                "プレイヤー名";

            lblName.Dock =
                DockStyle.Fill;

            lblName.TextAlign =
                ContentAlignment.MiddleLeft;

            layout.Controls.Add(
                lblName,
                0,
                2);

            txtPlayerName =
                new TextBox();

            txtPlayerName.Dock =
                DockStyle.Fill;

            txtPlayerName.Enabled =
                false;

            layout.Controls.Add(
                txtPlayerName,
                0,
                3);

            /*
             * ========================================================
             * Player ID
             * ========================================================
             */

            lblPlayerId =
                new Label();

            lblPlayerId.Text =
                "未選択";

            lblPlayerId.Dock =
                DockStyle.Fill;

            lblPlayerId.TextAlign =
                ContentAlignment.MiddleLeft;

            layout.Controls.Add(
                lblPlayerId,
                0,
                4);

            /*
             * ========================================================
             * 所属プロファイル
             * ========================================================
             */

            Label lblDetail =
                new Label();

            lblDetail.Text =
                "所属プロファイル";

            lblDetail.Dock =
                DockStyle.Fill;

            lblDetail.TextAlign =
                ContentAlignment.MiddleLeft;

            layout.Controls.Add(
                lblDetail,
                0,
                5);

            TableLayoutPanel profileDetail =
                new TableLayoutPanel();

            profileDetail.Dock =
                DockStyle.Fill;

            profileDetail.ColumnCount =
                1;

            profileDetail.RowCount =
                3;

            profileDetail.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    24));

            profileDetail.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    100));

            profileDetail.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    38));

            /*
             * 代表プロファイル
             */

            lblRepresentative =
                new Label();

            lblRepresentative.Text =
                "代表プロファイル : ---";

            lblRepresentative.Dock =
                DockStyle.Fill;

            lblRepresentative.TextAlign =
                ContentAlignment.MiddleLeft;

            profileDetail.Controls.Add(
                lblRepresentative,
                0,
                0);

            /*
             * 所属プロファイル一覧
             */

            lstPlayerProfiles =
                new ListBox();

            lstPlayerProfiles.Dock =
                DockStyle.Fill;

            lstPlayerProfiles.SelectionMode =
                SelectionMode.One;

            lstPlayerProfiles.HorizontalScrollbar =
                true;

            lstPlayerProfiles.SelectedIndexChanged +=
                LstPlayerProfiles_SelectedIndexChanged;

            profileDetail.Controls.Add(
                lstPlayerProfiles,
                0,
                1);

            /*
             * プロファイル操作ボタン
             */

            FlowLayoutPanel profileButtons =
                new FlowLayoutPanel();

            profileButtons.Dock =
                DockStyle.Fill;

            profileButtons.FlowDirection =
                FlowDirection.LeftToRight;

            profileButtons.WrapContents =
                false;

            profileButtons.AutoScroll =
                true;

            btnSetRepresentative =
                new Button();

            btnSetRepresentative.Text =
                "代表にする";

            btnSetRepresentative.Width =
                100;

            btnSetRepresentative.Enabled =
                false;

            btnSetRepresentative.Click +=
                BtnSetRepresentative_Click;

            profileButtons.Controls.Add(
                btnSetRepresentative);

            btnRemoveProfile =
                new Button();

            btnRemoveProfile.Text =
                "所属から削除";

            btnRemoveProfile.Width =
                100;

            btnRemoveProfile.Enabled =
                false;

            btnRemoveProfile.Click +=
                BtnRemoveProfile_Click;

            profileButtons.Controls.Add(
                btnRemoveProfile);

            profileDetail.Controls.Add(
                profileButtons,
                0,
                2);

            layout.Controls.Add(
                profileDetail,
                0,
                6);

            /*
             * ========================================================
             * プレイヤー操作ボタン
             * ========================================================
             *
             * 「プロファイル追加」はここには置かない。
             *
             * プレイヤー名変更とプレイヤー削除のみ。
             */

            FlowLayoutPanel buttons =
                new FlowLayoutPanel();

            buttons.Dock =
                DockStyle.Fill;

            buttons.FlowDirection =
                FlowDirection.LeftToRight;

            buttons.WrapContents =
                false;

            buttons.AutoScroll =
                true;

            /*
             * プレイヤー名変更
             */

            btnRenamePlayer =
                new Button();

            btnRenamePlayer.Text =
                "プレイヤー名変更";

            btnRenamePlayer.Width =
                120;

            btnRenamePlayer.Enabled =
                false;

            btnRenamePlayer.Click +=
                BtnRenamePlayer_Click;

            buttons.Controls.Add(
                btnRenamePlayer);

            /*
             * プレイヤー削除
             */

            btnDeletePlayer =
                new Button();

            btnDeletePlayer.Text =
                "プレイヤー削除";

            btnDeletePlayer.Width =
                110;

            btnDeletePlayer.Enabled =
                false;

            btnDeletePlayer.Click +=
                BtnDeletePlayer_Click;

            buttons.Controls.Add(
                btnDeletePlayer);

            layout.Controls.Add(
                buttons,
                0,
                7);

            return group;
        }

        private Control CreateProfilePanel()
        {
            GroupBox group =
                new GroupBox();

            group.Text =
                "プロファイル一覧";

            group.Dock =
                DockStyle.Fill;

            group.Padding =
                new Padding(8);

            TableLayoutPanel layout =
                new TableLayoutPanel();

            layout.Dock =
                DockStyle.Fill;

            layout.ColumnCount =
                1;

            layout.RowCount =
                5;

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    28));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    34));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    30));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    100));

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    48));

            group.Controls.Add(
                layout);

            /*
             * ========================================================
             * 説明
             * ========================================================
             */

            Label lbl =
                new Label();

            lbl.Text =
                "Default.db に記録されているプロファイル";

            lbl.Dock =
                DockStyle.Fill;

            lbl.TextAlign =
                ContentAlignment.MiddleLeft;

            layout.Controls.Add(
                lbl,
                0,
                0);

            /*
             * ========================================================
             * 絞り込みボタン
             * ========================================================
             */

            FlowLayoutPanel filterButtons =
                new FlowLayoutPanel();

            filterButtons.Dock =
                DockStyle.Fill;

            filterButtons.FlowDirection =
                FlowDirection.LeftToRight;

            filterButtons.WrapContents =
                false;

            filterButtons.AutoScroll =
                true;

            /*
             * 全部
             */

            btnAllProfiles =
                new Button();

            btnAllProfiles.Text =
                "全部";

            btnAllProfiles.Width =
                80;

            btnAllProfiles.Click +=
                BtnAllProfiles_Click;

            filterButtons.Controls.Add(
                btnAllProfiles);

            /*
             * 未登録
             */

            btnUnregisteredProfiles =
                new Button();

            btnUnregisteredProfiles.Text =
                "未登録";

            btnUnregisteredProfiles.Width =
                90;

            btnUnregisteredProfiles.Click +=
                BtnUnregisteredProfiles_Click;

            filterButtons.Controls.Add(
                btnUnregisteredProfiles);

            /*
             * 登録済み
             */

            btnRegisteredProfiles =
                new Button();

            btnRegisteredProfiles.Text =
                "登録済み";

            btnRegisteredProfiles.Width =
                90;

            btnRegisteredProfiles.Click +=
                BtnRegisteredProfiles_Click;

            filterButtons.Controls.Add(
                btnRegisteredProfiles);

            layout.Controls.Add(
                filterButtons,
                0,
                1);

            /*
             * ========================================================
             * 検索
             * ========================================================
             */

            txtProfileSearch =
                new TextBox();

            txtProfileSearch.Dock =
                DockStyle.Fill;

            txtProfileSearch.TextChanged +=
                TxtProfileSearch_TextChanged;

            layout.Controls.Add(
                txtProfileSearch,
                0,
                2);

            /*
             * ========================================================
             * プロファイル一覧
             * ========================================================
             */

            lstProfiles =
                new ListBox();

            lstProfiles.Dock =
                DockStyle.Fill;

            lstProfiles.SelectionMode =
                SelectionMode.One;

            lstProfiles.HorizontalScrollbar =
                true;

            lstProfiles.SelectedIndexChanged +=
                LstProfiles_SelectedIndexChanged;

            layout.Controls.Add(
                lstProfiles,
                0,
                3);

            /*
             * ========================================================
             * 説明
             * ========================================================
             */

            Label help =
                new Label();

            help.Dock =
                DockStyle.Fill;

            help.TextAlign =
                ContentAlignment.MiddleLeft;

            help.Text =
                "プロファイルを選択すると、登録状態に応じて操作できます。";

            layout.Controls.Add(
                help,
                0,
                4);

            return group;
        }

        private void LoadData()
        {
            try
            {
                LoadPlayers();

                LoadProfiles();

                ClearPlayerSelection();

                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "プレイヤー管理の読み込みに失敗しました。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadPlayers()
        {
            allPlayers =
                database
                    .GetPlayers()
                    .OrderBy(
                        x => database.GetDisplayName(x),
                        StringComparer.CurrentCulture)
                    .ToList();

            RefreshPlayerList();
        }

        private void LoadProfiles()
        {
            allProfileNames =
                tskDatabase
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

            RefreshProfileList();
        }

        private void RefreshPlayerList()
        {
            if (lstPlayers == null)
            {
                return;
            }

            int selectedId =
                selectedPlayer == null
                    ? -1
                    : selectedPlayer.PlayerId;

            updatingUi =
                true;

            try
            {
                lstPlayers.BeginUpdate();

                try
                {
                    lstPlayers.Items.Clear();

                    foreach (CombinedPlayer player
                        in allPlayers)
                    {
                        lstPlayers.Items.Add(
                            player);
                    }

                    if (selectedId >= 0)
                    {
                        SelectPlayerById(
                            selectedId);
                    }
                }
                finally
                {
                    lstPlayers.EndUpdate();
                }
            }
            finally
            {
                updatingUi =
                    false;
            }
        }

        private void RefreshProfileList()
        {
            if (lstProfiles == null)
            {
                return;
            }

            string keyword =
                txtProfileSearch == null
                    ? ""
                    : txtProfileSearch.Text;

            IEnumerable<string> filtered =
                allProfileNames;

            /*
             * ========================================================
             * 登録状態による絞り込み
             * ========================================================
             */

            if (currentProfileFilter ==
                ProfileFilter.Unregistered)
            {
                filtered =
                    filtered.Where(
                        profile =>
                            FindPlayerByProfile(
                                profile) == null);
            }
            else if (
                currentProfileFilter ==
                ProfileFilter.Registered)
            {
                filtered =
                    filtered.Where(
                        profile =>
                            FindPlayerByProfile(
                                profile) != null);
            }

            /*
             * ========================================================
             * 検索文字列
             * ========================================================
             */

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

            List<string> filteredList =
                filtered.ToList();

            string selectedProfile =
                GetSelectedProfile();

            updatingUi =
                true;

            try
            {
                lstProfiles.BeginUpdate();

                try
                {
                    lstProfiles.Items.Clear();

                    foreach (string profile
                        in filteredList)
                    {
                        lstProfiles.Items.Add(
                            profile);
                    }

                    if (!string.IsNullOrWhiteSpace(
                        selectedProfile))
                    {
                        for (int i = 0;
                             i < lstProfiles.Items.Count;
                             i++)
                        {
                            if (string.Equals(
                                Convert.ToString(
                                    lstProfiles.Items[i]),
                                selectedProfile,
                                StringComparison.Ordinal))
                            {
                                lstProfiles.SelectedIndex =
                                    i;

                                break;
                            }
                        }
                    }
                }
                finally
                {
                    lstProfiles.EndUpdate();
                }
            }
            finally
            {
                updatingUi =
                    false;
            }

            UpdateFilterButtonState();
        }

        private void UpdateFilterButtonState()
        {
            if (btnAllProfiles == null)
            {
                return;
            }

            /*
             * 現在選択中のフィルターだけ
             * ボタンを無効化する。
             */

            btnAllProfiles.Enabled =
                currentProfileFilter !=
                ProfileFilter.All;

            btnUnregisteredProfiles.Enabled =
                currentProfileFilter !=
                ProfileFilter.Unregistered;

            btnRegisteredProfiles.Enabled =
                currentProfileFilter !=
                ProfileFilter.Registered;
        }

        private void BtnAllProfiles_Click(
            object sender,
            EventArgs e)
        {
            currentProfileFilter =
                ProfileFilter.All;

            RefreshProfileList();
        }

        private void BtnUnregisteredProfiles_Click(
            object sender,
            EventArgs e)
        {
            currentProfileFilter =
                ProfileFilter.Unregistered;

            RefreshProfileList();
        }

        private void BtnRegisteredProfiles_Click(
            object sender,
            EventArgs e)
        {
            currentProfileFilter =
                ProfileFilter.Registered;

            RefreshProfileList();
        }

        private void LstPlayers_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            if (updatingUi)
            {
                return;
            }

            selectedPlayer =
                lstPlayers.SelectedItem
                    as CombinedPlayer;

            if (selectedPlayer == null)
            {
                ClearPlayerSelection();

                return;
            }

            ShowSelectedPlayer();
        }

        private void ShowSelectedPlayer()
        {
            if (selectedPlayer == null)
            {
                return;
            }

            txtPlayerName.Text =
                selectedPlayer.PlayerName;

            txtPlayerName.Enabled =
                false;

            btnRenamePlayer.Enabled =
                true;

            btnDeletePlayer.Enabled =
                true;

            lblPlayerId.Text =
                "Player ID : " +
                selectedPlayer.PlayerId;

            lblRepresentative.Text =
                "代表プロファイル : " +
                selectedPlayer.RepresentativeProfile;

            RefreshPlayerProfileList();

            UpdateProfileButtons();
        }

        private void RefreshPlayerProfileList()
        {
            if (lstPlayerProfiles == null)
            {
                return;
            }

            lstPlayerProfiles.BeginUpdate();

            try
            {
                lstPlayerProfiles.Items.Clear();

                if (selectedPlayer == null)
                {
                    return;
                }

                if (selectedPlayer.Profiles == null)
                {
                    return;
                }

                foreach (string profile
                    in selectedPlayer.Profiles)
                {
                    lstPlayerProfiles.Items.Add(
                        profile);
                }
            }
            finally
            {
                lstPlayerProfiles.EndUpdate();
            }
        }

        private void ClearPlayerSelection()
        {
            updatingUi =
                true;

            try
            {
                selectedPlayer =
                    null;

                if (lstPlayers != null)
                {
                    lstPlayers.ClearSelected();
                }

                if (txtPlayerName != null)
                {
                    txtPlayerName.Clear();

                    txtPlayerName.Enabled =
                        false;
                }

                if (btnRenamePlayer != null)
                {
                    btnRenamePlayer.Enabled =
                        false;
                }

                if (btnDeletePlayer != null)
                {
                    btnDeletePlayer.Enabled =
                        false;
                }

                if (lblPlayerId != null)
                {
                    lblPlayerId.Text =
                        "未選択";
                }

                if (lblRepresentative != null)
                {
                    lblRepresentative.Text =
                        "代表プロファイル : ---";
                }

                if (lstPlayerProfiles != null)
                {
                    lstPlayerProfiles.Items.Clear();
                }
            }
            finally
            {
                updatingUi =
                    false;
            }

            UpdateProfileButtons();
        }

        private void TxtProfileSearch_TextChanged(
            object sender,
            EventArgs e)
        {
            RefreshProfileList();
        }

        private void LstProfiles_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            if (updatingUi)
            {
                return;
            }

            string profile =
                GetSelectedProfile();

            if (string.IsNullOrWhiteSpace(
                profile))
            {
                return;
            }

            HandleProfileSelection(
                profile);
        }

        private void HandleProfileSelection(
            string profile)
        {
            CombinedPlayer owner =
                FindPlayerByProfile(
                    profile);

            /*
             * ========================================================
             * 既に登録済み
             * ========================================================
             */

            if (owner != null)
            {
                DialogResult result =
                    MessageBox.Show(
                        "プロファイル「" +
                        profile +
                        "」は、プレイヤー「" +
                        database.GetDisplayName(owner) +
                        "」に登録されています。\r\n\r\n" +
                        "このプロファイルを所属から削除しますか？",
                        "登録済みプロファイル",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                if (result ==
                    DialogResult.Yes)
                {
                    RemoveProfileFromPlayer(
                        owner,
                        profile);
                }

                return;
            }

            /*
             * ========================================================
             * 未登録
             * ========================================================
             */

            DialogResult createResult =
                MessageBox.Show(
                    "プロファイル「" +
                    profile +
                    "」はまだプレイヤーに登録されていません。\r\n\r\n" +
                    "「はい」：新しいプレイヤーとして登録\r\n" +
                    "「いいえ」：既存プレイヤーに登録\r\n" +
                    "「キャンセル」：何もしない",
                    "プロファイル登録",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

            if (createResult ==
                DialogResult.Yes)
            {
                CreatePlayerFromProfile(
                    profile);

                return;
            }

            if (createResult ==
                DialogResult.No)
            {
                ChooseExistingPlayerAndAdd(
                    profile);
            }
        }

        private CombinedPlayer FindPlayerByProfile(
            string profile)
        {
            return allPlayers.FirstOrDefault(
                x =>
                    x.Profiles != null &&
                    x.Profiles.Any(
                        p =>
                            string.Equals(
                                p,
                                profile,
                                StringComparison.Ordinal)));
        }

        private void CreatePlayerFromProfile(
            string profile)
        {
            try
            {
                int playerId =
                    database.CreatePlayer(
                        profile,
                        profile,
                        new List<string>
                        {
                            profile
                        });

                ReloadAfterPlayerChange(
                    playerId);

                lblStatus.Text =
                    "プロファイル「" +
                    profile +
                    "」を新しいプレイヤーとして登録しました。";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "プレイヤーを登録できませんでした。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ChooseExistingPlayerAndAdd(
            string profile)
        {
            if (allPlayers.Count == 0)
            {
                MessageBox.Show(
                    "既存のプレイヤーがありません。\r\n" +
                    "新規プレイヤーとして登録してください。",
                    "確認",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using (Form dialog =
                new Form())
            {
                dialog.Text =
                    "登録先プレイヤーを選択";

                dialog.StartPosition =
                    FormStartPosition.CenterParent;

                dialog.Width =
                    450;

                dialog.Height =
                    450;

                dialog.MinimumSize =
                    new Size(
                        350,
                        300);

                ListBox list =
                    new ListBox();

                list.Dock =
                    DockStyle.Fill;

                foreach (CombinedPlayer player
                    in allPlayers)
                {
                    list.Items.Add(
                        player);
                }

                Button ok =
                    new Button();

                ok.Text =
                    "登録";

                ok.Width =
                    90;

                ok.DialogResult =
                    DialogResult.OK;

                Button cancel =
                    new Button();

                cancel.Text =
                    "キャンセル";

                cancel.Width =
                    90;

                cancel.DialogResult =
                    DialogResult.Cancel;

                FlowLayoutPanel bottom =
                    new FlowLayoutPanel();

                bottom.Dock =
                    DockStyle.Bottom;

                bottom.Height =
                    45;

                bottom.FlowDirection =
                    FlowDirection.RightToLeft;

                bottom.Controls.Add(
                    cancel);

                bottom.Controls.Add(
                    ok);

                dialog.Controls.Add(
                    list);

                dialog.Controls.Add(
                    bottom);

                dialog.AcceptButton =
                    ok;

                dialog.CancelButton =
                    cancel;

                if (dialog.ShowDialog(this) !=
                    DialogResult.OK)
                {
                    return;
                }

                CombinedPlayer targetPlayer =
                    list.SelectedItem
                        as CombinedPlayer;

                if (targetPlayer == null)
                {
                    MessageBox.Show(
                        "登録先プレイヤーを選択してください。",
                        "確認",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return;
                }

                List<string> profiles =
                    targetPlayer.Profiles == null
                        ? new List<string>()
                        : new List<string>(
                            targetPlayer.Profiles);

                if (!profiles.Contains(
                    profile,
                    StringComparer.Ordinal))
                {
                    profiles.Add(
                        profile);
                }

                /*
                 * 既存プレイヤーへ追加する場合、
                 * 代表プロファイルは変更しない。
                 *
                 * この時点でDBへ即保存される。
                 */

                database.UpdatePlayer(
                    targetPlayer.PlayerId,
                    targetPlayer.PlayerName,
                    targetPlayer.RepresentativeProfile,
                    profiles);

                ReloadAfterPlayerChange(
                    targetPlayer.PlayerId);

                lblStatus.Text =
                    "プロファイル「" +
                    profile +
                    "」を「" +
                    database.GetDisplayName(targetPlayer) +
                    "」に追加しました。";
            }
        }

        /*
         * ========================================================
         * プレイヤー名変更
         * ========================================================
         */

        private void BtnRenamePlayer_Click(
            object sender,
            EventArgs e)
        {
            if (selectedPlayer == null)
            {
                return;
            }

            string currentName =
                database.GetDisplayName(
                    selectedPlayer);

            using (Form dialog =
                new Form())
            {
                dialog.Text =
                    "プレイヤー名変更";

                dialog.StartPosition =
                    FormStartPosition.CenterParent;

                dialog.Width =
                    430;

                dialog.Height =
                    170;

                dialog.FormBorderStyle =
                    FormBorderStyle.FixedDialog;

                dialog.MaximizeBox =
                    false;

                dialog.MinimizeBox =
                    false;

                Label label =
                    new Label();

                label.Text =
                    "新しいプレイヤー名";

                label.Left =
                    10;

                label.Top =
                    15;

                label.Width =
                    180;

                TextBox text =
                    new TextBox();

                text.Left =
                    10;

                text.Top =
                    40;

                text.Width =
                    390;

                text.Text =
                    currentName;

                text.SelectAll();

                Button ok =
                    new Button();

                ok.Text =
                    "変更";

                ok.Width =
                    90;

                ok.Left =
                    215;

                ok.Top =
                    80;

                ok.DialogResult =
                    DialogResult.OK;

                Button cancel =
                    new Button();

                cancel.Text =
                    "キャンセル";

                cancel.Width =
                    90;

                cancel.Left =
                    310;

                cancel.Top =
                    80;

                cancel.DialogResult =
                    DialogResult.Cancel;

                dialog.Controls.Add(
                    label);

                dialog.Controls.Add(
                    text);

                dialog.Controls.Add(
                    ok);

                dialog.Controls.Add(
                    cancel);

                dialog.AcceptButton =
                    ok;

                dialog.CancelButton =
                    cancel;

                if (dialog.ShowDialog(this) !=
                    DialogResult.OK)
                {
                    return;
                }

                string newName =
                    text.Text.Trim();

                if (string.IsNullOrWhiteSpace(
                    newName))
                {
                    MessageBox.Show(
                        "プレイヤー名を入力してください。",
                        "確認",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return;
                }

                RenameSelectedPlayer(
                    newName);
            }
        }

        private void RenameSelectedPlayer(
            string newName)
        {
            if (selectedPlayer == null)
            {
                return;
            }

            try
            {
                int id =
                    selectedPlayer.PlayerId;

                /*
                 * プレイヤー名変更時点で
                 * DBへ即保存する。
                 */

                database.UpdatePlayer(
                    id,
                    newName,
                    selectedPlayer.RepresentativeProfile,
                    new List<string>(
                        selectedPlayer.Profiles));

                ReloadAfterPlayerChange(
                    id);

                lblStatus.Text =
                    "プレイヤー名を「" +
                    newName +
                    "」に変更しました。";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "プレイヤー名を変更できませんでした。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LstPlayerProfiles_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            UpdateProfileButtons();
        }

        private void UpdateProfileButtons()
        {
            bool hasPlayer =
                selectedPlayer != null;

            bool hasProfile =
                lstPlayerProfiles != null &&
                lstPlayerProfiles.SelectedIndex >= 0;

            if (btnSetRepresentative != null)
            {
                btnSetRepresentative.Enabled =
                    hasPlayer &&
                    hasProfile;
            }

            if (btnRemoveProfile != null)
            {
                btnRemoveProfile.Enabled =
                    hasPlayer &&
                    hasProfile;
            }

            if (btnRenamePlayer != null)
            {
                btnRenamePlayer.Enabled =
                    hasPlayer;
            }

            if (btnDeletePlayer != null)
            {
                btnDeletePlayer.Enabled =
                    hasPlayer;
            }
        }

        private string GetSelectedProfile()
        {
            if (lstProfiles == null ||
                lstProfiles.SelectedIndex < 0)
            {
                return "";
            }

            return Convert.ToString(
                lstProfiles.SelectedItem);
        }

        private string GetSelectedPlayerProfile()
        {
            if (lstPlayerProfiles == null ||
                lstPlayerProfiles.SelectedIndex < 0)
            {
                return "";
            }

            return Convert.ToString(
                lstPlayerProfiles.SelectedItem);
        }

        /*
         * ========================================================
         * 代表プロファイル変更
         * ========================================================
         */

        private void BtnSetRepresentative_Click(
            object sender,
            EventArgs e)
        {
            if (selectedPlayer == null)
            {
                return;
            }

            string profile =
                GetSelectedPlayerProfile();

            if (string.IsNullOrWhiteSpace(
                profile))
            {
                return;
            }

            if (string.Equals(
                selectedPlayer.RepresentativeProfile,
                profile,
                StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                /*
                 * 代表変更時点でDBへ即保存。
                 */

                database.UpdatePlayer(
                    selectedPlayer.PlayerId,
                    selectedPlayer.PlayerName,
                    profile,
                    new List<string>(
                        selectedPlayer.Profiles));

                int id =
                    selectedPlayer.PlayerId;

                ReloadAfterPlayerChange(
                    id);

                lblStatus.Text =
                    "代表プロファイルを「" +
                    profile +
                    "」に変更しました。";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "代表プロファイルを変更できませんでした。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * ========================================================
         * 所属プロファイル削除
         * ========================================================
         */

        private void BtnRemoveProfile_Click(
            object sender,
            EventArgs e)
        {
            if (selectedPlayer == null)
            {
                return;
            }

            string profile =
                GetSelectedPlayerProfile();

            if (string.IsNullOrWhiteSpace(
                profile))
            {
                return;
            }

            if (selectedPlayer.Profiles == null ||
                selectedPlayer.Profiles.Count <= 1)
            {
                MessageBox.Show(
                    "プレイヤーには少なくとも1つのプロファイルが必要です。",
                    "確認",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            DialogResult result =
                MessageBox.Show(
                    "プロファイル「" +
                    profile +
                    "」をこのプレイヤーから削除しますか？",
                    "所属プロファイル削除",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (result !=
                DialogResult.Yes)
            {
                return;
            }

            RemoveProfileFromPlayer(
                selectedPlayer,
                profile);
        }

        private void RemoveProfileFromPlayer(
            CombinedPlayer player,
            string profile)
        {
            if (player == null)
            {
                return;
            }

            List<string> profiles =
                player.Profiles == null
                    ? new List<string>()
                    : new List<string>(
                        player.Profiles);

            profiles.RemoveAll(
                x =>
                    string.Equals(
                        x,
                        profile,
                        StringComparison.Ordinal));

            if (profiles.Count == 0)
            {
                MessageBox.Show(
                    "プロファイルをすべて削除することはできません。",
                    "確認",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string representative =
                player.RepresentativeProfile;

            /*
             * 代表プロファイルを削除する場合は、
             * 残っている先頭プロファイルを
             * 新しい代表にする。
             */

            if (string.Equals(
                representative,
                profile,
                StringComparison.Ordinal))
            {
                representative =
                    profiles[0];
            }

            try
            {
                /*
                 * プロファイル削除時点で
                 * DBへ即保存する。
                 */

                database.UpdatePlayer(
                    player.PlayerId,
                    player.PlayerName,
                    representative,
                    profiles);

                ReloadAfterPlayerChange(
                    player.PlayerId);

                lblStatus.Text =
                    "プロファイル「" +
                    profile +
                    "」をプレイヤーから削除しました。";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "プロファイルを削除できませんでした。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * ========================================================
         * プレイヤー削除
         * ========================================================
         */

        private void BtnDeletePlayer_Click(
            object sender,
            EventArgs e)
        {
            if (selectedPlayer == null)
            {
                return;
            }

            string displayName =
                database.GetDisplayName(
                    selectedPlayer);

            DialogResult result =
                MessageBox.Show(
                    "プレイヤー「" +
                    displayName +
                    "」を削除しますか？\r\n\r\n" +
                    "所属プロファイルの登録も削除されます。",
                    "プレイヤー削除",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

            if (result !=
                DialogResult.Yes)
            {
                return;
            }

            try
            {
                database.DeletePlayer(
                    selectedPlayer.PlayerId);

                selectedPlayer =
                    null;

                LoadPlayers();

                ClearPlayerSelection();

                RefreshProfileList();

                lblStatus.Text =
                    "プレイヤーを削除しました。";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "プレイヤーを削除できませんでした。\r\n\r\n" +
                    ex.Message,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /*
         * ========================================================
         * プレイヤー変更後の再読み込み
         * ========================================================
         */

        private void ReloadAfterPlayerChange(
            int playerId)
        {
            allPlayers =
                database
                    .GetPlayers()
                    .OrderBy(
                        x => database.GetDisplayName(x),
                        StringComparer.CurrentCulture)
                    .ToList();

            selectedPlayer =
                allPlayers.FirstOrDefault(
                    x =>
                        x.PlayerId ==
                        playerId);

            RefreshPlayerList();

            if (selectedPlayer != null)
            {
                ShowSelectedPlayer();
            }
            else
            {
                ClearPlayerSelection();
            }

            RefreshProfileList();

            UpdateStatus();
        }

        /*
         * ========================================================
         * PlayerIdからリスト選択
         * ========================================================
         */

        private void SelectPlayerById(
            int playerId)
        {
            if (lstPlayers == null)
            {
                return;
            }

            for (int i = 0;
                 i < lstPlayers.Items.Count;
                 i++)
            {
                CombinedPlayer player =
                    lstPlayers.Items[i]
                        as CombinedPlayer;

                if (player == null)
                {
                    continue;
                }

                if (player.PlayerId !=
                    playerId)
                {
                    continue;
                }

                lstPlayers.SelectedIndex =
                    i;

                return;
            }
        }

        /*
         * ========================================================
         * ステータス表示
         * ========================================================
         */

        private void UpdateStatus()
        {
            if (lblStatus == null)
            {
                return;
            }

            int registeredCount =
                allProfileNames.Count(
                    profile =>
                        FindPlayerByProfile(
                            profile) != null);

            int unregisteredCount =
                allProfileNames.Count -
                registeredCount;

            lblStatus.Text =
                "プレイヤー " +
                allPlayers.Count +
                "件 / プロファイル " +
                allProfileNames.Count +
                "件" +
                "（登録済み " +
                registeredCount +
                " / 未登録 " +
                unregisteredCount +
                "）";
        }

        protected override void OnFormClosed(
            FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }
    }
}