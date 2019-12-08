﻿using BrightIdeasSoftware;
using DevExpress.XtraEditors;
using System;
using System.Linq;
using System.Windows.Forms;
using WolvenKit.CR2W;

namespace WolvenKit
{
    public partial class ChunkListViewer : XtraUserControl
    {
        private CR2WFile file;

        public ChunkListViewer()
        {
            InitializeComponent();
            limitTB.Enabled = limitCB.Checked;
            listView.ItemSelectionChanged += chunkListView_ItemSelectionChanged;
        }

        public CR2WFile File
        {
            get => file;
            set
            {
                file = value;
                UpdateList();
            }
        }

        public event EventHandler<SelectChunkArgs> OnSelectChunk;

        private void UpdateList(string keyword = "")
        {
            var limit = -1;
            if (limitCB.Checked) int.TryParse(limitTB.Text, out limit);
            if (File == null)
                return;
            if (!string.IsNullOrEmpty(keyword))
            {
                if (limit != -1)
                    listView.Objects = File.chunks.Where(x => x.Name.ToUpper().Contains(searchTB.Text.ToUpper()))
                        .Take(limit);
                else
                    listView.Objects = File.chunks.Where(x => x.Name.ToUpper().Contains(searchTB.Text.ToUpper()));
            }
            else
            {
                listView.Objects = File.chunks;
            }
        }

        private void chunkListView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (OnSelectChunk != null && (CR2WChunk)listView.SelectedObject != null)
                OnSelectChunk(this, new SelectChunkArgs { Chunk = (CR2WChunk)listView.SelectedObject });
        }

        private void addChunkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new frmAddChunk();

            if (dlg.ShowDialog() == DialogResult.OK)
                try
                {
                    var chunk = File.CreateChunk(dlg.ChunkType);
                    listView.AddObject(chunk);

                    if (OnSelectChunk != null && chunk != null)
                        OnSelectChunk(this, new SelectChunkArgs { Chunk = chunk });
                }
                catch (InvalidChunkTypeException ex)
                {
                    MessageBox.Show(ex.Message, "Error adding chunk.");
                }
        }

        private void deleteChunkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.SelectedObjects.Count == 0)
                return;

            if (MessageBox.Show(
                    "Are you sure you want to delete the selected chunk(s)? \n\n NOTE: Any pointers or handles to these chunks will NOT be deleted.",
                    "Confirmation", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                var selected = listView.SelectedObjects;
                foreach (var obj in selected) File.RemoveChunk((CR2WChunk)obj);

                listView.RemoveObjects(selected);
                listView.UpdateObjects(File.chunks);
            }
        }

        private void copyChunkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyChunks();
        }

        public void CopyChunks()
        {
            Clipboard.Clear();
            var chunks = listView.SelectedObjects.Cast<CR2WChunk>().ToList();
            CopyController.ChunkList = chunks;
            pasteChunkToolStripMenuItem.Enabled = true;
        }

        private void pasteChunkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasteChunks();
        }

        public void PasteChunks()
        {
            var copiedChunks = CopyController.ChunkList;
            if (copiedChunks != null && copiedChunks.Count > 0)
                foreach (var chunk in copiedChunks)
                    try
                    {
                        var pastedChunk = CR2WCopyAction.CopyChunk(chunk, chunk.CR2WOwner);
                        listView.AddObject(pastedChunk);
                        OnSelectChunk?.Invoke(this, new SelectChunkArgs { Chunk = pastedChunk });
                        MainController.Get().ProjectStatus = "Chunk copied";
                    }
                    catch (InvalidChunkTypeException ex)
                    {
                        MessageBox.Show(ex.Message, @"Error adding chunk.");
                    }
        }

        private void searchBTN_Click(object sender, EventArgs e)
        {
            UpdateList(searchTB.Text);
        }

        private void resetBTN_Click(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void limitCB_CheckStateChanged(object sender, EventArgs e)
        {
            limitTB.Enabled = limitCB.Checked;
        }

        private void searchTB_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == (int)Keys.Enter)
                UpdateList(searchTB.Text);
        }

        private void listView_ItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            MainController.Get().ProjectUnsaved = true;
        }
    }
}