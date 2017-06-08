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
    class CSVConsoleSinter
    {

        class Options
        {
            [ValueList(typeof(List<string>), MaximumElements = 3)]
            public IList<string> Args { get; set; }

            [Option('t', "timelimit", Required = false, DefaultValue = -1,
              HelpText = "The timelimit on how long a single simulation may run in seconds.  If the timelimit is exceeded, the simulation is stopped.")]
            public int Timelimit { get; set; }

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
                help.AppendLine("  inputs.csv: ");
                help.AppendLine("    The input csv file giving the inputs for each simulation to be run.  Each ");
                help.AppendLine("    row is a simulation, and each column is an input variable.  The first row");
                help.AppendLine("    gives the names of each input.  Those input names must be in the sinter");
                help.AppendLine("    config file.");
                help.AppendLine("  outputs.csv: ");
                help.AppendLine("    The outputs.csv file must exist.  Before CSVConsoleSinter is run, it should");
                help.AppendLine("    only contain a single row, the names of the output variables to extract.");
                help.AppendLine("    After CSVConsoleSinter is run, each simulation will be a row with the");
                help.AppendLine("    columns being the output variables. ");
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
                help.AppendLine("  CSVConsoleSinter is a tool for running one or more SimSinter simulations on the");
                help.AppendLine("local machine. It runs the simulations serially, one at a time, unlike Turbine,");
                help.AppendLine("which may run many simulations simultaneously.");
                help.AppendLine("  CSVConsoleSinter differs from ConsoleSinter in that CSVConsoleSinter takes the");
                help.AppendLine("input variables from a CSV file, and outputs a CSV file, rather than using json.");
                help.AppendLine();
                help.AppendLine("   CSVConsoleSinter SinterConfig.json inputs.csv outputs.csv [options]");
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

        static JArray parseCSVInputs(string infilename)
        {
            CsvFileReader inStream = new CsvFileReader(infilename);
            try
            {
                //Now parse the inputs.  They are in csv format, so read a names into an array, and build a dictionary of each input set.
                //So, at the end of this we should have 1 or more input sets in the JArray inputsArray

                JArray inputsArray = new JArray(); //Will be set if the input file is a set of runs
                JArray outputsArray = new JArray();

                List<object> headers = new List<object>();
                inStream.ReadRow(headers);
                int lineNum = 1;
                List<object> data = new List<object>();
                while (inStream.ReadRow(data))
                {
                    lineNum++; //Keep track of which line we're on for error checking
                    JObject thisRunInputsDict = new JObject();
                    JObject outterDict = new JObject();
                    if (headers.Count != data.Count)
                    {
                        throw new System.IO.IOException(String.Format("Data line {0} has incorrect number of columns.  Headers: {1} This Line: {2}", lineNum, headers.Count, data.Count));
                    }
                    for (int ii = 0; ii < headers.Count; ++ii)
                    {
                        thisRunInputsDict.Add((string)headers[ii], Convert.ToDouble(data[ii]));  //Should allow of strings I suppose 
                    }
                    outterDict.Add("inputs", thisRunInputsDict);
                    inputsArray.Add(outterDict);

                }

                return inputsArray;
            }
            finally
            {
                inStream.Close();
            }

        }


        static void writeOutCsv(string outfilename, JArray outputsArray)
        {
            CsvFileReader inStream = new CsvFileReader(outfilename);
            List<object> headers = new List<object>();
            inStream.ReadRow(headers);
            inStream.Close();

            CsvFileWriter outStream = new CsvFileWriter(outfilename);
            outStream.WriteRow(headers);
            foreach(JObject run in outputsArray) {
                JObject thisOutputs = (JObject)run["outputs"];
                List<object> csvOutputs = new List<object>();
                for (int ii = 0; ii < headers.Count; ++ii)
                {
                    csvOutputs.Add(thisOutputs[headers[ii]]["value"]);
                }
                outStream.WriteRow(csvOutputs);
            }
            outStream.Close();
        }


        static int Main(string[] args)
        {
            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                return 4;
            }

            if (options.Args.Count != 3)
            {
                Console.Write(options.GetUsage());
                return 5;
            }

            String sinterconf = options.Args[0];
            String infilename = options.Args[1];
            String outfilename = options.Args[2];

            String workingDir = Path.GetDirectoryName(sinterconf);
            String filenameBase = Path.GetFileNameWithoutExtension(sinterconf);
            String performanceFile = Path.Combine(workingDir, filenameBase + "._time.txt");

            JArray inputsArray = parseCSVInputs(infilename);
            JArray outputsArray = new JArray();

            List<sinter_AppError> runStatuses = null;
            List<List<object>> ts_byRunNumber = null;

            SinterProcess sp = new SinterProcess();
            sp.runSeries(sinterconf, null, inputsArray, options.Relaunch, options.Timelimit, ref outputsArray, ref runStatuses, ref ts_byRunNumber);

            writeOutCsv(outfilename, outputsArray);

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


