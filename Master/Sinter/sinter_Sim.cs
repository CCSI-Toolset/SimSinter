using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using sinter;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace sinter
{

    public abstract class sinter_Sim : ISimulation
    {
        #region data


        protected int processID = -1;

        protected string o_simName;    //Name of the current simulator
        protected string o_simVersion; //version of the current simulator

        //simulation variables
        //The setup file holds all the info we parse from it.
        protected sinter_SetupFile o_setupFile;

        //working directory for the simulation
        private string o_wrkDir;

        protected sinter_AppError o_runStatus = sinter_AppError.si_SIMULATION_NOT_RUN;
        protected sinter_simulatorStatus o_simulatorStatus = sinter_simulatorStatus.si_CLOSED;
        
        // run name is optionally used for identifying output files
        private string o_runName;

        //These are the settings availible for this simulation type.  This is set by the subclasses.
        protected Dictionary<string, Tuple<set_setting, get_setting, sinter_Variable.sinter_IOType>> o_availibleSettings;

        #endregion data

        #region constructor
        public sinter_Sim()
            : base()
        {
        }
        #endregion constructor

        #region pathing
        public abstract char pathSeperator { get; }

        //ACM and Excel have to do this differently
        public virtual IList<string> parsePath(string path)
        {
            while (path.Length > 0 && path[0] == pathSeperator)
            {
                path = path.Substring(1);
            }

            if (path.Length == 0)
            {
                return new List<String>();
            }

            return path.Split(pathSeperator).ToList();
        }

        //ACM and Excel have to do this differently
        public virtual string combinePath(IList<string> splitPath)
        {
            string path = splitPath[0];
            for (int ii = 1; ii < splitPath.Count; ++ii)
            {
                path += pathSeperator + splitPath[ii];
            }
            return path;
        }

        //A special case of combine path that allows on to use only the first <lastIndex> of the path
        public virtual String combinePath(IList<string> splitPath, int lastIndex) {
            string path = splitPath[0];
            for (int ii = 1; ii <= lastIndex; ++ii)
            {
                path += pathSeperator + splitPath[ii];
            }
            return path;
        }

        /** 
         * Adds a vector index enclose in parens to the end of the path, like:
         * foo.bar.path -> foo.bar.path(3)
         **/
        public string addVecIndex(string subpath, int index)
        {
            return string.Format("{0}({1})", subpath, index);
        }

        /**
         * Knocks the Array indicies off the end of the path, if they exist.  
         **/
        public string PathSansVectorIndex(string path)
        {
            int lastnameStart = path.LastIndexOf(pathSeperator);
            int lastnameLen = path.Length - lastnameStart;
            int lastLParen = path.LastIndexOf("(", path.Length, lastnameLen);
            if (lastLParen == -1)
            {
                return path;
            }

            int lastRParen = path.LastIndexOf(")", path.Length, lastnameLen);
            string noVecIndexSubstring = path.Substring(0, lastLParen);
            return noVecIndexSubstring;
        }


        /**
         * Parses out the array index from inside encolsing parens if there is one.
         * If there is no array index, it return '-1'
         **/
        public int ParseVectorIndex(string path)
        {
            int lastnameStart = path.LastIndexOf(pathSeperator);
            int lastnameLen = path.Length - lastnameStart;
            int lastLParen = path.LastIndexOf("(", path.Length, lastnameLen);
            if (lastLParen == -1)
            {
                return -1;
            }

            int lastRParen = path.LastIndexOf(")", path.Length, lastnameLen);
            int substringLen = lastRParen - (lastLParen + 1);
            string vecIndexSubstring = path.Substring(lastLParen + 1, substringLen);
            int nodeVecIndex = -1;
            if (Int32.TryParse(vecIndexSubstring, out nodeVecIndex))
            {
                return nodeVecIndex;
            }
            else
            {
                return -1;
            }
        }

        #endregion pathing

        #region settings
        protected delegate void set_setting(object obj);
        protected delegate object get_setting();

        public void setSetting(string name, object value)
        {

            set_setting thisSetting = o_availibleSettings[name].Item1;
            thisSetting(value);
        }

        /* Get a list of all availible settings for this simulation as sinter_Variables */
        public virtual IList<sinter_Variable> getSettings()
        {
            List<sinter_Variable> settings = new List<sinter_Variable>();
            foreach (KeyValuePair<string, Tuple<set_setting, get_setting, sinter_Variable.sinter_IOType>> entry in o_availibleSettings)
            {
                sinter_Variable thisSetting = sinter_Factory.createVariable(entry.Value.Item3);
                string[] addressString = { "setting(" + entry.Key + ")" };
                thisSetting.init(entry.Key, sinter_Variable.sinter_IOMode.si_IN, entry.Value.Item3, "Simulation specific setting: " + entry.Key, addressString);
                thisSetting.Value = entry.Value.Item2();
                thisSetting.dfault = thisSetting.Value;
                settings.Add(thisSetting);
            }
            return settings;
        }

        #endregion settings

        #region defaults
        /**
        *  This function gets the orginal values for all the input variables from 
        *  the simulation.  After this call, value and defaults for each input variable
        *  will be the same, whatever was in the simulation.
        **/
        public virtual void initializeDefaults()
        {
            foreach (sinter_Variable inputObj in o_setupFile.Variables)
            {
                if (inputObj.isInput)
                {
                    inputObj.recvFromSim(this);
                    inputObj.setDefaultToValue();
                }
            }
        }

        /**
         * This function sets all the values back to their defaults.  I needed it at one time for something, but what? 
         **/
        public void restoreDefaults()
        {
            for (int i = 1; i <= countIO; i++)
            {
                if (getIOByIndex(i).isInput)
                    getIOByIndex(i).resetToDefault();
            }
        }

        public void sendDefaults(JObject inputDict)
        {
            //If we didn't get any defaults, just return
            if (inputDict == null || inputDict.Count <= 0)
            {
                return;
            }

            checkAllInputs(inputDict);

            foreach (KeyValuePair<String, JToken> entry in inputDict)
            {
                int row = -1;
                int column = -1;
                string varName = sinter_SetupFile.parseVariable(entry.Key, ref row, ref column);

                sinter_IVariable inputVal = this.getIOByName(varName);
                if (inputVal == null)
                {
                    Console.WriteLine("Ignoring non-var: " + varName);
                }
                else if (inputVal.isOutput)
                {
                    throw new System.IO.IOException(string.Format("Variable {0} is an output varaible.  It should not be in the input list.", varName));
                }
                else
                {
                    if (inputVal.isScalar)
                    {
                        ((sinter_Variable)inputVal).dfault = sinter_HelperFunctions.convertJTokenToNative(entry.Value);
                    }
                    else if (inputVal.isVec)
                    {
                        //Check that the indexes are valid
                        sinter_Vector thisVar = (sinter_Vector)inputVal;
                        if (row >= 0)   //Editing a single entry of a vector
                        {
                            thisVar.setElementDefault(row, sinter_HelperFunctions.convertJTokenToNative(entry.Value));
                        }
                        else
                        { //copying in an entire vector at once

                            Newtonsoft.Json.Linq.JArray jsonArray = (Newtonsoft.Json.Linq.JArray)entry.Value;
                            if (thisVar.size != jsonArray.Count)
                            {
                                throw new System.IO.IOException(string.Format("Variable {0} has an index out of range.  Upper bound: {1}, bad index: {2}", varName, (int)thisVar.size - 1, row));
                            }
                            //Annoyingly, a jsonArray is not a C# Array, so I have to copy element by element, rather than just copying the array
                            for (int ii = 0; ii < thisVar.size; ++ii)
                            {
                                thisVar.setElementDefault(ii, sinter_HelperFunctions.convertJTokenToNative(jsonArray[ii]));
                            }
                        }
                    }

                    else if (inputVal.isTable)
                    {
                        sinter_Table thisTable = (sinter_Table)inputVal;
                        //Check that the indexes are valid
                        if (row >= 0 && row <= (int)thisTable.MNRows &&
                            column >= 0 && column <= (int)thisTable.MNCols)
                        {
                            thisTable.setElementDefault(row, column, sinter_HelperFunctions.convertJTokenToNative(entry.Value));
                        }
                        else
                        {
                            throw new System.IO.IOException(string.Format("Variable {0} has an index out of range. Range: {1},{2} Index: {3},{4}", varName, (int)thisTable.MNRows, (int)thisTable.MNCols, row, column));
                        }
                    }
                }

                //Now, make the default the current real value
                inputVal.resetToDefault();
            }

        }


        #endregion defaults

        #region sim-metadata

        public sinter_SetupFile setupFile
        {
            get
            {
                return o_setupFile;
            }
            set
            {
                o_setupFile = value;
            }
        }

        public string workingDir
        {
            get { return o_wrkDir; }
            set { o_wrkDir = value.Replace("/", "\\"); }
        }
        public string simFile
        {
            get { return o_setupFile.aspenFilename; }
            set { o_setupFile.aspenFilename = value; }
        }
        public string simFileHash
        {
            get { return o_setupFile.aspenFileHash; }
            set { o_setupFile.aspenFileHash = value; }
        }
        public string simFileHashAlgo
        {
            get { return o_setupFile.aspenFileHashAlgo; }
            set { o_setupFile.aspenFileHashAlgo = value; }
        }

        public List<string> additionalFiles
        {
            get { return o_setupFile.additionalFiles; }
            set { o_setupFile.additionalFiles = value; }
        }

        public List<string> additionalFilesHash
        {
            get { return o_setupFile.additionalFilesHash; }
            set { o_setupFile.additionalFilesHash = value; }
        }
        public List<string> additionalFilesHashAlgo
        {
            get { return o_setupFile.additionalFilesHashAlgo; }
            set { o_setupFile.additionalFilesHashAlgo = value; }
        }

        public Version configFileVersion
        {
            get { return o_setupFile.configFileVersion; }
            set { o_setupFile.configFileVersion = value; }
        }

        public string title
        {
            get { return o_setupFile.title; }
            set { o_setupFile.title = value; }
        }
        public string runName
        {
            // run name is optional used for identifying output files
            get { return o_runName; }
            set { o_runName = value; }
        }
        public string author
        {
            get { return o_setupFile.author; }
            set { o_setupFile.author = value; }
        }
        public string dateString
        {
            get { return o_setupFile.dateString; }
            set { o_setupFile.dateString = value; }
        }
        public string description
        {
            get { return o_setupFile.simulationDescription; }
            set { o_setupFile.simulationDescription = value; }
        }
        public virtual sinter_AppError runStatus
        {
            get { return o_runStatus; }
            set { o_runStatus = value; }
        }

        public virtual sinter_simulatorStatus simulatorStatus
        {
            get { return o_simulatorStatus; }
            set { o_simulatorStatus = value; }
        }

        public abstract string[] errorsBasic();
        public abstract string[] warningsBasic();

        public int ProcessID
        {
            get { return processID; }
        }

        public virtual bool IsInitializing
        {
            get { return o_simulatorStatus == sinter_simulatorStatus.si_INITIALIZING; }
        }

        //A string giving the type of simulation file in this setupfile.  This is used in the jsonconfig
        //as the key for the simulation file.  Could be aspenfile or spreadsheet
        public abstract string setupfileKey { get; }

        public int countIO
        {
            get { return o_setupFile.countIO; }
        }

        public string simName
        {
            get { return o_simName; }
            protected set { o_simName = value; }
        }
        public string simVersion
        {
            get { return o_simVersion; }
            protected set { 
                o_simVersion = value;
                if (simVersionRecommendation == "")  //If there is no setting for the recommendation, the current version should be used.
                {
                    simVersionRecommendation = o_simVersion;
                }
            }
        }

        //The setup file can require a particular version.  This is that version number.
        public string simVersionRecommendation
        {
            get { return setupFile.simVersionRecommendation; }
            set { setupFile.simVersionRecommendation = value; }
        }

        public sinter_versionConstraint simVersionConstraint
        {
            get { return setupFile.simVersionConstraint; }
            set { setupFile.simVersionConstraint = value; }
        }

        public static string constraintToName(sinter_versionConstraint constraint)
        {
            if (constraint == sinter_versionConstraint.version_ANY)
            {
                return "ANY";
            }
            if (constraint == sinter_versionConstraint.version_ATLEAST)
            {
                return "AT-LEAST";
            }
            if (constraint == sinter_versionConstraint.version_REQUIRED)
            {
                return "REQUIRED";
            }
            if (constraint == sinter_versionConstraint.version_RECOMMENDED)
            {
                return "RECOMMENDED";
            }

            throw new ArgumentException(String.Format("Unknown constraint {0}", constraint));
        }

        public static sinter_versionConstraint nameToConstraint(string constraintName)
        {
            if (String.Equals("ANY", constraintName, StringComparison.OrdinalIgnoreCase))
            {
                return sinter_versionConstraint.version_ANY;
            }
            if (String.Equals("AT-LEAST", constraintName, StringComparison.OrdinalIgnoreCase))
            {
                return sinter_versionConstraint.version_ATLEAST;
            }
            if (String.Equals("REQUIRED", constraintName, StringComparison.OrdinalIgnoreCase))
            {
                return sinter_versionConstraint.version_REQUIRED;
            }
            if (String.Equals("RECOMMENDED", constraintName, StringComparison.OrdinalIgnoreCase))
            {
                return sinter_versionConstraint.version_RECOMMENDED;
            }

            throw new ArgumentException(String.Format("Unknown constraint {0}", constraintName));

        }

        /** Returns the user known version name when passed in the internal version name.
         * For example, For execl 14.0 returns "2010"
         * Simulator specific, of course.
         * If the version number cannot be converted, the empty string is returned.
         **/
        public virtual string internal2externalVersion(string internalVersion)
        {
            return "";  
        }

        /** Returns the internal version number, when given a user known version string.
         * For example, For execl, the string "2010" will return 14.0
         * Simulator specific, of course.
         * If the version number cannot be converted, -1 is returned.
         **/
        public virtual string external2internalVersion(string externalVersion)
        {
            throw new NotImplementedException("This simulation does not seem to have any external to internal version number conversions defined.");
        }

        /** Returns a full list of all the external version names **/
        public abstract string[] externalVersionList();

        /** FOR internal versions only! 
         * Returns true if v1 is less than v2, false otherwise 
         * This works on standard version strings with only numbers, and up to 3 dots.  (ie 14.3.22.5144) 
         * If the simulator uses a different style it will have to implement it's on comparitor. **/
        public virtual bool versionLessThan(string v1, string v2)
        {
            var version1 = new Version(v1);
            var version2 = new Version(v2);
            return version1 < version2;
        }


        #endregion sim-metadata
     
        #region get-variables
        public bool isScalarByIndex(int i)
        {
            return o_setupFile.getIOByIndex(i).isScalar;
        }

        public bool isVectorByIndex(int i)
        {
            return o_setupFile.getIOByIndex(i).isVec;
        }

        public sinter_IVariable getIOByName(string name)
        {
            return o_setupFile.getIOByName(name);
        }
        public sinter_IVariable getIOByIndex(int i)
        {
            return o_setupFile.getIOByIndex(i);
        }

        public sinter_Variable getVariableByName(string name)
        {
            return (sinter_Variable)o_setupFile.getVariableByName(name);
        }

        public sinter_Variable getVariableByIndex(int i)
        {
            return (sinter_Variable)o_setupFile.getIOByIndex(i);
        }

        public sinter_Vector getVectorByName(string name)
        {
            return (sinter_Vector)o_setupFile.getVariableByName(name);
        }

        public sinter_Variable getVectorByIndex(int i)
        {
            return (sinter_Vector)o_setupFile.getIOByIndex(i);
        }


        public sinter_Variable getSettingByName(string name)
        {
            return (sinter_Variable)o_setupFile.getSettingByName(name);
        }
        public sinter_Variable getSettingByIndex(int i)
        {
            return (sinter_Variable)o_setupFile.getSettingByIndex(i);
        }

        //This feature seems to have fallen out of favor
        public abstract IList<sinter_IVariable> getHeatIntegrationVariables();

        #endregion get-variables

        #region sim-control

        public abstract void openSim();
        public abstract void closeSim();
        public abstract sinter_AppError runSim();
        public abstract sinter_AppError resetSim();
        public abstract bool terminate();
        public abstract void stopSim();

        public abstract bool dialogSuppress
        {
            get;
            set;
        }

        public abstract bool Vis
        {
            get;
            set;
        }

        #endregion sim-control

        #region variableTree

        protected VariableTree.VariableTree o_dataTree = null;

        public VariableTree.VariableTree dataTree
        {
            get
            {
                return o_dataTree;
            }
        }

        /** 
         * void makeDataTree
         * 
         * This function generates a tree based on the variables availible in the simulation.  All input and
         * output variables.  This is used primarily for the Sinter Config GUI.
         */
        public abstract void makeDataTree();
        public abstract void startDataTree();
        public abstract VariableTree.VariableTreeNode findDataTreeNode(IList<String> pathArray);

        #endregion variableTree

        #region IOTree

        private struct IOTreeNode
        {
            public string name;
            public int parent;
            public int level;
            public List<int> children;
            public int IOListIndex;
        }

        private List<IOTreeNode> o_IOTree = new List<IOTreeNode>();


        /** 
         * void makeIOTree
         * 
         * This function generates a tree based on the dot-seperated names of the sinter_IVariables 
         * So this scrictly covers the variables sinter knows about.  The VariableTree covers the variables
         * the simulation knows about.
         */
        public void makeIOTree()
        {
            int i = 0;
            int j = 0;
            int k = 0;
            string[] path = null;
            IOTreeNode node = new IOTreeNode();
            int found = 0;
            int old_found = 0;
            int parent = 0;

            node.name = "root";
            node.level = 0;
            node.parent = 0;
            node.IOListIndex = 0;
            node.children = new List<int>();

            o_IOTree.Clear();
            o_IOTree.Add(node);

            for (i = 1; i <= o_setupFile.Variables.Count; i++)
            {
                parent = 0;
                found = -1;
                path = Strings.Split(((sinter_Variable)o_setupFile.Variables[i]).name, ".");
                for (j = 0; j <= path.Length - 2; j++)
                {
                    old_found = found;
                    for (k = 0; k <= o_IOTree[parent].children.Count - 1; k++)
                    {
                        if (path[j] == o_IOTree[o_IOTree[parent].children[k]].name)
                        {
                            found = j;
                            parent = o_IOTree[parent].children[k];
                            break; // TODO: might not be correct. Was : Exit For
                        }
                    }
                    if (found == old_found)
                        break; // TODO: might not be correct. Was : Exit For
                }
                for (j = found + 1; j <= path.Length - 2; j++)
                {
                    parent = addNode(parent, path[j], 0);
                }
                addNode(parent, path[path.Length - 1], i);
            }
        }

        private int addNode(int parent, string name, int ioindex)
        {
            int n = 0;
            IOTreeNode node = new IOTreeNode();
            n = o_IOTree.Count;
            node.children = new List<int>();
            node.parent = parent;
            node.name = name;
            node.IOListIndex = ioindex;
            node.level = o_IOTree[parent].level + 1;
            o_IOTree.Add(node);
            o_IOTree[parent].children.Add(n);
            //node = Nothing
            return n;
        }

        public string treeNodeName(int i)
        {
            return o_IOTree[i].name;
        }

        public int treeNodeParent(int i)
        {
            return o_IOTree[i].parent;
        }

        public int treeNodeLevel(int i)
        {
            return o_IOTree[i].level;
        }

        public int treeNodeIOIndex(int i)
        {
            return o_IOTree[i].IOListIndex;
        }

        public int treeNodeChildrenCount(int i)
        {
            return o_IOTree[i].children.Count;
        }

        public int treeNodeChild(int i, int j)
        {
            return o_IOTree[i].children[j];
        }

        #endregion IOTree

        #region error-checking
        /**
         * void checkSimVersionConstraints()
         * 
         * throws an exception if the simulation version is not valie for the constraints.
         **/
        public void checkSimVersionConstraints()
        {
            if (setupFile.simNameRecommendation != null && setupFile.simNameRecommendation != "" && simName != setupFile.simNameRecommendation)
            {
                throw new ArgumentException(String.Format("The simulator ({0}) does not match the simulator recommended by the configuration file ({1}).", simName, setupFile.simNameRecommendation));
            }

            if (simVersionConstraint == sinter_versionConstraint.version_REQUIRED)
            {
                if (simVersion != simVersionRecommendation)
                {
                    throw new Sinter.SinterConstraintException(String.Format("The simulator version ({0}) does not match the version required by the Sinter Configuration File ({1}).",
                        internal2externalVersion(simVersion), internal2externalVersion(simVersionRecommendation)));
                }
            }
            if (simVersionConstraint == sinter_versionConstraint.version_ATLEAST || simVersionConstraint == sinter_versionConstraint.version_RECOMMENDED)
            {
                if (versionLessThan(simVersion, simVersionRecommendation))
                {
                    throw new Sinter.SinterConstraintException(String.Format("The Sinter Configuration File requires simulator version of at least ({0}).  This simulator ({1}) is  too insufficent",
                        internal2externalVersion(simVersionRecommendation), internal2externalVersion(simVersion)));
                }
            }
            //If simVersion Constraint is ANY don't bother to check anything

        }


        /**
         * bool checkAllInputs(JObject inputDict)
         * 
         *  Checks to make sure every input in the sinter configuration is represented in the input dictionary
         *  Mostly useful with sinter config file format 0.1, where there were seperate defaults and input files,
         *  neither of which was guarnateed to have all the variables.
         **/
        public bool checkAllInputs(JObject inputDict)
        {
            //If we didn't get any defaults, just return
            if (inputDict == null)
            {
                return false;
            }

            for (int ii = 1; ii <= this.countIO; ++ii)
            {
                sinter_IVariable inputVal = this.getIOByIndex(ii);
                if (inputVal.isInput)
                {
                    if (inputVal.isTable)
                    {
                        sinter_Table thisInput = (sinter_Table)inputVal;
                        string varName = inputVal.name;
                        int rowMax = (int)thisInput.MNRows;
                        int colMax = (int)thisInput.MNCols;
                        for (int row = 0; row <= rowMax; ++row)
                        {
                            for (int col = 0; col <= colMax; ++col)
                            {
                                string indexedVarName = string.Format("{0}[{1},{2}]", varName, row, col);
                                if (inputDict[indexedVarName] == null)
                                {
                                    throw new System.IO.IOException(string.Format("Required Input varaible {0} is not represented in the input dictionary!", indexedVarName));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (inputDict[inputVal.name] == null)
                        {
                            throw new System.IO.IOException(string.Format("Required Input varaible {0} is not represented in the input dictionary!", inputVal.name));
                        }
                    }
                }
            }
            return true;
        }

        #endregion error-checking

        #region inputs
        public abstract void sendInputsToSim();

        public void sendInputs(JObject inputDict)
        {
            //If we didn't get any defaults, just return
            if (inputDict == null || inputDict.Count <= 0)
            {
                return;
            }

            //Input file version 0.1 doesn't declare it's version, all subsequent versions should.  Hence, if there's no version
            //We'll assume it's version 0.1
            if ((inputDict["filetype"] == null) || (inputDict["version"] == null))
            {
                putInputsIntoSinter_1(inputDict);
                return;
            }

            string filetype = (string)inputDict["filetype"];
            double version = (double)inputDict["version"];

            if (filetype.Equals("sinterinputs"))
            {
                if (version == 0.2)
                {
                    putInputsIntoSinter_2(inputDict);
                }
                else
                {
                    throw new System.IO.IOException(String.Format("Got unknown sinterinputs version {0}, expected 0.2.", version));
                }
            }
            else
            {
                throw new System.IO.IOException(String.Format("Got unknown filetype {0}, expected sinterinputs.", filetype));
            }

        }

        //Puts a single input value from a Version 0.1 input file into SimSinter.
        public virtual void putInputIntoSinter_1(JObject inputDict, String name)
        {
            JToken value = inputDict[name];
            int row = -1;
            int column = -1;
            string varName = sinter_SetupFile.parseVariable(name, ref row, ref column);

            sinter_IVariable inputVal = this.getIOByName(varName);
            if (inputVal == null)
            {
                runStatus = sinter_AppError.si_INPUT_ERROR;
                throw new System.IO.IOException(string.Format("Input Variable {0} is unknown.  Please check that you have the correct inputs and sinter config for this simulation.", varName));
            }

            inputVal.setInput(varName, row, column, value, "");

        }

        //Inputs all the values from the input file version 0.1 into SimSinter 
        public virtual void putInputsIntoSinter_1(JObject inputDict)
        {
            //If we didn't get any defaults, just return
            if (inputDict == null || inputDict.Count <= 0)
            {
                return;
            }

            // If we've loaded a version 1.0 inputs file we don't know what the units are. 
            // We have to assume they are the same as the "Current Units" in ACM, so load those up.

            foreach (KeyValuePair<String, JToken> entry in inputDict)
            {
                putInputIntoSinter_1(inputDict, entry.Key);
            }
        }

        //Puts a single input value from a Version 0.2 input file into SimSinter.
        public virtual void putInputIntoSinter_2(JObject inputDict, String name)
        {
            int row = -1;
            int column = -1;
            string varName = sinter_SetupFile.parseVariable(name, ref row, ref column);
            JObject varData = (JObject)inputDict[name];
            string units = (string)varData["units"];
            if (units == null)
            {
                units = "";  //We prefer the empty string to a null value
            }

            sinter_IVariable inputVal = this.getIOByName(varName);
            if (inputVal == null)
            {
                runStatus = sinter_AppError.si_INPUT_ERROR;
                throw new System.IO.IOException(string.Format("Input Variable {0} is unknown.  Please check that you have the correct inputs and sinter config for this simulation.", varName));
            }

            inputVal.setInput(varName, row, column, varData["value"], units);

        }

        //Inputs all the values from the input file version 0.2 into SimSinter 
        public virtual void putInputsIntoSinter_2(JToken fileDict)
        {
            JObject inputDict = (JObject)fileDict["inputs"];

            //If we didn't get any defaults, just return
            if (inputDict == null || inputDict.Count <= 0)
            {
                return;
            }

            foreach (System.Collections.Generic.KeyValuePair<string, JToken> entry in inputDict)
            {
                putInputIntoSinter_2(inputDict, entry.Key);
            }
        }

        #endregion inputs

        #region outputs

        public virtual void recvOutputsFromSim()
        {
            foreach (sinter_Variable outputObj in o_setupFile.Variables)
            {
                if (outputObj.isOutput)
                {

                    outputObj.recvFromSim(this);
                }
            }
        }

        //old fashioned output, deprecated.
        public JObject getOutputsToJSON_1()
        {
            JObject outputDict = new JObject();
            for (int ii = 1; ii <= this.countIO; ++ii)
            {
                sinter_IVariable outputVal = this.getIOByIndex(ii);
                string varname = outputVal.name;
                if (outputVal.isTable)
                {
                    sinter_Table outputTable = (sinter_Table)outputVal;
                    int nrows = (int)outputTable.MNRows;
                    int ncols = (int)outputTable.MNCols;
                    for (int irow = 0; irow <= nrows; ++irow)
                    {
                        for (int icol = 0; icol <= ncols; ++icol)
                        {
                            outputDict.Add(varname + "[" + irow + "," + icol + "]", sinter_HelperFunctions.convertSinterValueToJToken(outputTable.getVariable(irow, icol)));
                        }
                    }
                }
                else if (outputVal.isVec)
                {
                    sinter_Variable outputVar = (sinter_Variable)outputVal;
                    Newtonsoft.Json.Linq.JArray jsonArray = new Newtonsoft.Json.Linq.JArray(outputVar.Value);
                    outputDict.Add(varname, jsonArray);
                }
                else
                {
                    sinter_Variable outputVar = (sinter_Variable)outputVal;
                    outputDict.Add(varname, sinter_HelperFunctions.convertSinterValueToJToken(outputVar));
                }
            }

            //Special case, Always put the run Status put the status in the outputs as "status".
            outputDict.Add("status", (int)runStatus);

            return outputDict;
        }

        //Put together the output file from Sinter
        public virtual JObject getOutputs()
        {
            JObject toplevelJObject = new JObject();

            toplevelJObject.Add("title", String.Format("Outputs from {0}", this.setupFile.title));
            toplevelJObject.Add("description", String.Format("Outputs from {0}", this.setupFile.title));
            toplevelJObject.Add(this.setupfileKey, this.setupFile.aspenFilename);
            toplevelJObject.Add("author", this.setupFile.author);
            toplevelJObject.Add("date", DateTime.Now.ToString("yyyy'-'MM'-'dd"));
            toplevelJObject.Add("filetype", "sinteroutputs");
            toplevelJObject.Add("version", 0.2);

            JObject inputs = new JObject();
            JObject outputs = new JObject();


            for (int ii = 1; ii <= this.countIO; ++ii)
            {
                sinter_Variable outputVar = (sinter_Variable)this.getIOByIndex(ii);
                string varname = outputVar.name;
                JObject jsonVar = new JObject();
                jsonVar.Add("value", outputVar.getOutput());
                if (outputVar.units == null || outputVar.units == "") //If units were not specified by the input file, use the default units.  (Note, may still be "")
                {
                    jsonVar.Add("units", outputVar.defaultUnits);
                }
                else
                {
                    jsonVar.Add("units", outputVar.units);
                }

                // If it's an output, add it to outputs, otherwise to inputs
                if (outputVar.isOutput)
                {
                    outputs.Add(varname, jsonVar);
                }
                else
                {
                    inputs.Add(varname, jsonVar);
                }
            }

            //Special case, Always put the run Status put the status in the outputs as "status".
            {
                JObject jsonVar = new JObject();
                jsonVar.Add("units", "");
                jsonVar.Add("value", (int)runStatus);
                outputs.Add("status", jsonVar);
            }

            toplevelJObject.Add("inputs", inputs);
            toplevelJObject.Add("outputs", outputs);

            return toplevelJObject;
        }
        #endregion outputs

        #region variable-meta-data-discovery

        public abstract sinter_Variable.sinter_IOType guessTypeFromSim(string path);
        public abstract sinter_Variable.sinter_IOType guessVectorTypeFromSim(string path, int[] indicies);
        public abstract void initializeUnits();

        public abstract int guessVectorSize(string path);
        public abstract int[] getVectorIndicies(string path, int size);

        public abstract string getCurrentUnits(string path);
        public abstract string getCurrentUnits(string path, int[] indicies);  //Vector version
        public abstract string getCurrentDescription(string path); //The simulation internal description, if it has one
        public abstract string getCurrentName(string name);   //The simulation internal name, if it has one.        
//        public abstract object getDefaultValue(string path);

        #endregion variable-meta-data-discovery

        #region get-variable-value
        public abstract void sendValueToSim<ValueType>(string path, ValueType value);
        public abstract void sendValueToSim<ValueType>(string path, int ii, ValueType value);  //for vectors
        public abstract void sendVectorToSim<ValueType>(string path, ValueType[] value);  //optimization to set whole vector at once

        public abstract ValueType recvValueFromSim<ValueType>(string path);
        //        public abstract ValueType recvValueFromSim<ValueType>(string path, int ii);  //For vectors
        public abstract void recvVectorFromSim<ValueType>(string path, int[] indicies, ValueType[] value);  //optimization to get whole vector at once
        #endregion get-variable-value


    }
}
