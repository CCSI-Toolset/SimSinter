using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using VariableTree;

namespace sinter
{
    /// <summary>
    ///   Define constants that indicate the status of a simulation.
    /// </summary>
    public enum sinter_AppError
    {
        si_OKAY = 0,
        si_SIMULATION_ERROR = 1,
        si_SIMULATION_WARNING = 2,
        si_COULD_NOT_CONTACT = 3,
        si_UNKNOWN_FIELD = 4,
        si_INPUT_ERROR = 5,
        si_SIMULATION_NOT_RUN = 6,
        si_SIMULATION_STOPPED = 7,  //The user stopped the simulation
        si_STOP_FAILED = 8,         //The user tried to stop the sim, but the stop timed out.  Terminate should probably be called.
        si_COM_EXCEPTION = 100

    }

    /// <summary>
    ///   Gives the current status of the simulator
    /// </summary>
    public enum sinter_simulatorStatus
    {
        si_CLOSED = 0,
        si_INITIALIZING = 1,
        si_OPEN = 2,
        si_RUNNING = 3,
        si_ERROR
    }

    /// <summary>
    ///   Options for version number checking compliance
    /// </summary>
    public enum sinter_versionConstraint
    {
        version_ANY = 0,      //Any version of the simulator is allowed, will always launch the newest
        version_ATLEAST = 1,  //Any version >= to the named version is OK, will always launch the newest, will throw exception if the newest is < the recommended version
        version_REQUIRED = 2,  //This simulation REQUIRES a particular simulator version, that version will be launched, or an exception thrown.
        version_RECOMMENDED = 3  //This simulation is RECOMMENDED to run on a particular version, but if that isn't availible, use the newest availible, as long as it's newer 
    }

    
    /// <summary>
    ///    The ISimulation is meant to be a common interface for a number
    ///    of numerical simulations. In particular, we are interested in
    ///    numerical simulations of carbon capture processes; however,
    ///    the interface could apply to a wider range of chemical processes.
    /// </summary>
    public interface ISimulation
    {

        /// <summary>
        /// The setupFile provides access to the setup file
        /// used to configure this simulation.
        /// </summary>
        sinter_SetupFile setupFile
        {
            get;
            set;
        }

        /// <summary>
        ///    The process ID property holds a process identifier provided
        ///    by the operating system for a child process that supports
        ///    this simulation. If no child process is currently running,
        ///    this will return -1.
        /// </summary>
        int ProcessID
        {
            get;
        }

        /// <summary>
        ///   This boolean is normally false except when the ISimulation
        ///   is launching a simulation subprocess to perform its expected
        ///   role. For example, this is try while Aspen Plus, Aspen Custom
        ///   Modeler, or MS Excel is being launch.
        /// </summary>
        bool IsInitializing
        {
            get;
        }

        /// <summary> 
        ///   Simulations are given a working directory in which to work (i.e.,
        ///   where their input and output files should be stored). Typically,
        ///   the code that creates the simulation will initialize the 
        ///   working directory and store the simulations input files in
        ///   that directory. Normally, this is set immediately after the
        ///   ISimulation is created and should definitely be set before
        ///   openSim() is called.
        /// </summary>
        string workingDir
        {
             get;
             set;
        }

        /// <summary>
        ///   Some simulations may have visual elements such as GUIs or 
        ///   graphical output. This boolean directs whether those visuals
        ///   should appear (if Vis is true) or be kept invisible (if
        ///   Vis is false). If a simulation has no GUI or graphical
        ///   output, setting Vis to true has no effect.  The default
        ///   value is simulation dependent, and this is normally set
        ///   after calling openSim().
        /// </summary>
        bool Vis
        {
             get;
             set;
        }

        /// <summary>
        ///   Some simulations can display dialog boxes during the simulation
        ///   to indicate the simulation status to the user. However, during
        ///   a batch run, there maybe nobody present to click on the "Ok"
        ///   button. The dialogSuppress property controls whether dialog
        ///   boxes should be presented (if dialogSuppress is false) or
        ///   whether dialog boxes should be withheld (if dialogSuppress is 
        ///   true). Simulations without graphical elements can safely
        ///   ignore this setting.  The default value of this is simulation
        ///   dependent, and this property should be set after openSim()
        ///   has been called.
        /// </summary>
        bool dialogSuppress
        {
            get;
            set;
        }

