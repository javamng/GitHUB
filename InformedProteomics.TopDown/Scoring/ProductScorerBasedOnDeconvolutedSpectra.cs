﻿using System;
using System.Collections.Generic;
using System.IO;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;

namespace InformedProteomics.TopDown.Scoring
{
    public class ProductScorerBasedOnDeconvolutedSpectra
    {
        public ProductScorerBasedOnDeconvolutedSpectra(
            LcMsRun run, 
            int minProductCharge = 1, int maxProductCharge = 10,
            double productTolerancePpm = 10,
            int isotopeOffsetTolerance = 2,
            double filteringWindowSize = 1.1
            )
            : this(run, minProductCharge, maxProductCharge, new Tolerance(productTolerancePpm), isotopeOffsetTolerance, filteringWindowSize)
        {
        }

        public ProductScorerBasedOnDeconvolutedSpectra(
            LcMsRun run,
            int minProductCharge, int maxProductCharge,
            Tolerance productTolerance,
            int isotopeOffsetTolerance = 2, 
            double filteringWindowSize = 1.1)
        {
            _run = run;
            _minProductCharge = minProductCharge;
            _maxProductCharge = maxProductCharge;
            _productTolerance = productTolerance;
            FilteringWindowSize = filteringWindowSize;
            IsotopeOffsetTolerance = isotopeOffsetTolerance;
        }

        public double FilteringWindowSize { get; private set; }    // 1.1
        public int IsotopeOffsetTolerance { get; private set; }   // 2

        public IScorer GetMs2Scorer(int scanNum)
        {
            IScorer scorer;
            if (_ms2Scorer.TryGetValue(scanNum, out scorer)) return scorer;
            return null;
        }

        public void DeconvoluteProductSpectra()
        {
            _ms2Scorer = new Dictionary<int, IScorer>();
            foreach (var scanNum in _run.GetScanNumbers(2))
            {
                var spec = _run.GetSpectrum(scanNum) as ProductSpectrum;
                if (spec == null) continue;
                //if (spec.ScanNum != 879) continue;
                var deconvolutedSpec = GetDeconvolutedSpectrum(spec, _minProductCharge, _maxProductCharge, _productTolerance, CorrScoreThresholdMs2) as ProductSpectrum;
                if (deconvolutedSpec != null) _ms2Scorer[scanNum] = new DeconvScorer(deconvolutedSpec, _productTolerance);
            }
        }

        public void DeconvoluteProductSpectra(int scanNum)
        {
            _ms2Scorer = new Dictionary<int, IScorer>();
            var spec = _run.GetSpectrum(scanNum) as ProductSpectrum;
            if (spec == null) return;
            //if (spec.ScanNum != 879) continue;
            var deconvolutedSpec = GetDeconvolutedSpectrum(spec, _minProductCharge, _maxProductCharge, _productTolerance, CorrScoreThresholdMs2) as ProductSpectrum;
            if (deconvolutedSpec != null) _ms2Scorer[scanNum] = new DeconvScorer(deconvolutedSpec, _productTolerance);
        }


        public Spectrum GetDeconvolutedSpectrum(Spectrum spec, int minCharge, int maxCharge, Tolerance tolerance, double corrThreshold)
        {
            return GetDeconvolutedSpectrum(spec, minCharge, maxCharge, tolerance, corrThreshold, IsotopeOffsetTolerance,
                FilteringWindowSize);
        }

        public static Spectrum GetDeconvolutedSpectrum(Spectrum spec, int minCharge, int maxCharge, Tolerance tolerance, double corrThreshold,
                                                       int isotopeOffsetTolerance, double filteringWindowSize = 1.1)
        {
            var deconvolutedPeaks = Deconvoluter.GetDeconvolutedPeaks(spec, minCharge, maxCharge, isotopeOffsetTolerance, filteringWindowSize, tolerance, corrThreshold);
            var peakList = new List<Peak>();
            var binHash = new HashSet<int>();
            foreach (var deconvolutedPeak in deconvolutedPeaks)
            {
                var mass = deconvolutedPeak.Mass;
                var binNum = GetBinNumber(mass);
                if (!binHash.Add(binNum)) continue;
                peakList.Add(new Peak(mass, deconvolutedPeak.Intensity));
            }

            var productSpec = spec as ProductSpectrum;
            if (productSpec != null)
            {
                return new ProductSpectrum(peakList, spec.ScanNum)
                {
                    MsLevel = spec.MsLevel,
                    ActivationMethod = productSpec.ActivationMethod,
                    IsolationWindow = productSpec.IsolationWindow
                };
            }

            return new Spectrum(peakList, spec.ScanNum);
        }

        internal class DeconvScorer : IScorer
        {
            //private readonly BaseIonType[] _baseIonTypes;
            private readonly double _prefixOffsetMass;
            private readonly double _suffixOffsetMass;
            private readonly HashSet<int> _ionMassBins;
            internal DeconvScorer(ProductSpectrum deconvolutedSpectrum, Tolerance productTolerance)
            {
                if (deconvolutedSpectrum.ActivationMethod != ActivationMethod.ETD)
                {
                    _prefixOffsetMass = BaseIonType.B.OffsetComposition.Mass;
                    _suffixOffsetMass = BaseIonType.Y.OffsetComposition.Mass;
                }
                else
                {
                    _prefixOffsetMass = BaseIonType.C.OffsetComposition.Mass;
                    _suffixOffsetMass = BaseIonType.Z.OffsetComposition.Mass;
                }

                _ionMassBins = new HashSet<int>();
                foreach (var p in deconvolutedSpectrum.Peaks)
                {
                    var mass = p.Mz;
                    var deltaMass = productTolerance.GetToleranceAsDa(mass, 1);
                    var minMass = mass - deltaMass;
                    var maxMass = mass + deltaMass;

                    var minBinNum = GetBinNumber(minMass);
                    var maxBinNum = GetBinNumber(maxMass);
                    for (var binNum = minBinNum; binNum <= maxBinNum; binNum++)
                    {
                        _ionMassBins.Add(binNum);
                    }
                }
            }

            public double GetPrecursorIonScore(Ion precursorIon)
            {
                return 0.0;
            }

            public double GetFragmentScore(Composition prefixFragmentComposition, Composition suffixFragmentComposition)
            {
                var score = 0.0;

                var prefixMass = prefixFragmentComposition.Mass + _prefixOffsetMass;
                if (_ionMassBins.Contains(GetBinNumber(prefixMass))) score += 1;

                var suffixMass = suffixFragmentComposition.Mass + _suffixOffsetMass;
                if (_ionMassBins.Contains(GetBinNumber(suffixMass))) score += 1;
                return score;
            }
        }

        public static int GetBinNumber(double mass)
        {
            return (int) Math.Round(mass*RescalingConstantHighPrecision);
        }

        public static double GetMz(int binNum)
        {
            return binNum/RescalingConstantHighPrecision;
        }

        public void WriteToFile(string outputFilePath)
        {
            using (var writer = new BinaryWriter(File.Open(outputFilePath, FileMode.Create)))
            {
                writer.Write(_minProductCharge);
                writer.Write(_maxProductCharge);
            }
        }

        private Dictionary<int, IScorer> _ms2Scorer;    // scan number -> scorer

        private readonly LcMsRun _run;
        private readonly int _minProductCharge;
        private readonly int _maxProductCharge;
        private readonly Tolerance _productTolerance;
        private const double RescalingConstantHighPrecision = Constants.RescalingConstantHighPrecision;
        private const double CorrScoreThresholdMs2 = 0.7;
    }
}
