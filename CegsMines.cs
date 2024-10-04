using System;
using System.ComponentModel;
using System.Threading.Tasks;
using static AeonHacs.Utilities.Utility;

namespace AeonHacs.Components;

public partial class CegsMines : Cegs
{
    #region HacsComponent
    
    [HacsConnect]
    protected override void Connect()
    {
        base.Connect();

        ChamberCT1 = Find<IChamber>("CT1");

        // Sections
        TF = Find<Section>("TF");
        CA = Find<Section>("CA");
        CTF = Find<Section>("CTF");
        CT1 = Find<Section>("CT1");
        CT2 = Find<Section>("CT2");

        TF_IP1 = Find<Section>("TF_IP1");
        FTG_IP1 = Find<Section>("FTG_IP1");
        FTG_IP2 = Find<Section>("FTG_IP2");
        IM_CT1 = Find<Section>("IM_CT1");
        IM_CT2 = Find<Section>("IM_CT2");
        IM_CA_CT1 = Find<Section>("IM_CA_CT1");
        IM_CA_CT2 = Find<Section>("IM_CA_CT2");

        CA1 = Find<SableCA10>("CA1");
        TF1 = Find<TcpTubeFurnace>("TF1");
        LnScale1 = Find<LNScale>("LnScale1");
        pAmbient = Find<AIManometer>("pAmbient");
        FTG_TFFlowManager = Find<FlowManager>("FTG_TFFlowManager");
        FTG_IMFlowManager = Find<FlowManager>("FTG_IMFlowManager");
        TFFlowManager = Find<FlowManager>("TFFlowManager");
        // Select Default Coil Trap
        SelectCT1();
    }

    #endregion HacsComponent

    #region System configuration

    #region HacsComponents

    public virtual IVacuumSystem VacuumSystem2 { get; set; }

    IChamber ChamberCT1 { get; set; }

    #region Sections

    /// <summary>
    /// The CT section, either CT1 or CT2, from which the CEGS should transfer the sample to the VTT.
    /// </summary>
    public override ISection CT
    {
        get => ct ?? CurrentCT;
        set => ct = value;
    }
    ISection ct;

    /// <summary>
    /// The sample gas collection path; one of IM_CT1, IM_CT2, IM_CA_CT1, IM_CA_CT2;
    /// </summary>
    protected override ISection IM_FirstTrap { get => base.IM_FirstTrap; set => base.IM_FirstTrap = value; }

    /// <summary>
    /// Tube furnace section
    /// </summary>
    public ISection TF { get; set; }

    /// <summary>
    /// CO2 Analyzer section
    /// </summary>
    public ISection CA { get; set; }

    /// <summary>
    /// Coil Trap Flow section
    /// </summary>
    public ISection CTF { get; set; }

    /// <summary>
    /// Coil Trap 1 section
    /// </summary>

    public ISection CT1 { get; set; }

    /// <summary>
    /// Coil Trap 2 section
    /// </summary>
    public ISection CT2 { get; set; }

    /// <summary>
    /// Tube furnace..Inlet Port 1 section
    /// </summary>
    public ISection TF_IP1 { get; set; }

    /// <summary>
    /// Flow-Through Gas..Tube Furnace..Inlet Port 1 section
    /// </summary>
    public ISection FTG_IP1 { get; set; }

    /// <summary>
    /// Flow-Through Gas..Inlet Port 2 section
    /// </summary>
    public ISection FTG_IP2 { get; set; }

    /// <summary>
    /// Inlet Manifold..Coil Trap 1 section (bypasses CO2 Analyzer)
    /// </summary>
    public ISection IM_CT1 { get; set; }

    /// <summary>
    /// Inlet Manifold..Coil Trap 2 section (bypasses CO2 Analyzer)
    /// </summary>
    public ISection IM_CT2 { get; set; }

    /// <summary>
    /// Inlet Manifold..CO2 analyzer..Coil Trap 1 section
    /// </summary>
    public ISection IM_CA_CT1 { get; set; }

    /// <summary>
    /// Inlet Manifold..CO2 analyzer..Coil Trap 2 section
    /// </summary>
    public ISection IM_CA_CT2 { get; set; }

    #endregion Sections

    /// <summary>
    /// CO2 analyzer
    /// </summary>
    public SableCA10 CA1 { get; set; }

    /// <summary>
    /// Tube furnace
    /// </summary>
    public TcpTubeFurnace TF1 { get; set; }

    /// <summary>
    /// Liquid nitrogen scale
    /// </summary>
    public LNScale LnScale1 { get; set; }

    /// <summary>
    /// Ambient air pressure.
    /// </summary>
    public AIManometer pAmbient { get; set; }

    /// <summary>
    /// Flow manager for gas (He or O2) into the tube furnace.
    /// </summary>        
    public FlowManager FTG_TFFlowManager { get; set; }

    /// <summary>
    /// Flow manager for gas (He or O2) through Inlet Port 2 into the Inlet Manifold.
    /// </summary>
    public FlowManager FTG_IMFlowManager { get; set; }

