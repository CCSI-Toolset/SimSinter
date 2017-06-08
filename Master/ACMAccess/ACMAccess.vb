Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Diagnostics
Imports System.IO
Imports System.ComponentModel
Imports System.Threading
Imports System.Timers

'Duplicate of sinter.sinter_AppError.in ISimulation, we'd have to break that into another project to share it.
Public Class ACMAccess
    Implements AspenCustomModelerLibrary.IAspenModelerEvents
    Public Enum sinter_AppError
        si_OKAY = 0
        si_SIMULATION_ERROR = 1
        si_SIMULATION_WARNING = 2
        si_COULD_NOT_CONTACT = 3
        si_UNKNOWN_FIELD = 4
        si_INPUT_ERROR = 5
        si_SIMULATION_NOT_RUN = 6
        si_SIMULATION_STOPPED = 7
        si_STOP_FAILED = 8
        si_COM_EXCEPTION = 100
    End Enum

    Public Enum sinter_IOType
        si_UNKNOWN = -1
        si_INTEGER = 0
        si_DOUBLE = 1
        si_DOUBLE_VEC = 2
        si_INTEGER_VEC = 3
        si_STRING_VEC = 4
        si_STRING = 5
        si_TABLE = 6
    End Enum

    'Private o_acm As AspenCustomModelerLibrary.IAspenModeler
    Private WithEvents o_acm As AspenCustomModelerLibrary.IAspenModeler
    Private WithEvents o_stopTimer As New System.Timers.Timer()

    'Syncronization of running and stopping the simulation.  
    'o_terminateMonitor is a condition variable that is signaled when either the simulation
    'has completed, or the user wants to stop it early.
    Private o_terminateMonitor As AutoResetEvent
    Private o_simPaused As Boolean 'Set to True when the simulation has completed, but runSim hasn't handled it yet
    Private o_stopSim As Boolean  'Set to true when stopSim is called, but runSim hasn't stopped the Sim Yet
    Private o_stopTimedOut As Boolean  'When stop is called, a timeOut is set up. If it times out the stop has failed.

    Private o_acm_version As Integer
    Private o_homotopy As Boolean
    Private o_printLevel As Integer
    Private o_runMode As String
    Private o_snapshot As String
    Private o_timeseries As Double()

    Private o_isInitializing As Boolean
    Public o_VariableTree As VariableTree.VariableTree

    ' Log message variables.  ACM keeps a log of messages, but we may not want the early stuff,
    ' and if we get errors from the log multiple times we don't want to get duplicates.  So we 
    ' keep track of log line we start out interested in.
    Private o_startMessageNum As Integer = 0

    'The status of the last run or any errors
    Private o_runStatus As Integer

    'The messages we've gotten so far
    Private o_simulationMessages As List(Of String)

    'Hopefully the ID of the ACM process, so we can terminate it if we have to
    Public o_processId As Int32

    ' Dim o_acm As Object
    Public Sub New()
        o_homotopy = False
        o_printLevel = 0        'LOW print level by default

        o_runMode = "Steady State"
        o_snapshot = ""

        o_isInitializing = False
        o_runStatus = sinter_AppError.si_OKAY

        o_simulationMessages = New List(Of String)()

        o_terminateMonitor = New AutoResetEvent(False)
        'Set to True when the simulation has completed, but runSim hasn't handled it yet
        o_simPaused = False
        'Set to true when stopSim is called, but runSim hasn't stopped the Sim Yet
        o_stopSim = False
    End Sub

    Public ReadOnly Property closed() As Boolean
        Get
            Return (o_acm Is Nothing)
        End Get
    End Property


    Public Property homotopy() As Boolean
        Get
            Return o_homotopy
        End Get
        Set(ByVal value As Boolean)
            o_homotopy = value
        End Set
    End Property

    Public Property printLevel() As Integer
        Get
            Return o_printLevel
        End Get
        Set(ByVal value As Integer)
            o_printLevel = value
            o_acm.Simulation.Options.PrintLevel = value 'Set it in the sim ASAP
        End Set
    End Property

    Public Property runMode() As String
        Get
            Return o_runMode
        End Get
        Set(ByVal value As String)
            o_runMode = value
        End Set
    End Property

    Public Property snapshot() As String
        Get
            Return o_snapshot
        End Get
        Set(ByVal value As String)
            o_snapshot = value
        End Set
    End Property

    Public Property timeseries() As Double()
        Get
            Return o_timeseries
        End Get
        Set(ByVal value As Double())
            o_timeseries = value
        End Set
    End Property


    Public Property runStatus() As Integer
        Get
            Return o_runStatus
        End Get
        Set(ByVal value As Integer)
            o_runStatus = value
        End Set
    End Property
    Public Property ProcessId() As Integer
        Get
            Return o_processId
        End Get
        Set(ByVal value As Integer)
            o_processId = value
        End Set
    End Property


    Public Property isInitializing() As Boolean
        Get
            Return o_isInitializing
        End Get
        Set(ByVal value As Boolean)
            o_isInitializing = value
        End Set
    End Property

    Public Sub presolve()
        '  run presolve scripts
        For Each blk In o_acm.Flowsheet.Blocks
            '  if you call the presolve script on a block with no 
            '  presolve script an exception is thrown, just ignore it.
            Try
                o_acm.Simulation.Flowsheet.resolve(blk.Name).presolve()
            Catch ex As Exception
                System.Diagnostics.Debug.Print(ex.Message)
            End Try
        Next
    End Sub

    Public Sub clearHomotopy()
        ' clear homotopy information  (Needs to be done if homotopy is on, no-op if homotopy is off
        o_acm.Simulation.Homotopy.HomotopyEnabled = False
        o_acm.Simulation.Homotopy.RemoveAll()
    End Sub


    'ACM Parameters don't have units, and there doesn't seem to be any way to detect that.
    Public Function getBaseUnits(ByVal path As String) As String
        Try
            Return o_acm.Simulation.Flowsheet.resolve(path).defaultunit
        Catch ex As Exception
            Return ""
        End Try
    End Function
    'ACM Parameters don't have units, and there doesn't seem to be any way to detect that.
    Public Function getCurrentUnits(ByVal path As String) As String
        Try
            Return o_acm.Simulation.Flowsheet.resolve(path).units
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Public Function getCurrentUnits(ByVal path As String, ByRef indicies As Integer()) As String
        Dim node = o_acm.Simulation.Flowsheet.resolve(path)
        Return node.Item(indicies(0)).units
    End Function

    Public Function getCurrentDescription(ByVal path As String) As String
        Try
            Return o_acm.Simulation.Flowsheet.resolve(path).description
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Public Function getCurrentName(ByVal path As String) As String
        Try
            Return o_acm.Simulation.Flowsheet.resolve(path).name
        Catch ex As Exception
            Return ""
        End Try
    End Function


    Public Function variableExists(ByVal path As String) As Boolean
        Try
            Dim var As Object
            var = o_acm.Simulation.Flowsheet.resolve(path)
            If (var IsNot Nothing) Then
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Sub setVariableValues(ByVal namesArray As Array, ByVal valuesArray As Array)
        'TODO TODO FIX FIX FIX THIS IS BROEKN ON 8.4!
        Dim names(namesArray.Length - 1) As VariantType
        Dim values(namesArray.Length - 1) As VariantType
        For ii = 0 To namesArray.Length - 1
            names(ii) = namesArray(ii)
            values(ii) = valuesArray(ii)
            'vars.AddValues(o_acm.Simulation.Flowsheet.resolve(namesArray(ii)))
        Next

        '        Dim vars
        '        vars = o_acm.Simulation.Flowsheet.NewVariableSet
        '        For ii = 0 To namesArray.Length - 1
        ' vars.AddValues(o_acm.Simulation.Flowsheet.resolve(namesArray(ii)))
        ' Next
        ' vars.SetVariableValues(valuesArray)
        Dim sim = o_acm.Simulation
        Dim flow = sim.Flowsheet

        flow.SetVariableValues(names, values)
    End Sub

    Private Sub initializeMessageLogging()
        'This is how we know where the interesting messages start.  Unfortunately I don't know how to 
        'restrict the number of messages yet.

        o_startMessageNum = o_acm.Simulation.OutputLogger.MessageCount
        ' And set the message reporting level so we don't get too much just
        o_acm.Simulation.Options.PrintLevel = printLevel

    End Sub

    Private Sub getLogMessages()
        If o_acm IsNot Nothing Then
            Dim endMessageNum As Integer = o_acm.Simulation.OutputLogger.MessageCount
            Dim messagesString As [String] = o_acm.Simulation.OutputLogger.MessageText(o_startMessageNum, endMessageNum)
            Dim messagesArray As [String]() = messagesString.Split(ControlChars.Lf)
            o_simulationMessages.AddRange(messagesArray)

            o_startMessageNum = endMessageNum
        End If
    End Sub

    Private Function runSim_SteadyState() As Integer
        ' this ACM module is for steady state runs
        o_acm.Simulation.RunMode = "Steady State"

        If o_homotopy Then
            Try
                o_acm.Simulation.Homotopy.HomotopyEnabled = True
            Catch ex As Exception
                System.Diagnostics.Debug.Print(ex.Message)
                getLogMessages()
                o_runStatus = sinter_AppError.si_SIMULATION_ERROR
                Throw New System.IO.IOException("Turning on Homotopy failed, not allowed on this simulation?")
            End Try
        End If

        'If the sim has already been canceled, don't run it.
        SyncLock (Me)
            If (o_stopSim) Then
                o_simPaused = False
                o_stopSim = False
                o_stopTimedOut = False
                o_terminateMonitor.Reset()
                runStatus = sinter_AppError.si_SIMULATION_STOPPED
                Return runStatus
            Else
                'Just make sure we can't accidentally have old flags still set.
                o_simPaused = False
                o_stopTimedOut = False
            End If
        End SyncLock

        Try
            'Run asynchronously
            o_acm.Simulation.Run(False)

            'Here we wait for an event or signal from the user.  We may get spurious signals on the Monitor,
            'So have a loop to check that.
            Dim ended As Boolean
            ended = False
            While (Not ended)
                SyncLock (Me)
                    If (o_stopSim) Then 'Checking this first should allow success to win a race between the two

                        o_stopTimer.Interval = 60000  '60 seconds
                        o_stopTimer.Start()
                        o_acm.Simulation.Interrupt(False) 'Waiting seems to cause ACM to hang
                        '                       o_simPaused = False
                        o_stopSim = False
                        ended = False
                        runStatus = sinter_AppError.si_SIMULATION_STOPPED
                        'This used to return immediately, but I thought that it better that we be sure 
                        'the interrupt has completed before proceeding, but having Interurupt(true) causes
                        'ACM (8.4 anyway) to hang. So instead this loops around again to catch the "paused" signal
                        'that should follow the Interrupt call above when it completes.
                    ElseIf (o_simPaused) Then
                        o_simPaused = False
                        o_stopSim = False
                        o_stopTimedOut = False
                        ended = True
                        o_terminateMonitor.Reset()
                        getLogMessages()
                        If (runStatus <> sinter_AppError.si_SIMULATION_STOPPED) Then
                            If (o_acm.Simulation.successful = True) Then
                                runStatus = sinter_AppError.si_OKAY
                            Else
                                runStatus = sinter_AppError.si_SIMULATION_ERROR
                            End If
                        Else
                            Thread.Sleep(5000)  'Sleep for 5 seconds to see if that helps stop reliabilibty.
                            closeDocument()
                        End If
                        Return runStatus
                    ElseIf (o_stopTimedOut) Then
                        o_stopTimedOut = False
                        If (runStatus = sinter_AppError.si_SIMULATION_STOPPED) Then 'Check that signal was valid before proceeding
                            o_simPaused = False
                            o_stopSim = False
                            ended = True
                            o_stopTimer.Stop()
                            o_terminateMonitor.Reset()
                            runStatus = sinter_AppError.si_STOP_FAILED
                            Return runStatus  'Stopping failed, bail out immediately
                        End If
                    End If
                End SyncLock
                o_terminateMonitor.WaitOne() 'Check status flags before waiting
            End While

        Catch ex As Exception
            System.Diagnostics.Debug.Print(ex.Message)
            getLogMessages()
            o_runStatus = sinter_AppError.si_SIMULATION_ERROR
            Return runStatus
        Finally
            o_stopTimer.Stop()
        End Try

        Return runStatus

    End Function

    Private Function runSim_Dynamic() As Integer
        ' this ACM module is for steady state runs
        o_acm.Simulation.RunMode = "Dynamic"
        o_acm.Simulation.Termination = "AtTime"

        'If the sim has already been canceled, don't run it.
        SyncLock (Me)
            If (o_stopSim) Then
                o_simPaused = False
                o_stopSim = False
                o_stopTimedOut = False
                o_terminateMonitor.Reset()
                runStatus = sinter_AppError.si_SIMULATION_STOPPED
                Return runStatus
            Else
                'Just make sure we can't accidentally have old flags still set.
                o_simPaused = False
                o_stopTimedOut = False
            End If
        End SyncLock

        'If the user defined a snapshot, try to load it up.  
        If (o_snapshot <> "") Then
            Try
                o_acm.Simulation.Results.Refresh()
                Dim snapshot As Object
                snapshot = o_acm.Simulation.Results.FindSnapshot(o_snapshot)
                o_acm.Simulation.Results.Rewind(snapshot)
            Catch
                Throw New ArgumentException(String.Format("ACM Dynamic run failed to load snapshot {0}.", o_snapshot))
            End Try

        End If

        'Dynamic doesn't work yet. 
        runStatus = sinter_AppError.si_SIMULATION_ERROR

        Return runStatus

    End Function


    Public Function runSim() As Integer

        If (runMode = "Steady State") Then
            Return runSim_SteadyState()
        ElseIf (runMode = "Dynamic") Then
            Return runSim_Dynamic()
        Else
            Throw New ArgumentException(String.Format("ACM.runSim has invalid runMode {0}", runMode))
        End If


    End Function

    Public Sub closeDocument()

        Try
            o_acm.CloseDocument(False)
        Catch ex As Exception
            System.Diagnostics.Debug.Print(ex.Message)
            getLogMessages()
            runStatus = sinter_AppError.si_SIMULATION_ERROR
            Throw New System.IO.FileNotFoundException("Weird, closing the ACM document failed.")
        End Try
    End Sub


    Public Sub openDocument(ByVal absBackupFilename As String)

        Try
            o_acm.OpenDocument(absBackupFilename)
            o_acm.AddEventSink(Me)
        Catch ex As Exception
            System.Diagnostics.Debug.Print(ex.Message)
            getLogMessages()
            runStatus = sinter_AppError.si_SIMULATION_ERROR
            If System.IO.File.Exists(absBackupFilename) Then
                Throw New System.IO.IOException("Open ACM Document failed.  Is " + absBackupFilename + " actually an ACM file?  Or does another process have it open? (Perhaps a crashed ACM Process?)")
            Else
                Throw New System.IO.FileNotFoundException("Requested File " + absBackupFilename + " could not be found.")
            End If
        End Try
    End Sub

    Public Sub openSim(ByVal absBackupFilename As String)
        isInitializing = True
        If o_acm IsNot Nothing Then
            o_acm = Nothing
        End If
        Dim type__1 As Type = Type.GetTypeFromProgID("ACM Application")
        Try
            'o_acm = new ACMWrapper.ACMWrapper();//AspenCustomModelerLibrary.AspenCustomModeler();
            o_acm = System.Activator.CreateInstance(type__1)
            ProcessId = o_acm.processId
            o_acm_version = o_acm.Version
        Catch ex As Exception
            isInitializing = False
            System.Diagnostics.Debug.Print(ex.Message)
            getLogMessages()
            runStatus = sinter_AppError.si_SIMULATION_ERROR
            Throw New System.IO.IOException("Could not open ACM")
        End Try

        If (o_acm Is Nothing) Then
            isInitializing = False
            Throw New System.IO.IOException("Could not open ACM")
        End If

        'Start Recording Possible error messages from here!
        initializeMessageLogging()

        Try
            o_acm.Visible = False
            openDocument(absBackupFilename)
        Finally
            isInitializing = False
        End Try


    End Sub

    Public Sub closeSim()
        Try
            'This used to have this retry section, but that caused ACM to hang if events are enabled.
            '           i = 1
            '            While (i <= 15)
            '  close aspen until its gone or give up after 15 trys
            '  i think when somthing kills excel it leaves the aspen
            '  object with extra in a refrence counter but I may be wrong.
            o_acm.Quit()
            '           i += 1
            '           End While
        Catch ex As Exception
            o_acm = Nothing
            System.Diagnostics.Debug.Print(ex.Message)
        End Try
    End Sub

    Public Sub stopSim()
        SyncLock (Me)
            o_stopSim = True
            o_terminateMonitor.Set()
        End SyncLock
    End Sub

    'This is a bit funky, we don't get errors and warnings with ACM, we just get meesages.
    'I've decided that if the sim fails there must be an error in there, if it passes, just ignore them.
    Public Function errorsBasic() As String()
        getLogMessages()
        Return o_simulationMessages.ToArray()
    End Function


    Public Sub sendValueToSim(Of ValueType)(ByVal path As String, ByVal value As ValueType)
        If homotopy Then
            Try
                o_acm.Simulation.Homotopy.addTarget(path, value, "CurrentUnits")
            Catch ex As Exception
                System.Diagnostics.Debug.Print(ex.Message)
                getLogMessages()
                runStatus = sinter_AppError.si_SIMULATION_ERROR
                Throw New System.IO.IOException("Could not add " + path + " as Homotopy target or value " + Convert.ToString(value) + " invalid.")
            End Try
        Else
            Try
                Dim sim = o_acm.Simulation
                Dim flow = sim.Flowsheet
                Dim node = flow.resolve(path)
                Dim val = node.Value
                'o_acm.Simulation.Flowsheet.resolve(path).Value["CurrentUnits"] = value;
                node.Value("CurrentUnits") = value
            Catch ex As Exception
                System.Diagnostics.Debug.Print(ex.Message)
                getLogMessages()
                runStatus = sinter_AppError.si_SIMULATION_ERROR
                Throw New System.IO.IOException("Could not set " + path + " to " + Convert.ToString(value) + ".  This sometimes happens if " + path + " should be a homotopy target.")
            End Try
        End If
    End Sub


    Public Sub sendValueToSim(Of ValueType)(ByVal path As String, ByVal ii As Integer, ByVal value As ValueType)
        Dim indicies = getVectorIndicies(path)
        Dim index = indicies(ii)

        If o_homotopy Then
            Try
                o_acm.Simulation.Homotopy.Item(index).addTarget(path, value, "CurrentUnits")
            Catch ex As Exception
                System.Diagnostics.Debug.Print(ex.Message)
                getLogMessages()
                runStatus = sinter_AppError.si_SIMULATION_ERROR
                Throw New System.IO.IOException("Could not add " + path + " as Homotopy target or value " + Convert.ToString(value) + " invalid.")
            End Try
        Else
            Try
                Dim sim = o_acm.Simulation
                Dim flow = sim.Flowsheet
                Dim node = flow.resolve(path)
                'o_acm.Simulation.Flowsheet.resolve(path).Value["CurrentUnits"] = value;
                node.Item(index).Value("CurrentUnits") = value
            Catch ex As Exception
                System.Diagnostics.Debug.Print(ex.Message)
                getLogMessages()
                runStatus = sinter_AppError.si_SIMULATION_ERROR
                Throw New System.IO.IOException("Could not set " + path + " to " + Convert.ToString(value) + ".  This sometimes happens if " + path + " should be a homotopy target.")
            End Try
        End If
    End Sub

    Public Sub sendVectorToSim(Of ValueType)(ByVal path As String, ByVal value As ValueType())
        Dim indicies = getVectorIndicies(path)

        If o_homotopy Then
            Try
                Dim len As Integer = value.Length
                For ii As Integer = 0 To len - 1
                    Dim index = indicies(ii)
                    o_acm.Simulation.Homotopy.Item(index).addTarget(path, value(ii), "CurrentUnits")
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.Print(ex.Message)
                getLogMessages()
                runStatus = sinter_AppError.si_SIMULATION_ERROR
                Throw New System.IO.IOException("Could not add " + path + " as Homotopy target or value " + Convert.ToString(value) + " invalid.")
            End Try
        Else
            Try
                Dim sim = o_acm.Simulation
                Dim flow = sim.Flowsheet
                Dim node = flow.resolve(path)
                Dim len As Integer = value.Length
                For ii As Integer = 0 To len - 1
                    Dim index = indicies(ii)
                    node.Item(index).Value("CurrentUnits") = value(ii)
                    'o_acm.Simulation.Flowsheet.resolve(path).Value["CurrentUnits"] = value;
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.Print(ex.Message)
                getLogMessages()
                runStatus = sinter_AppError.si_SIMULATION_ERROR
                Throw New System.IO.IOException("Could not set " + path + " to " + Convert.ToString(value) + ".  This sometimes happens if " + path + " should be a homotopy target.")
            End Try
        End If
    End Sub

    Public Function recvValueFromSimAsObject(ByVal path As String) As [Object]
        Try
            Return o_acm.Simulation.Flowsheet.resolve(path).Value("CurrentUnits")
        Catch ex As Exception
            System.Diagnostics.Debug.Print(ex.Message)
            getLogMessages()
            runStatus = sinter_AppError.si_SIMULATION_ERROR
            Throw New System.IO.IOException("Failed to get result for " + path + ". Does the variable exist?")
        End Try
    End Function

    'For vectors Takes the actual indicies in the simulation! (so, 1 for an 1-indexed array for example)
    Public Function recvValueFromSimAsObject(ByVal path As String, ByVal ii As Integer) As [Object]
        Try
            Dim node = o_acm.Simulation.Flowsheet.resolve(path)
            Return node.Item(ii).Value("CurrentUnits")
        Catch ex As Exception
            System.Diagnostics.Debug.Print(ex.Message)
            getLogMessages()
            runStatus = sinter_AppError.si_SIMULATION_ERROR
            Throw New System.IO.IOException("Failed to get result for " + path + ". Does the variable exist?")
        End Try
    End Function



    Public Function recvValueFromSim(Of ValueType)(ByVal path As String) As ValueType
        Try
            'We don't use "AsObject" because the casting gets weird.
            Dim node = o_acm.Simulation.Flowsheet.resolve(path)
            Return DirectCast(o_acm.Simulation.Flowsheet.resolve(path).Value("CurrentUnits"), ValueType)
        Catch ex As Exception
            System.Diagnostics.Debug.Print(ex.Message)
            getLogMessages()
            runStatus = sinter_AppError.si_SIMULATION_ERROR
            Throw New System.IO.IOException("Failed to get result for " + path + ". Does the variable exist?")
        End Try
    End Function

    'For vectors
    Public Function recvValueFromSim(Of ValueType)(ByVal path As String, ByVal ii As Integer) As ValueType
        Dim indicies = getVectorIndicies(path)
        Dim index = indicies(ii)
        Try
            'We don't use "AsObject" because the casting gets weird.
            Dim node = o_acm.Simulation.Flowsheet.resolve(path)
            Return DirectCast(node.Item(index).Value("CurrentUnits"), ValueType)
        Catch ex As Exception
            System.Diagnostics.Debug.Print(ex.Message)
            getLogMessages()
            runStatus = sinter_AppError.si_SIMULATION_ERROR
            Throw New System.IO.IOException("Failed to get result for " + path + ". Does the variable exist?")
        End Try
    End Function


    Public Sub recvVectorFromSim(Of ValueType)(ByVal path As String, ByRef indicies As Integer(), ByRef value As ValueType())
        ' Ok, this function uses GetVariableValues to get the entire vector at once, rather than
        ' requesting each variable in the vector individually, which speeds up the operation about 100x.
        ' Unfortunately, GetVariableValues has the worst API I have ever seen.  So the code below is evil.
        ' Basically, you create an ACM "variable set" with FindMatchingVariables.  You pass that into 
        ' GetVariableValues.  GetVariableValues then RE-ORDERS the set behind the scenes, then passes you
        ' back and array of values (as strings) IN THE NEW ORDER.  WHICH YOU DON'T KNOW.  IT DOESN'T GIVE
        ' YOU ANY WAY TO DISCOVER THE NEW ORDERING.  By trial and error I figured out that it is sorting
        ' the array paths alphabetically.  So, for a vector that becomes: foo(0), foo(1), foo(10), foo(100), 
        ' foo(101), etc.  So I recreate the ordering below so I can get the correct values in the correct
        ' variables.  So, if they ever decide to switch to a hash table internally, I'm hozed.
        Dim len As Integer = value.Length
        Dim fullPath As String = path + "(*)"
        'path(*) gets all the vars in the vector with FindMatchingVariables
        Dim resolvedOutVars = o_acm.Simulation.Flowsheet.FindMatchingVariables(fullPath)
        Dim outvars = o_acm.Simulation.Flowsheet.GetVariableValues(resolvedOutVars)

        'Ok, now make the variables names, so we can sort them into the same stupid order ACM uses (actually just the array indexes, not full names)
        Dim varNames As String() = New String(outvars.Length - 1) {}
        For ii As Integer = 0 To varNames.Count() - 1
            varNames(ii) = Convert.ToString(indicies(ii))
        Next

        Array.Sort(varNames)
        'Now put the array in alphabetical order to match ACM ordering
        For ii As Integer = 0 To varNames.Count() - 1
            'ii is SimSinter internal array index, we need to turn that back into an ACM index
            Dim acmIndex As Integer = Convert.ToInt32(varNames(ii))
            Dim sinIndex = Array.BinarySearch(indicies, acmIndex)

            value(sinIndex) = DirectCast(Convert.ChangeType(outvars(ii), GetType(ValueType)), ValueType)
        Next
    End Sub

    Public Property Vis() As Boolean
        Get
            Return o_acm.Visible
        End Get
        Set(ByVal value As Boolean)
            o_acm.Application.Visible = value
            o_acm.Interactive = value
            o_acm.Visible = value
        End Set
    End Property

    '<Summary>
    ' Suppress Aspen Dialog Boxes (Helps keep Aspen invisible)
    '</Summary>

    Public Property dialogSuppress() As Boolean
        'In Aspen you suppressDialogs, in ACM you set Errors visible or not, which is the opposite
        Get
            Return Not o_acm.ErrorMsgBoxesVisible
        End Get
        Set(ByVal value As Boolean)
            '                 if (value)
            '                else
            '                o_acm.ErrorMsgBoxesVisible = 1;
            o_acm.ErrorMsgBoxesVisible = Not value
        End Set
    End Property

    Public ReadOnly Property pathSeperator As Char
        Get
            Return "."c
        End Get
    End Property

    Public Function parsePath(ByVal path As String) As IList(Of String)

        While (path.Length > 0 AndAlso path(0) = pathSeperator)
            path = path.Substring(1)
        End While

        If (path.Length = 0) Then
            Return New List(Of String)
        End If

        Return path.Split(pathSeperator).ToList()
    End Function
    Public Function ParseVectorIndex(path As String) As Integer
        Dim lastnameStart As Integer
        Dim lastnameLen As Integer
        Dim lastLParen As Integer
        lastnameStart = path.LastIndexOf(pathSeperator)
        lastnameLen = path.Length - lastnameStart
        lastLParen = path.LastIndexOf("(", path.Length, lastnameLen)
        If (lastLParen = -1) Then
            Return -1
        End If

        Dim lastRParen As Integer
        Dim substringLen As Integer
        Dim vecIndexSubstring As String
        Dim nodeVecIndex As Integer
        lastRParen = path.LastIndexOf(")", path.Length, lastnameLen)
        substringLen = lastRParen - (lastLParen + 1)
        vecIndexSubstring = path.Substring(lastLParen + 1, substringLen)
        nodeVecIndex = -1
        If (Int32.TryParse(vecIndexSubstring, nodeVecIndex)) Then
            Return nodeVecIndex
        Else
            Return -1
        End If

    End Function




    'Shared treeout = New StreamWriter("TreeMatching.txt")

    'Public Sub printVisitor(ByVal node As VariableTree.VariableTreeNode)
    '    treeout.WriteLine(node.path)
    'End Sub


    '* 
    '         * void makeDataTree
    '         * 
    '         * This function generates a tree based on the variables availible in the simulation.  All input and
    '         * output variables.  This is used primarily for the Sinter Config GUI.
    '         
    Public Sub makeDataTree()

        o_VariableTree = New VariableTree.VariableTree(AddressOf parsePath, pathSeperator)

        Dim rootNode As New VariableTree.VariableTreeNode("", "", pathSeperator)
        Dim allNodes = o_acm.Simulation.Flowsheet.FindMatchingVariables("~")
        'Dim normout As StreamWriter
        '        normout = New StreamWriter("ACMMatching.txt")

        For Each child In allNodes
            Dim childPath As String
            childPath = child.Name
            rootNode.addNode(parsePath(childPath))
        Next

        o_VariableTree.rootNode = rootNode

        'Remove the Dummy Children (those are only required when doing incremental tree building
        rootNode.traverse(rootNode, Sub(thisNode)
                                        If (thisNode.o_children.ContainsKey("DummyChild")) Then
                                            thisNode.o_children.Remove("DummyChild")
                                        End If
                                    End Sub)

    End Sub

    '* 
    ' void startDataTree
    ' 
    ' This function generates the root of a variable tree.  It does not fill in any child nodes.  This is
    ' useful for generating the tree as the user opens nodes in the SinterConfigGUI 
    '
    Public Sub startDataTree()
        o_VariableTree = New VariableTree.VariableTree(AddressOf parsePath, pathSeperator)

        Dim rootNode As New VariableTree.VariableTreeNode("", "", pathSeperator)
        Dim rootChildren = o_acm.Simulation.Flowsheet.FindMatchingVariables("*")
        rootNode.o_children.Remove("DummyChild")

        For Each child In rootChildren
            Dim childPath As String
            childPath = child.Name
            rootNode.addNode(parsePath(childPath))
        Next

        Dim childNodes = o_acm.Simulation.Flowsheet.FindMatchingVariables("*.*")

        For Each child In childNodes
            Dim childPath As String
            childPath = child.Name
            Dim arrayPath = New List(Of String)
            arrayPath.Add(parsePath(childPath)(0))
            Dim childName = arrayPath(0)
            If (Not rootNode.o_children.ContainsKey(childName)) Then
                rootNode.addNode(arrayPath)
            End If

        Next


        o_VariableTree.rootNode = rootNode

    End Sub

    Public Function findDataTreeNode(ByVal pathArray As IList(Of String)) As VariableTree.VariableTreeNode
        Return findDataTreeNode(pathArray, o_VariableTree.rootNode)
    End Function

    '* Leftmost name in the path refers to child of "ThisNode"
    '
    Private Function findDataTreeNode(ByVal pathArray As IList(Of String), ByVal thisNode As VariableTree.VariableTreeNode) As VariableTree.VariableTreeNode

        If (thisNode.o_children.ContainsKey("DummyChild")) Then
            thisNode.o_children.Remove("DummyChild")
            Dim thisAspenNode = o_acm.Simulation.Flowsheet.resolve(thisNode.path)

            Dim childNodes
            Try
                childNodes = o_acm.Simulation.Flowsheet.FindMatchingVariables(thisNode.path + ".*")
                '                childNodes = thisAspenNode.FindMatchingVariables("*")
            Catch Ex As Exception
                If (pathArray.Count = 0) Then
                    Return thisNode
                Else
                    Throw Ex
                End If
            End Try


            For Each child In childNodes
                Dim childPath As String
                childPath = child.Name
                Dim parsedPath = parsePath(childPath)
                Dim childVariableTreeNode = New VariableTree.VariableTreeNode(parsedPath.Last, childPath, pathSeperator)
                thisNode.addChild(childVariableTreeNode)
            Next

        End If

        If (pathArray.Count = 0) Then
            Return thisNode
        Else
            Dim childName = pathArray(0)
            pathArray.RemoveAt(0)
            Return findDataTreeNode(pathArray, thisNode.o_children(childName))
        End If
    End Function

    Public Function getHeatIntegrationVariables() As IEnumerable(Of String)
        Dim heatPaths As HashSet(Of String)
        heatPaths = New HashSet(Of String)
        For Each block In o_acm.Simulation.Flowsheet.Blocks
            For Each port In block.ports
                Dim typename As String
                typename = port.TypeName
                If (typename.Equals("PortMat") Or typename.Equals("PortHeat") Or typename.Equals("PortInfo")) Then
                    Dim portPath As String
                    Dim childNodes
                    portPath = port.GetPath()
                    childNodes = o_acm.Simulation.Flowsheet.FindMatchingVariables(portPath + ".~")
                    For Each node In childNodes
                        Dim thisVarType As String
                        thisVarType = node.BaseTypename
                        If (thisVarType.Equals("RealVariable")) Then
                            heatPaths.Add(node.GetPath())
                        End If
                    Next
                End If
            Next
        Next
        Return heatPaths
    End Function

    Public Function search(ByVal searchPattern As String, ByVal searchType As String, ByVal sfixed As Boolean, ByVal free As Boolean,
            ByVal rateinitial As Boolean, ByVal initial As Boolean, ByVal parameters As Boolean, ByVal algebraics As Boolean, ByVal state As Boolean,
            ByVal inactive As Boolean, ByRef workerObj As Object) As IList(Of String)

        Dim worker As BackgroundWorker
        worker = workerObj

        Dim pathes As List(Of String)
        pathes = New List(Of String)

        Dim searchModifiers As New System.Text.StringBuilder()
        If (sfixed) Then
            searchModifiers.Append("fixed ")
        End If
        If (free) Then
            searchModifiers.Append("free ")
        End If
        If (rateinitial) Then
            searchModifiers.Append("rateinitial ")
        End If
        If (initial) Then
            searchModifiers.Append("initial ")
        End If
        Dim vars
        vars = o_acm.Simulation.Flowsheet.FindMatchingVariables(searchPattern, searchModifiers.ToString, searchType, parameters, algebraics, state, inactive)

        Dim varCount = 0
        Dim totalVars = vars.Count
        For Each variab In vars
            If (worker IsNot Nothing) Then
                varCount = varCount + 1
                Dim percentage = varCount / totalVars
                worker.ReportProgress(Convert.ToInt32(percentage * 100)) 'Calc precentage done

                If (worker.CancellationPending) Then
                    pathes.Clear() 'The user canceled the search, so just bail out with nothing
                    Return pathes
                End If
            End If


            pathes.Add(variab.getPath())
        Next

        Return pathes
    End Function
    'Simple Serach just searches and returns a list of the match paths found.  
    Public Function search(ByVal searchPattern As String) As IList(Of String)
        Dim vars
        vars = o_acm.Simulation.Flowsheet.FindMatchingVariables(searchPattern)
        Dim pathes As List(Of String)
        pathes = New List(Of String)
        For Each variab In vars
            pathes.Add(variab.getPath())
        Next
        Return pathes
    End Function

    Public Function getVectorIndicies(path As String) As IList(Of Integer)
        Dim paths
        Dim indicies
        paths = search(path + "(*)")
        indicies = New List(Of Integer)
        For Each thisPath In paths
            Dim index = ParseVectorIndex(thisPath)
            indicies.Add(index)
        Next

        indicies.Sort()
        Return indicies
    End Function



    Public Function guessVectorSize(ByVal path As String) As Integer
        Dim vector = o_acm.Simulation.Flowsheet.resolve(path)
        Return vector.count
    End Function

    Public Function guessTypeFromSim(ByVal path As String) As Integer
        Try
            Dim var = o_acm.Simulation.Flowsheet.resolve(path)
            If (var IsNot Nothing) Then
                Try  'Is it a vector?
                    Dim pathes
                    pathes = search(path & "(*)")
                    If (pathes.count > 0) Then 'If we got multiple paths back from that search string, it should be a vector
                        Dim anIndex = ParseVectorIndex(pathes(0)) 'Grab one of those (basically random) and get it's index
                        Dim thisVecValue = var.Item(anIndex).Value("CurrentUnits") 'Get the value at that index and see it's type.
                        If (TypeOf thisVecValue Is Double) Then                 'that's the type of the array
                            Return sinter_IOType.si_DOUBLE_VEC
                        ElseIf (TypeOf thisVecValue Is Integer) Then
                            Return sinter_IOType.si_INTEGER_VEC
                        ElseIf (TypeOf thisVecValue Is String) Then
                            Return sinter_IOType.si_STRING_VEC
                        End If
                    End If

                Catch ex As Exception

                End Try
                'If we got here, it's not an array, so it must be a scalar
                Dim thisValue = var.Value("CurrentUnits")
                If (TypeOf thisValue Is Double) Then
                    Return sinter_IOType.si_DOUBLE
                ElseIf (TypeOf thisValue Is Integer) Then
                    Return sinter_IOType.si_INTEGER
                ElseIf (TypeOf thisValue Is String) Then
                    Return sinter_IOType.si_STRING
                End If

            Else
                Return sinter_IOType.si_UNKNOWN
            End If
        Catch ex As Exception
            Return sinter_IOType.si_UNKNOWN
        End Try

        Return sinter_IOType.si_UNKNOWN
    End Function

    ' This is the method to run when the timer is raised. 
    Private Sub TimerEventProcessor(myObject As Object, ByVal myEventArgs As EventArgs) Handles o_stopTimer.Elapsed
        SyncLock (Me)
            o_stopTimedOut = True
            o_terminateMonitor.Set()
        End SyncLock
    End Sub

    Private Sub IAspenModelerEvents_OnRunPaused() Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnRunPaused
        SyncLock (Me)
            o_simPaused = True
            o_terminateMonitor.Set()
        End SyncLock
    End Sub


    Private Sub IAspenModelerEvents_OnDeletedBlock(ByVal sBlockName As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnDeletedBlock
    End Sub

    Private Sub IAspenModelerEvents_OnDeletedStream(ByVal sStreamName As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnDeletedStream
    End Sub

    Private Sub IAspenModelerEvents_OnHasQuit() Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnHasQuit

    End Sub

    Private Sub IAspenModelerEvents_OnHasSaved() Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnHasSaved

    End Sub

    Private Sub IAspenModelerEvents_OnNew() Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnNew

    End Sub

    Private Sub IAspenModelerEvents_OnNewBlock(ByVal sBlockName As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnNewBlock

    End Sub

    Private Sub IAspenModelerEvents_OnNewStream(ByVal sStreamName As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnNewStream

    End Sub

    Private Sub IAspenModelerEvents_OnOpened(ByVal sPath As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnOpened

    End Sub

    Private Sub IAspenModelerEvents_OnRewindorCopyValues() Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnRewindorCopyValues

    End Sub

    Private Sub IAspenModelerEvents_OnRunModeChanged(ByVal sRunMode As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnRunModeChanged

    End Sub


    Private Sub IAspenModelerEvents_OnRunStarted() Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnRunStarted

    End Sub

    Private Sub IAspenModelerEvents_OnSavedAs(ByVal sPath As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnSavedAs

    End Sub

    Private Sub IAspenModelerEvents_OnStepComplete() Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnStepComplete

    End Sub

    Private Sub IAspenModelerEvents_OnStreamConnected(ByVal sStreamName As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnStreamConnected

    End Sub

    Private Sub IAspenModelerEvents_OnStreamDisconnected(ByVal sStreamName As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnStreamDisconnected

    End Sub

    Private Sub IAspenModelerEvents_OnUomSetChanged(ByVal sUomSetName As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnUomSetChanged

    End Sub

    Private Sub IAspenModelerEvents_OnUserChangedVariable(ByVal sVariableName As Object, ByVal sAttributeName As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnUserChangedVariable

    End Sub

    Private Sub IAspenModelerEvents_OnUserEvent(ByVal sUserString As Object) Implements AspenCustomModelerLibrary.IAspenModelerEvents.OnUserEvent

    End Sub

End Class

