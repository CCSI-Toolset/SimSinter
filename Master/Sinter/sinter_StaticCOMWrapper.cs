using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace sinter
{

    // This class is just a quick, dumb, way to give COM access to static functions in the sinter library.
    // COM does not allow statics
    public class sinter_StaticCOMWrapper
    {

        public sinter_StaticCOMWrapper() { }

        public ISimulation createSinterBySetupString(string setupString)
        {
            return sinter_Factory.createSinter(setupString);
        }


        public ISimulation createSinter(string sinterConfigFilename)
        {
            //this function returns 0 if no error
            //another integer for errors
            StreamReader inFileStream = new StreamReader(sinterConfigFilename);
            string setupString = "";
            setupString = inFileStream.ReadToEnd();
            inFileStream.Close();

            return createSinterBySetupString(setupString);
        }
    }
}
