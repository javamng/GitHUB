﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;

namespace InformedProteomics.Scoring.TopDown
{
    public class FilteredProteinMassBinning : IMassBinning
    {
        public FilteredProteinMassBinning(IEnumerable<AminoAcid> aaArray, double maxProteinMass = 50000, int numBits = 27)
            : this(aaArray.Select(a => a.Mass).ToArray(), maxProteinMass, numBits)
        {
            
        }
        
        public FilteredProteinMassBinning(IEnumerable<Composition> compositions, double maxProteinMass = 50000, int numBits = 27)
            : this(GetUniqueMass(compositions), maxProteinMass, numBits)
        {

        }

        public FilteredProteinMassBinning(AminoAcidSet aaSet, double maxProteinMass = 50000, int numBits = 27)
            : this(GetUniqueMass(aaSet), maxProteinMass, numBits)
        {

        }
        
        public FilteredProteinMassBinning(double[] massList, double maxProteinMass = 50000, int numBits = 27)
        {
            Array.Sort(massList);
            MaxMass = maxProteinMass;
            MinMass = massList[0];

            _mzComparer = new MzComparerWithBinning(numBits);
            
            _minMzBinIndex = _mzComparer.GetBinNumber(MinMass);
            _maxMzBinIndex = _mzComparer.GetBinNumber(MaxMass);

            var numberOfMzBins = _maxMzBinIndex - _minMzBinIndex + 2; // pad zero mass bin
            _mzBinToFilteredBinMap = new int[numberOfMzBins];
            for (var i = 0; i < numberOfMzBins; i++) _mzBinToFilteredBinMap[i] = -1;

            var tempMap            = new int[numberOfMzBins];
            var fineNodes = new BitArray(Constants.GetBinNumHighPrecision(MaxMass));
            fineNodes[0] = true;
            
            var effectiveBinCounter = 0;
            for (var fineBinIdx = 0; fineBinIdx < fineNodes.Length; fineBinIdx++)
            {
                if (!fineNodes[fineBinIdx]) continue;

                var fineNodeMass = fineBinIdx / Constants.RescalingConstantHighPrecision;
                foreach (var m in massList)
                {
                    var validFineNodeIndex = Constants.GetBinNumHighPrecision(fineNodeMass + m);
                    if (validFineNodeIndex >= fineNodes.Length) break;
                    fineNodes[validFineNodeIndex] = true;
                }

                var binNum = _mzComparer.GetBinNumber(fineNodeMass);
                if (fineBinIdx == 0 || (binNum >= _minMzBinIndex && binNum <= _maxMzBinIndex && _mzBinToFilteredBinMap[binNum - _minMzBinIndex + 1] < 0))
                {
                    _mzBinToFilteredBinMap[binNum == 0 ? 0 : binNum - _minMzBinIndex + 1] = effectiveBinCounter;
                    tempMap[effectiveBinCounter] = binNum;
                    effectiveBinCounter++;
                }
            }
            _filteredBinToMzBinMap = new int[effectiveBinCounter];
            Array.Copy(tempMap, _filteredBinToMzBinMap, effectiveBinCounter);
        }

        public bool Filtered { get { return true;  } }

        public int GetBinNumber(double mass)
        {
            if (mass < double.Epsilon) return 0;
            
            var mzBinNum = _mzComparer.GetBinNumber(mass);
            
            if (mzBinNum >= _minMzBinIndex && mzBinNum <= _maxMzBinIndex)
                return _mzBinToFilteredBinMap[mzBinNum - _minMzBinIndex + 1];

            return -1;
        }

        public double GetMass(int binNumber)
        {
            if (binNumber < 1) return 0;
            if (binNumber >= NumberOfBins) return MaxMass;
            
            var mzBinNum = _filteredBinToMzBinMap[binNumber];
            return _mzComparer.GetMzAverage(mzBinNum);
        }
        
        public double GetMassStart(int binNumber)
        {
            if (binNumber < 1) return 0;
            if (binNumber >= NumberOfBins) return MaxMass;

            var mzBinNum = _filteredBinToMzBinMap[binNumber];
            return _mzComparer.GetMzStart(mzBinNum);
        }

        public double GetMassEnd(int binNumber)
        {
            if (binNumber < 1) return 0;
            if (binNumber >= NumberOfBins) return MaxMass;
            var mzBinNum = _filteredBinToMzBinMap[binNumber];
            return _mzComparer.GetMzEnd(mzBinNum);
        }

        public double MaxMass { get; private set; }
        public double MinMass { get; private set; }
        public int NumberOfBins { get { return _filteredBinToMzBinMap.Length; } }

        private static double[] GetUniqueMass(IEnumerable<Composition> compSet)
        {
            var massSet = new HashSet<double>();
            foreach (var comp in compSet) massSet.Add(comp.Mass);
            return massSet.ToArray();
        }

        private static double[] GetUniqueMass(AminoAcidSet aaSet)
        {
            var massSet = new HashSet<double>();
            var modParam = aaSet.GetModificationParams();
            var aminoAcidArray = AminoAcid.StandardAminoAcidArr;
            
            foreach (var aa in aminoAcidArray)
            {
                massSet.Add(aa.Composition.Mass);

                foreach (var modIndex in aaSet.GetModificationIndices(aa.Residue))
                {
                    var aa2 = new ModifiedAminoAcid(aa, modParam.GetModification(modIndex));
                    massSet.Add(aa2.Composition.Mass);
                }
            }

            return massSet.ToArray();
        }

        private readonly int[] _mzBinToFilteredBinMap;
        private readonly int[] _filteredBinToMzBinMap;

        private readonly MzComparerWithBinning _mzComparer;
        private readonly int _minMzBinIndex;
        private readonly int _maxMzBinIndex;

    }
}