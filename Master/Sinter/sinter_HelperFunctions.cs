using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sinter
{
    public class sinter_HelperFunctions
    {

        //Handy for comparing doubles
        public static bool fuzzyEquals(double a, double b, double epsilon)
        {
            if ((a == 0) && (b == 0))
                return true;
            else if (double.IsNaN(a) || double.IsNaN(b))
                return false;
            else if (double.IsPositiveInfinity(a))
                return double.IsPositiveInfinity(b);
            else if (double.IsNegativeInfinity(a))
                return double.IsNegativeInfinity(b);
            else
                return Math.Abs(a - b) <= Math.Abs(a * epsilon);
        }


        //JTokens have explicit converters to types like int, double, string, etc.  This simplifies things by
        //forcing the explicit conversion, so the value will be saved correctly inside sinter.
        public static object convertJTokenToNative(JToken thisToken)
        {
            if (thisToken.Type == JTokenType.Integer)
            {
                int ii = (int)thisToken;
                return ii;
            }
            else if (thisToken.Type == JTokenType.Float)
            {
                double dd = (double)thisToken;
                return dd;
            }
            else if (thisToken.Type == JTokenType.String)
            {
                String ss = (String)thisToken;
                return ss;
            }
            else
            {
                throw new System.IO.IOException(String.Format("Jtoken has unknown type {0}", thisToken.Type));
            }
        }

        public static JToken convertSinterValueToJToken(sinter_Variable sVar)
        {
            JToken jtok = null;
            if (sVar.isVec)
            {
                Newtonsoft.Json.Linq.JArray jsonArray = new Newtonsoft.Json.Linq.JArray(sVar.Value);
                jtok = (JToken)jsonArray;

            }
            else if (sVar.isScalar)
            {
                if (sVar.type == sinter_Variable.sinter_IOType.si_DOUBLE)
                {
                    double val = Convert.ToDouble(sVar.Value);
                    jtok = (JToken)val;
                }
                else if (sVar.type == sinter_Variable.sinter_IOType.si_INTEGER)
                {
                    int val = Convert.ToInt32(sVar.Value);
                    jtok = (JToken)val;
                }
                else if (sVar.type == sinter_Variable.sinter_IOType.si_STRING)
                {
                    String val = (String)sVar.Value;
                    jtok = (JToken)val;
                }
                else
                {
                    throw new System.IO.IOException(String.Format("Sinter Variable {0} has unknown type {1}", sVar.name, sVar.type));
                }

            }
            else
            {
                throw new System.IO.IOException(String.Format("Sinter Variable {0} has unknown type {1}", sVar.name, sVar.type));
            }
            return jtok;
        }

        //I don't know what else to do with the erros, so print 'em
        public static void printErrors(ISimulation stest)
        {
            try
            {
                if (stest.runStatus != 0)
                {

                    string[] errorList = stest.errorsBasic();
                    foreach (string error in errorList)
                    {
                        Console.WriteLine(error);
                    }

                    Console.WriteLine();

                    Console.WriteLine("Warnings:");

                    string[] warnList = stest.warningsBasic();
                    foreach (string error in warnList)
                    {
                        Console.WriteLine(error);
                    }

                    Console.WriteLine();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught: " + ex.Message);
            }

        }


        public static sinter_Variable makeNewVariable(sinter_Sim sim, string path)
        {
            sinter_Variable retVar = null;
            sinter.sinter_Variable.sinter_IOType vartype = sim.guessTypeFromSim(path);
            if (vartype == sinter.sinter_Variable.sinter_IOType.si_DOUBLE ||
                vartype == sinter.sinter_Variable.sinter_IOType.si_INTEGER ||
                vartype == sinter.sinter_Variable.sinter_IOType.si_STRING)
            {
                sinter_Variable previewVar = new sinter_Variable();
                string[] addressString = new string[1] { path };
                previewVar.init(sim, vartype, addressString);
                previewVar.initializeUnits(sim);
                previewVar.initializeDescription(sim);
                retVar = previewVar;
            }
            else if (vartype == sinter.sinter_Variable.sinter_IOType.si_DOUBLE_VEC ||
              vartype == sinter.sinter_Variable.sinter_IOType.si_INTEGER_VEC ||
              vartype == sinter.sinter_Variable.sinter_IOType.si_STRING_VEC)
            {
                sinter_Vector previewVar = new sinter_Vector();
                string[] addressString = new string[1] { path };
                previewVar.init(sim, vartype, addressString);
                previewVar.initializeUnits(sim);
                previewVar.initializeDescription(sim);
                retVar = previewVar;
            }
            return retVar;
        }


    }
}
