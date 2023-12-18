Tutorials
=========

This section consists of a series of tutorials for every step of
configuring gPROMS and SimSinter to work together. All the tutorials are
required the have a working simulation run. They are divided up to make
it easier for the user to find the section they are interested in.

Before attempting to configure your own gPROMS simulation, it is HIGHLY
recommended that you read Section 5.0 : Usage Information. It contains
all the tips and tricks for handling issues you are likely to run into.

Configuring gPROMS to Work with SimSinter
-----------------------------------------

Description

Unlike Aspen, changes have to be made to the gPROMS simulation process
to work with SimSinter. In fact, SimSinter does not define the inputs to
the simulation, gPROMS does. On the other hand, gPROMS does not
determine the outputs, SimSinter does. This odd and counter-intuitive
situation is the result of how gPROMS gO:Run_XML is designed.

The modification to the gPROMS simulation must be done by a developer
with an intimate understanding of the simulation, usually the simulation
writer. In some cases additional variables may need to be added to
handle an extra step between taking the input and inserting it into the
variable where gPROMS will use the data.

1. Open the gPROMS simulation file (ends in “.gPJ”) in ModelBuilder 4.0
   or newer. For this example the gPROMS install test file
   “BufferTank_FO.gPJ” is used. Double-clicking the “.gPJ” file will
   open ModelBuilder.

|image6|

Figure : Opening the example file.

3. This simulation was originally a simple BufferTank simulation.
   However, it was modified into an example of all the different kinds
   of variables a user can pass in to gPROMS via SimSinter. Therefore,
   it has a lot of extra variables that do not really do anything, with
   very generic names, like “SingleInt.”

The simulation consists of a single model, BufferTank, that contains all
the simulation logic, and most of the parameter and variable
declarations.

The SimSinter simulation will change some of these PARAMETERS and
VARIABLES to change the output of the simulation.

|image7|

Figure : The BufferTank model.

4. The example file contains two processes. SimSinter can only run
   gPROMS Processes, so any gPROMS simulation must be driven from a
   process.

SimulateTank is the original BufferTank example with hardcoded values,
SimulateTank_Sinter contains the example of setting values with Sinter.
The SimulateTank_Sinter example will be recreated in this tutorial.

|image8|

Figure : SimulateTank example process.

5. Copy the existing hard-coded process “SimulateTank.”

|image9|

Figure : Right-click and copy the original process.

6. Right-click “Processes” and then select “Paste” to create a new
   process.

|image10|

Figure : Making a new process.

7. The new process will be named “SimulateTank_1.” Rename the process to
   “SimulateTank_tutorial” by right-clicking “SimulateTank_1” and then
   selecting “Rename.”

|image11|

Figure : Rename the process to something useful.

8. Open the new “SimulateTank_tutorial” process. It has the same
   hard-coded values as “SimulateTank.”

|image12|

Figure : SimulateTank_tutorial process before any changes.

9. Add a FOREIGN_OBJECT, named “FO,” in the PARAMETER section.

Set that FOREIGN_OBJECT to “SimpleEventFOI::dummy” in the SET section.

This FOREIGN_OBJECT is how the user gets inputs from SimSinter.

|image13|

Figure : SinterConfigGUI Variable Configuration window, Preview Variable
frame.

10. This particular simulation has a large number of pointless input
    variables simply to demonstrate how to set different types. These
    are named based on their type. Any variable named similarly to
    “SingleInt” or “ArraySelector” can be safely ignored for this
    tutorial. For a full list of the methods for setting different types
    see Section 5.2 gPROMS Input Variable Assignment Types.

Any variable in the simulation can be an input, whether it is defined in
the process or one of the models referenced by the process, or in a
model referenced by a model, etc.

All inputs take their values from the FOREIGN_OBJECT the user defined, a
period, the type name, two underscores, the input variable name, an open
parenthesis, an optional index variable (for arrays), a close
parenthesis, and a semicolon.

For a scalar:

FO.<Type>\_\_<InputName>();

SimSinter can only handle arrays inputted in FOR loops such as:

FOR ii := 1 TO <array size> DO

<ArrayName>(ii) := FO.<Type>1\_\_<InputName>(ii);

END

For this example the user needs to set “T101.Alpha” in “PARAMETER,”
“T101.FlowIn” in “ASSIGN,” and “T101.Height” in “INITIAL.”

|image14|

Figure : SimulateTank_tutorial with all inputs set.

11. | Set gPLOT := ON in the SOLUTIONPARAMETERS section
    | Finally, SimSinter will be unable to get any outputs from
      gO:Run_XML if gPLOT is not set ON in the SOLUTIONPARAMETERS
      section. See Figure 10.
    | |image15|

Figure : gPLOT must be ON in the SOLUTIONPARAMETERS

