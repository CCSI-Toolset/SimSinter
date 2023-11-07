|image1|\ |image2|\ |image3|\ |image4|\ |image5|\ |New_DOE_Logo_Color_042808|

|CCSI_color_CS3_TM_300dpi.png|

SimSinter INSTALLATION GUIDE

Version 3.0.0

October 2023

Copyright (c) 2012 - 2023

**Copyright Notice**

SimSinter was produced under the DOE Carbon Capture Simulation
Initiative (CCSI), and is copyright (c) 2012 - 2023 by the software
owners: Oak Ridge Institute for Science and Education (ORISE), TRIAD
National Security, LLC., Lawrence Livermore National Security, LLC., The
Regents of the University of California, through Lawrence Berkeley
National Laboratory, Battelle Memorial Institute, Pacific Northwest
Division through Pacific Northwest National Laboratory, Carnegie Mellon
University, West Virginia University, Boston University, the Trustees of
Princeton University, The University of Texas at Austin, URS Energy &
Construction, Inc., et al.. All rights reserved.

NOTICE. This Software was developed under funding from the U.S.
Department of Energy and the U.S. Government consequently retains
certain rights. As such, the U.S. Government has been granted for itself
and others acting on its behalf a paid-up, nonexclusive, irrevocable,
worldwide license in the Software to reproduce, distribute copies to the
public, prepare derivative works, and perform publicly and display
publicly, and to permit other to do so.

**License Agreement**

SimSinter Copyright (c) 2012 - 2023, by the software owners: Oak Ridge
Institute for Science and Education (ORISE), TRIAD National Security,
LLC., Lawrence Livermore National Security, LLC., The Regents of the
University of California, through Lawrence Berkeley National Laboratory,
Battelle Memorial Institute, Pacific Northwest Division through Pacific
Northwest National Laboratory, Carnegie Mellon University, West Virginia
University, Boston University, the Trustees of Princeton University, The
University of Texas at Austin, URS Energy & Construction, Inc., et al.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.

3. Neither the name of the Carbon Capture Simulation Initiative, U.S.
   Dept. of Energy, the National Energy Technology Laboratory, Oak Ridge
   Institute for Science and Education (ORISE), TRIAD National Security,
   LLC., Lawrence Livermore National Security, LLC., the University of
   California, Lawrence Berkeley National Laboratory, Battelle Memorial
   Institute, Pacific Northwest National Laboratory, Carnegie Mellon
   University, West Virginia University, Boston University, the Trustees
   of Princeton University, the University of Texas at Austin, URS
   Energy & Construction, Inc., nor the names of its contributors may be
   used to endorse or promote products derived from this software
   without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER
OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

You are under no obligation whatsoever to provide any bug fixes,
patches, or upgrades to the features, functionality or performance of
the source code ("Enhancements") to anyone; however, if you choose to
make your Enhancements available either publicly, or directly to
Lawrence Berkeley National Laboratory, without imposing a separate
written license agreement for such Enhancements, then you hereby grant
the following license: a non-exclusive, royalty-free perpetual license
to install, use, modify, prepare derivative works, incorporate into
other computer software, distribute, and sublicense such enhancements or
derivative works thereof, in binary and source code form. This material
was produced under the DOE Carbon Capture Simulation.

Table of Contents
=================

`1. Introduction <#introduction>`__ `1-1 <#introduction>`__

`2. Prerequisites <#prerequisites>`__ `2-1 <#prerequisites>`__

`3. Basic Installation <#basic-installation>`__
`3-1 <#basic-installation>`__

`3.1. Third Party Software
Installation <#third-party-software-installation>`__
`3-1 <#third-party-software-installation>`__

`3.2. Product Installation <#product-installation>`__
`3-1 <#product-installation>`__

`4. Installation Test <#installation-test>`__
`4-2 <#installation-test>`__

`4.1. Opening a Simulation with
SinterConfigGUI <#opening-a-simulation-with-sinterconfiggui>`__
`4-2 <#opening-a-simulation-with-sinterconfiggui>`__

