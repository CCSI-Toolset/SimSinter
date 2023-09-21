using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Marshal = System.Runtime.InteropServices.Marshal;
using VariableTree;
using Happ;
using System.Threading;
using System.Timers;

namespace sinter
{

    public class sinter_SimAspen : sinter_InteractiveSim
    {
        private Happ.HappLS oaspen = null;
        private Happ.IHNode otree = null;

        //Syncronization of running and stopping the simulation.  
        //o_terminateMonitor is a condition variable that is signaled when either the simulation
        //has completed, or the user wants to stop it early.
        private AutoResetEvent o_terminateMonitor;
        private bool o_simPaused; //Set to True when the simulation has completed, but runSim hasn't handled it yet
        private bool o_stopSim;   //Set to true when stopSim is called, but runSim hasn't stopped the Sim Yet
        private bool o_stopTimedOut; //When stop is called, a timeOut is set up. If it times out the stop has failed.

        private System.Timers.Timer o_stopTimer; //Set up after stop is called.  If this times out, stop has failed.

        //Boolean variables for controlling what is visible from the simulation.
        //Should match what's in the simulation, these only exist to allow 
        //the options to be set when the simulation itself is not availible.
        private bool o_visible = false;
        private bool o_dialogSuppress = true;


        // Private i_aspen As Happ.IHapp  'ANother aspen connection?
        private List<string> o_streamNames = new List<string>();

        private List<string> o_blockNames = new List<string>();
        private List<string> o_streamInputs = new List<string>();
        private List<string> o_streamOutputs = new List<string>();
        private List<string> o_blockInputs = new List<string>();

        private List<string> o_blockOutputs = new List<string>();
        private Dictionary<string, List<string>> o_streamError = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> o_blockError = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> o_convergenceError = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> o_streamWarn = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> o_blockWarn = new Dictionary<string, List<string>>();

        //        private string o_EventMsg;

        // A creatable COM class must have a Public Sub New() 
        // with no parameters, otherwise, the class will not be 
        // registered in the COM registry and cannot be created 
        // via CreateObject.
        public sinter_SimAspen()
            : base()
        {
            o_availibleSettings = new Dictionary<string, Tuple<set_setting, get_setting, sinter_Variable.sinter_IOType>>();

            o_terminateMonitor = new AutoResetEvent(false);

            o_stopTimer = new System.Timers.Timer();
            //Set to True when the simulation has completed, but runSim hasn't handled it yet
            o_simPaused = false;
            //Set to true when stopSim is called, but runSim hasn't stopped the Sim Yet
            o_stopSim = false;
            //Set to true if the stop timeout time goes off
            o_stopTimedOut = false;
            simName = "Aspen Plus";
        }

        void HandleOnCalculationCompleted()
        {
            lock (o_terminateMonitor)
            {
                o_simPaused = true;
                o_terminateMonitor.Set();
            }
        }

        void HandleStopTimerElapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            lock (o_terminateMonitor)
            {
                o_stopTimedOut = true;
                o_terminateMonitor.Set();
            }
        }


        public override string setupfileKey { get { return "aspenfile"; } }

        public override char pathSeperator
        {
            get
            {
                return '\\';
            }
        }


        /** 
         * void makeDataTree
         * 
         * This function generates the entire variable tree of the variables availible in the simulation.  
         * All input and output variables.  This is used primarily for the Sinter Config GUI.
         */
        public override void makeDataTree()
        {
            //To save a little time and trouble, we make the Data node the root node.  We don't need anything
            //from the other 2 branches.
            o_dataTree = new VariableTree.VariableTree(parsePath, pathSeperator);
            Happ.IHNodeCol rootChildren = otree.Elements;
            Happ.IHNode dataNode = rootChildren["Data"];
            VariableTreeNode v_dataNode = new VariableTreeNode("Data", "", pathSeperator);

            Happ.IHNodeCol dataChildren = dataNode.Elements;

            Happ.IHNode optionsNode = dataChildren["Flowsheeting Options"];
            VariableTreeNode v_optionsNode = new VariableTreeNode("Flowsheeting Options", "Data.Flowsheeting Options", pathSeperator);
            v_dataNode.addChild(v_optionsNode);
            Happ.IHNodeCol optionsChildren = optionsNode.Elements;

            Happ.IHNode designSpecNode = optionsChildren["Design-Spec"];
            VariableTreeNode v_designSpecNode = new VariableTreeNode("Design-Spec", "Data.Flowsheeting Options.Design-Spec", pathSeperator);
            v_optionsNode.addChild(v_designSpecNode);
            addBlocksOrStreamsNodes(designSpecNode, v_designSpecNode);

            Happ.IHNode CalculatorNode = optionsChildren["Calculator"];
            VariableTreeNode v_CalculatorNode = new VariableTreeNode("Calculator", "Data.Flowsheeting Options.Calculator", pathSeperator);
            v_optionsNode.addChild(v_CalculatorNode);
            addBlocksOrStreamsNodes(CalculatorNode, v_CalculatorNode);


            Happ.IHNode streamsNode = dataChildren["Streams"];
            VariableTreeNode v_streamsNode = new VariableTreeNode("Streams", "Data.Streams", pathSeperator);
            v_dataNode.addChild(v_streamsNode);
            addBlocksOrStreamsNodes(streamsNode, v_streamsNode);


            Happ.IHNode blocksNode = dataChildren["Blocks"];
            VariableTreeNode v_blocksNode = new VariableTreeNode("Blocks", "Data.Blocks", pathSeperator);
            v_dataNode.addChild(v_blocksNode);
            addBlocksOrStreamsNodes(blocksNode, v_blocksNode);

            o_dataTree.rootNode = v_dataNode;
        }

