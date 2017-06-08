using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sinter
{
    public class sinter_Factory
    {
        /** 
         * createSinter parses part of the setup file passed in (as a string) to discover the 
         * extension of the simulation file provided.  
         * From that extension it creates the correct class for running that type of simulation.
        **/
        public static ISimulation createSinter(string setupFileString)
        {
           
            sinter_SetupFile thisSetupFile = sinter_SetupFile.determineFileTypeAndParse(setupFileString);
            
            string extension = System.IO.Path.GetExtension(thisSetupFile.aspenFilename);
            sinter_Sim thisAspen = null;

            if (extension == ".bkp" || extension == ".apw")
            {
                thisAspen = new sinter_SimAspen();
                thisAspen.setupFile = thisSetupFile;
            }
            else if (extension == ".acmf")
            {
                thisAspen = new sinter_SimACM();
                thisAspen.setupFile = thisSetupFile;
            }
            else if (extension == ".xlsm" || extension == ".xls" || extension == ".xlsx")
            {
                thisAspen = new sinter_SimExcel();
                thisAspen.setupFile = thisSetupFile;
            }
            else if (extension != null && extension.ToLower() == ".gencrypt")
            {
                thisAspen = new sinter.PSE.sinter_simGPROMS();
                thisAspen.setupFile = thisSetupFile;
            }
            else if (extension != null && extension.ToLower() == ".gPJ")
            {
                throw new System.IO.IOException(String.Format(
                    "gPJ is not an allowed extension for sinter simulation run.  SimSinter requires a .gENCRYPT file.  filename: {1}.", setupFileString));
            }

            else
            {
                throw new System.IO.IOException(String.Format(
                    "Unknown Aspen File extension {0} on filename: {1}.  Expecting either .bkp or .apw for Aspen+, .acmf for ACM, .xlsm .xls .xlsx for Excel, or .gencrypt for GPROMS", extension, setupFileString));
            }
            if (thisAspen == null)
            {
                throw new System.IO.IOException("Failed to create sinter object, reason unknown.");
            }
            if (thisAspen is sinter_InteractiveSim)
            {
                sinter_InteractiveSim sSim = (sinter_InteractiveSim)thisAspen;
                sSim.makeIOTree();
            }
            return thisAspen;
        }

        //This is the factory we use in any case where we're doing configuration rather than running a simulation
        public static ISimulation createSinterForConfig(string setupFileString)
        {

            sinter_SetupFile thisSetupFile = sinter_SetupFile.determineFileTypeAndParse(setupFileString);

            string extension = System.IO.Path.GetExtension(thisSetupFile.simDescFile);
            sinter_Sim thisAspen = null;

            if (extension == ".bkp" || extension == ".apw")
            {
                thisAspen = new sinter_SimAspen();
                thisAspen.setupFile = thisSetupFile;
            }
            else if (extension == ".acmf")
            {
                thisAspen = new sinter_SimACM();
                thisAspen.setupFile = thisSetupFile;
            }
            else if (extension == ".xlsm" || extension == ".xls" || extension == ".xlsx")
            {
                thisAspen = new sinter_SimExcel();
                thisAspen.setupFile = thisSetupFile;
            }
            else if (extension != null && extension.ToLower() == ".gpj")
            {
                thisAspen = new sinter.PSE.sinter_simGPROMSconfig();
                thisAspen.setupFile = thisSetupFile;
            }
            else if (extension != null && extension.ToLower() == ".gencrypt")
            {
                throw new System.IO.IOException(String.Format(
                    "gENCRYPT is not an allowed extension for sinter configuration.  SinterConfigGUI requires a .gPJ file.  filename: {1}.", setupFileString));
            }
            else
            {
                throw new System.IO.IOException(String.Format(
                    "Unknown Aspen File extension {0} on filename: {1}.  Expecting either .bkp or .apw for Aspen+, .acmf for ACM, .xlsm .xls .xlsx for Excel, or .gencrypt for GPROMS", extension, setupFileString));
            }
            if (thisAspen == null)
            {
                throw new System.IO.IOException("Failed to create sinter object, reason unknown.");
            }
            if (thisAspen is sinter_InteractiveSim)
            {
                sinter_InteractiveSim sSim = (sinter_InteractiveSim)thisAspen;
                sSim.makeIOTree();
            }
            return thisAspen;
        }


        /** 
         * createVariable creates the correct Variable type from the sinter_IOType.
        **/
        public static sinter_Variable createVariable(sinter_Variable.sinter_IOType type)
        {
            switch (type)
            {
                case sinter_Variable.sinter_IOType.si_DOUBLE:
                case sinter_Variable.sinter_IOType.si_INTEGER:
                case sinter_Variable.sinter_IOType.si_STRING:
                    return new sinter_Variable();

                case sinter_Variable.sinter_IOType.si_DOUBLE_VEC:
                case sinter_Variable.sinter_IOType.si_INTEGER_VEC:
                case sinter_Variable.sinter_IOType.si_STRING_VEC:
                    return new sinter_Vector();

                case sinter_Variable.sinter_IOType.si_DY_DOUBLE:
                case sinter_Variable.sinter_IOType.si_DY_INTEGER:
                case sinter_Variable.sinter_IOType.si_DY_STRING:
                    return new sinter_DynamicScalar();

                case sinter_Variable.sinter_IOType.si_DY_DOUBLE_VEC:
                case sinter_Variable.sinter_IOType.si_DY_INTEGER_VEC:
                case sinter_Variable.sinter_IOType.si_DY_STRING_VEC:
                    return new sinter_DynamicVector();

                default:
                    throw new ArgumentException(String.Format("Unknown type {0} passed to sinter_Factory.createVariable", (int)type));
            }
        }

    }
}
