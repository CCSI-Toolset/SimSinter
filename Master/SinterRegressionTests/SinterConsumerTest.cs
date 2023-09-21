using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sinter;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.VisualBasic;

namespace SinterRegressionTests
{
    [TestClass]
    public class SinterExecutionTest
    {
        [TestMethod]
        public void SinterACMTest()
        {
            var path = Properties.Settings.Default.ACMConfiguration;
            string cwd = Directory.GetCurrentDirectory();
            byte[] buffer = File.ReadAllBytes(path);
            var configuration = Encoding.UTF8.GetString(buffer);
            ISimulation sim = sinter_Factory.createSinter(configuration);
            sim.workingDir = System.IO.Path.GetDirectoryName(Properties.Settings.Default.ACMConfiguration);
            Assert.IsInstanceOfType(sim, typeof(sinter_SimACM), "Expecing ACM sinter");
            IDictionary<string, Object> myDict = null;

            try
            {
                sim.openSim();
                JObject defaultsDict = new JObject();
                sim.sendInputs(defaultsDict);
                sim.Vis = false; 
                sim.sendInputsToSim();
                sim.runSim();
                sim.recvOutputsFromSim();
                JObject superDict = sim.getOutputs();
                JObject outputDict = (JObject)superDict["outputs"];
                // HACK: Inefficient Just making it work w/o covariance issues
                string data = outputDict.ToString(Newtonsoft.Json.Formatting.None);
                myDict = JsonConvert.DeserializeObject<IDictionary<string, Object>>(data);
            }
            finally
            {
                sim.closeSim();
            }
            Assert.AreEqual(sinter.sinter_AppError.si_OKAY, sim.runStatus);

            Debug.WriteLine(myDict);
        }

        [TestMethod]
        public void SinterCOMInterfaceTest()
        {
            var path = Properties.Settings.Default.ACMConfiguration;
            string cwd = Directory.GetCurrentDirectory();
            byte[] buffer = File.ReadAllBytes(path);
            var configuration = Encoding.UTF8.GetString(buffer);
            ISimulation sim = sinter_Factory.createSinter(configuration);
            sim.workingDir = System.IO.Path.GetDirectoryName(Properties.Settings.Default.ACMConfiguration);
            Assert.IsInstanceOfType(sim, typeof(sinter_SimACM), "Expecing ACM sinter");
            IDictionary<string, Object> myDict = null;


            try
            {
                sim.openSim();
                JObject defaultsDict = new JObject();
                sim.sendInputs(defaultsDict);
                sim.Vis = false;
                sinter_SetupFile sf = sim.setupFile;
                sinter_IVariable ivar = sf.getIOByIndex(1);

                sim.sendInputsToSim();
                sim.runSim();
                sim.recvOutputsFromSim();
                JObject superDict = sim.getOutputs();
                JObject outputDict = (JObject)superDict["outputs"];
                // HACK: Inefficient Just making it work w/o covariance issues
                string data = outputDict.ToString(Newtonsoft.Json.Formatting.None);
                myDict = JsonConvert.DeserializeObject<IDictionary<string, Object>>(data);
            }
            finally
            {
                sim.closeSim();
            }
            Assert.AreEqual(sinter.sinter_AppError.si_OKAY, sim.runStatus);

            Debug.WriteLine(myDict);
        }


        [TestMethod]
        public void SinterDynamicACMTest_1() //Tests dynamic ACM with version 1.0 input format
        {
            var path = Path.Combine(Properties.Settings.Default.DynamicACMDir, Properties.Settings.Default.DynamicACMFilename);
            string cwd = Directory.GetCurrentDirectory();
            byte[] buffer = File.ReadAllBytes(path);
            var configuration = Encoding.UTF8.GetString(buffer);
            ISimulation sim = sinter_Factory.createSinter(configuration);
            Assert.IsInstanceOfType(sim, typeof(sinter_SimACM), "Expecing ACM sinter");

            sim.workingDir = Path.Combine(cwd, "testDynamicACM_1");
            if (Microsoft.VisualBasic.FileIO.FileSystem.DirectoryExists(sim.workingDir))
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(sim.workingDir, Microsoft.VisualBasic.FileIO.DeleteDirectoryOption.DeleteAllContents);
            }
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(Properties.Settings.Default.DynamicACMDir, sim.workingDir);
            string configFileName = Path.Combine(sim.workingDir, Properties.Settings.Default.DynamicACMFilename);
            string config = System.Text.Encoding.ASCII.GetString(buffer);

            IDictionary<string, Object> outDict = null;

            try
            {
                sim.openSim();

                byte[] inputs_buffer = File.ReadAllBytes(Path.Combine(Properties.Settings.Default.DynamicACMDir, Properties.Settings.Default.DynamicACMInputs));
                var inputsString = Encoding.UTF8.GetString(inputs_buffer);
                JObject defaultsDict = JObject.Parse(inputsString);
                sim.sendInputs(defaultsDict);
                sim.Vis = false;
                sim.sendInputsToSim();
                sim.runSim();
                sim.recvOutputsFromSim();
                JObject superDict = sim.getOutputs();
                JObject outputDict = (JObject)superDict["outputs"];
                // HACK: Inefficient Just making it work w/o covariance issues
                string data = outputDict.ToString(Newtonsoft.Json.Formatting.None);
                outDict = JsonConvert.DeserializeObject<IDictionary<string, Object>>(data);
            }
            finally
            {
                sim.closeSim();
            }
            Assert.AreEqual(sinter.sinter_AppError.si_OKAY, sim.runStatus);

            Debug.WriteLine(outDict);
        }

