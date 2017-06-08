using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using sinter;
using System.Text.RegularExpressions;


namespace SinterConfigGUI
{
    public class Presenter : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string strPropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(strPropertyName));
            }
        }

        #endregion

        #region data
        static public Presenter presenter_singleton = new Presenter();
        public sinter.sinter_Sim o_sim;
        public sinter.sinter_SimACM o_acm = null; //Only set if sim is an ACM
        public sinter.sinter_SimAspen o_aspenplus = null; //Only set if sim is an aspenplus
        public sinter.sinter_SimExcel o_excel = null; //Only set if sim is a excel
        public sinter.PSE.sinter_simGPROMSconfig o_gproms = null;  //Only set if sim is gPROMS

        private bool o_saveable = true;         //Should we try to save?  Occasionally we want to turn off saving
        public BackgroundWorker worker;          //our bg worker for doing long tasks
        public SearchCancelBox searchCancelBox = null;
        public IList<string> pathesFoundBySearch = new List<String>(); //Just so we don't have to check for null
        public string sinterStatus;
        public string o_vectorsOrFinishText; // On the variables page the next button text may say "Vectors" or "Finish"
        public int sinterProgressOnCurrentOp;
        public string o_openConfigFilename = "";
        public string o_saveConfigFilename = "";
        //This is a special file for foqus.  If foqus passes in a second command line argument.
        //it is the path for this file.  This file contains 1 string, the path to the save file.
        public string o_infoForFoqusFile = "";
        private string DMFPath = "c:\\Program Files (x86)\\foqus\\foqus\\dist\\DMF_Sim_Ingester.exe";
        private bool DMFAvailible = false;

        public ObservableCollection<String> inputFiles = new ObservableCollection<String>();
        public IList<String> selectedInputFiles = new List<String>();  //Input files on Meta-data page
        public ObservableCollection<String> inputFileHash = new ObservableCollection<String>();  //Idea 1: Keep a list of the SHA1s upfront, so if there is a problem making an SHA1 the user knows about it right away
        public ObservableCollection<String> inputFileHashAlgo = new ObservableCollection<String>();  //Idea 1: Keep a list of the SHA1s upfront, so if there is a problem making an SHA1 the user knows about it right away


        private int o_uniqueNameSuffix = 0;

        private string o_metaDataStatusText = "Nothing here";

        public VariableTreeViewModel o_variableTree;

        public string o_previewVariablePath = "";

        public ObservableCollection<VariableViewModel> previewVariable = new ObservableCollection<VariableViewModel>();
        public ObservableCollection<VariableViewModel> inputVariables = new ObservableCollection<VariableViewModel>();
        public ObservableCollection<VariableViewModel> outputVariables = new ObservableCollection<VariableViewModel>();
        public ObservableCollection<VariableViewModel> allVariables = new ObservableCollection<VariableViewModel>();

        public IList<VariableViewModel> selectedInputVars = new List<VariableViewModel>();
        public IList<VariableViewModel> selectedOutputVars = new List<VariableViewModel>();
        /** inputFocusLast is True if inputvariables last had focus, false if output vars did
         * The purpose is to keep removeVariables from deleteing variables that remained selected
         * from previous user action that the user has forgotten about. **/
        public bool inputFocusLast = true;  

        /***********************************************
         * A couple of static strings
        ***********************************************/
        private readonly string openedConfigFile = "Meta-Data parsed from input Sinter Configuration file.  Please update the meta-data and proceed.";
        private readonly string openedSimFile = "Please provide meta-data to describe the simulation that was just opened.";

        /*******************************************************
         * Search Variables for ACM
         *******************************************************/
        public string o_searchPattern = "";
        public string o_searchType = "";
        public bool o_search_fixed = true;
        public bool o_search_free = true;
        public bool o_search_rateinitial = true;
        public bool o_search_initial = true;
        public bool o_search_parameters = true;
        public bool o_search_algebraics = true;
        public bool o_search_state = true;
        public bool o_search_inactive = true;

        public ObservableCollection<FoundVariableViewModel> o_foundVariables = new ObservableCollection<FoundVariableViewModel>();

        #endregion data
        #region constructors
        Presenter()
        {
            o_sim = null;
            sinterStatus = "";
            sinterProgressOnCurrentOp = 0;

            handleCommandLineArgs();

            DMFAvailible = File.Exists(DMFPath);

        }

        private void clearPresenter()
        {
            if (o_sim != null)
            {
                o_sim.closeSim();
            }
            if (metaDataPage != null)
            {
                metaDataPage.Close();
                metaDataPage = null;
            }
            if (variablesPage != null)
            {
                variablesPage.Close();
                variablesPage = null;
            }

            o_openConfigFilename = "";
            o_saveConfigFilename = "";

            inputFiles.Clear();
            selectedInputFiles.Clear();

            o_variableTree = null;

            o_previewVariablePath = "";

            previewVariable.Clear();
            inputVariables.Clear();
            outputVariables.Clear();
            allVariables.Clear();

            selectedInputVars.Clear();
            selectedOutputVars.Clear();

            o_searchPattern = "";
            o_searchType = "";
            o_search_fixed = true;
            o_search_free = true;
            o_search_rateinitial = true;
            o_search_initial = true;
            o_search_parameters = true;
            o_search_algebraics = true;
            o_search_state = true;
            o_search_inactive = true;

            o_foundVariables.Clear();

            handleCommandLineArgs();
        }

        private void handleCommandLineArgs()
        {
            String[] argv = Environment.GetCommandLineArgs();
            if (argv.Count() >= 2)
            {
                openConfigFilename = argv[1];
            }
            if (argv.Count() >= 3)
            {
                o_infoForFoqusFile = argv[2];
            }

        }

        public void buildVariableGrids()
        {
            for (int ii = 1; ii <= o_sim.countIO; ++ii)
            {
                sinter_Variable thisVar = (sinter_Variable)o_sim.getIOByIndex(ii);
                if (thisVar.mode == sinter_Variable.sinter_IOMode.si_IN)
                {
                    if (thisVar.isSetting && (thisVar.name == "TimeUnits" || thisVar.name == "MinStepSize"))  //Sigh, special case to skip this read-only settings.  Maybe I should have a property for this. Skip
                    {
                        ;
                    } else {
                        inputVariables.Add(new VariableViewModel(thisVar));
                        allVariables.Add(new VariableViewModel(thisVar));
                    }
                }
                else
                {
                    outputVariables.Add(new VariableViewModel(thisVar));
                    allVariables.Add(new VariableViewModel(thisVar));
                }

            }

        }

        #endregion constructors

        #region localProperties


        
        public string previewVariablePath
        {
            get
            {
                return o_previewVariablePath;
            }
            set
            {
                o_previewVariablePath = value;
                OnPropertyChanged("previewVariablePath");
            }
        }

        public VariableTreeViewModel variableTree
        {
            get
            {
                return o_variableTree;
            }
            set
            {
                o_variableTree = value;
                OnPropertyChanged("variableTree");
            }
        }

        public ObservableCollection<FoundVariableViewModel> foundVariables
        {
            get
            {
                return o_foundVariables;
            }
            set
            {
                o_foundVariables = value;
                OnPropertyChanged("foundVariables");
            }
        }

        public string searchPattern
        {
            get
            {
                return o_searchPattern;
            }
            set
            {
                o_searchPattern = value;
                OnPropertyChanged("searchPattern");
            }
        }

        public string searchType
        {
            get
            {
                return o_searchType;
            }
            set
            {
                o_searchType = value;
                OnPropertyChanged("searchType");
            }
        }

        public bool search_fixed
        {
            get
            {
                return o_search_fixed;
            }
            set
            {
                o_search_fixed = value;
                OnPropertyChanged("search_fixed");
            }
        }

        public bool search_free
        {
            get
            {
                return o_search_free;
            }
            set
            {
                o_search_free = value;
                OnPropertyChanged("search_free");
            }
        }

        public bool search_rateinitial
        {
            get
            {
                return o_search_rateinitial;
            }
            set
            {
                o_search_rateinitial = value;
                OnPropertyChanged("search_rateinitial");
            }
        }

        public bool search_initial
        {
            get
            {
                return o_search_initial;
            }
            set
            {
                o_search_initial = value;
                OnPropertyChanged("search_initial");
            }
        }

        public bool search_parameters
        {
            get
            {
                return o_search_parameters;
            }
            set
            {
                o_search_parameters = value;
                OnPropertyChanged("search_parameters");
            }
        }

        public bool search_algebraics
        {
            get
            {
                return o_search_algebraics;
            }
            set
            {
                o_search_algebraics = value;
                OnPropertyChanged("search_algebraics");
            }
        }

        public bool search_state
        {
            get
            {
                return o_search_state;
            }
            set
            {
                o_search_state = value;
                OnPropertyChanged("search_state");
            }
        }

        public bool search_inactive
        {
            get
            {
                return o_search_inactive;
            }
            set
            {
                o_search_inactive = value;
                OnPropertyChanged("search_inactive");
            }
        }

        public string metaDataStatusText
        {
            get
            {
                return o_metaDataStatusText;
            }
            set
            {
                o_metaDataStatusText = value;
                OnPropertyChanged("metaDataStatusText");
            }
        }

        public bool saveable
        {
            get
            {
                return o_saveable;
            }
            set
            {
                o_saveable = value;
                OnPropertyChanged("saveable");
            }
        }

        //The "set"here is kind of silly, it doesn't use the passed in value at all.
        //But I need it to be a propery, so, I hope this isn't too confusing.
        public string vectorsOrFinishText
        {
            get { return o_vectorsOrFinishText; }
            set
            {
                if (vectorsOrFinishCheck())
                {
                    o_vectorsOrFinishText = "Next >";
                }
                else
                {
                    o_vectorsOrFinishText = "Finish";
                }
                OnPropertyChanged("vectorsOrFinishText");
            }
        }

        //Do I really need one of these for each kind of handler?
        public void vectorsOrFinishTextMethod(Object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            vectorsOrFinishText = "foo";  //Text doesn't matter.
        }

        public void vectorsOrFinishTextMethod2(Object sender, PropertyChangedEventArgs args)
        {
            vectorsOrFinishText = "foo";  //Text doesn't matter.
        }

        #endregion localProperties

        #region Windows
        public OpenFilePage openFilePage = null;
        public MetaDataPage metaDataPage = null;
        public VariablesPage variablesPage = null;
        public VectorInitPage vectorInitPage = null;
        public System.Windows.Window currentPage = null;

        //our delegate used for updating the UI
        public delegate void OpenMetaDataPageDelegate();

        public void OpenOpenFilePage()
        {

            OpenFilePage newOpenFilePage = new OpenFilePage();
            newOpenFilePage.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            newOpenFilePage.Width = 800; // currentPage.Width;
            newOpenFilePage.Height = 550; // currentPage.Height;
            newOpenFilePage.Show();

            clearPresenter();

            openFilePage = newOpenFilePage;
            currentPage = openFilePage;
        }

        public void OpenMetaDataPage()
        {
            if (metaDataPage == null)
            {
                metaDataPage = new MetaDataPage();
                metaDataPage.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                metaDataPage.Width = currentPage.Width;
                metaDataPage.Height = currentPage.Height;
            }
            currentPage.Close();
            currentPage = metaDataPage;
            metaDataPage = null;
            currentPage.Show();
        }

        public void OpenVariablesPage()
        {
            if (variablesPage == null)
            {
                variablesPage = new VariablesPage();
                variablesPage.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                variablesPage.Width = currentPage.Width;
                variablesPage.Height = currentPage.Height;
            }
            currentPage.Close();
            currentPage = variablesPage;
            variablesPage = null;
            currentPage.Show();
        }

        public void OpenVectorInitPage()
        {
            if (vectorInitPage == null)
            {
                vectorInitPage = new VectorInitPage();
                vectorInitPage.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                vectorInitPage.Width = currentPage.Width;
                vectorInitPage.Height = currentPage.Height;
            }
            currentPage.Close();
            currentPage = vectorInitPage;
            vectorInitPage = null;
            currentPage.Show();

        }


        #endregion Windows

        #region sinterProperties

        public string openConfigFilename
        {
            get { return o_openConfigFilename; }
            set
            {
                o_openConfigFilename = value;
                OnPropertyChanged("openConfigFilename");
            }
        }


        public string saveConfigFilename
        {
            get { return o_saveConfigFilename; }
            set
            {
                o_saveConfigFilename = value;
                OnPropertyChanged("saveConfigFilename");
            }
        }

        public string VariableTreeToolTip
        {
            get
            {
                if (o_acm != null)
                {
                    return "This search box finds variables in ACM using the ACM Variable Find mechanism.";
                }
                else if (o_aspenplus != null)
                {
                    return "This tree mirrors the tree found in AspenPlus Variable Explorer";
                }
                else if (o_excel != null)
                {
                    return "This tree cna be used to find address in Excel by their column and row address.";
                }
                else
                {
                    return "Unknown simulation.";
                }
            }
        }

        public sinter.sinter_Sim sim
        {
            get { return o_sim; }
            set
            {
                o_sim = value;
                if (o_sim is sinter.sinter_SimACM)
                {
                    o_acm = (sinter.sinter_SimACM)o_sim;
                    o_aspenplus = null;
                    o_excel = null;
                    o_gproms = null;
                }
                else if (o_sim is sinter.sinter_SimAspen)
                {
                    o_aspenplus = (sinter.sinter_SimAspen)o_sim;
                    o_acm = null;
                    o_excel = null;
                    o_gproms = null;
                }
                else if (o_sim is sinter.sinter_SimExcel)
                {
                    o_excel = (sinter.sinter_SimExcel)o_sim;
                    o_acm = null;
                    o_aspenplus = null;
                    o_gproms = null;
                }
                else if (o_sim is sinter.PSE.sinter_simGPROMSconfig)
                {
                    o_gproms = (sinter.PSE.sinter_simGPROMSconfig)o_sim;
                    o_acm = null;
                    o_aspenplus = null;
                    o_excel = null;
                }
                else
                {
                    throw new IOException("Unknown simulation type!");
                }

                OnPropertyChanged("sim");
            }
        }

        public sinter.sinter_SimACM acm
        {
            get { return o_acm; }

        }

        public string status
        {
            get { return sinterStatus; }
            set
            {
                sinterStatus = value;
                OnPropertyChanged("status");
            }
        }

        public int progress
        {
            get { return sinterProgressOnCurrentOp; }
            set
            {
                sinterProgressOnCurrentOp = value;
                OnPropertyChanged("progress");
            }
        }

        public string simNameHeader
        {
            get
            {
                return "Application: " + sim.simName;
            }
        }

        public string simVersionRecommendation
        {
            get
            {
                if (sim.simVersionRecommendation == "")  //If there is no setting for the recommendation, the current version should be used.
                {
                    sim.simVersionRecommendation = sim.simVersion;
                }

                return sim.internal2externalVersion(sim.simVersionRecommendation);
            }
            set
            {
                sim.simVersionRecommendation = sim.external2internalVersion(value);

                OnPropertyChanged("simVersionRecommendation");
            }
        }

        public List<String> availableVersions
        {
            get
            {
                List<string> vers = new List<String>(sim.externalVersionList());
                string currentExVer = sim.internal2externalVersion(sim.simVersion);
                if (!vers.Contains<String>(currentExVer))
                {
                    vers.Add(currentExVer);
                }
                return vers;
            }
        }

        public string simVersionConstraint
        {
            get
            {

                return sinter_Sim.constraintToName(sim.simVersionConstraint);
            }
            set
            {
                sim.simVersionConstraint = sinter_Sim.nameToConstraint(value);

                OnPropertyChanged("simVersionConstraint");
            }
        }

        public List<String> availableConstraints
        {
            get
            {
                List<string> cons = new List<String>();
                foreach (sinter_versionConstraint constraint in Enum.GetValues(typeof(sinter_versionConstraint)))
                {
                    cons.Add(sinter_Sim.constraintToName(constraint));
                }
                return cons;
            }
        }

        public string author
        {
            get { return o_sim.author; }
            set
            {
                o_sim.author = value;
                OnPropertyChanged("author");
            }
        }

        public string dateString
        {
            get { return o_sim.dateString; }
            set
            {
                o_sim.dateString = value;
                OnPropertyChanged("dateString");
            }
        }

        public string description
        {
            get { return o_sim.description; }
            set
            {
                o_sim.description = value;
                OnPropertyChanged("description");
            }
        }

        int titleErrorCountdown = 0;  //I didn't like displaying the error every time a key is pressed, so now it will only happen every (5) times.
        public string title
        {
            get { return o_sim.title; }
            set
            {
                if (!isValidTitle(value))
                {
                    if (titleErrorCountdown == 0)
                    {
                        displayError("Simulation Title can only contain letters, numbers, or \"_\".  No spaces or special characters are allowed.");
                        titleErrorCountdown = 5;
                    }
                    else
                    {
                        --titleErrorCountdown;
                    }

                }
                //Save even if the title is invlaid. (So at least it matches the text box.)  It gets checked before saving any way.
                o_sim.title = value;
                OnPropertyChanged("title");
                
            }
        }

        public string configFileVersion
        {
            get { return o_sim.configFileVersion.ToString(); }
            set
            {
                string old_versionString = o_sim.configFileVersion.ToString();
                try
                {
                    o_sim.configFileVersion = new Version(value);
                }
                catch
                {
                }
                OnPropertyChanged("configFileVersion");
            }
        }

        #endregion sinterProperties

        #region viewProperties

        public bool o_isBusy = false;
        public bool o_isOpeningFile = false;


        public bool isOpeningFile
        {
            get { return o_isOpeningFile; }
            set
            {
                o_isOpeningFile = value;
                OnPropertyChanged("isOpeningFile");
            }
        }

        public bool isBusy
        {
            get { return o_isBusy; }
            set
            {
                o_isBusy = value;
                OnPropertyChanged("isBusy");
            }
        }

        #endregion viewProperties

        #region errors

        /**
         * Here we do our best to make sure that the config file and simulation file are in the
         * same directory, and the simfile is saved in the config file without a path.
         **/
        public bool checkModelFileLocation()
        {
            string simFilename = sim.simFile;
            string workingdir = System.IO.Path.GetFullPath(sim.workingDir);

            string dirname = workingdir;  //Generally the filename will just be a filename, no path, so assume it's in the working dir

            if (simFilename.Contains('\\'))  //If it IS a path, get it.
            {
                dirname = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(simFilename));
            }

            if (dirname == workingdir)  //Now SOMETHING should be in dirname.  If it's the workingdir we're happy, just force the plain name for good measure
            {
                sim.simFile = System.IO.Path.GetFileName(sim.simFile);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool checkFileLocation(string saveFilename)
        {
            string workingdir = System.IO.Path.GetFullPath(sim.workingDir);

            string dirname = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(saveFilename));

            if (dirname == workingdir)  //Now SOMETHING should be in dirname.  If it's the workingdir we're happy, just force the plain name for good measure
            {
                sim.simFile = System.IO.Path.GetFileName(sim.simFile);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool checkSaveFileExtension()
        {
            if (System.IO.Path.GetExtension(saveConfigFilename) != ".json")
            {
                return false;
            }
            return true;
        }


        public void displayError(string messageBoxText)
        {
            string caption = "Sim Sinter Configuration UI ERROR";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Warning;
            System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);
        }

        public string checkACMSettings()
        {
            if (o_acm != null)
            {
                { //Snapshot check
                    VariableViewModel snapshotVar = getVariableViewByName("Snapshot", inputVariables);
                    if (snapshotVar != null)
                    {
                        String snapshot_name = (String)snapshotVar.value;
                        if (o_acm.loadSnapshot(snapshot_name) == -1)
                        {
                            return String.Format("Could not find Snapshot {0}.  Please check the name and try again.", snapshot_name);
                        }
                    }
                }

                { //RunMode checks
                    VariableViewModel runMode = getVariableViewByName("RunMode", inputVariables);
                    VariableViewModel homotopy = getVariableViewByName("homotopy", inputVariables);

                    if (runMode != null)
                    {
                        string runVal = (String)runMode.value;
                        if (runVal != "Steady State" && runVal != "Dynamic" && runVal != "Optimization") // took out "Initialization" and "Estimation" options 
                        {
                            string upRunVal = runVal.ToUpper();
                            if (upRunVal.Contains("STEADY O")) {
                                runMode.dfault = "Steady Optimization";
                            }
                            else if (upRunVal.Contains("DYNAMIC O"))
                            {
                                runMode.dfault = "Dynamic Optimization";
                            }
                            else if (upRunVal[0] == 'S')
                            {
                                runMode.dfault = "Steady State";
                            }
                            else if (upRunVal[0] == 'D')
                            {
                                runMode.dfault = "Dynamic";
                            }
                            else if (upRunVal[0] == 'O')
                            {
                                runMode.dfault = "Optimization";
                            }
                            else
                            {
                                return String.Format("Unknown RunMode {0} requested.  Please set RunMode to 'Steady State' or 'Dynamic'", runVal);
                            }
                        }
                        if ( String.Compare((String)runMode.dfault, "Dynamic Optimization", true) == 0) 
                        {
                            return String.Format("Dynamic Optimization is not currently supported by SimSinter.");
                        }
                        if (homotopy != null && ((string)runMode.dfault).Contains("Optimization") && ((int)homotopy.dfault) != 0)
                        {
                            return String.Format("The homotopy solver cannot be used with Optimization RunModes.  Please turn off homotopy or set RunMode to 'Steady State'");
                        }

                    }
                }


                { //TimeSeries checks
                    {
                        VariableViewModel snapshotVar = getVariableViewByName("Snapshot", inputVariables);
                        VariableViewModel runMode = getVariableViewByName("RunMode", inputVariables);

                        if (runMode != null)
                        {
                            string runVal = (String)runMode.value;
                            if (runVal == "Dynamic")
                            {
                                double snapshotVal = 0;
                                if (snapshotVar.value != null && ((String)snapshotVar.value) != "")
                                {
                                    snapshotVal = o_acm.loadSnapshot((String)snapshotVar.value);
                                }

                                VariableViewModel timeSeriesVar = getVariableViewByName("TimeSeries", inputVariables);
                                double[] timeSeriesVals = (double[])timeSeriesVar.value;

                                if (timeSeriesVals.Length == 1 && timeSeriesVals[0] == 0)  //OK, if the user hasn't set the timeseries yet, pick a default from the snapshot/
                                {
                                    timeSeriesVals[0] = snapshotVal + 1;
                                }

                                if (snapshotVal >= timeSeriesVals[0])
                                {
                                    return String.Format("The timeseries must have end times greater than the snapshot start time {0}.", snapshotVal);
                                }

                                for (int ii = 1; ii < timeSeriesVals.Length; ++ii)
                                {
                                    if (timeSeriesVals[ii - 1] >= timeSeriesVals[ii])
                                    {
                                        return String.Format("The timeseries must strictly monotonically increasing. Indexs {0} and {1} violate this.", ii - 1, ii);
                                    }
                                }
                            }

                        }
                    }

                }

            }
            return null;
        }

        public string checkgPROMSErrors()
        {
            String error = null;
            foreach (VariableViewModel vv in allVariables)
            {
                if (vv.path.Contains("__"))
                {
                    error = String.Format("WARNING: Variable path {0} contains double underscores.  This could indicate a bug in your gPROMS file.\n If your Foriegn Object reference looks like this VAR = FO.<Type>__VAR; gPROMS and SimSinter will not interpret it correctly.\n  You must have parenthesis '()' at the end of the reference.\n  See the SimSinter gPROMS Technical Manual Section 5.3.\n", vv.path);
                    break;
                }
            }
            return error;
        }

        #endregion errors

        #region commands

        public void OpenFileBrowserCommand_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = !isOpeningFile;
        }

        public void OpenFileBrowserCommand_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            Microsoft.Win32.OpenFileDialog _fd = new Microsoft.Win32.OpenFileDialog();
            _fd.Filter = "All SimSinter Files (*.json,*.txt,*.bkp,*.apw,*.acmf,*.xlsm,*.gPJ)|*.json;*.txt;*.bkp;*.apw;*.acmf;*.xlsm;*.xls;*.xlsx;*.gPJ|AspenSinter Configs(*.json,*.txt)|*.json;*.txt|Aspen Plus Files(*.bkp,*.apw)|*.bkp;*.apw|Aspen Custom Modeler|*.acmf|gPROMS|*.gPJ|Excel Files|*.xlsm;*.xls;*.xlsx|All Files(*.*)|*.*"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = _fd.ShowDialog();

            if (result == true)
            {
                // The open document is found, launch it
                openConfigFilename = _fd.FileName;
                OpenCommand_Executed(sender, null);
            }

        }

        public void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = !isOpeningFile && openConfigFilename != "";
        }

        public void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            string filename = openConfigFilename;
            isOpeningFile = true;
            isBusy = true;

            //If we get back that we can't write to this directory, ask the user about proceeding
            if (!checkWritePermissions(System.IO.Path.GetDirectoryName(openConfigFilename)))
            {
                if (MessageBox.Show("You do not have write access to the directory containing the file you wish to open.  This can cause serious isses with FOQUS and SimSinter.  Are you sure you wish to proceed?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    isOpeningFile = false;
                    isBusy = false;
                    return;
                }
                else
                {
                    ; //Let the user proceed to be an idiot. 
                }
            }

            //create our background worker and support cancellation
            worker = new BackgroundWorker();

            worker.DoWork += delegate(object s, DoWorkEventArgs doWorkArgs)
            {
                status = String.Format("User Selected File {0}.", filename);

                try
                {

                    string extension = System.IO.Path.GetExtension(filename);

                    if (extension == ".txt" || extension == ".json")
                    {
                        //SinterSingleton.sinterSingleton.sinterConfigFilename = filename;
                        StreamReader inFileStream = new StreamReader(filename);
                        string setupString = "";
                        setupString = inFileStream.ReadToEnd();
                        inFileStream.Close();

                        status = "Attempting to Open Sinter";
                        sim = (sinter.sinter_Sim)sinter.sinter_Factory.createSinterForConfig(setupString);
                        sim.workingDir = System.IO.Path.GetDirectoryName(filename);
                        metaDataStatusText = openedConfigFile;

                        if (!checkModelFileLocation())
                        {
                            string caption = "SimSinter Configuration UI WARNING";
                            MessageBoxButton button = MessageBoxButton.YesNo;
                            MessageBoxImage icon = MessageBoxImage.Warning;
                            string messageBoxText = String.Format("WARNING: Your sinter config file and your simulation model file\n do not not seem to reside in the same directory.\n This is non-standard and may confuse other tools.\n  Would you like to copy your config file to the simulation file directory?\n SimFile: {0}\n ConfigFile dir: {1}\n", sim.simFile, sim.workingDir);
                            MessageBoxResult boxresult = System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);
                            if (boxresult == MessageBoxResult.Yes)
                            {
                                sim.workingDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(sim.simFile));
                                sim.simFile = System.IO.Path.GetFileName(sim.simFile);
                                o_openConfigFilename = System.IO.Path.Combine(sim.workingDir, System.IO.Path.GetFileName(o_openConfigFilename));
                                saveConfig(o_openConfigFilename);  //This is the "copy" operation.
                            }
                        }

                        //Auto Bump the config file version here is a little odd, but it's the only place I could find that will only happen once per file configuration an nowhere else
                        sim.configFileVersion = new Version(sim.configFileVersion.Major, sim.configFileVersion.Minor + 1);  


                    }
                    else if (extension == ".gencrypt")
                    {
                        displayError("gPROMS .gencrypt files cannot be read by SinterConfigGUI.  Please supply a .gPJ file.");
                    }
                    else if (extension == ".gpj" || extension == ".gPJ")
                    {
                        sinter.PSE.sinter_simGPROMSconfig thisGPROMS = new sinter.PSE.sinter_simGPROMSconfig();
                        thisGPROMS.setupFile = new sinter.sinter_JsonSetupFile();
                        thisGPROMS.setupFile.aspenFilename = System.IO.Path.GetFileName(filename);
                        thisGPROMS.setupFile.simDescFile = System.IO.Path.GetFileName(filename);
                        sim = thisGPROMS;
                        sim.workingDir = System.IO.Path.GetDirectoryName(filename);
                        metaDataStatusText = openedSimFile;


                    }
                    else if (extension == ".bkp" || extension == ".apw")
                    {
                        sinter.sinter_SimAspen thisAspen = new sinter.sinter_SimAspen();
                        thisAspen.setupFile = new sinter.sinter_JsonSetupFile();
                        thisAspen.setupFile.aspenFilename = System.IO.Path.GetFileName(filename);
                        thisAspen.setupFile.simDescFile = System.IO.Path.GetFileName(filename);
                        sim = thisAspen;
                        sim.workingDir = System.IO.Path.GetDirectoryName(filename);
                        metaDataStatusText = openedSimFile;


                    }
                    else if (extension == ".acmf")
                    {
                        sinter.sinter_SimACM thisAspen = new sinter.sinter_SimACM();
                        thisAspen.setupFile = new sinter.sinter_JsonSetupFile();
                        thisAspen.setupFile.aspenFilename = System.IO.Path.GetFileName(filename);
                        thisAspen.setupFile.simDescFile = System.IO.Path.GetFileName(filename);
                        sim = thisAspen;
                        sim.workingDir = System.IO.Path.GetDirectoryName(filename);
                        metaDataStatusText = openedSimFile;

                    }
                    else if (extension == ".xlsm" || extension == ".xls" || extension == ".xlsx")
                    {
                        sinter.sinter_SimExcel thisAspen = new sinter.sinter_SimExcel();
                        thisAspen.setupFile = new sinter.sinter_JsonSetupFile();
                        thisAspen.setupFile.aspenFilename = System.IO.Path.GetFileName(filename);
                        thisAspen.setupFile.simDescFile = System.IO.Path.GetFileName(filename);
                        sim = thisAspen;
                        sim.workingDir = System.IO.Path.GetDirectoryName(filename);
                        metaDataStatusText = openedSimFile;


                    }
                    else
                    {
                        displayError(String.Format("File extension {0} is not supported. Unknown file type.", extension));
                    }


                    status = "Attempting to Open the Simulator";
                    try
                    {
                        sim.openSim();
                    }
                    catch (Sinter.SinterConstraintException cons_ex)
                    {
                        string caption = "SimSinter Configuration UI WARNING";
                        MessageBoxButton button = MessageBoxButton.OKCancel;
                        MessageBoxImage icon = MessageBoxImage.Warning;
                        string messageBoxText = String.Format("WARNING: {0}\n If you proceed you can change the constraints.\n Proceed?", cons_ex.Message);
                        MessageBoxResult boxresult = System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);
                        if (boxresult == MessageBoxResult.OK)
                        {
                            //Proceed
                        }
                        else
                        {
                            throw cons_ex;
                        }
                    }

                    sim.Vis = true;
                    sim.dialogSuppress = false;

                    //Special case for converting .txt files to the new format.  Blow away all the defaults.  (We don't expect any users to open .txt files.)
                    if (extension == ".txt")
                    {
                        sim.runSim();
                        sim.initializeDefaults();
                    }


                    //TODO: This is kind of lame, I think we need to break the presenter up so this can go in a constructor
                    buildVariableGrids();

                    status = "Simulator Successfully Opened";
                    // perform task
                    //Run sim here

                    //Make the default save filename
                    saveConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(openConfigFilename), System.IO.Path.GetFileNameWithoutExtension(openConfigFilename)) + ".json";

                    //Make default title if necessary
                    title = makeTitleFromPath(saveConfigFilename);

                    //And ask the WPF thread to open the next (MetaData) Window
                    System.Windows.Threading.Dispatcher winDispatcher = ((System.Windows.Threading.DispatcherObject)sender).Dispatcher;
                    OpenMetaDataPageDelegate winDelegate = new OpenMetaDataPageDelegate(OpenMetaDataPage);
                    //invoke the dispatcher and pass the percentage and max record count
                    winDispatcher.BeginInvoke(winDelegate);

                }

                catch (Exception ex)
                {
                    if (sim != null && sim.simulatorStatus == sinter_simulatorStatus.si_OPEN)
                    {
                        sim.closeSim();
                    }
                    displayError(String.Format("Sinter caught this following exception when trying to open the Simulation File: \n {0}", ex.Message));
                }
                finally
                {
                    isBusy = false;
                    isOpeningFile = false;
                }

            };

            //run the process then show the progress dialog
            worker.RunWorkerAsync(filename);

        }


        public void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        //A simple save command from the user.  Not associated with quitting or changing commands or anything.
        public void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            runChecksAndSave();  //We don't need to check the return result in this case, because all the user tried to do was save.
        }

        public void SaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        //Save As command, launched a browser to allow the user to put in a save command name
        public void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            Microsoft.Win32.SaveFileDialog _fd = new Microsoft.Win32.SaveFileDialog();
            _fd.DefaultExt = ".json"; // Default file extension
            _fd.Filter = "AspenSinter Config (*.json)|*.json;|All Files(*.*)|*.*"; // Filter files by extension
            _fd.InitialDirectory = sim.workingDir;

            // Show open file dialog box
            Nullable<bool> result = _fd.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                saveConfigFilename = _fd.FileName;
            }
            else  //If the user clicked cancel, don't save, just bail.
            {
                return;
            }
            runChecksAndSave();  //We don't need to check the return result in this case, because all the user tried to do was save.
        }


        public void SaveAndQuit_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        public void SaveAndQuit_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            MessageBoxResult saveResult = runChecksAndSave();
            if (saveResult != MessageBoxResult.Cancel)
            {
                saveable = false;
                Application.Current.Shutdown();
            }
        }

        public void UploadToDMF_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = DMFAvailible;
        }

        public void UploadToDMF_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            SaveCommand_Executed(sender, args);  //Gotta save before we can upload
