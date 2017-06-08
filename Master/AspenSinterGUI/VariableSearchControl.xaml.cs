using System;
using System.Collections.Generic;
using System.Linq;
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

namespace SinterConfigGUI
{
    /// <summary>
    /// Interaction logic for VariableSearchControl.xaml
    /// </summary>
    public partial class VariableSearchControl : UserControl
    {
        Presenter presenter = Presenter.presenter_singleton;

        public VariableSearchControl()
        {
            InitializeComponent();
            this.DataContext = presenter;

        }

        private void searchPatternBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Command.Search.Execute(sender, null);
        }

        private void searchResults_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Command.PreviewVariable.Execute(sender, null);
        }

        private void searchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Command.PreviewVariable.Execute(sender, null);
        }

    }
}
