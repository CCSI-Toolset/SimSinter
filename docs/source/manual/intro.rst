Introduction
============

SimSinter is a standard interface library for driving single-process
Windows\ :sup:`®`-based simulation software. SimSinter has been tested
on:

-  Aspen Plus\ :sup:`®` 12.x and 14.x

-  Aspen Custom Modeler\ :sup:`®` (ACM) 12.x and 14.x,

-  Microsoft\ :sup:`®` Excel\ :sup:`®` 2021

When called, SimSinter can open the simulator, initialize the
simulation, set variables in the simulation, run the simulation, and get
the resulting output variables from the simulation. SimSinter is an
integral part of the Turbine Gateway and every other Carbon Capture
Simulation Initiative (CCSI) tool that runs process simulations.

Motivating Example
------------------

Aspen Plus runs a single process simulation by itself just fine, so why
is SimSinter necessary? SimSinter is most useful for running sets of
simulations designed by some other software.

A common use case is parameter studies for uncertainly quantification
(UQ). A new carbon capture process simulation was designed where the
reaction parameters for carbon capture and release were determined
experimentally. Those experiments provided bounds for the reaction
parameters, but how the process would perform with in that range still
had to be determined.

A set of 40,000 runs was designed in the PSUADE Uncertainty
Quantification tool. Using TurbineClient/PSUADESinter the 40,000 jobs
were sent to the Gateway. The Gateway launched 100 ACM instances via
SimSinter and ran all 40,000 jobs over three days. The results of the
simulation were retreived from the Gateway with FOQUS. Those results
were loaded back into PSUADE. With PSUADE the effectiveness of the
simulation and under what conditions the simulation would have problems
were analyzed.

Features List
-------------

SimSinter can interface to the following simulators:

1. Aspen Plus

2. ACM (Steady State, Dynamic, and Optimization run modes)

3. Microsoft Excel

SimSinter can be called from the following interfaces:

1. The Turbine Gateway (or a standalone Gateway). The Gateway can be
   called either from the Framework for Optimization and Quantification
   of Uncertainty and Sensitivity (FOQUS) or TurbineClient.
   TurbineClient can take JSON, PSUADE, or CSV (Comma Separated Values,
   a common spreadsheet format) files.

5. Microsoft Excel. The SimSinter installation includes a spreadsheet
   that can be used with any Sinter configuration file to perform a
   single run or series of runs on the user’s workstation.

6. The standalone tools included with the SimSinter installation such as
   ConsoleSinter, which can run a single run or series of runs on the
   user’s workstation.

SimSinter also installs the following helper tools:

-  SinterConfigGUI: Used to generate JSON format configuration files by
   enabling the user to interface with Aspen or Excel.

-  ConsoleSinter: Takes JSON format inputs to perform a run or series of
   runs locally.

-  CSVConsoleSinter: Takes .csv to perform a series of runs locally.

-  DefaultBuilder: Generates a JSON format file of defaults, pulled from
   the current values of the inputs in the simulation file. (This file
   is useful for generating some example inputs for running
   ConsoleSinter.)

Overview: What a User Needs to Know About SimSinter and this Manual
-------------------------------------------------------------------

SimSinter itself must be run on a machine that has the simulator and all
of the necessary licenses installed. For example, SimSinter could run
Aspen Plus simulations on a desktop computer that has Aspen Plus
installed and the necessary Aspen Plus licenses. This includes
configuring simulations with the SinterConfigGUI. SinterConfigGUI
interfaces with the simulator to interrogate the simulation. So
SimSinter and the simulator must exist on the same machine.

However, a user that does not have a simulator license may still use
SimSinter if they have access to a remote computer that has the
simulator installed, and also has the Turbine Science Gateway installed.
In this case, the user can send their simulations to the Gateway, and
the Gateway computer runs SimSinter and the simulator. (Or, it may, farm
the actually running of SimSinter and the simulator off to another
machine that has the licenses.)

There are two ways a user can run SimSinter:

1. Remotely by the Turbine Gateway or by a standalone Gateway
   installation.

2. Locally (on the same machine the user is using) by Microsoft Excel or
   by one of the tools included with the SimSinter installation.

For the remainder of this manual it is assumed that the user is running
SimSinter locally. Special cases for the Gateway are documented as
needed.

To drive a simulation SimSinter requires at least two files, which
should be in the same directory.

1. The simulation file to run SimSinter. This file is simulator
   specific. The file defines the simulation for the simulator. For
   example, for Aspen Plus this file can be a .bkp or .apw file.

2. The Sinter configuration file is a JSON file that gives meta-data
   about the simulation. Including all the input and output variables
   the simulation writer thinks the user might find useful, including
   name, type, defaults, units, and possible minimum and maximum values.

For some simulations additional files are required, for example, some
ACM simulations have a snapshot file, or additional simulator
functionality contained in a DLL. The files should be listed in the
SimSinter configuration file so that Turbine and the Data Management
Framework are aware of them, and can place them in the correct
directories for running the simulation.

SimSinter produces inputs and outputs in a simple JSON format. JSON is
easy for programs to parse and manipulate, but it is not easy to read,
nor is it used by most scientific tools. Furthermore, even though
SimSinter only writes the output variables requested in the Sinter
configuration file, the user is usually only interested in a small
subset of those variables.

Therefore, aside from the Excel SimSinter Interface, there are tools for
converting the outputs to two other useful formats: PSUADE format and
CSV format. A user can perform sets of runs in CSV format directly with
the included tool CSVConsoleSinter. Otherwise, refer to the
documentation for TurbineClient, which includes tools for converting to
and from PSUADE and CSV format, as well as doing Gateway runs with those
formats.
