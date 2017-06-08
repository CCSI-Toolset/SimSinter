using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sinter
{
    //
    // This class represents a 2D array of IOObjects.  It's mostly used for formatting purposes, 
    // but used to be an IOObject in it's own right.
    //

    public class sinter_Table : sinter_IVariable
    {
 
        //-------------------------------------------
        // Object variables
        //-------------------------------------------
        //the value of a variable
        protected sinter_Variable[,] o_value;

        //the name for a variable
        private string o_name;
        //a description of the variable
        private string o_description;

        //the IO mode, input or output
        private sinter_Variable.sinter_IOMode o_mode;

        //the row pposition of the var on spreadsheet
        private int o_rowOff;
        //the col position of the variable on spreadsheet
        private int o_colOff;
        //row labels for vector or matrix
        private string[] o_rowLabels;
        //column labels for vector or matrix
        private string[] o_colLabels;

        //a strings used to locate the varaible in a simulation program, may be more than one
        private string[] o_addressStrings;
        //row strings used in tables to make systematic address strings
        private string[] o_rowStrings;
        //column strints used in tables to make systematic address strings 
        private string[] o_colStrings;

        //----------------------------------
        //Constuctor
        //----------------------------------

        public sinter_Table()
            : base()
        {
            o_mode = sinter_Variable.sinter_IOMode.si_IN;
        }

        public void init(string thisName, sinter_Variable.sinter_IOMode iomode, string desc, string[] addStrings, int nn, int mm) {
            o_name = thisName;
            o_mode = iomode;
            o_description = desc;
            o_addressStrings = addStrings;
            if (nn >= 1 && mm >= 1)
            {
                o_value = new sinter_Variable[nn, mm];
            }
            else
            {
                o_value = null;
            }
        }

        public virtual void setInput(string varName, int row, int col, JToken jvalue, string in_units)
        {

            //Check that the indexes are valid
            if (row >= 0 && row <= (int)MNRows &&
                col >= 0 && col <= (int)MNCols)
            {
                setElement(row, col, sinter_HelperFunctions.convertJTokenToNative(jvalue));
            }
            else
            {
                throw new System.IO.IOException(string.Format("Table {0} has an index out of range. Range: {1},{2} Index: {3},{4}", varName, (int)MNRows, (int)MNCols, row, col));
            }
        }

        public virtual JToken getOutput()
        {
            throw new NotImplementedException("Table doesn't currently output contact Jim Leek");
        }
        //----------------------------------
        // Main properties
        //----------------------------------

        
        public sinter_Variable[,] Value
        {
            // value is the data stored in this object
            // value is an object any type of data can be stored
            get { return o_value; }
        }

        public sinter_Variable.sinter_IOMode mode
        {
            // this in the IO mode, input or output or both
            get { return o_mode; }
            set { o_mode = value; }
        }

        public string name
        {
            //this is the name of the object
            get { return o_name; }
            set { o_name = value; }
        }

        public string description
        {
            // this is a description of the object
            // it would be nice if units were in 
            // the description too
            get { return o_description; }
            set { o_description = value; }
        }

        public void setDefaultToValue()
        {
            for (int ii = 0; ii <= MNRows; ii++)
            {
                for (int jj = 0; jj <= MNCols; jj++)
                {
                    o_value[ii, jj].setDefaultToValue();
                }
            }
        }
        
        public void resetToDefault()
        {
            for (int ii = 0; ii <= MNRows; ii++)
            {
                for (int jj = 0; jj <= MNCols; jj++)
                {
                    o_value[ii, jj].resetToDefault();
                }
            }
        }
    

        public String typeString
        {
            get
            {
                return "table[" + Convert.ToString(MNRows) + "," + Convert.ToString(MNCols) + "]";
            }
        }

        public sinter_Variable getVariable(int ii, int jj)
        {
            return o_value[ii, jj];
        }

        public void setVariable(int ii, int jj, sinter_Variable value)
        {
            o_value[ii, jj] = value;
        }


        public object getElement(int ii, int jj)
        {
            return o_value[ii, jj].Value;
        }

        public void setElement(int ii, int jj, object value)
        {
            o_value[ii, jj].Value = value;
        }

        public object getElementMin(int ii, int jj) {
            return o_value[ii, jj].minimum;
        }

         public void setElementMin(int ii, int jj, object value)
            {
                o_value[ii, jj].minimum = value;
            }

         public object getElementMax(int ii, int jj)
         {
             return o_value[ii, jj].maximum;
         }

         public void setElementMax(int ii, int jj, object value)
         {
             o_value[ii, jj].maximum = value;
         }

         public object getElementDefault(int ii, int jj)
         {
             return o_value[ii, jj].dfault;
         }

         public void setElementDefault(int ii, int jj, object value)
         {
             o_value[ii, jj].dfault = value;
         }
        

        public int MNRows
        {
            get
            {
                return o_value.GetUpperBound(0);
            }
        }
        public int MNCols
        {
            get
            {
                return o_value.GetUpperBound(1);
            }
        }

        //---------------------------------------
        // Comunication with Simulation Program
        //---------------------------------------

        //<Summary>
        //send value to simulation
        //</Summary>
        public void sendToSim(sinter_InteractiveSim o_sim)
        {
            if (mode != sinter_Variable.sinter_IOMode.si_OUT)
            {
                //This allows one to pass in simulation settings as a sinter varaible if it has a path of the form: setting(blah) = value
                //So, setting( is a "reserved word" in sinter variable pathes (Maybe a special character would be better?
                for (int ii = 0; ii <= rowStringCount - 1; ii++)
                {
                    for (int jj = 0; jj <= colStringCount - 1; jj++)
                    {
                        o_value[ii, jj].sendToSim(o_sim);
                    }
                }
            }
        }

        //<Summary>
        //get value from a simulation
        //</Summary>
        public void recvFromSim(sinter_Sim o_sim)
        {
            if (mode != sinter_Variable.sinter_IOMode.si_IN)
            {
                //This allows one to pass in simulation settings as a sinter varaible if it has a path of the form: setting(blah) = value
                //So, setting( is a "reserved word" in sinter variable pathes (Maybe a special character would be better?
                for (int ii = 0; ii <= rowStringCount - 1; ii++)
                {
                    for (int jj = 0; jj <= colStringCount - 1; jj++)
                    {
                        o_value[ii, jj].recvFromSim(o_sim);
                    }
                }
            }
        }

        public void initializeUnits(sinter_Sim o_sim)
        {
            for (int ii = 0; ii <= rowStringCount - 1; ii++)
            {
                for (int jj = 0; jj <= colStringCount - 1; jj++)
                {
                    o_value[ii, jj].initializeUnits(o_sim);
                }
            }
        }


        //------------------------------------
        // Layout and formating
        //------------------------------------

        public void setOffset(int row, int col)
        {
            //set the row and column position
            //for spreadsheet layout
            o_rowOff = row;
            o_colOff = col;
        }
        public int rowOff
        {
            // This is a row offset for laying out a spreadsheet
            get { return o_rowOff; }
            set { o_rowOff = value; }
        }
        public int colOff
        {
            // this is a column offset for laying out a spreadsheet
            get { return o_colOff; }
            set { o_colOff = value; }
        }

        //
        //The row label array does not necessarily match
        //the length of a vector so need to check for out of bounds
        //The reason its independant like this is that some vector
        //may change length like a vector of concentrations on a
        //tray in a distalation column and the number of trays
        //may be a changing quantity
        //
        public string[] rowLabels
        {
            get { return o_rowLabels; }
            set
            {
                o_rowLabels = value;

            }
        }

        public string getRowLabel(int i)
        {
            if ((i < 0 | i >= rowLabelCount))
            {
                return i.ToString();
                //if out of range just return the index as the label
            }
            else
            {
                return o_rowLabels[i];
            }
        }

        public void setRowLabel(int i, string value)
        {
            if ((i >= 0 & i < rowLabelCount))
            {
                o_rowLabels[i] = value;
            }
        }
        
          
        public int rowLabelCount
        {
            // the number of row labels does not have to be the same a
            // a vector size
            get
            {
                if (o_rowLabels == null)
                    return 0;
                return o_rowLabels.Length;
            }
            set
            {
                Array.Resize(ref o_rowLabels, value);

            }
        }

        public string[] colLabels
        {
            get { return o_colLabels; }
            set
            {
                o_colLabels = value;

            }
        }



        public string getColLabel(int i)
        {
                if ((i < 0 | i >= colLabelCount))
                {
                    return i.ToString();
                }
                else
                {
                    return o_colLabels[i];
                }
            }
      public void setColLabel(int i, string value)
            {
                if ((i >= 0 & i < colLabelCount))
                {
                    o_colLabels[i] = value;
                }
            }
        
         
        public int colLabelCount
        {
            // the number of column labels does no have to be
            // the same as the number of columns in a matrix
            get
            {
                if (o_colLabels == null)
                    return 0;
                return o_colLabels.Length;
            }
            set
            {
                Array.Resize(ref o_colLabels, value);

            }
        }

        //----------------------------------------
        // Table Functions
        //----------------------------------------


        public int addressStringCount
        {
            get
            {
                if (o_addressStrings == null)
                    return 0;
                return o_addressStrings.Count();
            }
            set
            {
                Array.Resize(ref o_addressStrings, value);
            }
        }

        public int rowStringCount
        {
            get
            {
                if (o_rowStrings == null)
                    return 0;
                return o_rowStrings.Count();
            }
            set
            {
                Array.Resize(ref o_rowStrings, value);
            }
        }
        public int colStringCount
        {
            get
            {
                if (o_colStrings == null)
                    return 0;
                return o_colStrings.Count();
            }
            set
            {
                Array.Resize(ref o_colStrings, value);
            }
        }

        public string[] addressStrings
        {
            // get of set a strings that can be used to look up the varaible value in a simulation program
            get { return o_addressStrings; }
            set { o_addressStrings = value; }
        }

        public string[] rowStrings
        {
            get { return o_rowStrings; }
            set { o_rowStrings = value; }
        }

        public string[] colStrings
        {
            get { return o_colStrings; }
            set { o_colStrings = value; }
        }

        public string getRowString(int i)
        {
            return o_rowStrings[i];
        }

        public void setRowString(int i, string rowString)
        {
            o_rowStrings[i] = rowString;
        }

        public string getColString(int i)
        {
            return o_colStrings[i];
        }
        public void setColString(int i, string colString)
        {
            o_colStrings[i] = colString;
        }

        ////////////////////////////
        // Type Checks
        /// <summary>
        /// ////////////////////////
        /// </summary>
        public bool isScalar
        {
            get
            {
                return false;
            }
        }


        public bool isVec
        {
            get
            {
                return false;
            }
        }

        public bool isSetting
        {
            get
            {
                return false;
            }
        }

        public bool isTable
        {
            get
            {
                return true;
            }
        }

        public bool isInput
        {
            get
            {
                if (o_mode == sinter_Variable.sinter_IOMode.si_IN)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public bool isOutput
        {
            get
            {
                if (o_mode == sinter_Variable.sinter_IOMode.si_OUT)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public sinter_Variable.sinter_IOType type
        {
            get
            {
                return sinter_Variable.sinter_IOType.si_TABLE;
            }
        }

        /// <summary>
        /// In text setup files the IO Variables in a table need to be generated, as they don't exist independently in the
        /// file.  In a JSON file they do exist independently, so this function is unnecessary.
        /// </summary>

        public void fixupTable(sinter_SetupFile setupFile)
        {
            //First make sure the variable array is the right size
            int nn = rowStringCount;
            int mm = colStringCount;
            o_value = new sinter_Variable[nn, mm]; 

            for (int addressIndex = 0; addressIndex <= (addressStrings.Length - 1); addressIndex++)
            {
                for (int i = 0; i <= rowStringCount - 1; i++)
                {
                    for (int j = 0; j <= colStringCount - 1; j++)
                    {
                        string aString = addressStrings[addressIndex].Replace("%r", getRowString(i));
                        aString = aString.Replace("%c", getColString(j));
                        //Now create and init vars
                        sinter_Variable thisVar = new sinter_Variable();
                        //We don't have a real name, so just use table[i,j]
                        String thisName = String.Format("{0}[{1},{2}]", o_name, i, j);
                        String desc = String.Format("{0}[{1},{2}] (Table Description : {3} )", name, getRowLabel(i), getColLabel(j), description);
                        String[] aStrings = new String[1] { aString };
                        thisVar.init(thisName, mode, sinter_Variable.sinter_IOType.si_DOUBLE, desc, aStrings);
                        thisVar.tableRow = i;
                        thisVar.tableCol = j;
                        thisVar.tableName = name;
                        thisVar.table = this;
                        setVariable(i, j, thisVar);
                        setupFile.addVariable(thisVar);
                    }
                }
            }
        }



    }
    
}
