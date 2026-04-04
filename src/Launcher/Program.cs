using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaklGuiFixed
{
    internal enum SaklAction
    {
        Load,
        Read,
        Zeroize
    }

    internal enum SaklScope
    {
        Active,
        Device,
        Named
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly RadioButton loadActionRadio;
        private readonly RadioButton readActionRadio;
        private readonly RadioButton zeroizeActionRadio;
        private readonly RadioButton activeScopeRadio;
        private readonly RadioButton deviceScopeRadio;
        private readonly RadioButton namedScopeRadio;
        private readonly ComboBox actionComboBox;
        private readonly ComboBox scopeComboBox;
        private readonly TextBox ipTextBox;
        private readonly NumericUpDown portNumeric;
        private readonly NumericUpDown timeoutNumeric;
        private readonly TextBox wacnTextBox;
        private readonly TextBox systemTextBox;
        private readonly TextBox unitTextBox;
        private readonly TextBox keyTextBox;
        private readonly TextBox[] keyByteTextBoxes;
        private readonly Button generateKeyButton;
        private readonly Button pasteKeyButton;
        private readonly Button executeButton;
        private readonly TextBox outputTextBox;
        private readonly Label scopeHintLabel;
        private readonly CheckBox verboseOutputCheckBox;
        private readonly CheckBox darkModeCheckBox;
        private readonly CheckBox verboseCliCheckBox;
        private readonly TextBox resultSummaryLabel;
        private readonly string settingsFilePath;

        private Color appBackground;
        private Color sectionBackground;
        private Color accentColor;
        private Color textColor;
        private Color mutedTextColor;
        private Color outputBackground;
        private Color inputBackground;
        private bool darkModeEnabled;
        private bool suppressSelectionSync;

        public MainForm()
        {
            settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sakl_gui_settings.ini");
            SetTheme(false);

            Text = "Software Authentication Keyloader GUI";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1080, 760);
            MinimumSize = new Size(980, 700);
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = appBackground;

            var root = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 10),
                BackColor = appBackground
            };

            var topContentPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 470,
                BackColor = appBackground,
                Margin = new Padding(0)
            };

            var headerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = appBackground,
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(0),
                Height = 54
            };
            headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleLabel = new Label
            {
                AutoSize = true,
                Text = "Radio Authentication Console",
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point),
                Margin = new Padding(2, 0, 3, 0),
                ForeColor = accentColor,
                Tag = "title",
                BackColor = appBackground
            };
            headerPanel.Controls.Add(titleLabel, 0, 0);

            var subtitleLabel = new Label
            {
                AutoSize = true,
                Text = "Load, inspect, and zeroize authentication slots through the packaged sakl.exe tool.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Margin = new Padding(4, 0, 3, 2),
                ForeColor = mutedTextColor,
                Tag = "muted",
                BackColor = appBackground
            };
            headerPanel.Controls.Add(subtitleLabel, 0, 1);
            topContentPanel.Controls.Add(headerPanel);

            var actionPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0),
                BackColor = sectionBackground,
                Margin = new Padding(0, 0, 8, 0)
            };
            actionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var actionLabel = new Label
            {
                AutoSize = true,
                Text = "Action",
                Margin = new Padding(0, 0, 0, 4),
                ForeColor = textColor,
                BackColor = sectionBackground,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
            };
            loadActionRadio = new RadioButton { AutoSize = true, Text = "Load", Checked = true };
            readActionRadio = new RadioButton { AutoSize = true, Text = "Read Status" };
            zeroizeActionRadio = new RadioButton { AutoSize = true, Text = "Zeroize" };
            StyleSegmentRadio(loadActionRadio, 96);
            StyleSegmentRadio(readActionRadio, 116);
            StyleSegmentRadio(zeroizeActionRadio, 96);
            loadActionRadio.CheckedChanged += (_, __) => UpdateControlState();
            readActionRadio.CheckedChanged += (_, __) => UpdateControlState();
            zeroizeActionRadio.CheckedChanged += (_, __) => UpdateControlState();
            actionComboBox = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                IntegralHeight = false,
                DropDownHeight = 120
            };
            actionComboBox.Items.AddRange(new object[] { "Load", "Read Status", "Zeroize" });
            actionComboBox.SelectedIndex = 0;
            actionComboBox.SelectedIndexChanged += (_, __) =>
            {
                if (suppressSelectionSync)
                {
                    return;
                }

                switch (actionComboBox.SelectedItem as string)
                {
                    case "Read Status":
                        readActionRadio.Checked = true;
                        break;
                    case "Zeroize":
                        zeroizeActionRadio.Checked = true;
                        break;
                    default:
                        loadActionRadio.Checked = true;
                        break;
                }

                UpdateControlState();
            };
            actionPanel.Controls.Add(actionLabel, 0, 0);
            actionPanel.Controls.Add(actionComboBox, 0, 1);

            var optionsGroup = new GroupBox
            {
                Text = "Connection",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = sectionBackground,
                ForeColor = textColor,
                Padding = new Padding(8, 8, 8, 8),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
            };

            var optionsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                Margin = new Padding(0),
                BackColor = sectionBackground
            };
            optionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            optionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            optionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            optionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            ipTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = "192.168.128.1"
            };
            StyleInput(ipTextBox, textColor);

            portNumeric = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 65535,
                Value = 49165
            };
            StyleInput(portNumeric, textColor);

            timeoutNumeric = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 100,
                Maximum = 120000,
                Increment = 100,
                Value = 5000
            };
            StyleInput(timeoutNumeric, textColor);

            AddLabeledControl(optionsGrid, "IP Address", ipTextBox, 0, 0);
            AddLabeledControl(optionsGrid, "UDP Port", portNumeric, 2, 0);
            AddLabeledControl(optionsGrid, "Timeout (ms)", timeoutNumeric, 0, 2);
            optionsGroup.Controls.Add(optionsGrid);
            var cardsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = appBackground,
                Margin = new Padding(0),
                Padding = new Padding(0),
                Height = 404
            };
            cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cardsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 246F));
            cardsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));

            var leftTopCard = new GroupBox
            {
                Text = "Action And Connection",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 8),
                BackColor = sectionBackground,
                ForeColor = textColor,
                Padding = new Padding(8),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
            };
            var leftTopLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = sectionBackground,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            leftTopLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            leftTopLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var topSelectorsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = sectionBackground,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            topSelectorsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            topSelectorsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            topSelectorsLayout.Controls.Add(actionPanel, 0, 0);
            leftTopLayout.Controls.Add(topSelectorsLayout, 0, 0);
            leftTopLayout.Controls.Add(optionsGroup, 0, 1);
            leftTopCard.Controls.Add(leftTopLayout);

            var keyGroup = new GroupBox
            {
                Text = "Target And Key",
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 8),
                BackColor = sectionBackground,
                ForeColor = textColor,
                Padding = new Padding(10, 8, 10, 8),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
            };

            var keyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = sectionBackground,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            keyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            keyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            keyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            var keyGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Margin = new Padding(0),
                BackColor = sectionBackground
            };
            keyGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            keyGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            keyGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            keyGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            keyGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            keyGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 106F));

            wacnTextBox = new TextBox { Dock = DockStyle.Fill, MaxLength = 5, CharacterCasing = CharacterCasing.Upper };
            systemTextBox = new TextBox { Dock = DockStyle.Fill, MaxLength = 4, CharacterCasing = CharacterCasing.Upper };
            unitTextBox = new TextBox { Dock = DockStyle.Fill };
            StyleInput(wacnTextBox, textColor);
            StyleInput(systemTextBox, textColor);
            StyleInput(unitTextBox, textColor);
            keyTextBox = new TextBox
            {
                Visible = false,
                MaxLength = 32,
                CharacterCasing = CharacterCasing.Upper
            };
            keyByteTextBoxes = new TextBox[16];
            var keyByteGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 2,
                Margin = new Padding(0, 2, 0, 0),
                BackColor = sectionBackground
            };
            keyByteGrid.MinimumSize = new Size(0, 56);
            for (var index = 0; index < 8; index++)
            {
                keyByteGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            }
            keyByteGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            keyByteGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

            for (var index = 0; index < keyByteTextBoxes.Length; index++)
            {
                var byteBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    MaxLength = 2,
                    CharacterCasing = CharacterCasing.Upper,
                    Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point),
                    Margin = new Padding(index % 8 == 7 ? 0 : 4, 2, 0, 2),
                    TextAlign = HorizontalAlignment.Center
                };
                StyleInput(byteBox, textColor);
                byteBox.Margin = new Padding(index % 8 == 7 ? 0 : 4, 2, 0, 2);
                var capturedIndex = index;
                byteBox.TextChanged += (_, __) => NormalizeKeyByteText(capturedIndex);
                byteBox.KeyDown += (_, e) => HandleKeyByteKeyDown(capturedIndex, e);
                byteBox.ShortcutsEnabled = true;
                var pasteMenu = new ContextMenuStrip();
                var pasteItem = new ToolStripMenuItem("Paste Key");
                pasteItem.Click += (_, __) => PasteKeyFromClipboard(capturedIndex);
                pasteMenu.Items.Add(pasteItem);
                byteBox.ContextMenuStrip = pasteMenu;
                keyByteTextBoxes[index] = byteBox;
                keyByteGrid.Controls.Add(byteBox, index % 8, index / 8);
            }

            generateKeyButton = new Button
            {
                AutoSize = false,
                Width = 118,
                Height = 28,
                Text = "Auto-Generate",
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0)
            };
            StyleSecondaryButton(generateKeyButton);
            generateKeyButton.Click += (_, __) => GenerateRandomKey();

            pasteKeyButton = new Button
            {
                AutoSize = false,
                Width = 92,
                Height = 28,
                Text = "Paste Key",
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 0, 8, 0)
            };
            StyleSecondaryButton(pasteKeyButton);
            pasteKeyButton.Click += (_, __) => PasteKeyFromClipboard(0);

            AddCompactLabeledControl(keyGrid, "WACN (hex)", wacnTextBox, 0);
            AddCompactLabeledControl(keyGrid, "System (hex)", systemTextBox, 1);
            AddCompactLabeledControl(keyGrid, "Unit (decimal)", unitTextBox, 2);
            keyGrid.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "Key",
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(3, 7, 6, 3),
                BackColor = sectionBackground,
                ForeColor = mutedTextColor
            }, 0, 3);
            keyGrid.Controls.Add(keyByteGrid, 1, 3);

            var generatePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = sectionBackground,
                Margin = new Padding(0)
            };
            generatePanel.Controls.Add(generateKeyButton);
            generatePanel.Controls.Add(pasteKeyButton);
            pasteKeyButton.Location = new Point(
                Math.Max(0, generatePanel.ClientSize.Width - generateKeyButton.Width - pasteKeyButton.Width - 8),
                2);
            generateKeyButton.Location = new Point(Math.Max(0, generatePanel.Width - generateKeyButton.Width), 2);
            generatePanel.Resize += (_, __) =>
            {
                pasteKeyButton.Location = new Point(
                    Math.Max(0, generatePanel.ClientSize.Width - generateKeyButton.Width - pasteKeyButton.Width - 8),
                    2);
                generateKeyButton.Location = new Point(
                    Math.Max(0, generatePanel.ClientSize.Width - generateKeyButton.Width),
                    2);
            };

            keyLayout.Controls.Add(keyGrid, 0, 0);
            keyLayout.Controls.Add(generatePanel, 0, 1);
            keyGroup.Controls.Add(keyLayout);

            var scopeFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0),
                BackColor = sectionBackground
            };
            activeScopeRadio = new RadioButton { AutoSize = true, Text = "Active", Checked = true };
            deviceScopeRadio = new RadioButton { AutoSize = true, Text = "Device" };
            namedScopeRadio = new RadioButton { AutoSize = true, Text = "Named" };
            StyleSegmentRadio(activeScopeRadio, 96);
            StyleSegmentRadio(deviceScopeRadio, 96);
            StyleSegmentRadio(namedScopeRadio, 96);
            activeScopeRadio.CheckedChanged += (_, __) => UpdateControlState();
            deviceScopeRadio.CheckedChanged += (_, __) => UpdateControlState();
            namedScopeRadio.CheckedChanged += (_, __) => UpdateControlState();
            var scopeGroup = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(8, 0, 0, 0),
                BackColor = sectionBackground,
                Padding = new Padding(0)
            };
            scopeGroup.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            scopeGroup.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var scopeLabel = new Label
            {
                AutoSize = true,
                Text = "Scope",
                Margin = new Padding(0, 0, 0, 4),
                ForeColor = textColor,
                BackColor = sectionBackground,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
            };
            scopeComboBox = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                IntegralHeight = false,
                DropDownHeight = 120
            };
            scopeComboBox.SelectedIndexChanged += (_, __) =>
            {
                if (suppressSelectionSync)
                {
                    return;
                }

                switch (scopeComboBox.SelectedItem as string)
                {
                    case "Device":
                        deviceScopeRadio.Checked = true;
                        break;
                    case "Named":
                        namedScopeRadio.Checked = true;
                        break;
                    default:
                        activeScopeRadio.Checked = true;
                        break;
                }

                UpdateControlState();
            };
            scopeGroup.Controls.Add(scopeLabel, 0, 0);
            scopeGroup.Controls.Add(scopeComboBox, 0, 1);
            topSelectorsLayout.Controls.Add(scopeGroup, 1, 0);

            scopeHintLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = mutedTextColor,
                Margin = new Padding(0),
                Padding = new Padding(0),
                Tag = "muted",
                BackColor = sectionBackground,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                AutoEllipsis = true
            };

            resultSummaryLabel = new TextBox
            {
                Visible = false,
                Dock = DockStyle.Fill,
                BackColor = inputBackground,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point)
            };
            var resultPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = sectionBackground,
                Margin = new Padding(0)
            };
            resultPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            resultPanel.Controls.Add(resultSummaryLabel, 0, 0);

            var infoGroup = new GroupBox
            {
                Text = "Result And Info",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                BackColor = sectionBackground,
                ForeColor = textColor,
                Padding = new Padding(10, 8, 10, 8),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
            };
            infoGroup.Controls.Add(resultPanel);

            cardsGrid.Controls.Add(leftTopCard, 0, 0);
            cardsGrid.Controls.Add(keyGroup, 1, 0);
            cardsGrid.Controls.Add(infoGroup, 0, 1);
            cardsGrid.Controls.Add(scopeHintLabel, 1, 1);

            topContentPanel.Controls.Add(cardsGrid);

            var bottomPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = appBackground,
                Margin = new Padding(0),
                MinimumSize = new Size(0, 160)
            };
            bottomPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bottomPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 6),
                BackColor = appBackground
            };

            executeButton = new Button
            {
                AutoSize = false,
                Width = 92,
                Height = 30,
                Text = "Execute",
                Margin = new Padding(0, 0, 8, 0)
            };
            StylePrimaryButton(executeButton);
            executeButton.Click += ExecuteButton_Click;

            verboseOutputCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = "Show verbose log",
                Margin = new Padding(0, 6, 10, 0),
                ForeColor = textColor,
                Tag = "checkbox",
                BackColor = appBackground
            };

            verboseCliCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = "Verbose CLI",
                Margin = new Padding(0, 6, 10, 0),
                ForeColor = textColor,
                Tag = "checkbox",
                BackColor = appBackground
            };

            darkModeCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = "Dark mode",
                Margin = new Padding(0, 6, 10, 0),
                ForeColor = textColor,
                Tag = "checkbox",
                BackColor = appBackground
            };
            darkModeCheckBox.CheckedChanged += (_, __) =>
            {
                SetTheme(darkModeCheckBox.Checked);
                ApplyTheme();
                SaveSettings();
            };

            var manualButton = new Button
            {
                AutoSize = false,
                Width = 96,
                Height = 30,
                Text = "Open Manual",
                Margin = new Padding(0, 0, 8, 0)
            };
            StyleSecondaryButton(manualButton);
            manualButton.Click += (_, __) => OpenRelativeFile("sakl_manual.pdf");

            var folderButton = new Button
            {
                AutoSize = false,
                Width = 96,
                Height = 30,
                Text = "Open Folder",
                Margin = new Padding(0, 0, 8, 0)
            };
            StyleSecondaryButton(folderButton);
            folderButton.Click += (_, __) => OpenFolder();

            buttonFlow.Controls.Add(executeButton);
            buttonFlow.Controls.Add(verboseOutputCheckBox);
            buttonFlow.Controls.Add(verboseCliCheckBox);
            buttonFlow.Controls.Add(darkModeCheckBox);
            buttonFlow.Controls.Add(manualButton);
            buttonFlow.Controls.Add(folderButton);
            bottomPanel.Controls.Add(buttonFlow, 0, 0);

            outputTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = outputBackground,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0)
            };
            bottomPanel.Controls.Add(outputTextBox, 0, 1);
            root.Controls.Add(bottomPanel);
            root.Controls.Add(topContentPanel);

            Controls.Add(root);
            LoadSettings();
            UpdateControlState();
            ApplyTheme();
            AppendOutput("Ready.");
        }

        private SaklAction CurrentAction
        {
            get
            {
                var selected = actionComboBox.SelectedItem as string;
                if (string.Equals(selected, "Read Status", StringComparison.Ordinal))
                {
                    return SaklAction.Read;
                }

                if (string.Equals(selected, "Zeroize", StringComparison.Ordinal))
                {
                    return SaklAction.Zeroize;
                }

                return SaklAction.Load;
            }
        }

        private SaklScope CurrentScope
        {
            get
            {
                var selected = scopeComboBox.SelectedItem as string;
                if (string.Equals(selected, "Device", StringComparison.Ordinal))
                {
                    return SaklScope.Device;
                }

                if (string.Equals(selected, "Named", StringComparison.Ordinal))
                {
                    return SaklScope.Named;
                }

                return SaklScope.Active;
            }
        }

        private void AddLabeledControl(TableLayoutPanel panel, string labelText, Control control, int column, int row)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = labelText,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 6, 3, 3),
                BackColor = panel.BackColor,
                ForeColor = Color.FromArgb(60, 67, 78)
            }, column, row);
            panel.Controls.Add(control, column + 1, row);
        }

        private void AddCompactLabeledControl(TableLayoutPanel panel, string labelText, Control control, int row)
        {
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = labelText,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 7, 6, 3),
                BackColor = sectionBackground,
                ForeColor = mutedTextColor
            }, 0, row);

            control.Margin = new Padding(0, 3, 0, 3);
            panel.Controls.Add(control, 1, row);
        }

        private void StyleSegmentRadio(RadioButton radioButton, int width)
        {
            radioButton.Appearance = Appearance.Button;
            radioButton.AutoSize = false;
            radioButton.Width = width;
            radioButton.Height = 30;
            radioButton.TextAlign = ContentAlignment.MiddleCenter;
            radioButton.FlatStyle = FlatStyle.Flat;
            radioButton.FlatAppearance.BorderColor = accentColor;
            radioButton.FlatAppearance.BorderSize = 1;
            radioButton.BackColor = sectionBackground;
            radioButton.ForeColor = textColor;
            radioButton.Margin = new Padding(0, 0, 8, 0);
            radioButton.Tag = "segment";
        }

        private static void StyleInput(Control control, Color textColor)
        {
            control.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            control.ForeColor = textColor;
            control.Margin = new Padding(0, 0, 0, 8);
        }

        private void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = accentColor;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        }

        private void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = accentColor;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = sectionBackground;
            button.ForeColor = accentColor;
            button.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
        }

        private void SetTheme(bool darkMode)
        {
            darkModeEnabled = darkMode;
            if (darkModeEnabled)
            {
                appBackground = Color.FromArgb(19, 24, 31);
                sectionBackground = appBackground;
                accentColor = Color.FromArgb(72, 170, 232);
                textColor = Color.FromArgb(232, 238, 245);
                mutedTextColor = Color.FromArgb(160, 173, 189);
                outputBackground = Color.FromArgb(22, 28, 36);
                inputBackground = Color.FromArgb(36, 44, 54);
            }
            else
            {
                appBackground = Color.FromArgb(206, 217, 229);
                sectionBackground = appBackground;
                accentColor = Color.FromArgb(17, 110, 161);
                textColor = Color.FromArgb(27, 35, 47);
                mutedTextColor = Color.FromArgb(79, 92, 109);
                outputBackground = Color.FromArgb(226, 236, 246);
                inputBackground = Color.FromArgb(236, 243, 249);
            }
        }

        private void ApplyTheme()
        {
            BackColor = appBackground;
            ApplyThemeToControl(this);
            outputTextBox.BackColor = outputBackground;
            outputTextBox.ForeColor = textColor;
            resultSummaryLabel.BackColor = inputBackground;
            resultSummaryLabel.ForeColor = textColor;
            verboseOutputCheckBox.ForeColor = textColor;
            verboseCliCheckBox.ForeColor = textColor;
            darkModeCheckBox.ForeColor = textColor;
            RefreshSegmentStyles();
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(settingsFilePath))
                {
                    return;
                }

                var lines = File.ReadAllLines(settingsFilePath);
                for (var index = 0; index < lines.Length; index++)
                {
                    var line = lines[index].Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }

                    var key = line.Substring(0, separatorIndex).Trim();
                    var value = line.Substring(separatorIndex + 1).Trim();
                    if (string.Equals(key, "dark_mode", StringComparison.OrdinalIgnoreCase))
                    {
                        var isDark = value == "1" ||
                                     value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                     value.Equals("yes", StringComparison.OrdinalIgnoreCase);
                        darkModeCheckBox.Checked = isDark;
                    }
                }
            }
            catch
            {
            }
        }

        private void SaveSettings()
        {
            try
            {
                var lines = new[]
                {
                    "# Software Authentication Keyloader GUI settings",
                    "dark_mode=" + (darkModeCheckBox.Checked ? "1" : "0")
                };
                File.WriteAllLines(settingsFilePath, lines);
            }
            catch
            {
            }
        }

        private void ApplyThemeToControl(Control control)
        {
            if (control is GroupBox)
            {
                control.BackColor = sectionBackground;
                control.ForeColor = textColor;
            }
            else if (control is FlowLayoutPanel || control is TableLayoutPanel || control is Panel)
            {
                control.BackColor = control == this ? appBackground : InferPanelBackground(control);
            }
            else if (control is Label)
            {
                var label = (Label)control;
                if (Equals(label.Tag, "title"))
                {
                    label.ForeColor = accentColor;
                    label.BackColor = appBackground;
                }
                else if (Equals(label.Tag, "muted"))
                {
                    label.ForeColor = mutedTextColor;
                    label.BackColor = sectionBackground;
                }
                else
                {
                    var isSectionLabel = control.Parent is GroupBox || (control.Parent != null && control.Parent.BackColor == sectionBackground);
                    label.ForeColor = isSectionLabel ? mutedTextColor : textColor;
                    label.BackColor = control.Parent != null ? control.Parent.BackColor : appBackground;
                }
            }
            else if (control is TextBox || control is NumericUpDown || control is ComboBox)
            {
                control.BackColor = inputBackground;
                control.ForeColor = textColor;
            }
            else if (control is CheckBox)
            {
                var checkBox = (CheckBox)control;
                checkBox.BackColor = control.Parent != null ? control.Parent.BackColor : appBackground;
                checkBox.ForeColor = textColor;
            }
            else if (control is Button)
            {
                var button = (Button)control;
                if (button == executeButton)
                {
                    StylePrimaryButton(button);
                }
                else
                {
                    StyleSecondaryButton(button);
                }
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child);
            }
        }

        private Color InferPanelBackground(Control control)
        {
            return control.Parent == this || control.Parent == null ? appBackground : control.Parent.BackColor;
        }

        private void RefreshSegmentStyles()
        {
            ApplySegmentState(loadActionRadio);
            ApplySegmentState(readActionRadio);
            ApplySegmentState(zeroizeActionRadio);
            ApplySegmentState(activeScopeRadio);
            ApplySegmentState(deviceScopeRadio);
            ApplySegmentState(namedScopeRadio);
        }

        private void ApplySegmentState(RadioButton radioButton)
        {
            radioButton.FlatAppearance.BorderColor = accentColor;
            radioButton.BackColor = radioButton.Checked ? accentColor : inputBackground;
            radioButton.ForeColor = radioButton.Checked ? Color.White : textColor;
        }

        private void UpdateControlState()
        {
            var isLoad = CurrentAction == SaklAction.Load;
            var isRead = CurrentAction == SaklAction.Read;
            var isNamed = CurrentScope == SaklScope.Named;

            keyTextBox.Enabled = isLoad;
            generateKeyButton.Enabled = isLoad;

            activeScopeRadio.Visible = true;
            deviceScopeRadio.Visible = CurrentAction == SaklAction.Zeroize;
            namedScopeRadio.Visible = !isRead;

            if (isRead)
            {
                deviceScopeRadio.Enabled = true;
                activeScopeRadio.Enabled = true;
                namedScopeRadio.Enabled = false;
                if (namedScopeRadio.Checked)
                {
                    activeScopeRadio.Checked = true;
                }
            }
            else if (isLoad)
            {
                deviceScopeRadio.Enabled = false;
                activeScopeRadio.Enabled = true;
                namedScopeRadio.Enabled = true;
                if (deviceScopeRadio.Checked)
                {
                    activeScopeRadio.Checked = true;
                }
            }
            else
            {
                deviceScopeRadio.Enabled = true;
                activeScopeRadio.Enabled = true;
                namedScopeRadio.Enabled = true;
            }

            var enableNamedFields = isNamed && namedScopeRadio.Enabled;
            wacnTextBox.Enabled = enableNamedFields;
            systemTextBox.Enabled = enableNamedFields;
            unitTextBox.Enabled = enableNamedFields;

            if (isRead)
            {
                scopeHintLabel.Text = "Read: Active only. Shows metadata, not the secret key.";
            }
            else if (isLoad)
            {
                scopeHintLabel.Text = "Load: Active or Named. WACN/System hex, Unit decimal.";
            }
            else
            {
                scopeHintLabel.Text = "Zeroize: Active, Device, or Named. WACN/System hex, Unit decimal.";
            }

            suppressSelectionSync = true;
            actionComboBox.SelectedItem = isRead ? "Read Status" : (CurrentAction == SaklAction.Zeroize ? "Zeroize" : "Load");

            var desiredScope = CurrentScope == SaklScope.Device
                ? "Device"
                : (CurrentScope == SaklScope.Named ? "Named" : "Active");
            scopeComboBox.Items.Clear();
            scopeComboBox.Items.Add("Active");
            if (!isRead)
            {
                scopeComboBox.Items.Add("Named");
            }

            if (CurrentAction == SaklAction.Zeroize)
            {
                scopeComboBox.Items.Insert(1, "Device");
            }

            scopeComboBox.SelectedItem = scopeComboBox.Items.Contains(desiredScope) ? desiredScope : "Active";
            suppressSelectionSync = false;

            RefreshSegmentStyles();
        }

        private async void ExecuteButton_Click(object sender, EventArgs e)
        {
            string validationError;
            string arguments;
            try
            {
                arguments = BuildArguments(out validationError);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.IsNullOrEmpty(validationError))
            {
                MessageBox.Show(this, validationError, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            executeButton.Enabled = false;
            loadActionRadio.Enabled = false;
            readActionRadio.Enabled = false;
            zeroizeActionRadio.Enabled = false;
            resultSummaryLabel.Visible = false;
            AppendOutput(string.Empty);
            AppendOutput("Command: sakl.exe " + arguments);
            AppendOutput("Running...");

            try
            {
                var result = await Task.Run(() => RunCliWithRetry(arguments));
                UpdateResultSummary(result.Output, result.ExitCode == 0);
                AppendOutput(FilterOutput(result.Output).TrimEnd());

                if (result.ExitCode == 0)
                {
                    SetOperationStatus(true);
                    AppendOutput("Completed successfully.");
                }
                else
                {
                    SetOperationStatus(false);
                    AppendOutput("Process exited with code " + result.ExitCode.ToString(CultureInfo.InvariantCulture) + ".");
                }
            }
            catch (Exception ex)
            {
                SetOperationStatus(false);
                AppendOutput("Operation Failed - " + ex.Message);
                MessageBox.Show(this, ex.Message, "Operation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                executeButton.Enabled = true;
                loadActionRadio.Enabled = true;
                readActionRadio.Enabled = true;
                zeroizeActionRadio.Enabled = true;
            }
        }

        private string BuildArguments(out string validationError)
        {
            validationError = null;

            var cliPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sakl.exe");
            if (!File.Exists(cliPath))
            {
                validationError = "sakl.exe not found in this folder.";
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(ipTextBox.Text))
            {
                validationError = "IP address is required.";
                return string.Empty;
            }

            var args = new List<string>
            {
                "--ip", QuoteArgument(ipTextBox.Text.Trim()),
                "--port", portNumeric.Value.ToString(CultureInfo.InvariantCulture),
                "--timeout", timeoutNumeric.Value.ToString(CultureInfo.InvariantCulture)
            };

            if (verboseCliCheckBox.Checked)
            {
                args.Add("--verbose");
            }

            switch (CurrentAction)
            {
                case SaklAction.Load:
                    args.Add("--load");
                    break;
                case SaklAction.Read:
                    args.Add("--read");
                    break;
                case SaklAction.Zeroize:
                    args.Add("--zeroize");
                    break;
            }

            switch (CurrentScope)
            {
                case SaklScope.Active:
                    args.Add("--active");
                    break;
                case SaklScope.Device:
                    args.Add("--device");
                    break;
                case SaklScope.Named:
                    args.Add("--named");
                    if (string.IsNullOrWhiteSpace(wacnTextBox.Text) ||
                        string.IsNullOrWhiteSpace(systemTextBox.Text) ||
                        string.IsNullOrWhiteSpace(unitTextBox.Text))
                    {
                        validationError = "WACN, System, and Unit are required for named scope.";
                        return string.Empty;
                    }

                    args.Add("--wacn");
                    args.Add(wacnTextBox.Text.Trim());
                    args.Add("--system");
                    args.Add(systemTextBox.Text.Trim());
                    args.Add("--unit");
                    args.Add(ConvertDecimalUnitToHex(unitTextBox.Text.Trim(), out validationError));
                    if (!string.IsNullOrEmpty(validationError))
                    {
                        return string.Empty;
                    }
                    break;
            }

            if (CurrentAction == SaklAction.Load)
            {
                var key = GetNormalizedKey();
                if (string.IsNullOrWhiteSpace(key))
                {
                    validationError = "Key is required for load.";
                    return string.Empty;
                }

                if (key.Length != 32)
                {
                    validationError = "Key must be 32 hex characters.";
                    return string.Empty;
                }

                args.Add("--key");
                args.Add(key);
            }

            return string.Join(" ", args.ToArray());
        }

        private string GetNormalizedKey()
        {
            var builder = new StringBuilder(32);
            for (var index = 0; index < keyByteTextBoxes.Length; index++)
            {
                var text = keyByteTextBoxes[index].Text.Trim();
                for (var charIndex = 0; charIndex < text.Length && builder.Length < 32; charIndex++)
                {
                    var ch = text[charIndex];
                    if (Uri.IsHexDigit(ch))
                    {
                        builder.Append(char.ToUpperInvariant(ch));
                    }
                }
            }

            return builder.ToString();
        }

        private void NormalizeKeyByteText(int index)
        {
            var box = keyByteTextBoxes[index];
            var original = box.Text;
            var builder = new StringBuilder(2);
            for (var charIndex = 0; charIndex < original.Length && builder.Length < 2; charIndex++)
            {
                var ch = original[charIndex];
                if (Uri.IsHexDigit(ch))
                {
                    builder.Append(char.ToUpperInvariant(ch));
                }
            }

            var normalized = builder.ToString();
            if (!string.Equals(original, normalized, StringComparison.Ordinal))
            {
                var selectionStart = box.SelectionStart;
                box.Text = normalized;
                box.SelectionStart = Math.Min(selectionStart, box.TextLength);
            }

            if (normalized.Length == 2 && index + 1 < keyByteTextBoxes.Length && !keyByteTextBoxes[index + 1].Focused)
            {
                keyByteTextBoxes[index + 1].Focus();
            }
        }

        private void HandleKeyByteKeyDown(int index, KeyEventArgs e)
        {
            if ((e.Control && e.KeyCode == Keys.V) || (e.Shift && e.KeyCode == Keys.Insert))
            {
                PasteKeyFromClipboard(index);
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.Back)
            {
                var box = keyByteTextBoxes[index];
                if (box.SelectionLength == 0 && box.SelectionStart == 0 && string.IsNullOrEmpty(box.Text) && index > 0)
                {
                    var previousBox = keyByteTextBoxes[index - 1];
                    previousBox.Focus();
                    previousBox.SelectionStart = 0;
                    previousBox.SelectionLength = previousBox.TextLength;
                    e.SuppressKeyPress = true;
                }

                return;
            }

            if (e.KeyCode == Keys.Left && index > 0 && keyByteTextBoxes[index].SelectionStart == 0)
            {
                var previousBox = keyByteTextBoxes[index - 1];
                previousBox.Focus();
                previousBox.SelectionStart = 0;
                previousBox.SelectionLength = previousBox.TextLength;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.Right && index + 1 < keyByteTextBoxes.Length && keyByteTextBoxes[index].SelectionStart == keyByteTextBoxes[index].TextLength)
            {
                var nextBox = keyByteTextBoxes[index + 1];
                nextBox.Focus();
                nextBox.SelectionStart = 0;
                e.SuppressKeyPress = true;
            }
        }

        private void PasteKeyFromClipboard(int startIndex)
        {
            if (!Clipboard.ContainsText(TextDataFormat.UnicodeText) &&
                !Clipboard.ContainsText(TextDataFormat.Text))
            {
                return;
            }

            var pasted = Clipboard.ContainsText(TextDataFormat.UnicodeText)
                ? Clipboard.GetText(TextDataFormat.UnicodeText)
                : Clipboard.GetText(TextDataFormat.Text);
            var builder = new StringBuilder(32);
            for (var index = 0; index < pasted.Length && builder.Length < 32; index++)
            {
                var ch = pasted[index];
                if (Uri.IsHexDigit(ch))
                {
                    builder.Append(char.ToUpperInvariant(ch));
                }
            }

            var normalized = builder.ToString();
            if (normalized.Length == 0)
            {
                return;
            }

            if (startIndex <= 0)
            {
                SetNormalizedKey(normalized);
                var focusIndex = Math.Min((normalized.Length + 1) / 2, keyByteTextBoxes.Length - 1);
                keyByteTextBoxes[focusIndex].Focus();
                keyByteTextBoxes[focusIndex].SelectionStart = keyByteTextBoxes[focusIndex].TextLength;
                return;
            }

            var chunkIndex = 0;
            for (var index = startIndex; index < keyByteTextBoxes.Length && chunkIndex < normalized.Length; index++)
            {
                var remaining = normalized.Length - chunkIndex;
                var chunkLength = Math.Min(2, remaining);
                keyByteTextBoxes[index].Text = normalized.Substring(chunkIndex, chunkLength);
                chunkIndex += chunkLength;
            }
        }

        private static string ConvertDecimalUnitToHex(string decimalUnit, out string validationError)
        {
            validationError = null;
            long unitValue;
            if (!long.TryParse(decimalUnit, NumberStyles.None, CultureInfo.InvariantCulture, out unitValue))
            {
                validationError = "Unit must be a decimal number in the GUI.";
                return string.Empty;
            }

            if (unitValue < 0)
            {
                validationError = "Unit must be a non-negative decimal number.";
                return string.Empty;
            }

            return unitValue.ToString("X", CultureInfo.InvariantCulture);
        }

        private void GenerateRandomKey()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var builder = new StringBuilder(32);
            for (var index = 0; index < bytes.Length; index++)
            {
                builder.Append(bytes[index].ToString("X2", CultureInfo.InvariantCulture));
            }

            SetNormalizedKey(builder.ToString());
        }

        private void SetNormalizedKey(string key)
        {
            for (var index = 0; index < keyByteTextBoxes.Length; index++)
            {
                var offset = index * 2;
                keyByteTextBoxes[index].Text = offset + 2 <= key.Length
                    ? key.Substring(offset, 2)
                    : string.Empty;
            }
        }

        private ProcessResult RunCli(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sakl.exe"),
                Arguments = arguments,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();

                process.Start();
                stdout.Append(process.StandardOutput.ReadToEnd());
                stderr.Append(process.StandardError.ReadToEnd());
                process.WaitForExit();

                var output = stdout.ToString();
                var error = stderr.ToString();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    output = string.IsNullOrWhiteSpace(output) ? error : output + Environment.NewLine + error;
                }

                return new ProcessResult(process.ExitCode, string.IsNullOrWhiteSpace(output) ? "(no output)" : output);
            }
        }

        private ProcessResult RunCliWithRetry(string arguments)
        {
            var firstResult = RunCli(arguments);
            if (CurrentAction != SaklAction.Load)
            {
                return firstResult;
            }

            if (firstResult.ExitCode == 0)
            {
                return firstResult;
            }

            if (firstResult.Output.IndexOf("InvalidSubscriberId (0x0F)", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return firstResult;
            }

            System.Threading.Thread.Sleep(350);
            var secondResult = RunCli(arguments);
            var combinedOutput = firstResult.Output +
                                 Environment.NewLine +
                                 Environment.NewLine +
                                 "[Automatic retry after InvalidSubscriberId (0x0F)]" +
                                 Environment.NewLine +
                                 secondResult.Output;
            return new ProcessResult(secondResult.ExitCode, combinedOutput);
        }

        private string FilterOutput(string output)
        {
            if (verboseOutputCheckBox.Checked || string.IsNullOrWhiteSpace(output))
            {
                return output;
            }

            var reader = new StringReader(output);
            var builder = new StringBuilder();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (IsVerboseLine(line))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(line);
            }

            return builder.Length == 0 ? output : builder.ToString();
        }

        private void UpdateResultSummary(string output, bool succeeded)
        {
            var summary = TryBuildResultSummary(output, succeeded);
            if (string.IsNullOrWhiteSpace(summary))
            {
                resultSummaryLabel.Visible = false;
                resultSummaryLabel.Text = string.Empty;
                return;
            }

            resultSummaryLabel.Text = (succeeded ? "Operation Successful" : "Operation Failed")
                + Environment.NewLine
                + summary;
            resultSummaryLabel.Visible = true;
        }

        private void SetOperationStatus(bool succeeded)
        {
            resultSummaryLabel.BackColor = succeeded
                ? (darkModeEnabled ? Color.FromArgb(32, 82, 56) : Color.FromArgb(212, 244, 222))
                : (darkModeEnabled ? Color.FromArgb(98, 42, 42) : Color.FromArgb(251, 221, 221));
            resultSummaryLabel.ForeColor = succeeded
                ? (darkModeEnabled ? Color.FromArgb(214, 255, 226) : Color.FromArgb(28, 92, 46))
                : (darkModeEnabled ? Color.FromArgb(255, 223, 223) : Color.FromArgb(140, 32, 32));
            resultSummaryLabel.Visible = true;
        }

        private string TryBuildResultSummary(string output, bool succeeded)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return string.Empty;
            }

            if (CurrentAction == SaklAction.Read)
            {
                return succeeded ? TryBuildReadSummary(output) : "Read Result" + Environment.NewLine + "Read failed. Check the console log below for the radio response.";
            }

            if (CurrentAction == SaklAction.Zeroize)
            {
                return TryBuildZeroizeSummary(output, succeeded);
            }

            return succeeded
                ? "Load Result" + Environment.NewLine + "Authentication key loaded successfully."
                : "Load Result" + Environment.NewLine + "Load failed. Check the console log below for the radio response.";
        }

        private static string TryBuildReadSummary(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return string.Empty;
            }

            string wacn = null;
            string system = null;
            string unit = null;
            string keyAssigned = null;
            string isActive = null;

            using (var reader = new StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith("WACN:", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var rawPart in parts)
                    {
                        var part = rawPart.Trim();
                        if (part.StartsWith("WACN:", StringComparison.OrdinalIgnoreCase))
                        {
                            wacn = part.Substring(5).Trim();
                        }
                        else if (part.StartsWith("System:", StringComparison.OrdinalIgnoreCase))
                        {
                            system = part.Substring(7).Trim();
                        }
                        else if (part.StartsWith("Unit:", StringComparison.OrdinalIgnoreCase))
                        {
                            unit = part.Substring(5).Trim();
                        }
                        else if (part.StartsWith("Key Assigned:", StringComparison.OrdinalIgnoreCase))
                        {
                            keyAssigned = part.Substring(13).Trim();
                        }
                        else if (part.StartsWith("Is Active:", StringComparison.OrdinalIgnoreCase))
                        {
                            isActive = part.Substring(10).Trim();
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(wacn) &&
                string.IsNullOrWhiteSpace(system) &&
                string.IsNullOrWhiteSpace(unit) &&
                string.IsNullOrWhiteSpace(keyAssigned) &&
                string.IsNullOrWhiteSpace(isActive))
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Read Result");
            builder.AppendLine("WACN: " + (wacn ?? "(unknown)"));
            builder.AppendLine("System: " + (system ?? "(unknown)"));
            builder.AppendLine("Unit: " + (unit ?? "(unknown)"));
            builder.AppendLine("Key Assigned: " + (keyAssigned ?? "(unknown)"));
            builder.Append("Is Active: " + (isActive ?? "(unknown)"));
            return builder.ToString();
        }

        private static string TryBuildZeroizeSummary(string output, bool succeeded)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return string.Empty;
            }

            if (!succeeded)
            {
                if (output.IndexOf("NegativeAcknowledgement", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    output.IndexOf("InvalidMessageId (0x03)", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    output.IndexOf("DeleteAuthenticationKeyCommand (0x2A)", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Zeroize Result" + Environment.NewLine +
                           "Zeroize failed because the radio rejected DeleteAuthenticationKeyCommand (0x2A) with InvalidMessageId (0x03). This device or firmware likely does not support remote zeroize through sakl.exe.";
                }

                return "Zeroize Result" + Environment.NewLine +
                       "Zeroize failed. Read works on this device, but delete appears unsupported, denied, or rejected by the radio. Enable Verbose CLI for deeper protocol details.";
            }

            var statusLine = string.Empty;
            using (var reader = new StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.IndexOf("NumKeysDeleted", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("DeleteAuthenticationKeyResponse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("CommandWasPerformed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("CommandCouldNotBePerformed", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        statusLine = line.Trim();
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(statusLine))
            {
                statusLine = "Zeroize command sent. Check the console output below for the device response.";
            }

            return "Zeroize Result" + Environment.NewLine + statusLine;
        }

        private static bool IsVerboseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            return line.StartsWith("named:", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("wacn:", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("system:", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("unit:", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("key:", StringComparison.OrdinalIgnoreCase) ||
                   line.IndexOf("kmm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   line.StartsWith("ip address:", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("sending ", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("received ", StringComparison.OrdinalIgnoreCase) ||
                   line.StartsWith("response ", StringComparison.OrdinalIgnoreCase);
        }

        private void OpenRelativeFile(string fileName)
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (!File.Exists(fullPath))
            {
                MessageBox.Show(this, fileName + " not found in this folder.", "Missing File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            });
        }

        private void OpenFolder()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = true
            });
        }

        private void AppendOutput(string text)
        {
            if (outputTextBox.TextLength > 0)
            {
                outputTextBox.AppendText(Environment.NewLine);
            }

            outputTextBox.AppendText(text);
        }

        private static string QuoteArgument(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            return value.IndexOf(' ') >= 0 ? "\"" + value.Replace("\"", "\\\"") + "\"" : value;
        }

        private sealed class ProcessResult
        {
            public ProcessResult(int exitCode, string output)
            {
                ExitCode = exitCode;
                Output = output;
            }

            public int ExitCode { get; private set; }

            public string Output { get; private set; }
        }
    }
}
