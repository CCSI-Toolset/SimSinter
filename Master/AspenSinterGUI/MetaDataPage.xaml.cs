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
    /// Interaction logic for MetaDataPage.xaml
    /// </summary>
    public partial class MetaDataPage : Window
    {
        public Presenter presenter = Presenter.presenter_singleton;
        public MetaDataPage()
        {
            InitializeComponent();
            this.DataContext = presenter;

            presenter.inputFiles.Clear();
            presenter.inputFiles.Add(presenter.sim.simFile);  //The first input file is ALWAYS the simfile
            presenter.inputFileHash.Add(presenter.sim.simFileHash);
            presenter.inputFileHashAlgo.Add(presenter.sim.simFileHashAlgo);

            for(int ii = 0; ii < presenter.sim.additionalFiles.Count; ++ii) {
                presenter.inputFiles.Add(presenter.sim.additionalFiles[ii]);
                presenter.inputFileHash.Add(presenter.sim.additionalFilesHash[ii]);
                presenter.inputFileHashAlgo.Add(presenter.sim.additionalFilesHashAlgo[ii]);
            }

            InputFiles.ItemsSource = presenter.inputFiles;


            presenter.status = "Please check that all the meta-data is correct, or fill in as appropriate.";
            //If the file we opened doesn't have a date, default to today's date.
            if (presenter.dateString == null || presenter.dateString == "")
            {
                presenter.dateString = getTodaysDate();
            }

            CommandBinding ResetCommandBinding = new CommandBinding(Command.ResetProgram, presenter.ResetProgram_Executed, presenter.ResetProgram_CanExecute);
            this.CommandBindings.Add(ResetCommandBinding);
            CommandBinding SaveCommandBinding = new CommandBinding(ApplicationCommands.Save, presenter.SaveCommand_Executed, presenter.SaveCommand_CanExecute);
            this.CommandBindings.Add(SaveCommandBinding);
            CommandBinding SaveAsCommandBinding = new CommandBinding(ApplicationCommands.SaveAs, presenter.SaveAsCommand_Executed, presenter.SaveAsCommand_CanExecute);
            this.CommandBindings.Add(SaveAsCommandBinding);
            CommandBinding gotoVariablesPageBinding = new CommandBinding(Command.GotoVariablesPage, presenter.GotoVariablesPage_Executed, presenter.GotoVariablesPage_CanExecute);
            this.CommandBindings.Add(gotoVariablesPageBinding);
            CommandBinding gotoOpenFilePageBinding = new CommandBinding(Command.GotoOpenFilePage, presenter.GotoOpenFilePage_Executed, presenter.GotoOpenFilePage_CanExecute);
            this.CommandBindings.Add(gotoOpenFilePageBinding);
            CommandBinding addInputFile = new CommandBinding(Command.AddInputFile, presenter.AddInputFile_Executed, presenter.AddInputFile_CanExecute);
            this.CommandBindings.Add(addInputFile);
            CommandBinding removeInputFile = new CommandBinding(Command.RemoveInputFile, presenter.RemoveInputFile_Executed, presenter.RemoveInputFile_CanExecute);
            this.CommandBindings.Add(removeInputFile);

        }

        private string getTodaysDate()
        {
            return DateTime.Now.ToShortDateString();
        }

        private void TodaysDate_Click(object sender, RoutedEventArgs e)
        {
            presenter.dateString = getTodaysDate();
        }

        void InputFiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            System.Collections.IList items = InputFiles.SelectedItems;
            presenter.selectedInputFiles.Clear();
            foreach (string item in items)
            {
                presenter.selectedInputFiles.Add(item);
            }
        }


    }
}