        /// <summary>
        ///   Indicate the status of the most recent simulation execution.
        ///   Before a simulation runs, this should have the value si_OKAY.
        /// </summary>
        sinter_AppError runStatus
        {
            get;
        }

        /// <summary>
        ///   Indicate the status of the simulator.
        /// </summary>
        sinter_simulatorStatus simulatorStatus
        {
            get;
        }

        /// <summary>
        ///    For simulation types that connect to an interactive simulation
        ///    engine (e.g., Aspen Plus, Aspen Custom Modeler, or Excel), this
        ///    method opens a live connection to a running instance of the
        ///    simulation. Regardless of simulation type, openSim() must
        ///    be called before setting the Vis or dialogSuppress properties.
        ///    Similarly, openSim() must be called before calls to
        ///    resetSim(), runSim(), errorsBasic(), warningsBasic(),
        ///    sendInputs(), sendDefaults(), or getOutputs().
        /// </summary>
        /// <exception cref="sinter.sinter_SimulationException">
        /// This exception is thrown if the sinter configuration is incompatible, incomplete, or
        /// refers to files that do not exist or won't open, etc.
        /// </exception>
        void openSim();  

        /// <summary>
        ///   This method is the reverses the operation of openSim(). If the
        ///   simulation is connected to a running instance of a simulation,
        ///   this method will shutdown the child process and close the
        ///   connection. Once this method has been call, the object returns
        ///   to a state similar to the object before openSim(). Before any
        ///   useful work can be done this this object, openSim() must be
        ///   called again.
        /// </summary>
        void closeSim();

        /// <summary>
        ///    This method will restore all the variables in the simulation
        ///    to their default values.  openSim() must be executed before this
        ///    method is run.
        /// </summary>
        void restoreDefaults();

        /// <summary>
        ///    This method will clear any error conditions in the simulation
        ///    and attempt to reinitialize the model to its default state.
        ///    The default state means that the variables are back to their
        ///    initial conditions. In some cases, it may not change the
        ///    variable values.  openSim() must be executed before this
        ///    method is run.
        /// </summary>
        /// <returns>
        ///    The return value of this method indicates whether the
        ///    simulation reset was successful, and if not, it provides some
        ///    information about what kind of failure occurred.  si_OKAY 
        ///    indicates that everything was successful.
        /// </returns>
        sinter_AppError resetSim();

        /// <summary>
        ///   Run the calculation engine with the current input values and
        ///   generate output values. A typical execution would be openSim(),
        ///   sendInputs(), sendInputsToSim(), runSim(), recvOutputsFromSim,
        ///   and then getOutputs().  openSim() must be called before this 
        ///   method is run.
        /// </summary>
        /// <returns>
        ///   The return value of this method indicates whether the simulation
        ///   run was successful, and if not, it provides some information
        ///   about what kind of failure occurred.  si_OKAY indicates that
        ///   everything was successful.
        /// </returns>
        sinter_AppError runSim();

       
        /// <summary>
        ///    In the event that errors occurred during the simulation,
        ///    this method provides textual information from the simulation
        ///    to describe the nature of the error. The output and its format
        ///    are completely simulation specific.
        /// </summary>
        /// <returns>
        ///    A list of strings containing the diagnostic information from
        ///    the simulation.
        /// </returns>
        string[] errorsBasic();

        /// <summary>
        ///    In the event that warnings occurred during the simulation,
        ///    this method provides textual information from the simulation
        ///    to describe the nature of the warning. The output and its format
        ///    are completely simulation specific.
        /// </summary>
        /// <returns>
        ///    A list of strings containing the diagnostic information from
        ///    the simulation.
        /// </returns>
        string[] warningsBasic();

