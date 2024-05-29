using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sinter
{
    public abstract class sinter_SetupFile
    {

        //The collection of all the pure variables objects in the setupFile
        private Microsoft.VisualBasic.Collection o_Variables = new Microsoft.VisualBasic.Collection();

        //The collection of all the dynamic variables objects in the setupFile
        private Microsoft.VisualBasic.Collection o_DynamicVariables = new Microsoft.VisualBasic.Collection();

        //The collection of all the settings objects in the setupFile
        private Microsoft.VisualBasic.Collection o_Settings = new Microsoft.VisualBasic.Collection();

        //The collection of settings and variables (no tables)
        private Microsoft.VisualBasic.Collection o_VariablesAndSettings = new Microsoft.VisualBasic.Collection();

        //The collection of all the Table objects in the setupFile
        private Microsoft.VisualBasic.Collection o_Tables = new Microsoft.VisualBasic.Collection();

        //The collection of _all_ the IVariable objects in the setupFile, settings, tables, and variables
        private Microsoft.VisualBasic.Collection o_AllIVariable = new Microsoft.VisualBasic.Collection();

        //setup file version string
        protected Version o_configFileVersion = new Version(0, 0);  //Start out with version 0.0, just as a sentinal value

        //simulation file to run
        protected string o_aspenFilename;
        protected string o_aspenFileHash; //The SHA1 for the simulation.
        protected string o_aspenFileHashAlgo; //The SHA1 for the simulation.

        //Required input files other than the simulation file.  (DLLs, snapshots, etc.)
        protected List<String> o_additionalFiles = new List<String>();
        protected List<String> o_additionalFilesHash = new List<String>(); //The SHA1s for the additional files
        protected List<String> o_additionalFilesHashAlgo = new List<String>(); //The SHA1s for the additional files

        protected string o_simNameRecommendation = "";
        protected string o_simVersionRecommendation = ""; //The sim version required by the contraint in the setupfile
        protected sinter_versionConstraint o_simVersionConstraint = sinter_versionConstraint.version_ATLEAST;  //The constraint on how sim versions are handled, but the setupfile. Default is AT-LEAST

        //The file that contains the data for configuring the simulation.  For most simulators this is the same as o_aspenFilename, but
        //the authors of the simulation
        protected string o_author;
        //the revision date of the sim
        protected string o_dateString;
        //the title of the simulation
        protected string o_title;
        //a descriptio of the simulation
        protected string o_simDesc;

        public void addVariable(sinter_IVariable var)
        {
            o_AllIVariable.Add(var, var.name);
            if (var.isTable)
            {
                o_Tables.Add(var, var.name);
                return;
            }

            sinter_Variable vVar = (sinter_Variable) var;            
            o_VariablesAndSettings.Add(vVar, var.name);

            if (vVar.isDynamicVariable)
            {
                o_DynamicVariables.Add(vVar, vVar.name);
            }

            if (vVar.isSetting)
            {
                o_Settings.Add(vVar, var.name);
            }
            else
            {
                o_Variables.Add(vVar, vVar.name); //o_Variables doesn't include settings or tables
            }
        }

        //The below adds are a bit silly, addVariable does everything correctly already
        public void addDynamicVariable(sinter_Variable var)
        {
            o_AllIVariable.Add(var, var.name);
            o_Variables.Add(var, var.name);
            o_VariablesAndSettings.Add(var, var.name);
            o_DynamicVariables.Add(var, var.name);
        }


        public void addSetting(sinter_Variable var)
        {
            o_AllIVariable.Add(var, var.name);
            o_Settings.Add(var, var.name);
            o_VariablesAndSettings.Add(var, var.name);
        }

        public void addTable(sinter_Table var)
        {
            o_AllIVariable.Add(var, var.name);
            o_Tables.Add(var, var.name);
        }

        //Clears all varaibles, not tables 
        public void clearVariablesAndSettings()
        {
            o_Variables.Clear();
            o_DynamicVariables.Clear();
            o_VariablesAndSettings.Clear();
            o_AllIVariable.Clear();
            o_Settings.Clear();
            //Add the tables back in.
            foreach (sinter_Table table in o_Tables)
            {
                o_AllIVariable.Add(table);
            }

        }


        //Clears all varaibles, including tables 
        public void clearAllVariables()
        {
            o_Variables.Clear();
            o_DynamicVariables.Clear();
            o_VariablesAndSettings.Clear();
            o_AllIVariable.Clear();
            o_Settings.Clear();
            o_Tables.Clear();

        }

        public Microsoft.VisualBasic.Collection AllIO
        {
            get
            {
                return o_AllIVariable;
            }
        }

        public Microsoft.VisualBasic.Collection Variables
        {
            get
            {
                return o_Variables;
            }
        }

        public Microsoft.VisualBasic.Collection DynamicVariables
        {
            get
            {
                return o_DynamicVariables;
            }
        }

        public Microsoft.VisualBasic.Collection Settings
        {
            get
            {
                return o_Settings;
            }
        }

        public Microsoft.VisualBasic.Collection Tables
        {
            get
            {
                return o_Tables;
            }
        }

        public sinter_Variable getAsVariableByIndex(int i)
        {
            return (sinter_Variable)getIOByIndex(i);
        }
        public sinter_Vector getAsVectorByIndex(int i)
        {
            return (sinter_Vector)getIOByIndex(i);
        }

        public sinter_IVariable getVariableByName(string name)
        {
            try
            {
                if (o_Variables.Contains(name))
                {
                    return (sinter_IVariable)o_Variables[name];
                }
                else
                {
                    return (sinter_IVariable)o_Settings[name];
                }
            }
            catch 
            {
                return null;
            }
        }

        public sinter_IVariable getIOByName(string name)
        {
            try
            {
                return (sinter_IVariable)o_AllIVariable[name];
            }
            catch
            {
                return null;
            }
        }

        public sinter_IVariable getIOByIndex(int i)
        {
            return (sinter_IVariable)o_VariablesAndSettings[i];
        }
        public int countIO
        {
            get { return o_VariablesAndSettings.Count; }
        }

        public sinter_Variable getSettingByName(string name)
        {
            try
            {
                return (sinter_Variable)o_Settings[name];
            }
            catch
            {
                return null;
            }
        }
        public sinter_IVariable getSettingByIndex(int i)
        {
            return (sinter_IVariable)o_Settings[i];
        }

        public int countSettings
        {
            get { return o_Settings.Count; }
        }
        public sinter_IVariable getTableByName(string name)
        {
            try
            {
                return (sinter_IVariable)o_Tables[name];
            }
            catch
            {
                return null;
            }
        }
        public sinter_IVariable getTableByIndex(int i)
        {
            return (sinter_IVariable)o_Tables[i];
        }
        public int countTables
        {
            get { return o_Tables.Count; }
        }


        public string aspenFilename
        {
            set
            {
                o_aspenFilename = value;
            }
            get
            {
                return o_aspenFilename;
            }
        }

        public string aspenFileHash
        {
            set
            {
                o_aspenFileHash = value;
            }
            get
            {
                return o_aspenFileHash;
            }
        }

        public string aspenFileHashAlgo
        {
            set
            {
                o_aspenFileHashAlgo = value;
            }
            get
            {
                return o_aspenFileHashAlgo;
            }
        }

        public List<String> additionalFiles
        {
            set
            {
                o_additionalFiles = value;
            }
            get
            {
                return o_additionalFiles;
            }
        }

        public List<String> additionalFilesHash
        {
            set
            {
                o_additionalFilesHash = value;
            }
            get
            {
                return o_additionalFilesHash;
            }
        }

        public List<String> additionalFilesHashAlgo
        {
            set
            {
                o_additionalFilesHashAlgo = value;
            }
            get
            {
                return o_additionalFilesHashAlgo;
            }
        }


        public string simNameRecommendation
        {
            get { return o_simNameRecommendation; }
            set { o_simNameRecommendation = value; }
        }

        //The setup file can require a particular version.  This is that version number.
        public string simVersionRecommendation
        {
            get { return o_simVersionRecommendation; }
            set { o_simVersionRecommendation = value; }
        }

        public sinter_versionConstraint simVersionConstraint
        {
            get { return o_simVersionConstraint; }
            set { o_simVersionConstraint = value; }
        }

        public Version configFileVersion {

            get { return o_configFileVersion; }
            set { o_configFileVersion = value; }
        }

        public string author
        {
            get
            {
                return o_author;
            }
            set
            {
                o_author = value;
            }
        }

        public string dateString
        {
            get
            {
                return o_dateString;
            }
            set
            {
                o_dateString = value;
            }
        }

        public string title
        {
            get
            {
                return o_title;
            }
            set
            {
                o_title = value;
            }
        }

        public string simulationDescription
        {
            get
            {
                return o_simDesc;
            }
            set
            {
                o_simDesc = value;
            }
        }


        /// <summary>
        /// This attempts to parse a setup file.  The setup file may be in JSON or the old text based format.  So we attempt
        /// to parse JSON first, and if that fails, we try the old text format.
        /// </summary>
        /// <returns></returns>
        public static sinter_SetupFile determineFileTypeAndParse(string setupString)
        {

            sinter_SetupFile jsonfile = new sinter_JsonSetupFile();
            string jsonExceptionMessage;
            string txtExceptionMessage;
            try
            {
                jsonfile.parseFile(setupString);
                return jsonfile;
            }
            catch (JsonReaderException ex)
            { 
                jsonExceptionMessage = ex.Message;
            }

            try
            {
                sinter_SetupFile textfile = new sinter_TextSetupFile();
                textfile.parseFile(setupString);
                return textfile;
            }
            catch (Exception ex)
            {
                txtExceptionMessage = ex.Message;
            }

            string ExceptionMessage = String.Format("Sinter Config File parsing failed.  Errors from JSON and Text parsers below:\n" +
                                                     "JSON: {0}\n Text: {1}\n", jsonExceptionMessage, txtExceptionMessage);

            throw new System.IO.IOException(ExceptionMessage);

        }

        public abstract int parseFile(string fileName);

        static public String parseVariable(String var, ref int row, ref int column)
        {
            row = -1;
            column = -1;

            char[] lparen = { '[' };
            string[] fields = var.Split(lparen);
            for (int ii = 0; ii < fields.Length; ++ii)
            {
                fields[ii] = fields[ii].Trim(); //Make sure names are clean
            }

            if (fields.Length == 1)
            { //Not a table
                return fields[0];
            }
            if (fields.Count() == 2)
            { //table or vector
                string varname = fields[0];
                string indices = fields[1];
                if (indices[indices.Length - 1] != ']')
                {
                    throw new System.IO.IOException(string.Format("Variable {0} is missing a closing bracket.", var));
                }
                else
                {
                    indices = indices.Remove(indices.Length - 1, 1); //Hack off the trailing rparen
                }
                char[] comma = { ',' };
                string[] indexArrayS = indices.Split(comma);
                if (indexArrayS.Length == 1)
                {  //vector
                    row = int.Parse(indexArrayS[0].Trim());
                    column = -1;
                }
                else if (indexArrayS.Length == 2)
                {//table
                    row = int.Parse(indexArrayS[0].Trim());
                    column = int.Parse(indexArrayS[1].Trim());
                }
                else
                {
                    throw new System.IO.IOException(string.Format("Variable {0} must have 2 indicies, but seems to have {1}.", var, indexArrayS.Length));
                }
                if (row < 0 || column < -1)
                {
                    throw new System.IO.IOException(string.Format("Variable {0} has an index that is less than 0", var));
                }
                return varname;
            }
            else
            {
                throw new System.IO.IOException(string.Format("Unable to parse Variable {0}.  Must either be a scalar or array reference.", var));
            }

        }


    }
}
