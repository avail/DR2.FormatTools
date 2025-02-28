﻿using DarkUI.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FormatTools.UI
{
    public partial class TextViewForm : DarkForm
    {
        public TextViewForm(string title = "DR2: Text View", string content = "")
        {
            InitializeComponent();

            Text = title;
            textEditorControl1.BackColor = Color.FromArgb(0);
            textEditorControl1.Text = content;
            textEditorControl1.Refresh();
        }
    }
}