12. Test the “SimulateTank_tutorial” by selecting it and then clicking
    the “green Simulate triangle”. When the simulation runs it will ask
    for every input variable the user has defined.

For the example variables that do not effect the simulation, such as
“SingleInt”, any valid value is acceptable.

For the values that do effect the simulation, these values work:

-  REAL\__AlphaFO = .08

-  REAL\__FlowInFO = 14

-  REAL\__HeightFO = 7.5

|image16|

Figure : Test edits to the SimulateTank_tutorial process.

Exporting an Encrypted Simulation to Run with SimSinter
-------------------------------------------------------

SimSinter can only run encrypted gPROMS simulations. These files have
the .gENCRYPT extension. If the user’s additions to the simulation for
reading input variables ran correctly in the previous section, that
process is ready to be exported for use by SimSinter.

1. Right click the “process” to export (“SimulateTank_tutorial”) and
   then select “Export.”

|image17|

Figure : Select “Export.”

13. In the resulting Export window, select “Encrypted input file for
    simulation by gO:RUN” and then click “OK.”

|image18|

Figure : Select “Encrypted input file” and then click “OK.”

14. On the second window, be sure to set the “Export directory” to a
    directory the user can find. Preferably one without any other files
    in it so the user would not be confused by the output.

If the “Input file name” or “Encryption password” are not changed,
SimSinter will be able to guess the password. If either of these values
are changed, the user will have to set the correct password in the
SinterConfigGUI password setting.

A Decryption password is probably uncessecary, as the user has the
original file. SimSinter does not use it.

When finished setting up these fields, click “Export Project.”

|image19|

Figure : Export entity window.

15. | The resulting .gENCRYPT file is saved to a subdirectory named
      “input” in the save directory specified in Step 3. The first part
      of the name is identical to the .gPJ filename. The user should not
      rename the file because the SinterConfig file will guess this
      name, and currently changing it requires editing the SinterConfig
      file.
    | We recommend that you copy the .gENCRYPT file up to the same
      directory as the .gPJ file, so that FOQUS can find it.

Configuring SimSinter to Work with gPROMS
-----------------------------------------

A SimSinter configuration file must also be produced to tell SimSinter
how to run the gPROMS simulation.

1. Open the “SinterConfigGUI” from the “Start” menu, as shown in Figure
   14.

|image20|

Figure : Start menu, SinterConfigGUI.

16. Initially the SimSinter Configuration File Builder splash screen
    displays, as shown in Figure 15. Either click the “splash screen” to
    proceed, or wait 10 seconds for the screen to close automatically.

|image21|

Figure : SimSinter Configuration File Builder splash screen.

17. The SinterConfigGUI Open Simulation window displays as shown in
    Figure 16. Click “Browse” to select the file to open and then click
    “Open File and Configure Variables” to open the file.

SinterConfigGUI **cannot** read the .gENCRYPT file that is actually run
by SimSinter. Instead, the user must open the .gPJ file the ModelBuilder
uses.

In this case use the file configured in the 4.1 Configuring gPROMS to
Work with SimSinter tutorial. Or the example may be found at:
C:\\SimSinterFiles\\gPROMS_Test\\BufferTank_FO.gPJ.

|image22|

Figure : SinterConfigGUI Open Simulation window.

18. The SinterConfigGUI Simulation Meta-Data window displays as shown in
    Figure 17. Unlike the other simulations, gPROMS has not started up
    in any way. SinterConfigGUI does not get information from gPROMS
    directly, it must parse the .gPJ file instead.

19. | The first and most important piece of meta-data is the “SimSinter
      Save Location” at the top of the window. This is where the Sinter
      configuration file is saved. The system suggests a file location
      and name. The user should confirm this is the intended location of
      the files to not accidently overwrite other files.
    | Futhermore, the configuration file autosaves when “Next >” is
      clicked, so please ensure that the filename is correct, and will
      not overwrite any important files.

|image23|

Figure : SimSinter Save Location.

20. | SimSinter cannot enforce version constraints on gPROMS, so there
      is no point in setting them, except as a method of informing the
      user.
    | Some simulations have additional files they require to run, but
      this simulation does not, so a full tutorial will not be given
      here. For more information see the Dynamic ACM simulation section
      **Error! Reference source not found.** in the SimSinter Technical
      Manual.
    | If any additional files are required, they may be attached to the
      simulation via the Input Files section. The simulation file itself
      is always included in the Input Files, and cannot be removed.
    | |image24|

Figure : Additional simulation files may be attached here

21. The SinterConfigGUI Variable Configuration Page window displays as
    shown in Figure 19. gPROMS has two settings, ProcessName and
    password. SimSinter has guessed at both the ProcessName and the
    password. For this example the password is correct, but the
    ProcessName is incorrect. SimulateTank is the process that is not
    configured to work with SimSinter.

