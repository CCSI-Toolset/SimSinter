using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sinter
{
    public interface sinter_IDynamicVariable
    {
        //When the input file is read, the TimeSeries may be a different size than the one in the configuration file.
        //We then need to tell the dynamic variables so they can still error check the input file.
        void changeTimeSeriesLength(int size);

        //Resets the internal time series index to 0
        void resetTimeSeries();

        //Steps the TimeSeries ahead by one
        int advance_timestep();

        //sets or gets the current time series index
        int TimeSeriesIndex {
         get;
         set;
        }

        //Gets the current time series length
        int TimeSeriesLength {
         get;   
        }
    }
}
