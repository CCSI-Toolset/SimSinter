using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using VariableTree;
using System.Threading;
using System.Timers;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sinter
{
    public class sinter_SimACM : sinter_InteractiveSim, AspenCustomModelerLibrary.IAspenModelerEvents 
    {
        #region data

        //Private o_acm As AspenCustomModelerLibrary.IAspenModeler
        AspenCustomModelerLibrary.IAspenModeler o_acm;
        System.Timers.Timer o_stopTimer = new System.Timers.Timer();

        //Syncronization of running and stopping the simulation.  
        //o_terminateMonitor is a condition variable that is signaled when either the simulation
        //has completed, or the user wants to stop it early.
        private AutoResetEvent o_terminateMonitor;
        //Set to True when the simulation has completed, but runSim hasn't handled it yet
        private bool o_simPaused;
        //Set to true when stopSim is called, but runSim hasn't stopped the Sim Yet
        private bool o_stopSim;
        //When stop is called, a timeOut is set up. If it times out the stop has failed.
        private bool o_stopTimedOut;

        private bool o_homotopy;
        private int o_printLevel;
        private string o_runMode;
        private string o_snapshot;
        private string o_script;

        //Boolean variables for controlling what is visible from the simulation.
        //Should match what's in the simulation, these only exist to allow 
        //the options to be set when the simulation itself is not availible.
        bool o_visible = false;
        bool o_dialogSuppress = true;

        private double[] o_TimeSeries = new double[1];

        public VariableTree.VariableTree o_VariableTree;
        // Log message variables.  ACM keeps a log of messages, but we may not want the early stuff,
        // and if we get errors from the log multiple times we don't want to get duplicates.  So we 
        // keep track of log line we start out interested in.

        private int o_startMessageNum = 0;
        //The status of the last run or any errors

        //The messages we've gotten so far
        private List<string> o_simulationMessages;

        //The settings in our prefered display order
        String[] orderedSettings = {"RunMode", 
                                       "printlevel",
                                       "homotopy",
                                       "Snapshot",
                                       "TimeSeries",
                                       "TimeUnits",
                                       "MinStepSize",
                                       "Script" };


        #endregion data

        #region constructors

        // Dim o_acm As Object
        public sinter_SimACM()
        {
            o_availibleSettings = new Dictionary<string, Tuple<set_setting, get_setting, sinter_Variable.sinter_IOType>>
            {
                {"homotopy", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_homotopy, getobj_homotopy, sinter_Variable.sinter_IOType.si_INTEGER)},
                {"printlevel", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_printLevel, getobj_printLevel, sinter_Variable.sinter_IOType.si_INTEGER)},
                {"RunMode", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_runMode, getobj_runMode, sinter_Variable.sinter_IOType.si_STRING)},
                {"Snapshot", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_snapshot, getobj_snapshot, sinter_Variable.sinter_IOType.si_STRING)},
                {"TimeSeries", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_TimeSeries, getobj_TimeSeries, sinter_Variable.sinter_IOType.si_DOUBLE_VEC)}, 
                {"TimeUnits", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_timeUnits, getobj_timeUnits, sinter_Variable.sinter_IOType.si_STRING)},
                {"MinStepSize", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_minStepSize, getobj_minStepSize, sinter_Variable.sinter_IOType.si_DOUBLE)},
                {"Script", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_script, getobj_script, sinter_Variable.sinter_IOType.si_STRING)}
            };

            o_homotopy = false;
            o_printLevel = 0;        //LOW print level by default

            o_runMode = "Steady State";
            o_snapshot = "";
            o_script = "";

            runStatus = sinter.sinter_AppError.si_SIMULATION_NOT_RUN; // sinter.sinter_AppError.si_OKAY;

            o_simulationMessages = new List<String>();

            o_terminateMonitor = new AutoResetEvent(false);
            //Set to True when the simulation has completed, but runSim hasn't handled it yet
            o_simPaused = false;
            //Set to true when stopSim is called, but runSim hasn't stopped the Sim Yet
            o_stopSim = false;

            simName = "Aspen Custom Modeler";

        } 

        #endregion constructors

        #region properties 
        public override string setupfileKey { get { return "aspenfile"; } }

        public bool closed
        {
            get
            {
                return o_acm == null;
            }
        }

        public bool homotopy
        {
            get
            {
                return o_homotopy;
            }
            set
            {
                o_homotopy = value;
                if (runMode.Contains("Optimization")) // sanity check: homotopy cannot be used with Optimization
                {
                    o_homotopy = false;
                }
            }
        }

        public int printLevel
        {
            get
            {
                return o_printLevel;
            }
            set
            {
                o_printLevel = value;
                if (o_acm != null)
                {
                    o_acm.Simulation.Options.PrintLevel = value; //Set it in the sim ASAP
                }
            }
        }

        public string runMode
        {
            get
            {
                return o_runMode;
            }
            set
            {
                o_runMode = value;
                if (runMode.Contains("Optimization")) // sanity check: homotopy cannot be used with Optimization
                {
                    homotopy = false;
                }
            }
        }

        public string snapshot
        {
            get
            {
                return o_snapshot;
            }
            set
            {
                o_snapshot = value;
                if (loadSnapshot(o_snapshot) == -1)
                {
                    throw new ArgumentException(string.Format("ACM failed to load snapshot {0}.", o_snapshot));
                }
            }
        }
        
        public string script
        {
            get
            {
                return o_script;
            }
            set
            {
                o_script = value;
            }
        }

        public double[] TimeSeries
        {
            get
            {
                return o_TimeSeries;
            }
            set
            {
                o_TimeSeries = value;
            }
        }

        public void setobj_homotopy(object value)
        {
            homotopy = Convert.ToBoolean(value);
        }

        public void setobj_printLevel(object value)
        {
            printLevel = Convert.ToInt32(value);
        }

        public void setobj_runMode(object value)
        {
            runMode = (String)value;
        }

        public void setobj_snapshot(object value)
        {
            snapshot = (String)value;
        }
        
        public void setobj_script(object value)
        {
            script = (String)value;
        }

        public void setobj_TimeSeries(object value)
        {
            TimeSeries = (double[])value;
        }

        public void setobj_minStepSize(object value)  //minStepSize is read only
        {
        }

        public void setobj_timeUnits(object value)   //timeUnits is read only
        {
        }


        public object getobj_homotopy()
        {
            return homotopy;
        }
        
        public object getobj_script()
        {
            return script;
        }

        public object getobj_printLevel()
        {
            return printLevel;
        }

        public object getobj_runMode()
        {
            return runMode;
        }

        public object getobj_snapshot()
        {
            return snapshot;
        }

        public object getobj_TimeSeries()
        {
            return TimeSeries;
        }

        public object getobj_minStepSize()
        {
            if (o_acm != null)
            {
                return o_acm.Simulation.Options.Integration.MinStepSize;
            }
            else
            {
                return -1;
            }
        }

        public object getobj_timeUnits()
        {
            if (o_acm != null)
            {
                return o_acm.Simulation.Options.TimeSettings.CommunicationUnits;
            }
            else
            {
                return "ACM not open.";
            }
        }

        public override char pathSeperator
        {
            get
            {
                return '.';
            }
        }

        #endregion properties 

        #region parsing

        public IList<string> splitPath(string path)
        {

            while ((path.Length > 0 && path[0] == pathSeperator))
            {
                path = path.Substring(1);
            }

            if ((path.Length == 0))
            {
                return new List<string>();
            }

            return path.Split(pathSeperator).ToList();
        }

        public override string combinePath(IList<string> splitPath)
        {
            string path = splitPath[0];
            for (int ii = 1; ii < splitPath.Count; ++ii)
            {
                path += pathSeperator + splitPath[ii];
            }
            return path;
        }

        public override int guessVectorSize(string path)
        {
            dynamic vector = o_acm.Simulation.Flowsheet.resolve(path);
            return vector.count;
        }

        public IList<int> getVectorIndicies(string path)
        {
            IList<string> paths = search(path + "(*)");
            List<int> indicies = new List<int>();
            foreach (dynamic thisPath in paths)
            {
                dynamic index = ParseVectorIndex(thisPath);
                indicies.Add(index);
            }

            indicies.Sort();
            return indicies;
        }

        public override int[] getVectorIndicies(string path, int size)
        {
            IList<string> paths = search(path + "(*)");
            List<int> indicies = new List<int>();
            foreach (string thisPath in paths)
            {
                int index = ParseVectorIndex(thisPath);
                indicies.Add(index);
            }
            indicies.Sort();
            return indicies.ToArray();
        }

        #endregion parsing

        #region data-tree

        //* 
        //         * void makeDataTree
        //         * 
        //         * This function generates a tree based on the variables availible in the simulation.  All input and
        //         * output variables.  This is used primarily for the Sinter Config GUI.
        //         

        public override void makeDataTree()
        {
            o_VariableTree = new VariableTree.VariableTree(splitPath, pathSeperator);

            VariableTree.VariableTreeNode rootNode = new VariableTree.VariableTreeNode("", "", pathSeperator);
            dynamic allNodes = o_acm.Simulation.Flowsheet.FindMatchingVariables("~");
            //Dim normout As StreamWriter
            //        normout = New StreamWriter("ACMMatching.txt")

            foreach (dynamic child in allNodes)
            {
                string childPath = null;
                childPath = child.Name;
                rootNode.addNode(splitPath(childPath));
            }

            o_VariableTree.rootNode = rootNode;

            //Remove the Dummy Children (those are only required when doing incremental tree building
            rootNode.traverse(rootNode, thisNode =>
            {
                if ((thisNode.o_children.ContainsKey("DummyChild")))
                {
                    thisNode.o_children.Remove("DummyChild");
                }
            });

        }

        //* 
        // void startDataTree
        // 
        // This function generates the root of a variable tree.  It does not fill in any child nodes.  This is
        // useful for generating the tree as the user opens nodes in the SinterConfigGUI 
        //
        public override void startDataTree()
        {
            o_VariableTree = new VariableTree.VariableTree(splitPath, pathSeperator);

            VariableTree.VariableTreeNode rootNode = new VariableTree.VariableTreeNode("", "", pathSeperator);
            dynamic rootChildren = o_acm.Simulation.Flowsheet.FindMatchingVariables("*");
            rootNode.o_children.Remove("DummyChild");

            foreach (dynamic child in rootChildren)
            {
                string childPath = null;
                childPath = child.Name;
                rootNode.addNode(splitPath(childPath));
            }

            dynamic childNodes = o_acm.Simulation.Flowsheet.FindMatchingVariables("*.*");

            foreach (dynamic child in childNodes)
            {
                string childPath = null;
                childPath = child.Name;
                List<string> arrayPath = new List<string>();
                arrayPath.Add( splitPath(childPath)[0] );
                dynamic childName = arrayPath[0];
                if ((!rootNode.o_children.ContainsKey(childName)))
                {
                    rootNode.addNode(arrayPath);
                }

            }


            o_VariableTree.rootNode = rootNode;

        }

        public override VariableTree.VariableTreeNode findDataTreeNode(IList<string> pathArray)
        {
            return findDataTreeNode(pathArray, o_VariableTree.rootNode);
        }

        //* Leftmost name in the path refers to child of "ThisNode"
        //
        private VariableTree.VariableTreeNode findDataTreeNode(IList<string> pathArray, VariableTree.VariableTreeNode thisNode)
        {

            if ((thisNode.o_children.ContainsKey("DummyChild")))
            {
                thisNode.o_children.Remove("DummyChild");
                dynamic thisAspenNode = o_acm.Simulation.Flowsheet.resolve(thisNode.path);

                dynamic childNodes = null;
                try
                {
                    childNodes = o_acm.Simulation.Flowsheet.FindMatchingVariables(thisNode.path + ".*");
                    //                childNodes = thisAspenNode.FindMatchingVariables("*")
                }
                catch (Exception Ex)
                {
                    if ((pathArray.Count == 0))
                    {
                        return thisNode;
                    }
                    else
                    {
                        throw Ex;
                    }
                }


                foreach (dynamic child in childNodes)
                {
                    string childPath = null;
                    childPath = child.Name;
                    dynamic parsedPath = splitPath(childPath);
                    dynamic childVariableTreeNode = new VariableTree.VariableTreeNode(parsedPath.Last, childPath, pathSeperator);
                    thisNode.addChild(childVariableTreeNode);
                }
            }

            if ((pathArray.Count == 0))
            {
                return thisNode;
            }
            else
            {
                dynamic childName = pathArray[0];
                pathArray.RemoveAt(0);
                return findDataTreeNode(pathArray, thisNode.o_children[childName]);
            }
        }

        public VariableTreeNode makeDataTreeNode(dynamic aspenNode, VariableTreeNode parent)
        {
            return null;
        }

        //Some AspenPlus nodes cannot be included in the data tree, but all ACM nodes can be, so this function does nothing.
        private bool isIllegalNode(String name, String parentName)
        {
            return false;
        }

        #endregion data-tree

        #region units

        protected Dictionary<string, string> aspen2standard = new Dictionary<string, string>
        {
//        {"sqft", "ft^2"}, //square feet (seriously aspen?)
	    {"C", "degC"},    //Celcius
        {"F", "degF"}     //Fahrenheit
        };

        public string convertAspenUnitsToStandard(string aspenUnitString)
        {
            if (aspenUnitString != null && aspenUnitString != "")
            {
                string result;
                //See if there is a conversion for Aspen unit string to a standard unit string 
                //If there is, the correct name will appear in "out result."
                //If not, TryGetValue will return false, which means the aspen string is probably OK, so use it.
                if (!aspen2standard.TryGetValue(aspenUnitString, out result))
                {
                    result = aspenUnitString;
                }
                return result;
            }
            return aspenUnitString;
        }


        public String getBaseUnits(String path)
        {
            try
            {
                return o_acm.Simulation.Flowsheet.resolve(path).defaultunit;
            }
            catch
            {
                return "";
            }
        }

        public override String getCurrentUnits(String path)
        {
            try
            {
                return o_acm.Simulation.Flowsheet.resolve(path).units;
            }
            catch
            {
                return "";
            }
        }

        public override String getCurrentUnits(String path, int[] indicies)
        {
            dynamic node = o_acm.Simulation.Flowsheet.resolve(path);
            try
            {
                return node.Item(indicies[0]).units;
            }
            catch
            {
                return "";
            }
        }
        #endregion units

        #region acm-properties

        /* Get a list of all availible settings for this simulation as sinter_Variables 
         * Specialized for ACM because the ordering of the settings matters.
         */
        public override IList<sinter_Variable> getSettings()
        {
            List<sinter_Variable> settings = new List<sinter_Variable>();
            foreach (String settingName in orderedSettings) //KeyValuePair<string, Tuple<set_setting, get_setting, sinter_Variable.sinter_IOType>> entry in o_availibleSettings)
            {
                Tuple<set_setting, get_setting, sinter_Variable.sinter_IOType> tuple = o_availibleSettings[settingName];
                sinter_Variable thisSetting = sinter_Factory.createVariable(tuple.Item3);
                string[] addressString = { "setting(" + settingName + ")" };
                thisSetting.init(settingName, sinter_Variable.sinter_IOMode.si_IN, tuple.Item3, "Simulation specific setting: " + settingName, addressString);
                thisSetting.Value = tuple.Item2();
                thisSetting.dfault = thisSetting.Value;
                settings.Insert(0, thisSetting);  //Ordered backwards in teh GUI for some reason, hack fix.
            }
            return settings;
        }


        public override String getCurrentDescription(String path)
        {
            try
            {
                return o_acm.Simulation.Flowsheet.resolve(path).description;
            }
            catch
            {
                return "";
            }
        }

        public override String getCurrentName(String path)
        {
            try
            {
                return o_acm.Simulation.Flowsheet.resolve(path).name;
            }
            catch (Exception)
            {
                return "";
            }

        }


        public override sinter_Variable.sinter_IOType guessTypeFromSim(string path)
        {
            try
            {
                dynamic var = o_acm.Simulation.Flowsheet.resolve(path);
                if ((var != null))
                {
                    //Is it a vector?
                    try
                    {
                        dynamic pathes = null;
                        pathes = search(path + "(*)");
                        //If we got multiple paths back from that search string, it should be a vector
                        if ((pathes.Count > 0))
                        {
                            dynamic anIndex = ParseVectorIndex(pathes[0]);
                            //Grab one of those (basically random) and get it's index
                            dynamic thisVecValue = var.Item(anIndex).Value("CurrentUnits");
                            //Get the value at that index and see it's type.
                            //that's the type of the array
                            if ((thisVecValue is double))
                            {
                                return sinter_Variable.sinter_IOType.si_DOUBLE_VEC;
                            }
                            else if ((thisVecValue is int))
                            {
                                return sinter_Variable.sinter_IOType.si_INTEGER_VEC;
                            }
                            else if ((thisVecValue is string))
                            {
                                return sinter_Variable.sinter_IOType.si_STRING_VEC;
                            }
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message); //GET RID?
                    }
                    //If we got here, it's not an array, so it must be a scalar
                    dynamic thisValue = var.Value("CurrentUnits");
                    if ((thisValue is double))
                    {
                        return sinter_Variable.sinter_IOType.si_DOUBLE;
                    }
                    else if ((thisValue is int))
                    {
                        return sinter_Variable.sinter_IOType.si_INTEGER;
                    }
                    else if ((thisValue is string))
                    {
                        return sinter_Variable.sinter_IOType.si_STRING;
                    }

                }
                else
                {
                    return sinter_Variable.sinter_IOType.si_UNKNOWN;
                }
            }
            catch (Exception)
            {
                return sinter_Variable.sinter_IOType.si_UNKNOWN;
            }

            return sinter_Variable.sinter_IOType.si_UNKNOWN;
        }

        //clear homotopy information  (Needs to be done if homotopy is on, no-op if homotopy is off
        private void clearHomotopy()
        {
            o_acm.Simulation.Homotopy.HomotopyEnabled = false;
            o_acm.Simulation.Homotopy.RemoveAll();
        }


        public void presolve()
        {
            //run presolve scripts
            foreach (dynamic blk in o_acm.Simulation.Flowsheet.Blocks)
            {
                //  if you call the presolve script on a block with no 
                // presolve script an exception is thrown, just ignore it.
                try
                {
                    o_acm.Simulation.Flowsheet.resolve(blk.Name).presolve();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                }


            }
        }


        //<Summary>
        // Suppress Aspen UI (Mostly keeps Aspen invisible)
        //</Summary>
        public override bool Vis
        {
            get
            {
                if (o_acm == null)
                {
                    return o_visible;
                }
                else
                {
                    o_visible = o_acm.Visible;
                    return o_acm.Visible;
                }
            }
            set
            {
                if (o_acm == null)
                {
                    o_visible = value;
                }
                else
                {
                    o_acm.Application.Visible = value;
                    o_acm.Interactive = value;
                    o_acm.Visible = value;
                    o_visible = o_acm.Visible;
                }
            }
        }

        
        //<Summary>
        // Suppress Aspen Dialog Boxes (Helps keep Aspen invisible)
        //</Summary>
        public override bool dialogSuppress
        {
            //In AspenPlus you suppressDialogs, in ACM you make error messages invisble
            get
            {
                if (o_acm == null)
                {
                    return o_dialogSuppress;
                }
                else
                {
                    o_dialogSuppress = !o_acm.ErrorMsgBoxesVisible;
                    return !o_acm.ErrorMsgBoxesVisible;
                }
            }
            set
            {
                if (o_acm == null)
                {
                    o_dialogSuppress = value;
                }
                else
                {
                    o_acm.ErrorMsgBoxesVisible = !value;
                    o_dialogSuppress = !o_acm.ErrorMsgBoxesVisible;
                }
            }
        }

        #endregion acm-properties

        #region search-acm

        public IList<string> search(string searchPattern, string searchType, bool sfixed, bool free, bool rateinitial, bool initial, bool parameters, bool algebraics, bool state, bool inactive,
        ref BackgroundWorker workerObj)
        {

             BackgroundWorker worker = workerObj;

            List<string> pathes = null;
            pathes = new List<string>();

            System.Text.StringBuilder searchModifiers = new System.Text.StringBuilder();
            if ((sfixed))
            {
                searchModifiers.Append("fixed ");
            }
            if ((free))
            {
                searchModifiers.Append("free ");
            }
            if ((rateinitial))
            {
                searchModifiers.Append("rateinitial ");
            }
            if ((initial))
            {
                searchModifiers.Append("initial ");
            }
            dynamic vars = null;
            vars = o_acm.Simulation.Flowsheet.FindMatchingVariables(searchPattern, searchModifiers.ToString(), searchType, parameters, algebraics, state, inactive);

            int varCount = 0;
            int totalVars = vars.Count;
            foreach (dynamic variab in vars)
            {
                if ((worker != null))
                {
                    varCount = varCount + 1;
                    dynamic percentage = varCount / totalVars;
                    worker.ReportProgress(Convert.ToInt32(percentage * 100));
                    //Calc precentage done

                    if ((worker.CancellationPending))
                    {
                        pathes.Clear();
                        //The user canceled the search, so just bail out with nothing
                        return pathes;
                    }
                }


                pathes.Add(variab.getPath());
            }

            return pathes;
        }

        //Simple Serach just searches and returns a list of the match paths found.  
        public IList<string> search(string searchPattern)
        {
            dynamic vars = null;
            vars = o_acm.Simulation.Flowsheet.FindMatchingVariables(searchPattern);
            List<string> pathes = null;
            pathes = new List<string>();
            foreach (dynamic variab in vars)
            {
                pathes.Add(variab.getPath());
            }
            return pathes;
        }

        #endregion search-acm

        #region inputsToSimSinter
        //I had to add ACM specific input handling to deal with some problems exclusive to dynamic inputs.  
        //It felt wrong to have special cases in sinter_Sim
        
        //Inputs all the values from the input file version 0.1 into SimSinter 
        public override void putInputsIntoSinter_1(JObject inputDict)
        {
            //If we didn't get any defaults, just return
            if (inputDict == null || inputDict.Count <= 0)
            {
                return;
            }

            // If we've loaded a version 1.0 inputs file we don't know what the units are. 
            // We have to assume they are the same as the "Current Units" in ACM, so load those up.
            //I don't think this is actually useful anymore 
            //initializeUnits();

            if (inputDict["TimeSeries"] != null)  //Do the Dynamic ACM TimeSeries first if one exists, because it sets expectations for other dynamic vars
            {
                putInputIntoSinter_1(inputDict, "TimeSeries");
                sinter_Vector ts = (sinter_Vector)setupFile.getSettingByName("TimeSeries");

                //Now update all the dynamic variables with new timeseries size
                Microsoft.VisualBasic.Collection dyvars = setupFile.DynamicVariables;
                foreach(sinter_IDynamicVariable dyvar in dyvars) {
                    dyvar.changeTimeSeriesLength(ts.size);
                }
            }

            foreach (KeyValuePair<String, JToken> entry in inputDict)
            {
                putInputIntoSinter_1(inputDict, entry.Key);
            }
        }

        //Inputs all the values from the input file version 0.2 into SimSinter 
        public override void putInputsIntoSinter_2(JToken fileDict)
        {
            JObject inputDict = (JObject)fileDict["inputs"];

            //If we didn't get any defaults, just return
            if (inputDict == null || inputDict.Count <= 0)
            {
                return;
            }

            if (inputDict["TimeSeries"] != null)  //Do the Dynamic ACM TimeSeries first if one exists, because it sets expectations for other dynamic vars
            {
                putInputIntoSinter_2(inputDict, "TimeSeries");
                sinter_Vector ts = (sinter_Vector)setupFile.getSettingByName("TimeSeries");

                //Now update all the dynamic variables with new timeseries size
                Microsoft.VisualBasic.Collection dyvars = setupFile.DynamicVariables;
                foreach (sinter_IDynamicVariable dyvar in dyvars)
                {
                    dyvar.changeTimeSeriesLength(ts.size);
                }
            }

            foreach (System.Collections.Generic.KeyValuePair<string, JToken> entry in inputDict)
            {
                putInputIntoSinter_2(inputDict, entry.Key);
            }
        }


        #endregion inputsToSimSinter

        #region acm-communication

        //This function optimizes sending variables to ACM as best it can.
        //It does this by batching up all variables and setting them at once, however
        //it can only optimize the non-homotopy case, homotopy variables still have to be
        //added individually.
        public override void sendInputsToSim()
        {

            clearHomotopy();

            //Guarantees settings will be done before any real interaction with the simulation.  
            foreach (sinter_Variable inputObj in o_setupFile.Settings)
            {
                inputObj.sendSetting(this);
            }

            //Homotopy variables must be done in the old way
            //if (homotopy)
            //{
                foreach (sinter_Variable inputObj in o_setupFile.Variables)
                {
                    inputObj.sendToSim(this);
                }
            //}
            //else  //Non-homotopy variables are batched up and sent as a set.
//            {

                //This works by building up 2 arrays of variables, all names and all values So we step through
                //each varaible and do that.  Of course that includes every array entry individually

                // Unfortunately, ACM must take all values in BaseUnits, not CurrentUnits, which means we have
                // to check and convert if necessary.  In the future this may be done with UDUNITS, for now we 
                // have to use ACM, which is kinda slow and has weird semantics.
            //    List<Object> names = new List<Object>();
            //    List<Object> values = new List<Object>();
            //    foreach (sinter_Variable inputObj in o_setupFile.Variables)
            //    {
            //        if ((inputObj.mode != sinter_Variable.sinter_IOMode.si_OUT) && (!inputObj.isSetting))
            //        {
            //            if (inputObj.isScalar)
            //            {
            //                Object t_value = (Object)inputObj.Value;

            //                string AspenBaseUnits = getBaseUnits(inputObj.addressStrings[0]);
            //                bool convertUnits = (inputObj.type == sinter_Variable.sinter_IOType.si_DOUBLE) &&
            //                    (AspenBaseUnits != "") && (AspenBaseUnits != inputObj.units);
            //                if (convertUnits)
            //                {
            //                    t_value = inputObj.unitsConversion(inputObj.units, AspenBaseUnits, (double)inputObj.Value);
            //                }
            //                //Now that we have the correct converted value, pass it into each address inside the simulation
            //                for (int addressIndex = 0; addressIndex <= (inputObj.addressStrings.Length - 1); addressIndex++)
            //                {
            //                    names.Add(inputObj.addressStrings[addressIndex]);
            //                    values.Add(t_value);
            //                }
            //            }
            //            else if (inputObj.isVec)
            //            {
            //                sinter_Vector vectorObj = (sinter_Vector)inputObj;
            //                string AspenBaseUnits = o_acmAccess.getBaseUnits(inputObj.addressStrings[0] + "(0)");  //Get the target units
            //                bool convertUnits = (vectorObj.type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC) &&  
            //                    (AspenBaseUnits != "") && (AspenBaseUnits !=  vectorObj.units);  

            //                for (int ii = 0; ii <= vectorObj.size - 1; ii++)
            //                {
            //                    Object t_value = (Object)vectorObj.getElement(ii);
            //                    if (convertUnits)
            //                    {   //TODO, this could be done as a block rather than one at a time
            //                        t_value = vectorObj.unitsConversion(vectorObj.units, AspenBaseUnits, (double)t_value);
            //                    }
            //                    for (int addressIndex = 0; addressIndex <= (vectorObj.addressStrings.Length - 1); addressIndex++)
            //                    {
            //                        names.Add(String.Format("{0}({1})", vectorObj.addressStrings[addressIndex], ii));
            //                        values.Add(t_value);
            //                    }
            //                }
            //            }
            //            else
            //            {
            //                throw new System.IO.IOException(String.Format("Variable {0} is not a scalar or a vector, what is it?", inputObj.name));
            //            }
            //        }
            //    }

            //    //Actually put the varaibles in the sim
            //    Object[] namesArray = names.ToArray();
            //    Object[] valuesArray = values.ToArray();

            //    o_acmAccess.setVariableValues(namesArray, valuesArray);
            //}
        }

        private void advanceDynamicVariables()
        {
            //Guarantees settings will be done before any real interaction with the simulation.  
            foreach (sinter_Variable inputObj in o_setupFile.DynamicVariables)
            {
                sinter_IDynamicVariable dyVar = (sinter_IDynamicVariable)inputObj;
                dyVar.advance_timestep();
            }

        }

        public override void recvOutputsFromSim()
        {
            //Turn homotopy back off before trying to get out the data.  Probably unnecessary
            clearHomotopy();

            base.recvOutputsFromSim();

            //The below would probably be faster, but doesn't work because you can't manipulate 
            //ACM Variables Sets from C#, only from VB, sigh.  So, it's saved in case that bug is ever
            //fixed, which is unlikely.

            //dynamic resolvedOutVars = o_acm.Simulation.Flowsheet.NewVariableSet();

            //foreach (sinter_Variable outputObj in o_setupFile.Variables)
            //{
            //    if (outputObj.isOutput)
            //    {
            //        if (outputObj.isVec)
            //        {
            //            sinter_Vector thisVec = (sinter_Vector) outputObj;
            //            //FindMatchingVariables("foo(*)") will get every variable in the vector foo
            //            string fullAddress = String.Format("{0}(*)", outputObj.addressStrings[0]);
            //            resolvedOutVars.addValues(o_acm.Simulation.Flowsheet.FindMatchingVariables(fullAddress));
            //        }
            //        else //Is scalar
            //        {
            //            resolvedOutVars.addValues(o_acm.Simulation.Flowsheet.FindMatchingVariables(outputObj.addressStrings[0]));
            //        }
            //    }
            //}

            ////The big call to get all the values from the sim.
            //dynamic outvars = o_acm.Simulation.Flowsheet.GetVariableValues(resolvedOutVars);
            //int outVarsCount = 0;

            //foreach( dynamic resolvedVar in resolvedOutVars) {
            //    string fullPath = resolvedVar.Name;
            //    string[] splitPath = fullPath.Split('(');
            //    if (splitPath.Count() == 1)
            //    { //Is a scalar
            //        sinter_Variable sinterVar = (sinter_Variable)o_setupFile.getIOByAddress(fullPath);
            //        Debug.Assert(sinterVar.isScalar, String.Format("variable {0} expected to be scalar, is not.", fullPath));
            //        sinterVar.setValue(outvars[outVarsCount]);
            //        ++outVarsCount;
            //    }
            //    else  //Should be a vector
            //    {
            //        Debug.Assert(splitPath.Count() == 2, "Why does variable {0} have two left parens?", fullPath);
            //        sinter_Vector sinterVar = (sinter_Vector)o_setupFile.getIOByAddress(splitPath[0]);
            //        string indexS = splitPath[1].Remove(splitPath[1].Length - 1, 1); //Hack off the trailing rparen
            //        int indexI = Convert.ToInt32(splitPath[1]);
            //        sinterVar.setElement(indexI, outvars[outVarsCount]);
            //    }

            //}
        }

        public override void sendValueToSim<ValueType>(string path, ValueType value)
        {
            if (homotopy)
            {
                try
                {
                    o_acm.Simulation.Homotopy.addTarget(path, value, "CurrentUnits");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    getLogMessages();
                    runStatus = sinter_AppError.si_SIMULATION_ERROR;
                    throw new System.IO.IOException("Could not add " + path + " as Homotopy target or value " + Convert.ToString(value) + " invalid.");
                }
            }
            else
            {
                try
                {
                    dynamic sim = o_acm.Simulation;
                    dynamic flow = sim.Flowsheet;
                    dynamic node = flow.resolve(path);
                    dynamic val = node.Value;
                    //o_acm.Simulation.Flowsheet.resolve(path).Value["CurrentUnits"] = value;
                    node.Value["CurrentUnits"] = value;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    getLogMessages();
                    runStatus = sinter_AppError.si_SIMULATION_ERROR;
                    throw new System.IO.IOException("Could not set " + path + " to " + Convert.ToString(value) + ".  This sometimes happens if " + path + " should be a homotopy target.");
                }
            }
        }


        public override void sendValueToSim<ValueType>(string path, int ii, ValueType value)
        {
            dynamic indicies = getVectorIndicies(path);
            dynamic index = indicies(ii);

            if (o_homotopy)
            {
                try
                {
                    o_acm.Simulation.Homotopy.Item(index).addTarget(path, value, "CurrentUnits");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    getLogMessages();
                    runStatus = sinter_AppError.si_SIMULATION_ERROR;
                    throw new System.IO.IOException("Could not add " + path + " as Homotopy target or value " + Convert.ToString(value) + " invalid.");
                }
            }
            else
            {
                try
                {
                    dynamic sim = o_acm.Simulation;
                    dynamic flow = sim.Flowsheet;
                    dynamic node = flow.resolve(path);
                    //o_acm.Simulation.Flowsheet.resolve(path).Value["CurrentUnits"] = value;
                    node.Item[index].Value["CurrentUnits"] = value;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    getLogMessages();
                    runStatus = sinter_AppError.si_SIMULATION_ERROR;
                    throw new System.IO.IOException("Could not set " + path + " to " + Convert.ToString(value) + ".  This sometimes happens if " + path + " should be a homotopy target.");
                }
            }
        }

        public override void sendVectorToSim<ValueType>(string path, ValueType[] value)
        {
            dynamic indicies = getVectorIndicies(path);

            if (o_homotopy)
            {
                try
                {
                    int len = value.Length;
                    for (int ii = 0; ii <= len - 1; ii++)
                    {
                        dynamic index = indicies(ii);
                        o_acm.Simulation.Homotopy.Item(index).addTarget(path, value[ii], "CurrentUnits");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    getLogMessages();
                    runStatus = sinter_AppError.si_SIMULATION_ERROR;
                    throw new System.IO.IOException("Could not add " + path + " as Homotopy target or value " + Convert.ToString(value) + " invalid.");
                }
            }
            else
            {
                try
                {
                    dynamic sim = o_acm.Simulation;
                    dynamic flow = sim.Flowsheet;
                    dynamic node = flow.resolve(path);
                    int len = value.Length;
                    for (int ii = 0; ii <= len - 1; ii++)
                    {
                        dynamic index = indicies(ii);
                        node.Item[index].Value["CurrentUnits"] = value[ii];
                        //o_acm.Simulation.Flowsheet.resolve(path).Value["CurrentUnits"] = value;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    getLogMessages();
                    runStatus = sinter_AppError.si_SIMULATION_ERROR;
                    throw new System.IO.IOException("Could not set " + path + " to " + Convert.ToString(value) + ".  This sometimes happens if " + path + " should be a homotopy target.");
                }
            }
        }

        public override Object recvValueFromSimAsObject(string path)
        {
            try
            {
                return o_acm.Simulation.Flowsheet.resolve(path).Value("CurrentUnits");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                getLogMessages();
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                throw new System.IO.IOException("Failed to get result for " + path + ". Does the variable exist?");
            }
        }

        //For vectors Takes the actual indicies in the simulation! (so, 1 for an 1-indexed array for example)
        public override Object recvValueFromSimAsObject(string path, int ii)
        {
            try
            {
                dynamic node = o_acm.Simulation.Flowsheet.resolve(path);
                return node.Item(ii).Value("CurrentUnits");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                getLogMessages();
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                throw new System.IO.IOException("Failed to get result for " + path + ". Does the variable exist?");
            }
        }



        public override ValueType recvValueFromSim<ValueType>(string path)
        {
            try
            {
                //We don't use "AsObject" because the casting gets weird.
                dynamic node = o_acm.Simulation.Flowsheet.resolve(path);
                return (ValueType)o_acm.Simulation.Flowsheet.resolve(path).Value("CurrentUnits");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                getLogMessages();
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                throw new System.IO.IOException("Failed to get result for " + path + ". Does the variable exist?");
            }
        }

        //For vectors
        public ValueType recvValueFromSim<ValueType>(string path, int ii)
        {
            dynamic indicies = getVectorIndicies(path);
            dynamic index = indicies(ii);
            try
            {
                //We don't use "AsObject" because the casting gets weird.
                dynamic node = o_acm.Simulation.Flowsheet.resolve(path);
                return (ValueType)node.Item(index).Value("CurrentUnits");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                getLogMessages();
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                throw new System.IO.IOException("Failed to get result for " + path + ". Does the variable exist?");
            }
        }


        public override void recvVectorFromSim<ValueType>(string path, int[] indicies, ValueType[] value)
        {
            // Ok, this function uses GetVariableValues to get the entire vector at once, rather than
            // requesting each variable in the vector individually, which speeds up the operation about 100x.
            // Unfortunately, GetVariableValues has the worst API I have ever seen.  So the code below is evil.
            // Basically, you create an ACM "variable set" with FindMatchingVariables.  You pass that into 
            // GetVariableValues.  GetVariableValues then RE-ORDERS the set behind the scenes, then passes you
            // back and array of values (as strings) IN THE NEW ORDER.  WHICH YOU DON'T KNOW.  IT DOESN'T GIVE
            // YOU ANY WAY TO DISCOVER THE NEW ORDERING.  By trial and error I figured out that it is sorting
            // the array paths alphabetically.  So, for a vector that becomes: foo(0), foo(1), foo(10), foo(100), 
            // foo(101), etc.  So I recreate the ordering below so I can get the correct values in the correct
            // variables.  So, if they ever decide to switch to a hash table internally, I'm hozed.
            int len = value.Length;
            string fullPath = path + "(*)";
            //path(*) gets all the vars in the vector with FindMatchingVariables
            dynamic resolvedOutVars = o_acm.Simulation.Flowsheet.FindMatchingVariables(fullPath);
            dynamic outvars = o_acm.Simulation.Flowsheet.GetVariableValues(resolvedOutVars);

            //Ok, now make the variables names, so we can sort them into the same stupid order ACM uses (actually just the array indexes, not full names)
            string[] varNames = new string[outvars.Length];
            for (int ii = 0; ii <= varNames.Count() - 1; ii++)
            {
                varNames[ii] = Convert.ToString(indicies[ii]);
            }

            Array.Sort(varNames);
            //Now put the array in alphabetical order to match ACM ordering
            for (int ii = 0; ii <= varNames.Count() - 1; ii++)
            {
                //ii is SimSinter internal array index, we need to turn that back into an ACM index
                int acmIndex = Convert.ToInt32(varNames[ii]);
                dynamic sinIndex = Array.BinarySearch(indicies, acmIndex);

                value[sinIndex] = (ValueType)Convert.ChangeType(outvars[ii], typeof(ValueType));
            }
        }

        public override IList<sinter_IVariable> getHeatIntegrationVariables()
        {
            HashSet<string> heatPaths = default(HashSet<string>);
            heatPaths = new HashSet<string>();
            foreach (dynamic block in o_acm.Simulation.Flowsheet.Blocks)
            {
                foreach (dynamic port in block.ports)
                {
                    string typename = null;
                    typename = port.TypeName;
                    if ((typename.Equals("PortMat") | typename.Equals("PortHeat") | typename.Equals("PortInfo")))
                    {
                        string portPath = null;
                        dynamic childNodes = null;
                        portPath = port.GetPath();
                        childNodes = o_acm.Simulation.Flowsheet.FindMatchingVariables(portPath + ".~");
                        foreach (dynamic node in childNodes)
                        {
                            string thisVarType = null;
                            thisVarType = node.BaseTypename;
                            if ((thisVarType.Equals("RealVariable")))
                            {
                                heatPaths.Add(node.GetPath());
                            }
                        }
                    }
                }
            }

            List<sinter_IVariable> heatVars = new List<sinter_IVariable>();
            foreach (string heatPath in heatPaths)
            {
                sinter_IVariable thisHeatVar = sinter_HelperFunctions.makeNewVariable(this, heatPath);
                // To avoid collisions we use the whole path as the name, we can get away with this cast because we know this never returns tables
                thisHeatVar.name = ((sinter_Variable)thisHeatVar).addressStrings[0];
                heatVars.Add(thisHeatVar);

                heatVars.Add(thisHeatVar);
            }
            return heatVars;

        }


        #endregion acm-communication

        #region logging 
        //This is how we know where the interesting messages start.  Unfortunately I don't know how to 
        //restrict the number of messages yet.
        private void initializeMessageLogging()
        {
            o_simulationMessages.Clear();
            o_acm.Simulation.OutputLogger.ClearWindow();
            o_startMessageNum = o_acm.Simulation.OutputLogger.MessageCount;
            //And set the message reporting level so we don't get too much just
            o_acm.Simulation.Options.PrintLevel = printLevel;
        }

        private void getLogMessages()
        {
            if (o_acm != null)
            {
                int endMessageNum = o_acm.Simulation.OutputLogger.MessageCount;
                String messagesString = o_acm.Simulation.OutputLogger.MessageText(o_startMessageNum, endMessageNum);
                String[] messagesArray = messagesString.Split(Microsoft.VisualBasic.ControlChars.Lf);
                o_simulationMessages.AddRange(messagesArray);

                o_startMessageNum = endMessageNum;
            }
        }

        //This is a bit funky, we don't get errors and warnings with ACM, we just get meesages.
        //I've decided that if the sim fails there must be an error in there, if it passes, just ignore them.
        public override string[] errorsBasic()
        {
            getLogMessages();
            return o_simulationMessages.ToArray();
        }

        public override string[] warningsBasic()
        {
            //Nothing to implement
            return new string[0];
        }


        #endregion logging

        #region runSim
        private sinter.sinter_AppError runSim_SteadyState(bool opt=false)
        {

            initializeMessageLogging();


            // this ACM module is for steady state, and steady state
            // optimization runs
            if (opt)
            {
                //steady state in optimization mode
                //the model should be set up for steaady state optimization
                //if it is setup for dynamic who knows what will happen
                o_acm.Simulation.RunMode = "Optimization";
            }
            else
            {
                //Regular steady state run
                o_acm.Simulation.RunMode = "Steady State";
            }

            if (o_homotopy && !opt) //don't think homotopy makes sense with optimization
            {
                try
                {
                    o_acm.Simulation.Homotopy.HomotopyEnabled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    getLogMessages();
                    o_runStatus = sinter.sinter_AppError.si_SIMULATION_ERROR;
                    throw new System.IO.IOException("Turning on Homotopy failed, not allowed on this simulation?");
                }
            }
            
            if (runScript(o_script) != 0)
            {
                throw new ArgumentException(string.Format("ACM {0} run failed to run script {1}.", o_acm.Simulation.RunMode, o_script));
            }

            //If the sim has already been canceled, don't run it.
            lock ((this))
            {
                if ((o_stopSim))
                {
                    o_simPaused = false;
                    o_stopSim = false;
                    o_stopTimedOut = false;
                    o_terminateMonitor.Reset();
                    runStatus = sinter_AppError.si_SIMULATION_STOPPED;
                    return runStatus;
                }
                else
                {
                    //Just make sure we can't accidentally have old flags still set.
                    o_simPaused = false;
                    o_stopTimedOut = false;
                }
            }

            try
            {
                //Run asynchronously
                o_acm.Simulation.Run(false);

                //Here we wait for an event or signal from the user.  We may get spurious signals on the Monitor,
                //So have a loop to check that.
                bool ended = false;
                ended = false;
                while ((!ended))
                {
                    lock ((this))
                    {
                        //Checking this first should allow success to win a race between the two
                        if ((o_stopSim))
                        {

                            o_stopTimer.Interval = 60000;
                            //60 seconds
                            o_stopTimer.Start();
                            o_acm.Simulation.Interrupt(false);
                            //Waiting seems to cause ACM to hang
                            //                       o_simPaused = False
                            o_stopSim = false;
                            ended = false;
                            runStatus = sinter_AppError.si_SIMULATION_STOPPED;
                            //This used to return immediately, but I thought that it better that we be sure 
                            //the interrupt has completed before proceeding, but having Interurupt(true) causes
                            //ACM (8.4 anyway) to hang. So instead this loops around again to catch the "paused" signal
                            //that should follow the Interrupt call above when it completes.
                        }
                        else if ((o_simPaused))
                        {
                            o_simPaused = false;
                            o_stopSim = false;
                            o_stopTimedOut = false;
                            ended = true;
                            o_terminateMonitor.Reset();
                            getLogMessages();
                            if ((runStatus != sinter_AppError.si_SIMULATION_STOPPED))
                            {
                                if ((o_acm.Simulation.successful == true))
                                {
                                    runStatus = sinter_AppError.si_OKAY;
                                }
                                else
                                {
                                    runStatus = sinter_AppError.si_SIMULATION_ERROR;
                                }
                            }
                            else
                            {
                                Thread.Sleep(5000);
                                //Sleep for 5 seconds to see if that helps stop reliabilibty.
                                closeDocument();
                            }
                            return runStatus;
                        }
                        else if ((o_stopTimedOut))
                        {
                            o_stopTimedOut = false;
                            //Check that signal was valid before proceeding
                            if ((runStatus == sinter_AppError.si_SIMULATION_STOPPED))
                            {
                                o_simPaused = false;
                                o_stopSim = false;
                                ended = true;
                                o_stopTimer.Stop();
                                o_terminateMonitor.Reset();
                                runStatus = sinter_AppError.si_STOP_FAILED;
                                return runStatus;
                                //Stopping failed, bail out immediately
                            }
                        }
                    }
                    o_terminateMonitor.WaitOne();
                    //Check status flags before waiting
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                getLogMessages();
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                return runStatus;
            }
            finally
            {
                o_stopTimer.Stop();
            }

            return runStatus;

        }

        private sinter.sinter_AppError runSim_Dynamic()
        {
            // this ACM module is for steady state runs
            o_acm.Simulation.RunMode = "Dynamic";
            o_acm.Simulation.Termination = "AtTime";

            //If the sim has already been canceled, don't run it.
            lock ((this))
            {
                if ((o_stopSim))
                {
                    o_simPaused = false;
                    o_stopSim = false;
                    o_stopTimedOut = false;
                    o_terminateMonitor.Reset();
                    runStatus = sinter_AppError.si_SIMULATION_STOPPED;
                    return runStatus;
                }
                else
                {
                    //Just make sure we can't accidentally have old flags still set.
                    o_simPaused = false;
                    o_stopTimedOut = false;
                }
            }
            
            if (runScript(o_script) != 0)
            {
                throw new ArgumentException(string.Format("ACM {0} run failed to run script {1}.", o_acm.Simulation.RunMode, o_script));
            }

            try
            {
                //Running dynamically is rather complex because there are multiple end states, as well as a loop state.
                //Normally it loops until the end time is reached.  So when we get a normal stop, we need to check to see if we're done, and loop otherwise.
                //The we also have the stop state, which could happen at anytime (no necessarily on a timestep)
                //Another odd thing is that we assume the user has already called sendToSim once, and will call recvFromSim after the run is complete, but
                //we have to run those before and after each timestep here.  
                bool ended = false;
                ended = false;
                int currentTimestep = 0;
                double startTime = o_acm.Simulation.Time; 
                //User should've called send to sim before

                if (startTime >= o_TimeSeries[currentTimestep])
                {
                    runStatus = sinter_AppError.si_SIMULATION_ERROR;
                    throw new ArgumentException(String.Format("Simulation is at time {0} but timeseries give end time as {1}.  End time cannot be before current time.", o_acm.Simulation.Time, o_TimeSeries[currentTimestep]));
                }
                o_acm.Simulation.EndTime = o_TimeSeries[currentTimestep];
                //Set communication time to the exact time we have to wait until the next SimSinter communication
                o_acm.Simulation.Options.TimeSettings.CommunicationInterval = o_TimeSeries[currentTimestep]-startTime;
                o_acm.Simulation.Run(false);
                while ((!ended))
                {

                  //Here we wait for an event or signal from the user.  We may get spurious signals on the Monitor,
                  //So have a loop to check that.
                    lock ((this))
                    {
                        //Checking this first should allow success to win a race between the two
                        if ((o_stopSim))
                        {

                            o_stopTimer.Interval = 60000;
                            //60 seconds
                            o_stopTimer.Start();
                            o_acm.Simulation.Interrupt(false);
                            //Waiting seems to cause ACM to hang
                            //                       o_simPaused = False
                            o_stopSim = false;
                            ended = false;
                            runStatus = sinter_AppError.si_SIMULATION_STOPPED;
                            //This used to return immediately, but I thought that it better that we be sure 
                            //the interrupt has completed before proceeding, but having Interurupt(true) causes
                            //ACM (8.4 anyway) to hang. So instead this loops around again to catch the "paused" signal
                            //that should follow the Interrupt call above when it completes.
                        }
                        else if ((o_simPaused))
                        {
                            o_simPaused = false;
                            o_stopSim = false;
                            o_stopTimedOut = false;
                            if (currentTimestep < o_TimeSeries.Length - 1)  //If we just reached the next time step, advance and run again
                            {
                                recvOutputsFromSim();
                                ++currentTimestep;
                                advanceDynamicVariables();
                                sendInputsToSim();

                                if (o_acm.Simulation.Time >= o_TimeSeries[currentTimestep])  //Error check before next run step
                                {
                                    runStatus = sinter_AppError.si_SIMULATION_ERROR;
                                    throw new ArgumentException(String.Format("Simulation is at time {0} but timeseries give end time as {1}.  End time cannot be before current time.", o_acm.Simulation.Time, o_TimeSeries[currentTimestep]));
                                }

                                
                                o_acm.Simulation.EndTime = o_TimeSeries[currentTimestep];
                                //This of course assumes that ACM stopped on the place requested by the timeseries
                                o_acm.Simulation.Options.TimeSettings.CommunicationInterval = o_TimeSeries[currentTimestep] - o_TimeSeries[currentTimestep-1];
                                o_acm.Simulation.Run(false);

                            } 
                              else  //If we're out of timesteps, shut things down normally 
                            {
                                //User should call recvFromSim after this
                                ended = true;
                                o_terminateMonitor.Reset();
                                getLogMessages();
                                if ((runStatus != sinter_AppError.si_SIMULATION_STOPPED))
                                {
                                    if ((o_acm.Simulation.successful == true))
                                    {
                                        runStatus = sinter_AppError.si_OKAY;
                                    }
                                    else
                                    {
                                        runStatus = sinter_AppError.si_SIMULATION_ERROR;
                                    }
                                }
                                else
                                {
                                    Thread.Sleep(5000);
                                    //Sleep for 5 seconds to see if that helps stop reliabilibty.
                                    closeDocument();
                                }
                                return runStatus;
                            }
                        }
                        else if ((o_stopTimedOut))
                        {
                            o_stopTimedOut = false;
                            //Check that signal was valid before proceeding
                            if ((runStatus == sinter_AppError.si_SIMULATION_STOPPED))
                            {
                                o_simPaused = false;
                                o_stopSim = false;
                                ended = true;
                                o_stopTimer.Stop();
                                o_terminateMonitor.Reset();
                                runStatus = sinter_AppError.si_STOP_FAILED;
                                return runStatus;
                                //Stopping failed, bail out immediately
                            }
                        }
                    }
                    o_terminateMonitor.WaitOne();
                    //Check status flags before waiting
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                getLogMessages();
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                return runStatus;
            }
            finally
            {
                o_stopTimer.Stop();
            }

            return runStatus;

        }


        public override sinter.sinter_AppError runSim()
        {
            if (simulatorStatus != sinter_simulatorStatus.si_OPEN)
            {
                throw new ArgumentException("Simulator is not in Open status, cannon run!");
            }
            try
            {
                simulatorStatus = sinter_simulatorStatus.si_RUNNING;

                if (runMode == "Steady State")
                {
                    return runSim_SteadyState();
                }
                else if (runMode == "Dynamic")
                {
                    return runSim_Dynamic();
                }
                else if (runMode == "Steady Optimization" || runMode == "Optimization")
                {
                    return runSim_SteadyState(opt: true);
                }
                else if (runMode == "Dynamic Optimization")
                {
                    throw new ArgumentException(string.Format("ACM.runSim has invalid runMode {0}. Currently only steady state optimization is supported.", runMode));
                }
                else
                {
                    throw new ArgumentException(string.Format("ACM.runSim has invalid runMode {0}", runMode));
                }
            } finally {
                simulatorStatus = sinter_simulatorStatus.si_OPEN;
            }

        }

        #endregion runSim

        #region acm-control

        // Attempts to load a snapshot.  Returns the time of the snapshot if it exists.  Return -1 otherwise.
        public double loadSnapshot(String snapshot_name)
        {
            //If the user defined a snapshot, try to load it up.  
            if ((!string.IsNullOrEmpty(snapshot_name)))
            {
                try
                {
                    o_acm.Simulation.Results.Refresh();
                    object snapshot = null;
                    snapshot = o_acm.Simulation.Results.FindSnapshot(snapshot_name);
                    o_acm.Simulation.Results.Rewind(snapshot);
                }
                catch
                {
                    return -1;
                }

            }
            return o_acm.Simulation.Time;
        }

        public double runScript(String script_name)
        {
            if ((!string.IsNullOrEmpty(script_name)))
            {
                try
                {
                    //o_acm.Simulation.Results.Refresh();
                    //object snapshot = null;
                    //snapshot = o_acm.Simulation.Results.FindSnapshot(snapshot_name);
                    //o_acm.Simulation.Results.Rewind(snapshot);
                    o_acm.Simulation.Flowsheet.Invoke(script_name);
                    return 0;
                }
                catch
                {
                    return 1;
                } 
            }
            return 0;
        }

        public void openDocument(string absBackupFilename)
        {
            try
            {
                o_acm.AddEventSink(this);
                o_acm.OpenDocument(absBackupFilename);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                getLogMessages();
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                if (System.IO.File.Exists(absBackupFilename))
                {
                    throw new System.IO.IOException("Open ACM Document failed.  Is " + absBackupFilename + " actually an ACM file?  Or does another process have it open? (Perhaps a crashed ACM Process?)");
                }
                else
                {
                    throw new System.IO.FileNotFoundException("Requested File " + absBackupFilename + " could not be found.");
                }
            }
        }

        private void closeDocument()
        {
            try
            {
                o_acm.CloseDocument(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                getLogMessages();
                runStatus = sinter.sinter_AppError.si_SIMULATION_ERROR;
                throw new System.IO.FileNotFoundException("Weird, closing the ACM document failed.");
            }
        }

        public override sinter.sinter_AppError resetSim()
        {
            // Reset sim by reopening the saved version
            closeDocument();
            openDocument();
            return sinter_AppError.si_OKAY;
        }

        private void openDocument()
        {
            string backupFilename = System.IO.Path.Combine(workingDir, simFile);
            string absBackupFilename = System.IO.Path.GetFullPath(backupFilename);
            try
            {
                o_acm.OpenDocument(absBackupFilename);
                o_acm.AddEventSink(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                getLogMessages();
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                if (System.IO.File.Exists(absBackupFilename))
                {
                    throw new System.IO.IOException("Open ACM Document failed.  Is " + absBackupFilename + " actually an ACM file?  Or does another process have it open? (Perhaps a crashed ACM Process?)");
                }
                else
                {
                    throw new System.IO.FileNotFoundException("Requested File " + absBackupFilename + " could not be found.");
                }
            }
        }

        public override void openSim()
        {
            if (simulatorStatus != sinter_simulatorStatus.si_CLOSED)
            {
                return; //Nothing to do.
            }
            simulatorStatus = sinter_simulatorStatus.si_INITIALIZING;

            string backupFilename = System.IO.Path.Combine(workingDir, simFile);
            string absBackupFilename = System.IO.Path.GetFullPath(backupFilename);

            if (o_acm != null)
            {
                o_acm = null;
            }

            Type appType = null;
            //First, if we have a constraint that requires us to try to launch a specific version, do that.  
            if (simVersionConstraint == sinter_versionConstraint.version_REQUIRED ||
                simVersionConstraint == sinter_versionConstraint.version_RECOMMENDED)
            {
                double simVer = Convert.ToDouble(simVersionRecommendation);
                if (simVer <= 0)
                {
                    throw new Sinter.SinterConstraintException(String.Format("Could not convert version recommendation {0} to a valid version number.", simVersionRecommendation));
                }
                appType = Type.GetTypeFromProgID(String.Format("ACM Application {0}", simVer * 100));
            } 

            if (appType == null)  //If we couldn't get a recommended version, (or there isn't a recommended version) launch anything
            {
                appType = Type.GetTypeFromProgID("ACM Application");
            }

            if (appType == null)  //Workaround for aspen installation issue that can't find ACM Application, but can find specific versions, try all versions 
            {
                int versionNum = 99;  //Current version (8.8) is 35.  So 99 should cover us well into the future.
                for (; versionNum > 0; --versionNum)
                {
                    string typename = String.Format("ACM Application {0}", versionNum * 100);  //Numbering system similar to AP one, but * 100
                    appType = Type.GetTypeFromProgID(typename);
                    if (appType != null)
                    {
                        break;
                    }
                }
                if (appType == null)
                {
                    throw new System.InvalidProgramException("Could not find Aspen Custom Modeler.  Is it installed?");
                }
            }
            try
            {
                //o_acm = new ACMWrapper.ACMWrapper();//AspenCustomModelerLibrary.AspenCustomModeler();
                o_acm = (AspenCustomModelerLibrary.IAspenModeler)System.Activator.CreateInstance(appType);
                processID = o_acm.processId;
                simVersion = o_acm.Version.Trim();
            }
            catch (Exception ex)
            {
                simulatorStatus = sinter_simulatorStatus.si_ERROR;
                System.Diagnostics.Debug.Print(ex.Message);
                getLogMessages();
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                throw new System.IO.IOException(String.Format("Could not open ACM: {0}", ex.Message), ex);
            }

            if ((o_acm == null))
            {
                simulatorStatus = sinter_simulatorStatus.si_ERROR;
                throw new System.IO.IOException("Could not open ACM, but no exception was thrown");
            }

            try
            {
                Vis = o_visible;  //Enforce visibility and dialogsuppression ASAP.
                dialogSuppress = o_dialogSuppress;
                closeDocument();  //Sometimes ACM opens up with a blank document, get rid of that if it exists
                openDocument(absBackupFilename);
            }
            finally
            {
                simulatorStatus = sinter_simulatorStatus.si_OPEN;
                checkSimVersionConstraints();
            }
        }


        public override void closeSim()
        {
            if (simulatorStatus == sinter_simulatorStatus.si_OPEN)
            {
                try
                {
                    //This used to have this retry section, but that caused ACM to hang if events are enabled.
                    //           i = 1
                    //            While (i <= 15)
                    //  close aspen until its gone or give up after 15 trys
                    //  i think when somthing kills excel it leaves the aspen
                    //  object with extra in a refrence counter but I may be wrong.
                    o_acm.Quit();
                    //           i += 1
                    //           End While
                }
                catch (Exception ex)
                {
                    simulatorStatus = sinter_simulatorStatus.si_ERROR; //No idea if it's open or not
                    o_acm = null;
                    System.Diagnostics.Debug.Print(ex.Message);
                }
            }
        }

        public override void stopSim()
        {
            lock ((this))
            {
                if (simulatorStatus == sinter_simulatorStatus.si_INITIALIZING)
                {
                    runStatus = sinter_AppError.si_STOP_FAILED;
                }
                else
                {
                    o_stopSim = true;
                    o_terminateMonitor.Set();
                    simulatorStatus = sinter_simulatorStatus.si_OPEN;
                }
            }
        }

        public override bool terminate()
        {
            Debug.WriteLine(String.Format("Terminate ProcessId '{0}'", processID), GetType().Name);
            lock (this)
            {
                if (processID > 0)
                {
                    Process p = Process.GetProcessById(processID);
                    if (p != null) p.Kill();
                    simulatorStatus = sinter_simulatorStatus.si_CLOSED;
                    return true;
                }
                else
                {
                    simulatorStatus = sinter_simulatorStatus.si_CLOSED;
                    return false;
                }
            }
        }


        #endregion acm-control

        #region event-handlers

        private void IAspenModelerEvents_OnRunPaused()
        {
            lock ((this))
            {
                o_simPaused = true;
                o_terminateMonitor.Set();
            }
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnRunPaused()
        {
            IAspenModelerEvents_OnRunPaused();
        }


        private void IAspenModelerEvents_OnDeletedBlock(object sBlockName)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnDeletedBlock(object sBlockName)
        {
            IAspenModelerEvents_OnDeletedBlock(sBlockName);
        }

        private void IAspenModelerEvents_OnDeletedStream(object sStreamName)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnDeletedStream(object sStreamName)
        {
            IAspenModelerEvents_OnDeletedStream(sStreamName);
        }


        private void IAspenModelerEvents_OnHasQuit()
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnHasQuit()
        {
            IAspenModelerEvents_OnHasQuit();
        }


        private void IAspenModelerEvents_OnHasSaved()
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnHasSaved()
        {
            IAspenModelerEvents_OnHasSaved();
        }


        private void IAspenModelerEvents_OnNew()
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnNew()
        {
            IAspenModelerEvents_OnNew();
        }


        private void IAspenModelerEvents_OnNewBlock(object sBlockName)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnNewBlock(object sBlockName)
        {
            IAspenModelerEvents_OnNewBlock(sBlockName);
        }


        private void IAspenModelerEvents_OnNewStream(object sStreamName)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnNewStream(object sStreamName)
        {
            IAspenModelerEvents_OnNewStream(sStreamName);
        }


        private void IAspenModelerEvents_OnOpened(object sPath)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnOpened(object sPath)
        {
            IAspenModelerEvents_OnOpened(sPath);
        }


        private void IAspenModelerEvents_OnRewindorCopyValues()
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnRewindorCopyValues()
        {
            IAspenModelerEvents_OnRewindorCopyValues();
        }


        private void IAspenModelerEvents_OnRunModeChanged(object sRunMode)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnRunModeChanged(object sRunMode)
        {
            IAspenModelerEvents_OnRunModeChanged(sRunMode);
        }



        private void IAspenModelerEvents_OnRunStarted()
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnRunStarted()
        {
            IAspenModelerEvents_OnRunStarted();
        }


        private void IAspenModelerEvents_OnSavedAs(object sPath)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnSavedAs(object sPath)
        {
            IAspenModelerEvents_OnSavedAs(sPath);
        }


        private void IAspenModelerEvents_OnStepComplete()
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnStepComplete()
        {
            IAspenModelerEvents_OnStepComplete();
        }


        private void IAspenModelerEvents_OnStreamConnected(object sStreamName)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnStreamConnected(object sStreamName)
        {
            IAspenModelerEvents_OnStreamConnected(sStreamName);
        }


        private void IAspenModelerEvents_OnStreamDisconnected(object sStreamName)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnStreamDisconnected(object sStreamName)
        {
            IAspenModelerEvents_OnStreamDisconnected(sStreamName);
        }


        private void IAspenModelerEvents_OnUomSetChanged(object sUomSetName)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnUomSetChanged(object sUomSetName)
        {
            IAspenModelerEvents_OnUomSetChanged(sUomSetName);
        }


        private void IAspenModelerEvents_OnUserChangedVariable(object sVariableName, object sAttributeName)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnUserChangedVariable(object sVariableName, object sAttributeName)
        {
            IAspenModelerEvents_OnUserChangedVariable(sVariableName, sAttributeName);
        }


        private void IAspenModelerEvents_OnUserEvent(object sUserString)
        {
        }
        void AspenCustomModelerLibrary.IAspenModelerEvents.OnUserEvent(object sUserString)
        {
            IAspenModelerEvents_OnUserEvent(sUserString);
        }
        #endregion event-handlers

        #region versioning
        /** Returns the user known version name when passed in the internal version name.
         * For example, For execl 14.0 returns "2010"
         * Simulator specific, of course.
         * If the version number cannot be converted, the empty string is returned.
         **/
        public override string internal2externalVersion(string internalVersion)
        {
            if (internalVersion == "34.0")
            {
                return "8.8";
            }
            if (internalVersion == "32.0")
            {
                return "8.6";
            }
            if (internalVersion == "30.0")
            {
                return "8.4";
            }
            if (internalVersion == "28.0")
            {
                return "8.2";
            }
            if (internalVersion == "27.0")
            {
                return "8.0";
            }
            if (internalVersion == "26.0")
            {
                return "7.3.2";
            }

            return internalVersion;
        }

        /** Returns the internal version number, when given a user known version string.
         * For example, For execl, the string "2010" will return 14.0
         * Simulator specific, of course.
         * If the version number cannot be converted, the passed in name is returned, in hopes that it is actually an internal version number gotten from the simulator
         **/
        public override string external2internalVersion(string externalVersion)
        {
            if (externalVersion == "8.8")
            {
                return "34.0";
            }
            if (externalVersion == "8.6")
            {
                return "32.0";
            }
            if (externalVersion == "8.4")
            {
                return "30.0";
            }
            if (externalVersion == "8.2")
            {
                return "28.0";
            }
            if (externalVersion == "8.0")
            {
                return "27.0";
            }
            if (externalVersion == "7.3.2")
            {
                return "26.0";
            }
            return externalVersion;

        }

        /** Returns a full list of all the external version names known at time of writing.
         * If the found version is greater than these, then it will be referred to using the internal version
         * name in the UI.
         * SimSinter doesn't support versions earlier than 7.3.2 **/
        public override string[] externalVersionList()
        {
            string[] versions = { "7.3.2", "8.0", "8.2", "8.4", "8.6", "8.8" };
            return versions;
        }

        #endregion versioning
    }
}

