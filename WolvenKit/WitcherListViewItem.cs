﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WolvenKit.Common;

namespace WolvenKit
{
    public class WitcherListViewItem : ListViewItem, ICloneable
    {
        public bool IsDirectory { get; set; }
        public WitcherTreeNode Node { get; set; }
        public string FullPath { get; set; }

        public WitcherListViewItem() { }

        public WitcherListViewItem(IWitcherFile wf)
        {
            IsDirectory = false;
            Node = new WitcherTreeNode();
            Node.Name = Path.Combine("Root", wf.Bundle.TypeName, Path.GetDirectoryName(wf.Name));
            FullPath = wf.Name;
            this.Text = wf.Name;
        }

        public string ExplorerPath
        {
            get
            {
                return Path.Combine(Node.FullPath, Path.GetFileName(FullPath));
            }
        }

        public override object Clone()
        {
            var c = (WitcherListViewItem)this.MemberwiseClone();
            c.IsDirectory = IsDirectory;
            c.Node = Node;
            c.FullPath = FullPath;
            return c;
        }
    }
}
