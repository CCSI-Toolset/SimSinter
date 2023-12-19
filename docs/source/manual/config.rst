SIMSinter Configuration File
============================

Overview
--------

The central activity and difficulty of using SimSinter is writing the
configuration files. It is assumed that a user with a deep understanding
of the simulation being used also writes the Sinter configuration file.
Generally this is the person that wrote the Aspen simulation.

The configuration file contains general information about a simulation,
the simulation file location, and the variable (input/output)
definitions. Typographical errors in the configuration file were a
common source of problems when setting up a Sinter interface; therefore,
the SinterConfigGUI was built to simplify the creation.

**Types**

All variables must have a “mode” (i.e., “input” or “output”).

All variables must also have a “primitive type” (i.e., double, integer,
or string).

All variables also have a “class” scalar, vector, table, or setting.
Class is the most complex dimension because how to specify it varies
between the Text and JSON Sinter configuration formats.

-  **Scalar** – A single value inputs or outputs. They may have type
   int, double, or string.

-  **Vector** – Contains a series of values of the same type (i.e., they
   are 1-D arrays). The type is declared as “primativetype[length]”
   (e.g., “double[201]” declares that variable as a vector or 201
   doubles).

-  **Table** – Contains a 2-D table of logically related scalars. Table
   does not have an equivalent type in any simulator, rather, it is a
   convenience designed for formatting a set of variables in Excel.
   Table is of little use if SimSinter is not being used with Excel.

   In the JSON format tables are unrestricted. Any value in a table may
   have any type, and be located anywhere in the simulation data tree. Each
   entry in a table must refer to an already defined scalar in the scalar
   section of the configuration file.

   In Text format tables only doubles are permitted and an individual value
   location in the simulator data tree must follow a set of rules. However,
   the Text format table definition is much more concise and writable. It
   is hard to imagine anyone writing a JSON format table by hand.

-  **Setting** – A special scalar class for defining things about how
   the simulation is run, rather than defining a variable in the
   simulation. Settings are therefore simulator specific. For example,
   using SimSinter with ACM enables two settings: “homotopy,” which
   defines which solver is run, and “printlevel” which defines how much
   error detail to return. Like scalars, settings may have type int,
   double, or string.

**Naming**

The Sinter variables can be organized into a tree using periods in the
variable names, for example, the variables: stream-01.T, stream-02.T,
stream-01.P, and stream-02.P, stream-01.comp.O2, stream-02.comp.O2, stream-01.comp.N2, and stream-02.comp.N2 would be
organized into the following tree:

-  Root

-  stream-01

   -  T

   -  P

   -  comp

      -  O2

      -  N2

-  stream-02

   -  T

   -  P

   -  comp

      -  O2

      -  N2

Address Strings
---------------

Every variable input/output must have a matching address for finding the
variable in the simulation. This gets its own section because each
simulator has a different Address format, and way to find the address.

An input scalar or vector may have **multiple** address strings. This is
useful when a given variable has to be the same in different places in
the simulation. For example, a user may want to vary a reaction constant
that may be used in multiple blocks.

Address Strings for Tables are not covered in this section as the JSON
format does not use Address Strings for Tables. Tables are format
specific and are covered in those sections.

Settings are also not covered in this section, as their format is also
Text and JSON format specific, and are covered in those sections.

**Aspen Plus**

All Aspen Plus variables are held in the Aspen Plus data tree. The
variables have addresses to identify them in the tree. The variables are
“\\” separated and of the form:
\\Data\\Streams\\FEED\\Input\\TEMP\\MIXED.

Unfortunately, these addresses are not easy to find inside Aspen Plus.
They cannot be identified from inside the Data Explorer. A user must use
the Variable Explorer. The Variable Explorer is found at Tools →
Variable Explorer.

The Variable Explorer gives a tree view of the data. Open Root → Data.
From there a user can find Streams, Blocks, etc. When the user finds
the desired node, the user can copy the node address out of the second
box below “Path to Node.” It reads:
Application.Tree.FindNode(“\\Data\\Blocks\\FLASH\\Input\\
DIAMETER”). Copy and paste the portion within the quotes to the
“SinterConfigGUI Selected Path” text box, or just navigate through the
Variable Tree to the same location.

