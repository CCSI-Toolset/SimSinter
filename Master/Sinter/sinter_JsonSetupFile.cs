using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sinter;

namespace sinter
{
    public class sinter_JsonSetupFile : sinter_SetupFile
    {

        //This constructor is run from the configGUI if we open an simulation file (not an existing configuration)
        public sinter_JsonSetupFile()
            : base()
        {
            // the constructor method
            configFileVersion = new Version(1, 0);  //This is the first version of this configuration, so 1.0
        }

        static public JArray getInputFilesArray(ISimulation sim)
        {
            sinter_SetupFile setup = sim.setupFile;
            JArray files = new JArray();
            JObject setupfileObj = new JObject();

            JObject simDescFileObj = new JObject(); //Dictionary<String, Object> thisfileDict = new Dictionary<String, Object>();

            //A little defensive programming.  The number of files and SHAs should always be equal, but just in case, redo all the SHA1s if necessary
            if (setup.additionalFiles.Count != setup.additionalFilesHash.Count)
            {
                setup.additionalFilesHash.Clear();
                setup.additionalFilesHashAlgo.Clear();
                for (int ii = 0; ii < setup.additionalFiles.Count; ++ii)
                {
                    setup.additionalFilesHash.Add("");
                    setup.additionalFilesHashAlgo.Add("");
                }
            }
 
            for (int ii = 0; ii < setup.additionalFiles.Count; ++ii)
            {
                files.Add(sinter_JsonSetupFile.AddFilenameAndSHA1toJObject(sim, setup.additionalFiles[ii], setup.additionalFilesHash[ii], setup.additionalFilesHashAlgo[ii]));
            }

            return files;
}

        static public Dictionary<String, Object> generateConfigDictionary(ISimulation sim)
        {
            sinter_SetupFile setup = sim.setupFile;
            Dictionary<String, Object> jsonDict = new Dictionary<String, Object>();
            //First add the basic meta-data:
            jsonDict.Add("title", setup.title);
            jsonDict.Add("config-version", setup.configFileVersion.ToString());
            jsonDict.Add("description", setup.simulationDescription);
            {
                jsonDict.Add("model", AddFilenameAndSHA1toJObject(sim, setup.aspenFilename, setup.aspenFileHash, setup.aspenFileHashAlgo));
            }
            /*
                       jsonDict.Add("model", setup.aspenFilename);

                        if (setup.aspenFileSHA1 != null && setup.aspenFileSHA1 != "")
            {
                jsonDict.Add("model-SHA1", setup.aspenFileSHA1);// SHA1HashFile(System.IO.Path.Combine(sim.workingDir, setup.aspenFilename)));
            }
            else
            {
                string SHA1 = "";
                try
                {
                    string filename = setup.aspenFilename;
                    if (!System.IO.Path.IsPathRooted(filename))
                    {
                        filename = System.IO.Path.Combine(sim.workingDir, setup.aspenFilename);
                    }
                    SHA1 = SHA1HashFile(filename);
                }
                catch { }
                jsonDict.Add("model-SHA1", SHA1);

            }
             **/
            jsonDict.Add("input-files", getInputFilesArray(sim));

            //Only write the simulationDescriptionFile out if it isn't the same as the simFile
            if (setup.simDescFile != null && setup.simDescFile != "" && setup.simDescFile != setup.aspenFilename)
            {
                jsonDict.Add("simulationDescriptionFile", AddFilenameAndSHA1toJObject(sim, setup.simDescFile, setup.simDescFileHash, setup.simDescFileHashAlgo));
            }

            jsonDict.Add("author", setup.author);
            jsonDict.Add("date", setup.dateString);
            jsonDict.Add("filetype", "sinterconfig");
            jsonDict.Add("filetype-version", 0.3);

            //Now create the application block
            {
                Dictionary<String, Object> appDict = new Dictionary<String, Object>();
                appDict.Add("name", sim.simName);
                if (sim.simVersionRecommendation != "")  //There may be no recommentation made yet, in that case, us the current simulator version
                {
                    appDict.Add("version", sim.simVersionRecommendation);
                }
                else
                {
                    appDict.Add("version", sim.simVersion);
                }
                appDict.Add("constraint", sinter_Sim.constraintToName(sim.simVersionConstraint));
                jsonDict.Add("application", appDict);
            }

            //Now, create the settings section:
            {
                Microsoft.VisualBasic.Collection IOObjects = setup.Variables;
                Dictionary<String, Object> settingsDict = new Dictionary<String, Object>();

                foreach (sinter_Variable ioobj in setup.Settings)
                {
                    Dictionary<String, Object> thisSetting = new Dictionary<String, Object>();
                    thisSetting.Add("type", ioobj.typeString);
                    thisSetting.Add("default", ioobj.Value);
                    thisSetting.Add("description", ioobj.description);
                    settingsDict.Add(ioobj.settingName, thisSetting);   //Settings only have 1 address String, it should be the name of the setting
                }
                if (settingsDict.Count > 0)
                {
                    jsonDict.Add("settings", settingsDict);
                }
            }

            //Now the input section
            {
                Microsoft.VisualBasic.Collection IOObjects = setup.Variables;
                Dictionary<String, Object> inputsDict = new Dictionary<String, Object>();

                foreach (sinter_Variable ioobj in IOObjects)
                {
                    if (ioobj.isInput && !ioobj.isDynamicVariable) //not dynamic
                    {
                        inputsDict.Add(ioobj.name, ioobj.toJson());
                    }
                }
                if (inputsDict.Count > 0)
                {
                    jsonDict.Add("inputs", inputsDict);
                }
            }

            //Now the dynamic-input section
            {
                Microsoft.VisualBasic.Collection IOObjects = setup.Variables;
                Dictionary<String, Object> inputsDict = new Dictionary<String, Object>();

                foreach (sinter_Variable ioobj in IOObjects)
                {
                    if (ioobj.isInput && ioobj.isDynamicVariable) //is dynamic
                    {
                        inputsDict.Add(ioobj.name, ioobj.toJson());
                    }
                }
                if (inputsDict.Count > 0)
                {
                    jsonDict.Add("dynamic-inputs", inputsDict);
                }
            }

            //outputs section
            {
                Microsoft.VisualBasic.Collection IOObjects = setup.Variables;
                Dictionary<String, Object> outputsDict = new Dictionary<String, Object>();

                foreach (sinter_Variable ioobj in IOObjects)
                {
                    if (ioobj.isOutput && !ioobj.isDynamicVariable)
                    {
                        outputsDict.Add(ioobj.name, ioobj.toJson());
                    }
                }
                if (outputsDict.Count > 0)
                {
                    jsonDict.Add("outputs", outputsDict);
                }
            }

            //dynamic-outputs section
            {
                Microsoft.VisualBasic.Collection IOObjects = setup.Variables;
                Dictionary<String, Object> outputsDict = new Dictionary<String, Object>();

                foreach (sinter_Variable ioobj in IOObjects)
                {
                    if (ioobj.isOutput && ioobj.isDynamicVariable)
                    {
                        outputsDict.Add(ioobj.name, ioobj.toJson());
                    }
                }
                if (outputsDict.Count > 0)
                {
                    jsonDict.Add("dynamic-outputs", outputsDict);
                }
            }

            //Now the inputtables section
            {
                Microsoft.VisualBasic.Collection tables = setup.Tables;
                Dictionary<String, Object> tablesDict = new Dictionary<String, Object>();

                foreach (sinter_Table ioobj in tables)
                {
                    if (ioobj.isInput)
                    {
                        Dictionary<String, Object> thisInput = new Dictionary<String, Object>();
                        thisInput.Add("rows", ioobj.rowStrings);
                        thisInput.Add("columns", ioobj.colStrings);
                        thisInput.Add("description", ioobj.description);

                        sinter_Variable[,] tableVars = ioobj.Value;
                        int rLen = tableVars.GetLength(0);
                        int cLen = tableVars.GetLength(1);
                        string[][] names = new String[rLen][];

                        for (int ii = 0; ii < tableVars.GetLength(0); ++ii)
                        {
                            string[] thisColVars = new String[cLen];
                            for (int jj = 0; jj < tableVars.GetLength(1); ++jj)
                            {
                                thisColVars[jj] = tableVars[ii, jj].name;
                            }
                            names[ii] = thisColVars;
                        }

                        thisInput.Add("contents", names);
                        tablesDict.Add(ioobj.name, thisInput);
                    }
                }
                if (tablesDict.Count > 0)
                {
                    jsonDict.Add("inputTables", tablesDict);
                }
            }

            //Now the outputtables section
            {
                Microsoft.VisualBasic.Collection tables = setup.Tables;
                Dictionary<String, Object> tablesDict = new Dictionary<String, Object>();

                foreach (sinter_Table ioobj in tables)
                {
                    if (ioobj.isOutput)
                    {
                        Dictionary<String, Object> thisInput = new Dictionary<String, Object>();
                        thisInput.Add("rows", ioobj.rowStrings);
                        thisInput.Add("columns", ioobj.colStrings);
                        thisInput.Add("description", ioobj.description);

                        sinter_Variable[,] tableVars = ioobj.Value;
                        int rLen = tableVars.GetLength(0);
                        int cLen = tableVars.GetLength(1);
                        string[][] names = new String[rLen][];

                        for (int ii = 0; ii < tableVars.GetLength(0); ++ii)
                        {
                            string[] thisColVars = new String[cLen];
                            for (int jj = 0; jj < tableVars.GetLength(1); ++jj)
                            {
                                thisColVars[jj] = tableVars[ii, jj].name;
                            }
                            names[ii] = thisColVars;
                        }

                        thisInput.Add("contents", names);
                        tablesDict.Add(ioobj.name, thisInput);

                    }
                }
                if (tablesDict.Count > 0)
                {
                    jsonDict.Add("outputTables", tablesDict);
                }
            }

            return jsonDict;
        }

