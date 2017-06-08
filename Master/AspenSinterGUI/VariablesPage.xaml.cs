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
using System.Diagnostics;
using System.IO;

namespace SinterConfigGUI
{
    /// <summary>
    /// Interaction logic for VariablesPage.xaml
    /// </summary>
    public partial class VariablesPage : Window
    {

        
        Presenter presenter = Presenter.presenter_singleton;

        public VariablesPage()
        {
            InitializeComponent();
            this.DataContext = presenter;

            presenter.addSettingsToInputs();

            PreviewVariable.ItemsSource = presenter.previewVariable;
            InputVariables.ItemsSource = presenter.inputVariables;
            OutputVariables.ItemsSource = presenter.outputVariables;

            if (presenter.acm != null)
            {
                VariableLookupPanel.Children.Add(new VariableSearchControl());  //ACM has variable searching instead of a tree
                {   //ACM has dynamic variable support, so add that checkbox column to the variable grid
                    DataGridCheckBoxColumn dynamicColumn = new DataGridCheckBoxColumn();
                    dynamicColumn.Header = "Dynamic";
                    dynamicColumn.Binding = new Binding("isDynamic");
                    InputVariables.Columns.Insert(0, dynamicColumn);
                }
                {
                    DataGridCheckBoxColumn dynamicColumn = new DataGridCheckBoxColumn();
                    dynamicColumn.Header = "Dynamic";
                    dynamicColumn.Binding = new Binding("isDynamic");
                    OutputVariables.Columns.Insert(0, dynamicColumn);
                }
            }
            else
            {
                VariableLookupPanel.Children.Add(new VariableTreeControl());
            }

            CommandBinding previewVariable = new CommandBinding(Command.PreviewVariable, presenter.PreviewVariable_Executed, presenter.PreviewVariable_CanExecute);
            this.CommandBindings.Add(previewVariable);
            CommandBinding removeVariable = new CommandBinding(Command.RemoveVariable, presenter.RemoveVariable_Executed, presenter.RemoveVariable_CanExecute);
            this.CommandBindings.Add(removeVariable);
            CommandBinding previewToInput = new CommandBinding(Command.PreviewToInput, presenter.PreviewToInput_Executed, presenter.PreviewToInput_CanExecute);
            this.CommandBindings.Add(previewToInput);
            CommandBinding previewToOutput = new CommandBinding(Command.PreviewToOutput, presenter.PreviewToOutput_Executed, presenter.PreviewToOutput_CanExecute);
            this.CommandBindings.Add(previewToOutput);
            CommandBinding cancelPreview = new CommandBinding(Command.CancelPreview, presenter.CancelPreview_Executed, presenter.CancelPreview_CanExecute);
            this.CommandBindings.Add(cancelPreview);
            CommandBinding gotoMetaDataPage = new CommandBinding(Command.GotoMetaDataPage, presenter.GotoMetaDataPage_Executed, presenter.GotoMetaDataPage_CanExecute);
            this.CommandBindings.Add(gotoMetaDataPage);
            CommandBinding gotoVectorInitPage = new CommandBinding(Command.GotoVectorInitPage, presenter.GotoVectorInitPage_Executed, presenter.GotoVectorInitPage_CanExecute);
            this.CommandBindings.Add(gotoVectorInitPage);

            CommandBinding SaveCommandBinding = new CommandBinding(ApplicationCommands.Save, presenter.SaveCommand_Executed, presenter.SaveCommand_CanExecute);
            this.CommandBindings.Add(SaveCommandBinding);
            CommandBinding SaveAndQuitCommandBinding = new CommandBinding(Command.SaveAndQuit, presenter.SaveAndQuit_Executed, presenter.SaveAndQuit_CanExecute);
            this.CommandBindings.Add(SaveAndQuitCommandBinding);
            CommandBinding UploadToDMFCommandBinding = new CommandBinding(Command.UploadToDMF, presenter.UploadToDMF_Executed, presenter.UploadToDMF_CanExecute);
            this.CommandBindings.Add(UploadToDMFCommandBinding);
            
            CommandBinding VectorOrFinishCommandBinding = new CommandBinding(Command.VectorOrFinishCommand, presenter.VectorOrFinishCommand_Executed, presenter.VectorOrFinishCommand_CanExecute);
            this.CommandBindings.Add(VectorOrFinishCommandBinding);

            CommandBinding Search = new CommandBinding(Command.Search, presenter.SearchCommand_Executed, presenter.SearchCommand_CanExecute);
            this.CommandBindings.Add(Search);

            CommandBinding HeatIntegration = new CommandBinding(Command.HeatIntegration, presenter.HeatIntegrationCommand_Executed, presenter.HeatIntegrationCommand_CanExecute);
            this.CommandBindings.Add(HeatIntegration);

        }

        void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                presenter.variableTree.SearchCommand.Execute(null);
        }

        void InputVariables_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            System.Collections.IList items = InputVariables.SelectedItems;
            presenter.selectedInputVars.Clear();  
            foreach (VariableViewModel item in items)
            {
                presenter.selectedInputVars.Add(item);
            }

            //gPROMS: Crazy hack to get input variables to all change if the user changes the procesName
            if (presenter.o_gproms != null)
            {
                //First find the processname
                VariableViewModel processNameVM = null;
                foreach (VariableViewModel item in InputVariables.Items)
                {
                    if (item.name == "ProcessName")
                    {
                        processNameVM = item;
                        break;
                    }
                }

                //If the changed, update everything 
                if (processNameVM != null && ((String)processNameVM.value) != ((String)presenter.o_gproms.processName))
                {
                    if (presenter.o_gproms.gProcesses.ContainsKey((String)processNameVM.value))  //Check to make sure it's a really process name (if it isn't throw an error)
                    {
                        presenter.o_gproms.processName = (string) processNameVM.value;
                        presenter.o_gproms.resetInputVariables();
                        presenter.inputVariables.Clear(); //This causes the input varialbes selection to change, and processNameVM comes up null.
                        presenter.outputVariables.Clear();
                        presenter.allVariables.Clear();
                        presenter.buildVariableGrids();
                    }
                    else
                    {
                        presenter.displayError(String.Format("Invalid ProcessName: {0}", processNameVM.value));
                    }
                }

            }

        }

        void OutputVariables_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            System.Collections.IList items = OutputVariables.SelectedItems;
            presenter.selectedOutputVars.Clear();
            foreach (VariableViewModel item in items)
            {
                presenter.selectedOutputVars.Add(item);
            }
        }

        private void previewVariableBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Command.PreviewVariable.Execute(sender, null);
        }

        private void PreviewToInputButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void VectorButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_FocusableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void InputVariables_GotFocus(object sender, RoutedEventArgs e)
        {
            presenter.inputFocusLast = true;
        }

        private void OutputVariables_GotFocus(object sender, RoutedEventArgs e)
        {
            presenter.inputFocusLast = false;
        }


    }
}