An experienced Aspen Plus modeler should do fine with this, but a less
experienced modeler may need to consult the documentation or request
help.

**Aspen Custom Modeler**

All ACM variables are held in a tree, but the tree is not as obvious as
the Aspen Plus tree. In ACM, the flowsheet is the root of the tree, so
any flowsheet variables are identified by name. Other variables use “.”
separated addresses of the form: Flash.o_port_liq.T.

This variable breaks down to: The Flash Block → The liquid multiport →
The temperature.

Generally these addresses can be discovered pretty easily. Open the
“AllVariables Table” on the block or stream of interest. Inside the
table is a list of variables (the ones that are not hidden). Identify
the desired variable on that table. Then write “blockName.VarName” in
the “Selected Path” section of the SinterConfigGUI. An even easier way
is to use the search function built into ACM and the SinterConfigGUI.

For example, given a stream “Liquid,” double-click the stream in the
flowsheet. Find the Ethanol composition “z(“ETHANOL”)” in the table. The
resulting address is “Liquid.z(“ETHANOL”).”

Address strings for vectors are similar. If the “AllVariables Table” is
opened and contains a series of values with the same name except for a
trailing number in parenthesis that is a vector. For example:
ADSA.db.Value(0), ADSA.db.Value(1)… ADSA.db.Value(200). The address of
that vector is “ADSA.db.Value.” When writing the SinterConfig file, do
not forget to include the length of the vector correctly in the type.
Since that vector is “0..200,” the type is “double[201].”

**Microsoft Excel**

For Excel define the cell to set or get data from by
“Worksheet$Column$Row.” (The “$” is used because Excel uses “$” as a
separator to denote absolute addresses.)

If the user has a worksheet named “weight” and wants to access cell “C2”
in that worksheet, the Sinter address is: “weight$C$2.”

Vectors are assumed to proceed left to right, and the address is the
address of the first value. If a vector has type “double[3]” and an
address “height$A$6,” the three values are at addresses: “height$A$6,”
“height$B$6,” and “height$C$6.”

Settings
--------

Each supported simulator has a set of supported settings. The list of
possible settings for any simulator is enormous. Therefore, rather than
attempting to either enable them all, or predict what users may need,
they have been added as required.

**Aspen Plus**

There are currently no settings for Aspen Plus.

**Aspen Custom Modeler**

-  RunMode: String. Default “Steady State”

   RunMode determines the run mode of the simulation. The possible values
   are “Steady State,” “Dynamic,” and “Optimization.”

-  homotopy: Int. Default: 0

   If 1, the solver is set to the homotopy solver, and all of the inputs
   are set as homotopy targets.

   If 0 (or unused), the standard solver is used, the inputs are set
   directly, and the simulation is solved directly.

-  printlevel: Int. Default: 0

   Sets the level of error reporting. 0 provides the least detail on
   errors. 5 provides the maximum detail. (**Note:** 5 can provide so much
   detail on an error that the simulation can take an extremely long time
   returning all of the error messages.)

-  TimeSeries: double[]. Default: 0

   Only used for Dynamic simulation. Sets the end time of each time step of
   the Dynamic simulation. Dynamic variables are set and read at these
   times.

-  Snapshot: String. Default “”

   Only used for Dynamic simulation. Names the snapshot to start the
   Dynamic simulation from. If left as an empty string the simulation will
   start from time “0.”

   If Snapshots are used the simulation must be distributed with the .bak
   files in the “AM\_???” subdirectory.

**Microsoft Excel**

-  Macro: String. Default: “”

   SimSinter can optionally call a macro in Excel to perform the
   “simulation” desired. This setting gives the name of the macro to run.
   For example, in the included BMI example, the macro is “RunSinter.”

   If no macro should be run (all calculations are done “in sheet.”), the
   default empty-string does not run any macro.

Additional Variable Information
-------------------------------

Variables have some additional information that may be associated with
them. Some of this data is handled differently in the Text and JSON
Sinter configuration formats.

