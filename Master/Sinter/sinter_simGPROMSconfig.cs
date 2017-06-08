using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Security.Principal;
using Newtonsoft.Json.Linq;
using VariableTree;
using System.Text.RegularExpressions;

namespace sinter.PSE
{

    #region data-classes

    public class variableType :ICloneable
    {
        public string name;
        public string units;
        public double min;
        public double max;
        public double dfault;
        public variableType(string in_name, string in_units, double in_min, double in_max, double in_default)
        {
            name = in_name;
            units = in_units;
            min = in_min;
            max = in_max;
            dfault = in_default;
        }
        public variableType(string in_name, string in_units, string in_min, string in_max, string in_default)
        {
            name = in_name;
            units = in_units;
            min = Convert.ToDouble(in_min);
            max = Convert.ToDouble(in_max);
            dfault = Convert.ToDouble(in_default);
        }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class variableDecleration : ICloneable
    {
        public String name;
        public String typeName;
        public sinter.sinter_Variable.sinter_IOType type;
        public IOMode mode;  //At this point used to differentiate parameters (IN) from varaibles (IN/OUT).
        public int arraySize;

        public string units;  //Optional variable type data that may come from a variableType or from extra info on the variable decleration line
        public object min;
        public object max;
        public object dfault;
        public variableDecleration(string in_name, string in_typeName, int in_arraySize)
        {
            name = in_name;
            typeName = in_typeName;
            arraySize = in_arraySize;
            type = sinter_Variable.sinter_IOType.si_UNKNOWN;
            units = "";
            if (arraySize == -1)
            {
                if (typeName == "REAL")
                {
                    type = sinter_Variable.sinter_IOType.si_DOUBLE;
                    mode = IOMode.si_IN; 
                    min = 0.0;
                    max = 0.0;
                    dfault = 0.0;
                }
                else if (typeName == "INTEGER")
                {
                    type = sinter_Variable.sinter_IOType.si_INTEGER;
                    mode = IOMode.si_IN;
                    min = 0;
                    max = 0;
                    dfault = 0;
                }
                else if (typeName == "STRING")
                {
                    type = sinter_Variable.sinter_IOType.si_STRING;
                    mode = IOMode.si_IN;
                    min = "";
                    max = "";
                    dfault = "";
                }
                else if (typeName == "FOREIGN_OBJECT")
                {
                    type = sinter_Variable.sinter_IOType.si_STRING;
                    mode = IOMode.si_NONE;
                    min = "";
                    max = "";
                    dfault = "";
                }
                else  //Variables have typenames that are user defined, but their literal type is always REAL
                {
                    type = sinter_Variable.sinter_IOType.si_DOUBLE;
                    mode = IOMode.si_INOUT;
                    min = 0.0;
                    max = 0.0;
                    dfault = 0.0;
                }
            }
            else
            {
                if (typeName == "REAL")
                {
                    type = sinter_Variable.sinter_IOType.si_DOUBLE_VEC;
                    mode = IOMode.si_IN;
                    min = 0.0; // new double[arraySize];
                    max = 0.0; //new double[arraySize];
                    dfault = 0.0; //new double[arraySize];
                }
                else if (typeName == "INTEGER")
                {
                    type = sinter_Variable.sinter_IOType.si_INTEGER_VEC;
                    mode = IOMode.si_IN;
                    min = 0; // new int[arraySize];
                    max = 0; // new int[arraySize];
                    dfault = 0; // new int[arraySize];
                }
                else if (typeName == "STRING")
                {
                    type = sinter_Variable.sinter_IOType.si_STRING_VEC;
                    mode = IOMode.si_IN;
                    min = ""; // Enumerable.Repeat("", arraySize).ToArray();
                    max = ""; // Enumerable.Repeat("", arraySize).ToArray();
                    dfault = ""; // Enumerable.Repeat("", arraySize).ToArray();
                }
                else  //Variables have typenames that are user defined, but their literal type is always REAL
                {
                    type = sinter_Variable.sinter_IOType.si_DOUBLE_VEC;
                    mode = IOMode.si_INOUT;
                    min = 0.0; // new double[arraySize];
                    max = 0.0; //new double[arraySize];
                    dfault = 0.0; // new double[arraySize];
                }
            }
        }

        //This function finds the type of a given variable, and initializes the related data (min, max, default, units)
        //It returns true if this process was successful, and the variable should be added to the variable tree
        //It returns false if it could not find that variable type, in this case the variable type is probably included from and 
        //external library.  Since we don't know the type, skip this variable.
        //NOTE: We could probably just assume the type is REAL and has no units.
        public bool initVarType(Dictionary<String, variableType> vartypes)
        {   
            //User defined type as scalar
            if (typeName == "ORDERED_SET")
            {
                return false;
            }
                if (type == sinter_Variable.sinter_IOType.si_DOUBLE && typeName != "REAL")
                {
                    variableType thisVarType = null;
                    if(vartypes.ContainsKey(typeName)) {
                        thisVarType = vartypes[typeName];
                    } else {
                        return false;
                        //throw new ArgumentException(String.Format("Variable type {0} not found.", typeName));
                    }
                    units = thisVarType.units;
                    min = thisVarType.min;
                    max = thisVarType.max;
                    if ((double)dfault == 0.0)  //The user may have set the default in the declaration, so check that before setting it to the type default
                    {
                        dfault = thisVarType.dfault;
                    }
                    return true;
                }
                //User defined type vector
                else if (type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC && typeName != "REAL")
                {
                    variableType thisVarType = null;
                    if(vartypes.ContainsKey(typeName)) {
                        thisVarType = vartypes[typeName];
                    } else {
                        return false;
                        //                        throw new ArgumentException(String.Format("Variable type {0} not found.", typeName));
                    }
                    units = thisVarType.units;
                    min = thisVarType.min;//  Enumerable.Repeat(thisVarType.min, arraySize).ToArray();
                    max = thisVarType.max;//  Enumerable.Repeat(thisVarType.max, arraySize).ToArray();
                    if ((double)dfault != 0.0)  //The user may have set the default in the declaration, so check that before setting it to the type default
                    {
                        dfault = thisVarType.dfault;
                    }
                    return true;
                }
            return true;
        }

/*  I think this is crap
                            dfault = Enumerable.Repeat(dfault, arraySize).ToArray(); 


                bool defaultSet = false;  //If all the default array values are 0, the user hasn't set a special default.  (Not sure if possible for arrays anyway?)
                double[] dfaultArray = (double[]) dfault; 
                for (int ii = 0; ii < arraySize; ++ii)
                {
                    if (dfaultArray[ii] != 0)
                    {
                        defaultSet = true;
                        break;
                    }
                }

                if (!defaultSet)  //The user may have set the default in the declaration, so check that before setting it to the type default
                {
        */

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    /**
     * Similar to IOMode from sinter_Variable, but special for gPROMSconfig to describe some additional
     * modes that SimSinter doesn't really support.
     * */
    public enum IOMode
    {
      // this type describes wheather a variable is input or output for a simulation
      si_IN,
      si_OUT,
      si_INOUT,
      si_NONE,  //some gPROMS variables cannot be set or read by SimSinter
      si_UNKNOWN
    }


    /**
     * Variable Assignment from the Process section of the gPROMS file.
     * Variable Assignments can be very complex.  They are basically of the 
     * form:
     * <gPROMS PATH> := Value;
     * But Value may be a number, string, foriegn object type, or a value from a 
     * foriegn object.  
     * We are interested in the last case, the other cases are largely useless to
     * us because the user cannot set them.  So we are interested in values that
     * come from foriegn objects.  They look like this:
     * <gPROMS PATH> := <ForiegnObject Path>.Type__<input path>
     * or this:
     * <gPROMS PATH> := <ForiegnObject Path>.<input path>
     * 
     * The <input path> is an interesting problem.  For some reason gPROMS gives
     * input variables a different, shorter name, when coming from the foriegn object.
     * Also, to properly identify and parse this, we need to know the Foriegn Object Path.  
     * 
     * One final complication is that the <gPROMS PATH> may be an array reference.
     * For the first phase, I plan to only allow arrays to be set all at once.  So repeated references will 
     * be ignored.
     * **/
    public class variableAssignment : ICloneable
    {
        public String gPROMSPath;
        public String assignmentValue; //May be a double, int, a string, a foriegn object type, a variable, or a value from a foriegn object.  
        //        public sinter.sinter_Variable.sinter_IOType type;
        //        public sinter.sinter_Variable.sinter_IOMode mode;  //At this point used to differentiate parameters (IN) from varaibles (IN/OUT).
        public bool isVec;  //We parse out an ignore all the array infromation, so we just know if it was an array reference
        public IOMode mode; //May be in (parameter or ASSIGN), in/out (INITIAL), neither (params with constant assignments), or out (all variables not in ASSIGN)
        public String inputPath; //Parsed out later.  If this is an input variable, this is the name for it in the foriegn object
        public variableAssignment(string in_gPath, String in_value, bool in_isVec)
        {
            gPROMSPath = in_gPath;
            assignmentValue = in_value;
            isVec = in_isVec;
            mode = IOMode.si_UNKNOWN;  // We don't have quite enough information to determine this yet.  (Need to know the section)

            inputPath = "";
        }

        /**
         * This initialization function must be run after the input foriegn object path is discovered.
         * One we know that, we can decide if this variable is an input variable or not, and fully classify it's mode.
         **/
        public void init(string sectionName, string inputFOPath)
        {

            //Now we can use the inputFOPath to determine exactly the correct mode of the variable:
            
            //If there is N input path, or If the assignmentValue is shorter than the FO pathname, it is definately not an input.
            if (inputFOPath != "" && assignmentValue.Count() > inputFOPath.Count())
            {
                String maybePath = assignmentValue.Substring(0, inputFOPath.Count());
                if (String.Equals(maybePath, inputFOPath, StringComparison.OrdinalIgnoreCase))  //If it starts with the FO path, it is (somekind) of input.
                {
                    mode = IOMode.si_IN;
                    inputPath = assignmentValue.Substring(inputFOPath.Count() + 1);  //Gives us the actual input path for the simplier style, if it's the __ style we need more parsing (below)
                }
                else
                {
                    mode = IOMode.si_OUT;  //Just temporary, we know it's not an input, but i could still be either OUT or NONE.  So assign it OUT for now, and determine exactly next.  
                }
            }
            else
            {
                mode = IOMode.si_OUT; //Just temporary, we know it's not an input, but i could still be either OUT or NONE.  So assign it OUT for now, and determine exactly next.  
            }


            //Now we know if it's an input.  so try to parse out the inputPath.  There are two kinds:
            // <gPROMS PATH> := <ForiegnObject Path>.Type__<input path>
            // or this:
            // <gPROMS PATH> := <ForiegnObject Path>.<input path>  This one is always a single REAL
            //The inputPath is already correct for the second one, the first needs a bit of parsing.
            if (mode == IOMode.si_IN)
            {
                try
                {
                    //If the input path is the longer kind
                    if (inputPath.Contains("__"))
                    {
                        int pFrom = assignmentValue.IndexOf("__") + 2;
                        int pTo = assignmentValue.LastIndexOf("(");
                        if (pTo != -1 && pFrom != -1 && pTo > pFrom)  //Make sure we found something, and that it's reasonable  
                        {
                            inputPath = assignmentValue.Substring(pFrom, pTo - pFrom);
                        }
                    }
                }
                catch (Exception) { }
            }



            switch (sectionName)
            {
                //Set, selector, and assign can only be input varaibles, so if the FO isn't involved, they are nothing to us. 
                case "SET":
                    if (mode != IOMode.si_IN)
                    {
                        mode = IOMode.si_NONE;
                    }
                    break;

                case "INITIALSELECTOR":
                    if (mode != IOMode.si_IN)
                    {
                        mode = IOMode.si_NONE;
                    }
                    break;

                case "ASSIGN":
                    if (mode != IOMode.si_IN)
                    {
                        mode = IOMode.si_NONE;
                    }
                    break;

                case "INITIAL":  //INITIAL variables also can be outputs.
                    if (mode == IOMode.si_IN)
                    {
                        mode = IOMode.si_INOUT;
                    }
                    else
                    {
                        mode = IOMode.si_OUT;
                    }
                    break;

                default:
                    throw new System.ArgumentException(String.Format("setMode: Unknown section block {0}", sectionName));
            }

            return;
        }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }


    /**
     * For our purposes models consist of a name, a list of submodels, and a list of variables 
     * (the variables consist of both Parameters and Variables, in a shared variableDecleration class)
     **/
    public class Model : ICloneable
    {
        public String typename;    //What the model's name (AKA typename)
        public Dictionary<String, variableDecleration> variables; //The variables the model contains
        public Dictionary<String, Model> submodels;  //Sub models this model contains
        public Model(String in_typename, Dictionary<String, Model> in_submodels, Dictionary<String, variableDecleration> in_variables)
        {
            typename = in_typename;
            variables = in_variables;
            submodels = in_submodels;
        }
        public object Clone()
        {
            return this.MemberwiseClone();
        }

    }

    /**
     * For our purposes a GProcess consist of a name, a list of submodels, a list of variables specific to the GProcess,
     * and a list of assigned variables.  
     * The assigned status of the variables tells us if they are declared inputs, declared outputs, or both, or neither.
     **/
    public class GProcess : ICloneable
    {
        public String name;
        public Dictionary<String, variableDecleration> variables;
        public List<variableAssignment> assignments;
        public Dictionary<String, Model> submodels;
        public String inputFOpath;  //The path of the variable this process uses for SimSInter input.  (May not exist!)
        public GProcess(String in_name, Dictionary<String, Model> in_submodels,
                Dictionary<String, variableDecleration> in_variables, List<variableAssignment> in_assignments,
                String in_FOpath)
        {
            name = in_name;
            submodels = in_submodels;
            variables = in_variables;
            assignments = in_assignments;
            inputFOpath = in_FOpath;
        }
        public object Clone()
        {
            return this.MemberwiseClone();
        }

    }

    #endregion data-classes

    /**
     * sinter_simGPROMSconfig is for configuring gPROMS simulations with SinterConfigGUI.
     * There are basically 2 completely seperate actions SimSinter does with a simulation:
     * 1. Configuring the simulation with SinterConfigGUI
     * 2. Running the simulation
     * Interactive Simulations like Aspen can combine these actions, but gPROMS is run as a
     * batch process, and cannot be linked to to query information about the simulation.  
     * So the run and configure steps are complete seperate.  So we have 2 classes for them:
     * simGPROMSconfig: Parses a GPJ file to learn about the availible input output variables and configure the sim.
     * simGPROMS: Takes a configured simulation and runs it with the user supplied inputs
     **/
    public class sinter_simGPROMSconfig : sinter_Sim
    {
        #region data

        /**
         * The name of the process in this gPROMS file that is going to be run.  This setting is absolutely required.
         * We have nothing to run without a processName.
         * The process is also parsed to find the input and output variables.  We can't do anything without it, so 
         * a guess is made with sinterConfigGUI starts up.  (The user may change it, and then all the input variables 
         * must change.
         **/
        String o_processName;

        /**
         * The password used to encrypt the gENCRYPT file.  It may not be necessary (more testing is needed).  
         * SimSinter can also make a guess at it as the default is based on the gPJ filename.
         **/
        String o_password;

        /**
         * gPROMS variables (as opposed to parameters) always have a type that describes units, min, max, etc.
         * their primative type is always REAL.
         **/
        Dictionary<string, variableType> variableTypes = new Dictionary<string, variableType>();

        /**
         * For our purposes models consist of a name, a list of submodels, and a list of variables 
         * (the variables consist of both Parameters and Variables, in a shared variableDecleration class)
         **/
        Dictionary<string, Model> models = new Dictionary<string, Model>();

        /**
         * For our purposes a GProcess consist of a name, a list of submodels, a list of variables specific to the GProcess,
         * and a list of assigned variables.  
         * The assigned status of the variables tells us if they are declared inputs, declared outputs, or both, or neither.
         **/
        public Dictionary<string, GProcess> gProcesses = new Dictionary<string, GProcess>();

       


        //Need here: 
        //Array of lines from GPJ File
        //xmlDoc of GPJ file
        //dictionary of types
        //Parameters
        //Variables
        //VariableTree (extended)
        //VariableTree (limited)

        enum ModelKeywordsEnum
        {
            UNKNOWN = -1, 
            ASSIGN = 0,
            BOUNDRY,
            DISTRIBUTION_DOMAIN,
            EQUATION,
            INITIAL,
            INITIALISATION_PROCEDURE,
            INITIALSELECTOR,
            PARAMETER,
            PORT,
            PORTSET,
            PRESET,
            SELECTOR,
            SET,
            TOPOLOGY,
            UNIT,
            VARIABLE
        }

        enum ProcessKeywordsEnum
        {
            UNKNOWN = -1,
            ASSIGN = 0,
            BOUNDRY,
            EQUATION,
            INITIAL,
            INITIALISATION_PROCEDURE,
            INITIALSELECTOR,
            MONITOR,
            PARAMETER,
            PORT,
            PORTSET,
            PRESET,
            SCHEDULE,
            SELECTOR,
            SET,
            SOLUTIONPARAMETERS,
            UNIT,
            VARIABLE
        }

        string[] ModelKeywords = {
                                    "ASSIGN",
                                    "BOUNDRY",
                                    "DISTRIBUTION_DOMAIN",
                                    "EQUATION",
                                    "INITIAL",
                                    "INITIALISATION_PROCEDURE",
                                    "INITIALSELECTOR",
                                    "PARAMETER",
                                    "PORT",
                                    "PORTSET",
                                    "PRESET",
                                    "SELECTOR",
                                    "SET",
                                    "TOPOLOGY",
                                    "UNIT",
                                    "VARIABLE"
                                 };

        string[] ProcessKeywords = {
                                    "ASSIGN",
                                    "BOUNDRY",  
                                    "EQUATION",
                                    "INITIAL",
                                    "INITIALISATION_PROCEDURE",
                                    "INITIALSELECTOR",
                                    "MONITOR", 
                                    "PARAMETER",
                                    "PORT",
                                    "PORTSET",
                                    "PRESET",
                                    "SCHEDULE",
                                    "SELECTOR",
                                    "SET",
                                    "SOLUTIONPARAMETERS",
                                    "UNIT",
                                    "VARIABLE"
                                   };

        //This regex is used to find the end of sections we're interested in.
        string AllSectionsPattern = @"(^|\s)ASSIGN\s|(^|\s)BOUNDRY\s|(^|\s)DISTRIBUTION_DOMAIN\s|(^|\s)EQUATION\s|(^|\s)INITIAL\s|(^|\s)INITIALISATION_PROCEDURE\s|(^|\s)INITIALIZATION_PROCEDURE\s|(^|\s)INITIALSELECTOR\s|(^|\s)MONITOR\s|(^|\s)PARAMETER\s|(^|\s)PORT\s|(^|\s)PORTSET\s|(^|\s)PRESET\s|(^|\s)SCHEDULE\s|(^|\s)SELECTOR\s|(^|\s)SET\s|(^|\s)SOLUTIONPARAMETERS\s|(^|\s)TOPOLOGY\s|(^|\s)UNIT\s|(^|\s)VARIABLE\s";
        string AssignPattern = @"(^|\s)ASSIGN\s";
        string InitialPattern = @"(^|\s)INITIAL\s";
        string InitialSelectorPattern = @"(^|\s)INITIALSELECTOR\s";
        string ParameterPattern = @"(^|\s)PARAMETER\s";
        string SelectorPattern = @"(^|\s)SELECTOR\s";
        string SetPattern = @"(^|\s)SET\s";
        string UnitPattern = @"(^|\s)UNIT\s";
        string VariablePattern = @"(^|\s)VARIABLE\s";

        #endregion data

        #region constructor
        public sinter_simGPROMSconfig()
        {
            o_availibleSettings = new Dictionary<string, Tuple<set_setting, get_setting, sinter_Variable.sinter_IOType>>
            {
                {"ProcessName", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_processName, getobj_processName, sinter_Variable.sinter_IOType.si_STRING)},
                {"password", Tuple.Create<set_setting, get_setting, sinter_Variable.sinter_IOType>(setobj_password, getobj_password, sinter_Variable.sinter_IOType.si_STRING)}
            };

            o_processName = "";
            o_password = "";
            simName = "gPROMS";

        }

        #endregion constructor

        #region pathing
        public override char pathSeperator
        {
            get
            {
                return '.';
            }
        }

        public IList<string> splitPath(string path)
        {

            while ((path.Length > 0 && path[0] == pathSeperator))
            {
                path = path.Substring(1);
            }

            if ((path.Length == 0))
            {
                return new List<string>();
            }

            return path.Split(pathSeperator).ToList();
        }

        private Object getModelOrDeclearationByPath(String gpromsPath)
        {
            IList<string> pathList = parsePath(gpromsPath);
            if (pathList.Count <= 1)
            {
                return null;  //If we get a bad path, or a path that just consists of a processname
            }
            GProcess proc = gProcesses[pathList[0]];
            if(proc.variables.ContainsKey(pathList[1])) {
                return proc.variables[pathList[1]];
            }
            Model thisModel = proc.submodels[pathList[1]];
            for (int ii = 2; ii < pathList.Count; ++ii)
            {
                if (thisModel.submodels.ContainsKey(pathList[ii]))
                {
                    thisModel = thisModel.submodels[pathList[ii]];
                }
                else
                {
                    return thisModel.variables[pathList[ii]];  //Return a variable Decleration if that's what this is
                }
            }

            return thisModel;  //return a model if that's what the path points to
        }

        //Search for a Model from it's path.  Used for linking assignments (has a path) and declerations
        private Model getModelByPath(String gpromsPath)
        {
            Object temp = getModelOrDeclearationByPath(gpromsPath);
            if (temp is Model)
            {
                return (Model)temp;
            }
            else
            {
                return null;
            }
        }


        //Search for a variableDecleration from it's path.  Used for linking assignments (has a path) and declerations
        private variableDecleration getDeclearationByPath(String gpromsPath)
        {
            Object temp = getModelOrDeclearationByPath(gpromsPath);
            if (temp is variableDecleration)
            {
                return (variableDecleration)temp;
            }
            else
            {
                return null;
            }
        }
        
        #endregion pathing

        #region meta-data
        public override bool IsInitializing
        {
            get { return false; }
        }

        public override string[] errorsBasic()
        {
            throw new NotImplementedException();
        }

        public override string[] warningsBasic()
        {
            throw new NotImplementedException();
        }

        public override string setupfileKey { get { return "model"; } }

        public string processName
        {
            get
            {
                return o_processName;
            }
            set
            {
                if (o_processName != value)
                {
                    o_processName = value;
//                    resetInputVariables();  I think this should happen elsewhere so it doesn't happen at startup
                }
            }
        }

        public void setobj_processName(object value)
        {
            processName = (String)value;
        }

        public object getobj_processName()
        {
            return processName;
        }

        public string password
        {
            get
            {
                return o_password;
            }
            set
            {
                o_password = value;
            }
        }

        public void setobj_password(object value)
        {
            password = (String)value;
        }

        public object getobj_password()
        {
            return password;
        }

        public override string[] externalVersionList()
        { 
            string[] versions = {"4.0.0", "4.1.0", "4.2.0" };  //We don't support anything earlier than 4.0.0
            return versions;
        }

        /** Thankfully, gPROMS actually uses the same versioning format internally and externally**/
        public override string internal2externalVersion(string internalVersion)
        {
            return internalVersion;
        }

        public override string external2internalVersion(string externalVersion)
        {
            return externalVersion;
        }

        #endregion meta-data

        #region sim-control
        //This simulation type doesn't really have any sim control
        public override bool Vis
        {
            get
            {
                return false;
            }
            set
            {

            }
        }

        public override bool dialogSuppress
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public override void closeSim()
        {
           //Nothing to close, but this is called by SinterConfigGUI sometimes
        }

        public override sinter_AppError resetSim()
        {
            throw new NotImplementedException();
        }

        public override sinter_AppError runSim()
        {
            throw new NotImplementedException();
        }

        public override bool terminate()
        {
            return true;
        }

        //gPROMS has no way to stop a sim, so this call must just be ignroed.
        public override void stopSim() { return; }

        #endregion sim-control

        #region parse-gpj

        private ModelKeywordsEnum isModelKeyword(string keyword)
        {
            int idx = 0;
            for (idx = 0; idx < ModelKeywords.Count(); ++idx)
            {
                if (keyword == ModelKeywords[idx])
                {
                    return (ModelKeywordsEnum) idx;
                }
            }
            return ModelKeywordsEnum.UNKNOWN;
        }

        private ProcessKeywordsEnum isProcessKeyword(string keyword)
        {
            int idx = 0;
            for (idx = 0; idx < ProcessKeywords.Count(); ++idx)
            {
                if (keyword == ProcessKeywords[idx])
                {
                    return (ProcessKeywordsEnum) idx;
                }
            }
            return ProcessKeywordsEnum.UNKNOWN;
        }

        /**
         *  Parses out a variable type from the XML and stores it in the variableTypes dictionary
         **/
        private void addVariableType(XmlNode varTypeNode)
        {
            if (varTypeNode.Name == "VariableTypeEntity")
            {
                string typeName = varTypeNode.Attributes["name"].Value;
                XmlNode units_node = varTypeNode["Units"];
                string units = "";
                if (units_node != null)
                {
                    units = varTypeNode["Units"].InnerText;
                }
                string min = varTypeNode["MinValue"].InnerText;
                string max = varTypeNode["MaxValue"].InnerText;
                string dfault = varTypeNode["DefaultValue"].InnerText;
                variableType thisVar = new variableType(typeName, units, min, max, dfault);
                variableTypes[typeName] = thisVar;
            }
        }


        /**
         *  Parses the gPROMS model language to get out the parameters and variables,
         *  adds the resulting data to the models dictionary.
         **/
        private void addModel(string name, string text)
        {
            //First, delete the multiline comments from the code
            deleteComments(ref text);
            string unitSection = pullGPROMSFileSection(UnitPattern, text);

            string paramSection = pullGPROMSFileSection(ParameterPattern, text);
            string selectorSection = pullGPROMSFileSection(SelectorPattern, text);
            string variableSection = pullGPROMSFileSection(VariablePattern, text);
                
            // create the scanner to use
            Scanner scanner = new Scanner();

            // create the parser, and supply the scanner it should use
            Parser parser = new Parser(scanner);

            Dictionary<String, sinter.PSE.variableDecleration> allVarsList = new Dictionary<String, sinter.PSE.variableDecleration>();
            if (paramSection != "" || selectorSection != "" || variableSection != "")
            {
                string allVars = paramSection + "\n" + selectorSection + "\n" + variableSection;
                // parse the input. the result is a parse tree.
                ParseTree varsTree = parser.Parse(allVars);
                // evaluate the parse tree; do not pass any additional parameters
                List<Object> tempVarList = (List<Object>)varsTree.Eval(null);
                foreach (Object tempVar in tempVarList)  //We get out an object list because the parser does both decl and assign
                {
                    sinter.PSE.variableDecleration var = (sinter.PSE.variableDecleration)tempVar;
                    //There are some special types we can't do anything with, just igore those.
                    bool success = var.initVarType(variableTypes);  //Set defaults, min, max, etc.  (Can't be done in the parser, so it can't be done in the constructor)
                    if (success)
                    {
                        allVarsList.Add(var.name, var);
                    }
                }
            }

            List<Object> unitListAsVars = new List<Object>(); 
            Dictionary<String, Model> unitList = new Dictionary<String, Model>();

            // Now we get out the UNIT section of the file.  A Model may have submodels listed under UNITS
            // Since the UNIT section has the same grammar as the VARIABLE section, we just parse it the same,
            // Then have to reinterpret the results
            if (unitSection != "")
            {
                ParseTree unitTree = parser.Parse(unitSection);
                unitListAsVars = (List<Object>)unitTree.Eval(null);

                foreach (Object unitAsObj in unitListAsVars)
                {
                    sinter.PSE.variableDecleration unitAsVar = (sinter.PSE.variableDecleration)unitAsObj;
                    String unitName = unitAsVar.name;
                    String typeName = unitAsVar.typeName;
                    unitList.Add(unitName, new Model(typeName, null, null));  //Model types are not fully defined yet.  This needs to be fixed up in post-processing
                }
            }

            //Now actually add the model we have now full parsed to the model list
            Model thisModel = new Model(name, unitList, allVarsList);
            models[name] = thisModel;
        }

        /*  Probably all trash, this is the code I used when I thought section keywords had to be on their own 
         * lines
string[] textlines = text.Split('\n');
int paramGroupStart = -1;
int paramGroupEnd = textlines.Count() - 1;
//Find the beginning of the PARAMETER group
for (int ii = 0; ii < textlines.Count(); ++ii)
{
    if (isModelKeyword(textlines[ii].Trim()) == ModelKeywordsEnum.PARAMETER)
    {
        paramGroupStart = ii+1; //Line past this one, exclude the PARAMETER section keyword
        break;
    }
}
//Find the end of the PARAMETER group.  If we don't find it, paramGroupEnd will still be the end of the text as above.
for (int ii = paramGroupStart+1; ii < textlines.Count(); ++ii)
{
    if (isModelKeyword(textlines[ii].Trim()) != ModelKeywordsEnum.UNKNOWN)
    {
        paramGroupEnd = ii-1;  //Line before this line (exclude the next section name)
        break;
    }
}
*/
        // define the input for the parser
        /*            string[] inputLines = new string[paramGroupEnd-paramGroupStart];
                    for (int ii = paramGroupStart; ii < paramGroupEnd; ++ii)
                    {
                        inputLines[ii - paramGroupStart] = textlines[ii];
                    }

                    string input = String.Join("\n", inputLines);
                    */


        /**
         *  Parses the gPROMS process language to figure out which variables are input and 
         *  which may be output.
         *  adds the resulting data to the processes dictionary.
         **/
        private void addGProcess(string name, string text) { 
                    string unitSection = pullGPROMSFileSection(UnitPattern, text);
                
            // create the scanner to use
            Scanner scanner = new Scanner();

            // create the parser, and supply the scanner it should use
            Parser parser = new Parser(scanner);

            List<Object> unitListAsVars = new List<Object>();
            Dictionary<String, Model> unitList = new Dictionary<String, Model>();

            // Now we get out the UNIT section of the GProcess.  A GProcess may have models listed under UNITS
            // Since the UNIT section has the same grammar as the VARIABLE section, we just parse it the same,
            // Then have to reinterpret the results
            if (unitSection != "")
            {
                ParseTree unitTree = parser.Parse(unitSection);
                unitListAsVars = (List<Object>)unitTree.Eval(null);

                foreach (Object unitAsObj in unitListAsVars)
                {
                    sinter.PSE.variableDecleration unitAsVar = (sinter.PSE.variableDecleration)unitAsObj;
                    String unitName = unitAsVar.name;
                    String typename = unitAsVar.typeName;
                    unitList.Add(unitName, models[typename]);  //The models are fully defined now, so just add a straight link to the type
                }
            }

            //First, delete the multiline comments from the code
            deleteComments(ref text);

            //GProcesses also can have PARAMETER and VARIABLE sections 
            string paramSection = pullGPROMSFileSection(ParameterPattern, text);
            string selectorSection = pullGPROMSFileSection(SelectorPattern, text);
            string variableSection = pullGPROMSFileSection(VariablePattern, text);

            Dictionary<String, sinter.PSE.variableDecleration> allVarsList = new Dictionary<String, sinter.PSE.variableDecleration>();
            if (paramSection != "" || selectorSection != "" || variableSection != "")
            {
                string allVars = paramSection + "\n" + selectorSection + "\n" + variableSection;
                // parse the input. the result is a parse tree.
                ParseTree varsTree = parser.Parse(allVars);
                // evaluate the parse tree; do not pass any additional parameters
                List<Object> tempVarList = (List<Object>)varsTree.Eval(null);
                foreach (Object tempVar in tempVarList)  //We get out an object list because the parser does both decl and assign
                {
                    sinter.PSE.variableDecleration var = (sinter.PSE.variableDecleration)tempVar;
                    var.initVarType(variableTypes);  //Set defaults, min, max, etc.  (Can't be done in the parser, so it can't be done in the constructor)
                    allVarsList.Add(var.name, var);
                }
            }
            //-----------------------------
            // Now we need to handle each assignment section SET INITIAL INITIALSELECTOR and ASSIGN
            //-----------------------------
            //The SET section
            string setSection = pullGPROMSFileSection(SetPattern, text);
            List<sinter.PSE.variableAssignment> setList = new List<sinter.PSE.variableAssignment>();
            if (setSection != "")
            {
                // parse the input. the result is a parse tree.
                ParseTree setTree = parser.Parse(setSection);
                // evaluate the parse tree; do not pass any additional parameters
                List<Object> tempSetList = (List<Object>)setTree.Eval(null);
                foreach (Object tempSet in tempSetList)  //We get out an object list because the parser does both decl and assign
                {
                    sinter.PSE.variableAssignment set = (sinter.PSE.variableAssignment)tempSet;
                    setList.Add(set);
                }
            }

            //The ASSIGN section
            string assignSection = pullGPROMSFileSection(AssignPattern, text);
            List<sinter.PSE.variableAssignment> assignList = new List<sinter.PSE.variableAssignment>();
            if (assignSection != "")
            {
                // parse the input. the result is a parse tree.
                ParseTree assignTree = parser.Parse(assignSection);
                // evaluate the parse tree; do not pass any additional parameters
                List<Object> tempAssignList = (List<Object>)assignTree.Eval(null);
                foreach (Object tempAssign in tempAssignList)  //We get out an object list because the parser does both decl and assign
                {
                    sinter.PSE.variableAssignment assign = (sinter.PSE.variableAssignment)tempAssign;
                    assignList.Add(assign);
                }
            }

            //The INITIAL section
            string initialSection = pullGPROMSFileSection(InitialPattern, text);
            List<sinter.PSE.variableAssignment> initialList = new List<sinter.PSE.variableAssignment>();
            if (initialSection != "")
            {
                // parse the input. the result is a parse tree.
                ParseTree initialTree = parser.Parse(initialSection);
                // evaluate the parse tree; do not pass any additional parameters
                List<Object> tempinitialList = (List<Object>)initialTree.Eval(null);
                foreach (Object tempinitial in tempinitialList)  //We get out an object list because the parser does both decl and assign
                {
                    sinter.PSE.variableAssignment initial = (sinter.PSE.variableAssignment)tempinitial;
                    initialList.Add(initial);
                }
            }

            //The INITIALSELECTOR section
            string selectSection = pullGPROMSFileSection(InitialSelectorPattern, text);
            List<sinter.PSE.variableAssignment> selectList = new List<sinter.PSE.variableAssignment>();
            if (selectSection != "")
            {
                // parse the input. the result is a parse tree.
                ParseTree selectTree = parser.Parse(selectSection);
                // evaluate the parse tree; do not pass any additional parameters
                List<Object> tempselectList = (List<Object>)selectTree.Eval(null);
                foreach (Object tempselect in tempselectList)  //We get out an object list because the parser does both decl and assign
                {
                    sinter.PSE.variableAssignment select = (sinter.PSE.variableAssignment)tempselect;
                    selectList.Add(select);
                }
            }

            //We need to make sure we know the path for the foriegn object variable, so we can parse it out of other varaibles and find the inputs
            String inputFOPath = "";
            foreach(variableAssignment thisvar in setList) {
                if (thisvar.assignmentValue == "\"SimpleEventFOI::dummy\"")
                {
                    if(inputFOPath != "") {
                        throw new ArgumentException(String.Format("Found two input foriegn objects in {0} : {1} and {2}", 
                            name, inputFOPath, thisvar.assignmentValue));
                    }
                    inputFOPath = thisvar.gPROMSPath;
                }
            }


            foreach (variableAssignment var in setList)
            {
                var.init("SET", inputFOPath);
            }
            foreach (variableAssignment var in assignList)
            {
                var.init("ASSIGN", inputFOPath);
            }
            foreach (variableAssignment var in initialList)
            {
                var.init("INITIAL", inputFOPath);
            }
            foreach (variableAssignment var in selectList)
            {
                var.init("INITIALSELECTOR", inputFOPath);
            }

            //Now add all the variables we made together into one list
            setList.AddRange(assignList);
            setList.AddRange(initialList);
            setList.AddRange(selectList);

            foreach (variableAssignment varAssign in setList)
            {
                varAssign.gPROMSPath = name + pathSeperator + varAssign.gPROMSPath;  
            }

            //Now needs to be a GProcess  
            GProcess thisGProcess = new GProcess(name, unitList, allVarsList, setList, name + "." + inputFOPath);
            gProcesses[name] = thisGProcess;
        }


        /** 
         * Parses the GPJ file to fill in all the data about the sim
         **/
        public override void openSim()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(System.IO.Path.Combine(workingDir, setupFile.simDescFile));
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
            namespaceManager.AddNamespace("gMB", "http://www.psenterprise.com/gMB");

            //Select the Project node so we can get the version number
            XmlNodeList projNodes = xmlDoc.SelectNodes("//gMB:GpromsProject", namespaceManager);
            foreach (XmlNode itemNode in projNodes)  //There should only be one, but for safety
            {
                simVersion = itemNode.Attributes["gMB_Version"].Value;
            }

            XmlNodeList itemNodes = xmlDoc.SelectNodes("//gMB:GpromsProject/Group", namespaceManager);

            //gPJ files have groups of interesting block types, like "Variable Types" or "Models"
            //Unfortunately, these groups don't have a useful tag name, the tag name is just "Group"
            //An attribute "name" has the type of group.  So we need to walk through all the Group blocks checking
            //that attribute to find the one we want.
            //We need to do this 3 times as I want to make sure I hav the variable types before the models, and the models before the processes
            foreach (XmlNode itemNode in itemNodes)
            {
                //First, do we have a Variable Types group?
                if (itemNode.Attributes["name"] != null && itemNode.Attributes["name"].Value == "Variable Types")
                {
                    foreach (XmlNode entity in itemNode.ChildNodes)
                    {
                        addVariableType(entity);
                    }
                }
            }
            //Do the models
            foreach (XmlNode itemNode in itemNodes)
            {
                //What about a Models group, is this one of those?
                if (itemNode.Attributes["name"] != null && itemNode.Attributes["name"].Value == "Models")
                {
                    foreach (XmlNode entity in itemNode.ChildNodes)
                    {
                        if (entity.Name == "ModelEntity")
                        {
                            addModel(entity.Attributes["name"].Value, entity["Body"].InnerText);
                        }
                    }
                }
            }

            //When we made each model's submodel, we couldn't be sure all the models were defined yet.  So we put in placeholder models.
            //Now that all the models are parsed, we should be able to find real model references for each submodel.  Do that.
            foreach (String key in models.Keys) {  //We get the key then lookup the model because is we do foreach model, C# won't let us edit the model
                Model thisModel = models[key];
                if (thisModel.submodels.Count > 0)  
                {
                    Dictionary<String, Model> oldSubModels = thisModel.submodels;
                    Dictionary<String, Model> newSubModels = new Dictionary<String, Model>();
                    foreach (String subModelName in oldSubModels.Keys)  //The old model's name and typename are the only valid things in it.
                    {
                        Model oldSubModel = oldSubModels[subModelName];
                        if(models.ContainsKey(oldSubModel.typename)) {  //Except when using libraries like PML, we may not actually have all the models, so skip the ones we can't find
                          Model newSubModel = models[oldSubModel.typename];
                          newSubModels.Add(subModelName, newSubModel);
                        }
                     }
                    thisModel.submodels = newSubModels;
                }
            }

            //Do the processes
            foreach (XmlNode itemNode in itemNodes)
            {

                //Processes group maybe?  (If none of these, just move on to the next group)
                if (itemNode.Attributes["name"] != null && itemNode.Attributes["name"].Value == "Processes")
                {
                    foreach (XmlNode entity in itemNode.ChildNodes)
                    {
                        if (entity.Name == "ProcessEntity")
                        {
                            addGProcess(entity.Attributes["name"].Value, entity["Body"].InnerText);
                        }
                    }
                }
            }

            if (gProcesses.Count() <= 0)
            {
                throw new ArgumentException(String.Format("No process found in {0}.  Processes are required for SimSinter to run gPROMS", setupFile.simDescFile));
            }

            //Guarantees settings will be done before any real interaction with the simulation.  
            //We may need to seperate this off and move it earlier in the future.
            foreach (sinter_Variable inputObj in o_setupFile.Settings)
            {
                inputObj.sendSetting(this);
            }

            ////////////////////////////////////////////////////////////////////////////////////
            //That's it for parsing the file.  Now initialize the settings and the input variables 
            ///////////////////////////////////////////////////////////////////////////////////
            //The simulation run file needs to be a .gENCRYPT file, the .gPJ file is found in simDescFile
            if (setupFile.aspenFilename == null || setupFile.aspenFilename == "" || ! String.Equals(Path.GetExtension(setupFile.aspenFilename), ".gENCRYPT", StringComparison.OrdinalIgnoreCase))
            {  //If the user has not provided a password, assume it's the filename sans extension (the default)
                setupFile.aspenFilename = Path.GetFileNameWithoutExtension(setupFile.aspenFilename) + ".gENCRYPT";
            }
            
            if (password == "")
            {  //If the user has not provided a password, assume it's the filename sans extension (the default)
                password = Path.GetFileNameWithoutExtension(setupFile.simDescFile);
            }

            if (processName == "")
            {
                processName = gProcesses.ElementAt(0).Value.name;  //If we don't have a json file, pick any process
                //Note, this call will also force a reset of the input variables.
            }

            resetInputVariables();
            restoreDefaults();
            checkSimVersionConstraints();  //Have to this last so the possible exception doesn't screw up state.

        }

