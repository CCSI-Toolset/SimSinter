using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sinter
{
    // This is used when a dynamic (usually ACM) simulation has vector input and outputs.
    // Has to reimplement the stuff in sinter_DynamicScalar so it can inherit code from sinter_Vector instead.

    public class sinter_DynamicVector : sinter_Vector, sinter_IDynamicVariable

    {
        #region data

        int o_TimeSeriesIndex;
        int o_TimeSeriesLength;

        object o_TimeSeriesValues;

        #endregion data

        #region constructors
        //----------------------------------
        //Constuctor
        //----------------------------------

        public sinter_DynamicVector()
            : base()
        {
            o_TimeSeriesIndex = 0;
            o_TimeSeriesLength = 0;
        }

        //Convert from a steady state variable to a dynamic one
        public sinter_DynamicVector(sinter_Vector rhs)
        {
            o_TimeSeriesLength = 1;
            o_TimeSeriesValues = new double[1];
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
                case sinter_Variable.sinter_IOType.si_DOUBLE_VEC:
                    type = sinter_Variable.sinter_IOType.si_DY_DOUBLE_VEC;
                    break;
                case sinter_Variable.sinter_IOType.si_INTEGER_VEC:
                    type = sinter_Variable.sinter_IOType.si_DY_INTEGER_VEC;
                    break;
                case sinter_Variable.sinter_IOType.si_STRING_VEC:
                    type = sinter_Variable.sinter_IOType.si_DY_STRING_VEC;
                    break;
            }
            resetToDefault();
        }


        public override void init(sinter_Sim sim, sinter_IOType type, string[] addStrings)
        {
            throw new NotImplementedException("Dynamic Scalar does not implement init with no time series");
        }

        public override void init(string thisName, sinter_Variable.sinter_IOMode iomode, sinter_Variable.sinter_IOType thisType, string desc, string[] addStrings)
        {
            throw new NotImplementedException("Dynamic Scalar does not implement init with no time series");
        }
        /**
         * This version of init attempts to discover as much as possible about the variable automatically.
         * This is useful for the GUI, when the user selects a variable off the tree we need to try to figure out all about it.
         **/
        public virtual void init(sinter_InteractiveSim sim, sinter.sinter_Variable.sinter_IOType type, int TimeSeriesLen, string[] addStrings)
        {
            o_TimeSeriesLength = TimeSeriesLen;
            base.init(sim, type, addStrings);
        }


        public virtual void init(string thisName, sinter_IOMode iomode, sinter_IOType thisType, string desc, int TimeSeriesLen, string[] addStrings, int nn)
        {
            o_TimeSeriesLength = TimeSeriesLen;
            base.init(thisName, iomode, thisType, desc, addStrings, nn);
        }


        public override void makeVector(int rows)
        {
            //
            // Setup the object to a TimeSeries integer or double scalar
            //
            base.makeValue();
            o_error = sinter_IOError.si_OKAY;
            //clear error flag
            if (type == sinter_IOType.si_DY_DOUBLE_VEC)
            {
                double[][] da = new double[o_TimeSeriesLength][];
                for (int ii = 0; ii < o_TimeSeriesLength; ++ii)
                {
                    da[ii] = new double[rows];
                }

                o_TimeSeriesValues = da;
                o_value = ((double[])o_TimeSeriesValues)[o_TimeSeriesIndex];
            }
            else if (type == sinter_IOType.si_DY_INTEGER_VEC)
            {
                int[][] da = new int[o_TimeSeriesLength][];
                for (int ii = 0; ii < o_TimeSeriesLength; ++ii)
                {
                    da[ii] = new int[rows];
                }

                o_TimeSeriesValues = da;
                o_value = ((int[])o_TimeSeriesValues)[o_TimeSeriesIndex];
            }
            else if (type == sinter_IOType.si_DY_STRING_VEC)
            {
                String[][] da = new String[o_TimeSeriesLength][];
                for (int ii = 0; ii < o_TimeSeriesLength; ++ii)
                {
                    da[ii] = new string[rows];
                }

                o_TimeSeriesValues = da;
                o_value = ((string[])o_TimeSeriesValues)[o_TimeSeriesIndex];
            }

            else
            {
                // error if trying to make a vector in a object not of vector type
                o_error = sinter_IOError.si_NOT_VECTOR;
            }
        }

#endregion constructors
        #region json
        //Set up the input values for this variable (must be an IN or INOUT variable)
        public override void setInput(string varName, int row, int col, JToken jvalue, string in_units)
        {
            if (isOutput)
            {
                throw new ArgumentException(string.Format("Variable {0} is an output varaible.  It should not be in the input list.", varName));
            }

            units = in_units;

            //Since this is a DynamicVector, we have an array of arrays.  The outter one is a TimeSeries array. 
            //The inner one is the vector values at each timestep. 
            Newtonsoft.Json.Linq.JArray TimeSeriesJSONArray = (Newtonsoft.Json.Linq.JArray)jvalue;
            if (TimeSeriesLength != TimeSeriesJSONArray.Count)
            {
                throw new ArgumentException(string.Format("Dynamic Vector {0} was passed a TimeSeries Array of incorrect size.  TimeSeries length: {1}, input length", varName, TimeSeriesLength, TimeSeriesJSONArray.Count));
            }

            //Step through the TimeSeries, setting each value (then be sure to reset it)
            for (int tt = 0; tt < TimeSeriesLength; ++tt)
            {
                o_TimeSeriesIndex = tt;

                if (row >= 0)   //Editing a single entry of a vector
                {
                    if (row >= size)
                    {
                        throw new ArgumentException(string.Format("DynamicVector {0} got an index out of range.  Upper bound: {1}, bad index: {2}", varName, (int)size - 1, row));
                    }

                    setElement(row, sinter_HelperFunctions.convertJTokenToNative(TimeSeriesJSONArray[tt]));
                }
                else  //If the use passed in a whole vector, we have to set values one by one
                {
                    Newtonsoft.Json.Linq.JArray thisVector = (Newtonsoft.Json.Linq.JArray)TimeSeriesJSONArray[tt];
                    if (size != thisVector.Count)
                    {
                        throw new ArgumentException(string.Format("DynamicVector {0} revieved a column that's the wrong size.  Column {1} should be: {2} is: {3}", varName, tt, size, thisVector.Count));
                    }
                    for (int ii = 0; ii < size; ++ii)
                    {
                        setElement(ii, sinter_HelperFunctions.convertJTokenToNative(thisVector[ii]));
                    }
                }
            }
            resetTimeSeries();
        }

        public override JToken getOutput()
        {
            Newtonsoft.Json.Linq.JArray timeArray = new Newtonsoft.Json.Linq.JArray();
            for (int ii = 0; ii < TimeSeriesLength; ++ii)
            {
                o_TimeSeriesIndex = ii;
                timeArray.Add(new Newtonsoft.Json.Linq.JArray(Value));
            }
            resetTimeSeries();
            return timeArray;
        }


        #endregion json

        #region to-and-from-sim

        //---------------------------------------
        // Comunication with Simulation Program
        //---------------------------------------

        public override void sendToSim(sinter_InteractiveSim o_sim)
        {
            if ((mode != sinter_IOMode.si_OUT) && (!isSetting))
            {
                if (o_type == sinter_IOType.si_DY_DOUBLE_VEC)  //Really, only doubles support type conversion
                {
                    double[] t_value = ((double[][])o_TimeSeriesValues)[o_TimeSeriesIndex];
                    t_value = unitsConversion(units, defaultUnits, (double[])t_value);  //Do units conversion if required

                    //Now that we have the correct converted value, pass it into each address inside the simulation
                    for (int addressIndex = 0; addressIndex <= (addressStrings.Length - 1); addressIndex++)
                    {
                        o_sim.sendVectorToSim<double>(addressStrings[addressIndex], t_value);
                    }
                }
                else if (o_type == sinter_IOType.si_DY_INTEGER_VEC)
                {
                    int[] t_value = ((int[][])o_TimeSeriesValues)[o_TimeSeriesIndex];

                    //Now that we have the correct converted value, pass it into each address inside the simulation
                    for (int addressIndex = 0; addressIndex <= (addressStrings.Length - 1); addressIndex++)
                    {
                        o_sim.sendVectorToSim<int>(addressStrings[addressIndex], t_value);
                    }
                }
                else if (o_type == sinter_IOType.si_DY_STRING_VEC)
                {
                    string[] t_value = ((string[][])o_TimeSeriesValues)[o_TimeSeriesIndex];

                    //Now that we have the correct converted value, pass it into each address inside the simulation
                    for (int addressIndex = 0; addressIndex <= (addressStrings.Length - 1); addressIndex++)
                    {
                        o_sim.sendVectorToSim<string>(addressStrings[addressIndex], t_value);
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Invalid sinter_IOType passed to sinter_DynamicVector.sendToSim", o_type));
                }
            }
        }

        //<Summary>
        //get value from a simulation
        //</Summary>
        public override void recvFromSim(sinter_Sim o_sim)
        {
            //this reads back the inputs
            // It's handy for default generation.
            //            try
            //            {
            if (!isSetting)
            {
                if (type == sinter_IOType.si_DY_DOUBLE_VEC)
                {
                    o_sim.recvVectorFromSim<double>(addressStrings[0], get_vectorIndicies(o_sim), (double[])o_value);
                    ((double[][])o_TimeSeriesValues)[o_TimeSeriesIndex] = (double[])o_value;  //Store it properly in the timevalues
                }
                else if (type == sinter_IOType.si_DY_INTEGER_VEC)
                {
                    o_sim.recvVectorFromSim<int>(addressStrings[0], get_vectorIndicies(o_sim), (int[])o_value);
                    ((int[][])o_TimeSeriesValues)[o_TimeSeriesIndex] = (int[])o_value;  //Store it properly in the timevalues
                }
                else if (type == sinter_IOType.si_DY_STRING_VEC)
                {
                    o_sim.recvVectorFromSim<string>(addressStrings[0], get_vectorIndicies(o_sim), (string[])o_value);
                    ((string[][])o_TimeSeriesValues)[o_TimeSeriesIndex] = (string[])o_value;  //Store it properly in the timevalues
                }
            }
        }

        #endregion to-and-from-sim

        #region type-checks
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


        public override bool isDynamicVariable
        {
            get { return true; }
        }

        #endregion type-checks

        #region data-properties

        //----------------------------------
        // Main properties
        //----------------------------------


        public override object Value
        {
            // value is the data stored in this object
            // value is an object any type of data can be stored
            get
            {
                if (type == sinter_IOType.si_DY_DOUBLE_VEC)
                {
                    return ((double[])o_TimeSeriesValues)[o_TimeSeriesIndex];
                }
                else if (type == sinter_IOType.si_DY_INTEGER_VEC)
                {
                    return ((int[])o_TimeSeriesValues)[o_TimeSeriesIndex];
                }
                else if (type == sinter_IOType.si_DY_STRING_VEC)
                {
                    return ((string[])o_TimeSeriesValues)[o_TimeSeriesIndex];
                }
                else
                {
                    throw new ArgumentException(string.Format("Invalid sinter_IOType passed to sinter_DynamicVector.Value", o_type));
                }
            }
            set
            {
                if (type == sinter_IOType.si_DY_DOUBLE_VEC)
                {
                    ((double[][])o_TimeSeriesValues)[o_TimeSeriesIndex] = (double[]) o_value;
                }
                else if (type == sinter_IOType.si_DY_INTEGER_VEC)
                {
                    ((int[][])o_TimeSeriesValues)[o_TimeSeriesIndex] = (int[])o_value;
                }
                else if (type == sinter_IOType.si_DY_STRING_VEC)
                {
                    ((string[][])o_TimeSeriesValues)[o_TimeSeriesIndex] = (string[])o_value;
                }
                else
                {
                    throw new ArgumentException(string.Format("Invalid sinter_IOType passed to sinter_DynamicVector.Value", o_type));
                }
           }
        }

        //------------------------------------
        // vector/matrix things
        //------------------------------------

        public override object getElement(int i)
        {
            if (type == sinter_IOType.si_DY_DOUBLE_VEC)
            {
                return ((double[][])o_TimeSeriesValues)[o_TimeSeriesIndex][i];
            }
            else if (type == sinter_IOType.si_DY_INTEGER_VEC)
            {
                return ((int[][])o_TimeSeriesValues)[o_TimeSeriesIndex][i];
            }
            else if (type == sinter_IOType.si_DY_STRING_VEC)
            {
                return ((string[][])o_TimeSeriesValues)[o_TimeSeriesIndex][i];
            }
            else
            {
                throw new ArgumentException(string.Format("Invalid sinter_IOType passed to sinter_DynamicVector.getElement", o_type));
            }
        }


        public override void setElement(int i, object value)
        {

            if (type == sinter_IOType.si_DY_DOUBLE_VEC)
            {
                ((double[])o_value)[i] = Convert.ToDouble(value);
                ((double[][])o_TimeSeriesValues)[o_TimeSeriesIndex][i] = ((double[])o_value)[i];
            }
            else if (type == sinter_IOType.si_DY_INTEGER_VEC)
            {
                ((int[])o_value)[i] = Convert.ToInt32(value);
                ((int[][])o_TimeSeriesValues)[o_TimeSeriesIndex][i] = ((int[])o_value)[i];
            }
            else if (type == sinter_IOType.si_DY_STRING_VEC)
            {
                ((String[])o_value)[i] = (String)value;
                ((String[][])o_TimeSeriesValues)[o_TimeSeriesIndex][i] = ((String[])o_value)[i];

            }
            else
            {
                throw new ArgumentException(string.Format("Invalid sinter_IOType passed to sinter_DynamicVector.setElement", o_type));
            }
        }


        #endregion data-properties

        #region default-handling
        public override void setDefaultToValue()
        {
            dfault = Value;
            o_defaultUnits = o_units;
        }


        public override void resetToDefault()
        {
            Value = dfault;
            o_units = o_defaultUnits;

            int saved_index = o_TimeSeriesIndex;
            for (o_TimeSeriesIndex = 0; o_TimeSeriesIndex < o_TimeSeriesLength; ++o_TimeSeriesIndex)
            {
                Value = o_value;  //Saves me the trouble of rewriting all those casts.  At the cost of duplicating work
            }
            o_TimeSeriesIndex = saved_index;
        }

        #endregion default-handling

        #region dynamic-indexing

        //When the input file is read, the TimeSeries may be a different size than the one in the configuration file.
        //We then need to tell the dynamic variables so they can still error check the input file.
        public void changeTimeSeriesLength(int TS_size)
        {
            o_TimeSeriesLength = TS_size;
            o_TimeSeriesValues = new double[TS_size];
            makeVector(size);  //Make Value resets to default internally
            resetTimeSeries();
        }


        public void resetTimeSeries()
        {
            TimeSeriesIndex = 0;
        }

        //Advances the timestep by one and returns the new timestep index if it can be advanced
        //If it's reached the end of the TimeSeries, it doesn't advance and returns -1
        public int advance_timestep()
        {
            if (o_TimeSeriesIndex < o_TimeSeriesIndex - 1)
            {
                return ++o_TimeSeriesIndex;
            }
            else
            {
                return -1;
            }
        }

        public int TimeSeriesIndex
        {
            get { return o_TimeSeriesIndex; }
            set {
                if(value >= 0 && value < o_TimeSeriesLength) {
                    o_TimeSeriesIndex = value;
                } else {
                    throw new IndexOutOfRangeException("TimeSeries set to value out of range.");
                }

            }
        }

        public int TimeSeriesLength
        {
           get { return o_TimeSeriesLength; }
        }

        #endregion dynamic-indexing


    }
    
}
