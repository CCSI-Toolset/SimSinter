using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CSVFileRW
{
    
    /// <summary>
    /// Class to write data to a CSV file
    /// </summary>
    public class CsvFileWriter : StreamWriter
    {
        public CsvFileWriter(Stream stream)
            : base(stream)
        {
        }

        public CsvFileWriter(string filename)
            : base(filename)
        {
        }

        /// <summary>
        /// Writes a single row to a CSV file.
        /// </summary>
        /// <param name="row">The row to be written</param>
        public void WriteRow(List<object> row)
        {
            StringBuilder builder = new StringBuilder();
            bool firstColumn = true;
            foreach (object value in row)
            {
                // Add separator if this isn't the first value
                if (!firstColumn)
                    builder.Append(',');
                // Implement special handling for values that contain comma or quote
                // Enclose in quotes and double up any double quotes
                if (value is string)
                {
                    string sval = (string)value;
                    if (sval.IndexOfAny(new char[] { '"', ',' }) != -1)
                    {
                        builder.AppendFormat("\"{0}\"", sval.Replace("\"", "\"\""));
                    }
                    else
                    {
                        builder.Append(sval);
                    }
                }
                else
                    builder.Append(Convert.ToString(value));
                firstColumn = false;
            }

            WriteLine(builder.ToString());
        }

    }

    /// <summary>
    /// Class to read data from a CSV file
    /// </summary>
    public class CsvFileReader : StreamReader
    {
        public CsvFileReader(Stream stream)
            : base(stream)
        {
        }

        public CsvFileReader(string filename)
            : base(filename)
        {
        }

        /// <summary>
        /// Reads a row of data from a CSV file
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool ReadRow(List<object> row)
        {
            string lineText = ReadLine();
            if (String.IsNullOrEmpty(lineText))
                return false;

            int pos = 0;
            int rows = 0;

            while (pos < lineText.Length)
            {
                string value;

                // Special handling for quoted field
                if (lineText[pos] == '"')
                {
                    // Skip initial quote
                    pos++;

                    // Parse quoted value
                    int start = pos;
                    while (pos < lineText.Length)
                    {
                        // Test for quote character
                        if (lineText[pos] == '"')
                        {
                            // Found one
                            pos++;

                            // If two quotes together, keep one
                            // Otherwise, indicates end of value
                            if (pos >= lineText.Length || lineText[pos] != '"')
                            {
                                pos--;
                                break;
                            }
                        }
                        pos++;
                    }
                    value = lineText.Substring(start, pos - start);
                    value = value.Replace("\"\"", "\"");
                }
                else
                {
                    // Parse unquoted value
                    int start = pos;
                    while (pos < lineText.Length && lineText[pos] != ',')
                        pos++;
                    value = lineText.Substring(start, pos - start);
                }

                // Add field to list
                if (rows < row.Count)
                    row[rows] = value;
                else
                    row.Add(value);
                rows++;

                // Eat up to and including next comma
                while (pos < lineText.Length && lineText[pos] != ',')
                    pos++;
                if (pos < lineText.Length)
                    pos++;
            }
            // Delete any unused items
            while (row.Count > rows)
                row.RemoveAt(rows);

            // Return true if any columns read
            return (row.Count > 0);
        }
    }
}

