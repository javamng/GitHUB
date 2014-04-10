using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;

namespace InformedProteomics.Scoring.LikelihoodScoring
{
    public enum IonPairFound
    {
        Neither = 0,
        SecondIon = 1,
        FirstIon = 2,
        Both = 3
    };

    public class MassErrorTable
    {
        private readonly IonType[] _ionTypes;
        private readonly Tolerance _tolerance;

        private readonly Histogram<double> _massError;
        private int _totalPairs;
        private readonly Histogram<IonPairFound> _ionPairFrequency;

        public List<Probability<double>> MassError
        {
            get
            {
                var bins = _massError.Bins;
                var binEdges = _massError.BinEdges;
                return binEdges.Select((t, i) => new Probability<double>(t, bins[i].Count, _totalPairs)).ToList();
            }
        }

        public List<Probability<IonPairFound>> IonPairFrequency
        {
            get
            {
                var bins = _ionPairFrequency.Bins;
                var binEdges = _ionPairFrequency.BinEdges;
                return binEdges.Select(t => new Probability<IonPairFound>(t, bins.Count, _ionPairFrequency.Total)).ToList();
            }
        }

        public MassErrorTable(IonType[] ionTypes, Tolerance tolerance, double binWidth=0.01)
        {
            _ionTypes = ionTypes;
            _totalPairs = 0;
            _tolerance = tolerance;
            _massError = new Histogram<double>();
            _ionPairFrequency = new Histogram<IonPairFound>((IonPairFound[])Enum.GetValues(typeof(IonPairFound)));
            GenerateEdges(binWidth);
        }

        private void GenerateEdges(double binWidth)
        {
            const double searchWidth = 100;
            var binEdges = new List<double>();
            for (double width = 0; width < searchWidth; width += binWidth)
            {
                binEdges.Add(width);
            }
            _massError.BinEdges = binEdges.ToArray();
        }

        public void AddMatches(List<SpectrumMatch> matchList)
        {
            var aminoAcidSet = new AminoAcidSet();
            foreach (var match in matchList)
            {
                foreach (var ionType in _ionTypes)
                {
                    var charge = ionType.Charge;
                    var peptide = ionType.IsPrefixIon ? match.GetPeptidePrefix() : match.GetPeptideSuffix();
                    var ions = match.GetCleavageIons(ionType);
                    int nextIonIndex = 1;
                    while(nextIonIndex < ions.Count)
                    {
                        // look for peak for current ion and next ion
                        _totalPairs++;
                        var currIonIndex = nextIonIndex - 1;
                        var currMz = ions[currIonIndex].GetMonoIsotopicMz();
                        var nextMz = ions[nextIonIndex].GetMonoIsotopicMz();
                        var currPeak = match.Spectrum.FindPeak(currMz, _tolerance);
                        var nextPeak = match.Spectrum.FindPeak(nextMz, _tolerance);

                        if (currPeak == null && nextPeak == null)
                            _ionPairFrequency.AddDatum(IonPairFound.Neither);
                        else if (nextPeak == null)
                            _ionPairFrequency.AddDatum(IonPairFound.FirstIon);
                        else if (currPeak == null)
                            _ionPairFrequency.AddDatum(IonPairFound.SecondIon);
                        else
                        {
                            // found both peaks, compute mass error
                            _ionPairFrequency.AddDatum(IonPairFound.Both);
                            var aminoAcid = aminoAcidSet.GetAminoAcid(peptide[currIonIndex]);
                            var aaMass = aminoAcid.GetMass() / charge;
                            var massError = Math.Abs(Math.Abs(currMz - nextMz) - aaMass);
                            _massError.AddDatum(massError);
                        }
                        nextIonIndex++;
                    }
                }
            }
        }
    }
}
