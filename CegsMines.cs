using AeonHacs.Utilities;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using static AeonHacs.Components.CegsPreferences;
using static AeonHacs.Utilities.Utility;

namespace AeonHacs.Components
{
    public partial class CegsMines : Cegs
    {
        #region HacsComponent

        [HacsPreConnect]
        protected virtual void PreConnect()
        {
            #region Logs

            SampleLog = Find<HacsLog>("SampleLog");

            VM1PressureLog = Find<DataLog>("VM1PressureLog");
            VM1PressureLog.Changed = (col) => col.Resolution > 0 && col.Source is Manometer m ?
                (col.PriorValue is double p ?
                    Manometer.SignificantChange(p, m.Pressure) :
                    true) :
                false;

            VM2PressureLog = Find<DataLog>("VM2PressureLog");
            VM2PressureLog.Changed = (col) => col.Resolution > 0 && col.Source is Manometer m ?
                (col.PriorValue is double p ?
                    Manometer.SignificantChange(p, m.Pressure) :
                    true) :
                false;

            AmbientLog = Find<DataLog>("AmbientLog");
            // These components are needed to allow the inclusion of
            // non-INamedValue properties of theirs in logged data.
            HeaterController1 = Find<HC6Controller>("HeaterController1");
            HeaterController2 = Find<HC6Controller>("HeaterController2");
            AmbientLog.AddNewValue("HC1.CJ", -1, "0.0",
                () => HeaterController1.ColdJunctionTemperature);
            AmbientLog.AddNewValue("HC2.CJ", -1, "0.0",
                () => HeaterController2.ColdJunctionTemperature);


            GRSTLog = Find<DataLog>("GRSampleTemperatureLog");
            // These components are needed to allow the inclusion of
            // non-INamedValue properties of theirs in logged data.
            GR1 = Find<GraphiteReactor>("GR1");
            GR2 = Find<GraphiteReactor>("GR2");
            GR3 = Find<GraphiteReactor>("GR3");
            GR4 = Find<GraphiteReactor>("GR4");
            GR5 = Find<GraphiteReactor>("GR5");
            GR6 = Find<GraphiteReactor>("GR6");
            GRSTLog.AddNewValue("GR1.SampleTemperature", 1, "0.0",
                () => GR1.SampleTemperature);
            GRSTLog.AddNewValue("GR2.SampleTemperature", 1, "0.0",
                () => GR2.SampleTemperature);
            GRSTLog.AddNewValue("GR3.SampleTemperature", 1, "0.0",
                () => GR3.SampleTemperature);
            GRSTLog.AddNewValue("GR4.SampleTemperature", 1, "0.0",
                () => GR4.SampleTemperature);
            GRSTLog.AddNewValue("GR5.SampleTemperature", 1, "0.0",
                () => GR5.SampleTemperature);
            GRSTLog.AddNewValue("GR6.SampleTemperature", 1, "0.0",
                () => GR6.SampleTemperature);

            #endregion Logs
        }

        [HacsConnect]
        protected override void Connect()
        {
            base.Connect();

            #region a Cegs needs these
            // The base Cegs really can't do "carbon extraction and graphitization"
            // unless these objects are defined.

            Power = Find<Power>("Power");
            Ambient = Find<Chamber>("Ambient");

            VacuumSystem1 = Find<VacuumSystem>("VacuumSystem1");
            VacuumSystem2 = Find<VacuumSystem>("VacuumSystem2");

            IM = Find<Section>("IM");
            VTT = Find<Section>("VTT");
            MC = Find<Section>("MC");
            Split = Find<Section>("Split");
            GM = Find<Section>("GM");

            VTT_MC = Find<Section>("VTT_MC");
            MC_Split = Find<Section>("MC_Split");

            ugCinMC = Find<Meter>("ugCinMC");

            InletPorts = CachedList<IInletPort>();
            GraphiteReactors = CachedList<IGraphiteReactor>();
            d13CPorts = CachedList<Id13CPort>();

            #endregion a Cegs needs these

            #region Cegs options
            // The Cegs doesn't require these, but provides
            // added functionality if they are present.

            #endregion Cegs options

            // Sections
            VM1 = Find<Section>("VM1");
            VM2 = Find<Section>("VM2");
            TF = Find<Section>("TF");
            CA = Find<Section>("CA");
            CTF = Find<Section>("CTF");
            CT1 = Find<Section>("CT1");
            CT2 = Find<Section>("CT2");
            d13C = Find<Section>("d13C");
            AM = Find<Section>("AM");
            FTG = Find<Section>("FTG");
            IP1 = Find<Section>("IP1");
            IP2 = Find<Section>("IP2");

            TF_IP1 = Find<Section>("TF_IP1");
            FTG_TF = Find<Section>("FTG_TF");
            FTG_IP1 = Find<Section>("FTG_IP1");
            FTG_IP2 = Find<Section>("FTG_IP2");
            IM_CTF = Find<Section>("IM_CTF");
            IM_CA_CTF = Find<Section>("IM_CTF");
            IM_CT1 = Find<Section>("IM_CT1");
            IM_CT2 = Find<Section>("IM_CT2");
            IM_CA_CT1 = Find<Section>("IM_CA_CT1");
            IM_CA_CT2 = Find<Section>("IM_CA_CT2");
            CTF_CT1 = Find<Section>("CTF_CT1");
            CTF_CT2 = Find<Section>("CTF_CT2");
            CT1_CTO = Find<Section>("CT1_CTO");
            CT2_CTO = Find<Section>("CT2_CTO");
            CT1_VTT = Find<Section>("CT1_VTT");
            CT2_VTT = Find<Section>("CT2_VTT");

            MC_GM = Find<Section>("MC_GM");

            VS1All = Find<Section>("VS1All");
            VS2All = Find<Section>("VS2All");

            CA1 = Find<SableCA10>("CA1");
            TF1 = Find<TcpTubeFurnace>("TF1");
            LnScale1 = Find<LnScale>("LnScale1");
            IpOvenRamper = Find<OvenRamper>("IpOvenRamper");
            pAmbient = Find<AIManometer>("pAmbient");
            FTG_TFFlowManager = Find<FlowManager>("FTG_TFFlowManager");
            FTG_IMFlowManager = Find<FlowManager>("FTG_IMFlowManager");
            TFFlowManager = Find<FlowManager>("TFFlowManager");
        }
        #endregion HacsComponent