        /// <summary>
        ///   Set a subset of the input variable values in the simulation using
        ///   the values in inputDict. Each input variable has two attributes
        ///   -- its current value and its default value. This sets the
        ///   current value. Despite its name depending on the simulation, 
        ///   this method may or may not change the value in the running
        ///    simulation engine. Normally, this is called before runSim().
        /// </summary>
        /// <exception cref="System.IO.IOException">
        ///   This exception is raised if a variable in the inputDict does
        ///   not match its specification in the model. For example, setting
        ///   an output variable can raise this exception. Providing a 
        ///   scalar value for an array quantity can cause this exception.
        ///   Including a variable in inputDict that the simulation does not
        ///   know about does NOT raise an exception. It is ignored.
        /// </exception>
        void sendInputs(JObject inputDict);

        /// <summary>
        ///   This call actually orders Sinter to set the variables in the simulation
        ///   to the value provided by the user.  This is split out from either sendInputs
        ///   or runSim because sometimes some tools and special cases require extra work.
        ///   Or don't want to call runSim.  see the comment of runSim for how this fits 
        ///   into a normal run sequence.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        ///   This exception is raised if the simulation has some issue with the variable
        ///   provided.  This is simulation specific, but should be rare.
        /// </exception>
        void sendInputsToSim();


        /// <summary> 
        ///   Set a subset of the input variable defaults in
        ///   the simulation using the values in inputDict. Each input
        ///   variable has two attributes -- its current value and its
        ///   default value. This sets the default value. Despite its
        ///   name depending on the simulation, this method may or may
        ///   not change the value in the running simulation engine.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        ///   This exception is raised if a variable in the inputDict does
        ///   not match its specification in the model. For example, setting
        ///   an output variable can raise this exception. Providing a 
        ///   scalar value for an array quantity can cause this exception.
        ///   Including a variable in inputDict that the simulation does not
        ///   know about does NOT raise an exception. It is ignored.
        /// </exception>
        void sendDefaults(JObject inputDict);

        /// <summary>
        ///   This call actually orders Sinter to get the output variables from the simulation.
        ///   This is split out from either getOutputs or runSim because sometimes some tools 
        ///   and special cases require extra work.
        ///   For some tools we don't even want to call runSim.
        ///   See the comment of runSim for how this fits into a normal run sequence.
        /// </summary>
        /// <exception cref="System.IO.IOException">
        ///   This exception is raised if one the output variables doesn't actually exist.
        /// </exception>
        void recvOutputsFromSim();


        /// <summary>
        ///    This returns the simulation outputs as a JObject dictionary.
        ///    This is only meaningful after runSim() has been called.
        /// </summary>
        /// <returns>
        ///    this returns a JSON dictionary (JObject) holding the values
        ///    of the simulations output variables.
        /// </returns>
        JObject getOutputs();

        /// <summary>
        /// This function has all variables request their units from their simulations.
        /// This it useful for generating configuration files.
        /// </summary>
//        void initializeUnits();

        /// <summary>
        /// This function gets the orginal values for all the input variables from 
        /// the simulation.  After this call, value and defaults for each input variable
        /// will be the same, whatever was in the simulation.
        /// </summary>
        void initializeDefaults();


        /// <summary>
        ///    This method will terminate the simulation child process
        ///    that is supporting this simulation object. This may not
        ///    make a clean shutdown, so in general, you should resetSim()
        ///    before runSim() again. You should still closeSim() before
        ///    abandoning the object.
        ///    returns true on success (or finding the process has already been destroyed)
        ///    returns false or exception on failure.
        /// </summary>
        bool terminate();

        /// <summary>
        ///    This method will stop (pause) the simulation if the simulator supports 
        ///    it.  Otherwise the call is just ignored.
        /// </summary>
        void stopSim();

        /// <summary>
        ///    This gets the simulation DataTree.  A tree showing all 
        ///    available variables in the simulation.
        /// </summary>
        VariableTree.VariableTree dataTree { get; }

        /// <summary>
        ///    Different simulators have different keys in the setup file.
        ///    Aspen -> aspenfile, excel -> spreadsheet
        /// </summary>
        string simName
        { get; }
        string simVersion
        { get; }

        //The setup file can require a particular version.  This is that version number.
        string simVersionRecommendation
        { get; set; }

        sinter_versionConstraint simVersionConstraint
        { get; set; }

    }   
}
