﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IDE.Controls.WPF.Windows;
using IDE.Core.Interfaces;


namespace IDE.Presentation.Views.ReplacePartDialog
{
    /// <summary>
    /// Interaction logic for ReplacePartDialog.xaml
    /// </summary>
    public partial class ReplacePartDialog : ModernWindow, IWindow
    {
        public ReplacePartDialog()
        {
            InitializeComponent();
        }
    }
}
