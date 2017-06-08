using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace SinterConfigGUI
{
    public static class Command
    {

        public static readonly RoutedUICommand ResetProgram = new RoutedUICommand("resetProgram", "resetProgram", typeof(System.Windows.Window));
        public static readonly RoutedUICommand OpenFileBrowserCommand = new RoutedUICommand("openFileBrowserCommand", "openFileBrowserCommand", typeof(System.Windows.Window));
        public static readonly RoutedUICommand GotoOpenFilePage = new RoutedUICommand("gotoOpenFilePage", "gotoOpenFilePage", typeof(System.Windows.Window));
        public static readonly RoutedUICommand GotoMetaDataPage = new RoutedUICommand("gotoMetaDataPage", "gotoMetaDataPage", typeof(System.Windows.Window));
        public static readonly RoutedUICommand GotoVariablesPage = new RoutedUICommand("gotoVariablesPage", "gotoVariablesPage", typeof(System.Windows.Window));
        public static readonly RoutedUICommand GotoVectorInitPage = new RoutedUICommand("gotoVectorInitPage", "gotoVectorInitPage", typeof(System.Windows.Window));
        public static readonly RoutedUICommand PreviewVariable = new RoutedUICommand("previewVariable", "previewVariable", typeof(System.Windows.Window));
        public static readonly RoutedUICommand PreviewToInput = new RoutedUICommand("previewToInput", "previewToInput", typeof(System.Windows.Window));
        public static readonly RoutedUICommand PreviewToOutput = new RoutedUICommand("previewToOutput", "previewToOutput", typeof(System.Windows.Window));
        public static readonly RoutedUICommand CancelPreview = new RoutedUICommand("cancelPreview", "cancelPreview", typeof(System.Windows.Window));
        public static readonly RoutedUICommand RemoveVariable = new RoutedUICommand("removeVariable", "removeVariable", typeof(System.Windows.Window));

        public static readonly RoutedUICommand AddInputFile = new RoutedUICommand("addInputFile", "addInputFile", typeof(System.Windows.Window));
        public static readonly RoutedUICommand RemoveInputFile = new RoutedUICommand("removeInputFile", "removeInputFile", typeof(System.Windows.Window));


        public static readonly RoutedUICommand Search = new RoutedUICommand("search", "search", typeof(System.Windows.Window));

        public static readonly RoutedUICommand HeatIntegration = new RoutedUICommand("heatIntegration", "heatIntegration", typeof(System.Windows.Window));
        public static readonly RoutedUICommand SaveAndQuit = new RoutedUICommand("saveAndQuit", "saveAndQuit", typeof(System.Windows.Window));
        public static readonly RoutedUICommand UploadToDMF = new RoutedUICommand("uploadToDMF", "uploadToDMF", typeof(System.Windows.Window));

        public static readonly RoutedUICommand VectorOrFinishCommand = new RoutedUICommand("vectorOrFinishCommand", "vectorOrFinishCommand", typeof(System.Windows.Window));

    }
}
