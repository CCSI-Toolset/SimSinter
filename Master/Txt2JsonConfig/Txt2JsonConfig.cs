using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sinter;
//using Turbine.Consumer;

namespace Txt2JsonConfig
{
    class Txt2JsonConfig
    {
        private static sinter.ISimulation stest;

        static bool shouldOutputMinMax(sinter_Variable ioobj)
        {
            //Strings don't have min/max, so skip.  Vectors need to have every entry checked
            if (ioobj.type == sinter_Variable.sinter_IOType.si_DOUBLE || ioobj.type == sinter_Variable.sinter_IOType.si_INTEGER)
            {
                if (ioobj.minimum != ioobj.maximum && Convert.ToDouble(ioobj.minimum) != 0.0)  //The default values are 0.0 for both
                {
                    return true;
                }
            }
            else if (ioobj.type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC || ioobj.type == sinter_Variable.sinter_IOType.si_INTEGER_VEC)
            {
                sinter_Vector thisVec = (sinter_Vector)ioobj;
                bool hasDifference = false;
                for (int ii = 0; ii < thisVec.size; ++ii)
                {
                    if (thisVec.getElementMin(ii) != thisVec.getElementMax(ii) && ((double)thisVec.getElementMin(ii)) != 0)  //The default values are 0.0 for both
                    {
                        hasDifference = true;
                        break;
                    }
                }
                if (hasDifference)  //JSON should be able to figure out these are arrays.
                {
                    return true;
                }
            }
            return false;
        }

        static Dictionary<String, Object> generateConfigDictionary(ISimulation sim)
        {
            sinter_SetupFile setup = sim.setupFile;
            Dictionary<String, Object> jsonDict = new Dictionary<String, Object>();
            //First add the basic meta-data:

            jsonDict.Add("title", setup.title);
            jsonDict.Add("description", setup.simulationDescription);
            jsonDict.Add("aspenfile", setup.aspenFilename);
            jsonDict.Add("author", setup.author);
            jsonDict.Add("date", setup.dateString);
            jsonDict.Add("filetype", "sinterconfig");
            jsonDict.Add("version", 0.2);

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
                    if (ioobj.isInput)
                    { //Vectors SHOULD just work here, since JSON knows how to deal with double, int, and string arrays...
                        Dictionary<String, Object> thisInput = new Dictionary<String, Object>();
                        thisInput.Add("path", ioobj.addressStrings);
                        thisInput.Add("type", ioobj.typeString);
                        thisInput.Add("default", ioobj.Value);
                        thisInput.Add("description", ioobj.description);
                        thisInput.Add("units", ioobj.defaultUnits); //Old config files don't have units.

                        if (shouldOutputMinMax(ioobj))
                        {
                            thisInput.Add("min", ioobj.minimum);
                            thisInput.Add("max", ioobj.maximum);
                        }
                        inputsDict.Add(ioobj.name, thisInput);
                    }
                }
                if (inputsDict.Count > 0)
                {
                    jsonDict.Add("inputs", inputsDict);
                }
            }

            //outputs section
            {
                Microsoft.VisualBasic.Collection IOObjects = setup.Variables;
                Dictionary<String, Object> outputsDict = new Dictionary<String, Object>();

                foreach (sinter_Variable ioobj in IOObjects)
                {
                    if (ioobj.isOutput)
                    {

                        Dictionary<String, Object> thisOutput = new Dictionary<String, Object>();
                        thisOutput.Add("path", ioobj.addressStrings);
                        thisOutput.Add("type", ioobj.typeString);
                        thisOutput.Add("default", ioobj.Value);  //Yeah, the default is the canonical result, not whatever is in default
                        thisOutput.Add("description", ioobj.description);
                        thisOutput.Add("units", ioobj.defaultUnits); //Old config files don't have units.

                        outputsDict.Add(ioobj.name, thisOutput);
                    }
                }
                if (outputsDict.Count > 0)
                {
                    jsonDict.Add("outputs", outputsDict);
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

        static void Main(string[] args)
        {
            //stest = new sinter_SimAspen();

            String configFileName = args[0];
            String defaultsfile = null;
            String outfilename = null;
            StreamReader defaultsStream = null;

            if (args.Length == 2)
            {
                outfilename = args[1];
            }
            else if (args.Length == 3)
            {
                defaultsfile = args[1];
                outfilename = args[2];
                defaultsStream = new StreamReader(defaultsfile);
            }

            String workingDir = Path.GetDirectoryName(configFileName);

             

            //this function returns 0 if no error
            //another integer for errors
            StreamReader inFileStream = new StreamReader(configFileName);
            string setupString = "";
            setupString = inFileStream.ReadToEnd();
            inFileStream.Close();


            stest = sinter_Factory.createSinter(setupString);
            stest.workingDir = workingDir;
            stest.openSim(); //connect to aspen
            stest.Vis = false;
            stest.dialogSuppress = true;
 
            string defaultsjson;

            if (defaultsfile != null)
            {
                try
                {
                    defaultsjson = defaultsStream.ReadToEnd();
                    JObject defaultsDict = JObject.Parse(defaultsjson);
                    stest.sendInputs(defaultsDict);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception reading input file! " + ex.Message);
                }
                finally
                {
                    defaultsStream.Close();

                }

            }
            else
            {
                stest.initializeDefaults(); //Get inputs for defaults
            }

            stest.sendInputsToSim();
            stest.runSim();

            sinter.sinter_AppError runStatus = stest.runStatus;
            if (runStatus != 0)
            {
                throw new System.IO.IOException("Run Failed, defaults are invalid!");
            }

            stest.initializeUnits();
            stest.initializeDefaults(); //Get inputs for defaults
            stest.recvOutputsFromSim();    //Out outputs for canonical values
            Dictionary<String, Object> outputDict = generateConfigDictionary(stest);
            JsonSerializerSettings jss = new JsonSerializerSettings();
            jss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            string jsonOutput = JsonConvert.SerializeObject(outputDict, Formatting.Indented, jss);

            StreamWriter outStream = new StreamWriter(outfilename);
            outStream.WriteLine(jsonOutput);
            outStream.Close();

            stest.closeSim();
            Console.WriteLine("CONVERSION FINISHED. PRESS ANY KEY TO END PROGRAM.");
            Console.ReadLine();
        }
    }
}
