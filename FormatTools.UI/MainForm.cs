using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarkUI.Controls;
using DarkUI.Forms;
using DR2.Formats.BigFile;

namespace FormatTools.UI
{
    public partial class MainForm : DarkForm
    {
        public Config Configuration;

        public MainForm()
        {
            InitializeComponent();

            if (Config.Initialize(out Configuration))
            {
                label1.Text = "Game path is: " + Configuration.GamePath;
            }
            else
            {
                MessageBox.Show("how did you even fuck up this bad?");
                Environment.Exit(1);
            }

            PopulateTree();
        }

        class NodeTag
        {
            public bool Clickable { get; set; }
            public string FullPath { get; set; }
        }
        
        class ListTag
        {
            public NodeTag NodeTag { get; set; }
            public BigFile BigFile { get; set; }
            public Entry Entry { get; set; }
        }

        private void PopulateTree()
        {
            var basePath = Path.Combine(Configuration.GamePath, "data");
            var rootFiles = Directory.GetFiles(basePath);
            var rootDirs = Directory.GetDirectories(basePath);

            List<DarkTreeNode> extraNodes = new List<DarkTreeNode>();

            void RecursiveDirectoryAdd(DarkTreeNode n, string baseDir)
            {
                var dirs = Directory.GetDirectories(baseDir);

                foreach (var d in dirs)
                {
                    var child = new DarkTreeNode(Path.GetFileName(d));
                    child.Tag = new NodeTag() { Clickable = false, FullPath = d };

                    RecursiveDirectoryAdd(child, d);

                    n.Nodes.Add(child);
                }

                var files = Directory.GetFiles(baseDir);

                foreach (var f in files)
                {
                    var node = new DarkTreeNode(Path.GetFileName(f));
                    node.Tag = new NodeTag() { Clickable = false, FullPath = f };
                    n.Nodes.Add(node);
                }
            }

            foreach (var d in rootDirs)
            {
                var node = new DarkTreeNode(Path.GetFileName(d));
                node.Tag = new NodeTag() { Clickable = false, FullPath = d };

                RecursiveDirectoryAdd(node, d);

                extraNodes.Add(node);
            }

            foreach (var f in rootFiles)
            {
                var node = new DarkTreeNode(Path.GetFileName(f));
                node.Tag = new NodeTag() { Clickable = true, FullPath = f };

                extraNodes.Add(node);
            }

            DarkTreeNode baseNode = new DarkTreeNode("DEAD RISING 2");

            extraNodes.ForEach((n) =>
            {
                baseNode.Nodes.Add(n);
            });

            filesTree.Nodes.Add(baseNode);
            filesTree.Click += HandleNodeClick;

            itemsList.Click += HandleListSelected;
            itemsList.DoubleClick += HandleListClicked;
        }

        private void HandleListClicked(object sender, EventArgs e)
        {
            if (itemsList.SelectedIndices.Count > 0)
            {
                var sel = itemsList.SelectedIndices[0];
                var item = itemsList.Items[sel];

                if (item.Tag != null)
                {
                    var tag = (ListTag)item.Tag;

                    if (tag.Entry.Name.EndsWith(".csv") ||
                        tag.Entry.Name.EndsWith(".txt"))
                    {
                        string content = tag.BigFile.ReadTextFile(tag.Entry.Name);

                        new TextViewForm(tag.Entry.Name, content).Show();
                    }
                }
            }
        }

        private void HandleListSelected(object sender, EventArgs e)
        {
            Debug.WriteLine("selected!");
        }

        private void HandleNodeClick(object sender, EventArgs ea)
        {
            itemsList.Items.Clear();

            if (filesTree.SelectedNodes.Count > 0)
            {
                var node = filesTree.SelectedNodes[0];

                if (node.Tag != null)
                {
                    var tag = (NodeTag)node.Tag;
                    if (tag.FullPath.EndsWith(".big"))
                    {
                        BigFile bf = new BigFile();
                        if (bf.Read(tag.FullPath))
                        {
                            //label2.Text = $"archive {Path.GetFileName(tag.FullPath)} has {bf.Entries.Count} files.";

                            foreach (var e in bf.Entries)
                            {
                                DarkListItem item = new DarkListItem(e.Name);
                                item.Tag = new ListTag() { NodeTag = tag, BigFile = bf, Entry = e };

                                itemsList.Items.Add(item);
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Failed reading {Path.GetFileName(tag.FullPath)} :(");
                        }
                    }
                }
            }
        }
    }
}