    /// <summary>
    /// Flow manager for gas out of the tube furnace.
    /// </summary>
    public FlowManager TFFlowManager { get; set; }

    #endregion HacsComponents

    #endregion System configuration

    #region Periodic system activities & maintenance

    protected override void ZeroPressureGauges() => ZeroPressureGauges([MC, CTF, IM, GM, .. GraphiteReactors]);

    #endregion Periodic system activities & maintenance

    #region Process Management

    protected override void BuildProcessDictionary()
    {
        Separators.Clear();

        // Running samples
        ProcessDictionary["Run samples"] = RunSamples;
        Separators.Add(ProcessDictionary.Count);

        // Preparation for running samples
        ProcessDictionary["Prepare GRs for new iron and desiccant"] = PrepareGRsForService;
        ProcessDictionary["Precondition GR iron"] = PreconditionGRs;
        ProcessDictionary["Replace iron in sulfur traps"] = ChangeSulfurFe;
        //ProcessDictionary["Service d13C ports"] = Service_d13CPorts;
        //ProcessDictionary["Load empty d13C ports"] = LoadEmpty_d13CPorts;
        //ProcessDictionary["Prepare loaded d13C ports"] = PrepareLoaded_d13CPorts;
        Separators.Add(ProcessDictionary.Count);

        ProcessDictionary["Prepare carbonate sample for acid"] = PrepareCarbonateSample;
        ProcessDictionary["Load acidified carbonate sample"] = LoadCarbonateSample;
        Separators.Add(ProcessDictionary.Count);

        // Open line
        ProcessDictionary["Open and evacuate line"] = OpenLine;
        ProcessDictionary["Open and evacuate VS1"] = OpenVS1Line;
        ProcessDictionary["Open and evacuate VS2"] = OpenVS2Line;
        Separators.Add(ProcessDictionary.Count);

        // Main process continuations
        ProcessDictionary["Collect, etc."] = CollectEtc;
        ProcessDictionary["Extract, etc."] = ExtractEtc;
        ProcessDictionary["Measure, etc."] = MeasureEtc;
        ProcessDictionary["Graphitize, etc."] = GraphitizeEtc;
        Separators.Add(ProcessDictionary.Count);

        // Quartz processes
        ProcessDictionary["Day 2"] = Day2;

        // Top-level steps for main process sequence
        ProcessDictionary["Admit sealed CO2 to InletPort"] = AdmitSealedCO2IP;
        ProcessDictionary["Collect CO2 from InletPort"] = Collect;
        ProcessDictionary["Extract"] = Extract;
        ProcessDictionary["Measure"] = Measure;
        ProcessDictionary["Discard excess CO2 by splits"] = DiscardSplit;
        ProcessDictionary["Remove sulfur"] = RemoveSulfur;
        ProcessDictionary["Dilute small sample"] = Dilute;
        ProcessDictionary["Graphitize aliquots"] = GraphitizeAliquots;
        ProcessDictionary["Open and evacuate TF and VS1"] = OpenTF_VS1;

        ProcessDictionary["Collect, etc."] = CollectEtc;
        ProcessDictionary["Extract, etc."] = ExtractEtc;
        ProcessDictionary["Measure, etc."] = MeasureEtc;
        ProcessDictionary["Graphitize, etc."] = GraphitizeEtc;
        Separators.Add(ProcessDictionary.Count);

        // Secondary-level process sub-steps
        ProcessDictionary["Evacuate Inlet Port"] = EvacuateIP;
        ProcessDictionary["Flush Inlet Port"] = FlushIP;
        ProcessDictionary["Admit O2 into Inlet Port"] = AdmitIPO2;
        ProcessDictionary["Heat Quartz and Open Line"] = HeatQuartzOpenLine;
        ProcessDictionary["Turn off IP furnaces"] = TurnOffIPFurnaces;
        ProcessDictionary["Discard IP gases"] = DiscardIPGases;
        ProcessDictionary["Close IP"] = CloseIP;
        ProcessDictionary["Start collecting"] = StartCollecting;
        ProcessDictionary["Clear collection conditions"] = ClearCollectionConditions;
        ProcessDictionary["Collect until condition met"] = CollectUntilConditionMet;
        ProcessDictionary["Stop collecting"] = StopCollecting;
        ProcessDictionary["Stop collecting after bleed down"] = StopCollectingAfterBleedDown;
        ProcessDictionary["Evacuate and Freeze VTT"] = FreezeVtt;
        ProcessDictionary["Admit Dead CO2 into MC"] = AdmitDeadCO2;
        ProcessDictionary["Purify CO2 in MC"] = CleanupCO2InMC;
        ProcessDictionary["Discard MC gases"] = DiscardMCGases;
        ProcessDictionary["Divide sample into aliquots"] = DivideAliquots;
        Separators.Add(ProcessDictionary.Count);

        // Granular inlet port & sample process control
        ProcessDictionary["Turn on quartz furnace"] = TurnOnIpQuartzFurnace;
        ProcessDictionary["Turn off quartz furnace"] = TurnOffIpQuartzFurnace;
        ProcessDictionary["Disable sample setpoint ramping"] = DisableIpRamp;
        ProcessDictionary["Enable sample setpoint ramping"] = EnableIpRamp;
        ProcessDictionary["Turn on sample furnace"] = TurnOnIpSampleFurnace;
        ProcessDictionary["Adjust sample setpoint"] = AdjustIpSetpoint;
        ProcessDictionary["Adjust sample ramp rate"] = AdjustIpRampRate;
        ProcessDictionary["Wait for sample to rise to setpoint"] = WaitIpRiseToSetpoint;
        ProcessDictionary["Wait for sample to fall to setpoint"] = WaitIpFallToSetpoint;
        ProcessDictionary["Turn off sample furnace"] = TurnOffIpSampleFurnace;
        Separators.Add(ProcessDictionary.Count);

        // General-purpose process control actions
        ProcessDictionary["Wait for timer"] = WaitForTimer;
        ProcessDictionary["Wait for operator"] = WaitForOperator;
        ProcessDictionary["Wait for CEGS to be free"] = WaitForCegs;
        ProcessDictionary["Start Extract, Etc"] = StartExtractEtc;
        Separators.Add(ProcessDictionary.Count);

        // Transferring CO2
        ProcessDictionary["Transfer CO2 from CT to VTT"] = TransferCO2FromCTToVTT;
        ProcessDictionary["Transfer CO2 from MC to VTT"] = TransferCO2FromMCToVTT;
        ProcessDictionary["Transfer CO2 from MC to GR"] = TransferCO2FromMCToGR;
        ProcessDictionary["Transfer CO2 from prior GR to MC"] = TransferCO2FromGRToMC;
        Separators.Add(ProcessDictionary.Count);

        // Flow control steps
        ProcessDictionary["No IP flow"] = NoIpFlow;
        ProcessDictionary["Use IP flow"] = UseIpFlow;
        ProcessDictionary["Include CO2 analyzer"] = IncludeCO2Analyzer;
        ProcessDictionary["Bypass CO2 analyzer"] = BypassCO2Analyzer;
        ProcessDictionary["Select CT1"] = SelectCT1;
        ProcessDictionary["Select CT2"] = SelectCT2;
        ProcessDictionary["Backfill TF with He"] = BackfillTF1WithHe;
        ProcessDictionary["Notify to load TF"] = LoadTF;
        ProcessDictionary["Admit O2 to TF"] = AdmitO2toTF;
        ProcessDictionary["Open TF to IP1"] = OpenTF_IP1;
        ProcessDictionary["Start collecting"] = StartCollecting;
        ProcessDictionary["Clear collection conditions"] = ClearCollectionConditions;
        ProcessDictionary["Collect until condition met"] = CollectUntilConditionMet;
        ProcessDictionary["Toggle CT collection"] = ToggleCT;
        ProcessDictionary["Stop collecting"] = StopCollecting;
        ProcessDictionary["Stop collecting after bleed down"] = StopCollectingAfterBleedDown;
        Separators.Add(ProcessDictionary.Count);

        // Flow control sub-steps
        ProcessDictionary["Start flow through to trap"] = StartFlowThroughToTrap;
        ProcessDictionary["Start flow through to waste"] = StartFlowThroughToWaste;
        ProcessDictionary["Stop flow-through gas"] = StopFlowThroughGas;
        Separators.Add(ProcessDictionary.Count);

        // d13C port service routines
        //ProcessDictionary["Empty completed d13C ports"] = EmptyCompleted_d13CPorts;
        //ProcessDictionary["Thaw frozen d13C ports"] = ThawFrozen_d13CPorts;
        //ProcessDictionary["Load empty d13C ports"] = LoadEmpty_d13CPorts;
        //ProcessDictionary["Prepare loaded d13C ports"] = PrepareLoaded_d13CPorts;
        //Separators.Add(ProcessDictionary.Count);

        // Utilities (generally not for sample processing)
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Exercise all Opened valves"] = ExerciseAllValves;
        ProcessDictionary["Close all Opened valves"] = CloseAllValves;
        ProcessDictionary["Exercise all LN Manifold valves"] = ExerciseLNValves;
        ProcessDictionary["Calibrate all multi-turn valves"] = CalibrateRS232Valves;
        ProcessDictionary["Measure MC volume (KV in MCP1)"] = MeasureVolumeMC;
        ProcessDictionary["Measure valve volumes (plug in MCP1)"] = MeasureValveVolumes;
        ProcessDictionary["Measure remaining chamber volumes"] = MeasureRemainingVolumes;
        ProcessDictionary["Check GR H2 density ratios"] = CalibrateGRH2;
        ProcessDictionary["Measure Extraction efficiency"] = MeasureExtractEfficiency;
        ProcessDictionary["Measure IP collection efficiency"] = MeasureIpCollectionEfficiency;

        // Test functions
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Test"] = Test;
    }

