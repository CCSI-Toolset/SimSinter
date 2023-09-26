using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sinter;

namespace SinterRegressionTests
{
    [TestClass]
    public class UnitsTest
    {

        [Priority(1), TestMethod]
        public void UnitTest1()
        {
            String sinterconf = Properties.Settings.Default.UnitsTestSinter;
            String infilename = Properties.Settings.Default.UnitsTestInputs;
            String workingDir = Properties.Settings.Default.UnitsWorkingDir;

            StreamReader inStream = new StreamReader(infilename);
            StreamReader sinterConfigStream = new StreamReader(sinterconf);

            string inString = inStream.ReadToEnd();
            string sinterConfigString = sinterConfigStream.ReadToEnd();
            inStream.Close();
            sinterConfigStream.Close();

            JArray injson = (JArray) JToken.Parse(inString);
            JArray outjson = new JArray();
            List<sinter_AppError> runStatuses = null;
            List<List<object>> ts_byRunNumber = null;
            SinterProcess sp = new SinterProcess();
            sp.runSeries(sinterconf, null, injson, false, -1, ref outjson, ref runStatuses, ref ts_byRunNumber);

            Assert.IsFalse(runStatuses[0] == sinter_AppError.si_OKAY);

            //Verify the inputs
            Assert.IsTrue(212.0 == (double)outjson[0]["inputs"]["in.boil.celsius"]["value"]);
            Assert.IsTrue("degF" == (string)outjson[0]["inputs"]["in.boil.celsius"]["units"]);
            Assert.IsTrue(100.0 == (double)outjson[0]["inputs"]["in.boil.fahrenheit"]["value"]);
            Assert.IsTrue("degC" == (string)outjson[0]["inputs"]["in.boil.fahrenheit"]["units"]);
            Assert.IsTrue(-273.15 == (double)outjson[0]["inputs"]["in.misc.kelvin"]["value"][0]);
            Assert.IsTrue("degC" == (string)outjson[0]["inputs"]["in.misc.kelvin"]["units"]);


        }
    }
}
