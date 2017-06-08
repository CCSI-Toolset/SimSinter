using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marshal = System.Runtime.InteropServices.Marshal;
using Excel = Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Diagnostics;
using VariableTree;
using Newtonsoft.Json.Linq;

namespace sinter
{
    /// <summary>
    /// ExcelSetupFile just a hack to get around calling everything aspenfile
    /// </summary>
 //   public class ExcelSetupFile : sinter.sinter_JsonSetupFile
//    {
//        override public int parseFile(JObject setupObject)
//        {
 //           String spreadsheet = (String)setupObject["spreadsheet"];
//            Debug.WriteLine(String.Format("Parse Model: {0}", spreadsheet), this.GetType().Name);
//            int val = base.parseFile(setupObject);
//            o_aspenFilename = spreadsheet;
 //           return val;
 //       }
 //   }

    public class sinter_SimExcel  : sinter_InteractiveSim
    {
        Excel.Application o_xlApp = null;
        Excel.Workbook o_xlWorkbook = null;
        string o_macroName;

        //Boolean variables for controlling what is visible from the simulation.
        //Should match what's in the simulation, these only exist to allow 
        //the options to be set when the simulation itself is not availible.
        private bool o_visible = false;
        private bool o_dialogSuppress = true;

        // Dim o_acm As Object
        public sinter_SimExcel()
        {
            o_macroName = "";
            o_availibleSettings = new Dictionary<string, Tuple<set_setting, get_setting, sinter_Variable.sinter_IOType>>
            {
                {"macro", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_macroName, getobj_macroName, sinter_Variable.sinter_IOType.si_STRING)}
            };
//            this.setupFile = new JsonSetupFile();
            simName = "Excel";

        }

        /// <summary>
        /// Every sinter has a configuration file, and this variable stores this sinter's configuration
        /// file information.
        /// </summary>

        public string macroName
        {
            get
            {
                return o_macroName;
            }
            set
            {
                o_macroName = value;
            }
        }

        public void setobj_macroName(object value)
        {
            macroName = Convert.ToString(value);
        }

        public object getobj_macroName()
        {
            return macroName;
        }

        public override string setupfileKey { get { return "spreadsheet"; } }

        public Excel.Application ExcelApp
        {
            get
            {
                return o_xlApp;
            }
        }

        public Excel.Workbook ExcelWorkbook
        {
            get
            {
                return o_xlWorkbook;
            }
        }

        public override char pathSeperator
        {
            get
            {
                return '$';
            }
        }

        void swap<ValueType>(IList<ValueType> thisList, int aa, int bb) {
            ValueType tmp = thisList[aa];
            thisList[aa] = thisList[bb];
            thisList[bb] = tmp;
        }

        /**
         * This splits an address into an array of strings sutiable for stepping down the DataTree.
         * It handles both Col$Row and Row$Col order
         * It handles 8 cases correctly:
         * Sheet$Col$Row will return a 3 element array Sheet,Row,Col
         * $Sheet$Col$Row will return a 3 element array Sheet,Row,Col
         * Col$Row will return a 2 element array Row,Col
         * $Col$Row will return a 2 element array Sheet,Row,Col
         * Sheet$Row$Col will return a 3 element array Sheet,Row,Col
         * $Sheet$Row$Col will return a 3 element array Sheet,Row,Col
         * Row$Col will return a 2 element array Row,Col
         * $Row$Col will return a 2 element array Sheet,Row,Col
         * **/
        public override IList<string> parsePath(String address)
        {
            if (address.Length == 0)
            {
                return new List<String>();
            }
            //Usually the absolute address will start with a $, like $C$7, that's nice, but we don't want it.
            if (address.Length > 0 && address[0] == pathSeperator)
            {
                address = address.Remove(0, 1);
            }

            if (address.Length == 0)
            {
                return new List<String>();
            }

            
            List<String> splitAddress = address.Split(pathSeperator).ToList<string>();
            int len = splitAddress.Count;

            //The data tree will throw a '*' in the column name to represent a whole row.  
            //Just blow that star away of we're parsing.
            if (len > 1 && splitAddress[len - 2][0] == '*')
            {
                splitAddress.RemoveAt(len - 2);
            }

            //When stepping down the tree, we want row,column order.  The Excel path is in column,row order.  So Swap.
            if (len > 1 && Char.IsLetter(splitAddress[len-2][0]))
            {
                swap<string>(splitAddress, len - 2, len - 1);
            }

            return splitAddress;
        }