    #region OpenLine

    /// <summary>
    /// Open and evacuate the entire vacuum line. This establishes
    /// the baseline system state: the condition it is normally left in
    /// when idle, and the expected starting point for major
    /// processes such as running samples.
    /// </summary>
    protected override void OpenLine()
    {
        ProcessStep.Start("Close gas supplies");
        CloseGasSupplies();
        ProcessStep.End();

        OpenVS1Line();
        OpenVS2Line();

        ProcessStep.Start($"Wait for both VacuumSystems to reach {OkPressure} Torr");
        WaitFor(() => VacuumSystem.Pressure <= OkPressure && VacuumSystem2.Pressure <= OkPressure);
        ProcessStep.End();

        ProcessStep.Start("Join VacuumSystem1 and VacuumSystem2 lines");
        Section.Connections(VacuumSystem.MySection, VacuumSystem2.MySection).Open();
        ProcessStep.End();

        ProcessStep.Start($"Isolate {CA.Name} (temp. due to leak)");
        CA.Isolate();
        ProcessStep.End();

    }

    /// <summary>
    /// Open and evacuate the chambers normally serviced by VacuumSystem1
    /// </summary>
    protected virtual void OpenVS1Line()
    {
        ProcessStep.Start("Open VacuumSystem1 line");
        OpenLine(VacuumSystem);
        ProcessStep.End();
    }
    /// <summary>
    /// Open and evacuate the chambers normally serviced by VacuumSystem1 including the TF
    /// </summary>
    protected virtual void OpenTF_VS1()
    {
        ProcessStep.Start("Open and evacuate TF and VS1");
        TF.Evacuate(IpEvacuationPressure);
        TF_IP1.Open();
        Find<InletPort>("IP1").Open();
        OpenVS1Line();
        ProcessStep.End();
    }

