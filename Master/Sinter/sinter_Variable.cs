using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sinter
{
    //
    // This is a generic class for storing input and output
    // for simulations.  It can be inherited to create
    // IO object classes for specific simulation types.
    //
    public class sinter_Variable : sinter_IVariable
    {
        #region enums
        public enum sinter_IOType
        {
            //data type codes
            si_UNKNOWN = -1,
            //this type describes the type of data stored in an IO object
            si_INTEGER = 0,
            si_DOUBLE = 1,
            si_STRING = 2,
            //Vector
            si_DOUBLE_VEC = 3,
            si_INTEGER_VEC = 4,
            si_STRING_VEC = 5,
            //Dynamic types
            si_DY_INTEGER = 10,
            si_DY_DOUBLE = 11,
            si_DY_STRING = 12,
            //Vector
            si_DY_DOUBLE_VEC = 13,
            si_DY_INTEGER_VEC = 14,
            si_DY_STRING_VEC = 15,

            //Matix
            //si_INTEGER_MAT,
            //si_DOUBLE_MAT,
            //string variable
            //table
            si_TABLE = 100
        }

        public enum sinter_IOMode
        {
            // this type describes wheather a variable is input or output for a simulation
            si_IN,
            //input to simulation
            si_OUT
            //output form simulation
        }


        public enum sinter_IOError
        {
            //error codes for this class
            si_OKAY,
            // everything went fine
            si_OUT_OF_RANGE,
            // trying to access out side range of an array
            si_TYPE_MISMATCH,
            // trying to do somethig with incompatable types
            si_GET_ERROR,
            // could not receive value from simulation
            si_SET_ERROR,
            // could not send value to a simulation
            si_NOT_VECTOR,
            // trying to do vector things on a nonvector
            si_NOT_MATRIX,
            // trying to do matrix things on a nonmatrix
            si_NOT_TABLE,
            // trying to do table things to a nontable
            si_NOT_SCALAR
            // trying to do scalar thing to a nonscalar
        }

        #endregion enums

        #region data
        //store error code
        protected sinter_IOError o_error;

        //the col position of the variable on spreadsheet
        private int o_colOff;

        //the row position of the variable on spreadsheet
        private int o_rowOff;

        //a string used to locate the varaible in a simulation program, may be more than one
        protected string[] o_addressStrings;

        //-------------------------------------------
        // Object variables
        //-------------------------------------------
        //the value of a variable
        protected object o_value;
        //the minimum value
        protected object o_min;
        //the maximum value
        protected object o_max;
        //default value
        protected object o_default;
        //the name for a variable
        protected string o_name;
        //a description of the variable
        protected string o_description;
        //a string containing units
        public string o_units;
        //a string containing the default units
        public string o_defaultUnits;


        //the type of data stored
        protected sinter_IOType o_type;
        //the IO mode, input or output
        protected sinter_IOMode o_mode;
        //The name of the setting variable.  Also used to determine if this is a setting or not
        protected string o_settingName;


        //If this variable is part of a table, this is the table name and indicies
        //If it is not part of a table, the o_tableName will be null
        public sinter_Table o_table = null;
        public string o_tableName = null;
        public int o_tableRow = 0;
        public int o_tableCol = 0;

        #endregion data

        #region constructors
        //----------------------------------
        //Constuctor
        //----------------------------------

        public sinter_Variable()
            : base()
        {
            // the constructor method
            // set some typlical defaults
            o_error = sinter_IOError.si_OKAY;
            o_mode = sinter_IOMode.si_IN;
            o_type = sinter_IOType.si_DOUBLE;
            o_units = null;
            o_defaultUnits = null;
        }

        public sinter_Variable(sinter_DynamicScalar rhs)
        {
            addressStrings = rhs.addressStrings;
            name = rhs.name;
            mode = rhs.mode;
            description = rhs.description;
            table = rhs.table;
            tableName = rhs.tableName;
            tableCol = rhs.tableCol;
            tableRow = rhs.tableRow;
            units = rhs.units;
            defaultUnits = rhs.defaultUnits;

            Value = rhs.Value;
            maximum = rhs.maximum;
            minimum = rhs.minimum;
            dfault = rhs.dfault;

            switch (rhs.type)
            {
                case sinter_Variable.sinter_IOType.si_DY_DOUBLE:
                    type = sinter_Variable.sinter_IOType.si_DOUBLE;
                    break;
                case sinter_Variable.sinter_IOType.si_DY_INTEGER:
                    type = sinter_Variable.sinter_IOType.si_INTEGER;
                    break;
                case sinter_Variable.sinter_IOType.si_DY_STRING:
                    type = sinter_Variable.sinter_IOType.si_STRING;
                    break;
            }
        }

        /**
         * This version of init attempts to discover as much as possible about the variable automatically.
         * This is useful for the GUI, when the user selects a variable off the tree we need to try to figure out all about it.
         **/
        public virtual void init(sinter_Sim sim, sinter.sinter_Variable.sinter_IOType type, string[] addStrings)
        {
            o_addressStrings = addStrings;
            IList<string> splitPath = sim.parsePath(o_addressStrings[0]);
            o_name = getVariableName(sim, addStrings[0]);
            o_mode = sinter_IOMode.si_IN; //Default to input
            o_type = type;
            o_description = null;
            o_table = null;
            o_tableName = null;
            o_tableCol = 0;
            o_tableRow = 0;

            makeValue();
            recvFromSim(sim);
            setDefaultToValue();
        }


        public virtual void init(string thisName, sinter_IOMode iomode, sinter_IOType thisType, string desc, string[] addStrings)
        {
            o_name = thisName;
            o_mode = iomode;
            o_type = thisType;
            o_description = desc;
            o_addressStrings = addStrings;
            o_table = null;
            o_tableName = null;
            o_tableCol = 0;
            o_tableRow = 0;

            makeValue();
            determineIsSetting();
        }


        public virtual void makeValue()
        {
            //
            // Setup the object to store a scalar value
            //
            o_error = sinter_IOError.si_OKAY;
            if (o_type == sinter_IOType.si_DOUBLE)
            {
                o_value = Convert.ToDouble(0.0);
                o_max = Convert.ToDouble(0.0);
                o_min = Convert.ToDouble(0.0);
                o_default = Convert.ToDouble(0.0);
            }
            else if (o_type == sinter_IOType.si_INTEGER)
            {
                o_value = Convert.ToInt32(0);
                o_max = Convert.ToInt32(0);
                o_min = Convert.ToInt32(0);
                o_default = Convert.ToInt32(0);
            }
            else if (o_type == sinter_IOType.si_STRING)
            {
                o_value = "";
                o_max = "";
                o_min = "";
                o_default = "";
            }
            else
            {
                o_error = sinter_IOError.si_NOT_SCALAR;
            }
        }
        #endregion constructors

        #region json

        // Since this is a scalar, the JObject should be a scalar
        public virtual void setInput(string varName, int row, int col, JToken jvalue, string in_units)
        {
            if (isOutput)
            {
                throw new ArgumentException(string.Format("Variable {0} is an output varaible.  It should not be in the input list.", varName));
            }

            Value = sinter_HelperFunctions.convertJTokenToNative(jvalue);
            units = in_units;
        }

        public virtual JToken getOutput()
        {
            JToken jtok = null;

            if (type == sinter_Variable.sinter_IOType.si_DOUBLE)
            {
                double val = Convert.ToDouble(Value);
                jtok = (JToken)val;
            }
            else if (type == sinter_Variable.sinter_IOType.si_INTEGER)
            {
                int val = Convert.ToInt32(Value);
                jtok = (JToken)val;
            }
            else if (type == sinter_Variable.sinter_IOType.si_STRING)
            {
                String val = (String)Value;
                jtok = (JToken)val;
            }
            else
            {
                throw new System.IO.IOException(String.Format("Sinter Variable {0} has unknown type {1}", name, type));
            }

            return jtok;
        }

        public Dictionary<String, Object> toJson()
        {
            Dictionary<String, Object> jsonOut = new Dictionary<String, Object>();
            jsonOut.Add("path", addressStrings);
            jsonOut.Add("type", typeString);
            jsonOut.Add("default", Value);
            jsonOut.Add("description", description);
            jsonOut.Add("units", defaultUnits); //Old config files don't have units.

            if (shouldOutputMinMax())
            {
                jsonOut.Add("min", minimum);
                jsonOut.Add("max", maximum);
            }
            return jsonOut;
        }

        private bool shouldOutputMinMax()
        {
            //Strings don't have min/max, so skip.  Vectors need to have every entry checked
            if (type == sinter_Variable.sinter_IOType.si_DOUBLE || type == sinter_Variable.sinter_IOType.si_INTEGER ||
                type == sinter_Variable.sinter_IOType.si_DY_DOUBLE || type == sinter_Variable.sinter_IOType.si_DY_INTEGER)
            {
                if (minimum != maximum && Convert.ToDouble(minimum) != 0.0)  //The default values are 0.0 for both
                {
                    return true;
                }
            }
            else if (type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC || type == sinter_Variable.sinter_IOType.si_INTEGER_VEC ||
                type == sinter_Variable.sinter_IOType.si_DY_DOUBLE_VEC || type == sinter_Variable.sinter_IOType.si_DY_INTEGER_VEC)
            {
                sinter_Vector thisVec = (sinter_Vector)this;
                bool hasDifference = false;
                for (int ii = 0; ii < thisVec.size; ++ii)
                {
                    if (thisVec.getElementMin(ii) != thisVec.getElementMax(ii) && Convert.ToDouble(thisVec.getElementMin(ii)) != 0)  //The default values are 0.0 for both
                    {
                        hasDifference = true;
                        break;
                    }
                }
                if (hasDifference)  //JSON should be able to figure out these are arrays.
                {
                    return true;
                }
            }
            return false;
        }

        #endregion json

        #region errors
        public sinter_IOError err
        {
            // read the error code for a variable
            get { return o_error; }
        }

        public void clearErr()
        {
            o_error = sinter_IOError.si_OKAY;
        }
        #endregion errors

        #region tostring
        //Doesn't seem to be used.  Was probably used for multidimensionaly arrays to write out the array bounds.
        public static string bounds2String(int[] bounds)
        {
            StringBuilder buff = new StringBuilder();
            StringWriter swbuff = new StringWriter(buff);
            String retString;
            try
            {
                if (bounds == null ||
                    bounds.Length <= 0)
                {
                    return "";
                }



                swbuff.Write("[");

                for (int ii = 0; ii < bounds.Length; ++ii)
                {
                    swbuff.Write(bounds[ii]);
                    if (ii < (bounds.Length - 1))
                    {
                        swbuff.Write(",");
                    }
                }
                swbuff.Write("[");
                retString = swbuff.ToString();
            }
            finally
            {
                swbuff.Close();
            }
            return retString;
        }

        public static void string2Type(String thisTypeString, ref sinter_IOType thisType, ref int[] sizes)
        {
            string[] fields = null;
            string[] sz = null;

            //Checks if it's a table the bounds will be in brackets
            fields = thisTypeString.Split('[');
            for (int i = 0; i <= fields.Length - 1; i++)
            {
                fields[i] = fields[i].Trim();
            }
            if (fields.Length == 2)
                if (fields[1][fields[1].Length - 1] == ']') //Last character in fields[1]
                    fields[1] = fields[1].Substring(0, fields[1].Length - 1);  //Drop Last char (Bracket)
            if (fields.Length == 2)
            {
                sz = fields[1].Split(',');
                for (int i = 0; i <= sz.Length - 1; i++)
                {
                    sz[i] = sz[i].Trim();
                }
                sizes = new int[sz.Length];
                for (int ii = 0; ii < sz.Length; ++ii)
                {
                    sizes[ii] = Convert.ToInt32(sz[ii]);
                }
            }

            thisType = sinter_IOType.si_UNKNOWN;

            //End table bounds parsing stuff
            switch (fields[0])
            {
                case "int":
                    if (sizes == null || sizes.Length == 0)
                    {
                        thisType = sinter_IOType.si_INTEGER;
                    }
                    else if (sizes.Length == 1)
                    {
                        thisType = sinter_IOType.si_INTEGER_VEC;
                    }
                    break;
                case "double":
                    if (sizes == null || sizes.Length == 0)
                    {
                        thisType = sinter_IOType.si_DOUBLE;
                    }
                    else if (sizes.Length == 1)
                    {
                        thisType = sinter_IOType.si_DOUBLE_VEC;
                    }
                    break;
                case "string":
                    if (sizes == null || sizes.Length == 0)
                    {
                        thisType = sinter_IOType.si_STRING;
                    }
                    else if (sizes.Length == 1)
                    {
                        thisType = sinter_IOType.si_STRING_VEC;
                    }
                    break;
                case "table":
                    thisType = sinter_IOType.si_TABLE;
                    break;
                default:
                    thisType = sinter_IOType.si_UNKNOWN;
                    break;
            }
        }

        //Sizes is an array to support possible multidimensional arrays...
        //Also, dynamic type names are the same as their non-dynamic counterpart
        public static string type2typeString(sinter_IOType thisType, int[] sizes)
        {
            if (thisType == sinter_IOType.si_INTEGER || thisType == sinter_IOType.si_DY_INTEGER)
            {
                return "int";
            }
            else if (thisType == sinter_IOType.si_DOUBLE || thisType == sinter_IOType.si_DY_DOUBLE)
            {
                return "double";
            }
            else if (thisType == sinter_IOType.si_STRING || thisType == sinter_IOType.si_DY_STRING)
            {
                return "string";
            }
            else if (thisType == sinter_IOType.si_INTEGER_VEC || thisType == sinter_IOType.si_DY_INTEGER_VEC)
            {
                return "int[" + Convert.ToString(sizes[0]) + "]";
            }
            else if (thisType == sinter_IOType.si_DOUBLE_VEC || thisType == sinter_IOType.si_DY_DOUBLE_VEC)
            {
                return "double[" + Convert.ToString(sizes[0]) + "]";
            }
            else if (thisType == sinter_IOType.si_STRING_VEC || thisType == sinter_IOType.si_DY_STRING_VEC)
            {
                return "string[" + Convert.ToString(sizes[0]) + "]";
            }

            return "unknown";
        }

        #endregion tostring

        #region to-and-from-sim

        //---------------------------------------
        // Comunication with Simulation Program
        //---------------------------------------

        public void sendSetting(sinter_Sim o_sim)
        {
            if (isSetting)
            {
                o_sim.setSetting(o_settingName, Value);
            }
        }


        //<Summary>
        //send value to simulation
        //</Summary>
        public virtual void sendToSim(sinter_InteractiveSim o_sim)
        {
            if ((mode != sinter_IOMode.si_OUT) && (!isSetting))
            {
                object t_value = o_value;
                if (o_type == sinter_IOType.si_DOUBLE)  //Really, only doubles support type conversion
                {
                    t_value = unitsConversion(units, defaultUnits, Convert.ToDouble(o_value));
                }
                //Now that we have the correct converted value, pass it into each address inside the simulation
                for (int addressIndex = 0; addressIndex <= (addressStrings.Length - 1); addressIndex++)
                {
                    o_sim.sendValueToSim(addressStrings[addressIndex], t_value);
                }
            }
        }

        //This is for querying the simulation for the current units in the simulation.  So it sets both units and default units.
        //This isn't for use during a normal simulation, it's mostly part of the sinter config generation process.
        public virtual void initializeUnits(sinter_Sim o_sim)
        {
            if (!isSetting)
            {
                if (o_units == null || o_units == "")  //If the user as already set some units, we want to keep those
                {
                    o_units = o_sim.getCurrentUnits(addressStrings[0]);
                    o_defaultUnits = o_units;
                }
            }
        }

        //This is for querying the simulation for the current units in the simulation.  So it sets both units and default units.
        //This isn't for use during a normal simulation, it's mostly part of the sinter config generation process.
        public virtual void initializeDescription(sinter_Sim o_sim)
        {
            if (!isSetting)
            {
                if (o_description == null || o_description == "")  //If the user as already set some units, we want to keep those
                {
                    o_description = o_sim.getCurrentDescription(addressStrings[0]);
                }
            }
        }

        //<Summary>
        //get value from a simulation
        //</Summary>
        public virtual void recvFromSim(sinter_Sim o_sim)
        {
            //this reads back the inputs
            // It's handy for default generation.
            //            try
            //            {
            if (!isSetting)
            {
                if (type == sinter_IOType.si_DOUBLE)
                {
                    o_value = o_sim.recvValueFromSim<double>(addressStrings[0]);
                }
                else if (type == sinter_IOType.si_INTEGER)
                {
                    o_value = o_sim.recvValueFromSim<int>(addressStrings[0]);
                }
                else if (type == sinter_IOType.si_STRING)
                {
                    o_value = o_sim.recvValueFromSim<String>(addressStrings[0]);
                }
            }
        }

        public virtual string getVariableName(sinter_Sim o_sim, string path)
        {
            //Get the simulator's name suggestion
            String returnName = o_sim.getCurrentName(path);
            return returnName;
/*
            IList<String> parsedPath = o_sim.parsePath(path);
            //If the name that comes back is just the path, try to make it the last part of the path, which is usually the most useful bit.
            if (returnName == path)
            {
                if (parsedPath.Count <= 1)  //If the path is only one segment long, just return it.  We have no useful guess.
                {
                    return returnName;
                } else {
                    returnName = parsedPath[parsedPath.Count - 1];
                }
            }

            if (String.Compare(returnName, "Value", true) != 0)
            {//If the last element in the path is not "Value" (often appears in ACM Vectors) return it.
                returnName = parsedPath[parsedPath.Count - 1];
            }
            else
            { //If the last element in the path IS "Value" get the second to last value, which is more useful usually.
                return parsedPath[parsedPath.Count - 2];
            }
        
            return returnName;
 */
        }

        #endregion to-and-from-sim

        #region meta-properties

        public string[] addressStrings
        {
            // get of set a strings that can be used to look up the varaible value in a simulation program
            get { return o_addressStrings; }
            set { o_addressStrings = value; }
        }

        public sinter_Table table
        {
            //The table this variable is part of, if it is part of one.  Null if not part of a table.
            get { return o_table; }
            set { o_table = value; }
        }


        public string tableName
        {
            //Name of table variable is in, null if not in a table
            get { return o_tableName; }
            set { o_tableName = value; }
        }

        public int tableRow
        {
            //Row in table of variable, if not in a table doesn't matter
            get { return o_tableRow; }
            set { o_tableRow = value; }
        }

        public int tableCol
        {
            //Col in table of variable, if not in a table doesn't matter
            get { return o_tableCol; }
            set { o_tableCol = value; }
        }


        public virtual bool isTable
        {
            get
            {
                return false;
            }
        }


        //-----------------------------------
        // type checks
        //-----------------------------------

        public virtual bool isScalar
        {
            // returns true if the io object is a scaler
            get
            {
                return true;
            }
        }
        public virtual bool isVec
        {
            // returns true if the io object is a vector
            get
            {
                return false;
            }

        }

        public virtual bool isInput
        {
            get
            {
                if (o_mode == sinter_IOMode.si_IN)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public virtual bool isOutput
        {
            get
            {
                if (o_mode == sinter_IOMode.si_OUT)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        public virtual bool isDynamicVariable
        {
            get { return false; }
        }


        public virtual string settingName
        {
            get { return o_settingName; }
            set { o_settingName = value; }
        }

        public virtual bool isSetting
        {
            get
            {
                return o_settingName != null;
            }
        }

        public virtual string typeString
        {
            get
            {
                return type2typeString(type, null);
            }
            set
            {
                int[] bounds = null;
                string2Type(value, ref o_type, ref bounds);
            }
        }


        public void determineIsSetting()
        {
            string[] fields = addressStrings[0].Split('(');
            if (fields[0] == "setting")
            {
                if (fields[1][fields[1].Length - 1] == ')') //Last character in fields[1]
                    fields[1] = fields[1].Substring(0, fields[1].Length - 1);  //Drop Last char (Bracket)
                else
                    throw new System.IO.IOException("Synax Error: Setting " + addressStrings[0] + " missing closing paren.");

                if (addressStrings.Length > 1)
                {
                    throw new System.IO.IOException("Settings should not have multiple address string. " + name + " does.");
                }
                o_settingName = fields[1];

            }
        }

        public sinter_Variable.sinter_IOError typeCheck()
        {
            sinter_Variable.sinter_IOError functionReturnValue = default(sinter_Variable.sinter_IOError);
            //
            // I just started this Its not ready for anything
            //
            System.Type otype = default(System.Type);
            System.Type etype = default(System.Type);
            otype = typeof(ValueType);

            etype = typeof(int);
            switch (type)
            {
                case sinter_IOType.si_DOUBLE:
                    etype = typeof(double);
                    break;
                case sinter_IOType.si_INTEGER:
                    etype = typeof(int);
                    break;
                default:
                    functionReturnValue = sinter_Variable.sinter_IOError.si_OKAY;
                    break;
            }

            if ((object.ReferenceEquals(etype, otype)))
            {
                functionReturnValue = sinter_Variable.sinter_IOError.si_OKAY;
            }
            else
            {
                functionReturnValue = sinter_IOError.si_TYPE_MISMATCH;
            }
            return functionReturnValue;
        }

        #endregion meta-properties

        #region excel-formatting

        //------------------------------------
        // Layout and formating
        //------------------------------------

        public void setOffset(int row, int col)
        {
            //set the row and column position
            //for spreadsheet layout
            o_rowOff = row;
            o_colOff = col;

        }

        public int rowOff
        {
            // this is a column offset for laying out a spreadsheet
            get { return o_rowOff; }
            set { o_rowOff = value; }
        }

        public int colOff
        {
            // this is a column offset for laying out a spreadsheet
            get { return o_colOff; }
            set { o_colOff = value; }
        }


        #endregion excel-formatting

        #region data-properties

        //----------------------------------
        // Main properties
        //----------------------------------


        public virtual object Value
        {
            // value is the data stored in this object
            // value is an object any type of data can be stored
            get { return o_value; }
            set
            {
                if (type == sinter_IOType.si_DOUBLE)
                {
                    o_value = Convert.ToDouble(value);
                }
                else if (type == sinter_IOType.si_INTEGER)
                {
                    o_value = Convert.ToInt32(value);
                }
                else if (type == sinter_IOType.si_STRING)
                {
                    o_value = Convert.ToString(value);
                }
                else
                {
                    o_value = value;
                }
            }
        }

        public object minimum
        {
            // value is the data stored in this object
            // declaring it an object lets any type of data be stored
            get { return o_min; }
            set
            {
                if (type == sinter_IOType.si_DOUBLE)
                {
                    o_min = Convert.ToDouble(value);
                }
                else if (type == sinter_IOType.si_INTEGER)
                {
                    o_min = Convert.ToInt32(value);
                }
                else if (type == sinter_IOType.si_STRING)
                {
                    o_min = Convert.ToString(value);
                }
                else
                {
                    o_min = value;
                }
            }
        }

        public object maximum
        {
            // value is the data stored in this object
            // declaring it an object lets any type of data be stored
            get { return o_max; }
            set
            {
                if (type == sinter_IOType.si_DOUBLE)
                {
                    o_max = Convert.ToDouble(value);
                }
                else if (type == sinter_IOType.si_INTEGER)
                {
                    o_max = Convert.ToInt32(value);
                }
                else if (type == sinter_IOType.si_STRING)
                {
                    o_max = Convert.ToString(value);
                }
                else
                {
                    o_max = value;
                }
            }
        }

        public object dfault
        {
            // value is the data stored in this object
            // declaring it an object lets any type of data be stored
            get { return o_default; }
            set
            {
                if (type == sinter_IOType.si_DOUBLE)
                {
                    o_default = Convert.ToDouble(value);
                }
                else if (type == sinter_IOType.si_INTEGER)
                {
                    o_default = Convert.ToInt32(value);
                }
                else if (type == sinter_IOType.si_STRING)
                {
                    o_default = Convert.ToString(value);
                }
                else
                {
                    o_default = value;
                }
            }
        }
        public sinter_IOMode mode
        {
            // this in the IO mode, input or output or both
            get { return o_mode; }
            set { o_mode = value; }
        }
        public sinter_IOType type
        {
            // This is the expected type of this object
            // It may be important to check that an integer
            // is really stored if you really want an integer
            get { return o_type; }
            set { o_type = value; }  //TODO: Convert the default and value here too
        }
        public string name
        {
            //this is the name of the object
            get { return o_name; }
            set { o_name = value; }
        }
        public string description
        {
            // this is a description of the object
            // it would be nice if units were in 
            // the description too
            get { return o_description; }
            set { o_description = value; }
        }
        public string units
        {
            //this is the name of the object
            get { return o_units; }
            set { o_units = value; }
        }
        public string defaultUnits
        {
            //this is the name of the object
            get { return o_defaultUnits; }
            set { o_defaultUnits = value; }
        }
        #endregion data-properties

        #region default-handling
        public virtual void setDefaultToValue()
        {
            dfault = o_value;
            o_defaultUnits = o_units;
        }


        public virtual void resetToDefault()
        {
            o_value = dfault;
            o_units = o_defaultUnits;
        }

        #endregion default-handling

        #region units-handling
        // Converts units from the input variable unit type, to the sinter config file type for this variable
        public double unitsConversionToSim()
        {
            return unitsConversion(units, defaultUnits, (double)Value);
        }

        public double unitsConversion(string sourceUnits, string targetUnits, double i_val)
        {
            double t_value = i_val;
            if (sourceUnits != null && sourceUnits != "")
            {
                if (targetUnits == "" || targetUnits == null)
                {
                    throw new System.IO.IOException(
                        String.Format("ERROR: Variable {0} doesn't allow units according to the sinter config file, but the input variable has units {1}", name, sourceUnits));
                }
                else
                {
                    if (!sourceUnits.Equals(targetUnits, StringComparison.Ordinal))  //Ok, if both unit strings are set, and they are different, convert from units -> defaultUnits
                    {
                        if (!sinter_Sim.ccsiUnits.CheckUnits(sourceUnits, targetUnits))
                        {
                            throw new System.IO.IOException(
                              String.Format("ERROR: Input file defined units {0} and Sinter Config file defined unit {1} are not compatiable!", sourceUnits, targetUnits));
                        }
                        t_value = sinter_Sim.ccsiUnits.ConvertUnits(i_val, sourceUnits, targetUnits);
                    }
                }
            }
            return t_value;
        }

        public double[] unitsConversion(string sourceUnits, string targetUnits, double[] i_val)
        {
            double[] t_value = (double[])i_val.Clone();
            if (sourceUnits != null && sourceUnits != "")
            {
                if (targetUnits == "" || targetUnits == null)
                {
                    throw new System.IO.IOException(
                        String.Format("ERROR: Variable {0} doesn't allow units according to the sinter config file, but the input variable has units {1}", name, sourceUnits));
                }
                else
                {
                    if (!sourceUnits.Equals(targetUnits, StringComparison.Ordinal))  //Ok, if both unit strings are set, and they are different, convert from units -> defaultUnits
                    {
                        if (!sinter_Sim.ccsiUnits.CheckUnits(sourceUnits, targetUnits))
                        {
                            throw new System.IO.IOException(
                              String.Format("ERROR: Input file defined units {0} and Sinter Config file defined unit {1} are not compatiable!", sourceUnits, targetUnits));
                        }

                        for (int ii = 0; ii < i_val.Length; ++ii)
                        {
                            t_value[ii] = sinter_Sim.ccsiUnits.ConvertUnits(i_val[ii], sourceUnits, targetUnits);
                        }
                    }
                }
            }
            return t_value;

        }

        #endregion units-handling

    }
}
