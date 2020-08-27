using DarkUI.Controls;

namespace FormatTools.UI
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.filesTree = new DarkUI.Controls.DarkTreeView();
            this.label1 = new System.Windows.Forms.Label();
            this.itemsList = new DarkUI.Controls.DarkListView();
            this.SuspendLayout();
            // 
            // filesView
            // 
            this.filesTree.Location = new System.Drawing.Point(12, 12);
            this.filesTree.MaxDragChange = 20;
            this.filesTree.Name = "filesView";
            this.filesTree.Size = new System.Drawing.Size(310, 426);
            this.filesTree.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(328, 423);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "label1";
            // 
            // listView1
            // 
            this.itemsList.Location = new System.Drawing.Point(328, 12);
            this.itemsList.Name = "listView1";
            this.itemsList.Size = new System.Drawing.Size(838, 408);
            this.itemsList.TabIndex = 2;
            this.itemsList.Text = "Files";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1178, 450);
            this.Controls.Add(this.itemsList);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.filesTree);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "DR2 FormatTools";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DarkTreeView filesTree;
        private System.Windows.Forms.Label label1;
        private DarkListView itemsList;
    }
}