    /// <summary>
    /// Open and evacuate the chambers normally serviced by VacuumSystem2
    /// </summary>
    protected virtual void OpenVS2Line()
    {
        ProcessStep.Start("Open VacuumSystem2 line");
        OpenLine(VacuumSystem2);
        ProcessStep.End();
    }

    #endregion OpenLine

    /// <summary>
    /// Whenever the MC sample measurement (in ugC) changes,
    /// notify subscribers that umolCinMC has changed as well.
    /// </summary>
    protected override void UpdateSampleMeasurement(object sender = null, PropertyChangedEventArgs e = null)
    {
        var ugC = ugCinMC.Value;
        base.UpdateSampleMeasurement(sender, e);
        if (ugCinMC.Value != ugC)
            NotifyPropertyChanged(nameof(umolCinMC));
    }

    #region Process Control Parameters

    /// <summary>
    /// The desired Tube Furnace  pressure, used for filling or flow management.
    /// </summary>
    public double TfPressureTarget => GetParameter("TfPressureTarget");

    /// <summary>
    /// Stop collecting into the coil trap when amount of carbon in the Coil Trap reaches this value,
    /// provided that it is a number (i.e., not NaN).
    /// </summary>
    public double CollectUntilUgc => GetParameter("CollectUntilUgc");

    #endregion Process Control Parameters

    #region Process Control Properties

    public virtual bool IpIsTubeFurnace => InletPort.SampleFurnace is TubeFurnace;

    /// <summary>
    /// Change the Tube Furnace setpoint at a controlled
    /// ramp rate, rather than immediately to the given value.
    /// </summary>
    public virtual bool EnableTfSetpointRamp { get; set; } = false;

    /// <summary>
    /// Provide a flow of oxygen through the Inlet Port to combust the sample,
    /// instead of a fixed pressure.
    /// </summary>
    public virtual bool NeedIpFlow { get; set; } = false;

    /// <summary>
    /// Direct the sample gas through the CO2 analyzer during collection.
    /// </summary>
    public virtual bool NeedAnalyzer { get; set; } = false;

    /// <summary>
    /// The coil trap currently being used to trap sample gas.
    /// </summary>
    public ISection CurrentCT => base.IM_FirstTrap.Chambers.Contains(ChamberCT1) ? CT1 : CT2;

    /// <summary>
    /// A CEGS task dispatched to run concurrently while the main 
    /// sample process sequence continues. There can be only one.
    /// The concurrent actions take place in the VTT or beyond.
    /// </summary>
    public Task CegsTask { get; set; }

    /// <summary>
    /// A Collection task dispatched to run concurrently while
    /// the main process sequence continues. There can be only one.
    /// </summary>

    public Task CollectionTask { get; set; }

    /// <summary>
    /// Estimated of the amount of CO2 that has been collected in the CT so far...
    /// </summary>
    protected virtual double CollectedUgc { get; set; } = 0;

    #endregion Process Control Properties

    #region Process Steps

    /// <summary>
    /// Use a flow of oxygen through the Inlet Port to combust the sample.
    /// </summary>
    protected virtual void UseIpFlow() => NeedIpFlow = true;

