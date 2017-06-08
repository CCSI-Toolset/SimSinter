using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sinter;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;


namespace SinterRegressionTests
{
    [TestClass]
    public class SinterExcelTest
    {

        /// <summary>
        /// Excel SinterTest_1 tests sinter inputs and config files of version 1 (The original stuff)
        /// </summary>

        /// <summary>
        /// ExcelSinterTest_2 tests sinter inputs and defaults of version 2.  (JSON sinter config)
        /// </summary>
        [TestMethod]
        public void RunExcelTest()
        {
            String sinterconf = Properties.Settings.Default.ExcelSinterJson;
            String defaultsfile = null;
            String infilename = Properties.Settings.Default.ExcelSinterInputs;
            String outfilename = Properties.Settings.Default.ExcelSinterOutputs;
            String canonicalOutputFilename = Properties.Settings.Default.ExcelSinterCanonicalOutputs;

            doSimulation(sinterconf, defaultsfile, infilename, outfilename, canonicalOutputFilename);

        }

        //Not really useful outside these tests.  For one thing, the value is assumed to be a double
        //Only checks one direction.  Checks that all the entries in outputs appear in canonOutputs, not the
        //other way around. 
        private bool outputsEqual(JObject outputs, JObject canonOutputs)
        {

            foreach (KeyValuePair<string, JToken> outputPair in outputs)
            {
                string name = outputPair.Key;
                JObject outputData = (JObject)outputPair.Value;

                //First just do a quick check that status is success (0)
                if (name == "status")
                {
                    int outValue = (int)outputData["value"];
                    if (outValue != 0)
                    {
                        return false;
                    }
                    continue;
                }

                

                JToken canonOutputToken = canonOutputs[name];
                if (canonOutputToken == null)
                {
                    return false;
                }

                JObject canonOutputData = (JObject)canonOutputToken;

                string outUnits = (string)outputData["units"];
                string canonOutUnits = (string)canonOutputData["units"];
                if (outUnits != canonOutUnits)
                {
                    return false;
                }

                try
                {
                    double outValue = (double)outputData["value"];
                    double canonOutValue = (double)canonOutputData["value"];

                    if (outValue != canonOutValue)
                    {
                        return false;
                    }
                }
                catch (ArgumentException)
                {
                    //If it's not a double, try an array?
                    JArray outValueArray = (JArray)outputData["value"];
                    JArray canonOutValueArray = (JArray)canonOutputData["value"];

                    for (int ii = 0; ii < outValueArray.Count; ++ii)
                    {
                        double outValue = (double)outValueArray[ii];
                        double canonOutValue = (double)canonOutValueArray[ii];

                        if (outValue != canonOutValue)
                        {
                            return false;
                        }
                    }
                }


            }

            return true;
        }

        private void doSimulation(String sinterconf, String defaultsfile,
                                  String infilename, String outfilename, String canonicalOutputFilename)
        {

            String workingDir = Path.GetDirectoryName(sinterconf);

            StreamReader defaultsStream = null;
            if (defaultsfile != null)
            {
                defaultsStream = new StreamReader(defaultsfile);
            }
            StreamReader inStream = new StreamReader(infilename);
            StreamWriter outStream = new StreamWriter(outfilename);
            StreamReader canonOutStream = new StreamReader(canonicalOutputFilename);

            //this function returns 0 if no error
            //another integer for errors
            StreamReader inFileStream = new StreamReader(sinterconf);
            string setupString = "";
            setupString = inFileStream.ReadToEnd();
            inFileStream.Close();

            //ISimulation stest = sinter_Factory.createSinter(setupString); //Need to change this to setup file contents string

            ISimulation stest = sinter_Factory.createSinter(setupString);
            //sinter_SetupFile thisSetupFile = sinter_SetupFile.determineFileTypeAndParse(setupString);
            //ISimulation stest = new sinter_SimExcel();
            //stest.setupFile.parseFile(setupString);
            //((sinter_Sim)stest).makeIOTree();

            Debug.WriteLine("SINTER: " + stest.GetType().Name, GetType().Name);
            stest.workingDir = workingDir;
            //stest.readSetup(sinterconf); //Read the setup file also opens sim
            stest.openSim(); //connect to aspen
            stest.Vis = false;
            Console.WriteLine(stest.Vis);
            //      stest.Layout();  //figure out spreadsheet layout
            stest.dialogSuppress = true;
            stest.resetSim();

            //The console version reads in json, the actual version just pulls the dictionary from the database
            string injson = "";
            string defaultsjson = "";
            string canonOutJson = "";
            JObject inputDict = null;
            JObject defaultsDict = null;
            JObject canonOutDict = null;
            try
            {
                injson = inStream.ReadToEnd();
                inputDict = JObject.Parse(injson); // JsonConvert.DeserializeObject<Dictionary<String, Object>>(injson);
                canonOutJson = canonOutStream.ReadToEnd();
                canonOutDict = JObject.Parse(canonOutJson); // JsonConvert.DeserializeObject<Dictionary<String, Object>>(injson);

                if (defaultsStream != null)
                {
                    defaultsjson = defaultsStream.ReadToEnd();
                    defaultsDict = JObject.Parse(defaultsjson); // Convert.DeserializeObject<Dictionary<String, Object>>(defaultsjson);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception reading input file! " + ex.Message);
            }
            finally
            {
                inStream.Close();

            }


            if (defaultsDict != null)
            {
                stest.sendDefaults(defaultsDict);
            }
            stest.sendInputs(inputDict);
            stest.sendInputsToSim();
            stest.runSim();
            stest.recvOutputsFromSim();
            sinter.sinter_AppError runStatus = stest.runStatus;
            Console.WriteLine("RunStatus: {0}", (int)runStatus);

            Console.WriteLine("Errors:");
            string[] errorList = stest.errorsBasic();
            foreach (string error in errorList)
            {
                Console.Write(error);
            }

            Console.WriteLine();

            Console.WriteLine("Warnings:");
            string[] warnList = stest.warningsBasic();
            foreach (string error in warnList)
            {
                Console.Write(error);
            }

            Console.WriteLine();


            JObject outputDict = stest.getOutputs();
            Debug.WriteLine("Output: " + outputDict, GetType().Name);

            JsonSerializerSettings jss = new JsonSerializerSettings();
            jss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            string jsonOutput = JsonConvert.SerializeObject(outputDict, Formatting.Indented, jss);
            outStream.WriteLine(jsonOutput);

            stest.closeSim();

            outStream.Close();

            JObject outTok = (JObject)outputDict["outputs"];
            JObject canOutTok = (JObject)canonOutDict["outputs"];

            Assert.IsTrue(outputsEqual(outTok, canOutTok));

//            Assert.IsTrue(outputDict.Equals(canonOutDict));



        }

    }

}
