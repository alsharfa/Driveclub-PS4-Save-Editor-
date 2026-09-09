using System.Globalization;

namespace DriveclubSaveEditor;

public sealed class MainForm : Form
{
    private sealed record VehicleChoice(string Code, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record GarageRowState(DriveclubSave.StatsStoreEntry Stat, int OriginalLevel, ulong? OriginalFame);
    private sealed record FloatCounterRowState(DriveclubSave.FloatEntry Entry, float OriginalValue);
    private sealed record ScalarRowState(DriveclubSave.ScalarEntry Entry, long OriginalSigned, ulong OriginalUnsigned);

    private enum TrophyEditKind
    {
        Fame,
        StatValue1,
        FloatCounter
    }

    private sealed record TrophyEditTarget(
        TrophyEditKind Kind,
        string Key,
        decimal? Requirement,
        string RequirementText,
        decimal CurrentValue,
        decimal OriginalValue);

    private readonly Dictionary<string, decimal> _trophyOriginalValues = new(StringComparer.Ordinal);

    // Refined dark UI palette. No third-party UI framework is required.
    private static readonly Color AppBack = Color.FromArgb(10, 13, 18);
    private static readonly Color SidebarBack = Color.FromArgb(13, 17, 23);
    private static readonly Color Surface = Color.FromArgb(20, 25, 33);
    private static readonly Color SurfaceRaised = Color.FromArgb(27, 33, 43);
    private static readonly Color SurfaceHover = Color.FromArgb(34, 42, 54);
    private static readonly Color InputBack = Color.FromArgb(16, 21, 28);
    private static readonly Color Border = Color.FromArgb(43, 52, 66);
    private static readonly Color Accent = Color.FromArgb(56, 111, 246);
    private static readonly Color AccentHover = Color.FromArgb(78, 132, 255);
    private static readonly Color AccentSoft = Color.FromArgb(25, 48, 96);
    private static readonly Color TextPrimary = Color.FromArgb(245, 247, 252);
    private static readonly Color TextSecondary = Color.FromArgb(158, 168, 187);
    private static readonly Color TextMuted = Color.FromArgb(103, 115, 136);
    private static readonly Color Success = Color.FromArgb(76, 205, 155);
    private static readonly Color Warning = Color.FromArgb(241, 184, 83);
    private static readonly Color Danger = Color.FromArgb(245, 105, 114);

    private DriveclubSave? _save;

    private readonly TabControl _tabs = new();
    private readonly List<Button> _navButtons = new();
    private readonly Label _pageTitle = new();
    private readonly Label _pageSubtitle = new();
    private readonly Label _fileLabel = new();
    private readonly Label _saveStateBadge = new();
    private readonly Label _statusLabel = new();
    private readonly RichTextBox _overviewBox = new();

    private readonly Label _homeChecksumValue = new();
    private readonly Label _homeRankValue = new();
    private readonly Label _homeFameValue = new();
    private readonly Label _homeVehiclesValue = new();

    private readonly NumericUpDown _driverFame = new();
    private readonly NumericUpDown _targetDriverRank = new();
    private readonly Label _driverRankInfo = new();
    private readonly Label _driverRankSub = new();
    private readonly Label _driverFameInfo = new();
    private readonly ProgressBar _rankProgress = new();
    private bool _loadingFriendly;
    private bool _driverFameDirty;
    private readonly CheckedListBox _features = new();
    private readonly Dictionary<string, string> _featureOriginalRaw = new(StringComparer.Ordinal);
    private readonly ComboBox _recentCar = new();
    private readonly ComboBox _recentBike = new();
    private string? _recentCarOriginal;
    private string? _recentBikeOriginal;

    private readonly TextBox _garageSearch = new();
    private readonly DataGridView _garageGrid = CreateGrid();

    // Customisation / livery editor.
    private readonly ComboBox _customSlot = new();
    private readonly ComboBox _liveryDesign = new();
    private readonly NumericUpDown _raceNumber = new();
    private readonly ComboBox _raceBadge = new();
    private readonly ComboBox _raceFont = new();
    private readonly NumericUpDown[] _paintFinish = [new(), new(), new(), new()];
    private readonly NumericUpDown[] _paintColour = [new(), new(), new(), new()];
    private readonly ComboBox _leftDecal = new();
    private readonly ComboBox _rightDecal = new();
    private readonly ComboBox[] _badgeLayerAsset = [new(), new()];
    private readonly NumericUpDown[] _badgeLayerPaint = [new(), new()];
    private readonly NumericUpDown[] _badgeLayerScale = [new(), new()];
    private readonly Label _customAssetSummary = new();
    private readonly CustomisationProfile?[] _customProfiles = new CustomisationProfile?[2];
    private int _customLoadedIndex = -1;
    private bool _loadingCustomisation;

    // Progression / trophy views.
    private readonly DataGridView _eliteGrid = CreateGrid();
    private readonly DataGridView _progressCounterGrid = CreateGrid();
    private readonly DataGridView _trophyGrid = CreateGrid();

    private readonly DataGridView _statsGrid = CreateGrid();
    private readonly DataGridView _rawStatsGrid = CreateGrid();

    private readonly DataGridView _profileGrid = CreateGrid();
    private readonly DataGridView _profileFloatGrid = CreateGrid();
    private readonly DataGridView _rankGrid = CreateGrid();
    private readonly DataGridView _fameGrid = CreateGrid();
    private readonly DataGridView _storeGrid = CreateGrid();

    // Verified save-side convenience / cheat controls. These only touch fields that were
    // confirmed in the v1.28 profile and matched against the game/UI strings.
    private readonly CheckBox _cheatFgeComplete = new();
    private readonly ComboBox _cheatDifficulty = new();
    private readonly NumericUpDown _cheatOpponents = new();
    private readonly NumericUpDown _cheatLaps = new();
    private readonly ComboBox _cheatOpponentVehicle = new();
    private readonly Dictionary<string, string> _cheatOriginalRaw = new(StringComparer.Ordinal);
    private bool _loadingCheats;
    private bool _cheatFgeDirty;
    private bool _cheatDifficultyDirty;
    private bool _cheatOpponentsDirty;
    private bool _cheatLapsDirty;
    private bool _cheatOpponentVehicleDirty;

    private static readonly (string Title, string Subtitle)[] PageInfo =
    [
        ("Dashboard", "Save health, progression and quick access"),
        ("Player", "Driver Fame, target rank and gameplay features"),
        ("Garage", "Vehicle levels with readable manufacturer and model names"),
        ("Customisation", "Liveries, paint, race numbers, badges and decals"),
        ("Progression", "Elite requirements, accolades and challenge counters"),
        ("Trophy Prep", "Individually edit trophy-related save counters and trigger targets"),
        ("Cheats", "Verified save-side presets and shortcuts without unsafe entitlement edits"),
        ("Statistics", "Readable vehicle, track and gameplay statistics"),
        ("Advanced", "Raw save structures for research and troubleshooting")
    ];

    public MainForm()
    {
        Text = "Driveclub PS4 Save Editor";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1220, 780);
        Size = new Size(1480, 920);
        Font = new Font("Segoe UI", 10F);
        BackColor = AppBack;
        ForeColor = TextPrimary;
        DoubleBuffered = true;

        _tabs.Dock = DockStyle.Fill;
        _tabs.Appearance = TabAppearance.FlatButtons;
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.ItemSize = new Size(0, 1);
        _tabs.Multiline = true;
        _tabs.Padding = new Point(0, 0);
        _tabs.TabStop = false;
        _tabs.TabPages.Add(BuildOverviewTab());
        _tabs.TabPages.Add(BuildPlayerTab());
        _tabs.TabPages.Add(BuildGarageTab());
        _tabs.TabPages.Add(BuildCustomisationTab());
        _tabs.TabPages.Add(BuildProgressionTab());
        _tabs.TabPages.Add(BuildTrophyPrepTab());
        _tabs.TabPages.Add(BuildCheatsTab());
        _tabs.TabPages.Add(BuildStatsTab());
        _tabs.TabPages.Add(BuildAdvancedTab());

        var header = BuildHeader();
        var sidebar = BuildSidebar();
        var pageHeader = BuildPageHeader();

        var content = new Panel { Dock = DockStyle.Fill, BackColor = AppBack, Padding = new Padding(26, 0, 26, 20) };
        content.Controls.Add(_tabs);
        content.Controls.Add(pageHeader);

        var workspace = new Panel { Dock = DockStyle.Fill, BackColor = AppBack };
        workspace.Controls.Add(content);
        workspace.Controls.Add(sidebar);

        var statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = SidebarBack };
        var statusAccent = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = Accent };
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Padding = new Padding(16, 9, 0, 0);
        _statusLabel.ForeColor = TextSecondary;
        _statusLabel.Text = "Ready. Open a decrypted Driveclub profile.sav to begin.";
        statusPanel.Controls.Add(_statusLabel);
        statusPanel.Controls.Add(statusAccent);

        Controls.Add(workspace);
        Controls.Add(statusPanel);
        Controls.Add(header);

        NavigateTo(0);
    }

    private Panel BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Surface, Padding = new Padding(0) };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        var brand = new Panel { Dock = DockStyle.Left, Width = 248, BackColor = SidebarBack };
        var logoBlock = new Panel { Location = new Point(20, 20), Size = new Size(44, 44), BackColor = Accent };
        logoBlock.Paint += (_, e) =>
        {
            using var pen = new Pen(AccentHover);
            e.Graphics.DrawRectangle(pen, 0, 0, logoBlock.Width - 1, logoBlock.Height - 1);
        };
        var logo = new Label
        {
            Text = "DC",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            ForeColor = Color.White
        };
        logoBlock.Controls.Add(logo);
        brand.Controls.Add(logoBlock);
        brand.Controls.Add(new Label
        {
            Text = "DRIVECLUB",
            AutoSize = true,
            Location = new Point(78, 20),
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = TextPrimary
        });
        brand.Controls.Add(new Label
        {
            Text = "PS4 SAVE EDITOR",
            AutoSize = true,
            Location = new Point(79, 47),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = TextSecondary
        });

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 356,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 23, 18, 0),
            BackColor = Surface
        };
        var save = MakeButton("SAVE", true, 86);
        var saveAs = MakeButton("SAVE COPY", false, 108);
        var open = MakeButton("OPEN SAVE", false, 110);
        save.Click += (_, _) => SaveCurrent(false);
        saveAs.Click += (_, _) => SaveCurrent(true);
        open.Click += (_, _) => OpenSave();
        actions.Controls.Add(save);
        actions.Controls.Add(saveAs);
        actions.Controls.Add(open);

        var fileArea = new Panel { Dock = DockStyle.Fill, BackColor = Surface };
        fileArea.Controls.Add(new Label
        {
            Text = "ACTIVE PROFILE",
            AutoSize = true,
            Location = new Point(26, 11),
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
            ForeColor = TextMuted
        });

        _fileLabel.AutoEllipsis = true;
        _fileLabel.AutoSize = false;
        _fileLabel.Location = new Point(26, 28);
        _fileLabel.Size = new Size(560, 22);
        _fileLabel.Text = "No save loaded";
        _fileLabel.ForeColor = TextSecondary;
        _fileLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        fileArea.Controls.Add(_fileLabel);

        _saveStateBadge.AutoSize = false;
        _saveStateBadge.Location = new Point(26, 54);
        _saveStateBadge.Size = new Size(132, 21);
        _saveStateBadge.TextAlign = ContentAlignment.MiddleCenter;
        _saveStateBadge.Font = new Font("Segoe UI", 7.8F, FontStyle.Bold);
        _saveStateBadge.Text = "NO SAVE LOADED";
        _saveStateBadge.BackColor = SurfaceRaised;
        _saveStateBadge.ForeColor = TextSecondary;
        fileArea.Controls.Add(_saveStateBadge);

        header.Controls.Add(fileArea);
        header.Controls.Add(actions);
        header.Controls.Add(brand);
        return header;
    }


    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 248, BackColor = SidebarBack, Padding = new Padding(14, 22, 14, 18) };

        var section = new Label
        {
            Text = "WORKSPACE",
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(12, 0, 0, 0),
            Font = new Font("Segoe UI", 7.8F, FontStyle.Bold),
            ForeColor = TextMuted
        };
        sidebar.Controls.Add(section);

        string[] names = ["Dashboard", "Player", "Garage", "Customisation", "Progression", "Trophy Prep", "Cheats", "Statistics", "Advanced"];
        for (int i = names.Length - 1; i >= 0; i--)
        {
            int index = i;
            string baseText = $"{i + 1:00}   {names[i]}";
            var button = new Button
            {
                Text = "   " + baseText,
                Tag = baseText,
                Dock = DockStyle.Top,
                Height = 52,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = SidebarBack,
                ForeColor = TextSecondary,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = SurfaceRaised;
            button.FlatAppearance.MouseDownBackColor = AccentSoft;
            button.Click += (_, _) => NavigateTo(index);
            _navButtons.Insert(0, button);
            sidebar.Controls.Add(button);
        }

        var info = new Panel { Dock = DockStyle.Bottom, Height = 126, BackColor = Surface, Padding = new Padding(0) };
        info.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);
            e.Graphics.DrawRectangle(pen, 0, 0, info.Width - 1, info.Height - 1);
            using var accentPen = new Pen(Accent, 3F);
            e.Graphics.DrawLine(accentPen, 0, 1, info.Width, 1);
        };
        info.Controls.Add(new Label
        {
            Text = "COMPATIBILITY",
            AutoSize = true,
            Location = new Point(14, 16),
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
            ForeColor = TextMuted
        });
        info.Controls.Add(new Label
        {
            Text = "ALL REGIONS  •  v1.28",
            AutoSize = true,
            Location = new Point(14, 37),
            Font = new Font("Segoe UI", 8.7F, FontStyle.Bold),
            ForeColor = TextPrimary
        });
        info.Controls.Add(new Label
        {
            Text = "DECRYPTED PROFILE.SAV\nAUTO BACKUP  •  CHECKSUM REPAIR",
            AutoSize = true,
            Location = new Point(14, 67),
            Font = new Font("Segoe UI", 7.9F),
            ForeColor = TextSecondary
        });
        sidebar.Controls.Add(info);
        return sidebar;
    }

    private Panel BuildPageHeader()
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 108, BackColor = AppBack };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);
            e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
        };
        panel.Controls.Add(new Label
        {
            Text = "DRIVECLUB SAVE TOOLS",
            AutoSize = true,
            Location = new Point(4, 13),
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
            ForeColor = AccentHover
        });
        _pageTitle.AutoSize = true;
        _pageTitle.Location = new Point(2, 31);
        _pageTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        _pageTitle.ForeColor = TextPrimary;
        _pageSubtitle.AutoSize = true;
        _pageSubtitle.Location = new Point(4, 73);
        _pageSubtitle.Font = new Font("Segoe UI", 9.5F);
        _pageSubtitle.ForeColor = TextSecondary;
        panel.Controls.Add(_pageTitle);
        panel.Controls.Add(_pageSubtitle);
        return panel;
    }

    private void NavigateTo(int index)
    {
        if (index < 0 || index >= _tabs.TabPages.Count) return;
        _tabs.SelectedIndex = index;
        _pageTitle.Text = PageInfo[index].Title;
        _pageSubtitle.Text = PageInfo[index].Subtitle;
        if (index == 5 && _save is not null) PopulateTrophyPrep();

        for (int i = 0; i < _navButtons.Count; i++)
        {
            bool selected = i == index;
            string baseText = Convert.ToString(_navButtons[i].Tag) ?? _navButtons[i].Text.Trim();
            _navButtons[i].Text = selected ? "▌  " + baseText : "    " + baseText;
            _navButtons[i].BackColor = selected ? SurfaceRaised : SidebarBack;
            _navButtons[i].ForeColor = selected ? Color.White : TextSecondary;
            _navButtons[i].FlatAppearance.BorderSize = 0;
        }
    }

    private TabPage BuildOverviewTab()
    {
        var page = NewPage("Dashboard");
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppBack };

        var metrics = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 148,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = AppBack,
            Padding = new Padding(0, 0, 0, 14)
        };
        for (int i = 0; i < 4; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        metrics.Controls.Add(MakeMetricCard("CHECKSUM", _homeChecksumValue, "Save integrity"), 0, 0);
        metrics.Controls.Add(MakeMetricCard("DRIVER RANK", _homeRankValue, "Calculated progression"), 1, 0);
        metrics.Controls.Add(MakeMetricCard("PLAYER FAME", _homeFameValue, "Game-facing Fame"), 2, 0);
        metrics.Controls.Add(MakeMetricCard("VEHICLES", _homeVehiclesValue, "Detected in save"), 3, 0);

        var quick = MakeCard("QUICK ACTIONS", "Common editor tasks", 118);
        quick.Width = 1100;
        var open = MakeButton("OPEN PROFILE.SAV", false, 150);
        var player = MakeButton("EDIT PLAYER", true, 120);
        var garage = MakeButton("OPEN GARAGE", false, 128);
        var save = MakeButton("SAVE CHANGES", false, 130);
        open.Location = new Point(20, 66);
        player.Location = new Point(182, 66);
        garage.Location = new Point(314, 66);
        save.Location = new Point(454, 66);
        open.Click += (_, _) => OpenSave();
        player.Click += (_, _) => NavigateTo(1);
        garage.Click += (_, _) => NavigateTo(2);
        save.Click += (_, _) => SaveCurrent(false);
        quick.Controls.Add(open);
        quick.Controls.Add(player);
        quick.Controls.Add(garage);
        quick.Controls.Add(save);

        var details = MakeCard("SAVE DETAILS", "Verified structures and current profile summary", 330);
        details.Width = 1100;
        _overviewBox.Location = new Point(20, 66);
        _overviewBox.Size = new Size(1000, 238);
        _overviewBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        _overviewBox.ReadOnly = true;
        _overviewBox.BorderStyle = BorderStyle.None;
        _overviewBox.BackColor = SurfaceRaised;
        _overviewBox.ForeColor = TextSecondary;
        _overviewBox.Font = new Font("Cascadia Mono", 10F);
        _overviewBox.Text =
            "No save loaded.\\n\\n" +
            "Open a decrypted Driveclub v1.28 profile.sav from any region.\\n" +
            "The editor creates a backup automatically and regenerates the internal checksum when saving.";
        details.Controls.Add(_overviewBox);

        quick.Dock = DockStyle.Top;
        details.Dock = DockStyle.Top;
        details.Margin = new Padding(0, 14, 0, 0);
        var spacer = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = AppBack };

        scroll.Controls.Add(details);
        scroll.Controls.Add(spacer);
        scroll.Controls.Add(quick);
        scroll.Controls.Add(metrics);
        page.Controls.Add(scroll);
        return page;
    }

    private TabPage BuildPlayerTab()
    {
        var page = NewPage("Player");
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppBack };

        var rankCard = MakeCard("DRIVER PROGRESS", "Real player.csv + Elite progression", 250);
        rankCard.Width = 1000;
        _driverRankInfo.Location = new Point(20, 72);
        _driverRankInfo.Size = new Size(150, 62);
        _driverRankInfo.Font = new Font("Segoe UI", 34F, FontStyle.Bold);
        _driverRankInfo.ForeColor = Color.White;
        _driverRankInfo.Text = "—";
        rankCard.Controls.Add(_driverRankInfo);

        _driverRankSub.Location = new Point(24, 136);
        _driverRankSub.AutoSize = true;
        _driverRankSub.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _driverRankSub.ForeColor = TextSecondary;
        _driverRankSub.Text = "DRIVER RANK  /  120";
        rankCard.Controls.Add(_driverRankSub);

        var fameLabel = MakeFieldLabel("PLAYER FAME", 205, 72);
        rankCard.Controls.Add(fameLabel);
        _driverFame.Location = new Point(205, 96);
        _driverFame.Size = new Size(220, 28);
        _driverFame.Minimum = 0;
        _driverFame.Maximum = 1_000_000_000;
        _driverFame.ThousandsSeparator = true;
        _driverFame.DecimalPlaces = 0;
        StyleNumeric(_driverFame);
        _driverFame.ValueChanged += (_, _) =>
        {
            if (_loadingFriendly) return;
            _driverFameDirty = true;
            UpdateDriverProgressPreview();
        };
        rankCard.Controls.Add(_driverFame);

        rankCard.Controls.Add(MakeFieldLabel("TARGET RANK", 466, 72));
        _targetDriverRank.Location = new Point(466, 96);
        _targetDriverRank.Size = new Size(95, 28);
        _targetDriverRank.Minimum = 1;
        _targetDriverRank.Maximum = 120;
        _targetDriverRank.Value = 120;
        StyleNumeric(_targetDriverRank);
        rankCard.Controls.Add(_targetDriverRank);

        var applyRank = MakeButton("SET FAME", true, 108);
        applyRank.Location = new Point(575, 94);
        applyRank.Click += (_, _) => SetTargetDriverRank((int)_targetDriverRank.Value);
        rankCard.Controls.Add(applyRank);

        var maxRank = MakeButton("MAX RANK 120", false, 130);
        maxRank.Location = new Point(695, 94);
        maxRank.Click += (_, _) => SetTargetDriverRank(120);
        rankCard.Controls.Add(maxRank);

        _rankProgress.Location = new Point(205, 150);
        _rankProgress.Size = new Size(620, 10);
        _rankProgress.Minimum = 0;
        _rankProgress.Maximum = 1000;
        _rankProgress.Style = ProgressBarStyle.Continuous;
        rankCard.Controls.Add(_rankProgress);

        _driverFameInfo.Location = new Point(205, 174);
        _driverFameInfo.AutoSize = true;
        _driverFameInfo.ForeColor = TextSecondary;
        _driverFameInfo.Text = "Load a save to calculate Fame progression.";
        rankCard.Controls.Add(_driverFameInfo);

        var featureCard = MakeCard("GAME FEATURES", "Enable or disable readable profile feature flags", 360);
        featureCard.Width = 1000;
        _features.Location = new Point(20, 70);
        _features.Size = new Size(610, 236);
        _features.BackColor = InputBack;
        _features.ForeColor = TextPrimary;
        _features.BorderStyle = BorderStyle.FixedSingle;
        _features.CheckOnClick = true;
        _features.Font = new Font("Segoe UI", 9.5F);
        foreach (var pair in FriendlyNames.FeatureLabels) _features.Items.Add(pair.Value);
        featureCard.Controls.Add(_features);
        var enableAll = MakeButton("ENABLE ALL FEATURES", false, 170);
        enableAll.Location = new Point(650, 70);
        enableAll.Click += (_, _) =>
        {
            for (int i = 0; i < _features.Items.Count; i++) _features.SetItemChecked(i, true);
        };
        featureCard.Controls.Add(enableAll);

        var recentCard = MakeCard("RECENT VEHICLE", "Choose the car and bike used by the profile", 176);
        recentCard.Width = 1000;
        recentCard.Controls.Add(MakeFieldLabel("RECENT CAR", 20, 72));
        recentCard.Controls.Add(MakeFieldLabel("RECENT BIKE", 470, 72));
        ConfigureCombo(_recentCar, new Point(20, 98), 410);
        ConfigureCombo(_recentBike, new Point(470, 98), 410);
        recentCard.Controls.Add(_recentCar);
        recentCard.Controls.Add(_recentBike);

        rankCard.Dock = DockStyle.Top;
        featureCard.Dock = DockStyle.Top;
        recentCard.Dock = DockStyle.Top;
        var gap1 = MakeGap(14);
        var gap2 = MakeGap(14);
        scroll.Controls.Add(recentCard);
        scroll.Controls.Add(gap2);
        scroll.Controls.Add(featureCard);
        scroll.Controls.Add(gap1);
        scroll.Controls.Add(rankCard);
        page.Controls.Add(scroll);
        return page;
    }

    private TabPage BuildGarageTab()
    {
        var page = NewPage("Garage");
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = SurfaceRaised, Padding = new Padding(16) };
        toolbar.Controls.Add(MakeFieldLabel("SEARCH VEHICLES", 16, 13));
        _garageSearch.Location = new Point(16, 36);
        _garageSearch.Size = new Size(310, 28);
        StyleTextBox(_garageSearch);
        _garageSearch.PlaceholderText = "Manufacturer, model or class...";
        _garageSearch.TextChanged += (_, _) => ApplyGarageFilter();
        toolbar.Controls.Add(_garageSearch);

        var maxAll = MakeButton("MAX ALL LEVELS", true, 138);
        maxAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        maxAll.Location = new Point(810, 24);
        maxAll.Click += (_, _) => SetAllGarageLevels(15);
        toolbar.Controls.Add(maxAll);
        var maxSelected = MakeButton("MAX SELECTED", false, 135);
        maxSelected.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        maxSelected.Location = new Point(665, 24);
        maxSelected.Click += (_, _) => SetSelectedGarageLevel(15);
        toolbar.Controls.Add(maxSelected);
        toolbar.Resize += (_, _) =>
        {
            maxAll.Left = toolbar.ClientSize.Width - maxAll.Width - 16;
            maxSelected.Left = maxAll.Left - maxSelected.Width - 10;
        };

        var note = MakeNote("Vehicle names are translated from internal IDs. Untouched vehicle Fame is preserved exactly; only a level you explicitly change is rewritten to Driveclub's verified progression threshold.", 58);

        _garageGrid.Columns.Add("Vehicle", "VEHICLE");
        _garageGrid.Columns.Add("Group", "CLASS / TYPE");
        _garageGrid.Columns.Add("Level", "LEVEL 1–15");
        _garageGrid.Columns.Add("Fame", "VEHICLE FAME / PROGRESS");
        _garageGrid.Columns.Add("Code", "Internal ID");
        _garageGrid.Columns[0].ReadOnly = true;
        _garageGrid.Columns[1].ReadOnly = true;
        _garageGrid.Columns[3].ReadOnly = true;
        _garageGrid.Columns[4].ReadOnly = true;
        _garageGrid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _garageGrid.Columns[1].Width = 180;
        _garageGrid.Columns[2].Width = 130;
        _garageGrid.Columns[3].Width = 220;
        _garageGrid.Columns[4].Visible = false;
        _garageGrid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 2)
                UpdateGarageFamePreview(_garageGrid.Rows[e.RowIndex]);
        };

        page.Controls.Add(_garageGrid);
        page.Controls.Add(note);
        page.Controls.Add(toolbar);
        return page;
    }

    private TabPage BuildCustomisationTab()
    {
        var page = NewPage("Customisation");
        var note = MakeNote("Edits the confirmed CustomisationS1/S2 text stored in profile.sav. Designs, badges, fonts and decal names come from the extracted Driveclub livery editor assets. Unknown customisation lines are preserved.", 62);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppBack, Padding = new Padding(0, 14, 0, 20) };

        var slotCard = MakeCard("CUSTOMISATION SLOT", "Edit only the saved slot you choose", 128);
        slotCard.Width = 1020;
        slotCard.Controls.Add(MakeFieldLabel("SAVED SLOT", 20, 70));
        ConfigureCombo(_customSlot, new Point(20, 92), 330);
        _customSlot.Items.AddRange(new object[] { "Customisation Slot 1", "Customisation Slot 2" });
        _customSlot.SelectedIndexChanged += (_, _) => SwitchCustomisationSlot();
        slotCard.Controls.Add(_customSlot);
        _customAssetSummary.AutoSize = true;
        _customAssetSummary.Location = new Point(390, 96);
        _customAssetSummary.ForeColor = TextSecondary;
        _customAssetSummary.Text = "555 designs • 17 race badges • 20 number fonts • 821 stickers";
        slotCard.Controls.Add(_customAssetSummary);
        var markSeen = MakeButton("MARK ALL SEEN", false, 130);
        markSeen.Location = new Point(860, 86);
        markSeen.Click += (_, _) => MarkAllCustomisationSeen();
        slotCard.Controls.Add(markSeen);

        var designCard = MakeCard("LIVERY & RACE IDENTITY", "Saved design, race badge, number and number font", 218);
        designCard.Width = 1020;
        designCard.Controls.Add(MakeFieldLabel("LIVERY DESIGN", 20, 70));
        ConfigureAssetCombo(_liveryDesign, LiveryAssets.Designs, new Point(20, 94), 430);
        designCard.Controls.Add(_liveryDesign);
        designCard.Controls.Add(MakeFieldLabel("RACE NUMBER", 480, 70));
        ConfigureNumeric(_raceNumber, 0, 999, new Point(480, 94), 120);
        designCard.Controls.Add(_raceNumber);
        designCard.Controls.Add(MakeFieldLabel("RACE BADGE", 20, 138));
        ConfigureAssetCombo(_raceBadge, LiveryAssets.RaceBadges, new Point(20, 162), 430, includeNone: true);
        designCard.Controls.Add(_raceBadge);
        designCard.Controls.Add(MakeFieldLabel("NUMBER FONT", 480, 138));
        ConfigureAssetCombo(_raceFont, LiveryAssets.RaceFonts, new Point(480, 162), 330, includeNone: true);
        designCard.Controls.Add(_raceFont);

        var paintCard = MakeCard("PAINT", "Four saved paint channels. Finish is kept as the game's numeric finish code; colour is the saved palette index.", 272);
        paintCard.Width = 1020;
        for (int i = 0; i < 4; i++)
        {
            int y = 72 + i * 45;
            paintCard.Controls.Add(MakeFieldLabel($"PAINT {i + 1}", 20, y + 5));
            paintCard.Controls.Add(MakeFieldLabel("FINISH CODE", 160, y + 5));
            ConfigureNumeric(_paintFinish[i], 0, 15, new Point(255, y), 100);
            paintCard.Controls.Add(_paintFinish[i]);
            paintCard.Controls.Add(MakeFieldLabel("COLOUR INDEX", 390, y + 5));
            ConfigureNumeric(_paintColour[i], 0, 999, new Point(495, y), 120);
            paintCard.Controls.Add(_paintColour[i]);
        }

        var decalCard = MakeCard("DECALS", "Left and right saved decal slots. Changing these is more experimental than design/paint editing.", 182);
        decalCard.Width = 1020;
        decalCard.Controls.Add(MakeFieldLabel("LEFT DECAL", 20, 72));
        ConfigureAssetCombo(_leftDecal, LiveryAssets.StickerAssets, new Point(20, 96), 440);
        decalCard.Controls.Add(_leftDecal);
        decalCard.Controls.Add(MakeFieldLabel("RIGHT DECAL", 500, 72));
        ConfigureAssetCombo(_rightDecal, LiveryAssets.StickerAssets, new Point(500, 96), 440);
        decalCard.Controls.Add(_rightDecal);
        var clearDecals = MakeButton("CLEAR DECALS", false, 130);
        clearDecals.Location = new Point(20, 136);
        clearDecals.Click += (_, _) =>
        {
            SelectAsset(_leftDecal, "off");
            SelectAsset(_rightDecal, "off");
        };
        decalCard.Controls.Add(clearDecals);

        var badgeCard = MakeCard("CLUB BADGE LAYERS", "The save stores badge assets as layers. Shield, flourish and symbol assets are shown by readable category/name.", 250);
        badgeCard.Width = 1020;
        for (int i = 0; i < 2; i++)
        {
            int y = 76 + i * 74;
            badgeCard.Controls.Add(MakeFieldLabel($"LAYER {i + 1}", 20, y + 4));
            ConfigureAssetCombo(_badgeLayerAsset[i], LiveryAssets.ClubBadgeAssets, new Point(105, y), 470);
            badgeCard.Controls.Add(_badgeLayerAsset[i]);
            badgeCard.Controls.Add(MakeFieldLabel("PAINT", 605, y + 4));
            ConfigureNumeric(_badgeLayerPaint[i], 0, 999, new Point(655, y), 90);
            badgeCard.Controls.Add(_badgeLayerPaint[i]);
            badgeCard.Controls.Add(MakeFieldLabel("SCALE", 775, y + 4));
            ConfigureNumeric(_badgeLayerScale[i], 0, 10, new Point(825, y), 100, 3, 0.001m);
            badgeCard.Controls.Add(_badgeLayerScale[i]);
        }

        var safeNote = MakeNote("Confirmed save fields: livery design, four paint values, race number, race badge, number font and badge-layer records. Decal replacement is marked experimental because the slot exists in the save but every sticker/vehicle combination has not been validated.", 70, Warning);
        safeNote.Width = 1020;
        safeNote.Dock = DockStyle.Top;
        slotCard.Dock = designCard.Dock = paintCard.Dock = decalCard.Dock = badgeCard.Dock = DockStyle.Top;
        scroll.Controls.Add(safeNote);
        scroll.Controls.Add(MakeGap(14));
        scroll.Controls.Add(badgeCard);
        scroll.Controls.Add(MakeGap(14));
        scroll.Controls.Add(decalCard);
        scroll.Controls.Add(MakeGap(14));
        scroll.Controls.Add(paintCard);
        scroll.Controls.Add(MakeGap(14));
        scroll.Controls.Add(designCard);
        scroll.Controls.Add(MakeGap(14));
        scroll.Controls.Add(slotCard);

        page.Controls.Add(scroll);
        page.Controls.Add(note);
        return page;
    }

    private TabPage BuildProgressionTab()
    {
        var page = NewPage("Progression");
        var note = MakeNote("Elite requirements are read from the real embedded elite.csv. The editor can set the Fame requirement, but it does not pretend to write unverified per-event Tour star records. Accolade/challenge counters below are the real named counters stored in profile.sav.", 68, Warning);
        var inner = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Padding = new Point(12, 6) };

        var elitePage = NewPage("Elite Progression");
        var eliteToolbar = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = SurfaceRaised };
        var setFame = MakeButton("SET SELECTED FAME", true, 160);
        setFame.Location = new Point(16, 12);
        setFame.Click += (_, _) => SetSelectedEliteFame();
        eliteToolbar.Controls.Add(setFame);
        var eliteInfo = new Label
        {
            AutoSize = true,
            Location = new Point(195, 20),
            ForeColor = TextSecondary,
            Text = "Tour stars and target time remain visible requirements; only the confirmed Fame value is changed."
        };
        eliteToolbar.Controls.Add(eliteInfo);
        _eliteGrid.ReadOnly = true;
        _eliteGrid.Columns.Add("Level", "ELITE / DRIVER RANK");
        _eliteGrid.Columns.Add("Fame", "FAME REQUIRED");
        _eliteGrid.Columns.Add("Stars", "TOUR STARS");
        _eliteGrid.Columns.Add("Time", "TARGET TIME");
        _eliteGrid.Columns.Add("Track", "TRACK");
        _eliteGrid.Columns.Add("Vehicle", "VEHICLE");
        _eliteGrid.Columns[0].Width = 150;
        _eliteGrid.Columns[1].Width = 165;
        _eliteGrid.Columns[2].Width = 110;
        _eliteGrid.Columns[3].Width = 125;
        _eliteGrid.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _eliteGrid.Columns[5].Width = 280;
        elitePage.Controls.Add(_eliteGrid);
        elitePage.Controls.Add(eliteToolbar);
        inner.TabPages.Add(elitePage);

        var counterPage = NewPage("Accolades & Challenges");
        var counterNote = MakeNote("Values are editable, but only named fields actually present in your PlayerProfile are shown here. The original internal key stays hidden in the normal view.", 54);
        _progressCounterGrid.Columns.Add("Category", "CATEGORY");
        _progressCounterGrid.Columns.Add("Name", "COUNTER");
        _progressCounterGrid.Columns.Add("Value", "VALUE");
        _progressCounterGrid.Columns.Add("Key", "INTERNAL KEY");
        _progressCounterGrid.Columns[0].ReadOnly = true;
        _progressCounterGrid.Columns[1].ReadOnly = true;
        _progressCounterGrid.Columns[3].ReadOnly = true;
        _progressCounterGrid.Columns[0].Width = 160;
        _progressCounterGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _progressCounterGrid.Columns[2].Width = 180;
        _progressCounterGrid.Columns[3].Visible = false;
        counterPage.Controls.Add(_progressCounterGrid);
        counterPage.Controls.Add(counterNote);
        inner.TabPages.Add(counterPage);

        page.Controls.Add(inner);
        page.Controls.Add(note);
        return page;
    }

    private TabPage BuildTrophyPrepTab()
    {
        var page = NewPage("Trophy Prep");
        var note = MakeNote("Every mapped trophy-related save counter can now be edited individually. Select a row, type any value in EDIT VALUE, then click APPLY VALUE. SET 1 BELOW / SET REQUIREMENT are enabled only for counters whose numeric requirement is verified. Raw-unit counters remain custom-editable without pretending their in-game unit is proven.", 78, Warning);
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 102,
            BackColor = SurfaceRaised,
            Padding = new Padding(14, 14, 14, 10),
            WrapContents = true
        };

        var apply = MakeButton("APPLY VALUE", true, 128);
        apply.Click += (_, _) => ApplySelectedTrophyValue();
        var below = MakeButton("SET 1 BELOW", false, 122);
        below.Click += (_, _) => SetSelectedTrophyRequirement(oneBelow: true);
        var requirement = MakeButton("SET REQUIREMENT", false, 142);
        requirement.Click += (_, _) => SetSelectedTrophyRequirement(oneBelow: false);
        var restore = MakeButton("RESTORE ORIGINAL", false, 148);
        restore.Click += (_, _) => RestoreSelectedTrophyValue();
        var fullHouse = MakeButton("PREP FULL HOUSE", false, 145);
        fullHouse.Click += (_, _) => PrepFullHouse();
        var challenge4 = MakeButton("SOLO WINS: 4", false, 120);
        challenge4.Click += (_, _) => PrepSoloChallengeWins(4);
        var challenge5 = MakeButton("SOLO WINS: 5", false, 120);
        challenge5.Click += (_, _) => PrepSoloChallengeWins(5);
        toolbar.Controls.AddRange(new Control[] { apply, below, requirement, restore, fullHouse, challenge4, challenge5 });

        _trophyGrid.ReadOnly = false;
        _trophyGrid.EditMode = DataGridViewEditMode.EditOnEnter;
        _trophyGrid.Columns.Add("Trophy", "TROPHY / GOAL");
        _trophyGrid.Columns.Add("Field", "SAVE FIELD");
        _trophyGrid.Columns.Add("Requirement", "REQUIREMENT");
        _trophyGrid.Columns.Add("Current", "CURRENT");
        _trophyGrid.Columns.Add("EditValue", "EDIT VALUE");
        _trophyGrid.Columns.Add("Status", "STATUS");
        _trophyGrid.Columns[0].Width = 220;
        _trophyGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _trophyGrid.Columns[2].Width = 190;
        _trophyGrid.Columns[3].Width = 145;
        _trophyGrid.Columns[4].Width = 150;
        _trophyGrid.Columns[5].Width = 130;
        for (int i = 0; i < _trophyGrid.Columns.Count; i++)
            _trophyGrid.Columns[i].ReadOnly = i != 4;
        _trophyGrid.Columns[4].DefaultCellStyle.BackColor = InputBack;
        _trophyGrid.Columns[4].DefaultCellStyle.ForeColor = TextPrimary;
        _trophyGrid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex != 4)
            {
                _trophyGrid.CurrentCell = _trophyGrid.Rows[e.RowIndex].Cells[4];
                _trophyGrid.BeginEdit(true);
            }
        };

        page.Controls.Add(_trophyGrid);
        page.Controls.Add(toolbar);
        page.Controls.Add(note);
        return page;
    }

    private TabPage BuildCheatsTab()
    {
        var page = NewPage("Cheats");
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppBack, Padding = new Padding(0, 14, 0, 20) };

        var verified = MakeNote(
            "Deep review result: profile.sav contains progression shortcuts and saved Free Play preferences, but Auto Drive, invulnerability, no-collision and similar driving cheats are runtime/EBOOT features. This page intentionally avoids commerce/entitlement and uncertain Tour-completion fields.",
            72, Warning);
        verified.Dock = DockStyle.Top;

        var quick = MakeCard("SAFE SAVE SHORTCUTS", "Uses the editor's already verified Fame, Garage and feature mappings", 190);
        quick.Width = 1040;
        var maxRank = MakeButton("MAX DRIVER RANK", true, 158);
        maxRank.Location = new Point(20, 76);
        maxRank.Click += (_, _) =>
        {
            SetTargetDriverRank(120);
            SetStatus("Cheat preset: Driver Rank target set to 120. Save to write the Fame change.");
        };
        quick.Controls.Add(maxRank);

        var maxVehicles = MakeButton("MAX ALL VEHICLES", false, 158);
        maxVehicles.Location = new Point(190, 76);
        maxVehicles.Click += (_, _) =>
        {
            SetAllGarageLevels(15);
            SetStatus("Cheat preset: all detected vehicle levels set to 15. Save to write changed vehicles.");
        };
        quick.Controls.Add(maxVehicles);

        var allFeatures = MakeButton("ENABLE ALL FEATURES", false, 168);
        allFeatures.Location = new Point(360, 76);
        allFeatures.Click += (_, _) =>
        {
            for (int i = 0; i < _features.Items.Count; i++)
                _features.SetItemChecked(i, true);
            SetStatus("Cheat preset: all mapped profile feature flags enabled. Save to apply.");
        };
        quick.Controls.Add(allFeatures);

        var tourEasy = MakeButton("TOUR EASY", false, 124);
        tourEasy.Location = new Point(540, 76);
        tourEasy.Click += (_, _) =>
        {
            SetFeatureChecked("toureasy", true);
            SetStatus("Tour Easy profile flag enabled. Save to apply.");
        };
        quick.Controls.Add(tourEasy);

        var trophy = MakeButton("TROPHY PREP", false, 132);
        trophy.Location = new Point(676, 76);
        trophy.Click += (_, _) => NavigateTo(5);
        quick.Controls.Add(trophy);

        quick.Controls.Add(new Label
        {
            Text = "These shortcuts change the same verified fields as Player, Garage and Trophy Prep; they do not bypass the save integrity checks.",
            AutoSize = false,
            Location = new Point(22, 128),
            Size = new Size(940, 34),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9F)
        });

        var fge = MakeCard("FIRST GAME EXPERIENCE", "Confirmed PlayerProfile FGEComplete getter/setter exists in the v1.28 EBOOT", 170);
        fge.Width = 1040;
        _cheatFgeComplete.Location = new Point(22, 76);
        _cheatFgeComplete.AutoSize = true;
        _cheatFgeComplete.Text = "Mark FGEComplete = true (skip first-time experience/tutorial gates)";
        _cheatFgeComplete.ForeColor = TextPrimary;
        _cheatFgeComplete.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _cheatFgeComplete.CheckedChanged += (_, _) =>
        {
            if (!_loadingCheats) _cheatFgeDirty = true;
        };
        fge.Controls.Add(_cheatFgeComplete);
        fge.Controls.Add(new Label
        {
            Text = "This flag is save-side. It is not Auto Drive and does not modify driving physics.",
            AutoSize = true,
            Location = new Point(24, 112),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8.8F)
        });

        var race = MakeCard("FREE PLAY PRESET", "Verified values from the game's Free Play combobox definitions", 280);
        race.Width = 1040;
        race.Controls.Add(MakeFieldLabel("AI DIFFICULTY", 20, 76));
        ConfigureCombo(_cheatDifficulty, new Point(20, 101), 220);
        _cheatDifficulty.Items.AddRange(new object[] { "Rookie", "Amateur", "Semi-Pro", "Professional", "Legend" });
        _cheatDifficulty.SelectedIndexChanged += (_, _) =>
        {
            if (!_loadingCheats) _cheatDifficultyDirty = true;
        };
        race.Controls.Add(_cheatDifficulty);

        race.Controls.Add(MakeFieldLabel("OPPONENTS", 264, 76));
        ConfigureNumeric(_cheatOpponents, 0, 11, new Point(264, 101), 140);
        _cheatOpponents.ValueChanged += (_, _) =>
        {
            if (!_loadingCheats) _cheatOpponentsDirty = true;
        };
        race.Controls.Add(_cheatOpponents);
        race.Controls.Add(new Label
        {
            Text = "0 = Random, 1–11 = competitor count",
            AutoSize = true,
            Location = new Point(264, 136),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8.2F)
        });

        race.Controls.Add(MakeFieldLabel("LAPS", 430, 76));
        ConfigureNumeric(_cheatLaps, 0, 25, new Point(430, 101), 120);
        _cheatLaps.ValueChanged += (_, _) =>
        {
            if (!_loadingCheats) _cheatLapsDirty = true;
        };
        race.Controls.Add(_cheatLaps);
        race.Controls.Add(new Label
        {
            Text = "0 = Random, 1–25",
            AutoSize = true,
            Location = new Point(430, 136),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8.2F)
        });

        race.Controls.Add(MakeFieldLabel("OPPONENT VEHICLES", 576, 76));
        ConfigureCombo(_cheatOpponentVehicle, new Point(576, 101), 220);
        _cheatOpponentVehicle.Items.AddRange(new object[] { "Player Class", "Player Vehicle" });
        _cheatOpponentVehicle.SelectedIndexChanged += (_, _) =>
        {
            if (!_loadingCheats) _cheatOpponentVehicleDirty = true;
        };
        race.Controls.Add(_cheatOpponentVehicle);

        var easy = MakeButton("APPLY EASY RACE", true, 158);
        easy.Location = new Point(20, 188);
        easy.Click += (_, _) =>
        {
            _cheatDifficulty.SelectedIndex = 0;
            _cheatOpponents.Value = 1;
            _cheatLaps.Value = 1;
            _cheatOpponentVehicle.SelectedIndex = 0;
            SetStatus("Easy Free Play preset selected: Rookie, 1 opponent, 1 lap, Player Class. Save to apply.");
        };
        race.Controls.Add(easy);
        race.Controls.Add(new Label
        {
            Text = "Note: these are normal Free Play menu values persisted in profile.sav. 0 opponents does NOT mean no opponents; value 0 is Random.",
            AutoSize = false,
            Location = new Point(194, 189),
            Size = new Size(790, 48),
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8.8F)
        });

        var unsafeCard = MakeCard("NOT EXPOSED AS CHEATS", "Fields found in the save but deliberately left alone", 190);
        unsafeCard.Width = 1040;
        unsafeCard.Controls.Add(new Label
        {
            Text = "• InPlus_*  — commerce / entitlement state (not treated as DLC ownership)\n" +
                   "• SFV*      — customisation/livery persistence\n" +
                   "• tourcompleted* — Tour/star completion bookkeeping\n" +
                   "• AutoPilot / Toggle AI control — runtime EBOOT feature, not profile.sav",
            AutoSize = false,
            Location = new Point(22, 72),
            Size = new Size(930, 96),
            ForeColor = TextSecondary,
            Font = new Font("Consolas", 9.2F)
        });

        quick.Dock = DockStyle.Top;
        fge.Dock = DockStyle.Top;
        race.Dock = DockStyle.Top;
        unsafeCard.Dock = DockStyle.Top;
        scroll.Controls.Add(unsafeCard);
        scroll.Controls.Add(MakeGap(14));
        scroll.Controls.Add(race);
        scroll.Controls.Add(MakeGap(14));
        scroll.Controls.Add(fge);
        scroll.Controls.Add(MakeGap(14));
        scroll.Controls.Add(quick);
        page.Controls.Add(scroll);
        page.Controls.Add(verified);
        return page;
    }

    private TabPage BuildStatsTab()
    {
        var page = NewPage("Statistics");
        var note = MakeNote("Readable statistics view. Vehicle and track IDs are translated to their Driveclub names. Detailed raw fields remain available in Advanced.", 56);

        _statsGrid.ReadOnly = true;
        _statsGrid.Columns.Add("Category", "CATEGORY");
        _statsGrid.Columns.Add("Name", "NAME");
        _statsGrid.Columns.Add("Primary", "PRIMARY VALUE");
        _statsGrid.Columns.Add("Count", "COUNT / USES");
        _statsGrid.Columns[0].Width = 140;
        _statsGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _statsGrid.Columns[2].Width = 190;
        _statsGrid.Columns[3].Width = 170;

        page.Controls.Add(_statsGrid);
        page.Controls.Add(note);
        return page;
    }

    private TabPage BuildAdvancedTab()
    {
        var page = NewPage("Advanced");
        var warning = MakeNote("Advanced mode exposes raw save structures. Normal Player and Garage editing is safer and easier to read.", 52, Warning);
        var inner = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Padding = new Point(12, 6)
        };

        var profilePage = NewPage("Profile Variables");
        _profileGrid.Columns.Add("Name", "INTERNAL VARIABLE");
        _profileGrid.Columns.Add("Value", "VALUE");
        _profileGrid.Columns[0].ReadOnly = true;
        _profileGrid.Columns[0].Width = 340;
        _profileGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        profilePage.Controls.Add(_profileGrid);
        inner.TabPages.Add(profilePage);

        var floatPage = NewPage("Profile Counters");
        _profileFloatGrid.Columns.Add("Name", "COUNTER");
        _profileFloatGrid.Columns.Add("Value", "VALUE");
        _profileFloatGrid.Columns[0].ReadOnly = true;
        _profileFloatGrid.Columns[0].Width = 430;
        _profileFloatGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        floatPage.Controls.Add(_profileFloatGrid);
        inner.TabPages.Add(floatPage);

        var rawStatsPage = NewPage("Stats Raw");
        _rawStatsGrid.Columns.Add("Name", "INTERNAL NAME");
        _rawStatsGrid.Columns.Add("Kind", "KIND");
        _rawStatsGrid.Columns.Add("V1", "VALUE 1");
        _rawStatsGrid.Columns.Add("V2", "VALUE 2");
        _rawStatsGrid.Columns.Add("V3", "VALUE 3");
        _rawStatsGrid.Columns.Add("V4", "VALUE 4");
        _rawStatsGrid.Columns.Add("V5", "VALUE 5");
        _rawStatsGrid.Columns[0].ReadOnly = true;
        _rawStatsGrid.Columns[1].ReadOnly = true;
        _rawStatsGrid.Columns[0].Width = 310;
        _rawStatsGrid.Columns[1].Width = 70;
        for (int i = 2; i < 7; i++) _rawStatsGrid.Columns[i].Width = 145;
        rawStatsPage.Controls.Add(_rawStatsGrid);
        inner.TabPages.Add(rawStatsPage);

        var rankPage = NewPage("Rank / Fame Raw");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260, BackColor = Border };
        ConfigureScalarGrid(_rankGrid, "Rank component");
        ConfigureScalarGrid(_fameGrid, "Fame component");
        split.Panel1.Controls.Add(_rankGrid);
        split.Panel2.Controls.Add(_fameGrid);
        rankPage.Controls.Add(split);
        inner.TabPages.Add(rankPage);

        var storePage = NewPage("Store Catalog Raw");
        _storeGrid.Columns.Add("Index", "INDEX");
        _storeGrid.Columns.Add("Value", "VALUE");
        _storeGrid.Columns[0].ReadOnly = true;
        _storeGrid.Columns[0].Width = 120;
        _storeGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        storePage.Controls.Add(_storeGrid);
        inner.TabPages.Add(storePage);

        page.Controls.Add(inner);
        page.Controls.Add(warning);
        return page;
    }

    private static void ConfigureScalarGrid(DataGridView grid, string label)
    {
        grid.Columns.Add("Name", label + " field");
        grid.Columns.Add("Type", "Type");
        grid.Columns.Add("Value", "Value");
        grid.Columns[0].ReadOnly = true;
        grid.Columns[1].ReadOnly = true;
        grid.Columns[0].Width = 220;
        grid.Columns[1].Width = 120;
        grid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }

    private static void ConfigureCombo(ComboBox combo, Point location, int width)
    {
        combo.Location = location;
        combo.Width = width;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.BackColor = InputBack;
        combo.ForeColor = TextPrimary;
        combo.FlatStyle = FlatStyle.Flat;
        combo.Font = new Font("Segoe UI", 9.5F);
        combo.MinimumSize = new Size(0, 30);
    }

    private static void ConfigureAssetCombo(ComboBox combo, IEnumerable<LiveryAssets.Choice> choices, Point location, int width, bool includeNone = false)
    {
        ConfigureCombo(combo, location, width);
        combo.MaxDropDownItems = 18;
        if (includeNone)
            combo.Items.Add(new LiveryAssets.Choice("off", "None"));
        foreach (var choice in choices)
            combo.Items.Add(choice);
    }

    private static void ConfigureNumeric(NumericUpDown numeric, decimal min, decimal max, Point location, int width, int decimals = 0, decimal increment = 1m)
    {
        numeric.Minimum = min;
        numeric.Maximum = max;
        numeric.DecimalPlaces = decimals;
        numeric.Increment = increment;
        numeric.Location = location;
        numeric.Width = width;
        StyleNumeric(numeric);
    }

    private static void SelectAsset(ComboBox combo, string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) id = "off";
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is LiveryAssets.Choice c && string.Equals(c.Id, id, StringComparison.Ordinal))
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        var unknown = new LiveryAssets.Choice(id, LiveryAssets.FriendlyAssetName(id) + "  (saved)");
        combo.Items.Insert(0, unknown);
        combo.SelectedIndex = 0;
    }

    private static string SelectedAssetId(ComboBox combo, string fallback) =>
        combo.SelectedItem is LiveryAssets.Choice c ? c.Id : fallback;

    private void OpenSave()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open Driveclub profile.sav",
            Filter = "Driveclub profile.sav|profile.sav|Save files (*.sav)|*.sav|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _save = DriveclubSave.Load(dialog.FileName);
            CaptureTrophyOriginalValues();
            PopulateAll();
            _fileLabel.Text = $"{Path.GetFileName(dialog.FileName)}  •  {new FileInfo(dialog.FileName).Length:N0} bytes";
            UpdateSaveBadge();
            if (!_save.ChecksumValid)
            {
                MessageBox.Show(this,
                    "This profile.sav has an invalid Driveclub checksum. The component layout parsed successfully, and saving can regenerate the checksum, but the file may already have been modified or corrupted. Keep the original and its backup until the edited save has been tested in-game.",
                    "Checksum warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SetStatus("Save loaded with INVALID checksum. Review carefully before saving a repaired copy.");
            }
            else
            {
                SetStatus("Save loaded. Friendly editor controls are ready.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Open failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Open failed.");
        }
    }

    private void PopulateAll()
    {
        if (_save is null) return;

        PopulatePlayer();
        PopulateGarage();
        PopulateCustomisation();
        PopulateProgression();
        PopulateTrophyPrep();
        PopulateCheats();
        PopulateStatistics();
        PopulateAdvanced();

        long fame = Math.Max(0, _save.EffectivePlayerFame);
        int rank = ProgressionData.DriverRankForFame((ulong)fame);
        int vehicles = _save.StatsStoreEntries.Count(x => x.Kind == 0);
        int tracks = _save.StatsStoreEntries.Count(x => x.Kind == 1);

        _homeChecksumValue.Text = _save.ChecksumValid ? "VALID" : "INVALID";
        _homeChecksumValue.ForeColor = _save.ChecksumValid ? Success : Danger;
        _homeRankValue.Text = $"{rank} / 120";
        _homeFameValue.Text = fame.ToString("N0", CultureInfo.InvariantCulture);
        _homeVehiclesValue.Text = vehicles.ToString("N0", CultureInfo.InvariantCulture);

        _overviewBox.Text =
            $"SAVE VERSION     {_save.SaveVersion}\n" +
            $"CHECKSUM         {(_save.ChecksumValid ? "VALID" : "INVALID")}\n" +
            $"PLAYER FAME      {fame:N0}\n" +
            $"DRIVER RANK      {rank} / 120\n" +
            $"VEHICLES         {vehicles:N0}\n" +
            $"TRACKS           {tracks:N0}\n\n" +
            "Use Player for Fame and Driver Rank targets, Garage for vehicle levels, and Customisation for the saved livery slots.\n" +
            "Progression shows verified Elite requirements and named counters; Trophy Prep provides conservative save-side presets. Cheats groups only verified save-side shortcuts and explicitly avoids commerce/entitlement fields. Advanced keeps raw structures out of the normal workflow.";

        UpdateSaveBadge();
    }

    private void PopulatePlayer()
    {
        if (_save is null) return;

        _loadingFriendly = true;
        try
        {
            long effectiveFame = Math.Max(0, _save.EffectivePlayerFame);
            _driverFame.Value = Math.Min((decimal)effectiveFame, _driverFame.Maximum);
            _driverFameDirty = false;
            UpdateDriverProgressPreview();
        }
        finally
        {
            _loadingFriendly = false;
        }

        _featureOriginalRaw.Clear();
        int featureIndex = 0;
        foreach (var pair in FriendlyNames.FeatureLabels)
        {
            var entry = _save.FindString(pair.Key);
            string raw = entry?.Value ?? string.Empty;
            _featureOriginalRaw[pair.Key] = raw;
            bool enabled = entry is not null && IsTrue(raw);
            _features.SetItemChecked(featureIndex++, enabled);
        }

        var vehicleEntries = _save.StatsStoreEntries.Where(x => x.Kind == 0).Select(x => x.Name).Distinct().ToList();
        var cars = vehicleEntries.Select(code => (code, info: FriendlyNames.GetVehicle(code))).Where(x => !x.info.IsBike).OrderBy(x => x.info.Name).ToList();
        var bikes = vehicleEntries.Select(code => (code, info: FriendlyNames.GetVehicle(code))).Where(x => x.info.IsBike).OrderBy(x => x.info.Name).ToList();

        _recentCar.Items.Clear();
        foreach (var x in cars) _recentCar.Items.Add(new VehicleChoice(x.code, x.info.Name));
        _recentBike.Items.Clear();
        foreach (var x in bikes) _recentBike.Items.Add(new VehicleChoice(x.code, x.info.Name));

        _recentCarOriginal = _save.FindString("recentvehicleDC_CARS")?.Value;
        _recentBikeOriginal = _save.FindString("recentvehicleDC_BIKES")?.Value;
        SelectVehicle(_recentCar, _recentCarOriginal);
        SelectVehicle(_recentBike, _recentBikeOriginal);
    }

    private void PopulateCustomisation()
    {
        if (_save is null) return;
        _loadingCustomisation = true;
        try
        {
            _customProfiles[0] = new CustomisationProfile(_save.FindString("CustomisationS1")?.Value ?? string.Empty);
            _customProfiles[1] = new CustomisationProfile(_save.FindString("CustomisationS2")?.Value ?? string.Empty);
            _customLoadedIndex = -1;
            _customSlot.SelectedIndex = 0;
            _customLoadedIndex = 0;
            LoadCustomisationControls(0);

            string assetNewness = _save.FindString("AssetNewness")?.Value ?? string.Empty;
            int newnessItems = assetNewness.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            _customAssetSummary.Text = $"555 designs • 17 badges • 20 fonts • 821 stickers  •  AssetNewness entries: ~{newnessItems:N0}";
        }
        finally
        {
            _loadingCustomisation = false;
        }
    }

    private void SwitchCustomisationSlot()
    {
        if (_loadingCustomisation) return;
        int next = _customSlot.SelectedIndex;
        if (next < 0 || next > 1) return;
        if (_customLoadedIndex >= 0)
            SaveCustomisationControlsToModel(_customLoadedIndex);
        _customLoadedIndex = next;
        LoadCustomisationControls(next);
    }

    private void LoadCustomisationControls(int index)
    {
        var model = _customProfiles[index];
        if (model is null) return;
        _loadingCustomisation = true;
        try
        {
            SelectAsset(_liveryDesign, model.GetSlot("design", 0, "livery_blank_01"));
            _raceNumber.Value = Math.Clamp(model.GetRaceNumber(), (int)_raceNumber.Minimum, (int)_raceNumber.Maximum);
            SelectAsset(_raceBadge, model.GetSlot("race_badge", 0, "off"));
            SelectAsset(_raceFont, model.GetSlot("race_digits", 0, "off"));

            for (int i = 0; i < 4; i++)
            {
                var paint = model.GetPaint(i);
                _paintFinish[i].Value = Math.Clamp(paint.FinishCode, (int)_paintFinish[i].Minimum, (int)_paintFinish[i].Maximum);
                _paintColour[i].Value = Math.Clamp(paint.PaletteIndex, (int)_paintColour[i].Minimum, (int)_paintColour[i].Maximum);
            }

            SelectAsset(_leftDecal, model.GetSlot("left", 0, "off"));
            SelectAsset(_rightDecal, model.GetSlot("right", 0, "off"));

            for (int i = 0; i < 2; i++)
            {
                var layer = model.GetBadgeLayer(i);
                SelectAsset(_badgeLayerAsset[i], layer.AssetId);
                _badgeLayerPaint[i].Value = Math.Clamp(layer.PaintIndex, (int)_badgeLayerPaint[i].Minimum, (int)_badgeLayerPaint[i].Maximum);
                decimal scale = (decimal)Math.Clamp(layer.Scale, 0f, 10f);
                _badgeLayerScale[i].Value = Math.Clamp(scale, _badgeLayerScale[i].Minimum, _badgeLayerScale[i].Maximum);
            }
        }
        finally
        {
            _loadingCustomisation = false;
        }
    }

    private void SaveCustomisationControlsToModel(int index)
    {
        if (_loadingCustomisation) return;
        var model = _customProfiles[index];
        if (model is null) return;

        model.SetSlot("design", 0, SelectedAssetId(_liveryDesign, "livery_blank_01"));
        model.SetRaceNumber(decimal.ToInt32(_raceNumber.Value));
        model.SetSlot("race_badge", 0, SelectedAssetId(_raceBadge, "off"));
        model.SetSlot("race_digits", 0, SelectedAssetId(_raceFont, "off"));
        for (int i = 0; i < 4; i++)
            model.SetPaint(i, decimal.ToInt32(_paintFinish[i].Value), decimal.ToInt32(_paintColour[i].Value));
        model.SetSlot("left", 0, SelectedAssetId(_leftDecal, "off"));
        model.SetSlot("right", 0, SelectedAssetId(_rightDecal, "off"));
        for (int i = 0; i < 2; i++)
            model.SetBadgeLayer(i, decimal.ToInt32(_badgeLayerPaint[i].Value), SelectedAssetId(_badgeLayerAsset[i], "none"), (float)_badgeLayerScale[i].Value);
    }

    private void CommitCustomisation()
    {
        if (_save is null) return;
        if (_customLoadedIndex >= 0)
            SaveCustomisationControlsToModel(_customLoadedIndex);
        for (int i = 0; i < 2; i++)
        {
            var e = _save.FindString(i == 0 ? "CustomisationS1" : "CustomisationS2");
            var model = _customProfiles[i];
            if (e is null || model is null || !model.IsDirty)
                continue;

            string desired = model.Build();
            bool advancedChanged = !string.Equals(e.Value, model.OriginalText, StringComparison.Ordinal);
            if (advancedChanged && !string.Equals(e.Value, desired, StringComparison.Ordinal))
                throw new InvalidDataException($"Customisation Slot {i + 1} was edited in both Customisation and Advanced with different values. Keep one version before saving.");

            e.Value = desired;
        }
    }

    private void MarkAllCustomisationSeen()
    {
        if (_save?.FindString("AssetNewness") is not { } entry)
        {
            SetStatus("AssetNewness is not present in this save; nothing was changed.");
            return;
        }
        var confirm = MessageBox.Show(this,
            "This clears the saved NEW/UNSEEN markers for the customisation categories currently present in AssetNewness.\n\nIt does NOT unlock liveries or DLC. Continue?",
            "Mark customisation items seen",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        var matches = System.Text.RegularExpressions.Regex.Matches(entry.Value ?? string.Empty, @"@([^,@]+),");
        if (matches.Count == 0)
        {
            SetStatus("AssetNewness did not contain recognised category markers.");
            return;
        }

        var cleaned = new System.Text.StringBuilder();
        foreach (System.Text.RegularExpressions.Match match in matches)
            cleaned.Append('@').Append(match.Groups[1].Value).Append(',');
        entry.Value = cleaned.ToString();
        _customAssetSummary.Text = "Customisation NEW markers cleared • this does not change ownership/unlocks";
        foreach (DataGridViewRow row in _profileGrid.Rows)
            if (row.Tag == entry) row.Cells[1].Value = entry.Value;
        SetStatus("All saved customisation newness markers were cleared. Save to apply.");
    }

    private void PopulateProgression()
    {
        if (_save is null) return;
        _eliteGrid.Rows.Clear();
        foreach (var row in ProgressionData.EliteRows)
        {
            int driverRank = 60 + row.Row;
            string time = FormatMilliseconds(row.TargetTimeMs);
            string track = FriendlyNames.GetTrack(row.TrackId);
            string vehicle = FriendlyNames.GetVehicle(row.VehicleId).Name;
            int ri = _eliteGrid.Rows.Add($"Elite {row.Row} / Rank {driverRank}", row.FameRequired.ToString("N0", CultureInfo.InvariantCulture), row.TourStarsRequired, time, track, vehicle);
            _eliteGrid.Rows[ri].Tag = row;
        }

        _progressCounterGrid.Rows.Clear();
        foreach (var item in _save.FloatEntries)
        {
            var label = ProfileLabels.GetCounter(item.Name);
            int ri = _progressCounterGrid.Rows.Add(label.Category, label.Name, item.Value.ToString("R", CultureInfo.InvariantCulture), item.Name);
            _progressCounterGrid.Rows[ri].Tag = new FloatCounterRowState(item, item.Value);
        }
    }

    private static string FormatMilliseconds(uint ms)
    {
        if (ms == 0) return "—";
        TimeSpan t = TimeSpan.FromMilliseconds(ms);
        return t.TotalHours >= 1
            ? t.ToString(@"h\:mm\:ss\.fff", CultureInfo.InvariantCulture)
            : t.ToString(@"m\:ss\.fff", CultureInfo.InvariantCulture);
    }

    private void SetSelectedEliteFame()
    {
        if (_eliteGrid.CurrentRow?.Tag is not ProgressionData.EliteRow row) return;
        _driverFame.Value = Math.Min((decimal)row.FameRequired, _driverFame.Maximum);
        _driverFameDirty = true;
        UpdateDriverProgressPreview();
        PopulateTrophyPrep();
        SetStatus($"Player Fame prepared for Elite {row.Row} / Driver Rank {60 + row.Row}. Tour stars were not changed.");
    }

    private void CommitProgressionCounters()
    {
        foreach (DataGridViewRow row in _progressCounterGrid.Rows)
        {
            if (row.Tag is not FloatCounterRowState state) continue;
            var item = state.Entry;
            string text = Convert.ToString(row.Cells[2].Value) ?? string.Empty;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value))
                throw new InvalidDataException($"{ProfileLabels.GetCounter(item.Name).Name} must be a finite decimal number.");

            bool normalChanged = BitConverter.SingleToInt32Bits(value) != BitConverter.SingleToInt32Bits(state.OriginalValue);
            if (!normalChanged)
                continue;

            bool advancedChanged = BitConverter.SingleToInt32Bits(item.Value) != BitConverter.SingleToInt32Bits(state.OriginalValue);
            if (advancedChanged && BitConverter.SingleToInt32Bits(item.Value) != BitConverter.SingleToInt32Bits(value))
                throw new InvalidDataException($"{ProfileLabels.GetCounter(item.Name).Name} was edited in both Progression and Advanced with different values.");

            item.Value = value;
        }
    }

    private void CaptureTrophyOriginalValues()
    {
        _trophyOriginalValues.Clear();
        if (_save is null) return;

        _trophyOriginalValues["FAME"] = Math.Max(0, _save.EffectivePlayerFame);
        if (_save.FindStats("Drift")?.Value1 is ulong drift)
            _trophyOriginalValues["STAT:Drift"] = drift;

        foreach (var item in _save.FloatEntries)
            _trophyOriginalValues["FLOAT:" + item.Name] = (decimal)item.Value;
    }

    private decimal OriginalTrophyValue(TrophyEditKind kind, string key, decimal fallback)
    {
        string storageKey = kind switch
        {
            TrophyEditKind.Fame => "FAME",
            TrophyEditKind.StatValue1 => "STAT:" + key,
            _ => "FLOAT:" + key
        };
        return _trophyOriginalValues.TryGetValue(storageKey, out decimal value) ? value : fallback;
    }

    private void PopulateTrophyPrep()
    {
        if (_save is null) return;
        _trophyGrid.Rows.Clear();

        decimal fame = _driverFame.Value;
        AddEditableTrophyRow("Level Up!", "Player Fame", ProgressionData.FameForDriverRank(2), "Driver Level 2", fame, TrophyEditKind.Fame, "FAME");
        AddEditableTrophyRow("5 And Counting", "Player Fame", ProgressionData.FameForDriverRank(5), "Driver Level 5", fame, TrophyEditKind.Fame, "FAME");
        AddEditableTrophyRow("Making Waves", "Player Fame", ProgressionData.FameForDriverRank(15), "Driver Level 15", fame, TrophyEditKind.Fame, "FAME");
        AddEditableTrophyRow("Life Begins", "Player Fame", ProgressionData.FameForDriverRank(30), "Driver Level 30", fame, TrophyEditKind.Fame, "FAME");
        AddEditableTrophyRow("Welcome To The Limit", "Player Fame", ProgressionData.FameForDriverRank(50), "Driver Level 50", fame, TrophyEditKind.Fame, "FAME");

        decimal drift = _save.FindStats("Drift")?.Value1 ?? 0UL;
        AddEditableTrophyRow("Hoon-a-tic", "Total Drift Points", 1_000_000m, "1,000,000 Drift Points", drift, TrophyEditKind.StatValue1, "Drift");

        AddFloatTrophyRow("Full House — Hot Hatch", "Hot Hatch Race Wins", "fullhouse_won_in_hothatch", 1m, "1 qualifying win");
        AddFloatTrophyRow("Full House — Performance", "Performance Race Wins", "fullhouse_won_in_performance", 1m, "1 qualifying win");
        AddFloatTrophyRow("Full House — Sports", "Sports Race Wins", "fullhouse_won_in_sports", 1m, "1 qualifying win");
        AddFloatTrophyRow("Full House — Super", "Super Race Wins", "fullhouse_won_in_super", 1m, "1 qualifying win");
        AddFloatTrophyRow("Full House — Hyper", "Hyper Race Wins", "fullhouse_won_in_hyper", 1m, "1 qualifying win");

        AddFloatTrophyRow("Unbeatable", "My Solo Challenges Won", "my_solo_challenges_won_for_trophies", 5m, "5 wins — legacy online-dependent");
        AddFloatTrophyRow("Challenge Trophy Counter", "Challenges Won", "challenges_won_for_trophies", null, "Custom / mapping not fully proven");
        AddFloatTrophyRow("Solo Challenge Counter", "Solo Challenges Won", "solo_challenges_won_for_trophies", null, "Custom / mapping not fully proven");
        AddFloatTrophyRow("Bike Race Counter", "Superbike Race Wins", "fullhouse_won_in_superbike", null, "Custom");

        AddFloatTrophyRow("Face-Off", "Face-Off Trophy Progress", "faceoff_value", null, "50 Face-Offs — internal unit unverified");
        AddFloatTrophyRow("Long Distance", "Long Distance Counter", "longdistance_value", null, "300 miles / 483 km — internal unit unverified");
        AddFloatTrophyRow("Lifer", "Lifer Distance Counter", "lifer_value", null, "1000 miles / 1610 km — internal unit unverified");

        AddFloatTrophyRow("Accolades — Vehicles", "Best Vehicle Accolade Progress", "best_in_accolade_group_cars", null, "Custom");
        AddFloatTrophyRow("Accolades — Events", "Best Event Accolade Progress", "best_in_accolade_group_event", null, "Custom");
        AddFloatTrophyRow("Accolades — Gameplay", "Best Gameplay Accolade Progress", "best_in_accolade_group_gameplay", null, "Custom");
        AddFloatTrophyRow("Accolades — Race", "Best Race / Game Mode Accolade Progress", "best_in_accolade_group_race", null, "Custom");

        string custom = _save.FindString("CustomisationS1")?.Value ?? string.Empty;
        AddReadOnlyTrophyRow("This One Is Mine", "Saved Customisation", "Customise a vehicle", string.IsNullOrWhiteSpace(custom) ? "No saved customisation" : "Customisation slot present", !string.IsNullOrWhiteSpace(custom));
    }

    private void AddFloatTrophyRow(string trophy, string fieldName, string key, decimal? requirement, string requirementText)
    {
        if (_save?.FindFloat(key) is not { } item) return;
        decimal current = (decimal)ReadPendingFloat(key);
        AddEditableTrophyRow(trophy, fieldName, requirement, requirementText, current, TrophyEditKind.FloatCounter, key);
    }

    private void AddEditableTrophyRow(string trophy, string fieldName, decimal? requirement, string requirementText, decimal current, TrophyEditKind kind, string key)
    {
        bool ready = requirement.HasValue && current >= requirement.Value;
        string currentText = kind == TrophyEditKind.StatValue1 || kind == TrophyEditKind.Fame
            ? decimal.Truncate(current).ToString("N0", CultureInfo.InvariantCulture)
            : current.ToString("0.###", CultureInfo.InvariantCulture);
        string editText = kind == TrophyEditKind.StatValue1 || kind == TrophyEditKind.Fame
            ? decimal.Truncate(current).ToString(CultureInfo.InvariantCulture)
            : current.ToString(CultureInfo.InvariantCulture); // preserve the exact loaded float value unless the user edits it

        int ri = _trophyGrid.Rows.Add(trophy, fieldName, requirementText, currentText, editText, requirement.HasValue ? (ready ? "READY / MET" : "NOT YET") : "CUSTOM");
        decimal original = OriginalTrophyValue(kind, key, current);
        _trophyGrid.Rows[ri].Tag = new TrophyEditTarget(kind, key, requirement, requirementText, current, original);
        _trophyGrid.Rows[ri].Cells[5].Style.ForeColor = requirement.HasValue ? (ready ? Success : TextSecondary) : Warning;
    }

    private void AddReadOnlyTrophyRow(string trophy, string fieldName, string requirement, string current, bool ready)
    {
        int ri = _trophyGrid.Rows.Add(trophy, fieldName, requirement, current, "—", ready ? "READY / MET" : "NOT YET");
        _trophyGrid.Rows[ri].Cells[4].ReadOnly = true;
        _trophyGrid.Rows[ri].Cells[4].Style.ForeColor = TextSecondary;
        _trophyGrid.Rows[ri].Cells[5].Style.ForeColor = ready ? Success : TextSecondary;
    }

    private float ReadPendingFloat(string key)
    {
        foreach (DataGridViewRow row in _progressCounterGrid.Rows)
        {
            if (row.Tag is FloatCounterRowState state && string.Equals(state.Entry.Name, key, StringComparison.Ordinal))
            {
                string text = Convert.ToString(row.Cells[2].Value) ?? string.Empty;
                if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return value;
            }
        }
        return _save?.FindFloat(key)?.Value ?? 0f;
    }

    private void CommitTrophyPrepEdits()
    {
        var pending = new Dictionary<string, (TrophyEditTarget Target, decimal Value, string Trophy)>(StringComparer.Ordinal);
        foreach (DataGridViewRow row in _trophyGrid.Rows)
        {
            if (row.Tag is not TrophyEditTarget target) continue;
            string text = Convert.ToString(row.Cells[4].Value) ?? string.Empty;
            if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value))
                throw new InvalidDataException($"{row.Cells[0].Value}: EDIT VALUE must be a valid number.");
            if (value == target.CurrentValue) continue;

            string identity = $"{target.Kind}:{target.Key}";
            string trophy = Convert.ToString(row.Cells[0].Value) ?? "Trophy counter";
            decimal backingNow = GetCurrentTrophyBackingValue(target);
            if (backingNow != target.CurrentValue && backingNow != value)
                throw new InvalidDataException($"{trophy} edits a field that was also changed on another page. The other page currently wants {backingNow.ToString(CultureInfo.InvariantCulture)}, while Trophy Prep wants {value.ToString(CultureInfo.InvariantCulture)}.");

            if (pending.TryGetValue(identity, out var existing) && existing.Value != value)
                throw new InvalidDataException($"{existing.Trophy} and {trophy} edit the same save field to different values. Keep only one of those EDIT VALUE changes before saving.");
            pending[identity] = (target, value, trophy);
        }

        foreach (var item in pending.Values)
            ApplyTrophyValue(item.Target, item.Value);
    }

    private decimal GetCurrentTrophyBackingValue(TrophyEditTarget target)
    {
        if (_save is null) return target.CurrentValue;
        return target.Kind switch
        {
            TrophyEditKind.Fame => (decimal)Math.Max(0, _save.EffectivePlayerFame),
            TrophyEditKind.StatValue1 => (decimal)(_save.FindStats(target.Key)?.Value1 ?? 0UL),
            TrophyEditKind.FloatCounter => (decimal)(_save.FindFloat(target.Key)?.Value ?? 0f),
            _ => target.CurrentValue
        };
    }

    private void ApplySelectedTrophyValue()
    {
        if (_trophyGrid.CurrentRow?.Tag is not TrophyEditTarget target)
        {
            SetStatus("Select an editable Trophy Prep row first.");
            return;
        }

        string text = Convert.ToString(_trophyGrid.CurrentRow.Cells[4].Value) ?? string.Empty;
        if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value))
        {
            MessageBox.Show(this, "EDIT VALUE must be a valid number.", "Trophy Prep", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string trophyName = Convert.ToString(_trophyGrid.CurrentRow.Cells[0].Value) ?? "Trophy counter";
        ApplyTrophyValue(target, value);
        PopulateStatistics();
        PopulateTrophyPrep();
        SetStatus($"{trophyName} updated.");
    }

    private void SetSelectedTrophyRequirement(bool oneBelow)
    {
        if (_trophyGrid.CurrentRow?.Tag is not TrophyEditTarget target)
        {
            SetStatus("Select an editable Trophy Prep row first.");
            return;
        }
        if (!target.Requirement.HasValue)
        {
            MessageBox.Show(this, "This counter has no verified numeric save-side requirement. Enter the value you want in EDIT VALUE and use APPLY VALUE.", "Trophy Prep", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        decimal value = target.Requirement.Value;
        if (oneBelow && value > 0m) value -= 1m;
        ApplyTrophyValue(target, value);
        PopulateStatistics();
        PopulateTrophyPrep();
        SetStatus(oneBelow ? "Selected trophy counter set one below its verified requirement." : "Selected trophy counter set to its verified requirement.");
    }

    private void RestoreSelectedTrophyValue()
    {
        if (_trophyGrid.CurrentRow?.Tag is not TrophyEditTarget target)
        {
            SetStatus("Select an editable Trophy Prep row first.");
            return;
        }
        ApplyTrophyValue(target, target.OriginalValue);
        PopulateStatistics();
        PopulateTrophyPrep();
        SetStatus("Selected trophy counter restored to the value from the originally opened save.");
    }

    private void ApplyTrophyValue(TrophyEditTarget target, decimal value)
    {
        if (value < 0m)
            throw new InvalidDataException("Trophy Prep values cannot be negative.");

        switch (target.Kind)
        {
            case TrophyEditKind.Fame:
                if (value > _driverFame.Maximum) throw new InvalidDataException($"Player Fame cannot exceed {_driverFame.Maximum:N0} in this editor.");
                _driverFame.Value = decimal.Truncate(value);
                _driverFameDirty = true;
                UpdateDriverProgressPreview();
                break;

            case TrophyEditKind.StatValue1:
                decimal whole = decimal.Truncate(value);
                if (whole > ulong.MaxValue) throw new InvalidDataException("The value is too large for this save counter.");
                SetStatValue1(target.Key, decimal.ToUInt64(whole));
                break;

            case TrophyEditKind.FloatCounter:
                float f = (float)value;
                if (float.IsNaN(f) || float.IsInfinity(f)) throw new InvalidDataException("The value is too large for this save counter.");
                SetFloatCounter(target.Key, f);
                break;
        }
    }

    private void PrepFullHouse()
    {
        SetFloatCounter("fullhouse_won_in_hothatch", 1f);
        SetFloatCounter("fullhouse_won_in_performance", 1f);
        SetFloatCounter("fullhouse_won_in_sports", 1f);
        SetFloatCounter("fullhouse_won_in_super", 1f);
        SetFloatCounter("fullhouse_won_in_hyper", 0f);
        PopulateTrophyPrep();
        SetStatus("Full House prepared: four classes are marked, Hyper is left at 0 for the next qualifying race.");
    }

    private void PrepSoloChallengeWins(int wins)
    {
        SetFloatCounter("my_solo_challenges_won_for_trophies", wins);
        PopulateTrophyPrep();
        SetStatus($"My Solo Challenge trophy counter set to {wins}.");
    }

    private void SetFloatCounter(string key, float value)
    {
        if (_save is null) throw new InvalidOperationException("No save is loaded.");
        if (_save.FindFloat(key) is not { } item)
            throw new InvalidDataException($"The loaded save does not contain the expected profile counter '{key}'.");
        item.Value = value;
        foreach (DataGridViewRow row in _progressCounterGrid.Rows)
            if (row.Tag is FloatCounterRowState state && ReferenceEquals(state.Entry, item))
                row.Cells[2].Value = value.ToString("R", CultureInfo.InvariantCulture);
        foreach (DataGridViewRow row in _profileFloatGrid.Rows)
            if (row.Tag == item) row.Cells[1].Value = value.ToString("R", CultureInfo.InvariantCulture);
    }

    private void SetStatValue1(string name, ulong value)
    {
        if (_save is null) throw new InvalidOperationException("No save is loaded.");
        if (_save.FindStats(name) is not { } item)
            throw new InvalidDataException($"The loaded save does not contain the expected StatsStore entry '{name}'.");
        item.Value1 = value;
        foreach (DataGridViewRow row in _rawStatsGrid.Rows)
            if (row.Tag == item) row.Cells[2].Value = value.ToString(CultureInfo.InvariantCulture);
    }

    private void SetTargetDriverRank(int rank)
    {
        ulong fame = ProgressionData.FameForDriverRank(rank);
        _targetDriverRank.Value = Math.Clamp(rank, 1, 120);
        _driverFame.Value = Math.Min((decimal)fame, _driverFame.Maximum);
        _driverFameDirty = true;
        UpdateDriverProgressPreview();
    }

    private void UpdateDriverProgressPreview()
    {
        ulong fame = decimal.ToUInt64(_driverFame.Value);
        int rank = ProgressionData.DriverRankForFame(fame);
        _driverRankInfo.Text = $"{rank}";
        _driverRankSub.Text = $"DRIVER RANK  /  120";

        ulong currentFloor = ProgressionData.FameForDriverRank(rank);
        ulong? next = ProgressionData.FameForNextDriverRank(fame);
        if (next.HasValue)
        {
            ulong remaining = next.Value > fame ? next.Value - fame : 0;
            ulong span = next.Value > currentFloor ? next.Value - currentFloor : 1;
            ulong earned = fame > currentFloor ? fame - currentFloor : 0;
            int progress = (int)Math.Clamp((long)(earned * 1000UL / span), 0L, 1000L);
            _rankProgress.Value = progress;
            _driverFameInfo.Text = $"Rank {rank + 1} at {next.Value:N0} Fame  •  {remaining:N0} remaining";
        }
        else
        {
            _rankProgress.Value = 1000;
            _driverFameInfo.Text = "Maximum Driver Rank reached.";
        }
    }

    private static void SelectVehicle(ComboBox combo, string? code)
    {
        if (code is null) return;
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is VehicleChoice item && item.Code == code)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private void PopulateCheats()
    {
        if (_save is null) return;

        _loadingCheats = true;
        try
        {
            _cheatOriginalRaw.Clear();
            foreach (string key in new[] { "FGEComplete", "settings.difficulty", "settings.opponents", "settings.laps", "settings.opponentvehicle" })
                _cheatOriginalRaw[key] = _save.FindString(key)?.Value ?? string.Empty;

            _cheatFgeComplete.Checked = IsTrue(_cheatOriginalRaw["FGEComplete"]);

            int difficulty = ParseSavedInt(_cheatOriginalRaw["settings.difficulty"], 1);
            _cheatDifficulty.SelectedIndex = Math.Clamp(difficulty, 0, 4);

            int opponents = ParseSavedInt(_cheatOriginalRaw["settings.opponents"], 1);
            _cheatOpponents.Value = Math.Clamp(opponents, (int)_cheatOpponents.Minimum, (int)_cheatOpponents.Maximum);

            int laps = ParseSavedInt(_cheatOriginalRaw["settings.laps"], 1);
            _cheatLaps.Value = Math.Clamp(laps, (int)_cheatLaps.Minimum, (int)_cheatLaps.Maximum);

            int opponentVehicle = ParseSavedInt(_cheatOriginalRaw["settings.opponentvehicle"], 0);
            _cheatOpponentVehicle.SelectedIndex = Math.Clamp(opponentVehicle, 0, 1);

            _cheatFgeDirty = false;
            _cheatDifficultyDirty = false;
            _cheatOpponentsDirty = false;
            _cheatLapsDirty = false;
            _cheatOpponentVehicleDirty = false;
        }
        finally
        {
            _loadingCheats = false;
        }
    }

    private static int ParseSavedInt(string raw, int fallback) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;

    private void SetFeatureChecked(string key, bool enabled)
    {
        int index = 0;
        foreach (var pair in FriendlyNames.FeatureLabels)
        {
            if (string.Equals(pair.Key, key, StringComparison.Ordinal))
            {
                _features.SetItemChecked(index, enabled);
                return;
            }
            index++;
        }
    }

    private void CommitCheats()
    {
        if (_save is null) return;

        CommitCheatString("FGEComplete", _cheatFgeComplete.Checked ? "true" : "false", _cheatFgeDirty, "FGEComplete");
        CommitCheatString("settings.difficulty", Math.Max(0, _cheatDifficulty.SelectedIndex).ToString(CultureInfo.InvariantCulture), _cheatDifficultyDirty, "Free Play difficulty");
        CommitCheatString("settings.opponents", decimal.ToInt32(_cheatOpponents.Value).ToString(CultureInfo.InvariantCulture), _cheatOpponentsDirty, "Free Play opponents");
        CommitCheatString("settings.laps", decimal.ToInt32(_cheatLaps.Value).ToString(CultureInfo.InvariantCulture), _cheatLapsDirty, "Free Play laps");
        CommitCheatString("settings.opponentvehicle", Math.Max(0, _cheatOpponentVehicle.SelectedIndex).ToString(CultureInfo.InvariantCulture), _cheatOpponentVehicleDirty, "Free Play opponent vehicle rule");
    }

    private void CommitCheatString(string key, string desired, bool friendlyChanged, string label)
    {
        if (_save is null || !friendlyChanged) return;
        var entry = _save.FindString(key);
        if (entry is null)
            throw new InvalidDataException($"{label} field '{key}' is missing from this profile.");

        string original = _cheatOriginalRaw.TryGetValue(key, out var raw) ? raw : entry.Value;
        bool advancedChanged = !string.Equals(entry.Value, original, StringComparison.Ordinal);
        if (advancedChanged && !string.Equals(entry.Value, desired, StringComparison.Ordinal))
            throw new InvalidDataException($"{label} was edited in both Cheats and Advanced with different values.");

        entry.Value = desired;
    }

    private void PopulateGarage()
    {
        if (_save is null) return;
        _garageGrid.Rows.Clear();

        foreach (var stat in _save.StatsStoreEntries.Where(x => x.Kind == 0).OrderBy(x => FriendlyNames.GetVehicle(x.Name).Name))
        {
            var info = FriendlyNames.GetVehicle(stat.Name);
            ulong fameForLevel = stat.Value1 ?? 0;
            int level = FriendlyNames.GetVehicleLevel(stat.Name, fameForLevel);
            string fameText = stat.Value1.HasValue
                ? stat.Value1.Value.ToString("N0", CultureInfo.InvariantCulture)
                : "— not stored —";
            int rowIndex = _garageGrid.Rows.Add(info.Name, info.Group, level, fameText, stat.Name);
            _garageGrid.Rows[rowIndex].Tag = new GarageRowState(stat, level, stat.Value1);
        }
        ApplyGarageFilter();
    }

    private void PopulateStatistics()
    {
        if (_save is null) return;
        _statsGrid.Rows.Clear();

        foreach (var stat in _save.StatsStoreEntries)
        {
            string category;
            string name;
            if (stat.Kind == 0)
            {
                category = "Vehicle";
                name = FriendlyNames.GetVehicle(stat.Name).Name;
            }
            else if (stat.Kind == 1)
            {
                category = "Track";
                name = FriendlyNames.GetTrack(stat.Name);
            }
            else
            {
                category = stat.Kind switch { 2 => "Driving", 3 => "Face-Off", 4 => "Game Mode", 5 => "Solo", _ => "Other" };
                name = FriendlyNames.Humanize(stat.Name.Replace("Mode_", ""));
            }

            _statsGrid.Rows.Add(
                category,
                name,
                FormatFriendlyStatValue1(stat),
                stat.Value3?.ToString("N0", CultureInfo.InvariantCulture) ?? "—");
        }
    }

    private static string FormatFriendlyStatValue1(DriveclubSave.StatsStoreEntry stat)
    {
        if (!stat.Value1.HasValue) return "—";

        // A few Driving counters are serialized in the uint64 slot using two's-complement
        // negative values (for example collision/off-track penalties). Showing the raw uint64
        // produces misleading 18-quintillion values in the normal Statistics page. Advanced
        // still shows the exact unsigned bits.
        if (stat.Kind == 2 && stat.Value1.Value > long.MaxValue)
            return unchecked((long)stat.Value1.Value).ToString("N0", CultureInfo.InvariantCulture);

        return stat.Value1.Value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private void PopulateAdvanced()
    {
        if (_save is null) return;
        _profileGrid.Rows.Clear();
        _profileFloatGrid.Rows.Clear();
        _rawStatsGrid.Rows.Clear();
        _rankGrid.Rows.Clear();
        _fameGrid.Rows.Clear();
        _storeGrid.Rows.Clear();

        foreach (var item in _save.StringEntries)
        {
            int row = _profileGrid.Rows.Add(item.Name, item.Value);
            _profileGrid.Rows[row].Tag = item;
        }
        foreach (var item in _save.FloatEntries)
        {
            int row = _profileFloatGrid.Rows.Add(item.Name, item.Value.ToString("R", CultureInfo.InvariantCulture));
            _profileFloatGrid.Rows[row].Tag = item;
        }
        foreach (var item in _save.StatsStoreEntries)
        {
            int row = _rawStatsGrid.Rows.Add(
                item.Name,
                item.Kind,
                item.Value1?.ToString(CultureInfo.InvariantCulture) ?? "",
                item.Value2?.ToString(CultureInfo.InvariantCulture) ?? "",
                item.Value3?.ToString(CultureInfo.InvariantCulture) ?? "",
                item.Value4?.ToString(CultureInfo.InvariantCulture) ?? "",
                item.Value5?.ToString(CultureInfo.InvariantCulture) ?? "");
            _rawStatsGrid.Rows[row].Tag = item;
            SetOptionalRawCell(_rawStatsGrid.Rows[row].Cells[2], item.Value1.HasValue);
            SetOptionalRawCell(_rawStatsGrid.Rows[row].Cells[3], item.Value2.HasValue);
            SetOptionalRawCell(_rawStatsGrid.Rows[row].Cells[4], item.Value3.HasValue);
            SetOptionalRawCell(_rawStatsGrid.Rows[row].Cells[5], item.Value4.HasValue);
            SetOptionalRawCell(_rawStatsGrid.Rows[row].Cells[6], item.Value5.HasValue);
        }

        PopulateScalarGrid(_rankGrid, _save.RankEntries);
        PopulateScalarGrid(_fameGrid, _save.FameEntries);
        foreach (var item in _save.StoreCatalogEntries)
        {
            int row = _storeGrid.Rows.Add(item.Index, item.Value.ToString(CultureInfo.InvariantCulture));
            _storeGrid.Rows[row].Tag = item;
        }
    }

    private static void PopulateScalarGrid(DataGridView grid, IEnumerable<DriveclubSave.ScalarEntry> entries)
    {
        foreach (var item in entries)
        {
            string value = item.IsUnsigned ? item.UnsignedValue.ToString(CultureInfo.InvariantCulture) : item.SignedValue.ToString(CultureInfo.InvariantCulture);
            int row = grid.Rows.Add(item.Name, item.Type, value);
            grid.Rows[row].Tag = new ScalarRowState(item, item.SignedValue, item.UnsignedValue);
        }
    }

    private void SaveCurrent(bool saveAs)
    {
        if (_save is null)
        {
            MessageBox.Show(this, "Open profile.sav first.", "Driveclub PS4 Save Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string path = _save.FilePath;
        if (saveAs)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save edited Driveclub profile.sav",
                FileName = "profile.sav",
                Filter = "Driveclub profile.sav|profile.sav|Save files (*.sav)|*.sav|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return; // Do not mutate the in-memory model when Save Copy is cancelled.
            path = dialog.FileName;
        }

        try
        {
            CommitAdvanced();
            CommitFriendly();

            bool overwritesLoadedFile = PathsEqual(path, _save.FilePath);
            bool makeBackup = !saveAs || overwritesLoadedFile;
            _save.Save(path, makeBackup);
            PopulateAll();
            _fileLabel.Text = $"{Path.GetFileName(path)}  •  {new FileInfo(path).Length:N0} bytes";
            UpdateSaveBadge();
            SetStatus(makeBackup
                ? "Saved and verified on disk. Backup created as profile.sav.bak."
                : "Saved edited copy and verified checksum/on-disk bytes.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Save failed.");
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private bool AdvancedPlayerFameWasEdited()
    {
        foreach (DataGridViewRow row in _fameGrid.Rows)
        {
            if (row.Tag is not ScalarRowState state || state.Entry.IsUnsigned) continue;
            if (state.Entry.Name is not ("Fame[0]" or "Fame[1]" or "Fame[2]")) continue;
            if (state.Entry.SignedValue != state.OriginalSigned)
                return true;
        }
        return false;
    }

    private void CommitFriendly()
    {
        if (_save is null) return;

        // Commit overlapping progression/trophy fields before writing Player Fame. A user may type
        // a Fame value directly into Trophy Prep and press Save without clicking Apply first.
        // CommitTrophyPrepEdits() marks _driverFameDirty, so Fame must be serialized afterwards.
        CommitProgressionCounters();
        CommitTrophyPrepEdits();
        CommitCheats();

        if (_driverFameDirty)
        {
            if (AdvancedPlayerFameWasEdited())
                throw new InvalidDataException("Player Fame was edited in both Player/Trophy Prep and Advanced Rank/Fame. Keep only one Fame edit before saving.");
            _save.SetEffectivePlayerFame(decimal.ToInt64(_driverFame.Value));
        }

        int index = 0;
        foreach (var pair in FriendlyNames.FeatureLabels)
        {
            var entry = _save.FindString(pair.Key);
            if (entry is null)
            {
                index++;
                continue;
            }

            string original = _featureOriginalRaw.TryGetValue(pair.Key, out string? raw) ? raw : entry.Value;
            string desired = _features.GetItemChecked(index) ? "true" : "false";
            bool friendlyChanged = IsTrue(original) != _features.GetItemChecked(index);
            bool advancedChanged = !string.Equals(entry.Value, original, StringComparison.Ordinal);

            if (friendlyChanged && advancedChanged && !string.Equals(entry.Value, desired, StringComparison.Ordinal))
                throw new InvalidDataException($"{pair.Value} was edited in both Player and Advanced with different values.");
            if (friendlyChanged)
                entry.Value = desired;
            index++;
        }

        if (_recentCar.SelectedItem is VehicleChoice car && !string.Equals(car.Code, _recentCarOriginal, StringComparison.Ordinal))
        {
            var e = _save.FindString("recentvehicleDC_CARS");
            if (e is not null)
            {
                if (!string.Equals(e.Value, _recentCarOriginal, StringComparison.Ordinal) && !string.Equals(e.Value, car.Code, StringComparison.Ordinal))
                    throw new InvalidDataException("Recent Car was edited in both Player and Advanced with different values.");
                e.Value = car.Code;
            }
        }
        if (_recentBike.SelectedItem is VehicleChoice bike && !string.Equals(bike.Code, _recentBikeOriginal, StringComparison.Ordinal))
        {
            var e = _save.FindString("recentvehicleDC_BIKES");
            if (e is not null)
            {
                if (!string.Equals(e.Value, _recentBikeOriginal, StringComparison.Ordinal) && !string.Equals(e.Value, bike.Code, StringComparison.Ordinal))
                    throw new InvalidDataException("Recent Bike was edited in both Player and Advanced with different values.");
                e.Value = bike.Code;
            }
        }

        foreach (DataGridViewRow row in _garageGrid.Rows)
        {
            if (row.Tag is not GarageRowState state) continue;
            var stat = state.Stat;
            string text = Convert.ToString(row.Cells[2].Value) ?? "";
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level) || level < 1 || level > 15)
                throw new InvalidDataException($"{FriendlyNames.GetVehicle(stat.Name).Name}: vehicle level must be between 1 and 15.");

            if (level == state.OriginalLevel)
                continue; // Critical: preserve exact Fame/progress and missing Value1 fields when untouched.

            if (stat.Value1 != state.OriginalFame)
                throw new InvalidDataException($"{FriendlyNames.GetVehicle(stat.Name).Name} was edited in both Garage and Advanced. Keep only one vehicle Fame/level edit before saving.");

            stat.Value1 = FriendlyNames.GetVehicleThreshold(stat.Name, level);
        }

        CommitCustomisation();
    }

    private void CommitAdvanced()
    {
        if (_save is null) return;

        foreach (DataGridViewRow row in _profileGrid.Rows)
        {
            if (row.Tag is DriveclubSave.StringEntry item)
                item.Value = Convert.ToString(row.Cells[1].Value) ?? "";
        }
        foreach (DataGridViewRow row in _profileFloatGrid.Rows)
        {
            if (row.Tag is not DriveclubSave.FloatEntry item) continue;
            string text = Convert.ToString(row.Cells[1].Value) ?? "";
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value))
                throw new InvalidDataException($"{item.Name} must be a finite decimal number.");
            item.Value = value;
        }
        foreach (DataGridViewRow row in _rawStatsGrid.Rows)
        {
            if (row.Tag is not DriveclubSave.StatsStoreEntry item) continue;
            if (item.Value1.HasValue) item.Value1 = ParseULong(row.Cells[2], item.Name, "Value 1");
            if (item.Value2.HasValue) item.Value2 = ParseUInt(row.Cells[3], item.Name, "Value 2");
            if (item.Value3.HasValue) item.Value3 = ParseUInt(row.Cells[4], item.Name, "Value 3");
            if (item.Value4.HasValue) item.Value4 = ParseUInt(row.Cells[5], item.Name, "Value 4");
            if (item.Value5.HasValue) item.Value5 = ParseUInt(row.Cells[6], item.Name, "Value 5");
        }

        CommitScalarGrid(_rankGrid);
        CommitScalarGrid(_fameGrid);
        foreach (DataGridViewRow row in _storeGrid.Rows)
        {
            if (row.Tag is not DriveclubSave.StoreCatalogEntry item) continue;
            string text = Convert.ToString(row.Cells[1].Value) ?? "";
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new InvalidDataException($"StoreCatalog[{item.Index}] must be a signed 32-bit integer.");
            item.Value = value;
        }
    }

    private static void CommitScalarGrid(DataGridView grid)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.Tag is not ScalarRowState state) continue;
            var item = state.Entry;
            string text = Convert.ToString(row.Cells[2].Value) ?? "";
            if (item.IsUnsigned)
            {
                if (!ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong value) || value > uint.MaxValue)
                    throw new InvalidDataException($"{item.Name} must be between 0 and {uint.MaxValue}.");
                item.UnsignedValue = value;
            }
            else
            {
                if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
                    throw new InvalidDataException($"{item.Name} must be a signed integer.");
                item.SignedValue = value;
            }
        }
    }

    private void SetAllGarageLevels(int level)
    {
        foreach (DataGridViewRow row in _garageGrid.Rows)
            row.Cells[2].Value = level;
        SetStatus($"All visible and hidden vehicles set to level {level}. Save to apply.");
    }

    private void SetSelectedGarageLevel(int level)
    {
        if (_garageGrid.CurrentRow is null) return;
        _garageGrid.CurrentRow.Cells[2].Value = level;
    }

    private void UpdateGarageFamePreview(DataGridViewRow row)
    {
        if (row.Tag is not GarageRowState state) return;
        string text = Convert.ToString(row.Cells[2].Value) ?? string.Empty;
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level) || level < 1 || level > 15)
        {
            row.Cells[3].Value = "Invalid level";
            return;
        }

        if (level == state.OriginalLevel)
        {
            row.Cells[3].Value = state.OriginalFame.HasValue
                ? state.OriginalFame.Value.ToString("N0", CultureInfo.InvariantCulture)
                : "— not stored —";
            return;
        }

        ulong target = FriendlyNames.GetVehicleThreshold(state.Stat.Name, level);
        row.Cells[3].Value = target.ToString("N0", CultureInfo.InvariantCulture) + "  (will set)";
    }

    private void ApplyGarageFilter()
    {
        string q = _garageSearch.Text.Trim();
        foreach (DataGridViewRow row in _garageGrid.Rows)
        {
            string vehicle = Convert.ToString(row.Cells[0].Value) ?? "";
            string group = Convert.ToString(row.Cells[1].Value) ?? "";
            row.Visible = q.Length == 0 || vehicle.Contains(q, StringComparison.OrdinalIgnoreCase) || group.Contains(q, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void SetOptionalRawCell(DataGridViewCell cell, bool exists)
    {
        cell.ReadOnly = !exists;
        if (!exists)
        {
            cell.Style.BackColor = Color.FromArgb(42, 42, 42);
            cell.Style.ForeColor = Color.DimGray;
        }
    }

    private static ulong ParseULong(DataGridViewCell cell, string name, string field)
    {
        string text = Convert.ToString(cell.Value) ?? "";
        if (!ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong value))
            throw new InvalidDataException($"{name} {field} must be an unsigned 64-bit integer.");
        return value;
    }

    private static uint ParseUInt(DataGridViewCell cell, string name, string field)
    {
        string text = Convert.ToString(cell.Value) ?? "";
        if (!uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint value))
            throw new InvalidDataException($"{name} {field} must be an unsigned 32-bit integer.");
        return value;
    }

    private static bool IsTrue(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";

    private static Panel MakeMetricCard(string title, Label valueLabel, string subtitle)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 14, 0),
            BackColor = Surface,
            Padding = new Padding(18)
        };
        card.Paint += (_, e) =>
        {
            using var borderPen = new Pen(Border);
            e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
            using var accentPen = new Pen(Accent, 3F);
            e.Graphics.DrawLine(accentPen, 1, 1, card.Width - 2, 1);
        };
        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Location = new Point(18, 18),
            Font = new Font("Segoe UI", 7.8F, FontStyle.Bold),
            ForeColor = TextMuted
        };
        valueLabel.AutoSize = true;
        valueLabel.Location = new Point(18, 45);
        valueLabel.Font = new Font("Segoe UI", 21F, FontStyle.Bold);
        valueLabel.ForeColor = TextPrimary;
        valueLabel.Text = "—";
        var sub = new Label
        {
            Text = subtitle,
            AutoSize = true,
            Location = new Point(20, 91),
            Font = new Font("Segoe UI", 8.3F),
            ForeColor = TextSecondary
        };
        card.Controls.Add(titleLabel);
        card.Controls.Add(valueLabel);
        card.Controls.Add(sub);
        return card;
    }

    private static Panel MakeCard(string title, string subtitle, int height)
    {
        var card = new Panel { Height = height, BackColor = Surface, Padding = new Padding(0), Margin = new Padding(0) };
        card.Paint += (_, e) =>
        {
            using var borderPen = new Pen(Border);
            e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
            using var accentPen = new Pen(Accent, 3F);
            e.Graphics.DrawLine(accentPen, 1, 1, card.Width - 2, 1);
            using var dividerPen = new Pen(Border);
            e.Graphics.DrawLine(dividerPen, 18, 58, card.Width - 18, 58);
        };
        card.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Location = new Point(20, 14),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = TextPrimary
        });
        card.Controls.Add(new Label
        {
            Text = subtitle,
            AutoSize = true,
            Location = new Point(20, 36),
            Font = new Font("Segoe UI", 8.4F),
            ForeColor = TextSecondary
        });
        return card;
    }

    private static Label MakeFieldLabel(string text, int x, int y) => new()
    {
        Text = text,
        AutoSize = true,
        Location = new Point(x, y),
        Font = new Font("Segoe UI", 8F, FontStyle.Bold),
        ForeColor = TextSecondary
    };

    private static Panel MakeGap(int height) => new() { Dock = DockStyle.Top, Height = height, BackColor = AppBack };

    private static Label MakeNote(string text, int height, Color? accent = null)
    {
        var label = new Label
        {
            Dock = DockStyle.Top,
            Height = height,
            Padding = new Padding(16, 12, 16, 0),
            Text = text,
            BackColor = SurfaceRaised,
            ForeColor = TextSecondary,
            BorderStyle = BorderStyle.FixedSingle
        };
        if (accent.HasValue) label.ForeColor = accent.Value;
        return label;
    }

    private static TabPage NewPage(string title) => new(title)
    {
        BackColor = AppBack,
        ForeColor = TextPrimary,
        Padding = new Padding(0)
    };

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Surface,
            GridColor = Border,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 42,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            AutoGenerateColumns = false
        };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = SurfaceRaised,
            ForeColor = TextSecondary,
            SelectionBackColor = SurfaceRaised,
            SelectionForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
            Padding = new Padding(10, 0, 0, 0)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = TextPrimary,
            SelectionBackColor = AccentSoft,
            SelectionForeColor = Color.White,
            Font = new Font("Segoe UI", 9.3F),
            Padding = new Padding(10, 0, 0, 0)
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(23, 29, 38), ForeColor = TextPrimary };
        grid.RowTemplate.Height = 38;
        return grid;
    }

    private static Button MakeButton(string text, bool primary = false, int width = 120)
    {
        var normal = primary ? Accent : SurfaceRaised;
        var hover = primary ? AccentHover : SurfaceHover;
        var pressed = primary ? Color.FromArgb(43, 91, 209) : AccentSoft;
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = normal,
            ForeColor = Color.White,
            Margin = new Padding(5, 0, 5, 0),
            Padding = new Padding(8, 0, 8, 0),
            Font = new Font("Segoe UI", 8.4F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = hover;
        button.FlatAppearance.MouseDownBackColor = pressed;
        return button;
    }

    private static void StyleNumeric(NumericUpDown numeric)
    {
        numeric.BackColor = InputBack;
        numeric.ForeColor = TextPrimary;
        numeric.BorderStyle = BorderStyle.FixedSingle;
        numeric.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        numeric.MinimumSize = new Size(0, 30);
    }

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.BackColor = InputBack;
        textBox.ForeColor = TextPrimary;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = new Font("Segoe UI", 10F);
        textBox.MinimumSize = new Size(0, 30);
    }

    private void UpdateSaveBadge()
    {
        if (_save is null)
        {
            _saveStateBadge.Text = "NO SAVE LOADED";
            _saveStateBadge.BackColor = SurfaceRaised;
            _saveStateBadge.ForeColor = TextSecondary;
            return;
        }

        _saveStateBadge.Text = _save.ChecksumValid ? "CHECKSUM VALID" : "CHECKSUM INVALID";
        _saveStateBadge.BackColor = _save.ChecksumValid ? Color.FromArgb(27, 76, 61) : Color.FromArgb(87, 36, 42);
        _saveStateBadge.ForeColor = _save.ChecksumValid ? Success : Danger;
    }

    private void SetStatus(string text) => _statusLabel.Text = text;
}
