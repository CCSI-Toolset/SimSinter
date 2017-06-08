using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Security.Principal;
using Newtonsoft.Json.Linq;
using VariableTree;

namespace sinter.PSE
{
    /**
    * sinter_simGPROMS is for running configured gPROMS simulations.  
    * There are basically 2 completely seperate actions SimSinter does with a simulation:
    * 1. Configuring the simulation with SinterConfigGUI
    * 2. Running the simulation
    * Interactive Simulations like Aspen can combine these actions, but gPROMS is run as a
    * batch process, and cannot be linked to to query information about the simulation.  
    * So the run and configure steps are complete seperate.  So we have 2 classes for them:
    * simGPROMSconfig: Parses a GPJ file to learn about the availible input output variables and configure the sim.
    * simGPROMS: Takes a configured simulation and runs it with the user supplied inputs
    **/

    public class sinter_simGPROMS : sinter_Sim
    {
        #region data

        private static string GPROMS_EXECUTABLE_NAME = "gORUN_xml.exe"; //c:\\Program Files\\PSE\\gPROMS-core_4.0.0.54901\\bin\\
        private static string GPROMS_INPUT_FILENAME = "sinterInput.xml";
        private static string GPROMS_OUTPUT_FILENAME = "sinterOutput.xml";
        private string GPROMS_EXECUTABLE;

        private static Random s_entropy;
        /// <summary>
        /// The current job ID is stored in o_jobId. Each job submitted to gPROMS goRUN_xml is supposed
        /// to have a unique string job ID. The job ID of the current simulation is stored in this
        /// variable.
        /// </summary>
        private string o_jobId;

        /// <summary>
        /// The current job time is stored in o_jobTime. Each job submitted to gPROMS goRUN_XML is
        /// supposed to have a job time. The output is supposed to have this job time in it.
        /// </summary>
        private string o_jobTime;

        /// <summary>
        ///  Store the error messages to report to the client of this object.
        /// </summary>
        private List<string> o_errorMsgs;

        /// <summary>
        /// Store the warning message output.
        /// </summary>
        private List<string> o_warningMsgs;

        /// <summary>
        /// This controls whether an MS-DOS window should be visible while the console application
        /// is running.
        /// </summary>
        private bool o_showWindow = false;

        /// <summary>
        /// This variable controls whether a dialog box should be displayed if the process fails to launch.
        /// </summary>
        private bool o_showDialog = false;

        /// <summary>
        /// After the simulation is run, the output variables are stuck in the path->string value dictionary
        /// We don't know what type they are at that point.  Later ValueFromSim<ValueType> will tell us how to 
        /// interpret the string
        /// </summary>
        private Dictionary<string, string> o_outputtedVariables = new Dictionary<string, string>(); 
        #endregion data

        #region constructor

        public sinter_simGPROMS()
        {
            s_entropy = new Random();
            o_jobId = newJobId();
            workingDir = Directory.GetCurrentDirectory(); // a reasonable starting value
            o_errorMsgs = new List<string>();
            o_warningMsgs = new List<string>();
            simName = "gPROMS";

            string gpromshome = Environment.GetEnvironmentVariable("GPROMSHOME");
            if (gpromshome == null)
            {
                GPROMS_EXECUTABLE = GPROMS_EXECUTABLE_NAME;  //On the crazy off chance that GPROMSHOME is not set, use the PATH to try to find gorun_xml
            }
            else
            {
                GPROMS_EXECUTABLE = System.IO.Path.Combine(gpromshome, "bin", GPROMS_EXECUTABLE_NAME);
                if (!System.IO.File.Exists(GPROMS_EXECUTABLE))
                { 
                    GPROMS_EXECUTABLE = GPROMS_EXECUTABLE_NAME; //Again, if that path doesn't exist for some reason, fall back to the system path
                }
            }
        }

        #endregion constructor

        #region pathing
        public override char pathSeperator
        {
            get
            {
                return '.';
            }
        }

        #endregion pathing

        #region meta-data
        private string inputFilename
        {
            get { return GPROMS_INPUT_FILENAME; }
        }

        private string outputFilename
        {
            get { return GPROMS_OUTPUT_FILENAME; }
        }

        public override bool IsInitializing
        {
            get { return false; }
        }

        public override string[] errorsBasic()
        {
            return o_errorMsgs.ToArray();
        }

