﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.Database;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.TopDown.Scoring;

namespace InformedProteomics.TopDown.Execution
{
    public class IcTopDownLauncher
    {
        public const int NumMatchesPerSpectrum = 1;
        public const string TargetFileExtension = ".ic2tresult";
        public const string DecoyFileExtension = ".ic2dresult";

        public IcTopDownLauncher(
            string specFilePath,
            string dbFilePath,
            AminoAcidSet aaSet,
            int minSequenceLength = 21,
            int maxSequenceLength = 300,
            int maxNumNTermCleavages = 1,
            int maxNumCTermCleavages = 0,
            int minPrecursorIonCharge = 2,
            int maxPrecursorIonCharge = 30,
            int minProductIonCharge = 1,
            int maxProductIonCharge = 15,
            double precursorIonTolerancePpm = 10,
            double productIonTolerancePpm = 10,
            bool? runTargetDecoyAnalysis = true,
            int searchMode = 1)
        {
            SpecFilePath = specFilePath;
            DatabaseFilePath = dbFilePath;
            AminoAcidSet = aaSet;
            MinSequenceLength = minSequenceLength;
            MaxSequenceLength = maxSequenceLength;
            MaxNumNTermCleavages = maxNumNTermCleavages;
            MaxNumCTermCleavages = maxNumCTermCleavages;
            MinPrecursorIonCharge = minPrecursorIonCharge;
            MaxPrecursorIonCharge = maxPrecursorIonCharge;
            MinProductIonCharge = minProductIonCharge;
            MaxProductIonCharge = maxProductIonCharge;
            PrecursorIonTolerance = new Tolerance(precursorIonTolerancePpm);
            ProductIonTolerance = new Tolerance(productIonTolerancePpm);
            RunTargetDecoyAnalysis = runTargetDecoyAnalysis;
            SearchMode = 0;
            SearchMode = searchMode;
        }
        
        // Consider all subsequenes of lengths [minSequenceLength,maxSequenceLength]
        public IcTopDownLauncher(
            string specFilePath,
            string dbFilePath,
            AminoAcidSet aaSet,
            int minSequenceLength = 21,
            int maxSequenceLength = 300,
            int minPrecursorIonCharge = 2,
            int maxPrecursorIonCharge = 30,
            int minProductIonCharge = 1,
            int maxProductIonCharge = 15,
            double precursorIonTolerancePpm = 10,
            double productIonTolerancePpm = 10,
            bool? runTargetDecoyAnalysis = true) : this(
            specFilePath,
            dbFilePath,
            aaSet,
            minSequenceLength,
            maxSequenceLength,
            0,
            0,
            minPrecursorIonCharge,
            maxPrecursorIonCharge,
            minProductIonCharge,
            maxProductIonCharge,
            precursorIonTolerancePpm,
            productIonTolerancePpm,
            runTargetDecoyAnalysis,
            0
            )
        {
        }

        public string SpecFilePath { get; private set; }
        public string DatabaseFilePath { get; private set; }
        public AminoAcidSet AminoAcidSet { get; private set; }
        public int MinSequenceLength { get; private set; }
        public int MaxSequenceLength { get; private set; }
        public int MaxNumNTermCleavages { get; private set; }
        public int MaxNumCTermCleavages { get; private set; }
        public int MinPrecursorIonCharge { get; private set; }
        public int MaxPrecursorIonCharge { get; private set; }
        public int MinProductIonCharge { get; private set; }
        public int MaxProductIonCharge { get; private set; }
        public Tolerance PrecursorIonTolerance { get; private set; }
        public Tolerance ProductIonTolerance { get; private set; }
        public bool? RunTargetDecoyAnalysis { get; private set; } // true: target and decoy, false: target only, null: decoy only

        // 0: all internal sequences, 
        // 1: #NCleavges <= Max OR Cleavages <= Max (Default)
        // 2: 1: #NCleavges <= Max AND Cleavages <= Max
        public int SearchMode { get; private set; } 

        private LcMsRun _run;
        private ProductScorerBasedOnDeconvolutedSpectra _ms2ScorerFactory;

