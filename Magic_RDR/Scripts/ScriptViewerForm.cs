using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Magic_RDR.Application;
using Magic_RDR.RPF;
using static Magic_RDR.RPF6.RPF6TOC;

namespace Magic_RDR
{
    public partial class ScriptViewerForm : Form
    {
        private IOReader Reader;
        private FileEntry FileEntry;
        private string FileName = "";
        private string TempFileName;

        public static object ThreadLock;
        public static string DecompiledCode;
        public static bool ShowRawDisassembly = false;

        public ScriptViewerForm(TOCSuperEntry entry)
        {
            InitializeComponent();
            FileEntry = entry.Entry.AsFile;
            btnShowRawDisassembly.Checked = ShowRawDisassembly;
            btnShowNativeNamespaces.Checked = NativeHashDB.ShowNativeNamespace;

            ThreadLock = new object();
            Text = string.Format("MagicRDR - Script Viewer [{0}]", entry.Entry.Name);
            FileName = entry.Entry.Name;

            RPFFile.RPFIO.Position = FileEntry.GetOffset();

            byte[] fileData = ResourceUtils.ResourceInfo.GetDataFromResourceBytes(RPFFile.RPFIO.ReadBytes(FileEntry.SizeInArchive));
            Reader = new IOReader(new MemoryStream(fileData), (AppGlobals.Platform == AppGlobals.PlatformEnum.Switch) ? IOReader.Endian.Little : IOReader.Endian.Big);

            InitForm();
        }

        void InitForm()
        {
            Reader.BaseStream.Seek(FileEntry.FlagInfo.RSC85_ObjectStart, SeekOrigin.Begin);

            ScriptFile script = new ScriptFile(Reader, FileEntry);

			DecompiledCode = script.ReadMainStructure();

			try
            {
                textBox.Text = DecompiledCode;
            }
            catch
            {
                MessageBox.Show("This script is too big. It'll now be shown as plain text...", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox.Text = "";
                TempFileName = Path.GetTempFileName();
                File.WriteAllText(TempFileName, DecompiledCode);
                textBox.OpenBindingFile(TempFileName, Encoding.UTF8);
            }
            charCountLabel.Text = string.Format("{0} characters, {1} lines", textBox.Text.Length, textBox.LinesCount);
            zoomLabel.Text = string.Format("Zoom {0}%", textBox.Zoom);
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                MessageBox.Show("There's nothing to export", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Export";
            dialog.Filter = "C Source Code|*.c";
            dialog.FileName = FileName.Replace(".xsc", ".c").Replace(".wsc", ".c");

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dialog.FileName, textBox.Text);
                MessageBox.Show("Successfully exported !", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void textBox_TextChanged(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            charCountLabel.Text = string.Format("{0} characters, {1} lines", textBox.Text.Length, textBox.LinesCount);
        }

        private void zoomLabel_Click(object sender, EventArgs e)
        {
            textBox.Zoom = 100;
        }

        private void textBox_ZoomChanged(object sender, EventArgs e)
        {
            zoomLabel.Text = string.Format("Zoom {0}%", textBox.Zoom);
        }

        private void ScriptViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (TempFileName == string.Empty)
                return;

            try { textBox.CloseBindingFile(); }
            catch { }
        }

        private void btnShowRawDisassembly_Click(object sender, EventArgs e)
        {
            ShowRawDisassembly = !ShowRawDisassembly;
            btnShowRawDisassembly.Checked = ShowRawDisassembly;
            InitForm();
        }

        private void btnShowNativeNamespaces_Click(object sender, EventArgs e)
        {
            NativeHashDB.ShowNativeNamespace = !NativeHashDB.ShowNativeNamespace;
            btnShowNativeNamespaces.Checked = NativeHashDB.ShowNativeNamespace;

            var curScroll = textBox.VerticalScroll.Value;
            InitForm();
            textBox.VerticalScroll.Value = curScroll;
            textBox.UpdateScrollbars();
        }
    }
}