        /**
         *  Throws a parse exception with message.
         *  This function tries to get error line and position information.
         **/
        void throwParseEx(JObject obj, String Message)
        {
            IJsonLineInfo linfo = (IJsonLineInfo)obj;
            if (linfo != null) //Sometimes line info is availible, sometimes it's not
            {
                int line = linfo.LineNumber;
                int pos = linfo.LinePosition;
                throw new Sinter.SinterFormatException(String.Format("ERROR: Sinter Config File Parse Failed at Line: {0} Pos {1}.\n {2}", line, pos, Message));
            }
            else
            {
                throw new Sinter.SinterFormatException(String.Format("ERROR: Sinter Config File Parse Failed.\n {0}.", Message));
            }

        }


        /** Attempts to parse out a string value from the passed in jObject, named by key
         *  If that value is not found, or it is not a string, a useful exception message is thrown 
         **/
        private string parseString(JObject jObject, string objName, string key)
        {
            JToken value = null;
            String returnVal = "";
            if (!jObject.TryGetValue(key, out value))  //Try to get the value, if there is no value of that name, throw useful exception
            {
                throwParseEx(jObject, String.Format("{0} must contain field \"{1}\"", objName, key));
            }
            try
            {
                returnVal = value.Value<String>();
            }
            catch
            {
                throwParseEx(jObject, String.Format("field \"{1}\" in {0} must be of type String", objName, key));
            }
            return returnVal;
        }

        /** Attempts to parse out an int value from the passed in jObject, named by key
         *  If that value is not found, or it is not an int, a useful exception message is thrown 
         **/
        private int parseInt(JObject jObject, string objName, string key)
        {
            JToken value = null;
            int returnVal = -1;
            if (!jObject.TryGetValue(key, out value))  //Try to get the value, if there is no value of that name, throw useful exception
            {
                throwParseEx(jObject, String.Format("{0} must contain field \"{1}\"", objName, key));
            }
            try
            {
                returnVal = value.Value<int>();
            }
            catch
            {
                throwParseEx(jObject, String.Format("field \"{1}\" in {0} must be of type Int", objName, key));
            }
            return returnVal;
        }

