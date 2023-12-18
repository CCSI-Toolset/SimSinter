Understanding SimSinter
=======================

Steady State Simulation
-----------------------

The majority of Simulations run on SimSinter are Steady State
simulations. Steady State simulations do not have a time component. They
simply simulate the simple ideal case of the reactor running at
equilibrium according to the provided constants and input variables.
Aspen Plus and Excel can only do Steady State simulation. ACM supports
both Steady State and Dynamic simulation. gPROMS supports both Steady
State and Dynamic simulation, but SimSinter can currently only perform
Steady State simulations with gPROMS.

Steady State simulations have a single set of inputs and outputs.
SimSinter sets the inputs before the simulation starts, and collects the
outputs when the simulation completes and returns them in the output
file.

Dynamic Simulation
------------------

**Overview**

Dynamic simulation is much more complex because it involves time.
Dynamic simulation is used to see how the reactor responds to changes
over time, and can be used to simulate conditions such as starting up,
shutting down, faults in the system, changes in fuel mix, etc. The most
important component of the Dynamic simulation is therefore the
“TimeSeries” which lists the time steps the simulation will go through.

| Dynamic simulation is currently only supported by ACM, and the Dynamic
  simulation features were designed to meet the needs of the DR-M
  builder. The dynamic features are general enough that other projects
  may find the dynamic simulator useful, but more development may be
  required. Please send an
| e-mail to ccsi-support@acceleratecarboncapture.org with any requests
  for improvements or bugs.

**TimeSeries**

The Dynamic simulation moves through time in accordance with the
TimeSeries. The TimeSeries is an array of doubles, where each double
represents the time that the time step will END (and the next one will
begin). The dynamic output variables are read from the simulator at
these breaks, and the input variables are set.

+------+-----------+-----------+-----------+-----------+-----------+
| Time | S         | T         | T         | T         | T         |
|      | imulation | imeSeries | imeSeries | imeSeries | imeSeries |
|      | Start     | Time 1    | Time 2    | Time 3    | Time 4    |
+======+===========+===========+===========+===========+===========+
| I    | Input 1   | Input 2   | Input 3   | Input 4   | --        |
| nput | set       | set       | set       | set       |           |
+------+-----------+-----------+-----------+-----------+-----------+
| Ou   | --        | Output 1  | Output 2  | Output 3  | Output 4  |
| tput |           | read      | read      | read      | read      |
+------+-----------+-----------+-----------+-----------+-----------+

**Snapshot**

Snapshots are a feature of ACM that allow Dynamic simulations to be
restarted from a saved time and condition. For example, a user may want
to run through a fault scenario multiple times with slightly different
starting conditions. Saving a snapshot just before the fault scenario
allows this to be done efficiently.

To use snapshots from SimSinter, a user must be careful to do three
things correctly:

1. Set the Snapshot “input” setting to the name of the snapshot to start
   from.

2. Set the “TimeSeries” such that the first value is strictly greater
   than the snapshot time, and the values monotonically.

3. The simulation must be distributed with the “AM\_???” subdirectory
   created by ACM, containing any .bak files found there. ACM stores the
   snapshots in those .bak files, so if they are not included, ACM will
   not be able to restore the snapshot.

**Variables in Dynamic Simulations**

Dynamic simulations have four kinds of variables.

- **Steady State Input Variables** are
  functionally equivalent to the input variables of Steady State
  simulations. They have a single input value that is set at the
  beginning of the run and is never changed. Actually, that value is
  reinserted at each time step break, so if the value changes in the
  simulation it will be reset back to the initial value at every time
  step.
- **Steady State Output Variables** 
  are functionally equivalent to the output variables of Steady State
  simulations. Only the value found at the last time step is returned
  in the output data. Steady State output variables are mostly only
  useful for statistical data in Dynamic simulations.
- **Dynamic Input Variables** have values 
  that change at each time step. Internally Dynamic input variables
  are represented in SimSinter by an array of the same length as the
  TimeSeries array. At each time step the input variable in the
  simulation is updated to the value ad that address in the array.
  Currently there is no fine control of ramping the values up and down
  included in SimSinter. ACM has internal controls for handing the change
  in input values that may be modified by the user.
  Dynamic input variables also only have a single default value for the
  whole input array. Therefore, a dynamic scalar only has a single default
  value (e.g., 5) although the input data is represented as an array. As a
  result, if the user does not provide a Dynamic input variable (as an
  array) in the input data, that variable will hold its single default
  value throughout the run as if it was a Steady State variable.

- **Dynamic Output Variables** return the
  value of the simulation variable at the end of each time step.
  Internally Dynamic output variables are represented in SimSinter by
  an array of the same length as the TimeSeries array. At the end of
  each time step the output variable in the simulation is read and
  entered into the correct address in the array of the Dynamic output
  variable in SimSinter.

**Data Layout**

The most confusing thing about Dynamic simulation is how the various
pieces of data are split between the configuration file and the input
file in practice.

-  **TimeSeries** – The configuration file may contain a default
   TimeSeries as a 1 dimensional (1-D) vector of doubles. However, it
   does not have to. DR-M Builder never uses the default TimeSeries, so
   in most cases a valid TimeSeries default does not need to be provided
   in the configuration file.

-  **Snapshot** – Similarly, the configuration file also contains a
   default snapshot name. This can also be overridden in the input file,
   but DR-M builder rarely bothers to use Snapshots at all.

-  **Dynamic Input Variables** – In the configuration file Dynamic input
   variables have their own section (dynamic-inputs) but otherwise look
   the same as normal inputs, they have the same data and meta-data in
   the same layout. The default of a dynamic scalar double is just a
   single double, **not** a vector.
   In the input file there are no separate sections for Dynamic and Steady
   State variables, they are all in the same section. However, Dynamic
   input variables have the time dimension, so a dynamic scalar is
   represented as an array of doubles in the input file, and a dynamic
   vector is represented as a 2-D matrix of doubles.

-  **Dynamic Output Variables** – In the configuration file Dynamic
   output variables have their own section (dynamic-outputs) but
   otherwise look the same as normal outputs. They have the same data
   and meta-data in the same layout.
   Dynamic outputs do not appear at all in the input file, but in the
   output file there is no separation between Dynamic and Steady State
   variables, they are all in the same section. However, Dynamic output
   variables have the time dimension, so a dynamic scalar is represented as
   an array of doubles in the output file, and a dynamic vector is
   represented as a 2-D matrix of doubles.
