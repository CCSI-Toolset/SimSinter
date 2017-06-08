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
using System.Windows.Shapes;

namespace SinterConfigGUI
{
    /// <summary>
    /// Interaction logic for SearchCancelBox.xaml
    /// </summary>
    public partial class SearchCancelBox : Window
    {

        Presenter presenter = Presenter.presenter_singleton;

        public SearchCancelBox()
        {
            InitializeComponent();
            this.DataContext = presenter;
        }

        public void SearchCancelButton_Click(object sender, RoutedEventArgs e)
        {
            presenter.worker.CancelAsync();
        }

    }
}
