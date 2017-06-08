using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sinter;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VariableTree;

namespace SinterRegressionTests
{
    [TestClass]
    public class DataTreeTests
    {
        /// <summary>
        /// DataTreeTest tests that the data tree is created correctly, that you can resolve nodes,
        /// that the resolved nodes can be found in the actual simulation.
        /// </summary>
        [TestMethod]
        public void DataTreeTest()
        {
            String sinterconf = Properties.Settings.Default.ExcelSinterJson;
            String defaultsfile = null;
            String infilename = Properties.Settings.Default.ExcelSinterInputs;
            String outfilename = Properties.Settings.Default.ExcelSinterOutputs;
            String canonicalOutputFilename = Properties.Settings.Default.ExcelSinterCanonicalOutputs;

            ISimulation sim = startupSim(sinterconf, defaultsfile, infilename);
            sinter_InteractiveSim a_sim = (sinter_InteractiveSim)sim; //Only works on Interactive sims
            {
                String nodePath = "height$C$2";

                VariableTreeNode testNode = sim.dataTree.resolveNode(nodePath);
                Assert.IsTrue(nodePath == testNode.path);
                Assert.IsTrue(testNode.name == "C");

                sinter_Variable tmp = new sinter_Variable();
                tmp.init(a_sim, sinter_Variable.sinter_IOType.si_DOUBLE, new String[] { nodePath });

                Assert.IsTrue((double)tmp.dfault == 74);
                Assert.IsTrue(tmp.type == sinter_Variable.sinter_IOType.si_DOUBLE);
            }

            {
                String nodePath = "height$B$2";

                VariableTreeNode testNode = sim.dataTree.resolveNode(nodePath);
                Assert.IsTrue(nodePath == testNode.path);
                Assert.IsTrue(testNode.name == "B");

                sinter_Variable tmp = new sinter_Variable();
                tmp.init(a_sim, sinter_Variable.sinter_IOType.si_STRING, new String[] { nodePath });

                Assert.IsTrue((String)tmp.dfault == "Leek");
                Assert.IsTrue(tmp.type == sinter_Variable.sinter_IOType.si_STRING);


            }
            sim.closeSim();
        }


        private ISimulation startupSim(String sinterconf, String defaultsfile, String infilename)
        {
            String workingDir = Path.GetDirectoryName(sinterconf);

            StreamReader defaultsStream = null;
            if (defaultsfile != null)
            {
                defaultsStream = new StreamReader(defaultsfile);
            }
            StreamReader inStream = new StreamReader(infilename);

            //this function returns 0 if no error
            //another integer for errors
            StreamReader inFileStream = new StreamReader(sinterconf);
            string setupString = "";
            setupString = inFileStream.ReadToEnd();
            inFileStream.Close();

            ISimulation stest = sinter_Factory.createSinter(setupString); //Need to change this to setup file contents string
            stest.workingDir = workingDir;
            //stest.readSetup(sinterconf); //Read the setup file also opens sim
            stest.openSim(); //connect to aspen
            stest.Vis = false;
            Console.WriteLine(stest.Vis);
            //      stest.Layout();  //figure out spreadsheet layout
            stest.dialogSuppress = true;
            stest.resetSim();

            //We only use this with Jim's tightly coupled sims
            if (stest is sinter_InteractiveSim)
            {
                sinter_InteractiveSim stest_sim = (sinter_InteractiveSim)stest;
                stest_sim.makeDataTree();
            }
            return stest;
        }
    }
}
