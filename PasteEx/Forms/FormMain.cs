﻿using PasteEx.Core;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PasteEx.Forms
{
    public partial class FormMain : Form
    {
        #region Init

        private static FormMain dialogue = null;

        private ClipboardData data;

        private string currentLocation;

        private string lastAutoGeneratedFileName;

        public string CurrentLocation
        {
            get
            {
                return currentLocation;
            }
            set
            {
                currentLocation = value.EndsWith("\\") ? value : value + "\\";
                tsslCurrentLocation.ToolTipText = currentLocation;
                tsslCurrentLocation.Text = GenerateDisplayLocation(currentLocation);
            }
        }

        public static FormMain GetInstance()
        {
            return dialogue;
        }

        public FormMain()
        {
            dialogue = this;
            InitializeComponent();
            CurrentLocation = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        public FormMain(string location)
        {
            dialogue = this;
            InitializeComponent();
            CurrentLocation = location;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            data = new ClipboardData(Clipboard.GetDataObject());
            data.SaveCompleted += () => Application.Exit(); // exit when save completed
            string[] extensions = data.Analyze();
            cboExtension.Items.AddRange(extensions);
            if (extensions.Length > 0)
            {
                cboExtension.Text = extensions[0] ?? "";
            }
            else
            {
                if (MessageBox.Show(this, Resources.Strings.TipAnalyzeFailed, Resources.Strings.Title,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    btnChooseLocation.Enabled = false;
                    btnSave.Enabled = false;
                    txtFileName.Enabled = false;
                    cboExtension.Enabled = false;
                    tsslCurrentLocation.Text = Resources.Strings.TxtCanOnlyUse;
                }
                else
                {
                    Environment.Exit(0);
                }

            }

            lastAutoGeneratedFileName = GenerateFileName(CurrentLocation, cboExtension.Text);
            txtFileName.Text = lastAutoGeneratedFileName;
        }
        #endregion

        #region Generate path
        public static string GenerateFileName(string folder, string extension)
        {
            string defaultFileName = "Clipboard_" + DateTime.Now.ToString("yyyyMMdd");
            string path = folder + defaultFileName + "." + extension;

            string result;
            string newFileName = defaultFileName;
            int i = 0;
            while (true)
            {
                if (File.Exists(path))
                {
                    newFileName = defaultFileName + " (" + ++i + ")";
                    path = folder + newFileName + "." + extension;
                }
                else
                {
                    result = newFileName;
                    break;
                }

                if (i > 300)
                {
                    result = "Default";
                    break;
                }
            }
            return result;
        }

        private string GenerateDisplayLocation(string location)
        {
            const int maxLength = 47;
            const string ellipsis = "...";

            int length = Encoding.Default.GetBytes(location).Length;
            if (length <= maxLength)
            {
                return location;
            }

            // short display location
            int i;
            byte[] b;
            int tail = 0;
            char[] tailChars = new char[location.Length];
            int k = 0;
            for (i = location.Length - 1; i >= 0; i--)
            {
                b = Encoding.Default.GetBytes(location[i].ToString());
                if (b.Length > 1)
                {
                    tail += 2;
                }
                else
                {
                    tail++;
                }
                tailChars[k++] = location[i];
                if (location[i] == '\\' && i != location.Length - 1)
                {
                    break;
                }
            }
            int head = maxLength - ellipsis.Length - tail;
            if (head >= 3)
            {
                // c:\xxx\xxx\xx...\xxxxx\
                StringBuilder sb = new StringBuilder();
                sb.Append(StrCut(location, head));
                sb.Append(ellipsis);
                string tailStr = "";
                for (i = tailChars.Length - 1; i >= 0; i--)
                {
                    if (tailChars[i] != '\0')
                    {
                        tailStr += tailChars[i];
                    }
                }
                sb.Append(tailStr);
                return sb.ToString();
            }
            else
            {
                // c:\xxx\xxx\xxxx\xxxxx...
                return StrCut(location, maxLength - ellipsis.Length) + ellipsis;
            }
        }

        private string StrCut(string str, int length)
        {
            int len = 0;
            byte[] b;
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                b = Encoding.Default.GetBytes(str[i].ToString());
                if (b.Length > 1)
                {
                    len += 2;
                }
                else
                {
                    len++;
                }

                if (len >= length)
                {
                    break;
                }
                sb.Append(str[i]);
            }

            return sb.ToString();
        }
        #endregion

        #region UI event
        private void btnSave_Click(object sender, EventArgs e)
        {
            btnChooseLocation.Enabled = false;
            btnSettings.Enabled = false;
            btnSave.Enabled = false;

            string location = CurrentLocation.EndsWith("\\") ? CurrentLocation : CurrentLocation + "\\";
            string path = location + txtFileName.Text + "." + cboExtension.Text;

            if (File.Exists(path))
            {
                DialogResult result = MessageBox.Show(String.Format(Resources.Strings.TipTargetFileExisted, path),
                    Resources.Strings.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    data.SaveAsync(path, cboExtension.Text);
                }
                else if (result == DialogResult.No)
                {
                    btnChooseLocation.Enabled = true;
                    btnSettings.Enabled = true;
                    btnSave.Enabled = true;
                    return;
                }
            }
            else
            {
                data.SaveAsync(path, cboExtension.Text);
            }
        }

        private void btnChooseLocation_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, Resources.Strings.TipPathNotNull,
                        Resources.Strings.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else
                {
                    CurrentLocation = dialog.SelectedPath;
                }
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            Button btnSender = (Button)sender;
            System.Drawing.Point ptLowerLeft = new System.Drawing.Point(0, btnSender.Height);
            ptLowerLeft = btnSender.PointToScreen(ptLowerLeft);
            contextMenuStripSetting.Show(ptLowerLeft);


        }

        private void monitorModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // dispose
            data = null;

            // init control properties
            autoToolStripMenuItem.Checked = Properties.Settings.Default.autoImageTofile;
            startMonitorToolStripMenuItem.Visible = false;
            stopMonitorToolStripMenuItem.Visible = true;

            // hide main window and display system tray icon
            dialogue.WindowState = FormWindowState.Minimized;
            dialogue.ShowInTaskbar = false;
            dialogue.Hide();
            dialogue.notifyIcon.Visible = true;

            ModeController.StartMonitorMode();
        }

        private void collectModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormCollection formCollection = new FormCollection();
            formCollection.Show();
        }

        private void settingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form f = FormSetting.GetInstance();
            f.ShowDialog();
            f.Activate();
        }

        private void cboExtension_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Re-Generate FileName
            if (lastAutoGeneratedFileName == txtFileName.Text)
            {
                lastAutoGeneratedFileName = GenerateFileName(CurrentLocation, cboExtension.Text);
                txtFileName.Text = lastAutoGeneratedFileName;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // (keyData == (Keys.Control | Keys.S))
            if (keyData == Keys.Enter)
            {
                btnSave_Click(null, null);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }


        public void ChangeTsslCurrentLocation(string str)
        {
            tsslCurrentLocation.Text = str;
        }

        private void autoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            autoToolStripMenuItem.Checked = !autoToolStripMenuItem.Checked;
            Properties.Settings.Default.autoImageTofile = autoToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeController.StopMonitorMode();
            this.Close();
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Form f = FormSetting.GetInstance();
            f.Show();
            f.Activate();
        }

        private void startMonitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClipboardMonitor.Start();
            startMonitorToolStripMenuItem.Visible = false;
            stopMonitorToolStripMenuItem.Visible = true;
        }

        private void stopMonitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClipboardMonitor.Stop();
            startMonitorToolStripMenuItem.Visible = true;
            stopMonitorToolStripMenuItem.Visible = false;
        }

        #endregion
    }
}