`5. Installation Problems <#installation-problems>`__
`5-6 <#installation-problems>`__

`5.1. Known Issues/Fixes <#known-issuesfixes>`__
`5-6 <#known-issuesfixes>`__

`5.2. Reporting Installation issues <#reporting-installation-issues>`__
`5-6 <#reporting-installation-issues>`__

`5.3. Version Log <#version-log>`__ `5-6 <#version-log>`__

Introduction 
=============

SimSinter is a standard interface library for driving single-process
Windows\ :sup:`®`-based simulation software. SimSinter supports Aspen
Plus\ :sup:`®`, Aspen Custom Modeler\ :sup:`®` (ACM), PSE gPROMS, and
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

Prerequisites
=============

SimSinter has been tested with Windows 10, Windows 11, Windows Server
2019 and Windows Server 2022.

To get any use out of SimSinter you will also need at least one
simulator to use it with. SimSinter has been tested with:

-  Aspen Plus, version 12 or newer

-  Aspen Custom Modeler (ACM), version 12 or newer

-  Microsoft Excel, 2021 or newer

Basic Installation 
===================

Third Party Software Installation
---------------------------------

In order to run a simulation, the correct simulator is also required.
SimSinter may use Aspen Plus, Aspen Custom Modeler, GPROMS, or Microsoft
Excel. Please install the appropriate simulator by following the
simulator vendor provided documentation.

Product Installation
--------------------

To install SimSinter:

1. Download SimSinterInstaller.msi from
   https://github.com/CCSI-Toolset/SimSinter/releases

2. Run SimSinterInstaller.msi

3. Click Next

4. Accept the terms of the License agreement

5. Click either the “Typical” or “Complete” button; either will install
   all of SimSinter. The Custom button may be used to not install
   certain features.

6. Click the Install button.

7. Give permission for SimSinter to install, enter administrator login
   information if necessary.

8. Click “Finish” to complete the installation.

9. SimSinter should now be installed and entered into the Windows
   registry. It should now be accessible by either Microsoft Excel or
   the command line tools.

Installation Test 
==================

Three tests are included with the SimSinter installation that will allow
testing that SimSinter has installed correctly. There is one test for
each of the three supported simulators. The tests demonstrate running
SimSinter from Microsoft Excel, so to run them you must have Microsoft
Excel installed.

To test, please choose the appropriate simulator below and follow the
instructions.

Opening a Simulation with SinterConfigGUI
-----------------------------------------

   This is a simple test to make sure that SimSinter is installed
   correctly, and can correctly open your simulation and simulator.

1. Choose a simulation to open, and make sure you have the correct
   simulator installed. For example, I will be using the Aspen Custom
   Modeler simulation included with the SimSinter install. So I should
   make sure that I have both Aspen Custom Modeler installed, and
   SimSinter, as shown in Figure 1.

..

   |image6|\ |image7|

2. Open SinterConfigGUI by selecting it from the start menu, as in
   Figure 1.

1. Initially the SimSinter Configuration File Builder splash screen
   displays, as shown in Figure 2. Either click the splash screen to
   proceed or wait 10 seconds for the screen to close automatically.

|image8|

Figure : SimSinter Splash Screen

3. | The SinterConfigGUI Open Simulation window displays as shown in
     Figure 3. Click “Browse” to select the file to open and then click
     “Open File and Configure Variables” to open the file. The user can
     either open a fresh ACM simulation (.acmf file) or an existing
     Sinter configuration file. In these instructions, a fresh
     simulation is opened.
   | It may take a few minutes after clicking the button to
     SinterConfigGUI to move on. It must open your simulator, so you
     must expect it to take at least as long as your simulator normally
     takes to open. For Aspen products that use a networked license
     server, this may take as long as a few minutes. During that
     SinterConfigGUI will remain on the Open File Page, but the
     “Attempting to Open Aspen” message will appear at the bottom of the
     window.

..

   |image9|

   Figure : SinterConfigGUI Open Simulation Window