-  **default** – The simulation writer can include a default value for
   input variables in the simulation. This default is optional in the
   Text format, but is required in the JSON format.

   Because defaults are not required for the Text format, ConsoleSinter
   requires a separate defaults file when a Text format Sinter
   configuration is used.

   If SinterConfigGUI is used, the defaults in the JSON configuration are
   pulled from the current values of the simulation.

   The JSON configuration also has defaults for the outputs. The defaults
   are pulled from sim when it is run with the default inputs. The idea is
   that these are canonical outputs, and can be useful for comparison.

-  **units** – The units entry gives the expected unit of measurement
   for this variable. The input file also enables a unit of measurement
   to be defined. If the unit in the input file is different but
   compatible with the unit in the Sinter configuration file, SimSinter
   automatically converts the input value to the expected unit type.

   Units is a required entry in the JSON format, although it may be empty
   (“” or null) for unit-less values.

   The SinterConfigGUI automatically fills in the units values with the
   simulation defaults when it is run with Aspen Plus or ACM. Excel cannot
   provide this information; therefore, the configuration writer should
   provide the values if possible by typing them into the units field.

-  **min** – Min (minimum) and max (maximum) are both optional in both
   formats. Min and max provide a suggested range for the user to vary a
   given variable. A modeler may have some insight into how a variable
   might behave in the real world. A user may value this information,
   but is also free to ignore it.

   For example, a modeler may expect some cooling water to be 25°C on
   average, but the modeler may also know that the cooling water may, in
   reality, vary between 15°C and 40°C. Therefore, the modeler may set:
   default: 25 min:15 max: 40.

   The user may ignore this advice, and, vary the value 0C-20C in their
   experiment. Min and max are just suggestions, but may be valuable
   information.

-  **max** – (maximum) See min above.

See the Flash Example configuration files for examples of how this
information is used.

JSON Sinter Configuration Format
--------------------------------

**Meta-Data features**

The JSON Sinter Configuration format contains multiple items of file or
simulation meta-data that help describe the simulation, the
configuration, and how it is used. All of these meta-data features are
contained in the top level of the file, they are not bound into sections
like the variables are.

-  Every file must declare it’s file format version.
   There are currently 2 JSON file format versions 
   (there is also an older, deprecated, “text” format.)
   The first JSON format was version 0.2. It is indentified with::
   
    "filetype" : "sinterconfig",
    "version" : 0.2,
   
   The new JSON file format is 0.3. It is identified with::
   
    "filetype": "sinterconfig",
    "filetype-version": 0.3,
    
-  Each file has a set four meta-data entries to describe the file
   that are defined by the user::

    "title": “This is a nice short title for the simulation”,
    "author": “This is the person who configured the simulation”,
    "date": "3/15/2016",
    "description": “This is a long, detailed field covering everything
     else users should know”,

-  The file also has a version number for the configuration itself. It
   defaults to 1.0 when the file is first created, and will
   automatically increment each time the file is edited in
   SinterConfigGUI, but the user can also set it manually::

    "config-version": "1.0",

-  The “application block” says which simulator the simulation runs
   under, and has optional simulator version constraints, as described
   in the tutorial sections.
   
   In the file the “internal version number” of the simulator is used.
   Many programs have version names that are used for marketing
   purposes, and version numbers that are used internally. For
   example, Microsoft Excel 2010 is actually version 14.0.
   SinterConfigGUI attempts to show the marketing name, as that is
   more familiar to users, but internally SimSinter uses the “real”
   version numbers::

    "application": {
        "name": "Aspen Custom Modeler",
        "version": "34.0",
        "constraint": "AT-LEAST",
    },

-  The “model block” declares the main simulation file to open with
   the simulator. It also includes a hash to verify that any file
   found on the file system is actually the file intended at
   configuration time::

    "model": {
        "file": "Flash_Example.acmf",
        "DigestValue": "8eede360cab95e12376c2f9d9013a794b4e86b5d",
        "SignatureMethodAlgorithm": "sha1"
    },

