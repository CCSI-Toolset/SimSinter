using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sinter
{
    /**
     * This exception is thrown when the simulator does not match the constraints required by the SimSinter Configuration File.
     * In the SinterConfigGUI it may be recovered from, but it should cause a hard failure when doing runs.
     **/
    public class SinterConstraintException : JsonException
    {
        public SinterConstraintException(String message) : base(message)
        {
        }
    }
}
