﻿using IniParserLTK;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace WolvenKit
{
    public partial class SettingsDialogView : XtraForm
    {
        public const string wcc_sha256 = "fb20d7aa45b95446baac9b376533b06b86add732cbe40fd0620e4a4feffae47b";
        public const string wcc_sha256_patched = "275faa214c6263287deea47ddbcd7afcf6c2503a76ff57f2799bc158f5af7c5d";
        public const string wcc_sha256_patched2 = "104f50142fde883337d332d319d205701e8a302197360f5237e6bb426984212a";
        public string wccLiteexe = string.Empty;
        public string witcherexe = string.Empty;

        public SettingsDialogView()
        {
            InitializeComponent();
            var config = MainController.Get().Configuration;
            txExecutablePath.Text = config.ExecutablePath;
            txTextLanguage.Text = config.TextLanguage;
            txVoiceLanguage.Text = config.VoiceLanguage;
            txWCC_Lite.Text = config.WccLite;
            exeSearcherSlave.RunWorkerAsync();
            btSave.Enabled =
                File.Exists(txWCC_Lite.Text) && Path.GetExtension(txWCC_Lite.Text) == ".exe" &&
                txWCC_Lite.Text.Contains("wcc_lite.exe") && File.Exists(txExecutablePath.Text) &&
                Path.GetExtension(txExecutablePath.Text) == ".exe" && txExecutablePath.Text.Contains("witcher3.exe");
        }

        private void btnBrowseExe_Click(object sender, EventArgs e)
        {
            var dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Title = "Select Witcher 3 Executable.";
            dlg.FileName = txExecutablePath.Text;
            dlg.Filter = "witcher3.exe|witcher3.exe";
            if (dlg.ShowDialog(this) == DialogResult.OK) txExecutablePath.Text = dlg.FileName;
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            if (!File.Exists(txExecutablePath.Text))
            {
                DialogResult = DialogResult.None;
                txExecutablePath.Focus();
                MessageBox.Show("Invalid witcher3.exe path", "failed to save.");
                return;
            }

            if (!File.Exists(txWCC_Lite.Text))
            {
                DialogResult = DialogResult.None;
                txWCC_Lite.Focus();
                MessageBox.Show("Invalid wcc_lite.exe path", "failed to save.");
                return;
            }

            var config = MainController.Get().Configuration;
            config.ExecutablePath = txExecutablePath.Text;
            config.WccLite = txWCC_Lite.Text;
            config.TextLanguage = txTextLanguage.Text;
            config.VoiceLanguage = txVoiceLanguage.Text;
            config.Save();
            try
            {
                var ip = new IniParser(Path.Combine(MainController.Get().Configuration.GameRootDir,
                    "bin\\config\\base\\general.ini"));
                if (!ip.HasSection("General") || ip.GetSetting("General", "DBGConsoleOn", true) != "true")
                    if (MessageBox.Show(
                            "WolvenKit has detected that your game has the debug console disabled. It is a usefull tool when testing mods. Would you like it to be enabled?",
                            "Debug console enabling", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                        DialogResult.Yes)
                    {
                        ip.AddSetting("General", "DBGConsoleOn", "true");
                        ip.Save();
                    }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }

            try
            {
                using (var fs = new FileStream(txWCC_Lite.Text, FileMode.Open))
                using (var bw = new BinaryWriter(fs))
                {
                    var shawcc = SHA256.Create().ComputeHash(fs)
                        .Aggregate(string.Empty, (c, n) => c += n.ToString("x2"));
                    switch (shawcc)
                    {
                        case wcc_sha256:
                            {
                                if (MessageBox.Show(@"wcc_lite is a great tool by CD Projekt red but
due to some internal problems they didn't really have time to properly develop it.
Due to this the tool takes an age to start up since it is searching for a CD Projekt red mssql server.
WolvenKit can patch this with a method figured out by blobbins on the witcher 3 forums.
Would you like to perform this patch?", "wcc_lite faster patch", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                                    DialogResult.Yes)
                                {
                                    //We perform the patch
                                    bw.BaseStream.Seek(0x00713CD0, SeekOrigin.Begin);
                                    bw.Write(new byte[0xDD].Select(x => x = 0x90).ToArray());
                                }

                                //Recompute hash
                                fs.Seek(0, SeekOrigin.Begin);
                                shawcc = SHA256.Create().ComputeHash(fs)
                                    .Aggregate(string.Empty, (c, n) => c += n.ToString("x2"));
                                if (shawcc == wcc_sha256_patched)
                                    MessageBox.Show("Succesfully patched!", "Patch completed", MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                                else
                                    MessageBox.Show("Failed to patch! Please reinstall wcc_lite and try again",
                                        "Patch completed", MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                break;
                            }
                        case wcc_sha256_patched2:
                        case wcc_sha256_patched:
                            {
                                //Do nothing we are cool.
                                break;
                            }
                        default:
                            {
                                DialogResult = DialogResult.None;
                                txExecutablePath.Focus();
                                MessageBox.Show("Invalid wcc_lite.exe path you seem to have on older version",
                                    "failed to save.");
                                return;
                            }
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }


        private void btBrowseWCC_Lite_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select wcc_lite.exe.",
                FileName = txExecutablePath.Text,
                Filter = "wcc_lite.exe|wcc_lite.exe"
            };
            if (dlg.ShowDialog(this) == DialogResult.OK) txWCC_Lite.Text = dlg.FileName;
        }

        private void exeSearcherSlave_DoWork(object sender, DoWorkEventArgs e)
        {
            const string uninstallkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\";
            const string uninstallkey2 = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\";
            var w3 = string.Empty;
            var wcc = string.Empty;
            try
            {
                Parallel.ForEach(Registry.LocalMachine.OpenSubKey(uninstallkey)?.GetSubKeyNames(), item =>
                {
                    var programName = Registry.LocalMachine.OpenSubKey(uninstallkey + item)
                        ?.GetValue("DisplayName");
                    var installLocation = Registry.LocalMachine.OpenSubKey(uninstallkey + item)
                        ?.GetValue("InstallLocation");
                    if (programName != null && installLocation != null)
                    {
                        if (programName.ToString().Contains("Witcher 3 Mod Tools"))
                            wcc = Directory.GetFiles(installLocation.ToString(), "wcc_lite.exe",
                                SearchOption.AllDirectories).First();

                        if (programName.ToString().Contains("The Witcher 3 - Wild Hunt") ||
                            programName.ToString().Contains("The Witcher 3: Wild Hunt"))
                            w3 = Directory.GetFiles(installLocation.ToString(), "witcher3.exe",
                                SearchOption.AllDirectories).First();
                    }

                    exeSearcherSlave.ReportProgress(0, new Tuple<string, string, int, int>(w3, wcc, 0, 0));
                });
                Parallel.ForEach(Registry.LocalMachine.OpenSubKey(uninstallkey2)?.GetSubKeyNames(), item =>
                {
                    var programName = Registry.LocalMachine.OpenSubKey(uninstallkey2 + item)
                        ?.GetValue("DisplayName");
                    var installLocation = Registry.LocalMachine.OpenSubKey(uninstallkey2 + item)
                        ?.GetValue("InstallLocation");
                    if (programName != null && installLocation != null)
                    {
                        if (programName.ToString().Contains("Witcher 3 Mod Tools"))
                            wcc = Directory.GetFiles(installLocation.ToString(), "wcc_lite.exe",
                                SearchOption.AllDirectories).First();

                        if (programName.ToString().Contains("The Witcher 3 - Wild Hunt") ||
                            programName.ToString().Contains("The Witcher 3: Wild Hunt"))
                            w3 = Directory.GetFiles(installLocation.ToString(), "witcher3.exe",
                                SearchOption.AllDirectories).First();
                    }

                    exeSearcherSlave.ReportProgress(0, new Tuple<string, string, int, int>(w3, wcc, 0, 0));
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }

        private void exeSearcherSlave_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (File.Exists(witcherexe)) txExecutablePath.Text = witcherexe;
            if (File.Exists(wccLiteexe)) txWCC_Lite.Text = wccLiteexe;
        }

        private void exeSearcherSlave_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var report = e.UserState as Tuple<string, string, int, int>;
            witcherexe = report?.Item1;
            wccLiteexe = report?.Item2;
        }

        private void txWCC_Lite_TextChanged(object sender, EventArgs e)
        {
            var path = txWCC_Lite.Text;
            if (File.Exists(path) && Path.GetExtension(path) == ".exe" && path.Contains("wcc_lite.exe"))
            {
                WCCexeTickLBL.Text = "✓";
                WCCexeTickLBL.ForeColor = Color.Green;
            }
            else
            {
                WCCexeTickLBL.Text = "X";
                WCCexeTickLBL.ForeColor = Color.Red;
            }

            btSave.Enabled =
                File.Exists(txWCC_Lite.Text) && Path.GetExtension(txWCC_Lite.Text) == ".exe" &&
                txWCC_Lite.Text.Contains("wcc_lite.exe") && File.Exists(txExecutablePath.Text) &&
                Path.GetExtension(txExecutablePath.Text) == ".exe" && txExecutablePath.Text.Contains("witcher3.exe");
        }

        private void txExecutablePath_TextChanged(object sender, EventArgs e)
        {
            var path = txExecutablePath.Text;
            if (File.Exists(path) && Path.GetExtension(path) == ".exe" && path.Contains("witcher3.exe"))
            {
                W3exeTickLBL.Text = "✓";
                W3exeTickLBL.ForeColor = Color.Green;
            }
            else
            {
                W3exeTickLBL.Text = "X";
                W3exeTickLBL.ForeColor = Color.Red;
            }

            btSave.Enabled =
                File.Exists(txWCC_Lite.Text) && Path.GetExtension(txWCC_Lite.Text) == ".exe" &&
                txWCC_Lite.Text.Contains("wcc_lite.exe") && File.Exists(txExecutablePath.Text) &&
                Path.GetExtension(txExecutablePath.Text) == ".exe" && txExecutablePath.Text.Contains("witcher3.exe");
        }
    }
}