        /** 
         * This combines an array of strings into a path.
         * As usual, the trouble with Excel is that the row and column addresses are flipped.
         **/
        public override string combinePath(IList<string> splitPath)
        {
            int len = splitPath.Count;
            swap<string>(splitPath, len - 2, len - 1);

            string path = splitPath[0];
            for (int ii = 1; ii < splitPath.Count; ++ii)
            {
                path += pathSeperator + splitPath[ii];
            }
            swap<string>(splitPath, len - 2, len - 1);  //Swap them back when we're done.  Dumb idea?
            return path;
        }

        private string resolveVectorPath(string path, int ii)
        {
            IList<string> splitAddress = parsePath(path);
            int len = splitAddress.Count;
            int columnNumber = NumberFromExcelColumn(splitAddress[len - 1]);
            columnNumber += ii;
            splitAddress[len - 1] = ExcelColumnFromNumber(columnNumber);

            return combinePath(splitAddress);

        }


        /**
         * Converts an integer into an Excel Column name/address.  1=A 26=Z 27=AA....
         * Thanks to astander on StackOverflow.com for this code.
         **/
        public string ExcelColumnFromNumber(int column)
        {
            string columnString = "";
            decimal columnNumber = column;
            while (columnNumber > 0)
            {
                decimal currentLetterNumber = (columnNumber - 1) % 26;
                char currentLetter = (char)(currentLetterNumber + 65);
                columnString = currentLetter + columnString;
                columnNumber = (columnNumber - (currentLetterNumber + 1)) / 26;
            }
            return columnString;
        }

        /**
         * Converts an Excel Column name/address in an integer.  A=1 Z=26 AA=27....
         * Thanks to astander on StackOverflow.com for this code.
         **/
        public int NumberFromExcelColumn(string column)
        {
            int retVal = 0;
            string col = column.ToUpper();
            for (int iChar = col.Length - 1; iChar >= 0; iChar--)
            {
                char colPiece = col[iChar];
                int colNum = colPiece - 64;
                retVal = retVal + colNum * (int)Math.Pow(26, col.Length - (iChar + 1));
            }
            return retVal;
        }



        /** 
         * void makeDataTree
         * 
         * This function generates a tree based on the variables availible in the simulation.  All input and
         * output variables.  This is used primarily for the Sinter Config GUI.
         *
         * Excel data isn't really organized in a tree, so this function is a little weird 
         * The most annoying piece of the process is that I want the Tree to go Sheet->Row->Col
         * but Excel goes Sheet$Col$Row  ie, column major order.  Thanks Excel.
         * * */
        public override void makeDataTree()
        {
            o_dataTree = new VariableTree.VariableTree(parsePath, pathSeperator);
            VariableTreeNode rootNode = new VariableTreeNode("root", "", pathSeperator);
            Excel.Sheets xlWorksheets = (Excel.Sheets)o_xlWorkbook.Worksheets;

            foreach (Excel.Worksheet xlWorksheet in xlWorksheets)
            {
                String sheetName = xlWorksheet.Name;
                VariableTreeNode sheetNode = new VariableTreeNode(sheetName, sheetName, pathSeperator);

                Excel.Range xlRange = xlWorksheet.UsedRange;
                Excel.Range rows = xlRange.Rows;
                Excel.Range cols = xlRange.Columns;

                //Calculate the rows and columns locally, because having Excel do it takes a LONG time.
                String rowStartS = parsePath(((Excel.Range)xlRange.Cells[1, 1]).Address)[0];
                int rowStartI = Convert.ToInt32(rowStartS);
                String colStartS = parsePath(((Excel.Range)xlRange.Cells[1, 1]).Address)[1];
                int colStartI = NumberFromExcelColumn(colStartS);

                for (int rr = 0; rr < rows.Count; ++rr)
                {
                    String rowName = Convert.ToString(rowStartI + rr);
                    String rowPath = String.Format("{0}$*${1}", sheetName, rowName);
                    VariableTreeNode rowNode = new VariableTreeNode(rowName, rowPath, pathSeperator);
                    sheetNode.addChild(rowNode);

                    for (int cc = 0; cc < cols.Count; ++cc)
                    {
                        String colName = ExcelColumnFromNumber(colStartI + cc);
                        String colPath = String.Format("{0}${1}${2}", sheetName, colName, rowName);
                        VariableTreeNode colNode = new VariableTreeNode(colName, colPath, pathSeperator);
                        rowNode.addChild(colNode);
                    }
                }
                rootNode.addChild(sheetNode);
            }
            
            o_dataTree.rootNode = rootNode;

        //Remove the Dummy Children (those are only required when doing incremental tree building
            rootNode.traverse(rootNode, thisNode =>
            {
                if (thisNode.o_children.ContainsKey("DummyChild"))
                {
                    thisNode.o_children.Remove("DummyChild");
                }
            });

        }