        #endregion parse-gpj

        #region variable-tree
        /** 
         * void startDataTree
         * 
         * This function generates the root of a variable tree.  It does not fill in any child nodes.  This is
         * useful for generating the tree as the user opens nodes in the SinterConfigGUI 
         * 
         * It does fill in the processes, and the processes first children.  This is because we want to get the tree filled
         * in to the point where we only have to worry about models and their variables, not processes.
         */
        public override void startDataTree()
        {
            o_dataTree = new VariableTree.VariableTree(splitPath, pathSeperator);

            VariableTree.VariableTreeNode rootNode = new VariableTree.VariableTreeNode("", "", pathSeperator);
            rootNode.o_children.Remove("DummyChild");

            foreach (GProcess thisProcess in gProcesses.Values)
            {
                string childPath = thisProcess.name;
                VariableTreeNode procNode = new VariableTreeNode(thisProcess.name, thisProcess.name, pathSeperator);
                procNode.o_children.Remove("DummyChild");

                foreach (String childName in thisProcess.variables.Keys)
                {
                    variableDecleration childVar = thisProcess.variables[childName];
                    if (childVar.mode == IOMode.si_OUT || childVar.mode == IOMode.si_INOUT)
                    {
                        VariableTreeNode childNode = new VariableTreeNode(childName, thisProcess.name + pathSeperator + childName, pathSeperator);
                        procNode.o_children.Remove("DummyChild");  //leaf node
                        procNode.addChild(childNode);
                    }
                }

                foreach (String childName in thisProcess.submodels.Keys)
                {
                    Model subModel = thisProcess.submodels[childName];
                    VariableTreeNode childNode = new VariableTreeNode(childName, thisProcess.name + pathSeperator + childName, pathSeperator);
                    procNode.addChild(childNode);  //Not a leaf node, but not expanded yet either
                }

                rootNode.addChild(procNode);
            }

            o_dataTree.rootNode = rootNode;
        }
            