        #region System configuration
        #region Component lists
        #endregion Component lists

        #region HacsComponents

        // TODO: Many of these can be omitted (along with the code that uses them)
        // by changing logged properties from doubles to NamedValues. See
        // SableCA10.cs for examples.
        public virtual IVacuumSystem VacuumSystem2 { get; set; }
        public virtual DataLog VM2PressureLog { get; set; }
        public DataLog GRSTLog { get; set; }
        public GraphiteReactor GR1 { get; set; }
        public GraphiteReactor GR2 { get; set; }
        public GraphiteReactor GR3 { get; set; }
        public GraphiteReactor GR4 { get; set; }
        public GraphiteReactor GR5 { get; set; }
        public GraphiteReactor GR6 { get; set; }
        public HC6Controller HeaterController1 { get; set; }
        public HC6Controller HeaterController2 { get; set; }
        public HC6Controller HeaterController3 { get; set; }
        public HC6Controller HeaterController4 { get; set; }

        /// <summary>
        /// The amount of carbon in the Measurement Chamber.
        /// </summary>
        public virtual double umolCinMC => ugCinMC.Value / gramsCarbonPerMole;


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
        public ISection IM_CT { get; set; }

        /// <summary>
        /// Vacuum Manifold 1 (Inlets)
        /// </summary>
        public ISection VM1{ get; set; }

        /// <summary>
        /// Vacuum Manifold 2 (CEGS)
        /// </summary>
        public ISection VM2 { get; set; }

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
        /// d13C section
        /// </summary>
        public ISection d13C { get; set; }

        /// <summary>
        /// Auxiliary Manifold section (vial port manifold)
        /// </summary>
        public ISection AM { get; set; }

        /// <summary>
        /// Flow-Through Gas section
        /// </summary>
        public ISection FTG { get; set; }

        /// <summary>
        /// Inlet Port 2 section
        /// </summary>
        public ISection IP2 { get; set; }

        /// <summary>
        /// Inlet Port 1 section
        /// </summary>
        public ISection IP1 { get; set; }


        /// <summary>
        /// Tube furnace..Inlet Port 1 section
        /// </summary>
        public ISection TF_IP1 { get; set; }

        /// <summary>
        /// Flow-Through Gas..Tube Furnace section
        /// </summary>
        public ISection FTG_TF { get; set; }

        /// <summary>
        /// Flow-Through Gas..Tube Furnace..Inlet Port 1 section
        /// </summary>
        public ISection FTG_IP1 { get; set; }

        /// <summary>
        /// Flow-Through Gas..Inlet Port 2 section
        /// </summary>
        public ISection FTG_IP2 { get; set; }

        /// <summary>
        /// Inlet Manifold..Coil Trap Flow section (bypasses CO2 analyzer)
        /// </summary>
        public ISection IM_CTF { get; set; }

        /// <summary>
        /// Inlet Manifold..CO2 Analyzer..Coil Trap Flow section
        /// </summary>
        public ISection IM_CA_CTF { get; set; }

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

        /// <summary>
        /// Coil Trap Flow..Coil Trap 1 section
        /// </summary>
        public ISection CTF_CT1 { get; set; }

