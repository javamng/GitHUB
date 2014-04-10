using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.Backend.Utils;
using InformedProteomics.Scoring.LikelihoodScoring;
using NUnit.Framework;

namespace InformedProteomics.Test
{
    [TestFixture]
    class TestRankPeakProbability
    {
        private string[] _names;
        private string _preTsv;
        private string _preRaw;
        private string _outPre;
        private int _maxRanks;
        private double _relativeIntensityThreshold;

        private bool _writeIonProbabilities;
        private string _ionProbabilityOutFileName;

        private bool _writeRankProbabilities;
        private string _rankProbabilityOutFileName;

        private bool _writePrecursorOffsetProbabilities;
        private string _precursorOffsetProbabilityOutFileName;

        private bool _writeMassErrorProbabilities;
        private string _massErrorProbabilityOutFileName;

        private List<IonType> _ionTypes;
        private double _selectedIonThreshold;
        private List<IonType>[] _selectedIons;
        private List<IonType>[] _unselectedIons; 
        private IonTypeFactory _ionTypeFactory;
        private ActivationMethod _act;
        private int _precursorCharge;
        private const double BinWidth = 1.005;

        private readonly Tolerance _defaultTolerance = new Tolerance(15, ToleranceUnit.Ppm);

        private int _retentionCount;
        private int _searchWidth;
        private double _precursorOffsetThreshold;

        [Test]
        public void RankPeakProbability()
        {
            // read configuration settings
            InitTest(new ConfigFileReader(@"\\protoapps\UserData\Wilkins\BottomUp\RankPeakProbabilityConfig.ini"));

            foreach (var name in _names)
            {
                var tsvName = _preTsv.Replace("@", name);
                var rawName = _preRaw.Replace("@", name);
                var txtFiles = Directory.GetFiles(tsvName).ToList();
                var rawFilesTemp = Directory.GetFiles(rawName).ToList();
                var rawFiles = rawFilesTemp.Where(rawFile => Path.GetExtension(rawFile) == ".raw").ToList();

                Assert.True(rawFiles.Count == txtFiles.Count);

                // Initialize probability tables
                var rankTables = new RankTable[_precursorCharge];
                var ionFrequencyTables = new CleavageIonFrequencyTable[_precursorCharge];
                var offsetFrequencyTables = new List<PrecursorOffsetFrequencyTable>[_precursorCharge];
                for (int i = 0; i < _precursorCharge; i++)
                {
                    rankTables[i] = new RankTable(_ionTypes.ToArray(), _maxRanks);
                    ionFrequencyTables[i] = new CleavageIonFrequencyTable(_ionTypes, _defaultTolerance, _relativeIntensityThreshold);
                    offsetFrequencyTables[i] = new List<PrecursorOffsetFrequencyTable>();
                    for (int j = 1; j <= (i + 1); j++)
                    {
                        offsetFrequencyTables[i].Add(new PrecursorOffsetFrequencyTable(_searchWidth / j, j, BinWidth / j));
                    }
                }

                // Read files
                var matchList = new SpectrumMatchList(_act, false, _precursorCharge);
                for (int i = 0; i < txtFiles.Count; i++)
                {
                    string textFile = txtFiles[i];
                    string rawFile = rawFiles[i];
                    Console.WriteLine("{0}\t{1}", textFile, rawFile);
                    var lcms = LcMsRun.GetLcMsRun(rawFile, MassSpecDataType.XCaliburRun, 0, 0);
                    matchList.AddMatchesFromFile(lcms, new TsvFileParser(txtFiles[i]));
                }

                // Calculate probability tables
                for (int j = 0; j < _precursorCharge; j++)
                {
                    // Calculate ion probabilities
                    var chargeMatches = matchList.GetCharge(j + 1);
                    chargeMatches.FilterSpectra(_searchWidth, _retentionCount);
                    ionFrequencyTables[j].AddMatches(chargeMatches);

                    // Calculate precursor offset probabilities
                    for (int i = 0; i < _precursorCharge; i++)
                    {
                        foreach (var offsetFrequencyTable in offsetFrequencyTables[i])
                        {
                            offsetFrequencyTable.AddMatches(chargeMatches);
                        }
                    }
                }

                // Select ion types
                for (int j = 0; j < _precursorCharge; j++)
                {
                    var selected = ionFrequencyTables[j].SelectIons(_selectedIonThreshold);
                    foreach (var selectedIonProb in selected)
                    {
                        _selectedIons[j].Add(_ionTypeFactory.GetIonType(selectedIonProb.DataLabel));
                    }
                    _unselectedIons[j] = _ionTypes.Except(_selectedIons[j]).ToList();
                }

                // Create Mass Error tables and Ion Pair Frequency tables
                var selectedMassErrors = new List<Probability<double>>[_precursorCharge, _selectedIons.Length];
                var selectedIonPairFrequencies = new List<Probability<IonPairFound>>[_precursorCharge, _selectedIons.Length];
                var unselectedMassErrors = new List<Probability<double>>[_precursorCharge, _unselectedIons.Length];
                var unselectedIonPairFrequencies = new List<Probability<IonPairFound>>[_precursorCharge, _unselectedIons.Length];
                for (int i = 0; i < _precursorCharge; i++)
                {
                    var chargeMatches = matchList.GetCharge(i + 1);
                    // create tables for selected ions
                    var selectedMassErrorTables = new MassErrorTable[_selectedIons.Length];
                    for (int j = 0; j < _selectedIons.Length; j++)
                    {
                        selectedMassErrorTables[j] = new MassErrorTable(new[] { _selectedIons[i][j] }, _defaultTolerance);
                        selectedMassErrorTables[j].AddMatches(chargeMatches);
                        selectedMassErrors[i, j] = selectedMassErrorTables[j].MassError;
                        selectedIonPairFrequencies[i, j] = selectedMassErrorTables[j].IonPairFrequency;
                    }
                    // create tables for unselected ions
                    var unselectedMassErrorTables = new MassErrorTable[_unselectedIons.Length];
                    for (int j = 0; j < _unselectedIons.Length; j++)
                    {
                        unselectedMassErrorTables[j] = new MassErrorTable(new[] { _unselectedIons[i][j] }, _defaultTolerance);
                        unselectedMassErrorTables[j].AddMatches(chargeMatches);
                        unselectedMassErrors[i, j] = unselectedMassErrorTables[j].MassError;
                        unselectedIonPairFrequencies[i, j] = unselectedMassErrorTables[j].IonPairFrequency;
                    }
                }

                // Initialize precursor filter
                var precursorFilter = new PrecursorFilter(_precursorCharge, _defaultTolerance);
                for (int j = 0; j < _precursorCharge; j++)
                {
                    precursorFilter.SetChargeOffsets(new PrecursorOffsets(offsetFrequencyTables[j], j + 1, _precursorOffsetThreshold));
                }

                // Calculate rank probabilities
                for (int j = 0; j < _precursorCharge; j++)
                {
                    var chargeMatches = precursorFilter.FilterMatches(matchList.GetCharge(j+1));
                    rankTables[j].RankMatches(chargeMatches, _defaultTolerance);
                }

                // Write ion probability output files
                if (_writeIonProbabilities)
                {
                    var outFile = _ionProbabilityOutFileName.Replace("@", name);
                    for (int i = 0; i < _precursorCharge; i++)
                    {
                        string outFileName = outFile.Replace("*", (i + 1).ToString(CultureInfo.InvariantCulture));
                        var ionProbabilities = ionFrequencyTables[i].IonProbabilityTable;
                        using (var finalOutputFile = new StreamWriter(outFileName))
                        {
                            finalOutputFile.WriteLine("Ion\tTarget");
                            foreach (var ionProbability in ionProbabilities)
                            {
                                finalOutputFile.WriteLine("{0}\t{1}", ionProbability.DataLabel, ionProbability.Prob);
                            }
                        }
                    }
                }

                // Write rank probability output files
                if (_writeRankProbabilities)
                {
                    var outFileName = _rankProbabilityOutFileName.Replace("@", name);
                    for (int charge = 0; charge < _precursorCharge; charge++)
                    {
                        var chargeOutFileName = outFileName.Replace("*",
                            (charge + 1).ToString(CultureInfo.InvariantCulture));
                        var ionProbabilities = rankTables[charge].IonProbabilities;
                        WriteRankProbabilities(ionProbabilities, charge, rankTables[charge].TotalRanks,
                            chargeOutFileName);
                    }
                }

                // Write precursor offset probability output files
                if (_writePrecursorOffsetProbabilities)
                {

                }

                // Write mass error probability output files
                if (_writeMassErrorProbabilities)
                {

                }
            }
        }