    /// <summary>
    /// Provide a fixed amount (pressure) of oxygen into the Inlet Port
    /// to combust the sample.
    /// </summary>
    protected virtual void NoIpFlow() => NeedIpFlow = false;

    /// <summary>
    /// Fill the Tube Furnace chamber with helium in preparation
    /// for opening.
    /// </summary>
    protected virtual void BackfillTF1WithHe()
    {
        ProcessStep.Start($"Fill {InletPort.Name} to {pAmbient.Pressure:0} Torr He");

        // Need to manage FTG gas source valve manually,
        // because we want the shutoff to be
        // downstream of the flow valve.
        var he = Find<IValve>("vHe_FTG");

        FTG_IP1.Isolate();
        var gs = InertGasSupply(TF);
        he.OpenWait();
        gs.FlowPressurize(pAmbient.Pressure);
        gs.ShutOff();
        he.CloseWait();

        ProcessStep.End();
    }

    /// <summary>
    /// Notify the operator to load the tube furnace and
    /// wait for their 'Ok" to continue.
    /// </summary>
    protected virtual void LoadTF()
    {
        var subject = "Ready For Operator";
        var message = "Load the Tube Furnace and seal it closed.";

        Notify.Warn(message, subject, NoticeType.Information);
    }

    /// <summary>
    /// Evacuate the Inlet Port to 'OkPressure'.
    /// </summary>
    protected override void EvacuateIP()
    {
        ProcessStep.Start($"Evacuate {InletPort.Name}");

        if (IpIsTubeFurnace)
            TF_IP1.Open();
        base.EvacuateIP(IpEvacuationPressure);

        ProcessStep.End();
    }

    /// <summary>
    /// Admit O2 into the tube furnace to reach a pressure of 
    /// TfPressureTarget. 
    /// </summary>
    protected virtual void AdmitO2toTF()
    {
        ProcessStep.Start($"Admit {TfPressureTarget:0} Torr O2 into {InletPort.Name}");

        var gs = GasSupply("O2", TF);
        // Need to manage FTG gas source valve manually,
        // because we want the shutoff (vFTG_TF) to be
        // downstream of the flow valve.
        var o2 = Find<IValve>("vO2_FTG");
        o2.OpenWait();
        gs.FlowPressurize(TfPressureTarget);        // normalization not possible
        o2.CloseWait();

        ProcessStep.End();
    }

    /// <summary>
    /// Start flowing O2 through the InletPort to vacuum. Include the analyzer (and 
    /// warm coil trap) in the path to vacuum, unless the InletPort is the TubeFurnace.
    /// </summary>
    protected virtual void StartFlowThroughToWaste() => StartFlowThrough(false);

    /// <summary>
    /// Start flowing O2 through the Inlet Port and the frozen coil trap.
    /// </summary>
    protected virtual void StartFlowThroughToTrap() => StartFlowThrough(true);

    /// <summary>
    /// Start flowing O2 through the Inlet Port.
    /// </summary>
    protected virtual void StartFlowThrough(bool trap)
    {
        ProcessStep.Start($"Start flowing O2 through {InletPort.Name}");

        var source = IpIsTubeFurnace ? FTG_IP1 : FTG_IP2;
        var gasfm = IpIsTubeFurnace ? FTG_TFFlowManager : FTG_IMFlowManager;
        // Need to manage the upstream FTG gas supply valve manually, because we want the shutoff to be
        // downstream of the flow valve.
        var o2 = Find<IValve>("vO2_FTG");

        ProcessStep.Start($"Isolate and open {source.Name}");
        source.Isolate();
        source.Open();
        ProcessStep.End();

        gasfm.FlowValve.CloseWait();

        if (IpIsTubeFurnace && !trap)
        {
            var vacfm = TFFlowManager;
            Find<IValve>("vTFBypass")?.CloseWait();
            vacfm.FlowValve.CloseWait();
            IM.Isolate();
            InletPort.Open();
            IM.OpenAndEvacuate();
            o2.OpenWait();
            gasfm.Start(TF.Pressure);	// get the O2 flowing
            gasfm.Stop();				// fix the O2 flow at its present rate
            vacfm.Start(TF.Pressure);   // Manage drain flow to maintain TF pressure
        }
        else
        {
            StartSampleFlow(trap);          // Manage CT flow to maintain bleed pressure
            o2.OpenWait();
            gasfm.Start(IM.Pressure);   // Manage supply flow to maintain IM pressure
        }


        ProcessStep.End();
    }

    /// <summary>
    /// Stop flowing O2 into the Inlet Port.
    /// </summary>
    protected virtual void StopFlowThroughGas()
    {
        ProcessStep.Start($"Stopping O2 flow into {InletPort.Name}");

        var gasfm = IpIsTubeFurnace ? FTG_TFFlowManager : FTG_IMFlowManager;
        var vacfm = IpIsTubeFurnace ? TFFlowManager : null;
        var supplyValve = Find<IValve>("vO2_FTG");
        var gasSupply = GasSupply("O2", IpIsTubeFurnace ? TF : IM);

        gasfm.Stop();
        gasfm.FlowValve.CloseWait();
        vacfm?.Stop();
        vacfm?.FlowValve.CloseWait();
        gasSupply.ShutOff();
        supplyValve.CloseWait();

        ProcessStep.End();
    }

