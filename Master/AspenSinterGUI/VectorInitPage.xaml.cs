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
using System.Collections.ObjectModel;
using System.ComponentModel;
using DataGrid2DLibrary;

namespace SinterConfigGUI
{
    /// <summary>
    /// Vector Init Page allows the user to set defaults for all their vector varaibles.  This is particularly
    /// important for dyanmic ACM, as we have the TimeSeries.  TimeSeries is also the only vector a user can change 
    /// the size of.  (Normally the vector size is defined by the simulation, but settings are internal.)
    /// Anyway, this is the only non-MVVM page, because this one is completely dyanamically generated based on the
    /// number of vectors defined by the user.
    /// </summary>
    public partial class VectorInitPage : Window
    {
        public Presenter presenter = Presenter.presenter_singleton;
        public ObservableCollection<VectorViewModel> vectorVariables = new ObservableCollection<VectorViewModel>();

        public VectorInitPage()
        {
            InitializeComponent();
            this.DataContext = presenter;
            presenter.status = "This page is for setting Vector default values.";
 
            CommandBinding ResetCommandBinding = new CommandBinding(Command.ResetProgram, presenter.ResetProgram_Executed, presenter.ResetProgram_CanExecute);
            this.CommandBindings.Add(ResetCommandBinding);
            CommandBinding SaveCommandBinding = new CommandBinding(ApplicationCommands.Save, presenter.SaveCommand_Executed, presenter.SaveCommand_CanExecute);
            this.CommandBindings.Add(SaveCommandBinding);
            CommandBinding SaveAsCommandBinding = new CommandBinding(ApplicationCommands.SaveAs, presenter.SaveAsCommand_Executed, presenter.SaveAsCommand_CanExecute);
            this.CommandBindings.Add(SaveAsCommandBinding);
            CommandBinding gotoVariablesPageBinding = new CommandBinding(Command.GotoVariablesPage, presenter.GotoVariablesPage_Executed, presenter.GotoVariablesPage_CanExecute);
            this.CommandBindings.Add(gotoVariablesPageBinding);
            CommandBinding SaveAndQuitBinding = new CommandBinding(Command.SaveAndQuit, presenter.SaveAndQuit_Executed, presenter.SaveAndQuit_CanExecute);
            this.CommandBindings.Add(SaveAndQuitBinding);
            CommandBinding UploadToDMFBinding = new CommandBinding(Command.UploadToDMF, presenter.UploadToDMF_Executed, presenter.UploadToDMF_CanExecute);
            this.CommandBindings.Add(UploadToDMFBinding);

            foreach (VariableViewModel varView in presenter.inputVariables)
            {
                if (varView.variable.isVec)
                {
                    VectorViewModel locVec = new VectorViewModel((sinter.sinter_Vector)varView.variable, varView.isSetting);  //You can change the size of setting vectors (currently dynamic ACM timeseries is the only one.) 
                    vectorVariables.Add(locVec);
                }
            }

            foreach (VectorViewModel thisVec in vectorVariables)
            {
                addName(thisVec);
                addSize(thisVec);
                addArray(thisVec);
            }

        }

        //Dyanmically generates the name block
        private void addName(VectorViewModel thisVec)
        {
            TextBox tt1 = new TextBox();
            tt1.IsReadOnly = true;
            tt1.Height = 20;
            tt1.Text = thisVec.name;
            tt1.Margin = new System.Windows.Thickness(1, 1, 1, 1);
            NameStackPanel.Children.Add(tt1);
        }

        //Dyanmically generates the size block, and binds it (can only change size if the variable is a setting.)
        private void addSize(VectorViewModel thisVec)
        {
            TextBox nn1 = new TextBox();
            nn1.Height = 20;
            nn1.DataContext = thisVec;
            nn1.Margin = new System.Windows.Thickness(1, 1, 1, 1);
            Binding datagrid2dBindingnn = new Binding();
            datagrid2dBindingnn.Path = new PropertyPath("size");
            if (thisVec.sizeCanChange)
            {
                datagrid2dBindingnn.Mode = BindingMode.TwoWay;
            }
            else
            {
                datagrid2dBindingnn.Mode = BindingMode.OneWay;
                nn1.IsReadOnly = true;
            }
            nn1.SetBinding(TextBox.TextProperty, datagrid2dBindingnn);
            SizeStackPanel.Children.Add(nn1);
        }

        //Dyanmically generates the array value views
        private void addArray(VectorViewModel thisVec)
        {
            DataGrid2D demo1 = new DataGrid2D();
            demo1.Height = 20;
            demo1.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            demo1.Margin = new System.Windows.Thickness(1, 1, 1, 1);

            demo1.HeadersVisibility = DataGridHeadersVisibility.None;

            demo1.DataContext = thisVec;
            Binding datagrid2dBinding = new Binding();
            datagrid2dBinding.Path = new PropertyPath("dataArray");
            datagrid2dBinding.Mode = BindingMode.TwoWay;
            datagrid2dBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            demo1.SetBinding(DataGrid2D.ItemsSource2DProperty, datagrid2dBinding);

            DataGridStackPanel.Children.Add(demo1);

        }

    }
}