        public override string[] warningsBasic()
        {
            return o_warningMsgs.ToArray();
        }

        public override string setupfileKey { get { return "model"; } }

        /// <summary>
        /// Return the login ID of the current user or "anonymous" if the current user id is not
        /// available. If the process is running as a windows service, there may be no user id
        /// associated with the run.
        /// </summary>
        /// <returns>A login ID or anonymous.</returns>
        private string getUserId()
        {
            try
            {
                WindowsIdentity wi = WindowsIdentity.GetCurrent();
                if (wi != null)
                {
                    string name = wi.Name;
                    int backslash = name.IndexOf('\\');
                    if (backslash >= 0)
                    {
                        name = name.Substring(backslash + 1);
                    }
                    return name;

                }
            }
            catch
            {
                // no action required
            }
            return "anonymous"; // returning correct user id is not crucial
        }

        /// <summary>
        /// Generate a unique job ID for this job. The job ID provided in the XML input file should
        /// be reflected in the XML output file. Apparently gPROMS doesn't like '+' in a job ID, so
        /// this should return sequences of [a-zA-Z0-9]+.
        /// </summary>
        /// <returns>Returns a string starting with "sinter-" followed by pseudo random text.</returns>
        private string newJobId()
        {
            byte[] rndBits = new byte[9];
            s_entropy.NextBytes(rndBits);
            return string.Concat("sinter-", Convert.ToBase64String(rndBits)).Replace('+', '0').Replace('/', '1');
        }

        private string timestamp()
        {
            return string.Concat(DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture), "Z");
        }

        public override string[] externalVersionList()
        {
            string[] versions = { "4.0.0", "4.1.0", "4.2.0" };  //We don't support anything earlier than 4.0.0
            return versions;
        }

        /** Thankfully, gPROMS actually uses the same versioning format internally and externally**/
        public override string internal2externalVersion(string internalVersion)
        {
            return internalVersion;
        }

        public override string external2internalVersion(string externalVersion)
        {
            return externalVersion;
        }

        #endregion meta-data

        #region sim-control
        public override bool Vis
        {
            get
            {
                return o_showWindow;
            }
            set
            {
                o_showWindow = value;
            }
        }

        public override bool dialogSuppress
        {
            get
            {
                return !o_showDialog;
            }
            set
            {
                o_showDialog = !value;
            }
        }

        public override void openSim()
        {
            
            simVersion = "9999.0.0"; //We have no way of checking the version, so just make it some stupid large number
            string inputDir = Path.Combine(workingDir,"input");
            string gPROMS_src = Path.Combine(workingDir, o_setupFile.aspenFilename);
            string gPROMS_dest = Path.Combine(inputDir, o_setupFile.aspenFilename);
            // gPROMS goRUN_xml expects the input file to be in a directory named "input".
            Directory.CreateDirectory(inputDir); // creating a dir that already exists is not an error
            // Remove any previous copies of the file
            File.Delete(gPROMS_dest); // deleting non-existent file raises no exception
            // Copy encrypted gPROMS model into the input directory
            File.Copy(gPROMS_src, gPROMS_dest);

            //Clear out old copies of the input / output files
            File.Delete(Path.Combine(workingDir, inputFilename)); // deleting non-existent file raises no exception
            File.Delete(Path.Combine(workingDir, outputFilename)); // deleting non-existent file raises no exception

            simulatorStatus = sinter_simulatorStatus.si_OPEN;
        }

        public override void closeSim()
        {
            terminate();
            resetSim();
            simulatorStatus = sinter_simulatorStatus.si_CLOSED;
        }

        public override sinter_AppError resetSim()
        {
            o_errorMsgs.Clear();
            o_warningMsgs.Clear();
            o_runStatus = sinter_AppError.si_OKAY;
            return sinter_AppError.si_OKAY;
        }

        StringBuilder o_stdOutput = null;

        private void GatherStandardOutput(object sendingProcess, DataReceivedEventArgs outline)
        {
            if (!string.IsNullOrEmpty(outline.Data))
            {
                o_stdOutput.Append(outline.Data);
                o_stdOutput.Append(Environment.NewLine);
            }
        }