        public override void startDataTree()
        {
            makeDataTree();
        }


        public override VariableTree.VariableTreeNode findDataTreeNode(IList<String> pathArray)
        {
            return o_dataTree.rootNode.resolveNode(pathArray);
        }


        public override sinter.sinter_AppError runSim()
        {
            if (simulatorStatus != sinter_simulatorStatus.si_OPEN)
            {
                throw new ArgumentException("Simulator is not in Open status, cannon run!");
            }

            //We don't want to run a macro if there isn't one.
            if (macroName.Length > 0)
            {
                try
                {
                    simulatorStatus = sinter_simulatorStatus.si_RUNNING;
                    o_xlApp.Run(macroName);
                    simulatorStatus = sinter_simulatorStatus.si_OPEN;
                }
                catch (Exception ex)
                {
                    simulatorStatus = sinter_simulatorStatus.si_ERROR;
                    runStatus = sinter_AppError.si_SIMULATION_ERROR;
                    throw new System.IO.IOException(ex.Message);
                    //throw new System.IO.IOException("Excel run failed, cause unknown.");
                }
            }
            
            recvOutputsFromSim();
            

            //Not sure how to get the run status back, so for now, it's a success if no exception is thrown.
            runStatus = sinter_AppError.si_OKAY;
            return sinter_AppError.si_OKAY;
        }

        public override sinter.sinter_AppError resetSim()
        {
            // Reset sim by reopening the saved version
            closeDocument();
            openDocument();
            return sinter_AppError.si_OKAY;
        }

        private void closeDocument()
        {
            Debug.WriteLine("o_xlWorkbook.closeDocument", GetType().Name);
            o_xlWorkbook.Close(false, Type.Missing, Type.Missing);
            Debug.WriteLine("Marshal.ReleaseComObject: o_xlWorkbook", GetType().Name);
            Marshal.ReleaseComObject(o_xlWorkbook);
            o_xlWorkbook = null;
        }

        private void openDocument()
        {
            Debug.WriteLine("openDocument", GetType().Name);
            var fname = System.IO.Path.Combine(workingDir, simFile);
            //Excel works better with an absolute path.
            string speadsheet = System.IO.Path.GetFullPath(fname);

            Excel.Workbooks workbooks = o_xlApp.Workbooks;
            Debug.WriteLine("workbooks.Open", GetType().Name);
            o_xlWorkbook = workbooks.Open(speadsheet, 0, true, Type.Missing, Type.Missing, Type.Missing, true, Type.Missing, Type.Missing, false, false, 0, true, 1, 0);
            Debug.WriteLine("Marshal.ReleaseComObject: workbooks", GetType().Name);
            Marshal.ReleaseComObject(workbooks);
        }