        [TestMethod]
        public void SinterDynamicACMTest_2()  //Tests dynamic ACM with version 2.0 input format
        {
            var path = Path.Combine(Properties.Settings.Default.DynamicACMDir, Properties.Settings.Default.DynamicACMFilename);
            string cwd = Directory.GetCurrentDirectory();
            byte[] buffer = File.ReadAllBytes(path);
            var configuration = Encoding.UTF8.GetString(buffer);
            ISimulation sim = sinter_Factory.createSinter(configuration);
            Assert.IsInstanceOfType(sim, typeof(sinter_SimACM), "Expecing ACM sinter");

            //Copy the whole directory to a temporary spot 
            sim.workingDir = Path.Combine(cwd, "testDynamicACM_2");
            if (Microsoft.VisualBasic.FileIO.FileSystem.DirectoryExists(sim.workingDir))  //delete teh old temp dir if it exists
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(sim.workingDir, Microsoft.VisualBasic.FileIO.DeleteDirectoryOption.DeleteAllContents);
            }
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(Properties.Settings.Default.DynamicACMDir, sim.workingDir);
            string configFileName = Path.Combine(sim.workingDir, Properties.Settings.Default.DynamicACMFilename);
            string config = System.Text.Encoding.ASCII.GetString(buffer);

            //Read the inputs file
            StreamReader sinterInputStream = new StreamReader(Path.Combine(Properties.Settings.Default.DynamicACMDir, Properties.Settings.Default.DynamicACMInputs_2));  //Read the version 2 input file
            String inputsString = sinterInputStream.ReadToEnd();
            JObject defaultsDict = JObject.Parse(inputsString);

            IDictionary<string, Object> myDict = null;


            try
            {
                sim.openSim();
                sim.Vis = false;
                sim.sendInputs(defaultsDict);
                sim.sendInputsToSim();
                sim.runSim();
                sim.recvOutputsFromSim();
                JObject superDict = sim.getOutputs();
                JObject outputDict = (JObject)superDict["outputs"];
                // HACK: Inefficient Just making it work w/o covariance issues
                string data = outputDict.ToString(Newtonsoft.Json.Formatting.None);
                myDict = JsonConvert.DeserializeObject<IDictionary<string, Object>>(data);
            }
            finally
            {
                sim.closeSim();
            }
            Assert.AreEqual(sinter.sinter_AppError.si_OKAY, sim.runStatus);