        /** Attempts to parse out a double value from the passed in jObject, named by key
         *  If that value is not found, or it is not a double, a useful exception message is thrown 
         **/
        private double parseDouble(JObject jObject, string objName, string key)
        {
            JToken value = null;
            double returnVal = -1.0;
            if (!jObject.TryGetValue(key, out value))  //Try to get the value, if there is no value of that name, throw useful exception
            {
                throwParseEx(jObject, String.Format("{0} must contain field \"{1}\"", objName, key));
            }
            try
            {
                returnVal = value.Value<double>();
            }
            catch
            {
                throwParseEx(jObject, String.Format("field \"{1}\" in {0} must be of type Double", objName, key));
            }
            return returnVal;
        }

        public virtual int parseFile(JObject setupObject)
        {
            JToken value = null;
            if (setupObject.TryGetValue("application", out value))
            {
                return parseFileV3(setupObject);
            }
            else
            {
                return parseFileV2(setupObject);
            }
        }

        public virtual bool keyExists(JObject obj, string key) {
            JToken token = null;
            if (obj.TryGetValue(key, out token))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

       /** Parses the (new) V0.3 setup file format.  It's very similar to the V0.2, but adds some more meta-data **/
        public int parseFileV3(JObject setupObject)
        {
            /* 1. Parse application block
             * 2. parse input files, get out simulation description file, aspenfile
             * 3. parse config version
             */
            String fileType = parseString(setupObject, "Config File Root", "filetype");
            double version = parseDouble(setupObject, "Config File Root", "filetype-version");

            String configVersionStr = parseString(setupObject, "Config File Version", "config-version");
            configFileVersion = new Version(configVersionStr);

            //In v3 all the simulation file and it's SHA1 are just under "model"
            {
                JToken model_token = null;
                if (!setupObject.TryGetValue("model", out model_token))
                {
                    throwParseEx(setupObject, String.Format("SinterConfigFile Format v0.3: {0} must contain field \"{1}\"", "root", "model"));
                }
                JObject model_object = (JObject)model_token;
                aspenFilename = parseString(model_object, "model file name", "file");
                //JToken token = null;
                if (keyExists(model_object, "SignatureMethodAlgorithm")) //, out token))  //The hash is optional, sometimes it can't be generated, so these entries won't always exist
                {
                    aspenFileHash = parseString(model_object, "model file Hash", "DigestValue");
                    aspenFileHashAlgo = parseString(model_object, "model file Hash Algo", "SignatureMethodAlgorithm");
                }
            }

            //The simulationDescriptionFile is optional.  It only exists for simulations where the simulation file and the file SimSinter uses
            //are not the same.  ie, it currently is only used by gPROMS.  Otherwise it just matches the modle file.
            {
                JToken desc_token = null;
                if (!setupObject.TryGetValue("simulationDescriptionFile", out desc_token)) //, out desc_token))
                { //Try to get the input files array (which has a pretty unique format)
                    simDescFile = aspenFilename;
                    simDescFileHash = aspenFileHash;
                    simDescFileHashAlgo = aspenFileHashAlgo;
                }
                else
                {
                    JObject desc_object = (JObject)desc_token;
                    simDescFile = parseString(desc_object, "Simulation Description File", "file");
                    //JToken token = null;
                    if (keyExists(desc_object, "SignatureMethodAlgorithm")) //, out token))  //The hash is optional, sometimes it can't be generated, so these entries won't always exist
                    {
                        simDescFileHash = parseString(desc_object, "Simulation Description file Hash", "DigestValue");
                        simDescFileHashAlgo = parseString(desc_object, "Simulation Description Hash Algo", "SignatureMethodAlgorithm");
                    }
                }
            }


            JToken inputfiles_token = null;
            if (!setupObject.TryGetValue("input-files", out inputfiles_token))
            { //Try to get the input files array (which has a pretty unique format)
                throwParseEx(setupObject, String.Format("SinterConfigFile Format v0.3: {0} must contain field \"{1}\"", "root", "input-files"));
            }
            JArray inputfilesJArray = (JArray)inputfiles_token;

            List<string> inputfiles = new List<String>();
            List<string> inputHash = new List<String>();
            List<string> inputHashAlgo = new List<String>();
            foreach (JToken entryToken in inputfilesJArray)
            {
                JObject entry = (JObject)entryToken;
                String filename = parseString(entry, "file name", "file");
                inputfiles.Add(filename);
                if (keyExists(entry, "SignatureMethodAlgorithm")) //, out token))  //The hash is optional, sometimes it can't be generated, so these entries won't always exist
                {
                    String FileHash = parseString(entry, "input file Hash", "DigestValue");
                    String FileHashAlgo = parseString(entry, "input file Hash Algo", "SignatureMethodAlgorithm");
                    inputHash.Add(FileHash);   /** NOTE: Not bothering to check SHA1s here.  Because I don't think we care? **/
                    inputHashAlgo.Add(FileHashAlgo);

                }
                else
                {
                    inputHash.Add(""); //If there's no hash there, fill it in with sentinal value
                    inputHashAlgo.Add("");
                }
            }

            additionalFiles = inputfiles;
            additionalFilesHash = inputHash;
            additionalFilesHashAlgo = inputHashAlgo;

            //Now parse the application block
            {
                JObject appBlock = (JObject)setupObject["application"];
                simNameRecommendation = parseString(appBlock, "application block name", "name");  //There may be no recommentation made yet, in that case, us the current simulator version
                simVersionRecommendation = parseString(appBlock, "application block version", "version");  //There may be no recommentation made yet, in that case, us the current simulator version
                string simConstraintStr = parseString(appBlock, "application block version", "constraint");
                simVersionConstraint = sinter_Sim.nameToConstraint(simConstraintStr);
            }

            return parseFileShared(setupObject);

        }

        /** Parses the V0.2 setup file format.  This is the original json format, and there are still plenty of these around. **/
        public int parseFileV2(JObject setupObject)
        {
            String fileType = parseString(setupObject, "Config File Root", "filetype");
            double version = parseDouble(setupObject, "Config File Root", "version");

            configFileVersion = new Version(0,0);  //Version 2 configs don't have a config version, so just make in 0.0

            //This is sad, Josh changed the key to the file to be simulation based.
            //But we haven't even created the simulation here yet!  So I have to run
            //through the possiblities.
            IDictionary<string, JToken> setupdict = setupObject;
            if (setupdict.ContainsKey("aspenfile"))
            {
                o_aspenFilename = parseString(setupObject, "Config File Root", "aspenfile");
            }
            else if (setupdict.ContainsKey("model"))
            {
                o_aspenFilename = parseString(setupObject, "Config File Root", "model");
            }
            else if (setupdict.ContainsKey("spreadsheet"))
            {
                o_aspenFilename = parseString(setupObject, "Config File Root", "spreadsheet");
            }
            else
            {
                throw new Sinter.SinterFormatException(String.Format("ERROR: Sinter Config File Parse Failed.\n The Config File Root must contain a simulation file under one of the following keys:\n gPROMS: \"model\", Aspen Plus or ACM: \"aspenfile\", Excel: \"spreadsheet\"."));
            }

            //check that some meta-data is correct
            if (!fileType.Equals("sinterconfig", StringComparison.Ordinal))
            {
                throw new System.IO.IOException(String.Format("This file is not a sinter config file, it is a {0} file!", fileType));
            }

            if (version != 0.2)
            {
                throw new System.IO.IOException(String.Format("Only sinterconfig version 0.2 is supported by this parser, this file appears to be version {0}.", version));
            }


            //This is only optional.
            if (setupdict.ContainsKey("simulationDescriptionFile"))
            {
                simDescFile = parseString(setupObject, "Config File Root", "simulationDescriptionFile");
            }
            else
            {
                simDescFile = o_aspenFilename;
            }

            return parseFileShared(setupObject);
        }

        /** Since v0.2 and v0.3 share most of the file format, this holds the parsing mechanisms common between them. **/
        public int parseFileShared(JObject setupObject)
        {
            title = parseString(setupObject, "Config File Root", "title");
            o_simDesc = parseString(setupObject, "Config File Root", "description");
            author = parseString(setupObject, "Config File Root", "author"); 
            dateString = parseString(setupObject, "Config File Root", "date");


            //Start parsing the contents of the file
            {
                JObject settings = (JObject)setupObject["settings"];
                if (settings != null)
                {
                    parseInputs(settings, true);
                }
            }

            {
                JObject inputs = (JObject)setupObject["inputs"];
                if (inputs != null)
                {
                    parseInputs(inputs, false);
                }
            }
            {
                JObject inputs = (JObject)setupObject["dynamic-inputs"];
                if (inputs != null)
                {
                    parseDynamicInputs(inputs);
                }
            }

            {
                JObject outputs = (JObject)setupObject["outputs"];
                if (outputs != null)
                {
                    parseOutputs(outputs);
                }
            }

            {
                JObject outputs = (JObject)setupObject["dynamic-outputs"];
                if (outputs != null)
                {
                    parseDynamicOutputs(outputs);
                }
            }


            {
                JObject inputTables = (JObject)setupObject["inputTables"];
                if (inputTables != null)
                {
                    parseInputTables(inputTables);
                }
            }

            {
                JObject outputTables = (JObject)setupObject["outputTables"];
                if (outputTables != null)
                {
                    parseOutputTables(outputTables);
                }
            }

            return 0;

        }

        public override int parseFile(string jsonString)
        {

            //Set some metadata for this config file
            JObject setupObject = JObject.Parse(jsonString);

            return parseFile(setupObject);

        }


        // Next 3 functions just copy JArrays to C# Arrays of the correct type
        // In C++ I could do this with templetes, but C# generics aren't that flexible.
        private string[] jArray2ArrayString(JObject jObject, String objName, String key)
        {
            JToken value = null;
            if (!jObject.TryGetValue(key, out value))  //Try to get the value, if there is no value of that name, throw useful exception
            {
                throwParseEx(jObject, String.Format("{0} must contain field \"{1}\"", objName, key));
            }

            JArray thisJArray = null;
            try
            {
                thisJArray = value.Value<JArray>();
            }
            catch
            {
                throwParseEx(jObject, String.Format("field \"{1}\" in {0} must be an array of Strings", objName, key));
            }

            string[] retArray = new string[thisJArray.Count];

            for (int ii = 0; ii < thisJArray.Count; ++ii)
            {
                try
                {
                    retArray[ii] = thisJArray[ii].Value<String>();
                }
                catch
                {
                    throwParseEx(jObject, String.Format("field \"{1}\" in {0} must be an array of Strings", objName, key));
                }
            }

            return retArray;
        }

        private int[] jArray2ArrayInt(JObject jObject, String objName, String key)
        {
            JToken value = null;
            if (!jObject.TryGetValue(key, out value))  //Try to get the value, if there is no value of that name, throw useful exception
            {
                throwParseEx(jObject, String.Format("{0} must contain field \"{1}\"", objName, key));
            }

            JArray thisJArray = null;
            try
            {
                thisJArray = value.Value<JArray>();
            }
            catch
            {
                throwParseEx(jObject, String.Format("field \"{1}\" in {0} must be an array of Int", objName, key));
            }

            int[] retArray = new int[thisJArray.Count];

            for (int ii = 0; ii < thisJArray.Count; ++ii)
            {
                try
                {
                    retArray[ii] = thisJArray[ii].Value<int>();
                }
                catch
                {
                    throwParseEx(jObject, String.Format("field \"{1}\" in {0} must be an array of Int", objName, key));
                }
            }

            return retArray;
        }

        private double[] jArray2ArrayDouble(JObject jObject, String objName, String key)
        {
            JToken value = null;
            if (!jObject.TryGetValue(key, out value))  //Try to get the value, if there is no value of that name, throw useful exception
            {
                throwParseEx(jObject, String.Format("{0} must contain field \"{1}\"", objName, key));
            }

            JArray thisJArray = null;
            try
            {
                thisJArray = value.Value<JArray>();
            }
            catch
            {
                throwParseEx(jObject, String.Format("field \"{1}\" in {0} must be an array of Int", objName, key));
            }

            double[] retArray = new double[thisJArray.Count];

            for (int ii = 0; ii < thisJArray.Count; ++ii)
            {
                try
                {
                    retArray[ii] = thisJArray[ii].Value<double>();
                }
                catch
                {
                    throwParseEx(jObject, String.Format("field \"{1}\" in {0} must be an array of Int", objName, key));
                }
            }

            return retArray;
        }

        /*
        private int[] jArray2ArrayInt(JArray thisJArray)
        {
            int[] retArray = new int[thisJArray.Count];

            for (int ii = 0; ii < thisJArray.Count; ++ii)
            {
                retArray[ii] = (int)thisJArray[ii];
            }

            return retArray;
        }

        private double[] jArray2ArrayDouble(JArray thisJArray)
        {
            double[] retArray = new double[thisJArray.Count];

            for (int ii = 0; ii < thisJArray.Count; ++ii)
            {
                retArray[ii] = (double)thisJArray[ii];
            }

            return retArray;
        }
        */


        void parseInputs(JObject inputsSet, bool isSetting)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, JToken> inputEntry in inputsSet)
            {
                JObject inputData = (JObject) inputEntry.Value;
                sinter_Variable inputVar = new sinter_Variable();
                string name = inputEntry.Key;

//                if (inputData["default"] == null || inputData["type"] == null || inputData["description"] == null || (!isSetting && inputData["path"] == null))
//                {
//                    throw new System.IO.IOException(String.Format("Required field \"{0}\" not found at Sinter Config File.", name));
//                }

                string[] path;
                string unitstring = null;
                if (isSetting)
                {
                    path = new string[] { String.Format("setting({0})", name) };
                }
                else
                {
                    path = jArray2ArrayString(inputData, name, "path");
                    unitstring = parseString(inputData, name, "units");
                }

                string typestring =  parseString(inputData, name, "type");
                string description = parseString(inputData, name, "description");
                int[] bounds = null;
                sinter_Variable.sinter_IOType type = sinter_Variable.sinter_IOType.si_UNKNOWN;
                sinter_Variable.string2Type(typestring, ref type, ref bounds);


                //Sadly, All the below is basically to make sure the type system doesn't get confused.
                //It needs to know what to convert the JToken to immediately
                if(type == sinter.sinter_Variable.sinter_IOType.si_DOUBLE) {
                        inputVar.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, path);
                        inputVar.defaultUnits = unitstring;
                        inputVar.dfault = parseDouble(inputData, name, "default");
                        if(inputData["min"] != null) {
                            inputVar.minimum = parseDouble(inputData, name, "min");
                        } 
                        if(inputData["max"] != null) {
                            inputVar.maximum = parseDouble(inputData, name, "max");
                        }

                } else if(type == sinter.sinter_Variable.sinter_IOType.si_INTEGER) {

                        inputVar.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, path);
                        inputVar.defaultUnits = unitstring;
                        inputVar.dfault = parseInt(inputData, name, "default");
                        if (inputData["min"] != null)
                        {
                            inputVar.minimum = parseInt(inputData, name, "min");
                        }
                        if (inputData["max"] != null)
                        {
                            inputVar.maximum = parseInt(inputData, name, "max");
                        }

                } else if(type == sinter.sinter_Variable.sinter_IOType.si_STRING) {
                        inputVar.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, path);
                        inputVar.defaultUnits = unitstring;
                        inputVar.dfault = parseString(inputData, name, "default");
                        if (inputData["min"] != null)
                        {
                            inputVar.minimum = parseString(inputData, name, "min");
                        }
                        if (inputData["max"] != null)
                        {
                            inputVar.maximum = parseString(inputData, name, "max");
                        }
                }
                 else if(type == sinter.sinter_Variable.sinter_IOType.si_DOUBLE_VEC) {
                        sinter_Vector inputVec = new sinter_Vector();
                        inputVec.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, path, bounds[0]);
                        inputVec.defaultUnits = unitstring;
                        inputVec.dfault = jArray2ArrayDouble(inputData, name, "default");
                    
                        if(inputData["min"] != null) {  //What in the word do mins and maxes mean for strings?
                            inputVec.minimum = jArray2ArrayDouble(inputData, name,"min");
                        } 
                        if(inputData["max"] != null) {
                            inputVec.maximum = jArray2ArrayDouble(inputData, name, "max");
                        }
                        inputVar = inputVec;
                 }

                else if (type == sinter.sinter_Variable.sinter_IOType.si_INTEGER_VEC) {
                        sinter_Vector inputVec = new sinter_Vector();
                        inputVec.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, path, bounds[0]);
                        inputVec.defaultUnits = unitstring;
                        inputVec.dfault = jArray2ArrayInt(inputData, name, "default");
                     
                        if(inputData["min"] != null) {  //What in the word do mins and maxes mean for strings?
                            inputVec.minimum = jArray2ArrayInt(inputData, name, "min");
                        } 
                        if(inputData["max"] != null) {
                            inputVec.maximum = jArray2ArrayInt(inputData, name, "max");
                        }
                        inputVar = inputVec;
                 }
                else if (type == sinter.sinter_Variable.sinter_IOType.si_STRING_VEC)
                {
                    sinter_Vector inputVec = new sinter_Vector();
                    inputVec.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, path, bounds[0]);
                    inputVec.defaultUnits = unitstring;
                    inputVec.dfault = jArray2ArrayString(inputData, name, "default");

                    if (inputData["min"] != null)
                    {  //What in the word do mins and maxes mean for strings?
                        inputVec.minimum = jArray2ArrayString(inputData, name, "min");
                    }
                    if (inputData["max"] != null)
                    {
                        inputVec.maximum = jArray2ArrayString(inputData, name, "max");
                    }
                    inputVar = inputVec;
                } else {
                    throwParseEx(inputData, String.Format("Unknown type string \"{0}\" in input variable \"{1}\".\n Allowed types are string, int, and double.", type, name));
                }

                inputVar.resetToDefault();

                if (isSetting)
                {
                    inputVar.settingName = name;                                                               
                    addSetting(inputVar);

                }
                else
                {
                    //Variable Init is complete, add the variable
                    addVariable(inputVar);
                }
            } //end for
        }

        void parseDynamicInputs(JObject inputsSet)
        {
            sinter_Vector TimeSeries = (sinter_Vector) getIOByName("TimeSeries");
            if (TimeSeries == null)
            {
                throwParseEx(inputsSet, String.Format("Dynamic Variables are defined in this config file, but there is no TimeSeries setting."));
            }

            foreach (System.Collections.Generic.KeyValuePair<string, JToken> inputEntry in inputsSet)
            {
                JObject inputData = (JObject)inputEntry.Value;
                sinter_Variable inputVar = null; // new sinter_Variable();
                string name = inputEntry.Key;

                string typestring = parseString(inputData, name, "type");
                string unitstring = parseString(inputData, name, "units");
                string[] path = jArray2ArrayString(inputData, name, "path");
                string description = parseString(inputData, name, "description");
                int[] bounds = null;
                sinter_Variable.sinter_IOType type = sinter_Variable.sinter_IOType.si_UNKNOWN;
                sinter_DynamicScalar.string2Type(typestring, ref type, ref bounds);


                //Sadly, All the below is basically to make sure the type system doesn't get confused.
                //It needs to know what to convert the JToken to immediately
                if (type == sinter.sinter_Variable.sinter_IOType.si_DY_DOUBLE)
                {
                    sinter_DynamicScalar inputDVar = new sinter_DynamicScalar();
                    
                    inputDVar.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, TimeSeries.size, path);
                    inputDVar.defaultUnits = unitstring;
                    inputDVar.dfault = parseDouble(inputData, name, "default");
                    if (inputData["min"] != null)
                    {
                        inputDVar.minimum = parseDouble(inputData, name, "min");
                    }
                    if (inputData["max"] != null)
                    {
                        inputDVar.maximum = parseDouble(inputData, name, "max");
                    }

                    inputVar = (sinter_Variable)inputDVar;
                }
                else if (type == sinter.sinter_Variable.sinter_IOType.si_DY_INTEGER)
                {
                    sinter_DynamicScalar inputDVar = new sinter_DynamicScalar();
                    inputDVar.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, TimeSeries.size, path);
                    inputDVar.defaultUnits = unitstring;
                    inputDVar.dfault = parseInt(inputData, name, "default");
                    if (inputData["min"] != null)
                    {
                        inputDVar.minimum = parseInt(inputData, name, "min");
                    }
                    if (inputData["max"] != null)
                    {
                        inputDVar.maximum = parseInt(inputData, name, "max");
                    }

                    inputVar = (sinter_Variable)inputDVar;

                }
                else if (type == sinter.sinter_Variable.sinter_IOType.si_DY_STRING)
                {
                    sinter_DynamicScalar inputDVar = new sinter_DynamicScalar();
                    inputDVar.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, TimeSeries.size, path);
                    inputDVar.defaultUnits = unitstring;
                    inputDVar.dfault = parseString(inputData, name, "default");
                    if (inputData["min"] != null)
                    {
                        inputDVar.minimum = parseString(inputData, name, "min");
                    }
                    if (inputData["max"] != null)
                    {
                        inputDVar.maximum = parseString(inputData, name, "max");
                    }

                    inputVar = (sinter_Variable)inputDVar;

                }
                else if (type == sinter.sinter_Variable.sinter_IOType.si_DY_DOUBLE_VEC)
                {
                    sinter_DynamicVector inputVec = new sinter_DynamicVector();
                    inputVec.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, TimeSeries.size, path, bounds[0]);
                    inputVec.defaultUnits = unitstring;
                    inputVec.dfault = jArray2ArrayDouble(inputData, name, "default");

                    if (inputData["min"] != null)
                    {  //What in the word do mins and maxes mean for strings?
                        inputVec.minimum = jArray2ArrayDouble(inputData, name, "min");
                    }
                    if (inputData["max"] != null)
                    {
                        inputVec.maximum = jArray2ArrayDouble(inputData, name, "max");
                    }

                    inputVar = inputVec;
                }

                else if (type == sinter.sinter_Variable.sinter_IOType.si_DY_INTEGER_VEC)
                {
                    sinter_DynamicVector inputVec = new sinter_DynamicVector();
                    inputVec.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, TimeSeries.size, path, bounds[0]);
                    inputVec.defaultUnits = unitstring;
                    inputVec.dfault = jArray2ArrayInt(inputData, name, "default");

                    if (inputData["min"] != null)
                    {  //What in the word do mins and maxes mean for strings?
                        inputVec.minimum = jArray2ArrayInt(inputData, name, "min");
                    }
                    if (inputData["max"] != null)
                    {
                        inputVec.maximum = jArray2ArrayInt(inputData, name, "max");
                    }
                    inputVar = inputVec;
                }
                else if (type == sinter.sinter_Variable.sinter_IOType.si_DY_STRING_VEC)
                {
                    sinter_DynamicVector inputVec = new sinter_DynamicVector();
                    inputVec.init(name, sinter.sinter_Variable.sinter_IOMode.si_IN, type, description, TimeSeries.size, path, bounds[0]);
                    inputVec.defaultUnits = unitstring;
                    inputVec.dfault = jArray2ArrayString(inputData, name, "default");

                    if (inputData["min"] != null)
                    {  //What in the word do mins and maxes mean for strings?
                        inputVec.minimum = jArray2ArrayString(inputData, name, "min");
                    }
                    if (inputData["max"] != null)
                    {
                        inputVec.maximum = jArray2ArrayString(inputData, name, "max");
                    }
                    inputVar = inputVec;
                }
                else
                {
                    throwParseEx(inputData, String.Format("Unknown type string \"{0}\" in dynamic input variable \"{1}\".\n Allowed types are string, int, and double.", type, name));
                }

                inputVar.resetToDefault();
                //Variable Init is complete, add the variable
                addVariable(inputVar);

            } //end for
        }

        void parseOutputs(JObject outputsSet)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, JToken> outputEntry in outputsSet)
            {
                JObject outputData = (JObject)outputEntry.Value;
                string name = outputEntry.Key;

                string typestring = parseString(outputData, name, "type");
                string unitstring = parseString(outputData, name, "units");
                string[] path = jArray2ArrayString(outputData, name, "path");
                string description = parseString(outputData, name, "description");
                int[] bounds = null;
                sinter_Variable.sinter_IOType type = sinter_Variable.sinter_IOType.si_UNKNOWN;
                sinter_Variable.string2Type(typestring, ref type, ref bounds);

                if (type == sinter_Variable.sinter_IOType.si_DOUBLE ||
                    type == sinter_Variable.sinter_IOType.si_INTEGER ||
                    type == sinter_Variable.sinter_IOType.si_STRING)
                {

                    sinter_Variable outputVar = new sinter_Variable();

                    outputVar.init(name, sinter.sinter_Variable.sinter_IOMode.si_OUT, type, description, path);
                    outputVar.defaultUnits = unitstring;
                    outputVar.units = unitstring;
                    addVariable(outputVar);
                }
                else if (type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC ||
                         type == sinter_Variable.sinter_IOType.si_INTEGER_VEC ||
                         type == sinter_Variable.sinter_IOType.si_STRING_VEC)
                {
                    sinter_Vector outputVar = new sinter_Vector();

                    outputVar.init(name, sinter.sinter_Variable.sinter_IOMode.si_OUT, type, description, path, bounds[0]);
                    outputVar.defaultUnits = unitstring;
                    outputVar.units = unitstring;
                    addVariable(outputVar);
                }
                else
                {
                    throwParseEx(outputData, String.Format("Unknown type string \"{0}\" in output variable \"{1}\".\n Allowed types are string, int, and double.", type, name));
                }
            }
        }

        void parseDynamicOutputs(JObject outputsSet)
        {
            sinter_Vector TimeSeries = (sinter_Vector)getIOByName("TimeSeries");
            if (TimeSeries == null)
            {
                throw new System.IO.IOException(String.Format("Dynamic Variables are defined in this config file, but there is not TimeSeries setting."));
            }


            foreach (System.Collections.Generic.KeyValuePair<string, JToken> outputEntry in outputsSet)
            {
                JObject outputData = (JObject)outputEntry.Value;
                string name = outputEntry.Key;

                string typestring = parseString(outputData, name, "type");
                string unitstring = parseString(outputData, name, "units");
                string[] path = jArray2ArrayString(outputData, name, "path");
                string description = parseString(outputData, name, "description");
                int[] bounds = null;
                sinter_Variable.sinter_IOType type = sinter_Variable.sinter_IOType.si_UNKNOWN;
                sinter_DynamicScalar.string2Type(typestring, ref type, ref bounds);

                if (type == sinter_Variable.sinter_IOType.si_DY_DOUBLE ||
                    type == sinter_Variable.sinter_IOType.si_DY_INTEGER ||
                    type == sinter_Variable.sinter_IOType.si_DY_STRING)
                {

                    sinter_DynamicScalar outputVar = new sinter_DynamicScalar();

                    outputVar.init(name, sinter.sinter_Variable.sinter_IOMode.si_OUT, type, description, TimeSeries.size, path);
                    outputVar.defaultUnits = unitstring;
                    outputVar.units = unitstring;
                    addDynamicVariable(outputVar);
                }
                else if (type == sinter_Variable.sinter_IOType.si_DY_DOUBLE_VEC ||
                         type == sinter_Variable.sinter_IOType.si_DY_INTEGER_VEC ||
                         type == sinter_Variable.sinter_IOType.si_DY_STRING_VEC)
                {
                    sinter_DynamicVector outputVar = new sinter_DynamicVector();

                    outputVar.init(name, sinter.sinter_Variable.sinter_IOMode.si_OUT, type, description, TimeSeries.size, path, bounds[0]);
                    outputVar.defaultUnits = unitstring;
                    outputVar.units = unitstring;
                    addDynamicVariable(outputVar);
                }
                else
                {
                    throwParseEx(outputData, String.Format("Unknown type string \"{0}\" in dynamic output variable \"{1}\".\n Allowed types are string, int, and double.", type, name));

                }
            }
        }

        /** Tables only made sense with the Excel interface, we should just get rid of them.... **/
        void parseInputTables(JObject inputsSet)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, JToken> inputEntry in inputsSet)
            {
                JObject inputData = (JObject)inputEntry.Value;
                sinter_Table inputTable = new sinter_Table();
                string name = inputEntry.Key;

                string description = parseString(inputData, name, "description");
                string[] rLabels = jArray2ArrayString(inputData, name, "rows");
                string[] cLabels = jArray2ArrayString(inputData, name, "columns");

                inputTable.init(name, sinter_Variable.sinter_IOMode.si_IN, description, null, rLabels.Length, cLabels.Length);

                inputTable.rowLabels = rLabels;
                inputTable.colLabels = cLabels;

                JArray variableRows = (JArray)inputData["contents"];
                if (variableRows.Count != rLabels.Length)
                {
                    throwParseEx(inputData, String.Format("Table {0} has {1} rows in the contents, but {2} rowLabels.", name, variableRows.Count, rLabels));
                }

                for( int rr = 0; rr < variableRows.Count; ++rr) {   
                    JArray variableCols = (JArray) variableRows[rr];
                    if (variableCols.Count != cLabels.Length)
                    {
                       throwParseEx(inputData, String.Format("Table {0} has {0} colLabels, but column {1} has {2} entries", name, cLabels.Length, rr, variableCols.Count));
                    }

                    for( int cc = 0; cc < variableCols.Count; ++cc) {   
                        string varName = (string) variableCols[cc];
                        sinter_IVariable thisIVar = getVariableByName(varName);

                        if (thisIVar == null)
                        {
                            throwParseEx(inputData, String.Format("Table {0} references {1}, but no variable of that name exists", inputTable.name, varName));
                        }

                        if(!thisIVar.isScalar) {
                            throwParseEx(inputData, String.Format("Table {0} contents cannot be vectors or tables, only scalars", inputTable.name));
                        }
                        if(!thisIVar.isInput) {
                            throwParseEx(inputData, String.Format("Input table {0} must only contain input variables", inputTable.name));
                        }
                        sinter_Variable thisVar = (sinter_Variable) thisIVar;
                        inputTable.setVariable(rr, cc, thisVar);
                        thisVar.tableName = inputTable.name;
                        thisVar.tableRow = rr;
                        thisVar.tableCol = cc;
                        thisVar.table = inputTable;
                    }
                }

            }
        }

        //Output tables
        void parseOutputTables(JObject outputsSet)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, JToken> outputEntry in outputsSet)
            {
                JObject outputData = (JObject)outputEntry.Value;
                sinter_Table outputTable = new sinter_Table();
                string name = outputEntry.Key;

                string description = parseString(outputData, name, "description");
                string[] rLabels = jArray2ArrayString(outputData, name, "rows");
                string[] cLabels = jArray2ArrayString(outputData, name, "columns");

                outputTable.init(name, sinter_Variable.sinter_IOMode.si_IN, description, null, rLabels.Length, cLabels.Length);

                outputTable.rowLabels = rLabels;
                outputTable.colLabels = cLabels;

                JArray variableRows = (JArray)outputData["contents"];
                if (variableRows.Count != rLabels.Length)
                {
                    throwParseEx(outputData, String.Format("Table {0} has {1} rows in the contents, but {2} rowLabels.", name, variableRows.Count, rLabels));
                }

                for (int rr = 0; rr < variableRows.Count; ++rr)
                {
                    JArray variableCols = (JArray)variableRows[rr];
                    if (variableCols.Count != cLabels.Length)
                    {
                        throwParseEx(outputData, String.Format("Table {0} has {0} colLabels, but column {1} has {2} entries", name, cLabels.Length, rr, variableCols.Count));
                    }

                    for (int cc = 0; cc < variableCols.Count; ++cc)
                    {
                        string varName = (string)variableCols[cc];
                        sinter_IVariable thisIVar = getVariableByName(varName);

                        if (thisIVar == null)
                        {
                            throwParseEx(outputData, String.Format("Table {0} references {1}, but no variable of that name exists", outputTable.name, varName));
                        }

                        if (!thisIVar.isScalar)
                        {
                            throwParseEx(outputData, String.Format("Table {0} contents cannot be vectors or tables, only scalars", outputTable.name));
                        }
                        if (!thisIVar.isOutput)
                        {
                            throwParseEx(outputData, String.Format("Input table {0} must only contain input variables", outputTable.name));   
                        }
                        sinter_Variable thisVar = (sinter_Variable)thisIVar;
                        outputTable.setVariable(rr, cc, thisVar);
                        thisVar.tableName = outputTable.name;
                        thisVar.tableRow = rr;
                        thisVar.tableCol = cc;
                        thisVar.table = outputTable;
                    }
                }

            }
        }

        #region SHA1
        // Create an SHA1 hash digest of a file
        static public string SHA1HashBytes(byte[] data)
        {
            byte[] hash = SHA1.Create().ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();  //Lower case is standard in SHA1
        }

        static public string SHA1HashFile(string fn)
        {
            byte[] data = File.ReadAllBytes(fn);
            return SHA1HashBytes(data);
        }

        public static JObject AddFilenameAndSHA1toJObject(ISimulation sim, string filename, string possibleHash, string hashAlgo)
        {
            JObject thisfileObj = new JObject(); 
            if (possibleHash == null || possibleHash == "")  //If it doesn't have a SHA1, try to make one.  May fail and remain ""
            {
                possibleHash = "";
                hashAlgo = "";
                string abs_filename = filename;
                try
                {
                    if (!System.IO.Path.IsPathRooted(filename))
                    {
                        abs_filename = System.IO.Path.Combine(sim.workingDir, abs_filename);
                    }
                    possibleHash = SHA1HashFile(abs_filename);
                    hashAlgo = "sha1";
                }
                catch { }
            }

            thisfileObj.Add(new JProperty("file", filename));
            if (possibleHash != "")  //Only output the hash if we actually have one
            {
                thisfileObj.Add(new JProperty("DigestValue", possibleHash)); // SHA1HashFile(System.IO.Path.Combine(sim.workingDir, name))));
                thisfileObj.Add(new JProperty("SignatureMethodAlgorithm", hashAlgo)); // SHA1HashFile(System.IO.Path.Combine(sim.workingDir, name))));
            }
            return thisfileObj;
        }
        #endregion SHA1

    }
}
