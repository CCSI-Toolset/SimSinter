using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sinter;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using CSVFileRW;
using CommandLine;
using CommandLine.Text;

namespace ConsoleSinter
{
    class ConsoleSinter
    {
        class Options
        {
            [ValueList(typeof(List<string>), MaximumElements = 3)]
            public IList<string> Args { get; set; }

            [Option('t', "timelimit", Required = false, DefaultValue = -1,
              HelpText = "The timelimit on how long a single simulation may run in seconds.  If the timelimit is exceeded, the simulation is stopped.")]
            public int Timelimit{ get; set; }

            [Option('r', "relaunch", DefaultValue = false,
              HelpText = "Relaunches the simulator between each simulation.")]
            public bool Relaunch { get; set; }

            [HelpOption]
            public string GetUsage()
            {

                var help = new StringBuilder();

                help.Append(HelpText.AutoBuild(this,
      (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current)));
                help.AppendLine();
                help.AppendLine("Positional Arguments: ");
                help.AppendLine("  SinterConfig.json: ");
                help.AppendLine("    A Sinter Configuration file is required.  It should reference a simulation ");
                help.AppendLine("    file located in the same directory.");
                help.AppendLine("  inputs.json: ");
                help.AppendLine("    The json file giving the inputs for each simulation to be run.  It can take ");
                help.AppendLine("    inputs in the old simple format, or the 0.2 format that declares units. ");
                help.AppendLine("    This file also determines how many runs will be done, but providing inputs");
                help.AppendLine("    for each run to be done.  If multiple runs are to be done, every set of");
                help.AppendLine("    inputs must be included in an overall enclosing json array. (ie [] )");
                help.AppendLine("  outputs.json: ");
                help.AppendLine("    The outputs.json file is the name of the file to write the outputs to. ");
                help.AppendLine("    Normally this file does not exist before ConsoleSinter is run. ");
                help.AppendLine("    ConsoleSinter will overwrite any existing file of the same name. ");
                help.AppendLine();
                help.AppendLine("----------------------------------------------------------------------------");
                help.AppendLine("   DESCRIPTION");
                help.AppendLine("----------------------------------------------------------------------------");
                help.AppendLine();
                help.AppendLine("  ConsoleSinter is a tool for running one or more SimSinter simulations on the");
                help.AppendLine("local machine. It runs the simulations serially, one at a time, unlike Turbine,");
                help.AppendLine("which may run many simulations simultaneously.");
                help.AppendLine();
                help.AppendLine("   ConsoleSinter SinterConfig.json inputs.json outputs.json [options]");
                help.AppendLine();
                help.AppendLine();


                return help.ToString();
            }   
        }

        static void printObjErrors(StreamWriter outStream, Dictionary<String, List<String>> errorDict, String aspenObj, String errorType)
        {
            foreach (KeyValuePair<String, List<String>> entry in errorDict)
            {
                outStream.WriteLine("{0} {1} has {2}:", aspenObj, entry.Key, errorType);
                foreach (String errormsg in entry.Value)
                {
                    outStream.WriteLine("1: " + errormsg);
                }
            }
        }


        /// <summary>
        /// Main 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {

            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                return 4;
            }

            if(options.Args.Count != 3) {
                Console.Write(options.GetUsage());
                return 5;
            }

            String sinterconf = options.Args[0];
            String infilename = options.Args[1];
            String outfilename = options.Args[2];

            String workingDir = Path.GetDirectoryName(sinterconf);
            String filenameBase = Path.GetFileNameWithoutExtension(sinterconf);
            String performanceFile = Path.Combine(workingDir, filenameBase + "._time.txt");

            StreamReader inStream = new StreamReader(infilename);

            //So, at the end of this we should have 1 or more input sets in the JArray inputsArray
            //The console version reads in json, the actual version just pulls the dictionary from the database
            string injson = "";
            JToken inputJToken = null;
            JArray inputsArray = null; //Will be set if the input file is a set of runs
            JArray outputsArray = new JArray();
            try
            {
                injson = inStream.ReadToEnd();
                inputJToken = JToken.Parse(injson);
                if (inputJToken is JArray)
                {
                    inputsArray = (JArray)inputJToken;
                }
                else if (inputJToken is JObject)
                {   //Console Sinter is expecting a JArray in the same format as the Gateway takes.  [ { Input = {...} }, { { Inputs = {...} } ]
                        inputsArray = new JArray();
                        JObject singleInputDict = (JObject)inputJToken;
                        JObject outterDict = new JObject();
                        outterDict.Add("inputs", singleInputDict);
                        inputsArray.Add(outterDict);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while running simulation: " + ex.Message);
            }
            finally
            {
                inStream.Close();
            }


            List<sinter_AppError> runStatuses = null;
            List<List<object>> ts_byRunNumber = null;

            //
            // NOTE: Implementation Detail WORKING DIRECTORY MUST CONTAIN SinterConfig File
            //
            SinterProcess sp = new SinterProcess();
            sp.runSeries(sinterconf, null, inputsArray, 
                options.Relaunch, options.Timelimit, ref outputsArray, ref runStatuses, ref ts_byRunNumber);

            JsonSerializerSettings jss = new JsonSerializerSettings();
            jss.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            string jsonOutput = JsonConvert.SerializeObject(outputsArray, Formatting.Indented, jss);
            StreamWriter outStream = new StreamWriter(outfilename);
            outStream.WriteLine(jsonOutput);
            outStream.Close();

            CsvFileWriter csvWriter = new CsvFileWriter(performanceFile);
            string headerString = "Open Time, Sinter Sent Time, Send Time, Run Time, Receive Time, Sinter Recieve Time";
            csvWriter.WriteLine(headerString);

            for (int ii = 0; ii < inputsArray.Count; ++ii)
            {
                csvWriter.WriteRow((List<object>)ts_byRunNumber[ii]);
            }
            csvWriter.Close();

            Console.WriteLine("FINISHED. PRESS ENTER KEY TO EXIT PROGRAM.");
            Console.ReadLine();
            return 0;
        }
    }
}


