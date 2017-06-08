using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sinter
{
    public interface sinter_IVariable
    {
        bool isTable
        {
            get;
        }

        sinter_Variable.sinter_IOType type
        {
            get;
        }


        sinter_Variable.sinter_IOMode mode
        {
            get;
            set;
        }

        string name
        {
            get;
            set;
        }


        string description
        {
            get;
            set;
        }


        void setDefaultToValue();

        void resetToDefault();

        // Set the input value of the variable from a Json Object.  Now every variable type should know what it's expecting from JSON.
        void setInput(string varName, int row, int col, JToken jvalue, string units);

        // Get the value of the object as a JSON Token/Object/Array whatever.
        JToken getOutput();

        String typeString
        {
            get;
        }


        bool isScalar
        {
            get;
        }

        bool isVec
        {
            get;
        }

        bool isSetting
        {
            get;
        }


        bool isInput
        {
            get;
        }

        bool isOutput
        {
            get;
        }

        
        void sendToSim(sinter_InteractiveSim o_sim);
        void recvFromSim(sinter_Sim o_sim);
        void initializeUnits(sinter_Sim o_sim);

    }
}
