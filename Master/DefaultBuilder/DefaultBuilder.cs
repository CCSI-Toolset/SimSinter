using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sinter;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace DefaultBuilder
{
    class DefaultBuilder
    {

        private static ISimulation stest;

        static public Dictionary<String, Object> getDefaultsFromAspen()
        {
            Dictionary<String, Object> outputDict = new Dictionary<String, Object>();
            for (int ii = 1; ii <= stest.setupFile.countIO; ++ii)
            {
                sinter_Variable outputVal = (sinter_Variable)stest.setupFile.getIOByIndex(ii);
                if (outputVal.isInput)
                {
                    string varname = outputVal.name;
                    outputDict.Add(varname, outputVal.Value);
                    try
                    {
                        Console.WriteLine(outputVal.Value);   //DELETE, just debugging output
                    }
                    catch
                    {
                        Console.WriteLine("Something went wrong with the outputfile....");
                    }
                }
            }
            return outputDict;
        }

        static void printUsage(String message)
        {
            Console.WriteLine(String.Format("ERROR: {0}", message));
            Console.WriteLine();
            Console.WriteLine("  DefaultBuilder is a tool for pulling the default values for the inputs from");
            Console.WriteLine("the simulation, and putting them into a simple json file. It is mostly used");
            Console.WriteLine("for generating a starter set of inputs for ConsoleSinter.");
            Console.WriteLine();
            Console.WriteLine("   DefaultBuilder SinterConfig.json defaults.json");
            Console.WriteLine();
            Console.WriteLine("Arguments: ");
            Console.WriteLine("  SinterConfig.json: ");
            Console.WriteLine("    A Sinter Configuration file is required.  It should reference a simulation ");
            Console.WriteLine("    file located in the same directory.");
            Console.WriteLine("  defaults.json: ");
            Console.WriteLine("    The defaults.json file is the name of the file to write the defaults to. ");
            Console.WriteLine("    Normally this file does not exist before DefaultBuilder is run. ");
            Console.WriteLine("    DefaultBuilder will overwrite any existing file of the same name. ");
            Console.WriteLine();
            Console.WriteLine("ERROR OUTPUT FINISHED. NO DEFAULTS WRITTEN. PRESS ENTER KEY TO EXIT PROGRAM.");
            Console.ReadLine();
        }


        static int Main(string[] args)
        {
            //stest = new sinter_SimAspen();
            String configFileName = "";
            String outfilename = "";
            if (args.Length == 2)
            {
                configFileName = args[0];
                outfilename = args[1];
            } else {
                printUsage(String.Format("2 Arguments are required. {0} were passed in.", args.Length));
                return 2;
            }
            String workingDir = Path.GetDirectoryName(configFileName);

            StreamReader inFileStream = new StreamReader(configFileName);
            string setupString = "";
            setupString = inFileStream.ReadToEnd();
            inFileStream.Close();

            stest = sinter_Factory.createSinterForConfig(setupString);
            stest.workingDir = workingDir;
            stest.openSim(); //connect to aspen
            stest.Vis = false;
            //stest.Layout();  //figure out spreadsheet layout
            stest.dialogSuppress = true;
//            stest.runSim();
            stest.initializeDefaults();
            stest.closeSim();
            Dictionary<String, Object> inputDict = getDefaultsFromAspen();
            Dictionary<String, Object> groupDict = new Dictionary<String, Object>();
            groupDict["inputs"] = inputDict;
            
            JsonSerializerSettings jss = new JsonSerializerSettings();
            jss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Object[] runArray = new Object[1] {groupDict};    
            string jsonOutput = JsonConvert.SerializeObject(runArray, Formatting.Indented, jss);

            StreamWriter outStream = new StreamWriter(outfilename);
            outStream.WriteLine(jsonOutput);
            outStream.Close();
            return 0;
        }
    }
}