        /// <summary>
        /// Coil Trap Flow..Coil Trap 2 section
        /// </summary>
        public ISection CTF_CT2 { get; set; }

        /// <summary>
        /// Coil Trap 1..Coil Trap Outlet section
        /// </summary>
        public ISection CT1_CTO { get; set; }

        /// <summary>
        /// Coil Trap 2..Coil Trap Outlet section
        /// </summary>
        public ISection CT2_CTO { get; set; }

        /// <summary>
        /// Coil Trap 1..Variable Temperature Trap section
        /// </summary>
        public ISection CT1_VTT { get; set; }

        /// <summary>
        /// Coil Trap 2..Variable Temperature Trap section
        /// </summary>
        public ISection CT2_VTT { get; set; }

        /// <summary>
        /// Measurement Chamber..Graphite Manifold section
        /// </summary>
        public ISection MC_GM { get; set; }

        /// <summary>
        /// All of the chambers evacuated by Vacuum System 1 (Inlets), except ports
        /// </summary>
        public ISection VS1All { get; set; }

        /// <summary>
        /// All of the chambers evacuated by Vacuum System 2 (CEGS), except ports
        /// </summary>
        public ISection VS2All { get; set; }

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
        public LnScale LnScale1 { get; set; }

        /// <summary>
        /// Ramped temperature controller for Inlet Port
        /// </summary>
        public OvenRamper IpOvenRamper { get; set; }

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

        protected override void ZeroPressureGauges()
        {
            base.ZeroPressureGauges();

            // do not auto-zero pressure gauges while a process is running
            if (Busy) return;

            bool OkToZeroManometer(ISection section) =>
                    section is Section s &&
                    s.VacuumSystem.TimeAtBaseline.TotalSeconds > 20 &&
                    (s.PathToVacuum?.IsOpened() ?? false);

            if (OkToZeroManometer(MC))
                ZeroIfNeeded(MC?.Manometer, 15);

            if (OkToZeroManometer(CTF))
                ZeroIfNeeded(CTF?.Manometer, 5);

            if (OkToZeroManometer(IM))
                ZeroIfNeeded(IM?.Manometer, 10);

            if (OkToZeroManometer(GM))
            {
                ZeroIfNeeded(GM?.Manometer, 10);
                foreach (var gr in GraphiteReactors)
                    if (Manifold(gr).PathToVacuum.IsOpened() && gr.IsOpened)
                        ZeroIfNeeded(gr.Manometer, 5);
            }
        }

        #endregion Periodic system activities & maintenance

        #region Process Management