        public override VariableTree.VariableTreeNode findDataTreeNode(IList<String> pathArray)
        {
            VariableTree.VariableTreeNode thisNode = o_dataTree.rootNode;
            for (int ii = 0; ii < pathArray.Count(); ++ii)
            {
                //The children have been added if necessary, now go to the child and repeat
                thisNode = thisNode.o_children[pathArray[ii]];

                //If we have more work to do, and this node has no children, add the children before moving on
                if (thisNode.o_children.ContainsKey("DummyChild"))
                {
                    thisNode.o_children.Remove("DummyChild");

                    String nodePath = combinePath(pathArray, ii);
                    Object pathNode = getModelOrDeclearationByPath(nodePath);
                    if (pathNode is Model)  //If what we got is a model, add it's children, if it's not a model, we're done.
                    {
                        Model thisModel = (Model)pathNode;
                        foreach (String childName in thisModel.variables.Keys)
                        {
                            variableDecleration childVar = thisModel.variables[childName];
                            if (childVar.mode == IOMode.si_OUT || childVar.mode == IOMode.si_INOUT)
                            {
                                VariableTreeNode childNode = new VariableTreeNode(childName, nodePath + pathSeperator + childName, pathSeperator);
                                thisNode.o_children.Remove("DummyChild");  //leaf node
                                thisNode.addChild(childNode);
                            }
                        }
                        foreach (String childName in thisModel.submodels.Keys)
                        {
                            Model subModel = thisModel.submodels[childName];
                            VariableTreeNode childNode = new VariableTreeNode(childName, nodePath + pathSeperator + childName, pathSeperator);
                            thisNode.addChild(childNode);  //Not a leaf node, but not expanded yet either
                        }
                    }
                }
            }
            return thisNode;
        }