On the left side is the Variable Tree. The root is connected to the
three processes defined in this .gPJ file.

Change the “ProcessName” to “SimulateTank_tutorial.”

|image25|

Figure : SinterConfigGUI Variable Configuration Page window.

22. When the user changes the ProcessName and presses “Enter” (or click
    away), the user will see all of the available input variables. This
    is because the input variables have been configured in gPROMS, and
    SimSinter has parsed them out of the .gPJ file, as long as the user
    has the ProcessName set correctly. This also means that the user
    cannot add new input variables in SinterConfigGUI, only in gPROMS.

SimSinter also does its best to identify the Default values, min, and
max of the variables.

The Default can only be calculated from the file if it was defined
purely in terms of actual numbers. SimSinter cannot evaluate other
variables or functions. So “DEFAULT 2 \* 3.1415 \* 12” will work. But
“DEFAULT 2 \* PI \* radius” will not work, and SimSinter will just set
the default to “0.”

Min and max values are taken from the variable Type, if there is one.

|image26|

Figure : SinterConfigGUI automatically discovers input variables from
gPROMS.

23. The output values can now be entered. Expand the
    “SimulateTank_tutorial” process on the tree, and then expand the
    “T101” model. Double-click “FlowOut” to make it the preview
    variable.

Notice that the “Make Input” button is unavailable. As stated above, the
user cannot make new input variables in SinterConfigGUI. Only “Make
Output” is allowed.

|image27|

Figure : Preview the SimulateTank_tutorial.T101.FlowOut variable.

24. Click “Make Output,” FlowOut is made an output variable as shown in
    Figure 22.

The user can update the description, but SimSinter made a good guess
this time, so there is not any need to.

|image28|

Figure : FlowOut as an output variable.

25. By the same method, make output variables “HoldUp” and “Height.”

|image29|

Figure : Additional output variables.

26. The variables names should be made shorter. Simply click the “name
    column” and then change the name to a preferred name.

|image30|

Figure : Changed the names of the output variables.

27. For future testing, make sure the defaults are good values. The only
    three input variables that matter have the following defaults:

-  AlphaFO: 0.8

-  FlowInFO: 14

-  HeightFO: 7.5

|image31|

Figure : Set defaults for input variables.

28. When finished making output variables, click “Next” at the bottom of
    the variables window.

If there were any input vectors, the Vector Default Initialization
window displays. Here the default values of the vectors can be edited.

|image32|

Figure : Vector Default Initialization window.

29. Click “Finish” to save the configuration file and then close
    SimSinter.

Running gPROMS Simulations with SimSinter
-----------------------------------------

After configuring gPROMS, exporting a .gENCRYPT file, and creating a
SinterConfig file, this should be the state of the gPROMS simulation
directory:

|image33|

Figure : Simulation directory before configuring for runs.

1. The .gENCRYPT file is in the input directory. Move it up to the same
   level as the SinterConfig file. After which the user may delete the
   input directory.

|image34|

Figure : Copy the .gENCRYPT file from the input directory.

|image35|

| Figure : Copy the .gENCRYPT file up to the SimSinter directory.
| The input directory may then be deleted.

30. Figure 29 contains three files. To run the simulation only the .json
    and .gENCRYPT files are required, but to configure the simulation or
    change it, the .gPJ file is required.

If the user wishes to run the simulation on Turbine, simply upload the
.json and .gENCRYPT files there. The .gPJ file may also be included for
archival and documentation purposes, but it is not required.

If the user wishes to test the simulation locally first, continue the
tutorial.

31. To run the simulation locally, a set of inputs is needed to be
    passed in. These inputs can be generated with the DefaultBuilder.exe
    program included in the SimSinter distribution.

DefaultBuilder.exe is run from the Windows command line. Open a command
line window by clicking “Start” or the “Windows Key” and then typing
“cmd.”

|image36|

Figure : Open the Start menu, type “cmd,” and then press “Enter.”

32. Change the directory to the user’s simulation directory. If the user
    is using the SimSinterFiles test directories, the command is:

cd c:\\SimSinterFiles\\gPROMS_Test

|image37|

Figure : Change the directory to the user’s simulation directory.

33. On the “command line” run the DefaultBuilder. It takes two
    arguments:

    a. The filename of the SinterConfig File.

    b. The output filename for the defaults file. Give this a nice
       descriptive name.

Here is an example:

c:\\Program Files (x86)\\CCSI\\SimSinter\\DefaultBuilder.exe
BufferTank_FO.json BufferTank_inputs.json

|image38|

Figure : Running the DefaultBuilder.

34. Observe in Windows explorer that the defaults/inputs file has been
    generated. This file may be edited in Notepad to change the values
    of the inputs and run different configurations. But for this test it
    is better to run with the defaults to avoid possible errors.