        public override void openSim()
        {
            if (simulatorStatus != sinter_simulatorStatus.si_CLOSED)
            {
                return; //Nothing to do.
            }
            simulatorStatus = sinter_simulatorStatus.si_INITIALIZING;

            if (o_xlApp != null)
            {
                o_xlApp = null;
            }

            o_xlApp = new Excel.Application();
            Vis = o_visible;  //Enforce visibility and dialogsuppression ASAP.
            dialogSuppress = o_dialogSuppress;

            simVersion = o_xlApp.Version.Trim();

            if ((o_xlApp == null))
            {
                simulatorStatus = sinter_simulatorStatus.si_ERROR;
                throw new System.IO.IOException("Could not open Excel.Application");
            }

            try
            {
                openDocument();
            }
            finally
            {
                simulatorStatus = sinter_simulatorStatus.si_OPEN;
                checkSimVersionConstraints();
            }
        }
        [DllImport("user32.dll")]     
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId); 
        
        /**
         * killExcel hard kills Excel.  It should only attempt this if Excel is really still open 
         **/
        private bool killExcel()
        {
            try
            {
                int hWnd = o_xlApp.Application.Hwnd;
                uint processID;
                GetWindowThreadProcessId((IntPtr)hWnd, out processID);
                if (processID != 0)  //If Excel is closed now, processID will be 0, and the kill unnecessary
                    Process.GetProcessById((int)processID).Kill();
                return true;
            }
            finally
            {
                simulatorStatus = sinter_simulatorStatus.si_CLOSED;
            }
        }

        public override void closeSim()
        {
            if (simulatorStatus == sinter_simulatorStatus.si_OPEN)
            {

                try
                {
                    closeDocument();
                    o_xlApp.Quit();

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    killExcel();  //If it's still open after quit, kill it dead.

                    Marshal.FinalReleaseComObject(o_xlApp);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    o_xlApp = null;
                }
                catch (Exception ex)
                {
                    simulatorStatus = sinter_simulatorStatus.si_ERROR;
                    throw ex;
                }
                simulatorStatus = sinter_simulatorStatus.si_CLOSED;
            }
        }
        public override void stopSim()
        {
            throw new System.NotImplementedException("Excel cannot currently stop a simulation");
            //oaspen.Engine.Stop();
            //return sinter.sinter_AppError.si_OKAY;
        }

        public override string[] errorsBasic()
        {
            //NOT IMPLEMENTED
            return new string[0];
        }

        public override string[] warningsBasic()
        {
            //NOT IMPLEMENTED
            return new string[0];
        }


        public override void sendValueToSim<ValueType>(string path, ValueType value)
        {
            try
            {
                string worksheetName;
                string cellName;
                string[] addressSplit = path.Split(new char[] { pathSeperator }, 2);
                if (addressSplit.Length != 2)
                {
                    throw new System.IO.IOException(String.Format("Mal-formed Excel address string {0}", path));
                }
                worksheetName = addressSplit[0];
                cellName = addressSplit[1];

                Excel.Sheets xlWorksheets = (Excel.Sheets) o_xlWorkbook.Worksheets;
                Excel.Worksheet xlWorksheet = (Excel.Worksheet) xlWorksheets[worksheetName];
                Excel.Range xlRange = xlWorksheet.get_Range(cellName);
                xlRange.Value = value;
                Marshal.ReleaseComObject(xlRange);
                Marshal.ReleaseComObject(xlWorksheet);
                Marshal.ReleaseComObject(xlWorksheets);

            }
            catch (Exception)
            {
                throw new System.IO.IOException("Could not set " + path + " to " + Convert.ToString(value) + ".");
            }
        }


        public override void sendValueToSim<ValueType>(string path, int ii, ValueType value)
        {
            try
            {
                string worksheetName;
                string cellName;
                string[] addressSplit = path.Split(new char[] { pathSeperator }, 2);
                if (addressSplit.Length != 2)
                {
                    throw new System.IO.IOException(String.Format("Mal-formed Excel address string {0}", path));
                }
                worksheetName = addressSplit[0];
                cellName = addressSplit[1];

                //The address is the base of the vector, add ii to the int component to get the individual address.
                string cellLetter = Regex.Match(cellName, "[a-zA-Z]+").ToString();
                string cellNumber = Regex.Match(cellName, "[0-9]+").ToString();
                int cellNumberInt = Convert.ToInt32(cellNumber);

                cellNumberInt += ii;

                string newCellName = string.Format("${0}${1}", cellLetter, cellNumberInt);

                Excel.Sheets xlWorksheets = (Excel.Sheets)o_xlWorkbook.Worksheets;
                Excel.Worksheet xlWorksheet = (Excel.Worksheet)xlWorksheets[worksheetName];
                Excel.Range xlRange = xlWorksheet.get_Range(newCellName);
                xlRange.Value = value;
                Marshal.ReleaseComObject(xlRange);
                Marshal.ReleaseComObject(xlWorksheet);
                Marshal.ReleaseComObject(xlWorksheets);
            }
            catch (Exception)
            {
                throw new System.IO.IOException("Could not set " + path + " to " + Convert.ToString(value) + ".");
            }

        }
        public override void sendVectorToSim<ValueType>(string path, ValueType[] value)
        {
            //Need to upgrade to 2D array for Excel
            object[,] sendValueArray = (Object[,])new Object[1, value.Length];
            for (int ii = 0; ii < value.Length; ++ii)
            {
                sendValueArray[0, ii] = (object)value[ii];
            }

            try
            {
                string worksheetName;
                string cellName;
                string[] addressSplit = path.Split(new char[] { pathSeperator }, 2);
                if (addressSplit.Length != 2)
                {
                    throw new System.IO.IOException(String.Format("Mal-formed Excel address string {0}", path));
                }
                worksheetName = addressSplit[0];
                cellName = addressSplit[1];

                Excel.Sheets xlWorksheets = (Excel.Sheets)o_xlWorkbook.Worksheets;
                Excel.Worksheet xlWorksheet = (Excel.Worksheet)xlWorksheets[worksheetName];
                Excel.Range xlRange = xlWorksheet.get_Range(cellName, resolveVectorPath(cellName, value.Length - 1));
                xlRange.Value = sendValueArray;

                Marshal.ReleaseComObject(xlRange);
                Marshal.ReleaseComObject(xlWorksheet);
                Marshal.ReleaseComObject(xlWorksheets);

            }
            catch
            {
                throw new System.IO.IOException("Could not set " + path + " to " + Convert.ToString(value) + ".");
            }


        }

        private object recvValueFromSimInternal(string path)
        {
            if(path == null || path.Contains("*")) {  //Paths that contain "*" are incomplete
                return null;
            }
            try
            {
                string worksheetName;
                string cellName;
                string[] addressSplit = path.Split(new char[] { pathSeperator }, 2);
                Object retVal;
                if (addressSplit.Length != 2)
                {
                    return null;  //Incomplete path
                }
                worksheetName = addressSplit[0];
                cellName = addressSplit[1];

                Excel.Sheets xlWorksheets = (Excel.Sheets)o_xlWorkbook.Worksheets;
                Excel.Worksheet xlWorksheet = (Excel.Worksheet)xlWorksheets[worksheetName];
                Excel.Range xlRange = xlWorksheet.get_Range(cellName);
                retVal = xlRange.Value;
                if (retVal is decimal) //Sinter doesn't really support decimal type, make do with double
                {
                    retVal = Convert.ToDouble(retVal);  
                }
                Marshal.ReleaseComObject(xlRange);
                Marshal.ReleaseComObject(xlWorksheet);
                Marshal.ReleaseComObject(xlWorksheets);
                return retVal;
            }
            catch 
            {
                throw new System.IO.IOException("Could not get value from " + path);
            }

        }

    
        public override ValueType recvValueFromSim<ValueType>(string path)
        {
           return (ValueType) recvValueFromSimAsObject(path);
        }

        //For vectors