        public override void makeDataTree()
        {
            throw new NotImplementedException();
        }

        /** We're not sure how to do Heat Intergration Variables in Aspen+ yet */
        public override IList<sinter_IVariable> getHeatIntegrationVariables()
        {
            return new List<sinter_IVariable>();
        }

        #endregion variable-tree

        public override void sendInputsToSim()
        {
            throw new NotImplementedException();
        }

        #region variable-meta-data-discovery

        public override void initializeDefaults()
        {
            restoreDefaults();
        }

        public override sinter_Variable.sinter_IOType guessTypeFromSim(string path) {
            variableDecleration varDecl = getDeclearationByPath(path);
            if (varDecl != null)
            {
                return varDecl.type;
            }
            return sinter_Variable.sinter_IOType.si_UNKNOWN;
        }

        public override sinter_Variable.sinter_IOType guessVectorTypeFromSim(string path, int[] indicies) {
            variableDecleration varDecl = getDeclearationByPath(path);
            if (varDecl != null)
            {
                return varDecl.type;
            }
            return sinter_Variable.sinter_IOType.si_UNKNOWN;
        }
        /** I don't think this needs to do anything, we should already know the units if the variable has any */
        public override void initializeUnits() {  }


        public override int guessVectorSize(string path) {
            variableDecleration varDecl = getDeclearationByPath(path);
            if (varDecl != null)
            {
                return varDecl.arraySize;
            }
            return 0;        }