    /// <summary>
    /// Open the TF outlet valves, joining TF to IP1
    /// </summary>
    protected virtual void OpenTF_IP1() => TF_IP1.Open();

    /// <summary>
    /// Include the CO2 Analyzer in the sample gas collection path.
    /// </summary>
    protected virtual void IncludeCO2Analyzer() => NeedAnalyzer = true;

    /// <summary>
    /// Bypass the CO2 Analyzer when collecting sample gas.
    /// </summary>
    protected virtual void BypassCO2Analyzer() => NeedAnalyzer = false;

    /// <summary>
    /// Use Coil Trap 1 for sample collection;
    /// </summary>
    protected virtual void SelectCT1() => base.IM_FirstTrap = NeedAnalyzer ? IM_CA_CT1 : IM_CT1;

    /// <summary>
    /// Use Coil Trap 2 for sample collection.
    /// </summary>
    protected virtual void SelectCT2() => base.IM_FirstTrap = NeedAnalyzer ? IM_CA_CT2 : IM_CT2;

    /// <summary>
    /// Switch coil traps.
    /// </summary>
    protected virtual void ToggleCT()
    {
        ProcessStep.Start($"Toggle CT");

        if (CT == CT1)
            SelectCT2();
        else
            SelectCT1();
        StartCollecting();

        ProcessStep.End();
    }

    /// <summary>
    /// Set all collection condition parameters to NaN
    /// </summary>
    protected override void ClearCollectionConditions()
    {
        base.ClearCollectionConditions();
        ClearParameter("CollectUntilUgc");
    }

    string stoppedBecause = "";
    /// <summary>
    /// Wait for a collection stop condition to occur.
    /// </summary>
    protected override void CollectUntilConditionMet()
    {
        ProcessStep.Start($"Wait for a collection stop condition");

        bool shouldStop()
        {
            if (CollectStopwatch.IsRunning && CollectStopwatch.ElapsedMilliseconds < 1000)
                return false;

            // TODO: what if flow manager becomes !Busy (because, e.g., FlowValve is fully open)?
            // TODO: should we invoke DuringBleed()? When?
            // TODO: should we disable/enable CT.VacuumSystem.Manometer?

            // Open flow bypass when conditions allow it without producing an excessive
            // downstream pressure spike.
            if (IM.Pressure - FirstTrap.Pressure < FirstTrapFlowBypassPressure)
                FirstTrap.Open();   // open bypass if available


            if (CollectCloseIpAtPressure.IsANumber() && InletPort.IsOpened && IM.Pressure <= CollectCloseIpAtPressure)
            {
                var p = IM.Pressure;
                InletPort.Close();
                SampleLog.Record($"{Sample.LabId}\tClosed {InletPort.Name} at {IM.Manometer.Name} = {p:0} Torr");
            }
            if (CollectCloseIpAtCtPressure.IsANumber() && InletPort.IsOpened && FirstTrap.Pressure <= CollectCloseIpAtCtPressure)
            {
                var p = FirstTrap.Pressure;
                InletPort.Close();
                SampleLog.Record($"{Sample.LabId}\tClosed {InletPort.Name} at {FirstTrap.Manometer.Name} = {p:0} Torr");
            }

            if (Stopping)
            {
                stoppedBecause = "CEGS is shutting down";
                return true;
            }
            if (CollectUntilTemperatureRises.IsANumber() && InletPort.Temperature >= CollectUntilTemperatureRises)
            {
                stoppedBecause = $"InletPort.Temperature rose to {CollectUntilTemperatureRises:0} °C";
                return true;
            }
            if (CollectUntilTemperatureFalls.IsANumber() && InletPort.Temperature <= CollectUntilTemperatureFalls)
            {
                stoppedBecause = $"InletPort.Temperature fell to {CollectUntilTemperatureFalls:0} °C";
                return true;
            }

            if (CollectUntilCtPressureFalls.IsANumber() &&
                FirstTrap.Pressure <= CollectUntilCtPressureFalls &&
                IM.Pressure < Math.Ceiling(CollectUntilCtPressureFalls) + 2)
            {
                stoppedBecause = $"{FirstTrap.Name}.Pressure fell to {CollectUntilCtPressureFalls:0.00} Torr";
                return true;
            }

            // old?: FirstTrap.Pressure < FirstTrapEndPressure;
            if (FirstTrapEndPressure.IsANumber() &&
                FirstTrap.Pressure <= FirstTrapEndPressure &&
                IM.Pressure < Math.Ceiling(FirstTrapEndPressure) + 2)
            {
                stoppedBecause = $"{FirstTrap.Name}.Pressure fell to {FirstTrapEndPressure:0.00} Torr";
                return true;
            }
            if (CollectUntilUgc.IsANumber() && CollectedUgc >= CollectUntilUgc)
            {
                stoppedBecause = $"Collected {CollectUntilUgc:0} µg C";
                return true;
            }
            if (CollectUntilMinutes.IsANumber() && CollectStopwatch.Elapsed.TotalMinutes >= CollectUntilMinutes)
            {
                stoppedBecause = $"{MinutesString((int)CollectUntilMinutes)} elapsed";
                return true;
            }

            stoppedBecause = "";
            return false;
        }
        WaitFor(shouldStop, -1, 1000);
        SampleLog.Record($"{Sample.LabId}\tStopped collecting:\t{stoppedBecause}");

        ProcessStep.End();
    }