-  The “input-files” block contains all the OTHER input files that may
   be required by the simulation. Some simulations require extra files
   beyond the model file, such as DLLs containing extra functionality,
   or snapshot files for reloading the simulation. This block is often
   empty, and it didn’t even exist in the 0.2 version of the
   configuration file format.
   
   It has a similar format to the model block, input-files also
   includes a file hash signature, although if one cannot be
   generated, it may be left out.
   
   Empty Case::

    "input-files": [],

   Snapshot files example::

    "input-files": [
        {
            "file": "AM_BFB\\\\snapshot.bak",
            "DigestValue": "1e558b7328428907b572ee13d0684b75832e2bce",
            "SignatureMethodAlgorithm": "sha1"
        },
        {
            "file": "AM_BFB\\\\tasksnap.bak",
            "DigestValue": "7554617594ef7e2f7efb7dd4b8f9bdfce5e03466",
            "SignatureMethodAlgorithm": "sha1"
        }
    ],

**JSON Format Sections**

Rather than mixing input, outputs, and settings as is done in the Text
format, the JSON format separates them into separate optional sections.
There are **seven** such sections in the JSON Sinter configuration
format: Settings, Inputs, Outputs, Dynamic-Inputs, Dynamic-Outputs,
TableInputs, and TableOutputs. These sections are all optional.
Different simulations may select to not use any of the sections
(although if “TableInputs” is used the user also needs an “Inputs”
sections).

-  **Inputs** – Inputs have seven entries (not including the name, which
   is the key to the data): type, description, units, path, default,
   min, and max.

   min and max are the *only* optional entries. min and max define a range
   suggested by the simulation writer for UQ variance.

   There may be multiple address strings. In the JSON format, the Addresses
   are held in a JSON Array, so it is simple to add additional strings.

   Names are given in “.” separated tree format as previously described.
   absorber.input.dia and absorber.input.ht could be visualized as:

   absorber

   \|-> input

   \|-> dia

   \|-> ht

-  **Outputs** – Outputs have five entries (not including the name):
   units, path, type, description, and default. Default is optional
   for outputs. Default can be useful, if included, for comparing the
   output of a simulation to a canonical value, or for input to the
   Heat Integration GAMS simulation.

-  **Dynamic-Inputs** – Dynamic Inputs are exactly the same as normal
   Inputs in the configuration file. They are just contained in a
   different section. However, in the input file time is added as a
   dimension to the variable. So scalar variables are represented as
   1-D vectors in the input file, and vectors are represented as 2-D
   matrices in the input file.

-  **Dynamic-Outputs** – Dynamic Outputs are exactly the same as
   normal Outputs in the configuration file. They are just contained
   in a different section. However, in the output file time is added
   as a dimension to the variable. So scalar variables are represented
   as 1-D vectors in the output file, and vectors are represented as
   2-D matrices in the output file.

-  **Settings** – Settings have three fields (not including the name):
   description, default, and type.

-  **InputsTables** – There are two sections for defining tables,
   InputTables and OutputTables. Tables cannot have mixed inputs and
   outputs in the same table. Recall that tables are completely
   optional; tables are only for improving formatting in Excel.
   Sometimes it is easier to read a given set of data as a table than
   in a tree format.

   A table has a name, and three internal arrays: rows, columns, and
   contents.

   1. Rows: 1-D array of row labels for users.

   2. Columns: 1-D array of column labels for users.

   3. Contents: 2-D array of the variables used to make up the table.
      (These names must match the names defined in either the inputs or the
      outputs section. **Note:** In the example they do not match to save
      space and make the example easier to read.)

-  **OutputsTables** – See the *InputTables* section. It is exactly
   the same, except that all of the used variables must be output
   variables.

*JSON Sinter Example*

