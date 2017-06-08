using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SinterConfigGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public void Application_Onexit(object sender, ExitEventArgs e)
        {
            Presenter presenter = Presenter.presenter_singleton;
            if (presenter.o_sim != null && presenter.saveable)
            {
                string messageBoxText = "Do you want to save changes?";
                string caption = "Word Processor";
                MessageBoxButton button = MessageBoxButton.YesNo;
                MessageBoxImage icon = MessageBoxImage.Warning;
                MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

                if (result == MessageBoxResult.Yes)
                {
                    Presenter.presenter_singleton.SaveCommand_Executed(this, null);
                }
                //status bar text won't actually update anyway....
//                setStatusBarText("Attempting to close Aspen.");
             //   SinterSingleton.sinterSingleton.isim_singleton.closeSim();
//                setStatusBarText("Aspen Closed.");
            
            //TODO: If not saved, put up a save query
            }


        }
    }
}
