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
//using System.Windows.Forms;
using System.Windows.Controls.Primitives;
using System.IO;
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel;

namespace SinterConfigGUI
{
    /// <summary>
    /// Interaction logic for OpenFilePage.xaml
    /// </summary>
    public partial class OpenFilePage : Window
    {
        Presenter presenter = Presenter.presenter_singleton;

        public OpenFilePage()
        {
            InitializeComponent();
            this.DataContext = presenter;
            presenter.openFilePage = this; //Register the opening page with the Presenter (Breaking MVVM)
            presenter.currentPage = this;
            presenter.status = "Waiting for user to choose Input File";
            CommandBinding OpenCommandBinding = new CommandBinding(ApplicationCommands.Open, presenter.OpenCommand_Executed, presenter.OpenCommand_CanExecute);
            this.CommandBindings.Add(OpenCommandBinding);
            CommandBinding OpenFileBrowserCommandBinding = new CommandBinding(Command.OpenFileBrowserCommand, presenter.OpenFileBrowserCommand_Executed, presenter.OpenFileBrowserCommand_CanExecute);
            this.CommandBindings.Add(OpenFileBrowserCommandBinding);

        }


   //     private void BrowseFilesButton_Click(object sender, RoutedEventArgs e)
    //    {
    //    }
         
    }
}