    // TODO take a look at trying to use the base version.
    /// <summary>
    /// Stop collecting. If 'immediately' is false, wait for CT pressure to bleed down after closing IP
    /// </summary>
    /// <param name="immediately">If false, wait for CT pressure to bleed down after closing IP</param>
    protected override void StopCollecting(bool immediately = true)
    {
        ProcessStep.Start("Stop Collecting");

        CT = CurrentCT;     // The VTT will take it from here
        CT.FlowManager?.Stop();
        InletPort.Close();
        if (!immediately)
            FinishCollecting();
        IM_FirstTrap.Close();
        CT.Isolate();
        CT.FlowValve.CloseWait();

        ProcessStep.End();
    }

    protected override void Collect()
    {
        VacuumSystem.MySection.Isolate();
        IM_FirstTrap.Isolate();
        IM_FirstTrap.FlowValve.OpenWait();
        IM_FirstTrap.OpenAndEvacuate(OkPressure);

        StartCollecting();
        CollectUntilConditionMet();
        StopCollecting(false);
        InletPort.State = LinePort.States.Complete;

        TransferCO2FromCTToVTT();
    }

    /// <summary>
    /// Wait for the CEGS to be ready to process a sample.
    /// ("CEGS" in this case is the section from the VTT onward.)
    /// </summary>
    protected virtual void WaitForCegs()
    {
        ProcessStep.Start("Wait for CEGS to be ready to process sample");

        bool cegsAvailable()
        {
            if (Stopping)
                return true;
            return CegsTask?.IsCompleted ?? true;
        }
        WaitFor(cegsAvailable, -1, 1000);

        ProcessStep.End();
    }

    /// <summary>
    /// Initiate the ExtractEtc process step, followed by evacuating VS2,
    /// to run concurrently while the Collection process continues.
    /// </summary>
    protected virtual void StartExtractEtc()
    {
        CegsTask = Task.Run(ExtractEtcThenEvacuateVS2);
    }

    /// <summary>
    /// Run the ExtractEtc process step, then evacuate VS2.
    /// </summary>
    protected virtual void ExtractEtcThenEvacuateVS2()
    {
        ProcessStep.Start($"Extract from {CT.Name}");

        TransferCO2FromCTToVTT();
        ExtractEtc();
        OpenVS2Line();
        VacuumSystem2.WaitForPressure(OkPressure);

        ProcessStep.End();
    }

    protected void RampedOxidation()
    {
        FlushIP();
        EvacuateIP(CleanPressure);

        IM.ClosePortsExcept(InletPort);
        while (PortLeakRate(InletPort) > LeakTightTorrLitersPerSecond)
        {
            var subject = "Sample Alert";
            var message = $"{InletPort.Name} is leaking.\r\n" +
                          $"Process Paused.\r\n" +
                          $"Ok to try again or\r\n" +
                          $"Restart the application to abort the process.";

            Notify.Warn(message, subject);
        }

        ProcessStep.Start($"Heat Quartz");
        TurnOnIpQuartzFurnace();
        WaitMinutes((int)QuartzFurnaceWarmupMinutes);
        ProcessStep.End();

        SelectCT1();
        SetParameter("IpRampRate", 5);
        SetParameter("IpSetpoint", 1000);
        IncludeCO2Analyzer();
        UseIpFlow();
        EnableIpRamp();

        StartFlowThroughToTrap();
        TurnOnIpSampleFurnace();
        ClearCollectionConditions();
        SetParameter("CollectUntilTemperatureRises", 150);
        CollectUntilConditionMet();
        WaitForCegs();
        StartExtractEtc();

        ToggleCT();
        SetParameter("CollectUntilTemperatureRises", 200);
        CollectUntilConditionMet();
        StopCollecting();
        WaitForCegs();
        StartExtractEtc();
        WaitForCegs();

        ToggleCT();
        SetParameter("CollectUntilTemperatureRises", 500);
        CollectUntilConditionMet();
        StopCollecting();
        WaitForCegs();
        StartExtractEtc();
        WaitForCegs();

        ToggleCT();
        SetParameter("CollectUntilTemperatureRises", 750);
        CollectUntilConditionMet();
        StopCollecting();
        WaitForCegs();
        StartExtractEtc();
        WaitForCegs();

        ToggleCT();
        SetParameter("CollectUntilTemperatureRises", 1000);
        CollectUntilConditionMet();
        StopCollecting();
        WaitForCegs();
        StartExtractEtc();
        WaitForCegs();

        OpenLine();
    }