        protected override void BuildProcessDictionary()
        {
            Separators.Clear();

            ProcessDictionary["Run samples"] = RunSamples;
            Separators.Add(ProcessDictionary.Count);

            ProcessDictionary["Prepare GRs for new iron and desiccant"] = PrepareGRsForService;
            ProcessDictionary["Precondition GR iron"] = PreconditionGRs;
            ProcessDictionary["Replace iron in sulfur traps"] = ChangeSulfurFe;
            Separators.Add(ProcessDictionary.Count);

            ProcessDictionary["Prepare carbonate sample for acid"] = PrepareCarbonateSample;
            ProcessDictionary["Load acidified carbonate sample"] = LoadCarbonateSample;
            Separators.Add(ProcessDictionary.Count);

            ProcessDictionary["Admit sealed CO2 to InletPort"] = AdmitSealedCO2IP;
            ProcessDictionary["Collect CO2 from InletPort"] = Collect;
            ProcessDictionary["Extract"] = Extract;
            ProcessDictionary["Measure"] = Measure;
            ProcessDictionary["Discard excess CO2 by splits"] = DiscardSplit;
            ProcessDictionary["Remove sulfur"] = RemoveSulfur;
            ProcessDictionary["Dilute small sample"] = Dilute;
            ProcessDictionary["Graphitize aliquots"] = GraphitizeAliquots;
            ProcessDictionary["Open and evacuate line"] = OpenLine;
            ProcessDictionary["Open and evacuate VS1"] = OpenVS1Line;
            ProcessDictionary["Open and evacuate TF and VS1"] = OpenTF_VS1;
            ProcessDictionary["Open and evacuate VS2"] = OpenVS2Line;
            Separators.Add(ProcessDictionary.Count);

            ProcessDictionary["Collect, etc."] = CollectEtc;
            ProcessDictionary["Extract, etc."] = ExtractEtc;
            ProcessDictionary["Measure, etc."] = MeasureEtc;
            ProcessDictionary["Graphitize, etc."] = GraphitizeEtc;
            Separators.Add(ProcessDictionary.Count);

            ProcessDictionary["Evacuate Inlet Port"] = EvacuateIP;
            ProcessDictionary["Flush Inlet Port"] = FlushIP;
            ProcessDictionary["Admit O2 into Inlet Port"] = AdmitIPO2;
            ProcessDictionary["Heat Quartz and Open Line"] = HeatQuartzOpenLine;
            ProcessDictionary["Turn off IP furnaces"] = TurnOffIPFurnaces;
            ProcessDictionary["Discard IP gases"] = DiscardIPGases;
            ProcessDictionary["Close IP"] = CloseIP;
            ProcessDictionary["Bleed IP gas through frozen CT"] = FrozenBleed;
            ProcessDictionary["Bleed IP gas through CT (no temperature control)"] = Bleed;
            ProcessDictionary["Evacuate and Freeze VTT"] = FreezeVtt;
            ProcessDictionary["Admit Dead CO2 into MC"] = AdmitDeadCO2;
            ProcessDictionary["Purify CO2 in MC"] = CleanupCO2InMC;
            ProcessDictionary["Discard MC gases"] = DiscardMCGases;
            ProcessDictionary["Divide sample into aliquots"] = DivideAliquots;
            ProcessDictionary["Wait for operator"] = WaitForOperator;
            Separators.Add(ProcessDictionary.Count);

            ProcessDictionary["Prepare loaded inlet ports for collection"] = PrepareIPsForCollection;
            ProcessDictionary["Transfer CO2 from CT to VTT"] = TransferCO2FromCTToVTT;
            ProcessDictionary["Transfer CO2 from MC to VTT"] = TransferCO2FromMCToVTT;
            ProcessDictionary["Transfer CO2 from MC to GR"] = TransferCO2FromMCToGR;
            ProcessDictionary["Transfer CO2 from prior GR to MC"] = TransferCO2FromGRToMC;
            Separators.Add(ProcessDictionary.Count);

            ProcessDictionary["Wait for timer"] = WaitForTimer;
            ProcessDictionary["Backfill TF with He"] = BackfillTF1WithHe;
            ProcessDictionary["Notify to load TF"] = LoadTF;
            ProcessDictionary["Admit O2 to TF"] = AdmitO2toTF;
            ProcessDictionary["Include CO2 analyzer"] = IncludeCO2Analyzer;
            ProcessDictionary["Bypass CO2 analyzer"] = BypassCO2Analyzer;
            ProcessDictionary["Use IP flow"] = UseIpFlow;
            ProcessDictionary["No IP flow"] = NoIpFlow;
            ProcessDictionary["Collect in CT1"] = CollectToCT1;
            ProcessDictionary["Collect in CT2"] = CollectToCT2;
            ProcessDictionary["Toggle CT collection"] = ToggleCT;
            ProcessDictionary["Start collecting"] = StartCollecting;
            ProcessDictionary["Clear collection conditions"] = ClearCollectionConditions;
            ProcessDictionary["Collect until condition met"] = CollectUntilConditionMet;
            ProcessDictionary["Stop collecting"] = StopCollecting;
            ProcessDictionary["Turn on quartz furnace"] = TurnOnIpQuartzFurnace;
            ProcessDictionary["Turn off quartz furnace"] = TurnOffIpQuartzFurnace;
            ProcessDictionary["Adjust sample setpoint"] = AdjustIpSetpoint;
            ProcessDictionary["Turn on sample furnace"] = TurnOnIpSampleFurnace;
            ProcessDictionary["Turn off sample furnace"] = TurnOffIpSampleFurnace;
            ProcessDictionary["Disable sample setpoint ramping"] = DisableIpRamp;
            ProcessDictionary["Enable sample setpoint ramping"] = EnableIpRamp;
            ProcessDictionary["Adjust sample ramp rate"] = AdjustIpRampRate;
            ProcessDictionary["Wait for sample to rise to setpoint"] = WaitIpRiseToSetpoint;
            ProcessDictionary["Wait for sample to fall to setpoint"] = WaitIpFallToSetpoint;
            ProcessDictionary["Start flow through to trap"] = StartFlowThroughToTrap;
            ProcessDictionary["Start flow through to waste"] = StartFlowThroughToWaste;
            ProcessDictionary["Stop flow-through gas"] = StopFlowThroughGas;
            ProcessDictionary["Open TF to IP1"] = OpenTF_IP1;            
            ProcessDictionary["Wait for CEGS to be free"] = WaitForCegs;
            ProcessDictionary["Start Extract, Etc"] = StartExtractEtc;

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

            Separators.Add(ProcessDictionary.Count);
            ProcessDictionary["Test"] = Test;
            base.BuildProcessDictionary();
        }