/*        public override ValueType recvValueFromSim<ValueType>(string path, int ii)
        {
//            try
//            {
                return (ValueType)recvValueFromSimAsObject(path, ii);
        }
        */
        public override Object recvValueFromSimAsObject(string path)
        {
            return recvValueFromSimInternal(path);
        }

        //For vectors Takes the actual indicies in the simulation! (so, 1 for an 1-indexed array for example)
        public override Object recvValueFromSimAsObject(string path, int ii)
        {
            string resolvedPath = resolveVectorPath(path, ii);
            return recvValueFromSimInternal(resolvedPath);
        }
        

        public override void recvVectorFromSim<ValueType>(string path, int[] indicies, ValueType[] value)
        {

            try
            {
                string worksheetName;
                string cellName;
                string[] addressSplit = path.Split(new char[] { pathSeperator }, 2);
                if (addressSplit.Length != 2)
                {
                    throw new System.IO.IOException(String.Format("Mal-formed Excel address string {0}", path));
                }
                worksheetName = addressSplit[0];
                cellName = addressSplit[1];

                Excel.Sheets xlWorksheets = (Excel.Sheets)o_xlWorkbook.Worksheets;
                Excel.Worksheet xlWorksheet = (Excel.Worksheet)xlWorksheets[worksheetName];
                Excel.Range xlRange = xlWorksheet.get_Range(cellName, resolveVectorPath(cellName, value.Length-1)); //Grab the whole range

                object[,] excelArray = (object[,])xlRange.get_Value(Excel.XlRangeValueDataType.xlRangeValueDefault);

                for (int ii = 0; ii < value.Length; ++ii)
                {
                    value[ii] = (ValueType)excelArray[1, ii+1];
                }

                Marshal.ReleaseComObject(xlRange);
                Marshal.ReleaseComObject(xlWorksheet);
                Marshal.ReleaseComObject(xlWorksheets);
            }
            catch
            {
                throw new System.IO.IOException("Could not get value from " + path);
            }

        } 

        public override string getCurrentUnits(string path)
        {
            //Excel doesn't have any concept of units
           return null; 
        }

        //Vector version
        public override string getCurrentUnits(string path, int[] indicies)
        {
            //Excel doesn't have any concept of units
            return null; 
        }

        public override String getCurrentDescription(String path)
        {
            return null;
        }

        public override string getCurrentName(string path)
        {
            return path;  //Excel paths are short enough to use as name guesses
        }

        public override bool Vis
        {
            get
            {
                if (o_xlApp == null)
                {
                    return o_visible;
                }
                else
                {
                    o_visible = o_xlApp.Visible;
                    return o_xlApp.Visible;
                }
            }
            set
            {
                if (o_xlApp == null)
                {
                    o_visible = value;
                }
                else
                {
                    o_xlApp.Visible = value;
                    o_visible = o_xlApp.Visible;
                }
            }
        }

        //<Summary>
        // Suppress Aspen Dialog Boxes (Helps keep Aspen invisible)
        //</Summary>

        public override bool dialogSuppress
        {
            //In Aspen you suppressDialogs, in Excel you turn off Display Alerts which is the opposite
            get
            {
                if (o_xlApp == null)
                {
                    return o_dialogSuppress;
                }
                else
                {
                    o_dialogSuppress = !o_xlApp.DisplayAlerts;
                    return !o_xlApp.DisplayAlerts;
                }
            }
            set
            {
                if (o_xlApp == null)
                {
                    o_dialogSuppress = value;
                }
                else
                {
                    o_xlApp.DisplayAlerts = !value;
                    o_dialogSuppress = !o_xlApp.DisplayAlerts;
                }
            }
        }

        public override bool terminate()
        {
            bool retval = false;
            retval = killExcel();
            o_xlApp = null;
            return retval;
        }

        /** Excel has no notion of Heat Intergration Variables... */
        public override IList<sinter_IVariable> getHeatIntegrationVariables()
        {
            return new List<sinter_IVariable>();
        }

        /** Honestly, there isn't any way to do this in Excel :| */
        public override int guessVectorSize(string path)
        {
            throw new NotImplementedException();
        }

        //Excel doesn't have any special indexing schemes
        public override int[] getVectorIndicies(string path, int size)
        {
            int[] retval = new int[size];
            for (int ii = 0; ii < size; ++ii)
            {
                retval[ii] = ii;
            }
            return retval;
        }

        /** Returns the user known version name when passed in the internal version name.
         * For example, For execl 14.0 returns "2010"
         * Simulator specific, of course.
         * If the version number cannot be converted, the empty string is returned.
         **/
        public override string internal2externalVersion(string internalVersion)
        {
            if (internalVersion == "12.0")
            {
                return "2007";
            }
            if (internalVersion == "16.0")
            {
                return "2016";
            }
            if (internalVersion == "15.0")
            {
                return "2013";
            }
            if (internalVersion == "14.0")
            {
                return "2010";
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
            if (externalVersion == "2016")
            {
                return "16.0";
            }
            if (externalVersion == "2013")
            {
                return "15.0";
            }
            if (externalVersion == "2010")
            {
                return "14.0";
            }
            if (externalVersion == "2007")
            {
                return "12.0";
            }
            return externalVersion;
        }

        /** Returns a full list of all the external version names known at time of writing.
         * If the found version is greater than these, then it will be referred to using the internal version
         * name in the UI **/
        public override string[] externalVersionList()
        {
            string[] versions = { "2007", "2010", "2013", "2016" };
            return versions;
        }


    }
}