    /// <summary>
    /// In situ quartz sample process, Day 1 (preparation)
    /// </summary>
    protected virtual void Day1()
    {
        TurnOnIpQuartzFurnace();
        BackfillTF1WithHe();
        LoadTF();
        SetParameter("IpEvacuationPressure", 1E-2);
        EvacuateIP();
        SetParameter("TfPressureTarget", 50);
        AdmitO2toTF();
        SetParameter("IpSetpoint", 100);
        TurnOnIpSampleFurnace();
        WaitIpRiseToSetpoint();
        StartFlowThroughToWaste();
        SetParameter("WaitTimerMinutes", 2);
        WaitForTimer();
        StopFlowThroughGas();
        EvacuateIP();
        AdmitO2toTF();
        StartFlowThroughToWaste();
        SetParameter("IpSetpoint", 95);
        TurnOffIpSampleFurnace();
        WaitIpFallToSetpoint();
        StopFlowThroughGas();
        TurnOffIpQuartzFurnace();
        OpenTF_VS1();
    }

    /// <summary>
    /// In situ quartz sample process, Day 2 (extraction)
    /// </summary>
    protected virtual void Day2()
    {
        TurnOnIpQuartzFurnace();
        BackfillTF1WithHe();
        LoadTF();
        EvacuateIP();
        SetParameter("TfPressureTarget", 50);
        AdmitO2toTF();
        SetParameter("IpSetpoint", 75);
        TurnOnIpSampleFurnace();
        WaitIpRiseToSetpoint();
        StartFlowThroughToWaste();
        SetParameter("WaitTimerMinutes", 1);
        WaitForTimer();
        StopFlowThroughGas();
        EvacuateIP();
        AdmitO2toTF();
        SetParameter("IpSetpoint", 150);
        AdjustIpSetpoint();
        WaitIpRiseToSetpoint();
        SetParameter("WaitTimerMinutes", 1);
        WaitForTimer();
        TurnOffIpSampleFurnace();
        BypassCO2Analyzer();
        SelectCT1();
        SetParameter("FirstTrapBleedPressure", 10);
        StartFlowThroughToTrap();
        SetParameter("CollectUntilTemperatureFalls", 80);
        CollectUntilConditionMet();
        StopFlowThroughGas();
        OpenTF_IP1();
        ClearCollectionConditions();
        SetParameter("CollectUntilCtPressureFalls", 4.0);
        CollectUntilConditionMet();
        StopCollecting();
        ExtractEtcThenEvacuateVS2();
        OpenLine();
    }


    /// <summary>
    /// Tube furnace bakeout procedure.
    /// </summary>
    protected void Bakeout()
    {
        EvacuateIP();
        SetParameter("TfPressureTarget", 50);
        AdmitO2toTF();
        SetParameter("IpSetpoint", 500);
        TurnOnIpSampleFurnace();
        WaitIpRiseToSetpoint();
        StartFlowThroughToWaste();
        SetParameter("WaitTimerMinutes", 10);
        WaitForTimer();
        StopFlowThroughGas();
        EvacuateIP();
        AdmitO2toTF();
        SetParameter("IpSetpoint", 1000);
        AdjustIpSetpoint();
        WaitIpRiseToSetpoint();
        SetParameter("WaitTimerMinutes", 2);
        WaitForTimer();
        TurnOffIpSampleFurnace();
        OpenLine();
    }

    #endregion Process Steps

    #endregion Process Management

    #region Test functions

    /// <summary>
    /// General-purpose code tester. Put whatever you want here.
    /// </summary>
    protected override void Test()
    {
        //FlushIP();
        //EvacuateIP(CleanPressure);

        //IM.ClosePortsExcept(InletPort);
        //while (PortLeakRate(InletPort) > LeakTightTorrLitersPerSecond)
        //    Pause("Sample Alert", $"{InletPort.Name} is leaking. Process Paused. Close Program to abort, or Ok to try again");
        EvacuateIP();

        ProcessStep.Start($"Heat Quartz");
        TurnOnIpQuartzFurnace();
        //WaitMinutes((int)QuartzFurnaceWarmupMinutes);
        ProcessStep.End();

        SelectCT1();
        SetParameter("IpRampRate", 5);
        SetParameter("IpSetpoint", 1000);
        IncludeCO2Analyzer();
        UseIpFlow();
        EnableIpRamp();

        StartFlowThroughToTrap();
        TurnOnIpSampleFurnace();
        ClearCollectionConditions();
        SetParameter("CollectUntilTemperatureRises", 75);
        CollectUntilConditionMet();
        WaitForCegs();
        StartExtractEtc();

        ToggleCT();
        SetParameter("CollectUntilTemperatureRises", 150); // 15 min
        CollectUntilConditionMet();
        StopCollecting();
        WaitForCegs();
        StartExtractEtc();
        WaitForCegs();

        OpenLine();
    }

    #endregion Test functions
}