        /** 
         * gPROMS arrays are indexed from 1
         **/
        public override int[] getVectorIndicies(string path, int size)
        {
            int[] indicies = new int[size];
            for (int ii = 1; ii <= size; ++ii)
            {
                indicies[ii - 1] = ii;
            }
            return indicies;
        }

        public override string getCurrentUnits(string path)
        {
            variableDecleration varDecl = getDeclearationByPath(path);
            return varDecl.units;
        }

        public override string getCurrentUnits(string path, int[] indicies)
        {
            variableDecleration varDecl = getDeclearationByPath(path);
            return varDecl.units;
        }

        /** I don't really have anyway to get a useful description here. **/
        public override string getCurrentDescription(string path)
        {
            variableDecleration varDecl = getDeclearationByPath(path);

            string desc;
            if (varDecl.units != "")
            {
                desc = varDecl.name + " in " + varDecl.units;
            }
            else
            {
                desc = varDecl.name + " as " + varDecl.typeName;
            }

            return desc;
        } 

        /** I don't really have anyway to get a useful name here. **/
        public override string getCurrentName(string path) {
            return path;
        }        

        #endregion variable-meta-data-discovery

        #region get-variable-value
        public override void sendValueToSim<ValueType>(string path, ValueType value) { throw new NotImplementedException(); }
        public override void sendValueToSim<ValueType>(string path, int ii, ValueType value) { throw new NotImplementedException(); }  //for vectors
        public override void sendVectorToSim<ValueType>(string path, ValueType[] value) { throw new NotImplementedException(); }  //optimization to set whole vector at once

