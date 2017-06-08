using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sinter
{
    public class sinter_stringLabel
    {

        // 
        //  This class is used to store text labels for spreadsheet output
        // 
        private string o_text;

        //  the text for a cell
        private int o_row;

        //  the cell row
        private int o_col;

        //  the cell column
        public sinter_stringLabel()
        {
            //  A constuctor
        }

        public void setAll(int r, int c, string t)
        {
            //  set all the label properties at once
            row = r;
            col = c;
            text = t;
        }

        // 
        //  The properties below are pretty self explanitory
        // 
        public int row
        {
            get
            {
                return o_row;
            }
            set
            {
                o_row = value;
            }
        }

        public int col
        {
            get
            {
                return o_col;
            }
            set
            {
                o_col = value;
            }
        }

        public string text
        {
            get
            {
                return o_text;
            }
            set
            {
                o_text = value;
            }
        }
    }
}
