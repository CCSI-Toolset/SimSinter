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
    // This class holds scalar variables used for dynamic simulations.
    //
    public class sinter_DynamicScalar : sinter_Variable, sinter_IDynamicVariable 
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

        public sinter_DynamicScalar()
            : base()
        {
            o_TimeSeriesIndex = 0;
            o_TimeSeriesLength = 0;
        }

        //Convert from a steady state variable to a dynamic one
        public sinter_DynamicScalar(sinter_Variable rhs)
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

            switch (rhs.type)
            {
                case sinter_Variable.sinter_IOType.si_DOUBLE:
                    type = sinter_Variable.sinter_IOType.si_DY_DOUBLE;
                    break;
                case sinter_Variable.sinter_IOType.si_INTEGER:
                    type = sinter_Variable.sinter_IOType.si_DY_INTEGER;
                    break;
                case sinter_Variable.sinter_IOType.si_STRING:
                    type = sinter_Variable.sinter_IOType.si_DY_STRING;
                    break;
            }

            units = rhs.units;
            defaultUnits = rhs.defaultUnits;

            Value = rhs.Value;
            maximum = rhs.maximum;
            minimum = rhs.minimum;
            dfault = rhs.dfault;
            
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


        public virtual void init(string thisName, sinter_IOMode iomode, sinter_IOType thisType, string desc, int TimeSeriesLen, string[] addStrings)
        {
            o_TimeSeriesLength = TimeSeriesLen;
            base.init(thisName, iomode, thisType, desc, addStrings);
        }

        public override void makeValue()
        {
            //
            // Setup the object to a TimeSeries integer or double scalar
            //
            base.makeValue();
            o_error = sinter_IOError.si_OKAY;
            //clear error flag
            if (type == sinter_IOType.si_DY_DOUBLE)
            {
                double[] da = new double[o_TimeSeriesLength];

                o_TimeSeriesValues = da;
                o_value = ((double[])o_TimeSeriesValues)[o_TimeSeriesIndex];
            }
            else if (type == sinter_IOType.si_DY_INTEGER)
            {
                int[] da = new int[o_TimeSeriesLength];

                o_TimeSeriesValues = da;
                o_value = ((int[])o_TimeSeriesValues)[o_TimeSeriesIndex];
            }
            else if (type == sinter_IOType.si_DY_STRING)
            {
                String[] da = new String[o_TimeSeriesLength];

                o_TimeSeriesValues = da;
                o_value = ((string[])o_TimeSeriesValues)[o_TimeSeriesIndex];
            }

            else
            {
                // error if trying to make a vector in a object not of vector type
                o_error = sinter_IOError.si_NOT_VECTOR;
            }
            resetToDefault();
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

            Newtonsoft.Json.Linq.JArray TimeSeriesJSONArray = (Newtonsoft.Json.Linq.JArray)jvalue;
            if (TimeSeriesLength != TimeSeriesJSONArray.Count)
            {
                throw new ArgumentException(string.Format("Dynamic Scalar {0} passed a JArray of incorrect size.  TimeSeries length: {1}, JArray length", varName, TimeSeriesLength, TimeSeriesJSONArray.Count));
            }

            //Step through the TimeSeries, setting each value (then be sure to reset it)
            for (int ii = 0; ii < TimeSeriesLength; ++ii)
            {
                o_TimeSeriesIndex = ii;
                Value = sinter_HelperFunctions.convertJTokenToNative(TimeSeriesJSONArray[ii]);
            }
            resetTimeSeries();
        }

        public override JToken getOutput()
        {
            Newtonsoft.Json.Linq.JArray jsonArray = new Newtonsoft.Json.Linq.JArray(o_TimeSeriesValues);
            return jsonArray;
        }

        #endregion json

        #region tostring

        //For dyanmic types all the type stuff looks the same, so just reuse the sinter_Variable version of this function to do the
        //work and make them dynamic types on the way out.
        public new static void string2Type(String thisTypeString, ref sinter_IOType thisType, ref int[] sizes)
        {
            sinter_Variable.string2Type(thisTypeString, ref thisType, ref sizes);

            switch(thisType) {
                case sinter_IOType.si_DOUBLE:
                thisType = sinter_IOType.si_DY_DOUBLE;
                break;

                case sinter_IOType.si_INTEGER:
                thisType = sinter_IOType.si_DY_INTEGER;
                break;

                case sinter_IOType.si_STRING:
                thisType = sinter_IOType.si_DY_STRING;
                break;

                case sinter_IOType.si_DOUBLE_VEC:
                thisType = sinter_IOType.si_DY_DOUBLE_VEC;
                break;

                case sinter_IOType.si_INTEGER_VEC:
                thisType = sinter_IOType.si_DY_INTEGER_VEC;
                break;

                case sinter_IOType.si_STRING_VEC:
                thisType = sinter_IOType.si_DY_STRING_VEC;
                break;

                default:
                throw new ArgumentException(string.Format("Unknown type {0} passed to sinter_DynmaicScalar.string2type", thisType));
            }
        }
        #endregion tostring

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
                if (o_type == sinter_IOType.si_DY_DOUBLE)  //Really, only doubles support type conversion
                {
                    double t_value = ((double[])o_TimeSeriesValues)[o_TimeSeriesIndex];
                    t_value = unitsConversion(units, defaultUnits, t_value);

                    //Now that we have the correct converted value, pass it into each address inside the simulation
                    for (int addressIndex = 0; addressIndex <= (addressStrings.Length - 1); addressIndex++)
                    {
                        o_sim.sendValueToSim(addressStrings[addressIndex], t_value);
                    }
                }
                else if (o_type == sinter_IOType.si_DY_INTEGER) 
                {
                    int t_value = ((int[])o_TimeSeriesValues)[o_TimeSeriesIndex];

                    //Now that we have the correct converted value, pass it into each address inside the simulation
                    for (int addressIndex = 0; addressIndex <= (addressStrings.Length - 1); addressIndex++)
                    {
                        o_sim.sendValueToSim(addressStrings[addressIndex], t_value);
                    }
                }
                else if (o_type == sinter_IOType.si_DY_STRING)
                {
                    string t_value = ((string[])o_TimeSeriesValues)[o_TimeSeriesIndex];

                    //Now that we have the correct converted value, pass it into each address inside the simulation
                    for (int addressIndex = 0; addressIndex <= (addressStrings.Length - 1); addressIndex++)
                    {
                        o_sim.sendValueToSim(addressStrings[addressIndex], t_value);
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Invalid sinter_IOType passed to sinter_DynamicScalar.sendToSim", o_type));
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
                if (type == sinter_IOType.si_DY_DOUBLE)
                {
                    o_value = o_sim.recvValueFromSim<double>(addressStrings[0]);
                    ((double[])o_TimeSeriesValues)[o_TimeSeriesIndex] = (double)o_value;  //Store it properly in the timevalues
                }
                else if (type == sinter_IOType.si_DY_INTEGER)
                {
                    o_value = o_sim.recvValueFromSim<int>(addressStrings[0]);
                    ((int[])o_TimeSeriesValues)[o_TimeSeriesIndex] = (int)o_value;  //Store it properly in the timevalues
                }
                else if (type == sinter_IOType.si_DY_STRING)
                {
                    o_value = o_sim.recvValueFromSim<String>(addressStrings[0]);
                    ((string[])o_TimeSeriesValues)[o_TimeSeriesIndex] = (string)o_value;  //Store it properly in the timevalues
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
                return true;
            }
        }
        public override bool isVec
        {
            // returns true if the io object is a vector
            get
            {
                return false;
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
                if (type == sinter_IOType.si_DY_DOUBLE)
                {
                    return ((double[])o_TimeSeriesValues)[o_TimeSeriesIndex];
                }
                else if (type == sinter_IOType.si_DY_INTEGER)
                {
                    return ((int[])o_TimeSeriesValues)[o_TimeSeriesIndex];
                }
                else if (type == sinter_IOType.si_DY_STRING)
                {
                    return ((string[])o_TimeSeriesValues)[o_TimeSeriesIndex];
                }
                else
                {
                    return o_value;
                }
            }
            set
            {
                if (type == sinter_IOType.si_DY_DOUBLE)
                {
                    o_value = Convert.ToDouble(value);
                    ((double[])o_TimeSeriesValues)[o_TimeSeriesIndex] = (double) o_value;
                }
                else if (type == sinter_IOType.si_DY_INTEGER)
                {
                    o_value = Convert.ToInt32(value);
                    ((int[])o_TimeSeriesValues)[o_TimeSeriesIndex] = (int)o_value;
                }
                else if (type == sinter_IOType.si_DY_STRING)
                {
                    o_value = Convert.ToString(value);
                    ((string[])o_TimeSeriesValues)[o_TimeSeriesIndex] = (string)o_value;
                }
                else
                {
                    o_value = value;
                }
           }
        }

        #endregion data-properties

        #region default-handling
        public override void setDefaultToValue()
        {
            dfault = o_value;
            o_defaultUnits = o_units;
        }


        public override void resetToDefault()
        {
            object tmp_value = dfault;
            o_units = o_defaultUnits;

            int saved_index = o_TimeSeriesIndex;
            for (o_TimeSeriesIndex = 0; o_TimeSeriesIndex < o_TimeSeriesLength; ++o_TimeSeriesIndex)
            {
                Value = tmp_value;  //Saves me the trouble of rewriting all those casts.  At the cost of duplicating work
            }
            o_TimeSeriesIndex = saved_index;
        }

        #endregion default-handling

        #region dynamic-indexing

        //When the input file is read, the TimeSeries may be a different size than the one in the configuration file.
        //We then need to tell the dynamic variables so they can still error check the input file.
        public void changeTimeSeriesLength(int size)
        {
            o_TimeSeriesLength = size;
            o_TimeSeriesValues = new double[size];
            makeValue();  //Make Value resets to default internally
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
            if (o_TimeSeriesIndex < o_TimeSeriesLength)
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
