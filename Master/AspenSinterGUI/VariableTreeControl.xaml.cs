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
using System.Diagnostics;
using System.IO;

namespace SinterConfigGUI
{
    /// <summary>
    /// Interaction logic for VariableTreeControl.xaml
    /// </summary>
    public partial class VariableTreeControl : UserControl
    {
        Presenter presenter = Presenter.presenter_singleton;

        public VariableTreeControl()
        {
            InitializeComponent();

            // Get raw family tree data from a database.
            VariableTree.VariableTree dataTree = presenter.sim.dataTree;
            this.DataContext = presenter;
            if (dataTree == null)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                presenter.sim.startDataTree();
                stopwatch.Stop();
                TimeSpan ts = stopwatch.Elapsed;

                dataTree = presenter.sim.dataTree;
            }

            // Create UI-friendly wrappers around the 
            // raw data objects (i.e. the view-model).
            presenter.variableTree = new VariableTreeViewModel(dataTree);

        }

        private void variableTree_KeyDown(object sender, KeyEventArgs e)
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