        //This is a specialized function for screening out the stuff we don't need from Streams and Blocks
        public void addBlocksOrStreamsNodes(Happ.IHNode bsNode, VariableTreeNode v_bsNode)
        {
            Happ.IHNodeCol children = bsNode.Elements;
            foreach (Happ.IHNode child in children)
            {
                String name = child.Name;
                VariableTreeNode v_childNode = new VariableTreeNode(name, v_bsNode.path + pathSeperator + name, pathSeperator);
                v_bsNode.addChild(v_childNode);
                addBlocksOrStreamsChild(child, v_childNode);
            }
        }

        public void addBlocksOrStreamsChild(Happ.IHNode bsNode, VariableTreeNode v_bsNode)
        {
            Happ.IHNodeCol children = bsNode.Elements;
            Happ.IHNode inputs = children["Input"];
            v_bsNode.addChild(makeDataTreeNode(inputs, v_bsNode));

            Happ.IHNode outputs = children["Output"];
            v_bsNode.addChild(makeDataTreeNode(outputs, v_bsNode));
        }

        /** 
         * void startDataTree
         * 
         * This function generates the root of a variable tree.  It does not fill in any child nodes.  This is
         * useful for generating the tree as the user opens nodes in the SinterConfigGUI 
         */
        public override void startDataTree()
        {
            //To save a little time and trouble, we make the Data node the root node.  We don't need anything
            //from the other 2 branches.
            o_dataTree = new VariableTree.VariableTree(parsePath, pathSeperator);
            Happ.IHNodeCol rootChildren = otree.Elements;
            Happ.IHNode dataNode = rootChildren["Data"];
            VariableTreeNode v_dataNode = new VariableTreeNode("", "", pathSeperator);

            o_dataTree.rootNode = v_dataNode;
        }

        public override VariableTree.VariableTreeNode findDataTreeNode(IList<String> pathArray)
        {
            return findDataTreeNode(pathArray, o_dataTree.rootNode);
        }


        /** Leftmost name in the path refers to child of "ThisNode"
         */
        private VariableTreeNode findDataTreeNode(IList<String> pathArray, VariableTreeNode thisNode)
        {

            if (thisNode.o_children.ContainsKey("DummyChild"))
            {
                thisNode.o_children.Remove("DummyChild");
                Happ.IHNode thisAspenNode = oaspen.Tree.FindNode(thisNode.path);
                if (thisAspenNode.Dimension == 1)
                {
                    Happ.IHNodeCol thisAspenNodeChildren = thisAspenNode.Elements;

                    foreach (Happ.IHNode child in thisAspenNodeChildren)
                    {
                        String name = child.Name;
                        VariableTreeNode v_childNode = new VariableTreeNode(name, thisNode.path + pathSeperator + name, pathSeperator);
                        thisNode.addChild(v_childNode);
                    }
                }
                else if (thisAspenNode.Dimension == 2)
                {
                    Happ.IHNodeCol thisAspenNodeChildren = thisAspenNode.Elements;
                    int OneDCount = 0;
                    int rowCount = thisAspenNodeChildren.get_RowCount(0);
                    int colCount = thisAspenNodeChildren.get_RowCount(1);
                    int firstIndex = thisAspenNode.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_FIRSTPAIR);
                    //get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_FIRSTPAIR);
                    foreach (Happ.IHNode child in thisAspenNodeChildren)
                    {
                        try
                        {
                            String prettyName = child.Name;
                            String name = string.Format("(#{0},#{1})", (OneDCount / colCount), OneDCount % colCount);
                            VariableTreeNode v_childNode = new VariableTreeNode(prettyName, name, thisNode.path + pathSeperator + name, pathSeperator);
                            thisNode.addChild(v_childNode);
                            OneDCount++;

                        }
                        catch (Exception)
                        {
                            //pass
                        }
                    }
                }

            }