        private void WriteRankProbabilities(IList<Dictionary<IonType, Probability<string>>> ionProbabilities, int charge, int totalRanks, string outFileCharge)
        {
            using (var outFile = new StreamWriter(outFileCharge))
            {
                // Write headers
                outFile.Write("Rank\t");
                foreach (var ionType in _selectedIons[charge])
                {
                    outFile.Write(ionType.Name + "\t");
                }
                outFile.Write("Unexplained");
                outFile.WriteLine();

                int maxRanks = _maxRanks;
                if (totalRanks < _maxRanks)
                    maxRanks = totalRanks;

                for (int i = 0; i < maxRanks; i++)
                {
                    outFile.Write(i + 1 + "\t");
                    // Write explained ion counts
                    for (int j = 0; j < _selectedIons[charge].Count; j++)
                    {
                        var ionFound = ionProbabilities[i][_selectedIons[charge][j]].Found;
                        outFile.Write("{0}\t", ionFound);
                    }

                    // Write average count for unexplained ions
                    int totalUnselected = 0;
                    int unselectedCount = 0;
                    for (int j = 0; j < _unselectedIons[charge].Count; j++)
                    {
                        totalUnselected += ionProbabilities[i][_unselectedIons[charge][j]].Found;
                        unselectedCount++;
                    }
                    outFile.Write(Math.Round((double)totalUnselected / unselectedCount, 2));
                    outFile.WriteLine();
                }
            }
        }