        /**
         * recvValueFromSimAsObjec was added to give us a chance to figure out the type of a variable automatically in the GUI
         * They should not be called on variables that may not exist, they will throw an Exception
         */
        //        public override Object recvValueFromSimAsObject(string path) { throw new NotImplementedException(); }
        //        public override Object recvValueFromSimAsObject(string path, int ii) { throw new NotImplementedException(); }  //For vectors type identification

        public override ValueType recvValueFromSim<ValueType>(string path)
        {
            variableDecleration varDecl = getDeclearationByPath(path);
            if (varDecl != null)
            {
                return (ValueType)varDecl.dfault;
            }
            else
            {
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                throw new System.IO.IOException("Failed to get result for " + path + ". Does the variable exist?");
            }
        }
        //        public override ValueType recvValueFromSim<ValueType>(string path, int ii);  //For vectors
        public override void recvVectorFromSim<ValueType>(string path, int[] indicies, ValueType[] value)
        {
            variableDecleration varDecl = getDeclearationByPath(path);
            if (varDecl != null)
            {
                value = (ValueType[]) Enumerable.Repeat((ValueType)varDecl.dfault, varDecl.arraySize).ToArray();
            }
            else
            {
                runStatus = sinter_AppError.si_SIMULATION_ERROR;
                throw new System.IO.IOException("Failed to get result for " + path + ". Does the variable exist?");
            }
        }  //optimization to get whole vector at once
        #endregion get-variable-value

