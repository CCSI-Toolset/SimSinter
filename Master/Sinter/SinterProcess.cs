using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sinter
{
    public class SinterProcess
    {

        //Syncronization of running and stopping the simulation.  
        //o_finishedMonitor is a condition variable that is signaled when either the finished (when runSim returns)
//        private Monitor o_finishedMonitor;
        //Set to True when the simulation has completed, (or was stopped successfully)
        private bool o_simFinished = false;
        //Set to true when stopSim is called
        private bool o_stopSim = false;
        //When stop is called, a timeOut is set up. If it times out the stop has failed, and terminate is called.
        private bool o_terminateCalled = false;

        public SinterProcess()
        {
//            o_finishedMonitor = new Monitor();
            o_simFinished = false;
            o_stopSim = false;
            o_terminateCalled = false; 
        }


        /// <summary>
        /// runSeries:
        /// NOTE Implementation Detail WORKING DIRECTORY MUST CONTAIN SinterConfig File
        /// All paths in sinterconfig are relative to working directory, the calling
        /// party MUST have the directory structure completed.
        /// </summary>
        /// <param name="setupString"></param>
        /// <param name="defaultsDict"></param>
        /// <param name="inputsArray"></param>
        /// <param name="workingDir"></param>
        /// <param name="relaunchSim"></param>
        /// <param name="outputsArray"></param>
        /// <param name="runStatuses"></param>
        /// <param name="ts_byRunNumber"></param>
        ///
        public void runSeries(string sinterconf, JObject defaultsDict, JArray inputsArray, 
            bool relaunchSim, int timelimit_in_seconds, ref JArray outputsArray, ref List<sinter_AppError> runStatuses, 
            ref List<List<object>> ts_byRunNumber)
        {
            ISimulation stest = null;

            String workingDir = Path.GetDirectoryName(sinterconf);
            String filenameBase = Path.GetFileNameWithoutExtension(sinterconf);
            String filename = Path.GetFileName(sinterconf);
            StreamReader sinterConfigStream = new StreamReader(sinterconf);
            String setupString = sinterConfigStream.ReadToEnd();
            sinterConfigStream.Close();

            //Just checked here to make sure filename is in working dir
            if (!File.Exists(Path.Combine(workingDir, filename)))
            {
                throw new ArgumentException("Working Directory Must Contain Sinter Configuration");
            }


            //Initialize all the arrays so they contain values we know are fake
            Stopwatch stopwatch = new Stopwatch();
            ts_byRunNumber = new List<List<object>>();
            runStatuses = new List<sinter_AppError>();

            for (int ii = 0; ii < inputsArray.Count; ++ii)
            {
                TimeSpan badSpan = new TimeSpan(9, 9, 9, 9);
                List<object> thisRow = new List<object>();

                thisRow.Add(badSpan);
                thisRow.Add(badSpan);
                thisRow.Add(badSpan);
                thisRow.Add(badSpan);
                thisRow.Add(badSpan);
                thisRow.Add(badSpan);

                ts_byRunNumber.Add(thisRow);
                runStatuses.Add(sinter_AppError.si_SIMULATION_NOT_RUN);
            }



            // Prime the main loop by opening up Sinter and the sim for the first time.
            // After this we may relaunch or just reinit at the end of the sim, depending on what the user wants
            try
            {
                stopwatch.Start();
                stest = sinter_Factory.createSinter(setupString);
                stest.workingDir = workingDir;
                stest.Vis = true;
                stest.dialogSuppress = true;  //Jinliang wants to see the window, but maybe no dialogs is OK
                stest.openSim(); //connect to aspen
                 stopwatch.Stop();
                ts_byRunNumber[0][0] = stopwatch.Elapsed;


            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while opening the simulation: " + ex.Message);
                Console.WriteLine("Subsequent text should be error messages from Simulation.");
                Console.WriteLine("RunStatus: {0}", -1);
                Console.WriteLine("FINISHED. PRESS ENTER KEY TO EXIT PROGRAM.");
                Console.ReadLine();
                stest.closeSim();
                return;
            }

            // Now we start running simulations
            for (int ii = 0; ii < inputsArray.Count; ++ii)
            {
                Console.WriteLine("Starting run {0}", ii);
                try
                {
                    using (StreamWriter w = File.AppendText("runtimings.txt"))
                    {
                        DateTime saveNow = DateTime.Now;
                        string dtString = saveNow.ToString(@"M/d/yyyy hh:mm:ss tt");
                        w.WriteLine("start Run {0}: {1}", ii, dtString);
                    }

                    stopwatch.Restart();
                    if (defaultsDict != null)
                    {
                        stest.sendDefaults(defaultsDict);
                    }
                    JObject thisRun = (JObject)inputsArray[ii]["inputs"];
                    stest.sendInputs(thisRun);
                    stopwatch.Stop();
                    ts_byRunNumber[ii][1] = stopwatch.Elapsed;

                    stopwatch.Restart();
                    stest.sendInputsToSim();
                    stopwatch.Stop();
                    ts_byRunNumber[ii][2] = stopwatch.Elapsed;

                    stopwatch.Restart();

                    o_simFinished = false;
                    o_stopSim = false;
                    o_terminateCalled = false;
                    Thread t = new Thread(() => runSimThread(stest));
                    t.Start();

                    System.TimeSpan polltime = new System.TimeSpan(0, 0, 10);
                    int terminatetime = 60 * 1000;  //Terminate after 1 minute if stop doesn't get us unstack
                    Stopwatch terminateWatch = new Stopwatch();

                    lock (this)
                    {
                        while (!o_simFinished)
                        {
                            Monitor.Wait(this, polltime);  //Timeout to check progress every 10 seconds
                            if (o_simFinished) { break; }

                            if (!o_stopSim && timelimit_in_seconds > 0 && stopwatch.ElapsedMilliseconds > timelimit_in_seconds * 1000)
                            { ///If we haven't already stopped the sim, and we have a run time limit, and that timelimit is excceeded, do this
                                o_stopSim = true;
                                stest.stopSim();
                                terminateWatch.Reset();
                                terminateWatch.Start();
                            }

                            if (o_stopSim && terminateWatch.ElapsedMilliseconds > terminatetime)
                            { //If we have stopped the sim, and the stop time limit has been excceeded
                                o_terminateCalled = true;
                                stest.terminate();
                                terminateWatch.Stop();
                                t.Join();  //Probably uneccesary, when terminate finishes, o_simFinished should be set and we exit.
                                break;
                            }

                            //If none of these cases occured, it's a spurious wake up, and we should just continute polling
                        }
                    }
                    //Time counting etc here


                    stopwatch.Stop();
                    ts_byRunNumber[ii][3] = stopwatch.Elapsed;

                    if (stest.runStatus == sinter_AppError.si_OKAY)  //Should should be enough to protect us in cases of stoppage or termination
                    {
                        stopwatch.Restart();
                        stest.recvOutputsFromSim();
                        stopwatch.Stop();
                        ts_byRunNumber[ii][4] = stopwatch.Elapsed;
                    }

                    using (StreamWriter w = File.AppendText("runtimings.txt"))
                    {
                        DateTime saveNow = DateTime.Now;
                        string dtString = saveNow.ToString(@"M/d/yyyy hh:mm:ss tt");
                        w.WriteLine("end Run {0}: {1}", ii, dtString);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception while running simulation: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("Subsequent text should be error messages from Simulation.");
                }
                runStatuses[ii] = stest.runStatus;

                stopwatch.Restart();
                sinter_HelperFunctions.printErrors(stest);
                outputsArray.Add(stest.getOutputs());
                stopwatch.Stop();
                ts_byRunNumber[ii][5] = stopwatch.Elapsed;


                //Prep for the next run if there is one
                if (ii + 1 < inputsArray.Count)
                {
                    if (relaunchSim == true)
                    {
                        try
                        {
                            stest.closeSim();

                            stopwatch.Start();
                            stest = sinter_Factory.createSinter(setupString);
                            stest.workingDir = workingDir;
                            stest.openSim(); //connect to aspen
                            stest.Vis = false;
                            stest.dialogSuppress = true;
                            stopwatch.Stop();
                            ts_byRunNumber[ii + 1][0] = stopwatch.Elapsed;

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(String.Format("Exception while opening the simulation on run number: {0}", ii));
                            Console.WriteLine(ex.Message);
                            Console.WriteLine("Subsequent text should be error messages from Simulation.");
                            Console.WriteLine("RunStatus: {0}", -1);
                            Console.WriteLine("FINISHED. PRESS ENTER KEY TO EXIT PROGRAM.");
                            Console.ReadLine();
                            return;
                        }
                    }
                    else
                    {
                        stopwatch.Start();
                        stest.resetSim();
                        stopwatch.Stop();
                        ts_byRunNumber[ii + 1][0] = stopwatch.Elapsed;
                    }
                }
            }


            stest.closeSim();
        }


        // This simple thread just runs the simulation, and signals when runSim has returned.  
        private void runSimThread(ISimulation isim)
        {
            lock (this)
            {
                o_simFinished = false;
            }
            try
            {
                isim.runSim();
            }
            finally
            {
                lock (this)
                {
                    o_simFinished = true;
                    Monitor.Pulse(this);  //Signal that sim as finished no matter how we got here.  Terminate is OK.
                }
            }
        }
    }
}