|image39|

Figure : The BufferTank_inputs.json file has been created.

35. SimSinter can be run from the command line with the new input file.

ConsoleSinter takes three arguments:

a. The SinterConfig file

b. The inputs file (here the defaults that were generated are used)

c. A file for the SimSinter outputs

The command is:

c:\\Program Files (x86)\\CCSI\\SimSinter\\ConsoleSinter.exe
BufferTank_FO.json BufferTank_inputs.json BufferTank_outputs.json

If the simulation runs properly, the outputs will be very uninteresting.
If there is an error there will be a much longer more complex message
output.

|image40|

Figure : Running ConsoleSinter.

36. To double check that the simulation ran correctly, look at the
    Sinter outputs in Notepad:

notepad BufferTank_outputs.json

|image41|

Figure : Use Notepad to read the Sinter output file.

37. Scroll down to the “outputs” section. The values should be:

-  FlowOut: 3.02714

-  HoldUp: 14318.1

-  Height: 14.3181

|image42|

Figure : The outputs from running BufferTank_FO with default inputs.

.. |image6| image:: ./media/image13.png
   :width: 6.5in
   :height: 1.78681in
.. |image7| image:: ./media/image14.png
   :width: 6.5in
   :height: 4.96528in
.. |image8| image:: ./media/image15.png
   :width: 6.10417in
   :height: 4.6629in
.. |image9| image:: ./media/image16.png
   :width: 6.03233in
   :height: 3.70833in
.. |image10| image:: ./media/image17.png
   :width: 6.05208in
   :height: 3.72048in
.. |image11| image:: ./media/image18.png
   :width: 2.79167in
   :height: 1.26042in
.. |image12| image:: ./media/image19.png
   :width: 5.89583in
   :height: 3.62443in
.. |image13| image:: ./media/image20.png
   :width: 6.08859in
   :height: 3.74292in
.. |image14| image:: ./media/image21.png
   :width: 6.5in
   :height: 4.13958in
.. |image15| image:: ./media/image22.png
   :width: 3.79861in
   :height: 0.72917in
.. |image16| image:: ./media/image23.png
   :width: 6.08946in
   :height: 3.87813in
.. |image17| image:: ./media/image24.png
   :width: 6.04775in
   :height: 3.85157in
.. |image18| image:: ./media/image25.png
   :width: 4.33333in
   :height: 4.5625in
.. |image19| image:: ./media/image26.png
   :width: 5.9375in
   :height: 3.27719in
.. |image20| image:: ./media/image27.png
   :width: 3.21695in
   :height: 3.4in
.. |image21| image:: ./media/image28.png
   :width: 2.92066in
   :height: 2.33653in
.. |image22| image:: ./media/image29.png
   :width: 5.80249in
   :height: 3.98921in
.. |image23| image:: ./media/image30.png
   :width: 5.63889in
   :height: 3.87787in
.. |image24| image:: ./media/image31.png
   :width: 6.49306in
   :height: 4.46528in
.. |image25| image:: ./media/image32.png
   :width: 6.03125in
   :height: 4.14648in
.. |image26| image:: ./media/image33.png
   :width: 5.96875in
   :height: 4.10352in
.. |image27| image:: ./media/image34.png
   :width: 6.08333in
   :height: 4.18229in
.. |image28| image:: ./media/image35.png
   :width: 5.96875in
   :height: 4.10352in
.. |image29| image:: ./media/image36.png
   :width: 6.07667in
   :height: 4.17771in
.. |image30| image:: ./media/image37.png
   :width: 6.13636in
   :height: 4.21875in
.. |image31| image:: ./media/image38.png
   :width: 5.90625in
   :height: 4.06055in
.. |image32| image:: ./media/image39.png
   :width: 6.5in
   :height: 1.31111in
.. |image33| image:: ./media/image40.png
   :width: 6.5in
   :height: 2.04514in
.. |image34| image:: ./media/image41.png
   :width: 6.5in
   :height: 2.04514in
.. |image35| image:: ./media/image42.png
   :width: 6.5in
   :height: 2.04514in
.. |image36| image:: ./media/image43.png
   :width: 3.80208in
   :height: 4.82643in
.. |image37| image:: ./media/image44.png
   :width: 5.93085in
   :height: 2.99584in
.. |image38| image:: ./media/image45.png
   :width: 5.74112in
   :height: 2.9in
.. |image39| image:: ./media/image46.png
   :width: 6.5in
   :height: 2.04514in
.. |image40| image:: ./media/image47.png
   :width: 6.21178in
   :height: 1.48658in
.. |image41| image:: ./media/image48.png
   :width: 6in
   :height: 2.39295in
.. |image42| image:: ./media/image49.png
   :width: 6.15625in
   :height: 4.19792in