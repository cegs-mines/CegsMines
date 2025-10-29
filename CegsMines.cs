using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static AeonHacs.Components.CegsPreferences;
using static AeonHacs.Notify;
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
        FTG_IM = Find<Section>("FTG_IM");
        IM_CT1 = Find<Section>("IM_CT1");
        IM_CT2 = Find<Section>("IM_CT2");
        IM_CA_CT1 = Find<Section>("IM_CA_CT1");
        IM_CA_CT2 = Find<Section>("IM_CA_CT2");

        CA1 = Find<SableCA10>("CA1");
        TF1 = Find<TcpTubeFurnace>("TF1");
        LnScale1 = Find<LNScale>("LnScale1");
        pAmbient = Find<AIManometer>("pAmbient");
        mfcHe = Find<ManagedMFC>("mfcHe");
        mfcO2 = Find<ManagedMFC>("mfcO2");
        TFFlowManager = Find<FlowManager>("TFFlowManager");

        // Select the default Coil Trap
        SelectCT1();

        CtFlowMonitor = Find<FlowMonitor>("CtFlowMonitor");
        CollectedUgc = Find<Meter>("CollectedUgc");
    }

    [HacsPostConnect]
    protected override void PostConnect()
    {
        base.PostConnect();
        CA1.PropertyChanged += UpdateCollectedCO2;
        CtFlowMonitor.PropertyChanged += UpdateCollectedCO2;

        mfcHe.PropertyChanged += UpdateFlowRate;
        mfcO2.PropertyChanged += UpdateFlowRate;
    }

    #endregion HacsComponent

    #region System configuration

    #region HacsComponents

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
    /// Flow-Through Gas..Tube Furnace section
    /// </summary>
    public ISection FTG_TF { get; set; }

    /// <summary>
    /// Flow-Through Gas..Inlet Manifold section
    /// </summary>
    public ISection FTG_IM { get; set; }

    /// <summary>
    /// Flow-Through Gas..Carbon Analyzer section
    /// </summary>
    public ISection FTG_CA { get; set; }

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
    /// Mass Flow Controller for He to FTG section
    /// </summary>
    public ManagedMFC mfcHe { get; set; }

    /// <summary>
    /// Mass Flow Controller for O2 to FTG section
    /// </summary>
    public ManagedMFC mfcO2 { get; set; }

    /// <summary>
    /// Flow manager for gas out of the tube furnace.
    /// </summary>
    public FlowManager TFFlowManager { get; set; }

    public FlowMonitor CtFlowMonitor { get; set; }
    public Meter CollectedUgc { get; set; }

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
        ProcessDictionary["Prepare loaded inlet ports for collection"] = PrepareIPsForCollection;
        Separators.Add(ProcessDictionary.Count);

        ProcessDictionary["Reload completed d13C ports"] = Reload_d13CPorts;
        ProcessDictionary["Prepare loaded d13C ports"] = PrepareLoaded_d13CPorts;
        Separators.Add(ProcessDictionary.Count);

        ProcessDictionary["Prepare carbonate sample for acid"] = PrepareCarbonateSample;
        ProcessDictionary["Load acidified carbonate sample"] = LoadCarbonateSample;
        Separators.Add(ProcessDictionary.Count);

        // Open line
        ProcessDictionary["Open and evacuate line"] = OpenLine;
        ProcessDictionary["Open and evacuate VS1"] = () => Find<VacuumSystem>("VacuumSystem1").OpenLine();
        ProcessDictionary["Open and evacuate VS2"] = () => Find<VacuumSystem>("VacuumSystem2").OpenLine();
        ProcessDictionary["Open and evacuate TF and VS1"] = OpenTF_VS1;
        Separators.Add(ProcessDictionary.Count);

        // Sample Process Methods
        ProcessDictionary["Tube furnace bakeout"] = Bakeout;
        ProcessDictionary["In situ quartz, day 1"] = Day1;
        ProcessDictionary["In situ quartz, day 2"] = Day2;
        ProcessDictionary["Ramped oxidation"] = RampedOxidation;
        Separators.Add(ProcessDictionary.Count);

        // Main process continuations
        ProcessDictionary["Collect, etc."] = CollectEtc;
        ProcessDictionary["Extract, etc."] = ExtractEtc;
        ProcessDictionary["Measure, etc."] = MeasureEtc;
        ProcessDictionary["Graphitize, etc."] = GraphitizeEtc;
        Separators.Add(ProcessDictionary.Count);

        // Top-level steps for main process sequence
        ProcessDictionary["Admit sealed CO2 to InletPort"] = AdmitSealedCO2IP;
        ProcessDictionary["Collect CO2 from InletPort"] = Collect;
        ProcessDictionary["Extract"] = Extract;
        ProcessDictionary["Measure"] = Measure;
        ProcessDictionary["Discard excess CO2 by splits"] = DiscardSplit;
        ProcessDictionary["Remove sulfur"] = RemoveSulfur;
        ProcessDictionary["Dilute small sample"] = Dilute;
        ProcessDictionary["Graphitize aliquots"] = GraphitizeAliquots;
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
        ProcessDictionary["Stop collecting immediately"] = StopCollectingImmediately;
        ProcessDictionary["Stop collecting after bleed down"] = StopCollectingAfterBleedDown;
        ProcessDictionary["Evacuate and Freeze first trap"] = FreezeFirstTrap;
        ProcessDictionary["Evacuate and Freeze VTT"] = FreezeVtt;
        ProcessDictionary["Admit Dead CO2 into MC"] = AdmitDeadCO2;
        ProcessDictionary["Purify CO2 in MC"] = CleanupCO2InMC;
        ProcessDictionary["Discard MC gases"] = DiscardMCGases;
        ProcessDictionary["Divide sample into aliquots"] = DivideAliquots;
        Separators.Add(ProcessDictionary.Count);

        // Granular inlet port & sample process control
        ProcessDictionary["Reset Inlet Port to Loaded"] = ResetIpToLoaded;
        ProcessDictionary["Freeze the Inlet Port"] = FreezeIp;
        ProcessDictionary["Raise LN on the Inlet Port"] = RaiseLNIp;
        ProcessDictionary["Thaw the Inlet Port"] = ThawIp;
        ProcessDictionary["Raise IP furnaces"] = RaiseIpFurnaces;
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
        ProcessDictionary["Wait for IP timer"] = WaitIpMinutes;
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

        // Flow control steps & special collection operations
        ProcessDictionary["Reset tracked flow and collected µgC."] = ResetUgcTracking;
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
        ProcessDictionary["Exercise all Opened valves"] = ExerciseAllValves;
        ProcessDictionary["Close all Opened valves"] = CloseAllValves;
        ProcessDictionary["Exercise all LN Manifold valves"] = ExerciseLNValves;
        ProcessDictionary["Calibrate all multi-turn valves"] = CalibrateRS232Valves;
        ProcessDictionary["Measure MC volume (KV in MCP1)"] = MeasureVolumeMC;
        ProcessDictionary["Measure valve volumes (plug in MCP1)"] = MeasureValveVolumes;
        ProcessDictionary["Measure remaining chamber volumes"] = MeasureRemainingVolumes;
        ProcessDictionary["Check GR H2 density ratios"] = CalibrateGRH2;
        ProcessDictionary["Calibrate VP He initial manifold pressure"] = CalibrateVPHeP0;
        ProcessDictionary["Measure Extraction efficiency"] = MeasureExtractEfficiency;
        ProcessDictionary["Measure IP collection efficiency"] = MeasureIpCollectionEfficiency;
        Separators.Add(ProcessDictionary.Count);

        // Test functions
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

        var vacuumSystems = VacuumSystems.Values.ToList();

        // Which do we want? Do we want to be able to choose?
        //vacuumSystems.ForEach(base.OpenLine);           // thaws coldfingers
        vacuumSystems.ForEach(vs => vs.OpenLine());   // doesn't thaw coldfingers

        ProcessStep.Start($"Wait for all vacuum systems to reach {OkPressure} Torr");
        WaitFor(() => vacuumSystems.All(vs => vs.Pressure <= OkPressure));
        ProcessStep.End();

        ProcessStep.Start("Join vacuum system lines");
        // compute all pairs?
        Section.Connections(vacuumSystems.First().MySection, vacuumSystems.Last().MySection).Open();
        ProcessStep.End();

        ProcessStep.Start($"Isolate {CA.Name} (temp. due to leak)");
        CA.Isolate();
        ProcessStep.End();

    }

    /// <summary>
    /// Open and evacuate the chambers normally serviced by TF's VacuumSystem
    /// </summary>
    protected virtual void OpenTF_VS1()
    {
        var vacuumSystem = TF.VacuumSystem;
        ProcessStep.Start($"Open and evacuate TF and {vacuumSystem.Name}.");
        TF.Evacuate(IpEvacuationPressure);
        TF_IP1.Open();
        Find<InletPort>("IP1").Open();
        vacuumSystem.OpenLine();
        //OpenLine(vacuumSystem);     // Thaws coldfingers.
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

        // TODO decide whether to use the InertGasSupply instead of mfcHe.
        //var gs = InertGasSupply(TF);
        FtgPressurize("He", FTG_IP1, pAmbient.Pressure);
        ProcessStep.End();
    }

    /// <summary>
    /// Notify the operator to load the tube furnace and
    /// wait for their 'Ok" to continue.
    /// </summary>
    protected virtual void LoadTF() =>
        WaitForOperator("Load the Tube Furnace and seal it closed.");

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
        FtgPressurize("O2", FTG_IP1, TfPressureTarget);
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

        ProcessStep.Start($"Isolate and open section {source.Name}.");
        source.Isolate();
        source.Open();
        ProcessStep.End();

        mfcHe.Setpoint = 0;
        mfcO2.Setpoint = 0;

        if (IpIsTubeFurnace && !trap)
        {
            var vacfm = TFFlowManager;
            Find<IValve>("vTFBypass")?.CloseWait();
            vacfm.FlowValve.CloseWait();
            IM.Isolate();
            InletPort.Open();
            IM.OpenAndEvacuate();

            mfcHe.TurnOn(GetParameter("FlowThroughHe"));
            mfcO2.TurnOn(GetParameter("FlowThroughO2"));

            // Wait for a target TF pressure first?
            vacfm.Start(TF.Pressure);           // Manage drain flow to maintain TF pressure
        }
        else
        {
            IM_FirstTrap.FlowManager.StopOnFullyOpened = false;
            StartSampleFlow(trap);              // Manage CT flow to maintain bleed pressure

            mfcHe.TurnOn(GetParameter("FlowThroughHe"));
            WaitFor(() => Math.Abs(mfcHe.FlowRate - GetParameter("FlowThroughHe")) < 0.1, -1, 1000);

            mfcO2.TurnOn(GetParameter("FlowThroughO2"));
            WaitFor(() => Math.Abs(mfcO2.FlowRate - GetParameter("FlowThroughO2")) < 0.1, -1, 1000);
        }
        ProcessStep.End();
    }


    /// <summary>
    /// Stop flowing O2 into the Inlet Port.
    /// </summary>
    protected virtual void StopFlowThroughGas()
    {
        ProcessStep.Start($"Stopping O2 flow into {InletPort.Name}");

        var vacfm = IpIsTubeFurnace ? TFFlowManager : null;

        mfcHe.TurnOff();
        mfcO2.TurnOff();
        vacfm?.Stop();
        vacfm?.FlowValve.CloseWait();

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

    protected void ResetUgcTracking()
    {
        CtFlowMonitor.Reset();
        CollectedUgc.Update(0);
        ugCTrackingStopwatch.Restart();
    }

    protected override void StartCollecting()
    {
        ResetUgcTracking();
        base.StartCollecting();
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

        var maximumSampleTemperatureStopwatch = new Stopwatch();
        var maximumSampleTemperature = GetParameter("MaximumSampleTemperature");
        var minutesAtMaximumTemperature = GetParameter("MinutesAtMaximumTemperature");

        bool shouldStop()
        {
            if (CollectStopwatch.IsRunning && CollectStopwatch.ElapsedMilliseconds < 1000)
                return false;

            // TODO: what if flow manager becomes !Busy (because, e.g., FlowValve is fully open)?
            // TODO: should we invoke DuringBleed()? When? (replaced by CollectionActions, right?)
            // TODO: should we disable/enable CT.VacuumSystem.Manometer?

            // Open flow bypass when conditions allow it without producing an excessive
            // downstream pressure spike.
            if (IM.Pressure - FirstTrap.Pressure < FirstTrapFlowBypassPressure)
                FirstTrap.Open();   // open bypass if available
                                    // (REVISIT THIS, normally we don't want Open to
                                    // open the flow valve or bypass....


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

            if (maximumSampleTemperature.IsANumber() && minutesAtMaximumTemperature.IsANumber())
            {
                if (maximumSampleTemperatureStopwatch.IsRunning)
                {
                    if (maximumSampleTemperatureStopwatch.Elapsed.TotalMinutes > minutesAtMaximumTemperature)
                    {
                        stoppedBecause = $"Reached {minutesAtMaximumTemperature} minutes at maximum temperature ({maximumSampleTemperature} °C)";
                        return true;
                    }
                }
                else if (InletPort.Temperature >= maximumSampleTemperature)
                    maximumSampleTemperatureStopwatch.Restart();
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
        IM_FirstTrap.FlowManager?.Stop();
        InletPort.Close();
        if (!immediately)
            FinishCollecting();
        IM_FirstTrap.Close();
        CT.Isolate();
        IM_FirstTrap.FlowValve.CloseWait();

        ProcessStep.End();
    }

    protected override void Collect()
    {
        IM_FirstTrap.VacuumSystem.MySection.Isolate();
        IM_FirstTrap.Isolate();
        IM_FirstTrap.FlowValve.OpenWait();
        IM_FirstTrap.OpenAndEvacuate(OkPressure);
        IM_FirstTrap.FlowManager.StopOnFullyOpened = false;

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

    protected override void ExtractEtc()
    {
        var lnManifolds = FindAll<LNManifold>();
        lnManifolds.ForEach(m => m.StayActive());
        base.ExtractEtc();
        lnManifolds.ForEach(m => m.Monitor());
    }

    /// <summary>
    /// Initiate the ExtractEtc process step, followed by evacuating VS2,
    /// to run concurrently while the Collection process continues.
    /// </summary>
    protected virtual void StartExtractEtc()
    {
        CegsTask = Task.Run(ExtractEtcThenEvacuate);
    }

    /// <summary>
    /// Run the ExtractEtc process step, then evacuate the line.
    /// </summary>
    protected virtual void ExtractEtcThenEvacuate()
    {
        TransferCO2FromCTToVTT();
        ExtractEtc();
        var vacuumSystem = MC.VacuumSystem;
        vacuumSystem.OpenLine();        // do not wait for cold coldfingers
        vacuumSystem.WaitForPressure(OkPressure);
    }


    protected void RampedOxidation()
    {
        SetParameter("HoldSampleAtPorts", 1);

        FTG_IP2.Close();
        EvacuateIP(CleanPressure);
        InletPort.Close();
        IM.VacuumSystem.OpenLine(); // don't thaw coldfingers
        EvacuateIP(OkPressure);
        FlushIP();

        while (PortLeakRate(InletPort) > 2 * LeakTightTorrLitersPerSecond)
        {
            if (Warn($"The gas load at {InletPort.Name} is too high.",
                $"Ok to try again or Cancel to move on.\r\n" +
                $"Restart the application to abort the process.").Ok())
            {
                continue;
            }
            break;
        }

        //TODO change this to CleanPressure? It's OkPressure for process testing.
        EvacuateIP(OkPressure);

        ProcessStep.Start($"Heat Quartz");
        TurnOnIpQuartzFurnace();
        WaitMinutes((int)QuartzFurnaceWarmupMinutes);
        ProcessStep.End();

        IncludeCO2Analyzer();
        SelectCT1();
        UseIpFlow();
        StartFlowThroughToTrap();
        ResetUgcTracking();

        // TODO restore correct values
        //SetParameter("IpSetpoint", 1000);
        //SetParameter("IpRampRate", 5);
        SetParameter("IpSetpoint", 500);        // DEBUGGING VALUE
        SetParameter("IpRampRate", 50);         // DEBUGGING VALUE

        EnableIpRamp();
        TurnOnIpSampleFurnace();

        List<Sample> Splits = new() { Sample as Sample };
        var numberOfSplits = GetParameter("NumberOfSplits");
        if (!numberOfSplits.IsANumber())
            numberOfSplits = 1;

        for (int i = 1; i <= numberOfSplits; i++)
        {
            if (i > 1)
            {
                // Clone the sample for subsequent splits
                Sample = Sample.Clone();
                Sample.DateTime = DateTime.Now;
                Sample.MicrogramsDilutionCarbon = 0;
                Sample.TotalMicrogramsCarbon = 0;
                Sample.Discards = 0;
                Sample.SelectedMicrogramsCarbon = 0;
                Sample.Micrograms_d13C = 0;
                Sample.d13CPartsPerMillion = 0;
                Sample.Traps = "";
                //Sample.State = Components.Sample.States.InProcess;
                // Use autogeneratedgraphite number for Aliquot IDs?
                // If not, we need a scheme to make them unique.
                // Prefix with Sample.Name?

                Splits.Add(Sample as Sample);
                ToggleCT();
            }

            List<Action> stopConditions =
            [
                () =>
                {
                    var targetUgc = GetValidParameter($"Split{i}TargetUgc");
                    if (targetUgc.IsANumber())
                        SetParameter("CollectUntilUgc", targetUgc);
                    SetParameter("MaximumSampleTemperature", 1000);
                    SetParameter("MinutesAtMaximumTemperature", 10);
                },
            ];
            
            stopConditions.ForEach(setStopConditions =>
            {
                ClearCollectionConditions();
                setStopConditions();
                CollectUntilConditionMet();
            });

            WaitForCegs();
            StartExtractEtc();
        }

        Splits.ForEach(s =>
        {
            Sample = s;

            // Re-freeze and start graphite reactor(s) (typically only 1 per split).
            Sample.Aliquots.ForEach(a =>
                Find<GraphiteReactor>(a.GraphiteReactor).Coldfinger.Raise());
            GraphitizeAliquots();

            AddCarrierTo_d13C();
        });

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
        ExtractEtcThenEvacuate();
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
    protected virtual void FtgPressurize(string gasName, ISection destination, double pressure)
    {
        var mfc =
            gasName == "He" ? mfcHe :
            gasName == "O2" ? mfcO2 :
            default;

        // TODO handle errors (Notify?)
        if (mfc == default) return;
        if (!destination.Chambers.Contains(Find<Chamber>("FTG"))) return; // don't know how to get there

        destination.OpenAndEvacuate();
        destination.Isolate();
        
        mfc.TurnOn(mfc.MaximumSetpoint);            // add Pressurize(Meter, double) to mfc class
        WaitFor(() => destination.Pressure >= pressure);
        mfc.TurnOff();
    }

    #region Coil trap flow rate testing and estimation

    Stopwatch ugCTrackingStopwatch = new Stopwatch();
    double Co2Percent;
    double CtSccm;
    protected virtual void UpdateCollectedCO2(object sender, PropertyChangedEventArgs e)
    {
        void update()
        {
            var minutes = ugCTrackingStopwatch.Elapsed.TotalMinutes;
            ugCTrackingStopwatch.Restart();
            var sccmCo2 = CtSccm * minutes * Co2Percent / 100;
            var ugc = CollectedUgc + Particles(Torr, sccmCo2, MC.Temperature) / CarbonAtomsPerMicrogram;
            CollectedUgc.Update(ugc);
        }

        var property = e.PropertyName;
        if (sender == CA1 && property == nameof(CA1.CO2Percent) && Co2Percent != CA1.CO2Percent)
        {
            update();
            Co2Percent = CA1.CO2Percent;
        }
        else if (sender == CtFlowMonitor && property == nameof(CtFlowMonitor.FlowRate) && CtSccm != CtFlowMonitor.FlowRate)
        {
            update();
            CtSccm = CtFlowMonitor.FlowRate;
        }
    }

    protected virtual void UpdateFlowRate(object sender, PropertyChangedEventArgs e)
    {
        var property = e.PropertyName;
        if (sender is IMassFlowController && property == nameof(IMassFlowController.FlowRate))
            CtFlowMonitor.FlowRateMeter.Update(mfcHe.FlowRate + mfcO2.FlowRate);
    }


    protected virtual (double, double) CTFlowTest(double pressure, int position)
    {
        var dest = Find<Section>("IM_CA");
        var vFlow = IM_CA_CT1.FlowManager.FlowValve;

        ProcessStep.Start("Evacuate IM_CA_CT1");
        IM_CA_CT1.Isolate();
        vFlow.Open();
        IM_CA_CT1.OpenAndEvacuate(0.005);
        vFlow.Close();
        ProcessStep.End();

        FtgPressurize("O2", dest, pressure);

        ProcessStep.Start("Bleed IM_CA down until pCT < 0.5 Torr");
        IM_CA_CT1.OpenAndEvacuate();
        vFlow.MoveTo(position);
        WaitSeconds(5);
        var p0 = IM.Pressure;
        WaitFor(() => CTF.Pressure < 0.5);
        var p1 = IM.Pressure;
        ProcessStep.End();

        return (p0, p1);
    }

    protected virtual void CTFlowTestSequence()
    {
        //var testPressure = 500.0;
        //var testPosition = 660;
        var testPressure = 300.0;
        var testPosition = 647;
        while (testPressure > 5 && testPosition >= 0)
        {
            var pa = CTFlowTest(testPressure, testPosition);
            var pHigh = pa.Item1;
            var pLow = pa.Item2;
            testPressure = Math.Min(pHigh, pLow * 1.4);
            var dPos = (int)((672 - testPosition) * 1.1);
            testPosition = Math.Max(0, testPosition - dPos);
        }
    }

    #endregion Coil trap flow rate testing and estimation

    protected override void TransferCO2FromMCToIP() => TransferCO2FromMCToIPviaGR();


    #region Test functions

    /// <summary>
    /// General-purpose code tester. Put whatever you want here.
    /// </summary>
    protected override void Test()
    {
        ResetUgcTracking();
    }

    #endregion Test functions
}