        #region section-snipping
        /**
         * This call is for pulling the code out of particular gPROMS sections, such as PARAMETER, VARIABLE, etc.
         * It takes a regex pattern to search for (such as ParameterPattern) then searches for the end of that
         * section.
         * This is necessary because I couldn't figure out how to get TinyPG to skip sections they way they 
         * are defined in gPROMS.  It's possible gPROMS isn't an LL(1) grammar in this case.
         **/
        /** Walk back to make sure this match wasn't found in a comment.  
 *  If '\n' or the begginning of the text is found first, it's a good match
 *  If '#' is found first, it's a false match
 *  **/
        public bool isRobustMatch(String text, int matchIndex)
        {
            for (int ii = matchIndex; ii >= 0; --ii)
            {
                if (text[ii] == '#')
                {
                    return false;
                }
                else if (text[ii] == '\n')
                {
                    return true;
                }
            }
            return true;
        }

        public void deleteComments(ref string text)
        {
            string multicommentRegex = @"{[^}]*}";
            text = Regex.Replace(text, multicommentRegex, "");
        }

        //robust match search for section headings.  Section names such as SET may appear in comments as well.
        //isRobustMatch eliminated the comment cases so we're sure we've really found a section start.
        // The beforeMatch argument says whether to return the match index BEFORE the match, or AFTER the match
        // RETURN: This function returns the index into the orig_text that follows the match.  So the matched section
        //          name will NOT appear in the text
        public int robustMatch(string sectionNamePattern, string orig_text, int startIndex, bool beforeMatch)
        {
            String text = orig_text.Substring(startIndex);
            Regex thisRegex = new Regex(sectionNamePattern, RegexOptions.IgnoreCase);
            Match thisMatch = thisRegex.Match(text);
            while (thisMatch.Success)
            {
                if (isRobustMatch(text, thisMatch.Index))
                {
                    if (beforeMatch)
                    {
                        return thisMatch.Index + startIndex;
                    }
                    else
                    {
                        return thisMatch.Index + thisMatch.Length + startIndex;
                    }
                }
                else
                {
                    thisMatch = thisMatch.NextMatch();
                }
            }

            return -1;

        }

