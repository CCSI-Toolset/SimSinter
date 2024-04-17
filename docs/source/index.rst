Contents
========

.. toctree::
   :maxdepth: 2

   install/index
   manual/index

SimSinter
=========

Overview
--------

SimSinter is a standard interface library for driving single-process
Windows\ :sup:`®`-based simulation software. SimSinter supports Aspen
Plus\ :sup:`®`, Aspen Custom Modeler\ :sup:`®` (ACM), and
Microsoft\ :sup:`®` Excel\ :sup:`®`. Additional simulators are planned
for future releases. When called, SimSinter can open the simulator,
initialize the simulation, set variables in the simulation, run the
simulation, and get the resulting output variables from the simulation.
SimSinter is an integral part of the Gateway and every other CCSI tool
that runs Aspen.

SimSinter is used by the Turbine Gateway, but users may also choose to
use SimSinter directly in three other ways:

1. SimSinter can be driven from Microsoft Excel

2. SimSinter comes with multiple command line tools for running jobs,
   getting data from simulators, and debugging.

3. SimSinter comes with a GUI for generating the Sinter Config files.

Further documentation about how to use SimSinter is available in the
SimSinter User Manual.

In order to drive a simulation SimSinter requires two files:

1. The simulation file to run. The simulation file is simulator
   specific. It defines the simulation for the simulator. For example,
   for AspenPlus this file may be a .bkp or .apw file.

2. The sinter configuration file. This file gives meta-data about all
   the input and output variables the simulation writer thinks the user
   might find useful, including name, type, defaults, units, and
   possible min and max values. This file is in JSON format.



.. include:: contact.rst
