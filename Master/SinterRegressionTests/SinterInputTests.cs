using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using sinter;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SinterRegressionTests
{
    /// <summary>
    /// A couple of tests to verify that functions used for preparing inputs to Sinter work correctly.
    /// </summary>
    [TestClass]
    public class SinterInputTests
    {
        public SinterInputTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion


        [TestMethod]
        public void ParseVariableTest()
        {
            string scalar = "foo";
            string goodRef = "baz[3,3]";
            string badRefRow = "bay[-1, 3]";
            string badRefCol = "bax[1, -3]";
            int row = -2;
            int col = -2;


            string result = sinter_SetupFile.parseVariable(scalar, ref row, ref col);
            Assert.IsTrue(result.CompareTo(scalar) == 0);
            Assert.IsTrue(row == -1);
            Assert.IsTrue(col == -1);

            result = sinter_SetupFile.parseVariable(goodRef, ref row, ref col);
            Assert.IsTrue(result.CompareTo("baz") == 0);
            Assert.IsTrue(row == 3);
            Assert.IsTrue(col == 3);

            bool exCaught = false;
            try
            {
                result = sinter_SetupFile.parseVariable(badRefRow, ref row, ref col);
            }
            catch (System.IO.IOException)
            {
                exCaught = true;
            }
            Assert.IsTrue(exCaught);

            exCaught = false;
            try
            {
                result = sinter_SetupFile.parseVariable(badRefCol, ref row, ref col);
            }
            catch (System.IO.IOException)
            {
                exCaught = true;
            }

            Assert.IsTrue(exCaught);

        }

        //[TestMethod]
        //public void InputsCheckTest()
        //{
        //    var dir = Directory.CreateDirectory("InputsCheckTest");
        //    string cwd = Directory.GetCurrentDirectory();
        //    Console.WriteLine(cwd);
        //    File.Copy(Properties.Settings.Default.sinterConfiguration, 
        //        Path.Combine(dir.FullName, Path.GetFileName(Properties.Settings.Default.sinterConfiguration)), 
        //        true);
        //    File.Copy(Properties.Settings.Default.simulationBackup,
        //        Path.Combine(dir.FullName, Path.GetFileName(Properties.Settings.Default.simulationBackup)), 
        //        true);

        //    //this function returns 0 if no error
        //    //another integer for errors
        //    StreamReader inFileStream = new StreamReader(Properties.Settings.Default.sinterConfiguration);
        //    string setupString = "";
        //    setupString = inFileStream.ReadToEnd();
        //    inFileStream.Close();            

        //    sinter_Sim stest = (sinter_Sim)sinter_Factory.createSinter(setupString);
        //    stest.workingDir = dir.FullName;
        //    //stest.openSim();

        //    bool exCaught = false;

        //    var goodJson = @"{""P_abs_top"":16.0,""abs_ht_wash"":5.0, ""abs_ht_mid"":10.7,""abs_ic_dT"":-11.3,""P_sol_pump"":30.0,""lean_load"":0.178,""P_regen_top"":21.2,""cond_T_regen"":121.0,""ht_regen"":13.0,""slv_cool_01"":130.0,""lr_rich_T"":207.0,""reg_dia_in"":17.37,""input_s[0,0]"":134.4,""input_s[0,1]"":16.0,""input_s[0,2]"":959553.0,""input_s[0,3]"":0.071,""input_s[0,4]"":0.0,""input_s[0,5]"":0.0,""input_s[0,6]"":0.211,""input_s[0,7]"":0.0,""input_s[0,8]"":0.0,""input_s[0,9]"":0.0,""input_s[0,10]"":0.0,""input_s[0,11]"":0.0,""input_s[0,12]"":0.0,""input_s[0,13]"":0.0,""input_s[0,14]"":0.0,""input_s[0,15]"":0.718,""input_s[1,0]"":126.0,""input_s[1,1]"":30.0,""input_s[1,2]"":4319500.4116,""input_s[1,3]"":0.66207726067,""input_s[1,4]"":0.28374739743,""input_s[1,5]"":0.0,""input_s[1,6]"":0.0541753419,""input_s[1,7]"":0.0,""input_s[1,8]"":0.0,""input_s[1,9]"":0.0,""input_s[1,10]"":0.0,""input_s[1,11]"":0.0,""input_s[1,12]"":0.0,""input_s[1,13]"":0.0,""input_s[1,14]"":0.0,""input_s[1,15]"":0.0,""input_s[2,0]"":120.0,""input_s[2,1]"":30.0,""input_s[2,2]"":10000.0,""input_s[2,3]"":1.0,""input_s[2,4]"":0.0,""input_s[2,5]"":0.0,""input_s[2,6]"":0.0,""input_s[2,7]"":0.0,""input_s[2,8]"":0.0,""input_s[2,9]"":0.0,""input_s[2,10]"":0.0,""input_s[2,11]"":0.0,""input_s[2,12]"":0.0,""input_s[2,13]"":0.0,""input_s[2,14]"":0.0,""input_s[2,15]"":0.0,""eq_par[0,0]"":0.7996,""eq_par[0,1]"":-8094.81,""eq_par[0,2]"":0.0,""eq_par[0,3]"":-0.007484,""eq_par[1,0]"":98.566,""eq_par[1,1]"":1353.8,""eq_par[1,2]"":-14.3043,""eq_par[1,3]"":0.0,""eq_par[2,0]"":216.049,""eq_par[2,1]"":-12431.7,""eq_par[2,2]"":-35.4819,""eq_par[2,3]"":0.0,""eq_par[3,0]"":1.282562,""eq_par[3,1]"":-3456.179,""eq_par[3,2]"":0.0,""eq_par[3,3]"":0.0,""eq_par[4,0]"":132.899,""eq_par[4,1]"":-13445.9,""eq_par[4,2]"":-22.4773,""eq_par[4,3]"":0.0}";
        //    var goodDict = Newtonsoft.Json.Linq.JObject.Parse(goodJson);
        //    stest.checkAllInputs(goodDict);
            
        //    var scalarMissingJson =  @"{""abs_ht_mid"":10.7,""abs_ic_dT"":-11.3,""P_sol_pump"":30.0,""lean_load"":0.178,""P_regen_top"":21.2,""cond_T_regen"":121.0,""ht_regen"":13.0,""slv_cool_01"":130.0,""lr_rich_T"":207.0,""reg_dia_in"":17.37,""input_s[0,0]"":134.4,""input_s[0,1]"":16.0,""input_s[0,2]"":959553.0,""input_s[0,3]"":0.071,""input_s[0,4]"":0.0,""input_s[0,5]"":0.0,""input_s[0,6]"":0.211,""input_s[0,7]"":0.0,""input_s[0,8]"":0.0,""input_s[0,9]"":0.0,""input_s[0,10]"":0.0,""input_s[0,11]"":0.0,""input_s[0,12]"":0.0,""input_s[0,13]"":0.0,""input_s[0,14]"":0.0,""input_s[0,15]"":0.718,""input_s[1,0]"":126.0,""input_s[1,1]"":30.0,""input_s[1,2]"":4319500.4116,""input_s[1,3]"":0.66207726067,""input_s[1,4]"":0.28374739743,""input_s[1,5]"":0.0,""input_s[1,6]"":0.0541753419,""input_s[1,7]"":0.0,""input_s[1,8]"":0.0,""input_s[1,9]"":0.0,""input_s[1,10]"":0.0,""input_s[1,11]"":0.0,""input_s[1,12]"":0.0,""input_s[1,13]"":0.0,""input_s[1,14]"":0.0,""input_s[1,15]"":0.0,""input_s[2,0]"":120.0,""input_s[2,1]"":30.0,""input_s[2,2]"":10000.0,""input_s[2,3]"":1.0,""input_s[2,4]"":0.0,""input_s[2,5]"":0.0,""input_s[2,6]"":0.0,""input_s[2,7]"":0.0,""input_s[2,8]"":0.0,""input_s[2,9]"":0.0,""input_s[2,10]"":0.0,""input_s[2,11]"":0.0,""input_s[2,12]"":0.0,""input_s[2,13]"":0.0,""input_s[2,14]"":0.0,""input_s[2,15]"":0.0,""eq_par[0,0]"":0.7996,""eq_par[0,1]"":-8094.81,""eq_par[0,2]"":0.0,""eq_par[0,3]"":-0.007484,""eq_par[1,0]"":98.566,""eq_par[1,1]"":1353.8,""eq_par[1,2]"":-14.3043,""eq_par[1,3]"":0.0,""eq_par[2,0]"":216.049,""eq_par[2,1]"":-12431.7,""eq_par[2,2]"":-35.4819,""eq_par[2,3]"":0.0,""eq_par[3,0]"":1.282562,""eq_par[3,1]"":-3456.179,""eq_par[3,2]"":0.0,""eq_par[3,3]"":0.0,""eq_par[4,0]"":132.899,""eq_par[4,1]"":-13445.9,""eq_par[4,2]"":-22.4773,""eq_par[4,3]"":0.0}";
        //    var scalarMissingDict = Newtonsoft.Json.Linq.JObject.Parse(scalarMissingJson);
        //    try
        //    {
        //        stest.checkAllInputs(scalarMissingDict);
        //    }
        //    catch (System.IO.IOException ex)
        //    {
        //        exCaught = true;
        //    }
        //    Assert.IsTrue(exCaught);
        //    exCaught = false;

        //    var arrayRefMissingJson = @"{""P_abs_top"":16.0,""abs_ht_wash"":5.0, ""abs_ht_mid"":10.7,""abs_ic_dT"":-11.3,""P_sol_pump"":30.0,""lean_load"":0.178,""P_regen_top"":21.2,""cond_T_regen"":121.0,""ht_regen"":13.0,""slv_cool_01"":130.0,""lr_rich_T"":207.0,""reg_dia_in"":17.37,""input_s[0,0]"":134.4,""input_s[0,2]"":959553.0,""input_s[0,3]"":0.071,""input_s[0,4]"":0.0,""input_s[0,5]"":0.0,""input_s[0,6]"":0.211,""input_s[0,7]"":0.0,""input_s[0,8]"":0.0,""input_s[0,9]"":0.0,""input_s[0,10]"":0.0,""input_s[0,11]"":0.0,""input_s[0,12]"":0.0,""input_s[0,13]"":0.0,""input_s[0,14]"":0.0,""input_s[0,15]"":0.718,""input_s[1,0]"":126.0,""input_s[1,1]"":30.0,""input_s[1,2]"":4319500.4116,""input_s[1,3]"":0.66207726067,""input_s[1,4]"":0.28374739743,""input_s[1,5]"":0.0,""input_s[1,6]"":0.0541753419,""input_s[1,7]"":0.0,""input_s[1,8]"":0.0,""input_s[1,9]"":0.0,""input_s[1,10]"":0.0,""input_s[1,11]"":0.0,""input_s[1,12]"":0.0,""input_s[1,13]"":0.0,""input_s[1,14]"":0.0,""input_s[1,15]"":0.0,""input_s[2,0]"":120.0,""input_s[2,1]"":30.0,""input_s[2,2]"":10000.0,""input_s[2,3]"":1.0,""input_s[2,4]"":0.0,""input_s[2,5]"":0.0,""input_s[2,6]"":0.0,""input_s[2,7]"":0.0,""input_s[2,8]"":0.0,""input_s[2,9]"":0.0,""input_s[2,10]"":0.0,""input_s[2,11]"":0.0,""input_s[2,12]"":0.0,""input_s[2,13]"":0.0,""input_s[2,14]"":0.0,""input_s[2,15]"":0.0,""eq_par[0,0]"":0.7996,""eq_par[0,1]"":-8094.81,""eq_par[0,2]"":0.0,""eq_par[0,3]"":-0.007484,""eq_par[1,0]"":98.566,""eq_par[1,1]"":1353.8,""eq_par[1,2]"":-14.3043,""eq_par[1,3]"":0.0,""eq_par[2,0]"":216.049,""eq_par[2,1]"":-12431.7,""eq_par[2,2]"":-35.4819,""eq_par[2,3]"":0.0,""eq_par[3,0]"":1.282562,""eq_par[3,1]"":-3456.179,""eq_par[3,2]"":0.0,""eq_par[3,3]"":0.0,""eq_par[4,0]"":132.899,""eq_par[4,1]"":-13445.9,""eq_par[4,2]"":-22.4773,""eq_par[4,3]"":0.0}";
        //    var arrayRefMissingDict = Newtonsoft.Json.Linq.JObject.Parse(arrayRefMissingJson);
        //    try
        //    {
        //        stest.checkAllInputs(arrayRefMissingDict);
        //    }
        //    catch (System.IO.IOException ex)
        //    {
        //        exCaught = true;
        //    }
        //    Assert.IsTrue(exCaught);
        //    exCaught = false;
        //}

        [TestMethod]
        [ExpectedException(typeof(System.IO.IOException))]
        public void SimFactoryEmptyStringTest()
        {
            sinter_Factory.createSinter("");
        }

        [TestMethod]
        [ExpectedException(typeof(Sinter.SinterFormatException))]
        public void SimFactoryEmptyObjectTest()
        {
            sinter_Factory.createSinter("{}");
        }
        
        [TestMethod]
        public void SimFactoryACMTest()
        {
            var path = Properties.Settings.Default.ACMConfiguration;
            byte[] buffer = File.ReadAllBytes(path);
            var configuration = Encoding.UTF8.GetString(buffer);
            var sinter = sinter_Factory.createSinter(configuration);
            Assert.IsInstanceOfType(sinter, typeof(sinter_SimACM), "Expecing ACM sinter");
        }

        [TestMethod]
        public void SimFactoryAspenPlusTest()
        {
            var path = Properties.Settings.Default.jsonConfiguration;
            byte[] buffer = File.ReadAllBytes(path);
            var configuration = Encoding.UTF8.GetString(buffer);
            var sinter = sinter_Factory.createSinter(configuration);
            Assert.IsInstanceOfType(sinter, typeof(sinter_SimAspen), "Expecing Aspen sinter");
        }
    }
}