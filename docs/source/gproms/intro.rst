Introduction
============

This document is a supplement to the SimSinter Technical Manual
specifically covering the gPROMS simulator. The document assumes that
the reader has read the SimSinter Technical Manual. This additional
document was written because gPROMS is significantly different from the
other simulators SimSinter supports, and the workflow is significantly
different.

**Note** SimSinter gPROMS support has not been tested for versions 3.0+

Motivating Example
------------------

gPROMS provides tools for doing batch runs of large numbers of gPROMS
simulations, in the form of gO:Run and gO:Run_XML, so why is SimSinter
necessary for gPROMS?

In fact, SimSinter uses the gO:Run_XML tool provided by PSE for running
batches of simulations, but SimSinter provides two additional benefits.

1. SimSinter does simplify the process of running jobs compared to the
   provided PSE tools. Users do not have to edit or generate their own
   XML files, for example.

2. SimSinter provides the same interface to gPROMS, Aspen
   Plus\ :sup:`®`, and Aspen Custom Modeler\ :sup:`®` (ACM). This allows
   users to continue using the same tools across all simulators, and to
   use the Framework for Optimization and Quantification of Uncertainty
   and Sensitivity (FOQUS) tool for statistical studies and uncertainty
   quantification (UQ).

The utility of being able to use multiple simulators with the same tools
is represented by the current carbon-capture amine-based adsorber and
regenerator modelling projects. The carbon-capture system was originally
modeled in ACM. Some issues were identified in the ACM version of the
simulation. Now that the simulations have been ported to gPROMS the same
statistical analyses on both simulators can be run to help uncover
problems and increase simulation fidelity.

Software Requirements for Using SimSinter with gPROMS
-----------------------------------------------------

SimSinter requires gPROMS 4.0 or newer.

To configure a gPROMS simulation to work with SimSinter, the user must
have PSE ModelBuilder 4.0.0 or newer.

To run gPROMS simulations, the simulation machine must have licenses for
gO:Run_XML, 4.0 or newer.

However, to configure SimSinter with SinterConfigGUI, no gPROMS licenses
of any kind are required, because SinterConfigGUI does not communicate
with any gPROMS program, it parses the .gPJ file itself.

What a User Needs to Know About Using gPROMS with SimSinter
-----------------------------------------------------------

gPROMS and SimSinter interact very differently than SimSinter and Aspen.
SimSinter directly communicates with the running Aspen simulation, and
can therefore get information on every variable in the simulation at
will. SimSinter cannot do that with gPROMS, instead, SimSinter must
communicate with gPROMS by reading and writing files. This makes the
interaction more complex for both gPROMS and SimSinter. gPROMS must be
configured to expect input from SimSinter, and SinterConfigGUI must read
the gPROMS .gPJ file to find out what inputs to provide. This extra
complexity is the reason for this document.

First, the gPROMS simulation must be configured to accept inputs from
SimSinter. This is done with the gPROMS FOREIGN_OBJECT interface. The
input variables and parameters are defined to accept input from the
FOREIGN_OBJECT. SinterConfigGUI can then read the gPROMS .gPJ file, and
discover the inputs by finding the parameters and variables that accept
inputs from the FOREIGN_OBJECT.

SimSinter uses gPROMS gO:Run_XML to actually run the gPROMS simulation.
gO:Run_XML accepts inputs as an XML file, and then provides those inputs
to the correct variables via the FOREIGN_OBJECT interface. The results
are passed out via another XML file for SimSinter to read.

It is HIGHLY recommended that the user read section 5.0 USAGE
Information before attempting to configure their own simulation.