        //Uses Robust match to find sections by matching first the section name we're looking for, and then the next section, and cuts the text out from between them.
        public string pullGPROMSFileSection(string sectionNamePattern, string text)
        {
            //First find the section start
            int startIndex = robustMatch(sectionNamePattern, text, 0, false);
            if (startIndex == -1)
            {
                return "";
            }

            //Then the section end
            int endIndex = robustMatch(AllSectionsPattern, text, startIndex, true);
            if (endIndex == -1)
            {
                endIndex = text.Count() - 1;
            }

            String sectionText = text.Substring(startIndex, (endIndex - startIndex));

            return sectionText;
        }

        
        /*        private string pullGPROMSFileSection(string sectionNamePattern, string text)
        {
            //First find the PARAMETER section
            Regex startRegex = new Regex(sectionNamePattern, RegexOptions.IgnoreCase);
            Regex EndRegex = new Regex(AllSectionsPattern, RegexOptions.IgnoreCase);

            Match startMatch = startRegex.Match(text);
            if (!startMatch.Success)  //There may be no matching section
            {
                return "";
            }
            Match endMatch = EndRegex.Match(text, startMatch.Index + ParameterPattern.Count());
            int endIndex = text.Count() - 1;  //The section may just run to the end of the text, so deal with that case
            if (endMatch.Success)
            {
                endIndex = endMatch.Index;
            }
            String sectionText = text.Substring(startMatch.Index, (endIndex - startMatch.Index));

            return sectionText;
        }
        */
        #endregion section-snipping

        //The input variables in the json file (if there was one) 
        //Write a function for this.  It needs to check the existing variables to make sure they don't have useful information previously set by the user
        public void resetInputVariables() {

            //First try to save the old variables.
            Microsoft.VisualBasic.Collection iovars_ref = setupFile.AllIO;
            Microsoft.VisualBasic.Collection iovars_inputs = new Microsoft.VisualBasic.Collection();
            Microsoft.VisualBasic.Collection iovars_other = new Microsoft.VisualBasic.Collection();

            //We need to save the input variables and output variables seperately because input variables are only used for some meta-data
            foreach (sinter_IVariable thisVar in iovars_ref)
            {
                if (thisVar.isInput && !thisVar.isTable)
                {
                    ; // do nothing after all iovars_inputs.Add(thisVar);
                }
                else
                {
                    iovars_other.Add(thisVar, thisVar.name);
                }
            }


            List<sinter.sinter_IVariable> newinputs = new List<sinter.sinter_IVariable>();
            GProcess thisProcess = gProcesses[processName];
            foreach (variableAssignment varAssign in thisProcess.assignments)
            {
                if (varAssign.mode == IOMode.si_IN || varAssign.mode == IOMode.si_INOUT)  //We only care about variables we're sure are inputs
                {

                    variableDecleration varDecl = getDeclearationByPath(varAssign.gPROMSPath);
                    sinter_IVariable existingVar = setupFile.getIOByPath(varAssign.inputPath);

                    //If there was a variable by that path, and it has the same type, etc.  keep it, so we can save the description and name
                    if (existingVar != null && existingVar.type == varDecl.type && existingVar.mode == sinter_Variable.sinter_IOMode.si_IN &&
                        (varDecl.mode == IOMode.si_IN || varDecl.mode == IOMode.si_INOUT)) 
                    {
                        newinputs.Add(existingVar);
                    }
                    else  //Otherwise make a new variable based on the decleration and whatnot.
                    {
                        if (varAssign.isVec)  //Vector case
                        {
                            sinter_Vector newVec = new sinter_Vector();
                            String[] address = { varAssign.inputPath };
                            newVec.init(varAssign.inputPath, sinter_Variable.sinter_IOMode.si_IN, varDecl.type, varAssign.gPROMSPath, address, varDecl.arraySize);

                            if (varDecl.type == sinter_Variable.sinter_IOType.si_STRING_VEC)
                            {
                                newVec.minimum = Enumerable.Repeat((String)varDecl.min, varDecl.arraySize).ToArray();
                                newVec.maximum = Enumerable.Repeat((String)varDecl.max, varDecl.arraySize).ToArray();
                                newVec.dfault = Enumerable.Repeat((String)varDecl.dfault, varDecl.arraySize).ToArray();
                            }
                            else if (varDecl.type == sinter_Variable.sinter_IOType.si_INTEGER_VEC)
                            {
                                newVec.minimum = Enumerable.Repeat((int)varDecl.min, varDecl.arraySize).ToArray();
                                newVec.maximum = Enumerable.Repeat((int)varDecl.max, varDecl.arraySize).ToArray();
                                newVec.dfault = Enumerable.Repeat((int)varDecl.dfault, varDecl.arraySize).ToArray();
                            }
                            else  //double is the default
                            {
                                newVec.minimum = Enumerable.Repeat((double)varDecl.min, varDecl.arraySize).ToArray();
                                newVec.maximum = Enumerable.Repeat((double)varDecl.max, varDecl.arraySize).ToArray();
                                newVec.dfault = Enumerable.Repeat((double)varDecl.dfault, varDecl.arraySize).ToArray();
                            }

                            newVec.units = varDecl.units;
                            newinputs.Add(newVec);
                        }
                        else //Scalar case
                        {
                            sinter_Variable newVar = new sinter_Variable();
                            String[] address = { varAssign.inputPath };
                            newVar.init(varAssign.inputPath, sinter_Variable.sinter_IOMode.si_IN, varDecl.type, varAssign.gPROMSPath, address);
                            newVar.minimum = varDecl.min;
                            newVar.maximum = varDecl.max;
                            newVar.dfault = varDecl.dfault;
                            newVar.units = varDecl.units;
                            newinputs.Add(newVar);
                        }
                    }
                }
            }

            //Now clear out the variables so we can replace them
            setupFile.clearAllVariables();
            //And add new ones in:
            //First the settings:
            IList < sinter_Variable > settings = getSettings();
            foreach (sinter_Variable setting in settings)
            {
                setupFile.addVariable(setting);
            }

            //Then the inputs
            foreach (sinter_IVariable inVar in newinputs)
            {
                setupFile.addVariable((sinter_Variable)inVar);
            }

            //Finally outputs and tables
            foreach (sinter_IVariable oVar in iovars_other)
            {
                if (oVar.isTable)
                {
                    setupFile.addTable((sinter_Table)oVar);
                }
                else
                {
                    setupFile.addVariable((sinter_Variable)oVar);
                }
            }

            restoreDefaults();  //Make sure all the variable values are set to their defaults to avoid possible conflicts
        }

    }
}
