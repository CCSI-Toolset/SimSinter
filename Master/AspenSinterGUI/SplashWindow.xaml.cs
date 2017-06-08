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
using System.Windows.Threading;
using System.ComponentModel;

namespace SinterConfigGUI
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        Presenter presenter = Presenter.presenter_singleton;
        public BackgroundWorker worker;          //our bg worker for doing long tasks
        public delegate void OpenFilePageDelegate();

        public SplashWindow()
        {
            InitializeComponent();

            //create our background worker and support cancellation
            worker = new BackgroundWorker();

            worker.DoWork += delegate(object s, DoWorkEventArgs doWorkArgs)
            {
//                try
//                {
                    System.Threading.Thread.Sleep(5000);

                    //And ask the WPF thread to open the next (OpenFile) Window
                    System.Windows.Threading.Dispatcher winDispatcher = this.Dispatcher;
                    OpenFilePageDelegate winDelegate = new OpenFilePageDelegate(this.close_click);
                    //invoke the dispatcher and pass the percentage and max record count
                    winDispatcher.BeginInvoke(winDelegate);
//                }
//                catch (Exception ex)
//                {
//                    ;
//                }
            };
            worker.RunWorkerAsync();
        }

        public void close_click()
        {
            if (this.IsVisible)
            {
                presenter.OpenOpenFilePage();
                this.Close();
            }
        }

        private void Grid_close(object sender, MouseButtonEventArgs e)
        {
            close_click();
        }
    }
}