        public override sinter_AppError runSim()
        {
            if (simulatorStatus != sinter_simulatorStatus.si_OPEN)
            {
                throw new ArgumentException("Simulator is not in Open status, cannon run!");
            }

            o_runStatus = sinter_AppError.si_OKAY;
            try
            {
                simulatorStatus = sinter_simulatorStatus.si_RUNNING;
                using (Process p = new Process())
                {
                    Debug.WriteLine(GPROMS_EXECUTABLE, this.GetType().ToString() + ".runSim()");
                    ProcessStartInfo info = new ProcessStartInfo(GPROMS_EXECUTABLE);
                    Debug.WriteLine("info.Arguemts = " + inputFilename + " " + outputFilename, this.GetType().ToString() + ".runSim()");
                    info.Arguments = inputFilename + " " + outputFilename;
                    info.CreateNoWindow = !o_showWindow;
                    info.UseShellExecute = false;
                    Debug.WriteLine("info.WorkingDirectory = " + workingDir, this.GetType().ToString() + ".runSim()");
                    info.WorkingDirectory = workingDir;
                    info.ErrorDialog = o_showDialog;
                    info.RedirectStandardError = true;
                    info.RedirectStandardOutput = true;
                    o_stdOutput = new StringBuilder();
                    p.OutputDataReceived += new DataReceivedEventHandler(GatherStandardOutput);
                    p.StartInfo = info;
                    try {
                    p.Start();
                    }
                    catch (System.ComponentModel.Win32Exception ex) 
                    {
                        throw new ArgumentException(String.Format("gPROMS executable {0} could not be found.\n  It is not in the PATH or at {1}.\n Please ensure that gPROMS is installed.\n Inner exception:\n {2}", GPROMS_EXECUTABLE, Environment.GetEnvironmentVariable("GPROMSHOME"), ex.Message));
                    }

                    processID = p.Id;
                    p.BeginOutputReadLine();
                    string stdErrorOutput = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    processID = -1;
                    try
                    {
                        parseOutput();

                        if (o_runStatus != sinter_AppError.si_OKAY)
                        {
                            o_errorMsgs.Add(o_stdOutput.ToString());
                            o_errorMsgs.Add(stdErrorOutput);
                        }
                        else
                        {
                            o_warningMsgs.Add(o_stdOutput.ToString());
                            o_warningMsgs.Add(stdErrorOutput);
                        }
                    }
                    catch (Exception ex)
                    {
                        o_runStatus = sinter_AppError.si_SIMULATION_ERROR;
                        o_errorMsgs.Add("Caught exception trying to parse gPROMS output.  gPROMS simulation failed.  Exception:");
                        o_errorMsgs.Add(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message, this.GetType().ToString() + ".runSim()");
                Debug.WriteLine("StackTrace: " + ex.StackTrace, this.GetType().ToString() + ".runSim()");
                o_errorMsgs.Add(ex.Message);
                o_errorMsgs.Add(ex.StackTrace);
                throw;
            }
            finally
            {
                processID = -1;
                o_stdOutput = null;
                simulatorStatus = sinter_simulatorStatus.si_OPEN;

            }
            return o_runStatus;
        }

        public override bool terminate()
        {
            int pID = ProcessID;
            if (pID > 0)
            {
                Process p = Process.GetProcessById(pID);
                if (p != null) p.Kill();
                processID = -1;
                simulatorStatus = sinter_simulatorStatus.si_CLOSED;
                return true;
            }
            simulatorStatus = sinter_simulatorStatus.si_CLOSED;
            return false;
        }

        //gPROMS has no way to stop a sim, so this call must just be ignroed.
        public override void stopSim() { return; }

        #endregion sim-control 

        #region variable-tree
        /** 
         * void startDataTree
         * 
         * This function generates the root of a variable tree.  It does not fill in any child nodes.  This is
         * useful for generating the tree as the user opens nodes in the SinterConfigGUI 
         */
        public override void startDataTree()
        {
            throw new NotImplementedException();
        }

        public override VariableTree.VariableTreeNode findDataTreeNode(IList<String> pathArray)
        {
            throw new NotImplementedException();
        }

        public override void makeDataTree()
        {
            throw new NotImplementedException();
        }
        /** Leftmost name in the path refers to child of "ThisNode"
         */
        private VariableTreeNode findDataTreeNode(IList<String> pathArray, VariableTreeNode thisNode)
        {
            throw new NotImplementedException();
        }

        /** We're not sure how to do Heat Intergration Variables in Aspen+ yet */
        public override IList<sinter_IVariable> getHeatIntegrationVariables()
        {
            return new List<sinter_IVariable>();
        }

        #endregion variable-tree

        #region write-inputs
        //Used to write setting values, would be better to do as a set automatically
        private string getSettingWithDefault(string key, string def)
        {
            if (o_setupFile.Settings.Contains(key))
            {
                sinter_IVariable ivar = (sinter_IVariable)o_setupFile.Settings[key];
                if (sinter_Variable.sinter_IOType.si_STRING == ivar.type)
                {
                    sinter_Variable svar = (sinter_Variable)ivar;
                    return svar.Value.ToString();
                }
            }
            return def;
        }

        private void writeVarValues(XmlTextWriter xw, sinter_Variable var)
        {
            switch (var.type)
            {
                case sinter_Variable.sinter_IOType.si_DOUBLE:
                    xw.WriteElementString("RealValue", var.Value.ToString());
                    break;
                case sinter_Variable.sinter_IOType.si_INTEGER:
                    xw.WriteElementString("IntegerValue", var.Value.ToString());
                    break;
                case sinter_Variable.sinter_IOType.si_STRING:
                    xw.WriteElementString("StringValue", var.Value.ToString());
                    break;
                case sinter_Variable.sinter_IOType.si_DOUBLE_VEC:
                    sinter_Vector dvect = (sinter_Vector)var;
                    for (int i = 0; i < dvect.size; ++i)
                    {
                        xw.WriteElementString("RealValue", dvect.getElement(i).ToString());
                    }
                    break;
                case sinter_Variable.sinter_IOType.si_INTEGER_VEC:
                    sinter_Vector ivect = (sinter_Vector)var;
                    for (int i = 0; i < ivect.size; ++i)
                    {
                        xw.WriteElementString("IntegerValue", ivect.getElement(i).ToString());
                    }
                    break;
                case sinter_Variable.sinter_IOType.si_STRING_VEC:
                    sinter_Vector svect = (sinter_Vector)var;
                    for (int i = 0; i < svect.size; ++i)
                    {
                        xw.WriteElementString("StringValue", svect.getElement(i).ToString());
                    }
                    break;
                default:
                    throw new sinter_SimulationException(String.Format("gPROMS input variable, {0}, does not have a type that is supported by gPROMS sinter.",
                          var.name));
            }
        }

        /** 
         *  SimSinter variable paths have the process name on the front of them, but when actually running a job,
         *  gPROMS doesn't have the process name.  So this function snips it off.
         **/
        string snipProcessNameFromPath(string processName, string path)
        {
            int firstPeriodIndex = path.IndexOf(".");
            if (firstPeriodIndex == -1)
            {
                throw new ArgumentException(String.Format("gPROMS Variable path {0} is bad!  Must have at least one period.", path));
            }

            string pathProcessName = path.Substring(0, firstPeriodIndex);
            if (! String.Equals(pathProcessName, processName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(String.Format("gPROMS Variable {0} seems to come from the wrong process.  All variables run must be from {1}.", path, processName));
            }
            string returnPath = path.Substring(firstPeriodIndex + 1);

            return returnPath;

        }

        /**
         * The input file to gORUN_XML contains all the output variable to report back
         * named as "ReportVariable."  For arrays you have to request every value in the array.
         **/
        private void writeReportVariable(XmlTextWriter xw, sinter_Variable var)
        {
            string basePath = snipProcessNameFromPath(getSettingWithDefault("ProcessName", ""), var.addressStrings[0]);
            
            switch (var.type)
            { 
                case sinter_Variable.sinter_IOType.si_DOUBLE:
                case sinter_Variable.sinter_IOType.si_INTEGER:
                case sinter_Variable.sinter_IOType.si_STRING:
                    xw.WriteStartElement("ReportVariable");
                    xw.WriteAttributeString("pathName", basePath);
                    xw.WriteEndElement();
                    break;
                case sinter_Variable.sinter_IOType.si_DOUBLE_VEC:
                case sinter_Variable.sinter_IOType.si_INTEGER_VEC:
                case sinter_Variable.sinter_IOType.si_STRING_VEC:
                    sinter_Vector vec = (sinter_Vector)var;
                    int[] indicies = vec.get_vectorIndicies(this);
                    foreach(int idx in indicies) {
                        xw.WriteStartElement("ReportVariable");
                        xw.WriteAttributeString("pathName", addVecIndex(basePath, idx));
                        xw.WriteEndElement();
                    }
                    break;
                default:
                    throw new NotImplementedException(string.Format("Only static types are currently allowed with gPROMS, unknown type: {0}", var.type));
            }
        }

        private void writeGoRunInput()
        {
            using (XmlTextWriter xw = new XmlTextWriter(Path.Combine(workingDir, inputFilename), null))
            {
                xw.Formatting = Formatting.Indented;
                xw.Indentation = 2;
                xw.WriteStartDocument();
                xw.WriteStartElement("Calculation");
                xw.WriteAttributeString("jobId", o_jobId);
                xw.WriteAttributeString("user", getUserId());
                o_jobTime = timestamp(); // save this value to later check against output file
                xw.WriteAttributeString("time", o_jobTime);
                xw.WriteAttributeString("version", "2");
                xw.WriteStartElement("Model");
                xw.WriteAttributeString("fileName", setupFile.aspenFilename);
                xw.WriteAttributeString("processName", getSettingWithDefault("ProcessName", ""));
                if (o_setupFile.Settings.Contains("password"))
                {
                    xw.WriteAttributeString("password", ((sinter_Variable)o_setupFile.Settings["password"]).Value.ToString());
                }
                xw.WriteEndElement();
                xw.WriteStartElement("Simulation");
                foreach (sinter_Variable var in o_setupFile.Variables)
                {
                    if (var.isInput)
                    {
                        xw.WriteStartElement("ModelSpecification");
                        xw.WriteAttributeString("name", var.addressStrings[0]);
                        writeVarValues(xw, var);
                        xw.WriteEndElement();
                    }
                }
                foreach (sinter_Variable var in o_setupFile.Variables)
                {
                    if (var.isOutput)
                    {
                        writeReportVariable(xw, var);
                    }
                }
                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.WriteEndDocument();
            }
        }

        public override void sendInputsToSim()
        {
            writeGoRunInput();
        }

        #endregion write-inputs

        #region output-handling

        private bool outputMatchesJob(XmlDocument doc)
        {
            XmlElement ce = doc.DocumentElement;
            return ("CalculationResult" == ce.Name) &&
                (o_jobId != null) && (o_jobTime != null) &&
                (o_jobId == ce.GetAttribute("jobId")) &&
                (o_jobTime == ce.GetAttribute("jobTime"));
        }

        private bool errorResult(XmlDocument doc)
        {
            XmlNode error = doc.SelectSingleNode("/CalculationResult/Error");
            if (error != null)
            {
                o_runStatus = sinter_AppError.si_SIMULATION_ERROR;
                o_errorMsgs.Add(error.InnerText);
                return true;
            }
            return false;
        }

        private void addOutputVariable(XmlNode node)
        {
            if (node is XmlElement)
            {
                JObject valDict = new JObject();
                XmlElement e = (XmlElement)node;
                String gPROMSoutVarPath = e.GetAttribute("pathName");
                String outVarPath = getSettingWithDefault("ProcessName", "") + "." + gPROMSoutVarPath;
                string valstring = e.GetAttribute("value");
                o_outputtedVariables[outVarPath] = valstring;  //outvarpath might point to an array entry, deal with that later
            }
        }

        /// <summary>
        /// Attempt to parse the gPROMS output XML file to determine either the value of the 
        /// output parameters or the error code and error message.
        /// </summary>
        /// <returns>null indicates an error condition was read in the output XML file; otherwise,
        /// the returned JSON object has the values of the output parameters.
        /// </returns>
        private void parseOutput()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(workingDir, outputFilename));
            
            if (errorResult(doc)) return;  //Check error result first, as some error results don't include the jobID

            if (outputMatchesJob(doc))
            {

                //Only get the outputs if we didn't get any kind of error from gPROMS
                if (runStatus == sinter_AppError.si_OKAY)
                {
                    foreach (XmlNode node in doc.SelectNodes("/CalculationResult/Simulation/ReportVariable"))
                    {
                        addOutputVariable(node);
                    }
                }
            }
            else
            {
                throw new sinter_SimulationException(String.Format("gPROMS output XML file, {0}, does not have a job ID and time matching the input XML file.",
                      outputFilename));

            }
            return;
        }

/* We'll now use the code in sinter_Sim
        public override JObject getOutputs()
        {
            return o_outputResults;
        }
        */
    #endregion output-handling

        #region variable-meta-data-discovery

        public override void initializeDefaults()
        {
            throw new NotImplementedException();
        }

        public override sinter_Variable.sinter_IOType guessTypeFromSim(string path) { throw new NotImplementedException(); }
        public override sinter_Variable.sinter_IOType guessVectorTypeFromSim(string path, int[] indicies) { throw new NotImplementedException();}
        public override void initializeUnits() { throw new NotImplementedException(); }

        public override int guessVectorSize(string path) { throw new NotImplementedException(); }

        /** 
         * gPROMS arrays are indexed from 1
         **/
        public override int[] getVectorIndicies(string path, int size) {
            int[] indicies = new int[size];
            for (int ii = 1; ii <= size; ++ii)
            {
                indicies[ii - 1] = ii; 
            }
            return indicies;
        }

        public override string getCurrentUnits(string path) { throw new NotImplementedException(); }
        public override string getCurrentUnits(string path, int[] indicies) { throw new NotImplementedException(); }//Vector version
        public override string getCurrentDescription(string path) { throw new NotImplementedException(); } //The simulation internal description, if it has one
        public override string getCurrentName(string name) { throw new NotImplementedException(); }   //The simulation internal name, if it has one.        
        #endregion variable-meta-data-discovery

        #region get-variable-value
        public override void sendValueToSim<ValueType>(string path, ValueType value) { throw new NotImplementedException(); }
        public override void sendValueToSim<ValueType>(string path, int ii, ValueType value) { throw new NotImplementedException(); }  //for vectors
        public override void sendVectorToSim<ValueType>(string path, ValueType[] value) { throw new NotImplementedException(); }  //optimization to set whole vector at once

        /**
         * recvValueFromSimAsObjec was added to give us a chance to figure out the type of a variable automatically in the GUI
         * They should not be called on variables that may not exist, they will throw an Exception
         */
//        public override Object recvValueFromSimAsObject(string path) { throw new NotImplementedException(); }
//        public override Object recvValueFromSimAsObject(string path, int ii) { throw new NotImplementedException(); }  //For vectors type identification

        public override ValueType recvValueFromSim<ValueType>(string path) {
            if (runStatus != sinter_AppError.si_OKAY)
            {
                throw new ArgumentException("Programming Error: Cannot call recvValueFromSim on a simulation that has not completed successfully.");
            }
            string valstring = o_outputtedVariables[path];
            if(typeof(ValueType) == typeof(string)) { //No template specialization in C#, so we have to
                return (ValueType)(object)valstring;  //break the typesystem.  
            } else if(typeof(ValueType) == typeof(double)) {
                return (ValueType)(object)Convert.ToDouble(valstring);
            } else if(typeof(ValueType) == typeof(int)) {
                return (ValueType)(object)Convert.ToInt32(valstring);
            } else {
                return default(ValueType);
            }
        }
        //        public override ValueType recvValueFromSim<ValueType>(string path, int ii);  //For vectors
        public override void recvVectorFromSim<ValueType>(string path, int[] indicies, ValueType[] value) {
            if (runStatus != sinter_AppError.si_OKAY)
            {
                throw new ArgumentException("Programming Error: Cannot call recvValueFromSim on a simulation that has not completed successfully.");
            }

            foreach (int idx in indicies)
            {
                string idx_path = addVecIndex(path, idx);
                string valstring = o_outputtedVariables[idx_path];
                if (typeof(ValueType) == typeof(string))
                { //No template specialization in C#, so we have to break the typesystem.
                    value[idx-1] = (ValueType)(object)valstring;    
                }
                else if (typeof(ValueType) == typeof(double))
                {
                    value[idx-1] = (ValueType)(object)Convert.ToDouble(valstring);
                }
                else if (typeof(ValueType) == typeof(int))
                {
                    value[idx-1] = (ValueType)(object)Convert.ToInt32(valstring);
                }
                else
                {
                    value[idx-1] = default(ValueType); 
                }
            }
        }  //optimization to get whole vector at once
        #endregion get-variable-value

    }
}
