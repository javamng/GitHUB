﻿using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.Backend.Utils;

namespace InformedProteomics.TopDown.Scoring
{
    public class Ms1FtFilter : ISequenceFilter
    {
        public Ms1FtFilter(LcMsRun run, Tolerance massTolerance, string ms1FtFileName, double minLikelihoodRatio = 0)
        {
            _lcMsChargeMap = new LcMsChargeMap(run, massTolerance);
            _minLikelihoodRatio = minLikelihoodRatio;

            Read(ms1FtFileName);
        }

        private readonly LcMsChargeMap _lcMsChargeMap;

        public IEnumerable<int> GetMatchingMs2ScanNums(double sequenceMass)
        {
            return _lcMsChargeMap.GetMatchingMs2ScanNums(sequenceMass);
        }

        public IEnumerable<double> GetMatchingMass(int ms2Scan)
        {
            return _lcMsChargeMap.GetMatchingMass(ms2Scan);
        }

        private void Read(string ms1FtFileName)
        {
            var ftFileParser = new TsvFileParser(ms1FtFileName);
            var monoMassArr = ftFileParser.GetData("MonoMass").Select(Convert.ToDouble).ToArray();
            var minScanArray = ftFileParser.GetData("MinScan").Select(s => Convert.ToInt32(s)).ToArray();
            var maxScanArray = ftFileParser.GetData("MaxScan").Select(s => Convert.ToInt32(s)).ToArray();
            var repScanArray = ftFileParser.GetData("RepScan").Select(s => Convert.ToInt32(s)).ToArray();
            var minChargeArray = ftFileParser.GetData("MinCharge").Select(s => Convert.ToInt32(s)).ToArray();
            var maxChargeArray = ftFileParser.GetData("MaxCharge").Select(s => Convert.ToInt32(s)).ToArray();
            var scoreArray = ftFileParser.GetData("LikelihoodRatio").Select(Convert.ToDouble).ToArray();
            var featureCountFiltered = 0;

            for (var i = 0; i < monoMassArr.Length; i++)
            {
                //if (flagArray[i] == 0 && probArray[i] < _minProbability)  continue;
                if (scoreArray[i] < _minLikelihoodRatio) continue;
                featureCountFiltered++;
                var monoMass = monoMassArr[i];
                _lcMsChargeMap.SetMatches(monoMass, minScanArray[i], maxScanArray[i], repScanArray[i], minChargeArray[i], maxChargeArray[i]);
            }

            // NOTE: The DMS Analysis Manager looks for this statistic; do not change it
            Console.Write(@"{0}/{1} features loaded...", featureCountFiltered, monoMassArr.Length);
            _lcMsChargeMap.CreateMassToScanNumMap();
        }

        private readonly double _minLikelihoodRatio;
    }
}
