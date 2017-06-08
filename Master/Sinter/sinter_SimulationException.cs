using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sinter
{
    /// <summary>
    /// This exception type is thrown for everything from problems in the simulation configuration
    /// to non-existent files.  See the message for more details.
    /// </summary>
    public class sinter_SimulationException : Exception
    {
        public sinter_SimulationException(string msg)
            : base(msg)
        {
        }
    }
}