        #region OpenLine
        protected virtual void CloseGasSupplies()
        {
            ProcessSubStep.Start("Close gas supplies");

            // Look only in CEGS vacuum systems; ignore other process managers
            var cegsGasSupplies = GasSupplies.Values.Where(gs => VacuumSystems.ContainsValue(gs.Destination.VacuumSystem)).ToList();
            cegsGasSupplies.ForEach(gs => gs.ShutOff());
            // close gas flow valves after all shutoff valves are closed
            cegsGasSupplies.ForEach(gs => gs.FlowValve?.CloseWait());

            ProcessSubStep.End();
        }

        /// <summary>
        /// Open and evacuate the entire vacuum line. This establishes
        /// the baseline system state: the condition it is normally left in
        /// when idle, and the expected starting point for major
        /// processes such as running samples.
        /// </summary>
        protected override void OpenLine() =>
            FastOpenLine();

        /// <summary>
        /// Open and evacuate the vacuum line quickly, without special
        /// attention to the sequence of chambers.
        /// </summary>
        protected virtual void FastOpenLine()
        {
            CloseGasSupplies();
            OpenVS1Line();
            OpenVS2Line();
            WaitFor(() => VS1All.VacuumSystem.Pressure <= OkPressure && VS2All.VacuumSystem.Pressure <= OkPressure);
            Section.Connections(VS1All, VS2All).Open();
        }

        /// <summary>
        /// Open and evacuate the chambers normally serviced by VacuumSystem1
        /// </summary>
        protected virtual void OpenVS1Line()
        {
            if (!VS1All.IsOpened || !VS1All.PathToVacuum.IsOpened())
                VS1All.OpenAndEvacuate();
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
        }


