﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace WolvenKit.Views
{
    public partial class BuildSettingsView : XtraForm
    {
        public BuildSettingsView()
        {
            InitializeComponent();
            Shown += OnShown;
            if (MainController.Get().Window.ActiveMod.Files.Any(x => x.EndsWith(".xbm")))
                texturecachecCHB.Checked = true;
            if (MainController.Get().Window.ActiveMod.Files.Any(x => x.EndsWith(".ws")))
                scriptsCHB.Checked = true;
            if (MainController.Get().Window.ActiveMod.Files.Any(x => x.EndsWith(".wem") || x.EndsWith(".bnk")))
                soundCHB.Checked = true;
            if (Directory.Exists(MainController.Get().Window.ActiveMod.ProjectDirectory + "\\strings") && Directory
                    .GetFiles(MainController.Get().Window.ActiveMod.ProjectDirectory + "\\strings")
                    .Any(x => x.EndsWith(".w3strings")))
                stringsCHB.Checked = true;
        }

        private void OnShown(object sender, EventArgs e)
        {
            simpleButtonOk.Focus();
        }

        public bool PackBundles => bundlesCHB.Checked;
        public bool GenMetadata => metadatastoreCHB.Checked;
        public bool GenTexCache => texturecachecCHB.Checked;
        public bool GenCollCache => collisionCacheCHB.Checked;
        public bool Scripts => scriptsCHB.Checked;
        public bool Sound => soundCHB.Checked;
        public bool Strings => stringsCHB.Checked;

        private void simpleButtonOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void simpleButtonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}