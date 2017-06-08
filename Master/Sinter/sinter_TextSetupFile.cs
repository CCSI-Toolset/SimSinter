using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace sinter
{
    public class sinter_TextSetupFile : sinter_SetupFile
    {

        private StringReader inFileStream;

        public sinter_TextSetupFile()
            : base()
        {
            // the constructor method
        }

        public override int parseFile(string setupString)
        {
            //this function returns 0 if no error
            //another integer for errors
            string line = null;
            sinter.sinter_AppError e = default(sinter.sinter_AppError);
            inFileStream = new StringReader(setupString);
      
            try
            {
                while (inFileStream.Peek() > 0) //ReadLine()) // (!(FileSystem.EOF(fileNo))))
                {
                    line = inFileStream.ReadLine(); // inFileStream.ReadLine();
                    e = processLine(line);
                    if ((e != sinter.sinter_AppError.si_OKAY))
                    {
                        //throw new System.IO.IOException("error reading file");
                        inFileStream.Close(); //FileSystem.FileClose(fileNo);
                        throw new System.IO.IOException("Error while parsing the Sinter Config File.");
                    }
                }
            }
            finally
            {
                inFileStream.Close(); //  FileSystem.FileClose(fileNo);
            }

            //After the file is all parsed, do some post processing on the tables
            foreach (sinter_Table thisTable in Tables)
            {
                thisTable.fixupTable(this);// addTableVariables(thisTable);
            }

            return 0;
        }

        private sinter.sinter_AppError processLine(string l)
        {
            string[] fields = null;
            sinter_Variable.sinter_IOType type = default(sinter_Variable.sinter_IOType);
            sinter_Variable.sinter_IOMode mode = default(sinter_Variable.sinter_IOMode);
            //
            // Split line using a bar
            //
            if(l.Length < 1)
                return sinter.sinter_AppError.si_OKAY;

            fields = l.Split('|');
            if (fields.Length == 0)
                return sinter.sinter_AppError.si_OKAY;
            for (int i = 0; i <= fields.Length - 1; i++)
            {
                fields[i] = fields[i].Trim();//  Strings.Trim(fields[i]);
                char tab = '\u0009';
                fields[i] = fields[i].Replace(tab.ToString(), "");
            }
            //
            // Check for special key words
            //
            if(fields[0].Length == 0) {
                return sinter.sinter_AppError.si_OKAY;
                //Blank or invalid line, ignore it
            }
            if (fields[0][0] == '#')  //If first char in line is a #
            {
                return sinter.sinter_AppError.si_OKAY;
                //it is a comment ignore line
            }
            else if (fields[0] == "file" & fields.Length == 2)
            {
                o_aspenFilename = fields[1];
                //e = sim.openSim();
                //open sim so can inspect IO
                //return e;
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "dir" & fields.Length == 2)
            {
//                o_workingDir = fields[1];
                return sinter.sinter_AppError.si_UNKNOWN_FIELD;
            }
            else if (fields[0] == "title" & fields.Length == 2)
            {
                o_title = fields[1];
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "author" & fields.Length == 2)
            {
                o_author = fields[1];
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "date" & fields.Length == 2)
            {
                o_dateString = fields[1];
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "description" & fields.Length == 2)
            {
                o_simDesc = fields[1];
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "min")
            {
                processMin(fields);
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "max")
            {
                processMax(fields);
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "default")
            {
                processDefault(fields);
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "limits")
            {
                if (getIOByName(fields[1]) == null)
                {
                    throw new System.IO.IOException("Cannot apply labels, Object " + fields[1] + " not found.  (Settings not allowed)");
                    //return sinter.sinter_AppError.si_OKAY;
                }
                else if (!(getIOByName(fields[1]).isScalar))
                {
                    throw new System.IO.IOException("Cannot use limits on non scalar");
                    //return sinter.sinter_AppError.si_OKAY;
                }
                else if (fields.Length != 4)
                {
                    throw new System.IO.IOException("Limits on " + fields[1] + " too few arguments");
                    //return sinter.sinter_AppError.si_OKAY;
                }
                if (getIOByName(fields[1]).type == sinter_Variable.sinter_IOType.si_DOUBLE)
                {
                    sinter_Variable thisVar = (sinter_Variable)getIOByName(fields[1]);
                    thisVar.minimum = Convert.ToDouble(fields[2]);
                    thisVar.maximum = Convert.ToDouble(fields[3]);
                }
                else if (getIOByName(fields[1]).type == sinter_Variable.sinter_IOType.si_INTEGER)
                {
                    sinter_Variable thisVar = (sinter_Variable)getIOByName(fields[1]);
                    thisVar.minimum = Convert.ToInt32(fields[2]);
                    thisVar.maximum = Convert.ToInt32(fields[3]);
                }
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "cLabels" | fields[0] == "clabels")
            {
                if (getIOByName(fields[1]) == null)
                {
                    throw new System.IO.IOException("Cannot apply labels, Object " + fields[1] + " not found");
                }
                processLabels("c", fields, getIOByName(fields[1]));
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "rLabels" | fields[0] == "rlabels")
            {
                if (getIOByName(fields[1]) == null)
                {
                    throw new System.IO.IOException("Cannot apply labels, Object " + fields[1] + " not found");
                }
                processLabels("r", fields, getIOByName(fields[1]));
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "cStrings" | fields[0] == "cstrings")
            {
                if (getIOByName(fields[1]) == null)
                {
                    throw new System.IO.IOException("Cannot apply strings, Object " + fields[1] + " not found");
                }
                processLabels("cs", fields, getIOByName(fields[1]));
                sinter_IVariable io = getIOByName(fields[1]);
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "rStrings" | fields[0] == "rstrings")
            {
                if (getIOByName(fields[1]) == null)
                {
                    throw new System.IO.IOException("Cannot apply strings, Object " + fields[1] + " not found");
                }
                processLabels("rs", fields, getIOByName(fields[1]));
                return sinter.sinter_AppError.si_OKAY;
            }
            else if (fields[0] == "rStrings_ForEach" | fields[0] == "rstrings_ForEach")
            {
                throw new System.IO.IOException("No Foreach allowed with this version of sinter.");
            }
            //
            // Check lines that add input and output
            //
            if (fields.Length >= 5)
            {
                //assume if a line isn't a keyword line and it has 5 or more fields
                // it is adding an input or output
                //
                // 0-name | 1-iomode | 2-iotype | 3-description | 4-addess-string ... (There may be multiple address strings)

                if ((fields[1] == "input"))
                {
                    mode = sinter_Variable.sinter_IOMode.si_IN;
                }
                else
                {
                    mode = sinter_Variable.sinter_IOMode.si_OUT;
                }
                int[] bounds = null;
                sinter_Variable.string2Type(fields[2], ref type, ref bounds);
                string[] addresses = new string[fields.Length - 4];
                Array.Copy(fields, 4, addresses, 0, fields.Length - 4);
                //Copy all address strings for this line
                addIO(mode, type, fields[0], fields[3], addresses, bounds);
                return sinter.sinter_AppError.si_OKAY;
            }

            throw new System.IO.IOException(String.Format("Sinter Config Line unknown: {0}", l));

        }

        private void processMin(string[] fields)
        {
            int i = 0;
            string line = null;
            string[] n = null;
            int j = 0;
            sinter_IVariable thisIVar = getIOByName(fields[1]);
            if (thisIVar == null)
            {
                throw new System.IO.IOException("Cannot apply labels, Object " + fields[1] + " not found");
            }
            if (thisIVar.isScalar)
            {
                sinter_Variable thisVar = (sinter_Variable)thisIVar;
                if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE)
                {
                    thisVar.minimum = Convert.ToDouble(fields[2]);
                }
                else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER)
                {
                    thisVar.minimum = Convert.ToInt32(fields[2]);
                }
            }
            else if (thisIVar.isVec)
            {
                sinter_Vector thisVar = (sinter_Vector)thisIVar;
                if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC)
                {
                    thisVar.setElementMin(0, Convert.ToDouble(fields[2]));
                }
                else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER_VEC)
                {
                    thisVar.setElementMin(0, Convert.ToInt32(fields[2]));
                }
                for (i = 1; i <= thisVar.size - 1; i++)
                {
                    line = inFileStream.ReadLine();
                    n = line.Split('|');
                    if (n.Length == 0)
                        return;
                    for (j = 0; j <= n.Length - 1; j++)
                    {
                        n[j] = n[j].Trim();
                    }
                    if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC)
                    {
                        thisVar.setElementMin(i, Convert.ToDouble(n[0]));
                    }
                    else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER_VEC)
                    {
                        thisVar.setElementMin(i, Convert.ToInt32(n[0]));
                    }
                }
            }
            else if (thisIVar.isTable) //getIOByName(fields[1]).isMat | 
            {
                sinter_Table thisTable = (sinter_Table)thisIVar;
                for (j = 2; j <= fields.Length - 1; j++)
                {
                    sinter_Variable thisVar = (sinter_Variable)thisTable.getElement(0, j - 2);
                    if(thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE) {
                        thisVar.minimum = Convert.ToDouble(fields[j]);
                    } else if(thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER) {
                        thisVar.minimum = Convert.ToInt32(fields[j]);
                    } else {
                        throw new System.IO.IOException("Cannot apply minimum on " + thisVar.name + ".  Bad type");
                    }

                }
                for (i = 1; i <= thisTable.MNRows; i++)
                {
                    line = inFileStream.ReadLine();
                    n = line.Split('|');
                    if (n.Length == 0)
                        return;
                    for (j = 0; j <= n.Length - 1; j++)
                    {
                        n[j] = n[j].Trim();
                    }
                    for (j = 0; j <= n.Length - 1; j++)
                    {
                        sinter_Variable thisVar = (sinter_Variable)thisTable.getElement(0, j);
                        if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE)
                        {
                            thisVar.minimum = Convert.ToDouble(fields[j]);
                        }
                        else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER)
                        {
                            thisVar.minimum = Convert.ToInt32(fields[j]);
                        }
                        else
                        {
                            throw new System.IO.IOException("Cannot apply minimum on " + thisVar.name + ".  Bad type");
                        }

                    }
                }
            }
        }

        private void processMax(string[] fields)
        {
            int i = 0;
            string line = null;
            string[] n = null;
            int j = 0;
            sinter_IVariable thisIVar = getIOByName(fields[1]);
            if (thisIVar == null)
            {
                throw new System.IO.IOException("Cannot apply labels, Object " + fields[1] + " not found");
            }
            if (thisIVar.isScalar)
            {
                sinter_Variable thisVar = (sinter_Variable)thisIVar;
                if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE)
                {
                    thisVar.maximum = Convert.ToDouble(fields[2]);
                }
                else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER)
                {
                    thisVar.maximum = Convert.ToInt32(fields[2]);
                }
            }
            else if (thisIVar.isVec)
            {
                sinter_Vector thisVar = (sinter_Vector)thisIVar;
                if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC)
                {
                    thisVar.setElementMax(0, Convert.ToDouble(fields[2]));
                }
                else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER_VEC)
                {
                    thisVar.setElementMax(0, Convert.ToInt32(fields[2]));
                }
                for (i = 1; i <= thisVar.size - 1; i++)
                {
                    line = inFileStream.ReadLine();
                    n = line.Split('|');
                    if (n.Length == 0)
                        return;
                    for (j = 0; j <= n.Length - 1; j++)
                    {
                        n[j] = n[j].Trim();
                    }
                    if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC)
                    {
                        thisVar.setElementMax(i, Convert.ToDouble(n[0]));
                    }
                    else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER_VEC)
                    {
                        thisVar.setElementMax(i, Convert.ToInt32(n[0]));
                    }
                }
            }
            else if (thisIVar.isTable)
            {
                sinter_Table thisTable = (sinter_Table)thisIVar;
                for (j = 2; j <= fields.Length - 1; j++)
                {
                    sinter_Variable thisVar = (sinter_Variable)thisTable.getElement(0, j - 2);
                    if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE)
                    {
                        thisVar.maximum = Convert.ToDouble(fields[j]);
                    }
                    else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER)
                    {
                        thisVar.maximum = Convert.ToInt32(fields[j]);
                    }
                    else
                    {
                        throw new System.IO.IOException("Cannot apply minimum on " + thisVar.name + ".  Bad type");
                    }

                }
                for (i = 1; i <= thisTable.MNRows; i++)
                {
                    line = inFileStream.ReadLine();
                    n = line.Split('|');
                    if (n.Length == 0)
                        return;
                    for (j = 0; j <= n.Length - 1; j++)
                    {
                        n[j] = n[j].Trim();
                    }
                    for (j = 0; j <= n.Length - 1; j++)
                    {
                        sinter_Variable thisVar = (sinter_Variable)thisTable.getElement(0, j);
                        if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE)
                        {
                            thisVar.maximum = Convert.ToDouble(fields[j]);
                        }
                        else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER)
                        {
                            thisVar.maximum = Convert.ToInt32(fields[j]);
                        }
                        else
                        {
                            throw new System.IO.IOException("Cannot apply maximum on " + thisVar.name + ".  Bad type");
                        }

                    }
                }
            }
        }

        private void processDefault(string[] fields)
        {
            int i = 0;
            string line = null;
            string[] n = null;
            int j = 0;
            sinter_IVariable thisIVar = (sinter_IVariable) getIOByName(fields[1]);
            if (thisIVar == null)
            {
                throw new System.IO.IOException("Cannot apply labels, Object " + fields[1] + " not found");
            }
            if (thisIVar.isScalar)
            {
                sinter_Variable thisVar = (sinter_Variable)thisIVar;
                if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE)
                {
                    thisVar.dfault = Convert.ToDouble(fields[2]);
                }
                else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER)
                {
                    thisVar.dfault = Convert.ToInt32(fields[2]);
                }
                else if (thisVar.type == sinter_Variable.sinter_IOType.si_STRING)
                {
                    thisVar.dfault = Convert.ToString(fields[2]);
                }
            }
            else if (thisIVar.isVec)
            {
                sinter_Vector thisVar = (sinter_Vector)thisIVar;
                if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC)
                {
                    thisVar.setElementDefault(0, Convert.ToDouble(fields[2]));
                }
                else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER_VEC)
                {
                    thisVar.setElementDefault(0, Convert.ToInt32(fields[2]));
                }
                for (i = 1; i <= thisVar.size - 1; i++)
                {
                    line = inFileStream.ReadLine();
                    n = line.Split('|');
                    if (n.Length == 0)
                        return;
                    for (j = 0; j <= n.Length - 1; j++)
                    {
                        n[j] = n[j].Trim();
                    }
                    if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC)
                    {
                        thisVar.setElementDefault(i, Convert.ToDouble(n[0]));
                    }
                    else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER_VEC)
                    {
                        thisVar.setElementDefault(i, Convert.ToInt32(n[0]));
                    }
                }
            }
            else if (thisIVar.isTable)
            {
                sinter_Table thisTable = (sinter_Table)thisIVar;
                for (j = 2; j <= fields.Length - 1; j++)
                {
                    sinter_Variable thisVar = (sinter_Variable)thisTable.getElement(0, j - 2);
                    if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE)
                    {
                        thisVar.dfault = Convert.ToDouble(fields[j]);
                    }
                    else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER)
                    {
                        thisVar.dfault = Convert.ToInt32(fields[j]);
                    }
                    else
                    {
                        throw new System.IO.IOException("Cannot apply minimum on " + thisVar.name + ".  Bad type");
                    }

                }
                for (i = 1; i <= thisTable.MNRows; i++)
                {
                    line = inFileStream.ReadLine();
                    n = line.Split('|');
                    if (n.Length == 0)
                        return;
                    for (j = 0; j <= n.Length - 1; j++)
                    {
                        n[j] = n[j].Trim();
                    }
                    for (j = 0; j <= n.Length - 1; j++)
                    {
                        sinter_Variable thisVar = (sinter_Variable)thisTable.getElement(0, j);
                        if (thisVar.type == sinter_Variable.sinter_IOType.si_DOUBLE)
                        {
                            thisVar.dfault = Convert.ToDouble(fields[j]);
                        }
                        else if (thisVar.type == sinter_Variable.sinter_IOType.si_INTEGER)
                        {
                            thisVar.dfault = Convert.ToInt32(fields[j]);
                        }
                        else
                        {
                            throw new System.IO.IOException("Cannot apply maximum on " + thisVar.name + ".  Bad type");
                        }

                    }

                }
            }
            ////////////////////////////////////////////////////////////////
            //Now that we've set all the defaults, make them the basic value
            ////////////////////////////////////////////////////////////////
            thisIVar.resetToDefault();
        }

        private void processType(string l, ref sinter_Variable.sinter_IOType type, ref int n, ref int m)
        {
            string[] fields = null;
            string[] sz = null;
            n = 0;
            m = 0;
            //Checks if it's a table the bounds will be in brackets
            fields = l.Split('[');
            for (int i = 0; i <= fields.Length - 1; i++)
            {
                fields[i] = fields[i].Trim();
            }
            if (fields.Length == 2) 
                if (fields[1][fields[1].Length - 1] == ']') //Last character in fields[1]
                    fields[1] = fields[1].Substring(0, fields[1].Length - 1);  //Drop Last char (Bracket)
            if (fields.Length == 2)
            {
                sz = fields[1].Split(',');
                for (int i = 0; i <= sz.Length - 1; i++)
                {
                    sz[i] = sz[i].Trim();
                }
                n = Convert.ToInt32(sz[0]);
                if (sz.Length == 2)
                    m = Convert.ToInt32(sz[1]);
            }
            //End table bounds parsing stuff
            switch (fields[0])
            {
                case "int":
                    if (n == 0 & m == 0)
                    {
                        type = sinter_Variable.sinter_IOType.si_INTEGER;
                    }
                    else if (n > 0 & m == 0)
                    {
                        type = sinter_Variable.sinter_IOType.si_INTEGER_VEC;
                    }
                    break;
                case "double":
                    if (n == 0 & m == 0)
                    {
                        type = sinter_Variable.sinter_IOType.si_DOUBLE;
                    }
                    else if (n > 0 & m == 0)
                    {
                        type = sinter_Variable.sinter_IOType.si_DOUBLE_VEC;
                    }
                    break;
                case "string":
                    type = sinter_Variable.sinter_IOType.si_STRING;
                    if ((n > 0 | m > 0))
                        throw new System.IO.IOException("String vector or matrix not supported");
                    break;
                case "table":
                    type = sinter_Variable.sinter_IOType.si_TABLE;
                    break;
                default:
                    type = sinter_Variable.sinter_IOType.si_STRING;
                    break;
            }
        }

        private void processLabels(string type, string[] fields, sinter_IVariable thisIVar)
        {
            string[] labels = new string[fields.Length - 2];
            int labelLen = labels.Length;
            int ii = 0;

            for (ii = 0; ii <= labelLen - 1; ii++) {
                labels[ii] = fields[ii + 2];
            }

            if (type == "r")
            {
                if (thisIVar.isTable)
                {
                    sinter_Table thisTable = (sinter_Table)thisIVar;
                    thisTable.rowLabelCount = labelLen;
                    for (ii = 0; ii <= labelLen - 1; ii++)
                    {
                        thisTable.setRowLabel(ii, labels[ii]);
                    }
                }
                else
                {
                    sinter_Vector thisVar = (sinter_Vector)thisIVar;
                    thisVar.rowLabelCount = labelLen;
                    for (ii = 0; ii <= labelLen - 1; ii++)
                    {
                        thisVar.setRowLabel(ii, labels[ii]);
                    }
                }
            }
            if ((type == "c"))
            {
                sinter_Table thisTable = (sinter_Table)thisIVar;
                thisTable.colLabelCount = labelLen;
                for (ii = 0; ii <= labelLen - 1; ii++)
                {
                    thisTable.setColLabel(ii, labels[ii]);
                }
            }

            if ((type == "rs"))
            {
                if (thisIVar.isTable)
                {
                    sinter_Table thisTable = (sinter_Table)thisIVar;
                    thisTable.rowStringCount = labelLen;
                    for (ii = 0; ii <= labelLen - 1; ii++)
                    {
                        thisTable.setRowString(ii, labels[ii]);
                    }
                }
                else
                {
                    sinter_Vector thisVar = (sinter_Vector)thisIVar;
                    thisVar.rowStringCount = labelLen;
                    for (ii = 0; ii <= labelLen - 1; ii++)
                    {
                        thisVar.setRowString(ii, labels[ii]);
                    }
                }
            }
            if ((type == "cs"))
            {
                sinter_Table thisTable = (sinter_Table)thisIVar;
                thisTable.colStringCount = labelLen;
                for (ii = 0; ii <= labelLen - 1; ii++)
                {
                    thisTable.setColString(ii, labels[ii]);
                }
            }

        }

        
        private void addIO(sinter_Variable.sinter_IOMode iomode, sinter_Variable.sinter_IOType type, string name, string desc, string[] addStrings, int[] bounds)
        {
            if (type == sinter_Variable.sinter_IOType.si_TABLE)
            {
                sinter_Table thisTable = new sinter_Table();
                if (bounds == null || bounds.Length < 2)
                {
                    thisTable.init(name, iomode, desc, addStrings, 0, 0);
                }
                else
                {
                    thisTable.init(name, iomode, desc, addStrings, bounds[0], bounds[1]);
                }
                addTable(thisTable);
            }
            else if (type == sinter_Variable.sinter_IOType.si_DOUBLE_VEC ||
                type == sinter_Variable.sinter_IOType.si_INTEGER_VEC ||
                type == sinter_Variable.sinter_IOType.si_STRING_VEC)
            {
                sinter_Vector o = new sinter_Vector();
                if (bounds == null || bounds.Length < 1)
                {
                    o.init(name, iomode, type, desc, addStrings, 0);
                }
                else
                {
                    o.init(name, iomode, type, desc, addStrings, bounds[0]);
                }
                    addVariable(o);
                
            }
            else
            {
                sinter_Variable o = new sinter_Variable();
                o.init(name, iomode, type, desc, addStrings);
                if (o.isSetting)
                {
                    addSetting(o);
                }
                else
                {
                    addVariable(o);
                }
            }
        }
    }

}
