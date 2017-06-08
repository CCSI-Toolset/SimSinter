using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using sinter;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace sinter
{

    public abstract class sinter_InteractiveSim : sinter_Sim
    {

        // run name is optionally used for identifying output files

        public sinter_InteractiveSim()
            : base()
        {
        }


        public override void sendInputsToSim()
        {
            //Guarantees settings will be done before any real interaction with the simulation.  
            //We may need to seperate this off and move it earlier in the future.
            foreach (sinter_Variable inputObj in o_setupFile.Settings)
            {  
                inputObj.sendSetting(this);
            }

            foreach (sinter_Variable inputObj in o_setupFile.Variables)
            {  //Don't really need all IO Objects here, Outputs will be ignored
               inputObj.sendToSim(this);
            }
        }

        //CHANGE THIS HERE
        //basically, don't call to the variables to get the outputs, just get the address string from the output and use it in the sim to get the output value
        //Q. Will this work for dynamic?  Not really, the dynamic variables have special internal arrays that must be dealt with, but if we set value in them it should still work?  I think so.
  /*      public override void recvOutputsFromSim()
        {
            foreach (sinter_Variable outputObj in o_setupFile.Variables)
            {
                if (outputObj.isOutput)
                {
                    if (outputObj.isVec)
                    {
                        sinter_Vector outVec = (sinter_Vector) outputObj;
                        if(outVec.type == sinter_Variable.sinter_IOType.
                        outVec.Value = recvVectorFromSimAsObject(outVec.addressStrings[0], outVec.get_vectorIndicies(this));// recvFromSim(this);
                    }
                    else
                    {
                        outputObj.Value = recvValueFromSimAsObject(outputObj.addressStrings[0]);// recvFromSim(this);
                    }
                }
            }
        }
        */
//Below These need abstrat versions in Sim, or to be moved there explicitly
        public override sinter_Variable.sinter_IOType guessTypeFromSim(string path)
        {
            Object value = recvValueFromSimAsObject(path);
            if (value is double)
            {
                return sinter_Variable.sinter_IOType.si_DOUBLE;
            }
            else if (value is int)
            {
                return sinter_Variable.sinter_IOType.si_INTEGER;
            }
            else if (value is String)
            {
                return sinter_Variable.sinter_IOType.si_STRING;
            }
            return sinter_Variable.sinter_IOType.si_UNKNOWN;
        }

        public override sinter_Variable.sinter_IOType guessVectorTypeFromSim(string path, int[] indicies)
        {
            Object value = recvValueFromSimAsObject(path, indicies[0]);
            if (value is double)
            {
                return sinter_Variable.sinter_IOType.si_DOUBLE_VEC;
            }
            else if (value is int)
            {
                return sinter_Variable.sinter_IOType.si_INTEGER_VEC;
            }
            else if (value is String)
            {
                return sinter_Variable.sinter_IOType.si_STRING_VEC;
            }
            return sinter_Variable.sinter_IOType.si_UNKNOWN;
        }

        //This function has all variables request their units from their simulations.
        //This it useful for generating configuration files.
        public override void initializeUnits()
        {
            foreach (sinter_Variable inputObj in o_setupFile.Variables)
            {  //Don't really need all IO Objects here, settings and Outputs will be ignored
                inputObj.initializeUnits(this);
            }
        }


        /**  
         * recvValueFromSimAsObject was added to give us a chance to figure out the type of a variable automatically in the GUI
         * They should not be called on variables that may not exist, they will throw an Exception
         * So far only used internally in interactive simulators, so it's here.
         */
        public abstract Object recvValueFromSimAsObject(string path);
        public abstract Object recvValueFromSimAsObject(string path, int ii);  //For vectors type identification


    }
}