            Debug.WriteLine(myDict);
        }

        static void stopInteractiveSim(sinter_InteractiveSim isim)
        {
            Thread.Sleep(10000);  //10 seconds
            isim.stopSim();
        }

        [TestMethod]
        public void StopACMTest()
        {
            //We start by making a copy of the config file and the simulation file
            //"Backup" means the simulation filename (it's from Aspen Plus)
            var path = Properties.Settings.Default.LongACMConfig;
            string cwd = Directory.GetCurrentDirectory();
            byte[] buffer = File.ReadAllBytes(path);
            var configuration = Encoding.UTF8.GetString(buffer);
            JArray inputsArray = new JArray();
            JObject emptyDict = new JObject();
            inputsArray.Add(emptyDict);
            JArray outputDict = new JArray();
            List<sinter_AppError> runStatuses = null;
            List<List<object>> ts_byRunNumber = null;

            SinterProcess sp = new SinterProcess();
            sp.runSeries(path, null, inputsArray, false, 10, ref outputDict, ref runStatuses, ref ts_byRunNumber);

            Assert.AreEqual(sinter.sinter_AppError.si_SIMULATION_STOPPED, runStatuses[0]);

            Debug.WriteLine(outputDict);
        }

        [TestMethod]
        public void StopAspenPlusTest()
        {
            //We start by making a copy of the config file and the simulation file
            //"Backup" means the simulation filename (it's from Aspen Plus)
            var path = Properties.Settings.Default.LongAPConfig;
            string cwd = Directory.GetCurrentDirectory();
            byte[] buffer = File.ReadAllBytes(path);
            var configuration = Encoding.UTF8.GetString(buffer);
            ISimulation sim = sinter_Factory.createSinter(configuration);
            sim.workingDir = System.IO.Path.GetDirectoryName(Properties.Settings.Default.LongAPConfig);
            Assert.IsInstanceOfType(sim, typeof(sinter_SimAspen), "Expecing AspenPlus sinter");

            IDictionary<string, Object> myDict = null;

            try
            {
                sim.openSim();
                JObject defaultsDict = new JObject();
                sim.sendInputs(defaultsDict);
                sim.Vis = false;
                sim.sendInputsToSim();

                Thread t = new Thread(() => stopInteractiveSim((sinter_InteractiveSim)sim));
                t.Start();

                sim.runSim();
                sinter.sinter_AppError runstat = sim.runStatus;
                if (runstat < sinter_AppError.si_SIMULATION_STOPPED)
                {

                    sim.recvOutputsFromSim();
                    JObject superDict = sim.getOutputs();
                    JObject outputDict = (JObject)superDict["outputs"];
                    // HACK: Inefficient Just making it work w/o covariance issues
                    string data = outputDict.ToString(Newtonsoft.Json.Formatting.None);
                    myDict = JsonConvert.DeserializeObject<IDictionary<string, Object>>(data);
                }
            }
            finally
            {
                if (sim.runStatus == sinter_AppError.si_STOP_FAILED) //If stop failed, do NOT call back into the sim.
                {
                    sim.terminate();
                }
                else
                {

                    sim.closeSim();
                }
            }
            Assert.AreEqual(sinter.sinter_AppError.si_SIMULATION_STOPPED, sim.runStatus);

            Debug.WriteLine(myDict);
        }


        public void SinterGPROMSTest()
        {

            var path = Properties.Settings.Default.gPROMSConfig;
            byte[] buffer = File.ReadAllBytes(path);
            var configuration = Encoding.UTF8.GetString(buffer);

            ISimulation sim = sinter_Factory.createSinter(configuration);
            sim.workingDir = System.IO.Path.GetDirectoryName(Properties.Settings.Default.gPROMSConfig);
            Assert.IsInstanceOfType(sim, typeof(sinter.PSE.sinter_simGPROMS), "Expecing gPROMS sinter");

            IDictionary<string, Object> myDict = null;

            JObject outputDict;
            try
            {
                sim.openSim();
                JObject defaultsDict = new JObject();
                sim.sendInputs(defaultsDict);
                sim.sendInputsToSim();
                sim.runSim();
                sim.recvOutputsFromSim();
                JObject superDict = sim.getOutputs();
                outputDict = (JObject)superDict["outputs"];
                // HACK: Inefficient Just making it work w/o covariance issues
                string data = outputDict.ToString(Newtonsoft.Json.Formatting.None);
                myDict = JsonConvert.DeserializeObject<IDictionary<string, Object>>(data);
            }
            finally
            {
                sim.closeSim();
            }
            Assert.AreEqual(sinter.sinter_AppError.si_OKAY, sim.runStatus);

            //Verify the outputs
            Assert.IsTrue(sinter_HelperFunctions.fuzzyEquals(14.3181, (double)outputDict["Height"]["value"], .001));
            Assert.IsTrue(sinter_HelperFunctions.fuzzyEquals(14318.1, (double)outputDict["HoldUp"]["value"], .001));
            Assert.IsTrue(sinter_HelperFunctions.fuzzyEquals(11, (double)outputDict["OutSingleInt"]["value"], .001));
            Assert.IsTrue(sinter_HelperFunctions.fuzzyEquals(12, (double)outputDict["OutArrayInt"]["value"][0], .001));
            Assert.IsTrue(sinter_HelperFunctions.fuzzyEquals(1, (double)outputDict["OutSingleSel"]["value"], .001));
            Assert.IsTrue(sinter_HelperFunctions.fuzzyEquals(1, (double)outputDict["OutArraySel"]["value"][1], .001));

            Debug.WriteLine(myDict);
        }

        [TestMethod]
        public void SinterAspenPlusTest()
        {
            var path = Properties.Settings.Default.jsonConfiguration;
            byte[] buffer = File.ReadAllBytes(path);
            var configuration = Encoding.UTF8.GetString(buffer);

            ISimulation sim = sinter_Factory.createSinter(configuration);
            sim.workingDir = System.IO.Path.GetDirectoryName(path);
            Assert.IsInstanceOfType(sim, typeof(sinter_SimAspen), "Expecing Aspen sinter");
            IDictionary<string, Object> myDict = null;

            try
            {
                sim.openSim();
                JObject defaultsDict = new JObject();
                sim.sendInputs(defaultsDict);
                sim.sendInputsToSim();
                sim.runSim();
                sim.recvOutputsFromSim();
                JObject superDict = sim.getOutputs();
                JObject outputDict = (JObject)superDict["outputs"];
                // HACK: Inefficient Just making it work w/o covariance issues
                string data = outputDict.ToString(Newtonsoft.Json.Formatting.None);
                myDict = JsonConvert.DeserializeObject<IDictionary<string, Object>>(data);
            }
            finally
            {
                sim.closeSim();
            }
            Assert.AreEqual(sinter.sinter_AppError.si_OKAY, sim.runStatus);

            Debug.WriteLine(myDict);

            //Need to add an actual output check here.  This didn't actually do anything
//            foreach (KeyValuePair<string,Object> k in myDict) 
//            {
                //Debug.WriteLine(String.Format("\"{0}\" : {1}", k.Key, k.Value));
//                var d = (JObject)k.Value;
//                JValue jval = (JValue)d["value"];
//                Object value = jval.Value;
//                Assert.AreEqual(d["units"], "");
                //Assert.IsInstanceOfType(d["value"], float);
 //               Debug.WriteLine(String.Format("\"{0}\" : {1}", k.Key, value.GetType()));

//            }
            /*

absorber.output.ic.duty = {
  "value": -58703516.4,
  "units": ""
}
absorber.output.capacity = {
  "value": 0.800040428,
  "units": ""
}
absorber.output.dia = {
  "value": 26.32,
  "units": ""
}
absorber.output.profile.TandP[0,0] = {
  "value": 145.291607,
  "units": ""
}
absorber.output.profile.TandP[0,1] = {
  "value": 145.486206,
  "units": ""
}
absorber.output.profile.TandP[0,2] = {
  "value": 15.0,
  "units": ""
}
absorber.output.profile.TandP[1,0] = {
  "value": 145.552779,
  "units": ""
}
absorber.output.profile.TandP[1,1] = {
  "value": 145.554855,
  "units": ""
}
absorber.output.profile.TandP[1,2] = {
  "value": 15.0049583,
  "units": ""
}
absorber.output.profile.TandP[2,0] = {
  "value": 145.578383,
  "units": ""
}
absorber.output.profile.TandP[2,1] = {
  "value": 145.578506,
  "units": ""
}
absorber.output.profile.TandP[2,2] = {
  "value": 15.0099189,
  "units": ""
}
absorber.output.profile.TandP[3,0] = {
  "value": 145.890044,
  "units": ""
}
absorber.output.profile.TandP[3,1] = {
  "value": 145.892367,
  "units": ""
}
absorber.output.profile.TandP[3,2] = {
  "value": 15.0148779,
  "units": ""
}
absorber.output.profile.TandP[4,0] = {
  "value": 135.444438,
  "units": ""
}
absorber.output.profile.TandP[4,1] = {
  "value": 151.352639,
  "units": ""
}
absorber.output.profile.TandP[4,2] = {
  "value": 15.0198338,
  "units": ""
}
absorber.output.profile.TandP[5,0] = {
  "value": 144.516371,
  "units": ""
}
absorber.output.profile.TandP[5,1] = {
  "value": 156.731741,
  "units": ""
}
absorber.output.profile.TandP[5,2] = {
  "value": 15.0220483,
  "units": ""
}
absorber.output.profile.TandP[6,0] = {
  "value": 152.416991,
  "units": ""
}
absorber.output.profile.TandP[6,1] = {
  "value": 160.725256,
  "units": ""
}
absorber.output.profile.TandP[6,2] = {
  "value": 15.0245383,
  "units": ""
}
absorber.output.profile.TandP[7,0] = {
  "value": 158.524214,
  "units": ""
}
absorber.output.profile.TandP[7,1] = {
  "value": 163.371443,
  "units": ""
}
absorber.output.profile.TandP[7,2] = {
  "value": 15.0276376,
  "units": ""
}
absorber.output.profile.TandP[8,0] = {
  "value": 162.70748,
  "units": ""
}
absorber.output.profile.TandP[8,1] = {
  "value": 164.873276,
  "units": ""
}
absorber.output.profile.TandP[8,2] = {
  "value": 15.0313538,
  "units": ""
}
absorber.output.profile.TandP[9,0] = {
  "value": 165.235914,
  "units": ""
}
absorber.output.profile.TandP[9,1] = {
  "value": 165.498866,
  "units": ""
}
absorber.output.profile.TandP[9,2] = {
  "value": 15.0356039,
  "units": ""
}
absorber.output.profile.TandP[10,0] = {
  "value": 166.52658,
  "units": ""
}
absorber.output.profile.TandP[10,1] = {
  "value": 165.498302,
  "units": ""
}
absorber.output.profile.TandP[10,2] = {
  "value": 15.0402447,
  "units": ""
}
absorber.output.profile.TandP[11,0] = {
  "value": 166.96591,
  "units": ""
}
absorber.output.profile.TandP[11,1] = {
  "value": 165.06531,
  "units": ""
}
absorber.output.profile.TandP[11,2] = {
  "value": 15.0451239,
  "units": ""
}
absorber.output.profile.TandP[12,0] = {
  "value": 166.839133,
  "units": ""
}
absorber.output.profile.TandP[12,1] = {
  "value": 164.332983,
  "units": ""
}
absorber.output.profile.TandP[12,2] = {
  "value": 15.0501149,
  "units": ""
}
absorber.output.profile.TandP[13,0] = {
  "value": 166.337783,
  "units": ""
}
absorber.output.profile.TandP[13,1] = {
  "value": 163.386315,
  "units": ""
}
absorber.output.profile.TandP[13,2] = {
  "value": 15.0551261,
  "units": ""
}
absorber.output.profile.TandP[14,0] = {
  "value": 165.582164,
  "units": ""
}
absorber.output.profile.TandP[14,1] = {
  "value": 162.276203,
  "units": ""
}
absorber.output.profile.TandP[14,2] = {
  "value": 15.0600962,
  "units": ""
}
absorber.output.profile.TandP[15,0] = {
  "value": 164.644869,
  "units": ""
}
absorber.output.profile.TandP[15,1] = {
  "value": 161.030888,
  "units": ""
}
absorber.output.profile.TandP[15,2] = {
  "value": 15.0649855,
  "units": ""
}
absorber.output.profile.TandP[16,0] = {
  "value": 163.568132,
  "units": ""
}
absorber.output.profile.TandP[16,1] = {
  "value": 159.663793,
  "units": ""
}
absorber.output.profile.TandP[16,2] = {
  "value": 15.0697686,
  "units": ""
}
absorber.output.profile.TandP[17,0] = {
  "value": 162.375149,
  "units": ""
}
absorber.output.profile.TandP[17,1] = {
  "value": 158.178395,
  "units": ""
}
absorber.output.profile.TandP[17,2] = {
  "value": 15.0744288,
  "units": ""
}
absorber.output.profile.TandP[18,0] = {
  "value": 161.07694,
  "units": ""
}
absorber.output.profile.TandP[18,1] = {
  "value": 156.571056,
  "units": ""
}
absorber.output.profile.TandP[18,2] = {
  "value": 15.0789546,
  "units": ""
}
absorber.output.profile.TandP[19,0] = {
  "value": 159.676274,
  "units": ""
}
absorber.output.profile.TandP[19,1] = {
  "value": 154.832525,
  "units": ""
}
absorber.output.profile.TandP[19,2] = {
  "value": 15.0833379,
  "units": ""
}
absorber.output.profile.TandP[20,0] = {
  "value": 158.1697,
  "units": ""
}
absorber.output.profile.TandP[20,1] = {
  "value": 152.948628,
  "units": ""
}
absorber.output.profile.TandP[20,2] = {
  "value": 15.0875722,
  "units": ""
}
absorber.output.profile.TandP[21,0] = {
  "value": 156.548387,
  "units": ""
}
absorber.output.profile.TandP[21,1] = {
  "value": 150.900477,
  "units": ""
}
absorber.output.profile.TandP[21,2] = {
  "value": 15.0916522,
  "units": ""
}
absorber.output.profile.TandP[22,0] = {
  "value": 154.798107,
  "units": ""
}
absorber.output.profile.TandP[22,1] = {
  "value": 148.664434,
  "units": ""
}
absorber.output.profile.TandP[22,2] = {
  "value": 15.0955727,
  "units": ""
}
absorber.output.profile.TandP[23,0] = {
  "value": 152.898552,
  "units": ""
}
absorber.output.profile.TandP[23,1] = {
  "value": 146.212017,
  "units": ""
}
absorber.output.profile.TandP[23,2] = {
  "value": 15.0993288,
  "units": ""
}
absorber.output.profile.TandP[24,0] = {
  "value": 150.821936,
  "units": ""
}
absorber.output.profile.TandP[24,1] = {
  "value": 143.509982,
  "units": ""
}
absorber.output.profile.TandP[24,2] = {
  "value": 15.1029153,
  "units": ""
}
absorber.output.profile.TandP[25,0] = {
  "value": 148.530712,
  "units": ""
}
absorber.output.profile.TandP[25,1] = {
  "value": 140.520889,
  "units": ""
}
absorber.output.profile.TandP[25,2] = {
  "value": 15.1063262,
  "units": ""
}
absorber.output.profile.TandP[26,0] = {
  "value": 145.973998,
  "units": ""
}
absorber.output.profile.TandP[26,1] = {
  "value": 137.20473,
  "units": ""
}
absorber.output.profile.TandP[26,2] = {
  "value": 15.109555,
  "units": ""
}
absorber.output.profile.TandP[27,0] = {
  "value": 131.578475,
  "units": ""
}
absorber.output.profile.TandP[27,1] = {
  "value": 133.52258,
  "units": ""
}
absorber.output.profile.TandP[27,2] = {
  "value": 15.1125941,
  "units": ""
}
absorber.output.profile.TandP[28,0] = {
  "value": 133.566227,
  "units": ""
}
absorber.output.profile.TandP[28,1] = {
  "value": 134.199007,
  "units": ""
}
absorber.output.profile.TandP[28,2] = {
  "value": 15.1155005,
  "units": ""
}
absorber.output.profile.TandP[29,0] = {
  "value": 134.517098,
  "units": ""
}
absorber.output.profile.TandP[29,1] = {
  "value": 134.389312,
  "units": ""
}
absorber.output.profile.TandP[29,2] = {
  "value": 15.118616,
  "units": ""
}
absorber.output.profile.TandP[30,0] = {
  "value": 134.765887,
  "units": ""
}
absorber.output.profile.TandP[30,1] = {
  "value": 134.299372,
  "units": ""
}
absorber.output.profile.TandP[30,2] = {
  "value": 15.1218519,
  "units": ""
}
absorber.output.profile.TandP[31,0] = {
  "value": 134.527031,
  "units": ""
}
absorber.output.profile.TandP[31,1] = {
  "value": 134.091745,
  "units": ""
}
absorber.output.profile.TandP[31,2] = {
  "value": 15.1251436,
  "units": ""
}
absorber.output.profile.TandP[32,0] = {
  "value": 133.928369,
  "units": ""
}
absorber.output.profile.TandP[32,1] = {
  "value": 133.915046,
  "units": ""
}
absorber.output.profile.TandP[32,2] = {
  "value": 15.1284471,
  "units": ""
}
absorber.output.profile.TandP[33,0] = {
  "value": 133.038043,
  "units": ""
}
absorber.output.profile.TandP[33,1] = {
  "value": 133.933201,
  "units": ""
}
absorber.output.profile.TandP[33,2] = {
  "value": 15.1317326,
  "units": ""
}
absorber.output.profile.CO2[0,0] = {
  "value": 0.0144638428,
  "units": ""
}
absorber.output.profile.CO2[0,1] = {
  "value": 0.223027633,
  "units": ""
}
absorber.output.profile.CO2[1,0] = {
  "value": 0.0144578658,
  "units": ""
}
absorber.output.profile.CO2[1,1] = {
  "value": 0.223338427,
  "units": ""
}
absorber.output.profile.CO2[2,0] = {
  "value": 0.0144579423,
  "units": ""
}
absorber.output.profile.CO2[2,1] = {
  "value": 0.223334458,
  "units": ""
}
absorber.output.profile.CO2[3,0] = {
  "value": 0.0144619167,
  "units": ""
}
absorber.output.profile.CO2[3,1] = {
  "value": 0.223230712,
  "units": ""
}
absorber.output.profile.CO2[4,0] = {
  "value": 0.0145381526,
  "units": ""
}
absorber.output.profile.CO2[4,1] = {
  "value": 0.221326395,
  "units": ""
}
absorber.output.profile.CO2[5,0] = {
  "value": 0.0187156145,
  "units": ""
}
absorber.output.profile.CO2[5,1] = {
  "value": 0.251143302,
  "units": ""
}
absorber.output.profile.CO2[6,0] = {
  "value": 0.0235155358,
  "units": ""
}
absorber.output.profile.CO2[6,1] = {
  "value": 0.276537821,
  "units": ""
}
absorber.output.profile.CO2[7,0] = {
  "value": 0.02863275,
  "units": ""
}
absorber.output.profile.CO2[7,1] = {
  "value": 0.295428227,
  "units": ""
}
absorber.output.profile.CO2[8,0] = {
  "value": 0.0336884071,
  "units": ""
}
absorber.output.profile.CO2[8,1] = {
  "value": 0.307251877,
  "units": ""
}
absorber.output.profile.CO2[9,0] = {
  "value": 0.0384419394,
  "units": ""
}
absorber.output.profile.CO2[9,1] = {
  "value": 0.312811598,
  "units": ""
}
absorber.output.profile.CO2[10,0] = {
  "value": 0.0428505855,
  "units": ""
}
absorber.output.profile.CO2[10,1] = {
  "value": 0.313543615,
  "units": ""
}
absorber.output.profile.CO2[11,0] = {
  "value": 0.0469829154,
  "units": ""
}
absorber.output.profile.CO2[11,1] = {
  "value": 0.310867766,
  "units": ""
}
absorber.output.profile.CO2[12,0] = {
  "value": 0.0509335166,
  "units": ""
}
absorber.output.profile.CO2[12,1] = {
  "value": 0.305886427,
  "units": ""
}
absorber.output.profile.CO2[13,0] = {
  "value": 0.0547815656,
  "units": ""
}
absorber.output.profile.CO2[13,1] = {
  "value": 0.299359614,
  "units": ""
}
absorber.output.profile.CO2[14,0] = {
  "value": 0.0585833385,
  "units": ""
}
absorber.output.profile.CO2[14,1] = {
  "value": 0.291773658,
  "units": ""
}
absorber.output.profile.CO2[15,0] = {
  "value": 0.0623760874,
  "units": ""
}
absorber.output.profile.CO2[15,1] = {
  "value": 0.283425391,
  "units": ""
}
absorber.output.profile.CO2[16,0] = {
  "value": 0.0661844361,
  "units": ""
}
absorber.output.profile.CO2[16,1] = {
  "value": 0.274487994,
  "units": ""
}
absorber.output.profile.CO2[17,0] = {
  "value": 0.0700260246,
  "units": ""
}
absorber.output.profile.CO2[17,1] = {
  "value": 0.265054948,
  "units": ""
}
absorber.output.profile.CO2[18,0] = {
  "value": 0.0739157288,
  "units": ""
}
absorber.output.profile.CO2[18,1] = {
  "value": 0.255166929,
  "units": ""
}
absorber.output.profile.CO2[19,0] = {
  "value": 0.0778686329,
  "units": ""
}
absorber.output.profile.CO2[19,1] = {
  "value": 0.244827076,
  "units": ""
}
absorber.output.profile.CO2[20,0] = {
  "value": 0.0819021262,
  "units": ""
}
absorber.output.profile.CO2[20,1] = {
  "value": 0.234008783,
  "units": ""
}
absorber.output.profile.CO2[21,0] = {
  "value": 0.0860374933,
  "units": ""
}
absorber.output.profile.CO2[21,1] = {
  "value": 0.222658522,
  "units": ""
}
absorber.output.profile.CO2[22,0] = {
  "value": 0.0903013346,
  "units": ""
}
absorber.output.profile.CO2[22,1] = {
  "value": 0.210695047,
  "units": ""
}
absorber.output.profile.CO2[23,0] = {
  "value": 0.0947271184,
  "units": ""
}
absorber.output.profile.CO2[23,1] = {
  "value": 0.198005357,
  "units": ""
}
absorber.output.profile.CO2[24,0] = {
  "value": 0.0993571959,
  "units": ""
}
absorber.output.profile.CO2[24,1] = {
  "value": 0.184437037,
  "units": ""
}
absorber.output.profile.CO2[25,0] = {
  "value": 0.104245686,
  "units": ""
}
absorber.output.profile.CO2[25,1] = {
  "value": 0.169785689,
  "units": ""
}
absorber.output.profile.CO2[26,0] = {
  "value": 0.109462831,
  "units": ""
}
absorber.output.profile.CO2[26,1] = {
  "value": 0.153774986,
  "units": ""
}
absorber.output.profile.CO2[27,0] = {
  "value": 0.115101771,
  "units": ""
}
absorber.output.profile.CO2[27,1] = {
  "value": 0.136024838,
  "units": ""
}
absorber.output.profile.CO2[28,0] = {
  "value": 0.121520338,
  "units": ""
}
absorber.output.profile.CO2[28,1] = {
  "value": 0.138061799,
  "units": ""
}
absorber.output.profile.CO2[29,0] = {
  "value": 0.12606453,
  "units": ""
}
absorber.output.profile.CO2[29,1] = {
  "value": 0.137768036,
  "units": ""
}
absorber.output.profile.CO2[30,0] = {
  "value": 0.129478512,
  "units": ""
}
absorber.output.profile.CO2[30,1] = {
  "value": 0.135713687,
  "units": ""
}
absorber.output.profile.CO2[31,0] = {
  "value": 0.132252709,
  "units": ""
}
absorber.output.profile.CO2[31,1] = {
  "value": 0.132266125,
  "units": ""
}
absorber.output.profile.CO2[32,0] = {
  "value": 0.13471076,
  "units": ""
}
absorber.output.profile.CO2[32,1] = {
  "value": 0.127610784,
  "units": ""
}
absorber.output.profile.CO2[33,0] = {
  "value": 0.137074654,
  "units": ""
}
absorber.output.profile.CO2[33,1] = {
  "value": 0.121781803,
  "units": ""
}
absorber.output.profile.kv[0,0] = {
  "value": 0.0,
  "units": ""
}
absorber.output.profile.kv[1,0] = {
  "value": 0.0,
  "units": ""
}
absorber.output.profile.kv[2,0] = {
  "value": 0.0,
  "units": ""
}
absorber.output.profile.kv[3,0] = {
  "value": 0.0,
  "units": ""
}
absorber.output.profile.kv[4,0] = {
  "value": 11977.5877,
  "units": ""
}
absorber.output.profile.kv[5,0] = {
  "value": 12366.8599,
  "units": ""
}
absorber.output.profile.kv[6,0] = {
  "value": 12749.8599,
  "units": ""
}
absorber.output.profile.kv[7,0] = {
  "value": 13088.5494,
  "units": ""
}
absorber.output.profile.kv[8,0] = {
  "value": 13353.0183,
  "units": ""
}
absorber.output.profile.kv[9,0] = {
  "value": 13535.2549,
  "units": ""
}
absorber.output.profile.kv[10,0] = {
  "value": 13646.0486,
  "units": ""
}
absorber.output.profile.kv[11,0] = {
  "value": 13703.3969,
  "units": ""
}
absorber.output.profile.kv[12,0] = {
  "value": 13724.3816,
  "units": ""
}
absorber.output.profile.kv[13,0] = {
  "value": 13721.727,
  "units": ""
}
absorber.output.profile.kv[14,0] = {
  "value": 13703.9276,
  "units": ""
}
absorber.output.profile.kv[15,0] = {
  "value": 13676.31,
  "units": ""
}
absorber.output.profile.kv[16,0] = {
  "value": 13642.0947,
  "units": ""
}
absorber.output.profile.kv[17,0] = {
  "value": 13603.1797,
  "units": ""
}
absorber.output.profile.kv[18,0] = {
  "value": 13560.6505,
  "units": ""
}
absorber.output.profile.kv[19,0] = {
  "value": 13515.0906,
  "units": ""
}
absorber.output.profile.kv[20,0] = {
  "value": 13466.762,
  "units": ""
}
absorber.output.profile.kv[21,0] = {
  "value": 13415.706,
  "units": ""
}
absorber.output.profile.kv[22,0] = {
  "value": 13361.7948,
  "units": ""
}
absorber.output.profile.kv[23,0] = {
  "value": 13304.755,
  "units": ""
}
absorber.output.profile.kv[24,0] = {
  "value": 13244.1716,
  "units": ""
}
absorber.output.profile.kv[25,0] = {
  "value": 13179.482,
  "units": ""
}
absorber.output.profile.kv[26,0] = {
  "value": 13109.9616,
  "units": ""
}
absorber.output.profile.kv[27,0] = {
  "value": 13276.9069,
  "units": ""
}
absorber.output.profile.kv[28,0] = {
  "value": 13404.7804,
  "units": ""
}
absorber.output.profile.kv[29,0] = {
  "value": 13476.8355,
  "units": ""
}
absorber.output.profile.kv[30,0] = {
  "value": 13512.6461,
  "units": ""
}
absorber.output.profile.kv[31,0] = {
  "value": 13525.104,
  "units": ""
}
absorber.output.profile.kv[32,0] = {
  "value": 13522.2981,
  "units": ""
}
absorber.output.profile.kv[33,0] = {
  "value": 13509.1745,
  "units": ""
}
absorber.output.profile.kl[0,0] = {
  "value": 6872.72753,
  "units": ""
}
absorber.output.profile.kl[1,0] = {
  "value": 6881.77662,
  "units": ""
}
absorber.output.profile.kl[2,0] = {
  "value": 6842.6817,
  "units": ""
}
absorber.output.profile.kl[3,0] = {
  "value": 6127.05342,
  "units": ""
}
absorber.output.profile.kl[4,0] = {
  "value": 131445.733,
  "units": ""
}
absorber.output.profile.kl[5,0] = {
  "value": 139337.294,
  "units": ""
}
absorber.output.profile.kl[6,0] = {
  "value": 146201.806,
  "units": ""
}
absorber.output.profile.kl[7,0] = {
  "value": 151437.554,
  "units": ""
}
absorber.output.profile.kl[8,0] = {
  "value": 154901.847,
  "units": ""
}
absorber.output.profile.kl[9,0] = {
  "value": 156836.885,
  "units": ""
}
absorber.output.profile.kl[10,0] = {
  "value": 157629.936,
  "units": ""
}
absorber.output.profile.kl[11,0] = {
  "value": 157642.715,
  "units": ""
}
absorber.output.profile.kl[12,0] = {
  "value": 157143.313,
  "units": ""
}
absorber.output.profile.kl[13,0] = {
  "value": 156311.564,
  "units": ""
}
absorber.output.profile.kl[14,0] = {
  "value": 155260.3,
  "units": ""
}
absorber.output.profile.kl[15,0] = {
  "value": 154057.609,
  "units": ""
}
absorber.output.profile.kl[16,0] = {
  "value": 152743.203,
  "units": ""
}
absorber.output.profile.kl[17,0] = {
  "value": 151339.066,
  "units": ""
}
absorber.output.profile.kl[18,0] = {
  "value": 149855.905,
  "units": ""
}
absorber.output.profile.kl[19,0] = {
  "value": 148296.841,
  "units": ""
}
absorber.output.profile.kl[20,0] = {
  "value": 146659.4,
  "units": ""
}
absorber.output.profile.kl[21,0] = {
  "value": 144936.398,
  "units": ""
}
absorber.output.profile.kl[22,0] = {
  "value": 143116.106,
  "units": ""
}
absorber.output.profile.kl[23,0] = {
  "value": 141181.855,
  "units": ""
}
absorber.output.profile.kl[24,0] = {
  "value": 139111.074,
  "units": ""
}
absorber.output.profile.kl[25,0] = {
  "value": 136873.674,
  "units": ""
}
absorber.output.profile.kl[26,0] = {
  "value": 134429.501,
  "units": ""
}
absorber.output.profile.kl[27,0] = {
  "value": 123697.869,
  "units": ""
}
absorber.output.profile.kl[28,0] = {
  "value": 125081.692,
  "units": ""
}
absorber.output.profile.kl[29,0] = {
  "value": 125680.458,
  "units": ""
}
absorber.output.profile.kl[30,0] = {
  "value": 125743.985,
  "units": ""
}
absorber.output.profile.kl[31,0] = {
  "value": 125431.915,
  "units": ""
}
absorber.output.profile.kl[32,0] = {
  "value": 124838.277,
  "units": ""
}
absorber.output.profile.kl[33,0] = {
  "value": 124011.497,
  "units": ""
}
solvent.output.stream.mass[0,0] = {
  "value": 126.0,
  "units": ""
}
solvent.output.stream.mass[0,1] = {
  "value": 30.0,
  "units": ""
}
solvent.output.stream.mass[0,2] = {
  "value": 4319500.41,
  "units": ""
}
solvent.output.stream.mass[0,3] = {
  "value": 0.661574215,
  "units": ""
}
solvent.output.stream.mass[0,4] = {
  "value": 0.134267742,
  "units": ""
}
solvent.output.stream.mass[0,5] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[0,6] = {
  "value": 4.83609036E-07,
  "units": ""
}
solvent.output.stream.mass[0,7] = {
  "value": 0.000913911857,
  "units": ""
}
solvent.output.stream.mass[0,8] = {
  "value": 0.125202187,
  "units": ""
}
solvent.output.stream.mass[0,9] = {
  "value": 0.0772468823,
  "units": ""
}
solvent.output.stream.mass[0,10] = {
  "value": 0.000783994075,
  "units": ""
}
solvent.output.stream.mass[0,11] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[0,12] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[0,13] = {
  "value": 1.13108612E-11,
  "units": ""
}
solvent.output.stream.mass[0,14] = {
  "value": 2.22782515E-06,
  "units": ""
}
solvent.output.stream.mass[0,15] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[1,0] = {
  "value": 120.0,
  "units": ""
}
solvent.output.stream.mass[1,1] = {
  "value": 30.0,
  "units": ""
}
solvent.output.stream.mass[1,2] = {
  "value": 10000.0,
  "units": ""
}
solvent.output.stream.mass[1,3] = {
  "value": 0.999999992,
  "units": ""
}
solvent.output.stream.mass[1,4] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[1,5] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[1,6] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[1,7] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[1,8] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[1,9] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[1,10] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[1,11] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[1,12] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[1,13] = {
  "value": 4.27179227E-09,
  "units": ""
}
solvent.output.stream.mass[1,14] = {
  "value": 3.81934676E-09,
  "units": ""
}
solvent.output.stream.mass[1,15] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[2,0] = {
  "value": 133.038043,
  "units": ""
}
solvent.output.stream.mass[2,1] = {
  "value": 15.1317326,
  "units": ""
}
solvent.output.stream.mass[2,2] = {
  "value": 4455584.81,
  "units": ""
}
solvent.output.stream.mass[2,3] = {
  "value": 0.626752688,
  "units": ""
}
solvent.output.stream.mass[2,4] = {
  "value": 0.0273663226,
  "units": ""
}
solvent.output.stream.mass[2,5] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[2,6] = {
  "value": 7.08823503E-05,
  "units": ""
}
solvent.output.stream.mass[2,7] = {
  "value": 0.0113072724,
  "units": ""
}
solvent.output.stream.mass[2,8] = {
  "value": 0.198998122,
  "units": ""
}
solvent.output.stream.mass[2,9] = {
  "value": 0.13307586,
  "units": ""
}
solvent.output.stream.mass[2,10] = {
  "value": 0.00138286583,
  "units": ""
}
solvent.output.stream.mass[2,11] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[2,12] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mass[2,13] = {
  "value": 1.90579241E-10,
  "units": ""
}
solvent.output.stream.mass[2,14] = {
  "value": 2.55782865E-07,
  "units": ""
}
solvent.output.stream.mass[2,15] = {
  "value": 0.0010457312,
  "units": ""
}
solvent.output.stream.mole[0,0] = {
  "value": 178811.304,
  "units": ""
}
solvent.output.stream.mole[0,1] = {
  "value": 0.887107198,
  "units": ""
}
solvent.output.stream.mole[0,2] = {
  "value": 0.053098806,
  "units": ""
}
solvent.output.stream.mole[0,3] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[0,4] = {
  "value": 2.65450508E-07,
  "units": ""
}
solvent.output.stream.mole[0,5] = {
  "value": 0.000361815443,
  "units": ""
}
solvent.output.stream.mole[0,6] = {
  "value": 0.0290584983,
  "units": ""
}
solvent.output.stream.mole[0,7] = {
  "value": 0.0300546613,
  "units": ""
}
solvent.output.stream.mole[0,8] = {
  "value": 0.00031559167,
  "units": ""
}
solvent.output.stream.mole[0,9] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[0,10] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[0,11] = {
  "value": 1.43635816E-11,
  "units": ""
}
solvent.output.stream.mole[0,12] = {
  "value": 3.16423851E-06,
  "units": ""
}
solvent.output.stream.mole[0,13] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[1,0] = {
  "value": 555.084351,
  "units": ""
}
solvent.output.stream.mole[1,1] = {
  "value": 0.999999992,
  "units": ""
}
solvent.output.stream.mole[1,2] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[1,3] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[1,4] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[1,5] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[1,6] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[1,7] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[1,8] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[1,9] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[1,10] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[1,11] = {
  "value": 4.04556951E-09,
  "units": ""
}
solvent.output.stream.mole[1,12] = {
  "value": 4.04556951E-09,
  "units": ""
}
solvent.output.stream.mole[1,13] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[2,0] = {
  "value": 176176.746,
  "units": ""
}
solvent.output.stream.mole[2,1] = {
  "value": 0.87985543,
  "units": ""
}
solvent.output.stream.mole[2,2] = {
  "value": 0.0113304483,
  "units": ""
}
solvent.output.stream.mole[2,3] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[2,4] = {
  "value": 4.07328544E-05,
  "units": ""
}
solvent.output.stream.mole[2,5] = {
  "value": 0.0046866036,
  "units": ""
}
solvent.output.stream.mole[2,6] = {
  "value": 0.0483534883,
  "units": ""
}
solvent.output.stream.mole[2,7] = {
  "value": 0.0542060474,
  "units": ""
}
solvent.output.stream.mole[2,8] = {
  "value": 0.000582787703,
  "units": ""
}
solvent.output.stream.mole[2,9] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[2,10] = {
  "value": 0.0,
  "units": ""
}
solvent.output.stream.mole[2,11] = {
  "value": 2.5337295E-10,
  "units": ""
}
solvent.output.stream.mole[2,12] = {
  "value": 3.80344499E-07,
  "units": ""
}
solvent.output.stream.mole[2,13] = {
  "value": 0.000944080729,
  "units": ""
}
gas.output.stream.mass[0,0] = {
  "value": 134.4,
  "units": ""
}
gas.output.stream.mass[0,1] = {
  "value": 16.0,
  "units": ""
}
gas.output.stream.mass[0,2] = {
  "value": 959553.0,
  "units": ""
}
gas.output.stream.mass[0,3] = {
  "value": 0.071,
  "units": ""
}
gas.output.stream.mass[0,4] = {
  "value": 0.0,
  "units": ""
}
gas.output.stream.mass[0,5] = {
  "value": 0.0,
  "units": ""
}
gas.output.stream.mass[0,6] = {
  "value": 0.211,
  "units": ""
}
gas.output.stream.mass[0,7] = {
  "value": 0.718,
  "units": ""
}
gas.output.stream.mass[1,0] = {
  "value": 145.486206,
  "units": ""
}
gas.output.stream.mass[1,1] = {
  "value": 15.0,
  "units": ""
}
gas.output.stream.mass[1,2] = {
  "value": 833406.755,
  "units": ""
}
gas.output.stream.mass[1,3] = {
  "value": 0.154445872,
  "units": ""
}
gas.output.stream.mass[1,4] = {
  "value": 8.48358637E-17,
  "units": ""
}
gas.output.stream.mass[1,5] = {
  "value": 0.0,
  "units": ""
}
gas.output.stream.mass[1,6] = {
  "value": 0.0244686327,
  "units": ""
}
gas.output.stream.mass[1,7] = {
  "value": 0.821085495,
  "units": ""
}
gas.output.stream.mole[0,0] = {
  "value": 32976.0017,
  "units": ""
}
gas.output.stream.mole[0,1] = {
  "value": 0.114680163,
  "units": ""
}
gas.output.stream.mole[0,2] = {
  "value": 0.0,
  "units": ""
}
gas.output.stream.mole[0,3] = {
  "value": 0.0,
  "units": ""
}
gas.output.stream.mole[0,4] = {
  "value": 0.13950958,
  "units": ""
}
gas.output.stream.mole[0,5] = {
  "value": 0.745810257,
  "units": ""
}
gas.output.stream.mole[1,0] = {
  "value": 32035.6566,
  "units": ""
}
gas.output.stream.mole[1,1] = {
  "value": 0.223027633,
  "units": ""
}
gas.output.stream.mole[1,2] = {
  "value": 3.61307847E-17,
  "units": ""
}
gas.output.stream.mole[1,3] = {
  "value": 0.0,
  "units": ""
}
gas.output.stream.mole[1,4] = {
  "value": 0.0144638428,
  "units": ""
}
gas.output.stream.mole[1,5] = {
  "value": 0.762508524,
  "units": ""
}
status = {
  "units": "",
  "value": 0
}
            */
        }
    }
}
