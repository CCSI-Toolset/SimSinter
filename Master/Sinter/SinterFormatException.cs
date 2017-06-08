using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sinter
{
    public class SinterFormatException : JsonException
    {
        public SinterFormatException(String message) : base(message)
        {
        }
    }
}