        public void RunSearch(double corrThreshold)
        {
            var sw = new Stopwatch();

            Console.Write("Reading raw file...");
            sw.Start();
            _run = LcMsRun.GetLcMsRun(SpecFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);
            sw.Stop();
            var sec = sw.ElapsedTicks / (double)Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

            sw.Reset();
            Console.Write("Determining precursor masses...");
            sw.Start();
            var ms1Filter = new Ms1IsotopeAndChargeCorrFilter(_run, MinPrecursorIonCharge, MaxPrecursorIonCharge, 
                10, 3000, 50000, corrThreshold, corrThreshold, corrThreshold);
            sec = sw.ElapsedTicks / (double)Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

            sw.Reset();
            Console.Write("Deconvoluting MS2 spectra...");
            sw.Start();
            _ms2ScorerFactory = new ProductScorerBasedOnDeconvolutedSpectra(
                _run,
                MinProductIonCharge, MaxProductIonCharge,
                ProductIonTolerance
                );
            _ms2ScorerFactory.DeconvoluteProductSpectra();
            sw.Stop();
            sec = sw.ElapsedTicks / (double)Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

            // Target database
            var targetDb = new FastaDatabase(DatabaseFilePath);

            if (RunTargetDecoyAnalysis != null)
            {
                sw.Reset();
                Console.Write("Reading the target database...");
                sw.Start();
                targetDb.Read();
                var indexedDbTarget = new IndexedDatabase(targetDb);
                IEnumerable<AnnotationAndOffset> annotationsAndOffsets;
                if (SearchMode == 0)
                {
                    annotationsAndOffsets = indexedDbTarget.AnnotationsAndOffsetsNoEnzyme(MinSequenceLength, MaxSequenceLength);
                }
                else if (SearchMode == 2)
                {
                    annotationsAndOffsets = indexedDbTarget.IntactSequenceAnnotationsAndOffsets(MinSequenceLength,
                        MaxSequenceLength, MaxNumCTermCleavages);
                }
                else
                {
                    annotationsAndOffsets = indexedDbTarget
                        .SequenceAnnotationsAndOffsetsWithNtermOrCtermCleavageNoLargerThan(
                            MinSequenceLength, MaxSequenceLength, MaxNumNTermCleavages, MaxNumCTermCleavages);
                }

                //var annotationsAndOffsets = (MaxNumNTermCleavages != null && MaxNumCTermCleavages != null)
                sw.Stop();
                sec = sw.ElapsedTicks / (double)Stopwatch.Frequency;
                Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

                sw.Reset();
                Console.WriteLine("Searching the target database");
                sw.Start();
                var targetMatches = RunSearch(annotationsAndOffsets, ms1Filter);
                var targetOutputFilePath = Path.ChangeExtension(SpecFilePath, TargetFileExtension);
                WriteResultsToFile(targetMatches, targetOutputFilePath, targetDb);
                sw.Stop();
                sec = sw.ElapsedTicks / (double)Stopwatch.Frequency;
                Console.WriteLine(@"Target database search elapsed Time: {0:f4} sec", sec);                
            }

            if (RunTargetDecoyAnalysis == true || RunTargetDecoyAnalysis == null)
            {
                // Decoy database
                sw.Reset();
                Console.Write("Reading the decoy database...");
                sw.Start();
                var decoyDb = targetDb.Decoy(null, true);
                decoyDb.Read();
                var indexedDbDecoy = new IndexedDatabase(decoyDb);
                IEnumerable<AnnotationAndOffset> annotationsAndOffsets;
                if (SearchMode == 0)
                {
                    annotationsAndOffsets = indexedDbDecoy.AnnotationsAndOffsetsNoEnzyme(MinSequenceLength, MaxSequenceLength);
                }
                else if (SearchMode == 2)
                {
                    annotationsAndOffsets = indexedDbDecoy.IntactSequenceAnnotationsAndOffsets(MinSequenceLength,
                        MaxSequenceLength, MaxNumCTermCleavages);
                }
                else
                {
                    annotationsAndOffsets = indexedDbDecoy
                        .SequenceAnnotationsAndOffsetsWithNtermOrCtermCleavageNoLargerThan(
                            MinSequenceLength, MaxSequenceLength, MaxNumNTermCleavages, MaxNumCTermCleavages);
                } 
                sec = sw.ElapsedTicks / (double)Stopwatch.Frequency;
                Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

                sw.Reset();
                Console.WriteLine("Searching the decoy database");
                sw.Start();
                var decoyMatches = RunSearch(annotationsAndOffsets, ms1Filter);
                var decoyOutputFilePath = Path.ChangeExtension(SpecFilePath, DecoyFileExtension);
                WriteResultsToFile(decoyMatches, decoyOutputFilePath, decoyDb);
                sw.Stop();
                sec = sw.ElapsedTicks / (double)Stopwatch.Frequency;
                Console.WriteLine(@"Decoy database search elapsed Time: {0:f4} sec", sec);
            }
            Console.WriteLine("Done.");
        }