        /// <summary>
        /// Open and evacuate the chambers normally serviced by VacuumSystem2
        /// </summary>
        protected virtual void OpenVS2Line()
        {
            if (!VS2All.IsOpened || !VS2All.PathToVacuum.IsOpened())
                VS2All.OpenAndEvacuate();
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
        /// Inlet Port sample furnace working setpoint ramp rate (degrees per minute).
        /// </summary>
        public double IpRampRate => GetParameter("IpRampRate");

        /// <summary>
        /// The Inlet Port sample furnace's target setpoint (the final setpoint when ramping).
        /// </summary>
        public double IpSetpoint => GetParameter("IpSetpoint");

        /// <summary>
        /// The desired Inlet Manifold pressure, used for filling or flow management.
        /// </summary>
        public double ImPressureTarget => GetParameter("ImPressureTarget");

        /// <summary>
        /// Tube furnace working setpoint ramp rate (degrees per minute).
        /// </summary>
        public double TfRampRate => GetParameter("TfRampRate");

        /// <summary>
        /// The Tube Furnace's target setpoint (the final setpoint when ramping).
        /// </summary>
        public double TfSetpoint => GetParameter("TfSetpoint");

        /// <summary>
        /// The desired Tube Furnace  pressure, used for filling or flow management.
        /// </summary>
        public double TfPressureTarget => GetParameter("TfPressureTarget");

        /// <summary>
        /// During sample collection, close the Inlet Port when the Inlet Manifold reaches this pressure. 
        /// Ignore Inlet Manifold pressure if this value is double.NaN (not a number).
        /// </summary>
        public double CollectCloseIpPressure => GetParameter("CollectCloseIpPressure");

        /// <summary>
        /// Stop collecting into the coil trap when the Inlet Port rises to this temperature. 
        /// Ignore this stop condition if the value is double.NaN (not a number).
        /// </summary>
        public double CollectUntilTemperatureRises => GetParameter("CollectUntilTemperatureRises");

        /// <summary>
        /// Stop collecting into the coil trap when the Inlet Port falls to this temperature. 
        /// Ignore this stop condition if the value is double.NaN (not a number).
        /// </summary>
        public double CollectUntilTemperatureFalls => GetParameter("CollectUntilTemperatureFalls");

        /// <summary>
        /// Stop collecting when the Coil Trap falls to or below this pressure. 
        /// Ignore Coil Trap pressure if this value is double.NaN (not a number).
        /// </summary>
        public double CollectUntilCtPressureFalls => GetParameter("CollectUntilCtPressureFalls");

        /// <summary>
        /// Stop collecting into the coil trap when amount of carbon in 
        /// the Coil Trap reaches this value. 
        /// Ignore the amount of carbon if this value is double.NaN (not a number).
        /// </summary>
        public double CollectUntilUgc => GetParameter("CollectUntilUgc");

        /// <summary>
        /// Stop collecting into the coil trap when this much time has elapsed. 
        /// Ignore the collection time if this value is double.NaN (not a number).
        /// </summary>
        public double CollectUntilMinutes => GetParameter("CollectUntilMinutes");

        /// <summary>
        /// How many minutes to wait.
        /// </summary>
        public double WaitTimerMinutes => GetParameter("WaitTimerMinutes");
        /// <summary>
        /// What pressure to evacuate to.
        /// </summary>
        public double IpEvacuationPressure => GetParameter("IpEvacuationPressure");

        #endregion Process Control Parameters


        #region Process Control Properties

        public virtual bool IpIsTubeFurnace => InletPort.SampleFurnace is TubeFurnace;

        /// <summary>
        /// Change the Inlet Port Sample furnace setpoint at a controlled
        /// ramp rate, rather than immediately to the given value.
        /// </summary>
        public virtual bool EnableIpSetpointRamp { get; set; } = false;

        /// <summary>
        /// Change the Tube Furnace setpoint at a controlled
        /// ramp rate, rather than immediately to the given value.
        /// </summary>
        public virtual bool EnableTfSetpointRamp { get; set; } = false;

        /// <summary>
        /// Provide a flow of oxygen through the Inlet Port to combust the sample,
        /// instead of a fixed pressure.
        /// </summary>
        public virtual bool NeedIpFlow { get; set; } = true;

        /// <summary>
        /// Direct the sample gas through the CO2 analyzer during collection.
        /// </summary>
        public virtual bool NeedAnalyzer { get; set; } = true;

        /// <summary>
        /// Monitors the time elapsed since the current sample collection phase began.
        /// </summary>
        public Stopwatch CollectStopwatch { get; set; } = new Stopwatch();

        /// <summary>
        /// The coil trap currently being used to trap sample gas.
        /// </summary>
        public ISection CurrentCT => IM_CT.Chambers.Contains(Find<Chamber>("CT1")) ? CT1 : CT2;

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
        /// Wait for timer minutes.
        /// </summary>
        protected virtual void WaitForTimer()
        {
            ProcessStep.Start($"Wait for {WaitTimerMinutes:0} minutes");
            WaitFor(() => ProcessStep.Elapsed.TotalMinutes >= WaitTimerMinutes);
            ProcessStep.End();
        }


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
        /// Turn on the Inlet Port quartz furnace.
        /// </summary>
        protected virtual void TurnOnIpQuartzFurnace() => InletPort.QuartzFurnace.TurnOn();

        /// <summary>
        /// Turn off the Inlet Port quartz furnace.
        /// </summary>
        protected virtual void TurnOffIpQuartzFurnace() => InletPort.QuartzFurnace.TurnOff();

        /// <summary>
        /// Adjust the Inlet Port sample furnace setpoint. If its
        /// setpoint ramp is enabled, the working setpoint will be managed
        /// to reach the new setpoint at the programmed ramp rate.
        /// </summary>
        protected virtual void AdjustIpSetpoint()
        {
            if (IpSetpoint == double.NaN) return;
            if (IpOvenRamper.Enabled)
                IpOvenRamper.Setpoint = IpSetpoint;
            else
                InletPort.SampleFurnace.Setpoint = IpSetpoint;
        }


        /// <summary>
        /// Wait for Inlet Port temperature to fall below IpSetpoint
        /// </summary>
        protected virtual void WaitIpFallToSetpoint()
        {
            AdjustIpSetpoint();
            bool shouldStop()
            {
                if (Stopping)
                    return true;
                if (InletPort.Temperature <= IpSetpoint)
                    return true;
                return false;
            }
            ProcessStep.Start($"Waiting for {InletPort.Name} to reach {IpSetpoint:0} °C");
            WaitFor(shouldStop, -1, 1000);
            ProcessStep.End();
        }
        /// <summary>
        /// Turn on the Inlet Port sample furnace.
        /// </summary>
        protected virtual void TurnOnIpSampleFurnace()
        {
            AdjustIpSetpoint();
            InletPort.SampleFurnace.TurnOn();
        }

        /// <summary>
        /// Wait for the InletPort sample furnace to reach the setpoint.
        /// </summary>
        protected virtual void WaitIpRiseToSetpoint()
        {
            bool shouldStop()
            {
                if (Stopping)
                    return true;
                if (InletPort.Temperature >= IpSetpoint)
                    return true;
                return false;
            }
            ProcessStep.Start($"Waiting for {InletPort.Name} to reach {IpSetpoint:0} °C");
            WaitFor(shouldStop, -1, 1000);
            ProcessStep.End();
        }

        /// <summary>
        /// Turn off the Inlet Port sample furnace.
        /// </summary>
        protected virtual void TurnOffIpSampleFurnace() => InletPort.SampleFurnace.TurnOff();

        /// <summary>
        /// Adjust the Inlet Port sample furnace ramp rate.
        /// </summary>
        protected virtual void AdjustIpRampRate() => IpOvenRamper.RateDegreesPerMinute = IpRampRate;

        /// <summary>
        /// Enable the Inlet Port sample furnace setpoint ramp.
        /// </summary>
        protected virtual void EnableIpRamp()
        {
            IpOvenRamper.Oven = InletPort.SampleFurnace;
            IpOvenRamper.Enable();
        }

        /// <summary>
        /// Disable the Inlet Port sample furnace setpoint ramp.
        /// </summary>
        protected virtual void DisableIpRamp() => IpOvenRamper.Disable();


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
            Pause("Ready for operator", "Load the Tube Furnace and seal it closed.");
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
        /// Start flowing O2 through the Inlet Port to vacuum.
        /// </summary>
        protected virtual void StartFlowThroughToWaste() => StartFlowThrough(false);

        /// <summary>
        /// Start flowing O2 through the Inlet Port to the coil trap.
        /// </summary>
        protected virtual void StartFlowThroughToTrap() => StartFlowThrough(true);

        /// <summary>
        /// Start flowing O2 through the Inlet Port.
        /// </summary>
        protected virtual void StartFlowThrough(bool trap)
        {
            ProcessStep.Start($"Start flowing O2 through {InletPort.Name}");

            var gasfm = IpIsTubeFurnace ? FTG_TFFlowManager : FTG_IMFlowManager;
            // Need to manage FTG gas source valve manually,
            // because we want the shutoff to be
            // downstream of the flow valve.
            var o2 = Find<IValve>("vO2_FTG");
            var destination = trap ? IM_CT : IM;

            var section = IpIsTubeFurnace ? FTG_IP1 : FTG_IP2;

            ProcessStep.Start($"Isolate and open {section.Name}");
            section.Isolate();
            section.Open();
            ProcessStep.End();

            // prepare upstream
            gasfm.FlowValve.CloseWait();

            // prepare downstream
            var vacfm = IpIsTubeFurnace ? TFFlowManager : null;
            vacfm?.FlowValve.CloseWait();
            Find<IValve>("vTFBypass")?.CloseWait();

            // join everything
            if (trap)
            {
                StartCollecting();
                o2.OpenWait();
            }
            else
            {
                destination.OpenAndEvacuate(OkPressure);
                destination.Isolate();
                o2.OpenWait();
                InletPort.Open();
                destination.Evacuate();
            }

            // regulate the gas flow to maintain pressure
            var pressure = IpIsTubeFurnace ? TF.Pressure : IM.Pressure;
            gasfm.Start(pressure);
            if (IpIsTubeFurnace)
            {
                gasfm.Stop();
                vacfm?.Start(pressure);
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
            var o2 = Find<IValve>("vO2_FTG");
            var gs = GasSupply("O2", IpIsTubeFurnace ? TF : IM);

            gasfm.Stop();
            gasfm.FlowValve.CloseWait();
            vacfm?.Stop();
            vacfm?.FlowValve.CloseWait();
            gs.ShutOff();
            o2.CloseWait();

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
        protected virtual void CollectToCT1() =>  IM_CT = NeedAnalyzer ? IM_CA_CT1 : IM_CT1;

        /// <summary>
        /// Use Coil Trap 2 for sample collection.
        /// </summary>
        protected virtual void CollectToCT2() => IM_CT = NeedAnalyzer ? IM_CA_CT2 : IM_CT2;

        /// <summary>
        /// Switch coil traps.
        /// </summary>
        protected virtual void ToggleCT()
        {
            ProcessStep.Start($"Toggle CT");

            if (CT == CT1)
                CollectToCT2();
            else
                CollectToCT1();
            StartCollecting();

            ProcessStep.End();
        }

        /// <summary>
        /// Start collecting sample into a coil trap.
        /// </summary>
        protected virtual void StartCollecting()
        {
            ProcessStep.Start($"Trapping sample in {CurrentCT.Name}");

            ClearCollectionConditions();
            var ct = CurrentCT;
            IM_CT.OpenAndEvacuate(OkPressure);
            ct.WaitForFrozen(false);
            ct.FlowManager.FlowValve.Close();
            InletPort.Open();
            CollectStopwatch.Restart();
            ct.FlowManager.Start(FirstTrapBleedPressure);

            ProcessStep.End();
        }

        /// <summary>
        /// Set all collection condition parameters to NaN
        /// </summary>
        protected void ClearCollectionConditions()
        {
            ClearParameter("CollectUntilTemperatureRises");
            ClearParameter("CollectUntilTemperatureFalls");
            ClearParameter("CollectCloseIpPressure");
            ClearParameter("CollectUntilCtPressureFalls");
            ClearParameter("CollectUntilUgc");
            ClearParameter("CollectUntilMinutes");
        }


        /// <summary>
        /// Wait for a collection stop condition to occur.
        /// </summary>
        protected virtual void CollectUntilConditionMet()
        {
            ProcessStep.Start($"Wait for a collection stop condition to occur");

            bool shouldStop()
            {
                if (Stopping)
                    return true;
                if (CollectUntilTemperatureRises != double.NaN && InletPort.Temperature >= CollectUntilTemperatureRises)
                    return true;
                if (CollectUntilTemperatureFalls != double.NaN && InletPort.Temperature <= CollectUntilTemperatureFalls)
                    return true;
                if (CollectCloseIpPressure != double.NaN && InletPort.IsOpened && InletPort.Pressure <= CollectCloseIpPressure)
                    InletPort.Close();
                if (CollectUntilCtPressureFalls != double.NaN && CT.Pressure <= CollectUntilCtPressureFalls)
                    return true;
                if (CollectUntilUgc != double.NaN && CollectedUgc >= CollectUntilUgc)
                    return true;
                if (CollectUntilMinutes != double.NaN && CollectStopwatch.Elapsed.TotalMinutes >= CollectUntilMinutes)
                    return true;
                return false;
            }
            WaitFor(shouldStop, -1, 1000);

            ProcessStep.End();
        }

        /// <summary>
        /// Stop collecting
        /// </summary>
        protected virtual void StopCollecting()
        {
            ProcessStep.Start("Stop Collecting");

            CT = CurrentCT;     // The VTT will take it from here
            CT.Isolate();
            CT.FlowManager.Stop();
            CT.FlowManager.FlowValve.Close();

            ProcessStep.End();
        }

        /// <summary>
        /// Wait for the CEGS to be ready to process a sample.
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
        /// Run the ExtractEtc process step, then evacuate VS2
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

        #endregion Process Steps


        #endregion Process Management

        #region Test functions

        /// <summary>
        /// In situ quartz sample process, Day 1 (preparation)
        /// </summary>
        //protected virtual void Day1()
        //{
            //TurnOnIpQuartzFurnace();
            //BackfillTF1WithHe();
            //LoadTF();
            //SetParameter("IpEvacuationPressure", 1E-2);
            //EvacuateIP();
            //SetParameter("TfPressureTarget", 50);
            //AdmitO2toTF();
            //SetParameter("IpSetpoint", 100);
            //TurnOnIpSampleFurnace();
            //WaitIpRiseToSetpoint();
            //StartFlowThroughToWaste();
            //SetParameter("WaitTimerMinutes", 2);
            //WaitForTimer();
            //StopFlowThroughGas();
            //EvacuateIP();
            //AdmitO2toTF();
            //StartFlowThroughToWaste();
            //SetParameter("IpSetpoint", 95);
            //TurnOffIpSampleFurnace();
           // WaitIpFallToSetpoint();
            //StopFlowThroughGas();
            //TurnOffIpQuartzFurnace();
            //OpenTF_VS1();
        //}

        /// <summary>
        /// In situ quartz sample process, Day 2 (extraction)
        /// </summary>
        //protected virtual void Day2()
        //{
            //TurnOnIpQuartzFurnace();
            //BackfillTF1WithHe();
            //LoadTF();
            //EvacuateIP();
            //SetParameter("TfPressureTarget", 50);
            //AdmitO2toTF();
            //SetParameter("IpSetpoint", 75);
            //TurnOnIpSampleFurnace();
            //WaitIpRiseToSetpoint();
            //StartFlowThroughToWaste();
            //SetParameter("WaitTimerMinutes", 1);
            //WaitForTimer();
            //StopFlowThroughGas();
            //EvacuateIP();
            //AdmitO2toTF();
            //SetParameter("IpSetpoint", 150);
            //AdjustIpSetpoint();
            //WaitIpRiseToSetpoint();
            //SetParameter("WaitTimerMinutes", 1);
            //WaitForTimer();
            //TurnOffIpSampleFurnace();
            //BypassCO2Analyzer();
            //CollectToCT1();
            //SetParameter("FirstTrapBleedPressure", 10);
            //StartFlowThroughToTrap();
            //SetParameter("CollectUntilTemperatureFalls", 80);
            //CollectUntilConditionMet();
            //StopFlowThroughGas();
            //OpenTF_IP1();
            //ClearCollectionConditions();
            //SetParameter("CollectUntilCtPressureFalls", 4.0);
            //CollectUntilConditionMet();
            //StopCollecting();
            //ExtractEtcThenEvacuateVS2();
            //OpenLine();
        //}


        /// <summary>
        /// General-purpose code tester. Put whatever you want here.
        /// </summary>
        protected void TestBakeout()
            {
                EvacuateIP();
            SetParameter("TfPressureTarget", 50);
            AdmitO2toTF();
            SetParameter("IpSetpoint", 500);
            TurnOnIpSampleFurnace();
            WaitIpRiseToSetpoint();
            StartFlowThroughToWaste();
            SetParameter("WaitTimerMinutes",10);
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

        protected override void Test()
        {
            TestBakeout();

        }
        #endregion Test functions

    }
}