The following is a simple example of a JSON Sinter configuration file.
The file is pulled from a real file, but has been shortened::

    {
        "title" : "ExampleSinterConfig",
        "description" : "An Example of What the Future JSON Sinter Config Might Look Like",
        "filetype" : "sinterconfig",
        "version" : 0.2,
        "aspenfile" : "exampleMEA.bkp",
        "author" : "Jim Leek",
        "date" : "2012-03-13",
        "settings": {
            "initialize": {
                "description": "Warm up the simulation",
                "default": 1,
                "type": "int"
            }
        },
        "inputs": {
            "absorber.input.dia": {
                "units": "ft",
                "path": ["\\\\Data\\\\Blocks\\\\ABSORBER\\\\Input\\\\PR_DIAM\\\\1"],
                "default": 15.4,
                "type": "double",
                "description": "The diameter of the absorber, initial guess if ds active"
            },
            "absorber.input.ht": {
                "description": "The height of the absorber column",
                "min": 10,
                "default": 15.4,
                "max": 20,
                "units": "ft",
                "path": ["\\\\Data\\\\Blocks\\\\ABSORBER\\\\Input\\\\PR_PACK_HT"],
                "type": "double"
            }
        },
        "inputTables" : {},
        "outputTables": {
            "solvent.output.table": {
                "description" : "The Solvent Ouptut Table",
                "rows": [
                    "LEAN-01",
                    "RICH-01"
                ],
                "contents": [
                    [
                    "solvent.output.lean-01.mea",
                    "solvent.output.lean-01.h2O",
                    "solvent.output.lean-01.cO2"
                    ],
                    [
                    "solvent.output.rich-01.mea",
                    "solvent.output.rich-01.h2O",
                    "solvent.output.rich-01.cO2"
                    ]
                ],
                "columns": [
                    "MEA",
                    "H2O",
                    "CO2"
                ]
            }
        },
        "outputs": {
            "abs.output.ic.duty": {
                "units": "degF",
                "path": [
                    "\\\\Data\\\\Blocks\\\\ABSORBER\\\\Subobjects\\\\Pumparounds\\\\P-1\\\\Output\\\\DUTY4\\\\P-1"
                ],
                "default": 12.1,
                "type": "double",
                "description": "Heat duty of absorber"
            },
            "solvent.output.lean-01.mea": {
                "path": [
                    "\\\\Data\\\\Streams\\\\LEAN-01\\\\Output\\\\STR_MAIN\\\\MASSFRAC\\\\MIXED\\\\MEA"
                ],
                "type": "double",
                "default": 0.133889938,
                "description": "lean solvent output mea mass fraction",
                "units": ""
            },
            "solvent.output.lean-01.mea": {
                "path": [
                    "\\\\Data\\\\Streams\\\\LEAN-01\\\\Output\\\\STR_MAIN\\\\MASSFRAC\\\\MIXED\\\\H20"
                ],
                "type": "double",
                "default": 0.661512942,
                "description": "lean solvent output H2O mass fraction ",
                "units": ""
            },
            "solvent.output.lean-01.mea": {
                "path": [
                    "\\\\Data\\\\Streams\\\\LEAN-01\\\\Output\\\\STR_MAIN\\\\MASSFRAC\\\\MIXED\\\\CO2"
                ],
                "type": "double",
                "default": 6.23713113E-08,
                "description": "lean solvent output CO2 mass fraction ",
                "units": ""
            },
            "solvent.output.rich-01.mea": {
                "path": [
                    "\\\\Data\\\\Streams\\\\RICH-01\\\\Output\\\\STR_MAIN\\\\MASSFRAC\\\\MIXED\\\\MEA"
                ],
                "type": "double",
                "default": 0.0340393925,
                "description": "rich solvent output mea mass fraction ",
                "units": ""
            },
            "solvent.output.rich-01.mea": {
                "path": [
                    "\\\\Data\\\\Streams\\\\RICH-01\\\\Output\\\\STR_MAIN\\\\MASSFRAC\\\\MIXED\\\\H20"
                ],
                "type": "double",
                "default": 0.631810932,
                "description": "rich solvent output H2O mass fraction",
                "units": ""
            },
            "solvent.output.rich-01.mea": {
                "path": [
                    "\\\\Data\\\\Streams\\\\RICH-01\\\\Output\\\\STR_MAIN\\\\MASSFRAC\\\\MIXED\\\\CO2"
                ],
                "type": "double",
                "default": 2.24997645E-05,
                "description": "rich solvent output CO2 mass fraction ",
                "units": ""
            }
        }
    }