            if (pathArray.Count == 0)
            {
                return thisNode;
            }
            else
            {
                string childName = pathArray[0];
                pathArray.RemoveAt(0);
                return findDataTreeNode(pathArray, thisNode.o_children[childName]);
            }
        }

        public VariableTreeNode makeDataTreeNode(Happ.IHNode aspenNode, VariableTreeNode parent)
        {
            String name = aspenNode.Name;
            VariableTreeNode thisNode = new VariableTreeNode(name, String.Format("{0}{1}{2}", parent.path, pathSeperator, name), pathSeperator);

            if (aspenNode.Dimension == 1)
            {
                try
                {
                    Happ.IHNodeCol children = aspenNode.Elements;
                    foreach (Happ.IHNode child in children)
                    {

                        thisNode.addChild(makeDataTreeNode(child, thisNode));
                    }

                }
                catch 
                {
                    //TODO, is something supposed to happen here?
                }
            }
            else if (aspenNode.Dimension == 2)
            {
                try
                {
                    Happ.IHNodeCol children = aspenNode.Elements;
                    foreach (Happ.IHNode child in children)
                    {

                        thisNode.addChild(makeDataTreeNode(child, thisNode));
                    }

                }
                catch (Exception)
                {
                    //TODO, is something supposed to happen here?
                }

            }
            return thisNode;
        }
        //Some AspenPlus nodes cannot be included in the data tree, this attempts to weed those out.
        //Currently it checks Stream Results/Table which can have duplicate names
        private bool isIllegalNode(String name, String parentName)
        {
            bool parentTrigger = parentName == "Stream Results" ||
                parentName == "Work Results" ||
                parentName == "Heat Results" ||
                parentName == "Stream-Sum";
            return parentTrigger && name == "Table";
        }


        //        public object getEventMsg()
        //       {
        //          return o_EventMsg;
        //     }


        //    public override string[] getNameOfEach(string s)
        //{
        //    int n = 0;
        //    int i = 0;

        //    //count nodes
        //    n = 0;
        //    Happ.IHNodeCol nodes = oaspen.Tree.FindNode("Data\\Blocks").Elements;
        //    foreach (Happ.IHNode node in nodes) {
        //        if (Convert.ToBoolean(node.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_RECORDTYPE) == s)) {
        //            n = n + 1;
        //        }
        //    }
        //    nodes = oaspen.Tree.FindNode("Data\\Streams").Elements;
        //    foreach (Happ.IHNode node in nodes) {
        //        if (Convert.ToBoolean(node.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_RECORDTYPE) == s)) {
        //            n = n + 1;
        //        }
        //    }

        //    //get list on names
        //    string[] names = new string[n];
        //    i = 0;
        //    nodes = oaspen.Tree.FindNode("Data\\Blocks").Elements;
        //    foreach (Happ.IHNode node in nodes) {
        //        if (Convert.ToBoolean(node.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_RECORDTYPE) == s)) {
        //            names[i] = node.Name;
        //            i = i + 1;
        //        }
        //    }
        //    nodes = oaspen.Tree.FindNode("Data\\Streams").Elements;
        //    foreach (Happ.IHNode node in nodes)
        //    {
        //        if (Convert.ToBoolean(node.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_RECORDTYPE) == s)) {
        //            names[i] = node.Name;
        //            i = i + 1;
        //        }
        //    }
        //    return names;
        //	}

        void initVersionNumber()
        {//Get the version number From a string something like: "Aspen Plus 34.0 OLE Services"
            string aspen_name = oaspen.Name;
            string numberPattern = @"(^|\s)[-+]?([0-9]*\.[0-9]+(E[-+]?[0-9]+)?|[0-9]+)\s"; //A good general number pattern, a bit much for a version #
            Regex thisRegex = new Regex(numberPattern, RegexOptions.IgnoreCase);
            Match thisMatch = thisRegex.Match(aspen_name);

            aspen_name = aspen_name.Substring(thisMatch.Index, thisMatch.Length);
            simVersion = aspen_name.Trim();
        }

        public override void openSim()
        {
            lock (this) // lock whenever accessing oaspen
            {
                if ((oaspen == null))
                {
                    simulatorStatus = sinter_simulatorStatus.si_INITIALIZING;
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
                        appType = Type.GetTypeFromProgID(String.Format("Apwn.Document.{0}.0", simVer));
                    }

                    if (appType == null)
                    {
                        appType = Type.GetTypeFromProgID("Apwn.Document");// new Happ.IHapp(); // Interaction.GetObject(workingDir + "\\" + simFile);
                    }
                    if (appType == null)  //Workaround for aspen issue that can't find Apwn.Document, but can find specific versions
                    {
                        int versionNum = 99;
                        for (; versionNum > 0; --versionNum)
                        {
                            string typename = String.Format("Apwn.Document.{0}.0", versionNum);
                            appType = Type.GetTypeFromProgID(typename);
                            if (appType != null)
                            {
                                break;
                            }
                        }
                        if (appType == null)
                        {
                            throw new System.InvalidProgramException("Could not find Aspen Plus.  Is it installed?");
                        }

                    }
                    Object app = Activator.CreateInstance(appType);
                    oaspen = (Happ.HappLS)app;
                }
                processID = oaspen.ProcessId;
                Debug.WriteLine(workingDir + "\\" + simFile);
                simulatorStatus = sinter_simulatorStatus.si_INITIALIZING;
                string backupFilename = System.IO.Path.Combine(workingDir, simFile);
                string absBackupFilename = System.IO.Path.GetFullPath(backupFilename);

                initVersionNumber();

                try
                {
                    oaspen.InitFromArchive2(absBackupFilename);
                    Vis = o_visible;  //Enforce visibility and dialogsuppression ASAP.
                    dialogSuppress = o_dialogSuppress;
                    otree = this.Aspen.Tree;
                }
                finally
                {
                    simulatorStatus = sinter_simulatorStatus.si_OPEN;
                    checkSimVersionConstraints();
                }
            }
        }

        public override bool Vis
        {
            get
            {
                lock (this)
                {
                    if (oaspen == null)
                    {
                        return o_visible;
                    }
                    else
                    {
                        o_visible = oaspen.Visible;
                        return oaspen.Visible;
                    }
                }
            }
            set
            {
                lock (this)
                {
                    if (oaspen == null)
                    {
                        o_visible = value;
                    }
                    else
                    {
                        oaspen.Visible = value;
                        o_visible = oaspen.Visible;
                    }
                }
            }
        }

        public override bool dialogSuppress
        {
            get
            {
                lock (this)
                {
                    if (oaspen == null)
                    {
                        return o_dialogSuppress;
                    }
                    else
                    {
                        if (oaspen.SuppressDialogs == 0)
                            o_dialogSuppress = false;
                        else
                            o_dialogSuppress = true;
                    }
                    return o_dialogSuppress;
                }
            }

            set
            {
                lock (this)
                {
                    if (oaspen == null)
                    {
                        o_dialogSuppress = value;
                    }
                    else
                    {
                        if (value)
                        {
                            oaspen.SuppressDialogs = 1;
                            o_dialogSuppress = value;
                        }
                        else
                        {
                            oaspen.SuppressDialogs = 0;
                            o_dialogSuppress = value;
                        }
                    }
                }
            }
        }
        [DllImport("ole32.dll", EntryPoint = "CoFreeUnusedLibraries", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]

        private static extern long CoFreeUnusedLibraries();

        public override void closeSim()
        {
            int i = 0;

            lock (this)
            {
                if (simulatorStatus == sinter_simulatorStatus.si_OPEN)
                {

                    try
                    {
                        oaspen.Engine.Stop();
                        for (i = 1; i <= 15; i++)
                        {
                            // close aspen until its gone or give up after 15 trys
                            // i think when somthing kills excel it leaves the aspen
                            // object with extra in a refrence counter but I may be wrong.
                            oaspen.Close();
                        }
                        CoFreeUnusedLibraries();
                        simulatorStatus = sinter_simulatorStatus.si_CLOSED;
                    }
                    catch
                    {
                        simulatorStatus = sinter_simulatorStatus.si_ERROR;
                        oaspen = null;
                        GC.Collect();
                    }
                }
            }
        }
        public override void stopSim()
        {
            lock (o_terminateMonitor)
            {
                o_stopSim = true;
                o_terminateMonitor.Set();
            }
        }

        public void PrettyPrintToFile(string node_path, ref TextWriter sw, string indent = "")
        {
            lock (this)
            {
                Happ.IHNode node = oaspen.Tree.FindNode(node_path);

                if ((node == null))
                {
                    Debug.WriteLine("node_path({0}) did not evalute to a IHNode", node_path);
                    throw new ArgumentException("node_path did not evalute to a IHNode");
                }
                Debug.WriteLine("FindNode({0}) = {1}", node_path, node.Name);
                PrettyPrintToFile(node, sw, indent, true);
            }
        }

        private void PrettyPrintToFile(ref Happ.IHNode node, ref TextWriter sw, string indent = "")
        {
            PrettyPrintToFile(node, sw, indent, true);
        }

        private void PrettyPrintToFile(Happ.IHNode node, TextWriter sw, string indent, bool last)
        {
            int it = 0;
            if ((node == null | object.ReferenceEquals(node, oaspen.Tree)))
            {
                node = oaspen.Tree;
                sw.WriteLine(indent + "{");
                it = 0;
                foreach (Happ.IHNode n in node.Elements)
                {
                    it = it + 1;
                    if ((n.Name == "Comments"))
                    {
                        string.Format("COMMENTS 1st {0} -- {1}", it == node.Elements.Count, node.Elements.Dimension);
                    }
                    PrettyPrintToFile(n, sw, indent + "    ", it == node.Elements.Count);
                }
                sw.WriteLine(indent + "}");
            }
            else
            {
                if ((node.Dimension == 0))
                {
                    // leaf node
                    if ((last))
                    {
                        sw.WriteLine(indent + "\"" + node.Name + "\"" + ":\"" + node.Value + "\"");
                        //XXX
                    }
                    else
                    {
                        sw.WriteLine(indent + "\"" + node.Name + "\"" + ":\"" + node.Value + "\",");
                        //XXX
                    }
                    return;
                }

                sw.WriteLine(indent + "\"" + node.Name + "\"" + ": {");

                it = 0;
                // Enumerates through IHNodes in container 
                // NOTE: USE A StringBuffer and remove last ,
                //
                StringBuilder buff = new StringBuilder();
                StringWriter swbuff = new StringWriter(buff);
                bool cc = false;
                int count = node.Elements.Count;
                foreach (Happ.IHNode ihnode in node.Elements)
                {
                    it = it + 1;
                    cc = it == count;
                    // Oddly sometimes there are children here not detected above.. ' System.Console
                    sw.WriteLine(string.Format("{0}{1} Enum ValueType {2} {3}/{4} -- Dim({5})", indent, ihnode.Name, ihnode.ValueType, it, count, node.Elements.Dimension));
                    PrettyPrintToFile(ihnode, swbuff, indent + "    ", cc);
                }
                swbuff.Close();

                // Remove Last comma if necessary
                string s = null;
                int idx = 0;
                if ((cc == false & it > 0))
                {
                    s = buff.ToString();
                    // NOTE: Optimize
                    idx = s.LastIndexOf(',');
                    if ((idx >= 0))
                    {
                        Debug.WriteLine("{0}REMOVE COMMA: ({1})", indent, s.Substring(idx));
                        //buff.Replace(","c, " "c, idx, 1)
                        buff.Remove(idx, 1);
                    }
                    else
                    {
                        Debug.WriteLine("{0}REMOVE COMMA FAILED", indent);
                    }
                }

                sw.Write(swbuff);

                if ((last))
                {
                    sw.WriteLine(indent + "}");
                    //XXX
                }
                else
                {
                    sw.WriteLine(indent + "},");
                    //XXX
                }
            }
        }


        private int addErrors(ref List<string> errorlist, Happ.IHNode objWithErrors)
        {
            Happ.IHNode output = objWithErrors.FindNode("Output/PER_ERROR");
            Happ.IHNodeCol myerrs = output.Elements;
            string currentError = "";
            //Aspen seperates errors by empty string
            foreach (dynamic myerr in myerrs)
            {
                if ((string.IsNullOrEmpty(myerr.Value)))
                {
                    errorlist.Add(currentError);
                    currentError = "";
                }
                else
                {
                    currentError = currentError + myerr.Value;
                }
            }

            return 0;
        }


        private sinter_AppError convCheck()
        {
            sinter_AppError functionReturnValue = sinter_AppError.si_OKAY;
            // look through the convergnce node and check for errors or warnings.
            int n_err = 0;
            int n_war = 0;

            o_convergenceError.Clear();

            Happ.IHNodeCol convNodes = oaspen.Tree.FindNode("Data\\Convergence\\Convergence").Elements;
            foreach (Happ.IHNode convNode in convNodes)
            {
                //Use bitwise and to check the success flag
                if ((convNode.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_COMPSTATUS, Type.Missing) & (short)Happ.HAPCompStatusCode.HAP_RESULTS_SUCCESS) > 0)
                {
                    continue;
                    //if not success, Use bitwise and to check the warning flag
                }
                else if ((convNode.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_COMPSTATUS) & (short)Happ.HAPCompStatusCode.HAP_RESULTS_WARNINGS) > 0)
                {
                    if ((!o_convergenceError.ContainsKey(convNode.Name)))
                    {
                        o_convergenceError.Add(convNode.Name, new List<string>());
                    }
                    List<string> convWarnTmp = o_convergenceError[convNode.Name];
                    addErrors(ref convWarnTmp, convNode);
                    o_convergenceError[convNode.Name] = convWarnTmp;
                    n_war = n_war + 1;
                    continue;
                }
                //If is is not success or warning it is an error
                if ((!o_convergenceError.ContainsKey(convNode.Name)))
                {
                    o_convergenceError.Add(convNode.Name, new List<string>());
                }
                List<string> convErrTmp = o_convergenceError[convNode.Name];
                addErrors(ref convErrTmp, convNode);
                o_convergenceError[convNode.Name] = convErrTmp;

                n_err = n_err + 1;
            }
            if (n_err > 0)
            {
                functionReturnValue = sinter_AppError.si_SIMULATION_ERROR;
            }
            else if (n_war > 0)
            {
                functionReturnValue = sinter_AppError.si_SIMULATION_WARNING;
            }
            else
            {
                functionReturnValue = sinter_AppError.si_OKAY; ;
            }
            return functionReturnValue;
        }


        private sinter_AppError blockCheck()
        {
            sinter_AppError functionReturnValue = sinter_AppError.si_OKAY;
            // look through the convergnce node and check for errors or warnings.

            int n_err = 0;
            int n_war = 0;

            Happ.IHNodeCol blockNodes = oaspen.Tree.FindNode("Data\\Blocks").Elements;
            foreach (Happ.IHNode blockNode in blockNodes)
            {
                //Use bitwise and to check the success flag
                if ((blockNode.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_COMPSTATUS) & (short)Happ.HAPCompStatusCode.HAP_RESULTS_SUCCESS) > 0)
                {
                    continue;
                }
                //if not success, Use bitwise and to check the warning flag
                else if ((blockNode.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_COMPSTATUS) & (short)Happ.HAPCompStatusCode.HAP_RESULTS_WARNINGS) > 0)
                {
                    if ((!o_blockWarn.ContainsKey(blockNode.Name)))
                    {
                        o_blockWarn.Add(blockNode.Name, new List<string>());
                    }
                    List<string> blockWarnTmp = o_blockWarn[blockNode.Name];
                    addErrors(ref blockWarnTmp, blockNode);
                    o_convergenceError[blockNode.Name] = blockWarnTmp;
                    n_war = n_war + 1;
                    continue;
                }
                //If its not success or a warning its an error
                if ((!o_blockError.ContainsKey(blockNode.Name)))
                {
                    o_blockError.Add(blockNode.Name, new List<string>());
                }
                List<string> blockErrTmp = o_blockError[blockNode.Name];
                addErrors(ref blockErrTmp, blockNode);
                o_blockError[blockNode.Name] = blockErrTmp;
                n_err = n_err + 1;
            }

            if (n_err > 0)
            {
                functionReturnValue = sinter_AppError.si_SIMULATION_ERROR;
            }
            else if (n_war > 0)
            {
                functionReturnValue = sinter_AppError.si_SIMULATION_WARNING;
            }
            else
            {
                functionReturnValue = sinter_AppError.si_OKAY; ;
            }

            return functionReturnValue;
        }

        private sinter_AppError streamCheck()
        {
            sinter_AppError functionReturnValue = sinter_AppError.si_OKAY;
            // look through the convergnce node and check for errors or warnings.
            int n_err = 0;
            int n_war = 0;
            Happ.IHNodeCol streamNodes = oaspen.Tree.FindNode("Data\\Streams").Elements;
            foreach (Happ.IHNode streamNode in streamNodes)
            {
                //Use bitwise and to check the success flag
                if ((streamNode.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_COMPSTATUS) & (short)Happ.HAPCompStatusCode.HAP_RESULTS_SUCCESS) > 0)
                {
                    continue;
                }
                //if not success, Use bitwise and to check the warning flag
                else if ((streamNode.get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_COMPSTATUS) & (short)Happ.HAPCompStatusCode.HAP_RESULTS_WARNINGS) > 0)
                {
                    if ((!o_streamWarn.ContainsKey(streamNode.Name)))
                    {
                        o_streamWarn.Add(streamNode.Name, new List<string>());
                    }
                    List<string> streamWarnTmp = o_streamWarn[streamNode.Name];
                    addErrors(ref streamWarnTmp, streamNode);
                    o_streamWarn[streamNode.Name] = streamWarnTmp;

                    n_war = n_war + 1;
                    continue;
                }
                //If its not success or a warning its an error
                if ((!o_streamError.ContainsKey(streamNode.Name)))
                {
                    o_streamError.Add(streamNode.Name, new List<string>());
                }
                List<string> streamErrTmp = o_streamError[streamNode.Name];
                addErrors(ref streamErrTmp, streamNode);
                o_streamError[streamNode.Name] = streamErrTmp;
                n_err = n_err + 1;
            }

            if (n_err > 0)
            {
                functionReturnValue = sinter_AppError.si_SIMULATION_ERROR;
            }
            else if (n_war > 0)
            {
                functionReturnValue = sinter_AppError.si_SIMULATION_WARNING;
            }
            else
            {
                functionReturnValue = sinter_AppError.si_OKAY; ;
            }
            return functionReturnValue;
        }

        public int nStreamError()
        {
            return o_streamError.Count;
        }

        public Dictionary<string, List<string>> streamError()
        {
            return o_streamError;
        }

        public int nStreamWarn()
        {
            return o_streamWarn.Count;
        }

        public Dictionary<string, List<string>> streamWarn()
        {
            return o_streamWarn;
        }

        public int nBlockError()
        {
            return o_blockError.Count;
        }

        public Dictionary<string, List<string>> blockError()
        {
            return o_blockError;
        }

        public int nBlockWarn()
        {
            return o_blockWarn.Count;
        }

        public Dictionary<string, List<string>> blockWarn()
        {
            return o_blockWarn;
        }

        public int nConvError()
        {
            return o_convergenceError.Count;
        }

        public Dictionary<string, List<string>> convError()
        {
            return o_convergenceError;
        }

        //Convergence warning are now always errors, so convWarn now is dead
        public int nConvWarn()
        {
            return 0;
        }

        //Convergence warning are now always errors, so convWarn now is dead
        public Dictionary<string, List<string>> convWarn()
        {
            return new Dictionary<string, List<string>>();
        }

        public bool getHideDS(string dsName)
        {
            return Convert.ToBoolean(oaspen.Tree.FindNode("Data\\Design-Spec\\" + dsName).get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_ISHIDDEN));
        }
        public void setHideDS(string dsName, bool value)
        {
            oaspen.Tree.FindNode("Data\\Design-Spec\\" + dsName).set_AttributeValue((short)Happ.HAPAttributeNumber.HAP_ISHIDDEN, Type.Missing, Convert.ToInt16(value));
        }



        public override sinter.sinter_AppError runSim()
        {
            if (simulatorStatus != sinter_simulatorStatus.si_OPEN)
            {
                throw new ArgumentException("Simulator is not in Open status, cannon run!");
            }


            bool exp_report = false;
            sinter_AppError sCheck = sinter_AppError.si_OKAY;
            sinter_AppError bCheck = sinter_AppError.si_OKAY;
            sinter_AppError cCheck = sinter_AppError.si_OKAY;

            try
            {
                simulatorStatus = sinter_simulatorStatus.si_RUNNING;
                sendInputsToSim();

                runStatus = sinter_AppError.si_SIMULATION_NOT_RUN;

                //If the sim has already been canceled, don't run it. (There is a race here, 
                //I'm not sure what happens if AspenPlus gets the stop command before the run command. 
                //In otherwords, after this block, but before Run2.
                lock (o_terminateMonitor)
                {
                    if (o_stopSim)
                    {
                        o_simPaused = false;
                        o_stopSim = false;
                        o_stopTimedOut = false;
                        o_terminateMonitor.Reset();
                        runStatus = sinter_AppError.si_SIMULATION_STOPPED;
                        return (runStatus);
                    }
                    else
                    {
                        //Just make sure we can't accidentally have old flags still set.
                        o_simPaused = false;
                        o_stopTimedOut = false;
                    }
                }

                //Event handlers
                oaspen.OnCalculationCompleted += HandleOnCalculationCompleted;
                oaspen.OnCalculationStopped += HandleOnCalculationCompleted;
                o_stopTimer.Elapsed += HandleStopTimerElapsed;

                oaspen.Engine.Run2(true);  //Run asyncronously
                bool ended = false;
                while (!ended)
                {
                    lock (o_terminateMonitor)
                    {
                        if (o_stopSim)
                        { //Checking this first should allow success to win a race between the two
                            o_stopTimer.Interval = 120000; //60 seconds
                            o_stopTimer.Start();

                            oaspen.Engine.Stop();

                            o_simPaused = false;
                            o_stopSim = false;
                            ended = false;
                            o_runStatus = sinter_AppError.si_SIMULATION_STOPPED;
                        }
                        else if (o_simPaused)
                        {
                            o_simPaused = false;
                            o_stopSim = false;
                            ended = true;
                            break;
                        }
                        else if (o_stopTimedOut)
                        {
                            o_stopTimedOut = false;
                            if (runStatus == sinter_AppError.si_SIMULATION_STOPPED)
                            { //Check that signal was valid before proceeding
                                o_simPaused = false;
                                o_stopSim = false;
                                ended = true;
                                o_stopTimer.Stop();
                                o_terminateMonitor.Reset();
                                runStatus = sinter_AppError.si_STOP_FAILED;
                                return runStatus;  //Stopping failed, bail out immediately
                            }

                        }
                    }
                    o_terminateMonitor.WaitOne();  //Check status flags before waiting
                }


                o_terminateMonitor.Reset();
                o_stopTimer.Stop();

                //Remove event handle to avoid spurious events (Lots of actualys may cause this event to fire)
                oaspen.OnCalculationCompleted -= HandleOnCalculationCompleted;
                oaspen.OnCalculationStopped -= HandleOnCalculationCompleted;
                o_stopTimer.Elapsed -= HandleStopTimerElapsed;

                cCheck = convCheck();
                bCheck = blockCheck();
                sCheck = streamCheck();

                if (runStatus == sinter_AppError.si_SIMULATION_NOT_RUN)
                {
                    if ((cCheck == sinter_AppError.si_SIMULATION_ERROR |
                        bCheck == sinter_AppError.si_SIMULATION_ERROR |
                        sCheck == sinter_AppError.si_SIMULATION_ERROR))
                    {
                        runStatus = sinter_AppError.si_SIMULATION_ERROR;
                    }
                    else if ((cCheck == sinter_AppError.si_SIMULATION_WARNING |
                         bCheck == sinter_AppError.si_SIMULATION_WARNING |
                         sCheck == sinter_AppError.si_SIMULATION_WARNING))
                    {
                        runStatus = sinter_AppError.si_SIMULATION_WARNING;
                    }
                    else
                    {
                        runStatus = sinter_AppError.si_OKAY;
                    }
                }

                if ((exp_report == true))
                {
                    oaspen.Export(Happ.HAPEXPType.HAPEXP_REPORT, "report-" + runName);
                    oaspen.Export(Happ.HAPEXPType.HAPEXP_RUNMSG, "runmsg-" + runName);
                    oaspen.Export(Happ.HAPEXPType.HAPEXP_BACKUP, "bkp-file-" + runName); //This line causes another Calculation Completed event
                }


                return runStatus;
            } catch (Exception ex)
            {
                Debug.WriteLine("Exception Caught: ");
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine(ex.Message);
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                o_stopTimer.Stop();
                return runStatus;
            }
            finally
            {
                simulatorStatus = sinter_simulatorStatus.si_OPEN;
            }
        }


        public override sinter.sinter_AppError resetSim()
        {
            oaspen.Engine.Reinit(Happ.IAP_REINIT_TYPE.IAP_REINIT_SIMULATION);
            return sinter.sinter_AppError.si_OKAY;
        }

        public Happ.IHapp Aspen
        {
            get { return oaspen; }
        }
        public Happ.IHNode Tree
        {
            get { return otree; }
        }


        public override void sendValueToSim<ValueType>(string path, ValueType value)
        {
            this.Aspen.Tree.FindNode(path).Value = value;
        }

        //for vectors
        public override void sendValueToSim<ValueType>(string path, int ii, ValueType value)
        {
            this.Aspen.Tree.FindNode(path).Elements[ii].Value = value;
        }

        // Get the entire vector at once, not each element individually
        public override void sendVectorToSim<ValueType>(string path, ValueType[] value)
        {
            dynamic node = this.Aspen.Tree.FindNode(path);
            for (int ii = 0; ii < value.Length; ++ii)
            {
                node.Elements[ii].Value = value[ii];
            }

        }



        public override Object recvValueFromSimAsObject(string path)
        {
            Object retVal;
            try
            {
                retVal = this.Aspen.Tree.FindNode(path).Value;
                return retVal;
            }
            //Ignore Null Reference Exceptions, just pass back the default value
            catch (NullReferenceException)
            {
                throw new System.IO.IOException(String.Format("Variable {0} does not exist!", path));
            }

        }

        //For vectors Takes the actual indicies in the simulation! (so, 1 for an 1-indexed array for example)
        public override Object recvValueFromSimAsObject(string path, int ii)
        {
            Object retVal;
            try
            {
                retVal = this.Aspen.Tree.FindNode(path).Elements[ii].Value;
                return retVal;
            }
            //Ignore Null Reference Exceptions, just pass back the default value
            catch (NullReferenceException)
            {
                throw new System.IO.IOException(String.Format("Variable {0}[{1}] does not exist!", path, ii));
            }
        }

        public override ValueType recvValueFromSim<ValueType>(string path)
        {
            ValueType retVal;
            try
            {
                retVal = (ValueType)this.Aspen.Tree.FindNode(path).Value;
                return retVal;
            }
            //Ignore Null Reference Exceptions, just pass back the default value
            catch (NullReferenceException)
            {
                return default(ValueType);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                return default(ValueType);
            }
        }

        //For vectors
        /*        public override ValueType recvValueFromSim<ValueType>(string path, int ii)
                {
                    ValueType retVal;
                    try
                    {
                        retVal = (ValueType) this.Aspen.Tree.FindNode(path).Elements[ii].Value;
                        return retVal;
                    }
                    //Ignore Null Reference Exceptions, just pass back the default value
                    catch (NullReferenceException ex)
                    {
                        return default(ValueType);
                    }
                    catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
                    {
                        return default(ValueType);
                    }

                }
                */
        /**
         * Get the whole vector at once.
         */
        public override void recvVectorFromSim<ValueType>(string path, int[] indicies, ValueType[] value)
        {  //for vectors
            int len = value.Length;
            dynamic node = this.Aspen.Tree.FindNode(path);

            if (node == null || node.Value == null)
            {
                for (int ii = 0; ii < len; ++ii)
                {
                    value[ii] = default(ValueType);
                }

            }
            else
            {
                for (int ii = 0; ii < len; ++ii)
                {
                    if (node.Elements[ii] == null || node.Elements[ii].Value == null)
                    {
                        value[ii] = default(ValueType);
                    }
                    else
                    {
                        value[ii] = (ValueType)node.Elements[ii].Value;
                    }
                }
            }
        }


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


        public override string getCurrentUnits(string path)
        {
            string retVal;
            try
            {
                string aspenUnitString = this.Aspen.Tree.FindNode(path).UnitString;
                retVal = convertAspenUnitsToStandard(aspenUnitString);
                return retVal;
            }
            //Ignore Null Reference Exceptions, just pass back the default value
            catch (NullReferenceException)
            {
                return "";
            }
        }

        public override string getCurrentDescription(string path)
        {
            string retVal;
            try
            {
                retVal = this.Aspen.Tree.FindNode(path).get_AttributeValue((short)Happ.HAPAttributeNumber.HAP_PROMPT);
                return retVal;
            }
            //Ignore Null Reference Exceptions, just pass back the default value
            catch (NullReferenceException)
            {
                return "";
            }
        }

        public override string getCurrentName(string path)
        {
            string retVal;
            try
            {
                retVal = this.Aspen.Tree.FindNode(path).Name;
                return retVal;
            }
            //Ignore Null Reference Exceptions, just pass back the default value
            catch (NullReferenceException)
            {
                return "";
            }
        }


        public override string getCurrentUnits(string path, int[] indicies)
        {
            string retVal;
            try
            {
                string aspenUnitString = this.Aspen.Tree.FindNode(path).Elements[0].UnitString;
                retVal = convertAspenUnitsToStandard(aspenUnitString);
                return retVal;
            }
            //Ignore Null Reference Exceptions, just pass back the default value
            catch (NullReferenceException)
            {
                return "";
            }
        }

        public void getStreamNames()
        {

            Happ.IHNode parent = oaspen.Tree.Elements["Data"].Elements["Streams"];
            Happ.IHNodeCol children = parent.Elements;
            o_streamNames.Clear();
            foreach (Happ.IHNode child in children)
            {
                o_streamNames.Add(child.Name);
            }
        }

        public int StreamCount
        {
            get { return o_streamNames.Count; }
        }

        public string getStream(int i)
        {
            return o_streamNames[i];
        }

        public void getBlockNames()
        {

            Happ.IHNode parent = oaspen.Tree.Elements["Data"].Elements["Blocks"];
            Happ.IHNodeCol children = parent.Elements;
            o_blockNames.Clear();
            foreach (Happ.IHNode child in children)
            {
                o_blockNames.Add(child.Name);
            }
        }

        public int BlockCount
        {
            get { return o_blockNames.Count; }
        }

        public string getBlock(int i)
        {
            return o_blockNames[i];
        }

        private String errorToString(Dictionary<String, List<String>> errorDict, string objectType, string errorType)
        {
            StringWriter outStream = new StringWriter();
            String retString;
            try
            {
                foreach (KeyValuePair<String, List<String>> entry in errorDict)
                {
                    outStream.WriteLine("{0} {1} has {2}:", objectType, entry.Key, errorType);
                    int count = 0;
                    foreach (String errormsg in entry.Value)
                    {
                        count++;
                        outStream.WriteLine("{0}: {1}", count, errormsg);
                    }
                }
                retString = outStream.ToString();
            }
            finally
            {
                outStream.Close();
            }
            return retString;
        }

        public override string[] errorsBasic()
        {
            List<string> errors = new List<string>();
            if (runStatus == sinter_AppError.si_SIMULATION_ERROR)
            {
                if (nConvError() > 0)
                {
                    errors.Add(errorToString(convError(), "Convergence Block", "Errors"));
                }
                else if (nBlockError() > 0)
                {
                    errors.Add(errorToString(blockError(), "Block", "Errors"));
                }
                else if (nStreamError() > 0)
                {
                    errors.Add(errorToString(streamError(), "Stream", "Errors"));
                }
            }
            return errors.ToArray();
        }

        public override string[] warningsBasic()
        {
            List<string> errors = new List<string>();
            if (runStatus == sinter_AppError.si_SIMULATION_WARNING)
            {
                if (nConvWarn() > 0)
                {
                    errors.Add(errorToString(convWarn(), "Convergence Block", "Warnings"));
                }
                else if (nBlockWarn() > 0)
                {
                    errors.Add(errorToString(blockWarn(), "Block", "Warnings"));
                }
                else if (nStreamWarn() > 0)
                {
                    errors.Add(errorToString(streamWarn(), "Stream", "Warnings"));
                }
            }
            return errors.ToArray();
        }

        /// <summary>
        /// terminate is called by monitor thread to kill underlying COM Server,
        /// method is called when hanging is detected.
        /// </summary>
        public override bool terminate()
        {
            Debug.WriteLine(String.Format("terminate processID '{0}'", processID), GetType().Name);
            if (processID > 0)
            {
                Process p = Process.GetProcessById(processID);
                if (p == null)
                {
                    simulatorStatus = sinter_simulatorStatus.si_CLOSED;
                    return true;
                }

                Debug.WriteLine(String.Format("kill processID '{0}'", processID), GetType().Name);
                p.Kill();
                bool lockTaken = false;
                int timeout = 1000 * 60 * 2;
                try
                {
                    System.Threading.Monitor.TryEnter(this, timeout, ref lockTaken);
                    if (!lockTaken) throw new TimeoutException("Lock not acquired, Timeout exceeded on terminate");
                    oaspen = null;
                    processID = -1;
                }
                finally
                {
                    if (lockTaken) System.Threading.Monitor.Exit(this);
                }
                simulatorStatus = sinter_simulatorStatus.si_CLOSED;
                return true;
            }
            else
            {
                simulatorStatus = sinter_simulatorStatus.si_CLOSED;
                return false;
            }
        }

        /** We're not sure how to do Heat Intergration Variables in Aspen+ yet */
        public override IList<sinter_IVariable> getHeatIntegrationVariables()
        {
            return new List<sinter_IVariable>();
        }

        /** One day we'll have an example of an AspenPlus file with vectors. :| */
        public override int guessVectorSize(string path)
        {
            throw new NotImplementedException();
        }

        //AFAIK AspenPlus doesn't have any special indexing schemes
        public override int[] getVectorIndicies(string path, int size)
        {
            int[] retval = new int[size];
            for (int ii = 0; ii < size; ++ii)
            {
                retval[ii] = ii;
            }
            return retval;
        }


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
