using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sinter
{
    //
    // This is a generic class for storing input and output
    // for simulations.  It can be inherited to create
    // IO object classes for specific simulation types.
    //

    public class sinter_Vector : sinter_Variable
    {
        #region data

        //column labels for vector or matrix
        private string[] o_rowLabels;

        //column strings used in tables to make systematic address strings 
        private string[] o_rowStrings;

        //Some simulators (eg ACM) don't have a particular ordering for their arrays, allow skipping elements, etc.
        private int[] o_vectorindicies;

        #endregion data

        #region constructors
        //----------------------------------
        //Constuctor
        //----------------------------------

        public sinter_Vector()
            : base()
        {
            // the constructor method
            // set some typlical defaults
            
            type = sinter_IOType.si_DOUBLE_VEC;
        }

        //Convert from a steady state variable to a dynamic one
        public sinter_Vector(sinter_DynamicVector rhs)
        {
            addressStrings = rhs.addressStrings;
            name = rhs.name;
            mode = rhs.mode;
            description = rhs.description;
            table = rhs.table;
            tableName = rhs.tableName;
            tableCol = rhs.tableCol;
            tableRow = rhs.tableRow;

            Value = rhs.Value;
            maximum = rhs.maximum;
            minimum = rhs.minimum;
            dfault = rhs.dfault;
            units = rhs.units;
            defaultUnits = rhs.defaultUnits;

            switch (rhs.type)
            {
                case sinter_Variable.sinter_IOType.si_DY_DOUBLE_VEC:
                    type = sinter_Variable.sinter_IOType.si_DOUBLE_VEC;
                    break;
                case sinter_Variable.sinter_IOType.si_INTEGER_VEC:
                    type = sinter_Variable.sinter_IOType.si_INTEGER_VEC;
                    break;
                case sinter_Variable.sinter_IOType.si_STRING_VEC:
                    type = sinter_Variable.sinter_IOType.si_STRING_VEC;
                    break;
            }
        }

        /**
         * This version of init attempts to discover as much as possible about the variable automatically.
         * This is useful for the GUI, when the user selects a variable off the tree we need to try to figure out all about it.
        **/
        public override void init(sinter_Sim sim, sinter.sinter_Variable.sinter_IOType type, string[] addStrings)
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

            int vecsize = sim.guessVectorSize(o_addressStrings[0]);

            makeVector(vecsize);
            recvFromSim(sim);
            setDefaultToValue();
            resetToDefault();
        }

        public override void init(string thisName, sinter_IOMode iomode, sinter_IOType thisType, string desc, string[] addStrings)
        {
            init(thisName, iomode, thisType, desc, addStrings, 1);  //Init a unit length vector (0 is bad)  Will be increased later
        }

        public void init(string thisName, sinter_IOMode iomode, sinter_IOType thisType, string desc, string[] addStrings, int nn)
        {
            base.init(thisName, iomode, thisType, desc, addStrings);

            makeVector(nn);
            
            determineIsSetting();
        }


        public virtual void makeVector(int rows)
        {
            //
            // Setup the object to store a integer or double vector 
            //
            o_error = sinter_IOError.si_OKAY;
            //clear error flag
            if (type == sinter_IOType.si_DOUBLE_VEC)
            {
                double[] da = new double[rows];
                double[] da_min = new double[rows];
                double[] da_max = new double[rows];
                double[] da_def = new double[rows];
                o_value = da;
                o_min = da_min;
                o_max = da_max;
                o_default = da_def;
            }
            else if (type == sinter_IOType.si_INTEGER_VEC)
            {
                int[] ia = new int[rows];
                int[] ia_min = new int[rows];
                int[] ia_max = new int[rows];
                int[] ia_def = new int[rows];
                o_value = ia;
                o_min = ia_min;
                o_max = ia_max;
                o_default = ia_def;
            }
            else if (type == sinter_IOType.si_STRING_VEC)
            {
                String[] ia = new String[rows];
                String[] ia_min = new String[rows];
                String[] ia_max = new String[rows];
                String[] ia_def = new String[rows];
                o_value = ia;
                o_min = ia_min;
                o_max = ia_max;
                o_default = ia_def;
            }

            else
            {
                // error if trying to make a vector in a object not of vector type
                o_error = sinter_IOError.si_NOT_VECTOR;
            }
        }
        #endregion constructors

        #region json
        // Since this is a scalar, the JObject should be a scalar
        public override void setInput(string varName, int row, int col, JToken jvalue, string in_units)
        {
            if (isOutput)
            {
                throw new ArgumentException(string.Format("Variable {0} is an output varaible.  It should not be in the input list.", varName));
            }

            units = in_units;

            if (row >= 0)   //Editing a single entry of a vector
            {
                if (row >= size)
                {
                    throw new ArgumentException(string.Format("Vector {0} got an index out of range.  Upper bound: {1}, bad index: {2}", varName, (int)size - 1, row));
                }
                setElement(row, sinter_HelperFunctions.convertJTokenToNative(jvalue));
            }
            else  //If the use passed in a whole vector, we have to set values one by one
            {
                Newtonsoft.Json.Linq.JArray jsonArray = (Newtonsoft.Json.Linq.JArray)jvalue;
                if (size != jsonArray.Count)
                {
                    if (varName == "TimeSeries")  //Special case to allow input files to set TimeSeries size
                    {
                        makeVector(jsonArray.Count);
                    }
                    else
                    {
                        throw new ArgumentException(string.Format("Vector {0} got an input array that's the wrong size.  Vector length: {1}, input length: {2}", varName, size, jsonArray.Count));
                    }
                    }
                for (int ii = 0; ii < size; ++ii)
                {
                    setElement(ii, sinter_HelperFunctions.convertJTokenToNative(jsonArray[ii]));
                }

            }
        }

        public override JToken getOutput()
        {
            Newtonsoft.Json.Linq.JArray jsonArray = new Newtonsoft.Json.Linq.JArray(Value);
            return jsonArray;
        }
        #endregion json

        #region tostring

        public override string typeString
        {
            get
            {
                return sinter_Variable.type2typeString(type, new int[1] {size});
            }
            //So far I don't really need this functionality here, and I wasn't sure of the best way to 
            //do vectors and matricies
            //set
            //{
            //    if (value.Equals("int", StringComparison.Ordinal))
            //    {
            //        type = sinter_IOType.si_INTEGER;
            //    }
            //    else if (value.Equals("double", StringComparison.Ordinal))
            //    {
            //        type = sinter_IOType.si_DOUBLE;
            //    }
            //    else if (value.Equals("string", StringComparison.Ordinal))
            //    {
            //        type = sinter_IOType.si_STRING;
            //    }
            //    else if (value.Equals("table", StringComparison.Ordinal))
            //    {
            //        type = sinter_IOType.si_TABLE;
            //    }
            //    else if (value.Equals("intvector", StringComparison.Ordinal))
            //    {
            //        type = sinter_IOType.si_INTEGER_VEC;
            //    }
            //    else if (value.Equals("doublevector", StringComparison.Ordinal))
            //    {
            //        type = sinter_IOType.si_DOUBLE_VEC;
            //    }
            //    else if (value.Equals("intmatrix", StringComparison.Ordinal))
            //    {
            //        type = sinter_IOType.si_INTEGER_MAT;
            //    }
            //    else if (value.Equals("doublematrix", StringComparison.Ordinal))
            //    {
            //        type = sinter_IOType.si_DOUBLE_MAT;
            //    }
            //    type = sinter_IOType.si_UNKNOWN;
            //}
        }

        #endregion tostring

        #region data-properties
        //------------------------------------
        // vector/matrix things
        //------------------------------------
        
        public virtual object getElement(int i)
        {
           if (type == sinter_IOType.si_DOUBLE_VEC)
           {
              return ((double[])o_value)[i];
           }
           else if (type == sinter_IOType.si_INTEGER_VEC)
           {
               return ((int[])o_value)[i];
           }
           else if (type == sinter_IOType.si_STRING_VEC)
           {
               return ((String[])o_value)[i];
           } else {
               throw new System.IO.IOException(name + " is not a vector.");
           }
        }


        public virtual void setElement(int i, object value)
        {

            if (type == sinter_IOType.si_DOUBLE_VEC)
            {
                ((double[])o_value)[i] = Convert.ToDouble(value);
            }
            else if (type == sinter_IOType.si_INTEGER_VEC)
            {
                ((int[])o_value)[i] = Convert.ToInt32(value);
            }
            else if (type == sinter_IOType.si_STRING_VEC)
            {
                ((String[])o_value)[i] = (String)value;
            }
            else
            {
                throw new System.IO.IOException(name + " is not a vector."); 
            }
        }


        public virtual object getElementMin(int ii)
        {
            if (type == sinter_IOType.si_DOUBLE_VEC)
            {
                return ((double[])o_min)[ii];
            }
            else if (type == sinter_IOType.si_INTEGER_VEC)
            {
                return ((int[])o_min)[ii];
            }
            else if (type == sinter_IOType.si_STRING_VEC)
            {
                return ((String[])o_min)[ii];
            }
            else
            {
                throw new System.IO.IOException(name + " is not a vector.");
            }
        }

        public virtual void setElementMin(int ii, object value)
        {

            if (type == sinter_IOType.si_DOUBLE_VEC)
            {
                ((double[])o_min)[ii] = (double)value;
            }
            else if (type == sinter_IOType.si_INTEGER_VEC)
            {
                ((int[])o_min)[ii] = (int)value;
            }
            else if (type == sinter_IOType.si_STRING_VEC)
            {
                ((String[])o_min)[ii] = (String)value;
            }
            else
            {
                throw new System.IO.IOException(name + " is not a vector.");
            }
        }


        public virtual object getElementMax(int ii)
        {
            if (type == sinter_IOType.si_DOUBLE_VEC)
            {
                return ((double[])o_max)[ii];
            }
            else if (type == sinter_IOType.si_INTEGER_VEC)
            {
                return ((int[])o_max)[ii];
            }
            else if (type == sinter_IOType.si_STRING_VEC)
            {
                return ((String[])o_max)[ii];
            }
            else
            {
                throw new System.IO.IOException(name + " is not a vector.");
            }
        }

        public virtual  void setElementMax(int ii, object value)
            {
                if (type == sinter_IOType.si_DOUBLE_VEC)
                {
                    ((double[])o_max)[ii] = (double)value;
                }
                else if (type == sinter_IOType.si_INTEGER_VEC)
                {
                    ((int[])o_max)[ii] = (int)value;
                }
                else if (type == sinter_IOType.si_STRING_VEC)
                {
                    ((String[])o_max)[ii] = (String)value;
                }
                else
                {
                    throw new System.IO.IOException(name + " is not a vector.");
                } 
            }
        

        public virtual  object getElementDefault(int ii)
            {
                if (type == sinter_IOType.si_DOUBLE_VEC)
                {
                    return ((double[])o_default)[ii];
                }
                else if (type == sinter_IOType.si_INTEGER_VEC)
                {
                    return ((int[])o_default)[ii];
                }
                else if (type == sinter_IOType.si_STRING_VEC)
                {
                    return ((String[])o_default)[ii];
                }
                else
                {
                    throw new System.IO.IOException(name + " is not a vector.");
                }
            }

        public virtual void setElementDefault(int ii, object value)
        {
            if (type == sinter_IOType.si_DOUBLE_VEC)
            {
                ((double[])o_default)[ii] = (double)value;
            }
            else if (type == sinter_IOType.si_INTEGER_VEC)
            {
                ((int[])o_default)[ii] = (int)value;
            }
            else if (type == sinter_IOType.si_STRING_VEC)
            {
                ((String[])o_default)[ii] = (String)value;
            }
            else
            {
                throw new System.IO.IOException(name + " is not a vector.");
            }
        }

        #endregion data-properties

        #region meta-properties
        public int size
        {
            get
            {
                if (type == sinter_IOType.si_DOUBLE_VEC)
                {
                    return ((double[])o_value).Length;
                }
                else if(type == sinter_IOType.si_INTEGER_VEC)
                {
                    return ((int[])o_value).Length;
                }
                else if (type == sinter_IOType.si_STRING_VEC)
                {
                    return ((String[])o_value).Length;
                }
                else
                {
                    throw new System.IO.IOException(name + " is not a vector.");
                }
            }
        }

        public int[] get_vectorIndicies(sinter_Sim sim)
        {
            if (o_vectorindicies == null) 
            {
                o_vectorindicies = sim.getVectorIndicies(o_addressStrings[0], size);
            }
        
            return o_vectorindicies;
        }

        //
        //The row label array does not necessarily match
        //the length of a vector so need to check for out of bounds
        //The reason its independant like this is that some vector
        //may change length like a vector of concentrations on a
        //tray in a distalation column and the number of trays
        //may be a changing quantity
        //
        public string getRowLabel(int i)
        {
            if ((i < 0 | i >= rowLabelCount))
            {
                return i.ToString();
                //if out of range just return the index as the label
            }
            else
            {
                return o_rowLabels[i];
            }
        }
        public void setRowLabel(int i, string value)
        {
            if ((i >= 0 & i < rowLabelCount))
            {
                o_rowLabels[i] = value;
            }
        }


        public int rowLabelCount
        {
            // the number of row labels does not have to be the same a
            // a vector size
            get
            {
                if (o_rowLabels == null)
                    return 0;
                return o_rowLabels.Length;
            }
            set
            {
                Array.Resize(ref o_rowLabels, value);

            }
        }


        //----------------------------------------
        // Table Functions
        //----------------------------------------

        public int rowStringCount
        {
            get
            {
                if (o_rowStrings == null)
                    return 0;
                return o_rowStrings.Count();
            }
            set
            {
                Array.Resize(ref o_rowStrings, value);

            }
        }

        public string[] rowStrings
        {
            get { return o_rowStrings; }
            set { o_rowStrings = value; }
        }
        public string getRowString(int i)
        {
            return o_rowStrings[i];
        }
        public void setRowString(int i, string rowString)
        {
            o_rowStrings[i] = rowString;
        }

        //-----------------------------------
        // type checks
        //-----------------------------------

        public override bool isScalar
        {
            // returns true if the io object is a scaler
            get
            {
                return false;
            }
        }
        public override bool isVec
        {
            // returns true if the io object is a vector
            get
            {
                return true;
            }
        }

        public override bool isTable
        {
            get
            {
                return false;
            }
        }


        #endregion meta-properties

        #region to-and-from-sim
        //---------------------------------------
        // Comunication with Simulation Program
        //---------------------------------------

        //<Summary>
        //send value to simulation
        //</Summary>
        public override void sendToSim(sinter_InteractiveSim o_sim)
        {
            if ((mode != sinter_IOMode.si_OUT) && (!isSetting))
            {
                //This allows one to pass in simulation settings as a sinter varaible if it has a path of the form: setting(blah) = value
                //So, setting( is a "reserved word" in sinter variable pathes (Maybe a special character would be better?
                for (int addressIndex = 0; addressIndex <= (addressStrings.Length - 1); addressIndex++)
                {
                    if (type == sinter_IOType.si_DOUBLE_VEC)
                    {
                        double[] t_value = unitsConversion(units, defaultUnits, (double[])o_value);  //Do units conversion if required
                        o_sim.sendVectorToSim<double>(addressStrings[addressIndex],  (double[])t_value);
                    }
                    else if (type == sinter_IOType.si_DY_INTEGER_VEC)
                    {
                        o_sim.sendVectorToSim<int>(addressStrings[addressIndex], (int[])o_value);
                    }
                    else if (type == sinter_IOType.si_DY_STRING_VEC)
                    {
                        o_sim.sendVectorToSim<string>(addressStrings[addressIndex], (string[])o_value);
                    }
                    else
                    {
                        throw new NotImplementedException(string.Format("Unknown type {0} passed to sinter_Vector.sendToSim", type)); 
                    }

                }
            }
        }
        //<Summary>
        //get value from a simulation
        //</Summary>
        public override void recvFromSim(sinter_Sim o_sim)
        {

            if (!isSetting)
            {


                if (type == sinter_IOType.si_DOUBLE_VEC)
                {
                    o_sim.recvVectorFromSim<double>(addressStrings[0], get_vectorIndicies(o_sim), (double[])o_value);
                }

                else if (type == sinter_IOType.si_INTEGER_VEC)
                {
                    o_sim.recvVectorFromSim<int>(addressStrings[0], get_vectorIndicies(o_sim), (int[])o_value);
                }
                else
                {
                    o_sim.recvVectorFromSim<string>(addressStrings[0], get_vectorIndicies(o_sim), (string[])o_value);
                }

            }
        }

        public override void initializeUnits(sinter_Sim o_sim)
        {
            if (!isSetting)
            {
                o_units = o_sim.getCurrentUnits(addressStrings[0], get_vectorIndicies(o_sim));
                o_defaultUnits = o_units;
            }
        }

        //This is for querying the simulation for the current units in the simulation.  So it sets both units and default units.
        //This isn't for use during a normal simulation, it's mostly part of the sinter config generation process.
        public override void initializeDescription(sinter_Sim o_sim)
        {
            if (!isSetting)
            {
                if (o_description == null || o_description == "")  //If the user as already set some description, we want to keep those
                {
                    o_description = o_sim.getCurrentDescription(addressStrings[0]);
                }
            }
        }


        #endregion to-and-from-sim

        #region default-handling

        public override void resetToDefault()
        {
            int i = 0;
            for (i = 0; i <= size - 1; i++)
            {
                setElement(i, getElementDefault(i));
            }
        }


        #endregion default-handling 

    }
    
}