        // Read Configuration file
        private void InitTest(ConfigFileReader reader)
        {
            // Read program variables
            var config = reader.GetNodes("vars").First();
            _precursorCharge = Convert.ToInt32(config.Contents["precursorcharge"]);
            _precursorOffsetThreshold = Convert.ToDouble(config.Contents["precursoroffsetthreshold"]);
            _searchWidth = Convert.ToInt32(config.Contents["searchwidth"]);
            _retentionCount = Convert.ToInt32(config.Contents["retentioncount"]);
            _relativeIntensityThreshold = Convert.ToDouble(config.Contents["relativeintensitythreshold"]);
            _selectedIonThreshold = Convert.ToDouble(config.Contents["selectedionthreshold"]);
            var actStr = config.Contents["activationmethod"].ToLower();
            switch (actStr)
            {
                case "hcd":
                    _act = ActivationMethod.HCD;
                    break;
                case "cid":
                    _act = ActivationMethod.CID;
                    break;
                case "etd":
                    _act = ActivationMethod.ETD;
                    break;
            }

            _selectedIons = new List<IonType>[_precursorCharge];
            for (int i = 0; i < _precursorCharge; i++)
            {
                _selectedIons[i] = new List<IonType>();
            }
            _unselectedIons = new List<IonType>[_precursorCharge];
            for (int i = 0; i < _precursorCharge; i++)
            {
                _unselectedIons[i] = new List<IonType>();
            }

            _maxRanks = Convert.ToInt32(config.Contents["maxranks"]);

            // Read ion data
            var ionInfo = reader.GetNodes("ion").First();
            int totalCharges = Convert.ToInt32(ionInfo.Contents["totalcharges"]);
            var ionTypeStr = ionInfo.Contents["iontype"].Split(',');
            var ions = new BaseIonType[ionTypeStr.Length];
            for (int i = 0; i < ionTypeStr.Length; i++)
            {
                switch (ionTypeStr[i].ToLower())
                {
                    case "a":
                        ions[i] = BaseIonType.A;
                        break;
                    case "b":
                        ions[i] = BaseIonType.B;
                        break;
                    case "c":
                        ions[i] = BaseIonType.C;
                        break;
                    case "x":
                        ions[i] = BaseIonType.X;
                        break;
                    case "y":
                        ions[i] = BaseIonType.Y;
                        break;
                    case "z":
                        ions[i] = BaseIonType.Z;
                        break;
                }
            }
            var ionLossStr = ionInfo.Contents["losses"].Split(',');
            var ionLosses = new NeutralLoss[ionLossStr.Length];
            for (int i = 0; i < ionLossStr.Length; i++)
            {
                switch (ionLossStr[i].ToLower())
                {
                    case "noloss":
                        ionLosses[i] = NeutralLoss.NoLoss;
                        break;
                    case "nh3":
                        ionLosses[i] = NeutralLoss.NH3;
                        break;
                    case "h2o":
                        ionLosses[i] = NeutralLoss.H2O;
                        break;
                }
            }
            _ionTypeFactory = new IonTypeFactory(ions, ionLosses, totalCharges);
            _ionTypes = _ionTypeFactory.GetAllKnownIonTypes().ToList();
            var tempIonList = new List<IonType>();
            if (ionInfo.Contents.ContainsKey("exclusions"))
            {
                var ionExclusions = ionInfo.Contents["exclusions"].Split(',');
                tempIonList.AddRange(_ionTypes.Where(ionType => !ionExclusions.Contains(ionType.Name)));
                _ionTypes = tempIonList;
            }

            // Read input and output file names
            var fileInfo = reader.GetNodes("fileinfo").First();
            _names = fileInfo.Contents["name"].Split(',');
            _preTsv = fileInfo.Contents["tsvpath"];
            _preRaw = fileInfo.Contents["rawpath"];
            var outPathtemp = fileInfo.Contents["outpath"];
            _outPre = outPathtemp;

            _writeIonProbabilities = fileInfo.Contents.ContainsKey("ionProbabilityOutput");
            if (_writeIonProbabilities)
                _ionProbabilityOutFileName = _outPre + fileInfo.Contents["ionProbabilityOutput"];

            _writeRankProbabilities = fileInfo.Contents.ContainsKey("rankProbabilityOutput");
            if (_writeRankProbabilities)
                _rankProbabilityOutFileName = _outPre + fileInfo.Contents["rankProbabilityOutput"];

            _writePrecursorOffsetProbabilities = fileInfo.Contents.ContainsKey("precursorOffsetProbabilityOutput");
            if (_writePrecursorOffsetProbabilities)
                _precursorOffsetProbabilityOutFileName = _outPre + fileInfo.Contents["precursorOffsetProbabilityOutput"];

            _writeMassErrorProbabilities = fileInfo.Contents.ContainsKey("massErrorProbabilityOutput");
            if (_writeMassErrorProbabilities)
                _massErrorProbabilityOutFileName = _outPre + fileInfo.Contents["massErrorProbabilityOutput"];
        }
    }
}