        private void WriteResultsToFile(SortedSet<ProteinSpectrumMatch>[] matches, string outputFilePath, FastaDatabase database)
        {
            using (var writer = new StreamWriter(outputFilePath))
            {
                writer.WriteLine("ScanNum\tSequence\tModifications\tComposition\tProteinName\tProteinDesc\tProteinLength\tStart\tEnd\tCharge\tMostAbundantIsotopeMz\tMass\tScore");
                for (var scanNum = _run.MinLcScan; scanNum <= _run.MaxLcScan; scanNum++)
                {
                    if (matches[scanNum] == null) continue;
                    foreach (var match in matches[scanNum].Reverse())
                    {
                        var sequence = match.Sequence;
                        var offset = match.Offset;
                        var start = database.GetZeroBasedPositionInProtein(offset) + 1 + match.NumNTermCleavages;
                        var end = start + sequence.Length - 1;
                        var proteinName = database.GetProteinName(match.Offset);
                        var protLength = database.GetProteinLength(proteinName);
                        var ion = match.Ion;

                        writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}",
                            scanNum,
                            sequence, // Sequence
                            match.Modifications, // Modifications
                            ion.Composition, // Composition
                            proteinName, // ProteinName
                            database.GetProteinDescription(match.Offset), // ProteinDescription
                            protLength, // ProteinLength
                            start, // Start
                            end, // End
                            ion.Charge, // precursorCharge
                            ion.GetMostAbundantIsotopeMz(), // MostAbundantIsotopeMz
                            ion.Composition.Mass,   // Mass
                            match.Score);   // Score
                    }
                }
            }
        }

        private SortedSet<ProteinSpectrumMatch>[] RunSearch(IEnumerable<AnnotationAndOffset> annotationsAndOffsets, ISequenceFilter ms1Filter)
        {
            var sw = new Stopwatch();
            var numProteins = 0;
            sw.Reset();
            sw.Start();

            var matches = new SortedSet<ProteinSpectrumMatch>[_run.MaxLcScan+1];

            var maxNumNTermCleavages = SearchMode == 2 ? MaxNumNTermCleavages : 0;

            foreach (var annotationAndOffset in annotationsAndOffsets)
            {
                ++numProteins;

                var annotation = annotationAndOffset.Annotation;
                var offset = annotationAndOffset.Offset;

                if (numProteins % 100000 == 0)
                {
                    Console.Write("Processing {0}{1} proteins...", numProteins,
                        numProteins == 1 ? "st" : numProteins == 2 ? "nd" : numProteins == 3 ? "rd" : "th");
                    if (numProteins != 0)
                    {
                        sw.Stop();
                        var sec = sw.ElapsedTicks / (double)Stopwatch.Frequency;
                        Console.WriteLine("Elapsed Time: {0:f4} sec", sec);
                        sw.Reset();
                        sw.Start();
                    }
                }

                var seqGraph = SequenceGraph.CreateGraph(AminoAcidSet, annotation);
                if (seqGraph == null)
                {
//                    Console.WriteLine("Ignoring illegal protein: {0}", annotation);
                    continue;
                }

                for (var numNTermCleavage = 0; numNTermCleavage <= maxNumNTermCleavages; numNTermCleavage++)
                {
                    var protCompositions = seqGraph.GetSequenceCompositionsWithNTermCleavage(numNTermCleavage);
                    for (var modIndex = 0; modIndex < protCompositions.Length; modIndex++)
                    {
                        seqGraph.SetSink(modIndex, numNTermCleavage);
                        var protCompositionWithH2O = seqGraph.GetSinkSequenceCompositionWithH2O();
                        var sequenceMass = protCompositionWithH2O.Mass;
                        var modCombinations = seqGraph.ModificationParams.GetModificationCombination(modIndex);

                        foreach (var ms2ScanNum in ms1Filter.GetMatchingMs2ScanNums(sequenceMass))
                        {
                            var spec = _run.GetSpectrum(ms2ScanNum) as ProductSpectrum;
                            if (spec == null) continue;
                            var charge =
                                (int)Math.Round(sequenceMass / (spec.IsolationWindow.IsolationWindowTargetMz - Constants.Proton));
                            var scorer = _ms2ScorerFactory.GetMs2Scorer(ms2ScanNum);
                            var score = seqGraph.GetScore(charge, scorer);
                            if (score <= 3) continue;

                            var precursorIon = new Ion(protCompositionWithH2O, charge);
                            var sequence = annotation.Substring(numNTermCleavage + 2,
                                annotation.Length - 4 - numNTermCleavage);
                            var prsm = new ProteinSpectrumMatch(sequence, ms2ScanNum, offset, numNTermCleavage, modCombinations,
                                precursorIon, score);

                            
                            if (matches[ms2ScanNum] == null)
                            {
                                matches[ms2ScanNum] = new SortedSet<ProteinSpectrumMatch> { prsm };
                            }
                            else // already exists
                            {
                                var existingMatches = matches[ms2ScanNum];
                                if (existingMatches.Count < NumMatchesPerSpectrum) existingMatches.Add(prsm);
                                else
                                {
                                    var minScore = existingMatches.Min.Score;
                                    if (score > minScore)
                                    {
                                        existingMatches.Add(prsm);
                                        existingMatches.Remove(existingMatches.Min);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return matches;
        }

        //public void RunIntactProteinSearch()
        //{
        //    var sw = new System.Diagnostics.Stopwatch();

        //    Console.Write("Reading the raw file...");
        //    sw.Start();
        //    _run = LcMsRun.GetLcMsRun(SpecFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);
        //    sw.Stop();
        //    var sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
        //    Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

        //    sw.Reset();
        //    Console.Write("Determining precursor masses...");
        //    sw.Start();
        //    var ms1Filter = new Ms1IsotopeMostAbundantPlusOneFilter(_run, MinPrecursorIonCharge, MaxPrecursorIonCharge,
        //        10, 3000, 50000);
        //    sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
        //    Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

        //    sw.Reset();
        //    Console.Write("Deconvoluting MS2 spectra...");
        //    sw.Start();
        //    _ms2ScorerFactory = new ProductScorerBasedOnDeconvolutedSpectra(
        //        _run,
        //        MinProductIonCharge, MaxProductIonCharge,
        //        ProductIonTolerance
        //        );
        //    _ms2ScorerFactory.DeconvoluteProductSpectra();
        //    sw.Stop();
        //    sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
        //    Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

        //    // Target database
        //    sw.Reset();
        //    Console.Write("Reading the target database...");
        //    sw.Start();
        //    var targetDb = new FastaDatabase(DatabaseFilePath);
        //    targetDb.Read();
        //    var indexedDbTarget = new IndexedDatabase(targetDb);
        //    sw.Stop();
        //    sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
        //    Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

        //    sw.Reset();
        //    Console.WriteLine("Initial search to determine search parameters");
        //    sw.Start();
        //    var annotationsAndOffsets = indexedDbTarget.IntactSequenceAnnotationsAndOffsets(MinSequenceLength,
        //        MaxSequenceLength, 0);
        //    var targetMatches = RunSearch(annotationsAndOffsets, ms1Filter);
        //    var targetOutputFilePath = Path.ChangeExtension(SpecFilePath, ".icintact");

        //    WriteResultsToFile(targetMatches, targetOutputFilePath, targetDb);
        //    Console.WriteLine("Done.");
        //}
    }
}