4. Click browse and select your file. I will be opening the ACM
   demonstration file included with SimSinter in
   C:\\SimSinterFiles\\ACM_Install_Test, as in Figure 4.

|image10|

Figure : Selecting the simulation file to open

5. Click “Open File and Configure Variables”

..

   |image11|

Figure : Clicking Open File button

6. It may take a few minutes after clicking the button to
   SinterConfigGUI to move on. It must open your simulator, so you must
   expect it to take at least as long as your simulator normally takes
   to open. For Aspen products that use a networked license server, this
   may take as long as a few minutes. During that time SinterConfigGUI
   will remain on the Open Simulation window, but the “Attempting to
   Open Aspen” message will appear at the bottom of the window.

7. | The SinterConfigGUI Simulation Meta-Data window displays as shown
     in Figure 6.
   | Also, the Aspen Custom Modeler has started up in the background.
     This is so the user can observe things about the simulation in
     question as they work on the configuration file
   | If you see an error instead, please attempt to debug the issue, or
     contact CCSI support at ccsi-support@acceleratecarboncapture.org

|image12|

Figure : Meta-Data window

8. If you see the window in Figure 6, SimSinter is working properly and
   can properly open simulators. If you wish to continue this tutorial,
   and configure the simulation, please see the tutorial section of the
   SimSinter User Manual. It includes sections on configuring
   simulations for Aspen Custom Modeler, Aspen Plus, and Microsoft
   Excel.

Installation Problems
=====================

Known Issues/Fixes
------------------

There are no known installation issues.

Reporting Installation issues
-----------------------------

If any issues are found with the installation, please contact:

ccsi-support@acceleratecarboncapture.org

Version Log
-----------

+-----------------------+---------+---------+-------------------------+
| Product               | Version | Release | Description             |
|                       | Number  | Date    |                         |
+=======================+=========+=========+=========================+
| SimSinter Install     | 3.0.0   | 10/     | Updates to copyright    |
| Manual                |         | 31/2023 | and license dates,      |
|                       |         |         | update of               |
|                       |         |         | prerequisites, and      |
|                       |         |         | removal of units        |
|                       |         |         | conversion test.        |
+-----------------------+---------+---------+-------------------------+
| SimSinter Install     | 2.0.1   | 08/     | License update (no      |
| Manual                |         | 15/2019 | functional changes)     |
+-----------------------+---------+---------+-------------------------+
| SimSinter Install     | 2.0.0   | 03/     | Initial Open Source     |
| Manual                |         | 31/2018 | release                 |
+-----------------------+---------+---------+-------------------------+

.. |image1| image:: ./media/image1.png
   :width: 6.02083in
   :height: 0.78264in
.. |image2| image:: ./media/image2.png
   :width: 6.02083in
   :height: 0.78264in
.. |image3| image:: ./media/image3.png
   :width: 6.02083in
   :height: 0.78264in
.. |image4| image:: ./media/image4.png
   :width: 6.02083in
   :height: 0.78264in
.. |image5| image:: ./media/image5.png
   :width: 6.02083in
   :height: 0.78264in
.. |New_DOE_Logo_Color_042808| image:: ./media/image11.png
   :width: 1.85in
   :height: 0.46875in
.. |CCSI_color_CS3_TM_300dpi.png| image:: ./media/image12.png
   :width: 6.46892in
   :height: 2.89545in
.. |image6| image:: ./media/image13.png
   :width: 5.98611in
   :height: 2.36111in
.. |image7| image:: ./media/image14.png
   :width: 5.98611in
   :height: 2.36111in
.. |image8| image:: ./media/image17.png
   :width: 3.04286in
   :height: 2.43429in
.. |image9| image:: ./media/image18.png
   :width: 5.88806in
   :height: 4.04804in
.. |image10| image:: ./media/image19.png
   :width: 5.67473in
   :height: 3.07442in
.. |image11| image:: ./media/image20.png
   :width: 5.71972in
   :height: 3.93231in
.. |image12| image:: ./media/image21.png
   :width: 5.63368in
   :height: 3.05639in