//            string originaldir = Directory.GetCurrentDirectory();
//            Directory.SetCurrentDirectory("");
            // Use ProcessStartInfo class.
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = DMFPath;
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.Arguments = saveConfigFilename;

//            try
//            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using-statement will close.
                using (System.Diagnostics.Process exeProcess = System.Diagnostics.Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
//            }
//            catch
//            {
//            }
//            Directory.SetCurrentDirectory(originaldir);
        }



        public void VectorOrFinishCommand_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        public void VectorOrFinishCommand_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (vectorsOrFinishCheck())
            {
                GotoVectorInitPage_Executed(sender, args);
            }
            else
            {
                SaveAndQuit_Executed(sender, args);
            }
        }

        public void ResetProgram_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        public void ResetProgram_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            saveable = false;
            string caption = "WARNING Sinter Configuration UI WARNING";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;
            string messageBoxText = String.Format("If you reset Sinter Config GUI will lose all of your unsaved progress!\n Are you sure you want to Reset?\n");
            MessageBoxResult boxresult = System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);

            //If they answer 'No' nothing will happen
            if (boxresult == MessageBoxResult.Yes)
            {
                //Close the sim, I think this is probably best.
                o_sim.closeSim();

                System.Windows.Forms.Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
        }

        public void GotoOpenFilePage_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        public void GotoOpenFilePage_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            OpenOpenFilePage();
        }

        public void GotoMetaDataPage_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        public void GotoMetaDataPage_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            moveVariableViewsIntoSinter();
            OpenMetaDataPage();
        }

        public void GotoVectorInitPage_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true; //TODO: Check for vectors, only count TimeSeries if RunMode == Dynamic
        }

        public void GotoVectorInitPage_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            MessageBoxResult saveResult = runChecksAndSave();
            if (saveResult != MessageBoxResult.Cancel)
            {
                OpenVectorInitPage();
            }
        }

        public void GotoVariablesPage_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = saveConfigFilename != "";
        }

        public void GotoVariablesPage_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            MessageBoxResult saveResult = runChecksAndSave();
            if (saveResult != MessageBoxResult.Cancel)
            {
                OpenVariablesPage();
            }
        }

        public void PreviewVariable_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = previewVariablePath.Length > 0;
        }

        public void PreviewVariable_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (o_acm == null)  //Since ACM has doesn't have the variable tree, this stuff doesn't apply
            {
                VariableTreeNodeViewModel foundNode;
                try
                {
                    foundNode = variableTree.rootNode.resolveNode(o_sim.parsePath(previewVariablePath));
                }
                catch (System.Collections.Generic.KeyNotFoundException ex)
                {
                    displayError(ex.Message);
                    return;
                }

                foundNode.IsSelected = true;
            }
            updatePreviewVariable();
        }

        public void PreviewToInput_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = (previewVariable.Count > 0 && o_gproms == null); //Can't make inputs in gproms
        }

        public void PreviewToInput_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            VariableViewModel thisVar = previewVariable[0];
            thisVar.mode = sinter_Variable.sinter_IOMode.si_IN;
            if (!checkForDuplicatePath(allVariables, thisVar))
            {
                displayError("This variable already exists." + System.Environment.NewLine +
                        " Name: " + thisVar.name + System.Environment.NewLine +
                        " Path: " + thisVar.path);
                return;
            }
            if (checkForDuplicateName(allVariables, thisVar) != null)
            {
                createUniqueName(allVariables, thisVar);
            }
            inputVariables.Add(thisVar);
            allVariables.Add(thisVar);
            previewVariable.Clear();
        }

        public void PreviewToOutput_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = previewVariable.Count > 0;
        }

        public void PreviewToOutput_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            VariableViewModel thisVar = previewVariable[0];
            thisVar.mode = sinter_Variable.sinter_IOMode.si_OUT;
            if (!checkForDuplicatePath(allVariables, thisVar))
            {
                displayError("This variable already exists." + System.Environment.NewLine +
                        " Name: " + thisVar.name + System.Environment.NewLine +
                        " Path: " + thisVar.path);
                return;
            }
            if (checkForDuplicateName(allVariables, thisVar) != null)
            {
                createUniqueName(allVariables, thisVar);
            }
            outputVariables.Add(thisVar);
            allVariables.Add(thisVar);
            previewVariable.Clear();

        }

        public void CancelPreview_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = previewVariable.Count > 0;
        }

        public void CancelPreview_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            previewVariable.Clear();
        }

        public void RemoveVariable_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            bool inputsOK = false;
            if (selectedInputVars != null && selectedInputVars.Count > 0)
            {
                foreach (VariableViewModel invar in selectedInputVars)
                {
                    if (!invar.isSetting)
                    {
                        inputsOK = true;
                        break;
                    }
                }
            }
            bool outputsOK = selectedOutputVars != null && selectedOutputVars.Count > 0;

            args.CanExecute = inputsOK || outputsOK;
        }

        public void RemoveVariable_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (inputFocusLast == true && selectedInputVars != null && selectedInputVars.Count > 0)
            {
                //A bit weird, we can't remove the item from the inputVariables list directly, because the selected outputvars are linked to it
                //and it will throw an exception, so make a new list of the vars to remove, then iterate through the new list doing the remove.
                List<VariableViewModel> removalItems = new List<VariableViewModel>();
                foreach (VariableViewModel invar in selectedInputVars)
                {
                    if (!invar.isSetting)  
                    {
                        removalItems.Add(invar);
                    }
                }

                foreach (VariableViewModel invar in removalItems)
                {
                    inputVariables.Remove(invar);
                    allVariables.Remove(invar);
                }

            }

            if (inputFocusLast == false && selectedOutputVars != null && selectedOutputVars.Count > 0)
            {
                //A bit weird, we can't remove the item from the outputVariables list directly, because the selected outputvars are linked to it
                //and it will throw an exception, so make a new list of the vars to remove, then iterate through the new list doing the remove.
                List<VariableViewModel> removalItems = new List<VariableViewModel>();
                foreach (VariableViewModel outvar in selectedOutputVars)
                {
                    removalItems.Add(outvar);
                }

                foreach (VariableViewModel outvar in removalItems)
                {
                    outputVariables.Remove(outvar);
                    allVariables.Remove(outvar);
                }
            }
            
        }

        public void AddInputFile_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        /**
         * When an input file is added on the metadata page, the user uses the file browser to find the file.
         * The it is checked to make sure it is in a directory Turbine can mimic.  A relative path (relative 
         * to the working directory) is eventually saved.
         **/
        public void AddInputFile_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            //Browse add file
            Microsoft.Win32.OpenFileDialog _fd = new Microsoft.Win32.OpenFileDialog();
            _fd.ValidateNames = false; //Gets rid of the "This File is in Use" message (we don't care)
            _fd.Filter = "All Files(*.*)|*.*"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = _fd.ShowDialog();


            if (result != true)
            {
                return;  //If the user cancels, just bail
            }
            string workingdir = System.IO.Path.GetFullPath(sim.workingDir);
            string abs_filename = System.IO.Path.GetFullPath(_fd.FileName);
            string relativePath = makeRelativePath(_fd.FileName, workingdir);  //This will be the eventual answer 

            while (relativePath.StartsWith("..")) //If the file is not in a subdirectory of the working directory, we have a problem
            {
                string caption = "SimSinter Configuration UI WARNING";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;
                string messageBoxText = "";
                messageBoxText = String.Format("ERROR: You are attempting to require a file {0}\n that does not exist in the same directory (or a subdirectory) as your configuration file {1}.\n This is unsupported.\n", abs_filename, workingdir);
                MessageBoxResult boxresult = System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);

                {
                    Microsoft.Win32.SaveFileDialog _fd2 = new Microsoft.Win32.SaveFileDialog();
                    _fd2.ValidateNames = false;
                    _fd2.DefaultExt = ".json"; // Default file extension
                    _fd2.Filter = "All Files(*.*)|*.*"; // Filter files by extension
//                    _fd2.InitialDirectory = sim.workingDir;

                    // Show open file dialog box
                    Nullable<bool> result2 = _fd2.ShowDialog();

                    // Process save file dialog box results
                    if (result2 == true)
                    {
                        workingdir = System.IO.Path.GetFullPath(sim.workingDir);
                        abs_filename = System.IO.Path.GetFullPath(_fd2.FileName);
                        relativePath = makeRelativePath(_fd2.FileName, workingdir);
                    }
                    else  //If the user clicked cancel, don't save, just bail.
                    {
                        return;
                    }

                }
            }

            //If the user has just selected the simFile, ignore it.  That file is already an input file.
            if (relativePath == sim.simFile)
            {
                return;
            }

            /** Now we're going to try to make a hash.  It's not really necessary, because the DMF has to
             * redo the hashes anyway, becuase there is no guarantee the files won't change before upload.
             * Also, open files can't have hashes made.  It's probably a waste, but the code is already here.
             * For now it stays. **/
            try
            {
                string SHA1 = sinter_JsonSetupFile.SHA1HashFile(abs_filename);
                inputFiles.Add(relativePath);
                inputFileHash.Add(SHA1);
                inputFileHashAlgo.Add("sha1");
            }
            catch
            {       
                    inputFiles.Add(relativePath);
                    inputFileHash.Add("");  //We made an attempt to make hash, if we can't just skip it. 
                    inputFileHashAlgo.Add(""); 
            }
        }

        //This abuses the Uri library to take a path and make it relative
        public static string makeRelativePath(string filePath, string referencePath)
        {
            //Make sure there's a trailing slash on the end of the reference directory (working dir)
            referencePath = referencePath.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;

            var fileUri = new Uri(filePath);
            var referenceUri = new Uri(referencePath);
            var rel_Uri = referenceUri.MakeRelativeUri(fileUri);
            string rel_str = rel_Uri.ToString();
            return rel_str.Replace('/', System.IO.Path.DirectorySeparatorChar);
        }

        private void moveInputFilesIntoSinter()
        {
            //First set all the variables to what is in the input/output observeable collections
            List<String> additionalFiles = new List<String>();
            List<String> additionalHash = new List<String>();
            List<String> additionalHashAlgo = new List<String>();
            if (inputFiles.Count > 1)
            {
                for (int ii = 1; ii < inputFiles.Count; ++ii)  //Got to skip the first file, which should always be the simfile
                {
                    additionalFiles.Add(inputFiles[ii]);
                    additionalHash.Add(inputFileHash[ii]);
                    additionalHashAlgo.Add(inputFileHashAlgo[ii]);
                }
            }
            sim.additionalFiles = additionalFiles;
            sim.additionalFilesHash = additionalHash;
            sim.additionalFilesHashAlgo = additionalHashAlgo;
        }
        


        public void RemoveInputFile_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = (selectedInputFiles.Count > 0);
        }

        public void RemoveInputFile_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            //TODO: Must also do SHA1s! 
            if (selectedInputFiles != null && selectedInputFiles.Count > 0)
            {
                //A bit weird, but we can't remove the item from the inputFiles list directly, because it is bound to the display.
                // so make a new list of the vars to remove, then iterate through the new list doing the remove.
                List<String> removalItems = new List<String>();
                foreach (string infile in selectedInputFiles) 
                {
                    if (infile != sim.simFile)
                    {
                        removalItems.Add(infile);
                    }
                }

                foreach (string infile in removalItems)
                {
                    int idx = inputFiles.IndexOf(infile);
                    inputFiles.RemoveAt(idx); //Remove(infile);
                    inputFileHash.RemoveAt(idx);
                    inputFileHashAlgo.RemoveAt(idx);
                }

            }
        }
    


        public void SearchCommand_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            if (o_searchPattern.Length > 0)
            {
                args.CanExecute = true;
            }
            else
            {
                args.CanExecute = false;
            }
        }

        //This event handler cleans up after the search worker
        private void searchworker_RunWorkerCompleted(
          object sender, RunWorkerCompletedEventArgs e)
        {


            if (!worker.CancellationPending)
            {
                foreach (string thisPath in pathesFoundBySearch)
                {
                    if (worker.CancellationPending)
                    {
                        break;
                    }
                    /**                    if (o_acm.ParseVectorIndex(thisPath) == 0) //Whoops, not all arrays index from 0, BetaKe doesn't
                                        {
                                            int lastLParen = thisPath.LastIndexOf("(");
                                            string vectorPath = thisPath.Substring(0, lastLParen);
                                            FoundVariableViewModel thisVector = new FoundVariableViewModel(vectorPath);
                                            thisVector.isVector = true;
                                            o_foundVariables.Add(thisVector);
                                        }
                                        o_foundVariables.Add(new FoundVariableViewModel(thisPath));**/

                    //If this path looks like a vector, (ie has a number in some parenthesis at the end)
                    //Get the vector name.  If that name doesn't exist in the list, add it
                    if (o_acm.ParseVectorIndex(thisPath) != -1) //If this variable as a number in parentesis, it's a vector
                    {
                        int lastLParen = thisPath.LastIndexOf("(");
                        string vectorPath = thisPath.Substring(0, lastLParen); //hack off the (?) to get a vector path.
                        FoundVariableViewModel thisVector = new FoundVariableViewModel(vectorPath);
                        thisVector.isVector = true;
                        if (!o_foundVariables.Contains(thisVector))
                        {
                            o_foundVariables.Add(thisVector);
                        }
                    }
                    o_foundVariables.Add(new FoundVariableViewModel(thisPath));

                }
            }

            searchCancelBox.Close();
            if (pathesFoundBySearch.Count == 0)
            {
                displayError("Your search returned 0 results.");
            }

            pathesFoundBySearch.Clear();

        }
        public void SearchCommand_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            o_foundVariables.Clear();

            //create our background worker and support cancellation
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.RunWorkerCompleted += searchworker_RunWorkerCompleted;

            worker.ProgressChanged += delegate(object s, ProgressChangedEventArgs aa)
            {
                progress = aa.ProgressPercentage;
            };

            worker.DoWork += delegate(object s, DoWorkEventArgs doWorkArgs)
            {

                pathesFoundBySearch = o_acm.search(o_searchPattern, o_searchType, o_search_fixed, o_search_free,
                   o_search_rateinitial, o_search_initial, o_search_parameters, o_search_algebraics, o_search_state,
                   o_search_inactive, ref worker);

            };

            //run the process then show the progress dialog
            worker.RunWorkerAsync();

            searchCancelBox = new SearchCancelBox();
            searchCancelBox.ShowDialog();

        }


        public void HeatIntegrationCommand_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        public void HeatIntegrationCommand_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            IList<sinter_IVariable> heatVars = o_sim.getHeatIntegrationVariables();
            foreach (sinter_IVariable heatVar in heatVars)
            {
                VariableViewModel heatVarVM = new VariableViewModel((sinter_Variable)heatVar);
                if (!inputVariables.Contains(heatVarVM))
                {
                    inputVariables.Add(heatVarVM);
                    allVariables.Add(heatVarVM);
                }
            }

        }


        #endregion commands

        #region helperFunctions

        private VariableViewModel getVariableViewByName(String varName, ObservableCollection<VariableViewModel> varCollection)
        {
            foreach(VariableViewModel vview in varCollection) {
                if (vview.name == varName)
                {
                    return vview;
                }
            }
            return null;
        }

        private void updatePreviewVariable()
        {
            //When the preview path changes, we also want to change the preview variable
            if (previewVariable.Count > 0)
            {
                previewVariable.Clear();
            }
            sinter_Variable thisVar = sinter.sinter_HelperFunctions.makeNewVariable(sim, o_previewVariablePath);
            if (thisVar != null)
            {
                previewVariable.Add(new VariableViewModel(thisVar));
            }
        }

        private void moveVariableViewsIntoSinter()
        {
            //First set all the variables to what is in the input/output observeable collections
            o_sim.setupFile.clearVariablesAndSettings();
            foreach (VariableViewModel varVM in inputVariables)
            {
                if (varVM.variable.isSetting)
                {
                    o_sim.setupFile.addSetting((sinter_Variable)varVM.variable);
                }
                else
                {
                    o_sim.setupFile.addVariable((sinter_Variable)varVM.variable);
                }
            }
            foreach (VariableViewModel varVM in outputVariables)
            {
                o_sim.setupFile.addVariable((sinter_Variable)varVM.variable);
            }

            if (o_acm != null)  //Add back in timeunits and minstepsize
            {
                IList<sinter_Variable> settings = o_sim.getSettings();
                foreach (sinter_Variable setting in settings)
                {
                    if (setting.name == "TimeUnits" || setting.name == "MinStepSize")  //Sigh, special case to skip this read-only settings.  Maybe I should have a property for this.
                    {
                        if (o_acm.getSettingByName(setting.name) == null)
                        {
                            o_acm.setupFile.addSetting(setting);
                        }
                    }
                }

            }
        }

        public void addSettingsToInputs()
        {
            IList<sinter_Variable> settings = o_sim.getSettings();
            foreach (sinter_Variable setting in settings)
            {
                VariableViewModel settingVM = new VariableViewModel(setting);
                if (settingVM.name != "TimeUnits" && settingVM.name != "MinStepSize")  //Sigh, special case to skip this read-only settings.  Maybe I should have a property for this.
                {
                    if (!inputVariables.Contains(settingVM))
                    {
                        inputVariables.Insert(0, settingVM);
                        allVariables.Insert(0, settingVM);
                    }
                }
            }

            //On the variables page there is a piece of text (vectorsOrFinishText) that changes based on the content of the input variables.
            //These delegates will keep an eye on that change.
            vectorsOrFinishText = "foo";  //This text doesn't matter.  It set by and internal check
            System.Collections.Specialized.NotifyCollectionChangedEventHandler vectorsOrFinishTextdelegate = new System.Collections.Specialized.NotifyCollectionChangedEventHandler(vectorsOrFinishTextMethod);
            inputVariables.CollectionChanged += vectorsOrFinishTextdelegate;

            VariableViewModel runMode = getVariableViewByName("RunMode", inputVariables);  //If we have a runMode, we need to keep an eye on it as well
            if (runMode != null)
            {
                PropertyChangedEventHandler vectorsOrFinishTextdelegate2 = new PropertyChangedEventHandler(vectorsOrFinishTextMethod2);
                runMode.PropertyChanged += vectorsOrFinishTextdelegate2;
            }

        }

        public bool checkForDuplicatePath(ObservableCollection<VariableViewModel> varlist, VariableViewModel newVariable)
        {
            foreach (VariableViewModel var in varlist)
            {
                if (var.path == newVariable.path)
                {
                    return false;
                }
            }
            return true;
        }


        public VariableViewModel checkForDuplicateName(ObservableCollection<VariableViewModel> varlist, VariableViewModel newVariable)
        {
            foreach (VariableViewModel var in varlist)
            {
                if (var.name == newVariable.name && var.path != newVariable.path)
                {
                    return var;
                }
            }
            return null;
        }

        public void renameAllForDuplicates()
        {
            foreach (VariableViewModel var in allVariables)
            {
                VariableViewModel dupvar = checkForDuplicateName(allVariables, var);
                if (dupvar != null)
                {
                    createUniqueName(allVariables, dupvar);
                    displayError("There was more than one variable with the name " + var.name + ". " + System.Environment.NewLine +
                    "Variable 1 path: " + var.path + System.Environment.NewLine +
                    "Variable 2 path: " + dupvar.path + System.Environment.NewLine +
                    "Variable 2 has been given a new name " + dupvar.name + System.Environment.NewLine +
                    "Your file will be saved with the new name.");
                }
            }
        }

        public string getUniqueNameSuffix()
        {
            o_uniqueNameSuffix++;
            return o_uniqueNameSuffix.ToString();
        }

        public void createUniqueName(ObservableCollection<VariableViewModel> varlist, VariableViewModel varToRename)
        {
            string baseName = varToRename.name;
            varToRename.name = baseName + getUniqueNameSuffix();
            while (checkForDuplicateName(varlist, varToRename) != null)
            {
                varToRename.name = baseName + getUniqueNameSuffix();
            }
        }

        //Saves the sinter config file, but has some consitencey checks that might fail.   
        //If the checks fail, the user will be asked YesNoCancel.  
        //Yes: Save anyway
        //No: Don't save, but continue whatever was going on.  (May happen on exit, or between pages)
        //Cancel: Don't save, and try to stop whatever was going on.  (Can't stop shutdown/exit commands from Windows for example.)
        public MessageBoxResult runChecksAndSave()
        {
            renameAllForDuplicates();
            String errorStr = null;
            if(o_acm != null) {
                errorStr = checkACMSettings();
            }
            if (o_gproms != null)
            {
                errorStr = checkgPROMSErrors();
            }
            if (errorStr != null)
            {
                string caption = "SimSinter Configuration UI WARNING";
                MessageBoxButton button = MessageBoxButton.YesNoCancel;
                MessageBoxImage icon = MessageBoxImage.Warning;
                string messageBoxText = String.Format("{0}\n Save Anyway?\n", errorStr);
                MessageBoxResult boxresult = System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);
                if (boxresult == MessageBoxResult.Yes)
                {
                    sim.simFile = System.IO.Path.Combine(sim.workingDir, sim.simFile);
                }
                else
                {
                    return boxresult;  //No save, just bail out
                }

            }

            if (!isValidTitle(title))
            {
                string oldtitle = title;
                title = makeTitleFromPath(oldtitle);
                string caption = "SimSinter Configuration UI WARNING";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;
                string messageBoxText = String.Format("Title contained invalid characters.  Only letters, numbers, or \"_\" are allowed.\n Title has be automatically changed from:\n{0}\nto:\n{1}\n", oldtitle, title);
                MessageBoxResult boxresult = System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);
            }

            if (saveConfigFilename == null ||
                saveConfigFilename == "")
            {
                Microsoft.Win32.SaveFileDialog _fd = new Microsoft.Win32.SaveFileDialog();
                _fd.DefaultExt = ".json"; // Default file extension
                _fd.Filter = "SimSinter Config (*.json)|*.json;|All Files(*.*)|*.*"; // Filter files by extension
                _fd.InitialDirectory = sim.workingDir;

                // Show open file dialog box
                Nullable<bool> result = _fd.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    saveConfigFilename = _fd.FileName;
                }
                else  //If the user clicked cancel, don't save, just bail.
                {
                    return MessageBoxResult.Cancel;
                }
            }
            moveInputFilesIntoSinter();
            moveVariableViewsIntoSinter();

            bool badLocation = !checkFileLocation(saveConfigFilename);
            bool badExtension = !checkSaveFileExtension();
            while (badLocation || badExtension)
            {
                string caption = "SimSinter Configuration UI WARNING";
                MessageBoxButton button = MessageBoxButton.YesNo;
                MessageBoxImage icon = MessageBoxImage.Warning;
                string messageBoxText = "";
                if (badLocation)
                    messageBoxText = String.Format("WARNING: You are attempting to save your sinter config file to a different location:\n{0}\n than your simulation model file\n{1}\n This is non-standard and may confuse other tools.\n  Are you sure you want to do this? ('No' will reopen the filebrowser to the simulation file location.)\n", sim.workingDir, System.IO.Path.GetDirectoryName(saveConfigFilename));
                else if (badExtension)
                    messageBoxText = String.Format("WARNING: You are attempting to save your sinter config file with the extension {0} rather than .json.\n This is non-standard and may confuse other tools.\n  Are you sure you want to do this? ('No' will reopen the filebrowser to the simulation file location.)\n", System.IO.Path.GetExtension(saveConfigFilename));
                else
                    messageBoxText = "SinterConfigGUI bug\n";
                MessageBoxResult boxresult = System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);
                if (boxresult == MessageBoxResult.Yes)
                {
                    sim.simFile = System.IO.Path.Combine(sim.workingDir, sim.simFile);
                    break;
                }
                else
                {
                    Microsoft.Win32.SaveFileDialog _fd = new Microsoft.Win32.SaveFileDialog();
                    _fd.DefaultExt = ".json"; // Default file extension
                    _fd.Filter = "SimSinter Config (*.json)|*.json;|All Files(*.*)|*.*"; // Filter files by extension
                    _fd.InitialDirectory = sim.workingDir;

                    // Show open file dialog box
                    Nullable<bool> result = _fd.ShowDialog();

                    // Process save file dialog box results
                    if (result == true)
                    {
                        saveConfigFilename = _fd.FileName;
                    }
                    else  //If the user clicked cancel, don't save, just bail.
                    {
                        return MessageBoxResult.Cancel;
                    }

                }
                badLocation = !checkFileLocation(saveConfigFilename);
                badExtension = !checkSaveFileExtension();
            }

            // Save document
            saveConfig(saveConfigFilename);

            return MessageBoxResult.Yes;
        }


        public void saveConfig(string saveConfigFilename)
        {
            Dictionary<String, Object> outputDict = sinter.sinter_JsonSetupFile.generateConfigDictionary(o_sim);
            JsonSerializerSettings jss = new JsonSerializerSettings();
            jss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            string jsonOutput = JsonConvert.SerializeObject(outputDict, Formatting.Indented, jss);

            try
            {
                StreamWriter outStream = new StreamWriter(saveConfigFilename);
                outStream.WriteLine(jsonOutput);
                outStream.Close();
            }
            catch (UnauthorizedAccessException)
            {
                displayError("You do not seem to have write access to " + saveConfigFilename + ".  Your file has NOT been saved!");
            }

            //Write out the file for foqus to read
            if (o_infoForFoqusFile != "")
            {
                try
                {

                    StreamWriter foutStream = new StreamWriter(o_infoForFoqusFile);
                    foutStream.WriteLine(saveConfigFilename);
                    foutStream.Close();
                }
                catch (UnauthorizedAccessException)
                {
                    ;  //Do nothing, if we've gotten this far it's a bit late for us to do much.
                }
            }
        }


        bool checkWritePermissions(string dirname)
        {
            try
            {
                //Find a file we can test without hurting something.
                string tmpfilename = "tmp";
                int tmpcount = 0;
                while (System.IO.File.Exists(System.IO.Path.Combine(dirname, tmpfilename + tmpcount.ToString())))
                {
                    tmpcount++;
                }
                string full_tmpfilename = System.IO.Path.Combine(dirname, tmpfilename + tmpcount.ToString());
                StreamWriter outStream = new StreamWriter(full_tmpfilename);
                outStream.WriteLine("test test");
                outStream.Close();

                System.IO.File.Delete(full_tmpfilename);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return true;
        }

        //Returns true if we should have the user check out vector variables.
        //This should happen if the user has a vector input variable
        //unless that input variable is "TimeSeries" and "RunMode" != Dynamic.  That case doesn't count.
        public bool vectorsOrFinishCheck()
        {
            foreach (VariableViewModel vvm in inputVariables)
            {
                if (vvm.isVec)
                {
                    if (vvm.name == "TimeSeries")
                    {
                        VariableViewModel runMode = getVariableViewByName("RunMode", inputVariables);
                        if (runMode != null && ((String)runMode.value) == "Dynamic")
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        //Returns a default title made up from the sinter config filename, minus any non alpha numeric characters
        string makeTitleFromPath(string pathname)
        {
            string titleStr = System.IO.Path.GetFileNameWithoutExtension(pathname);
            
            //Replace any non-alphanumeric characters with the empty string.
            Regex rgx = new Regex("[^a-zA-Z0-9_]");
            titleStr = rgx.Replace(titleStr, ""); 
           
            return titleStr;
        }

        //Checks that the title only consists of alpha numeric characters or '_'
        bool isValidTitle(string title)
        {

            //Replace any non-alphanumeric characters with the empty string.
            Regex rgx = new Regex("^[a-zA-Z0-9_]+$");
            if (rgx.IsMatch(title))
            {
                return true;
            }
            return false;
        }


        #endregion helperFunctions
    }
}
