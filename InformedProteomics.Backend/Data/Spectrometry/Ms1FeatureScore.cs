﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Data.Spectrometry
{
    public static class Ms1FeatureScore
    {
        public const byte EnvelopeCorrelation = 0;
        public const byte EnvelopeCorrelationSummed = 1;

        public const byte RankSum = 2;
        public const byte Poisson = 3;

        public const byte BhattacharyyaDistance = 4;
        public const byte BhattacharyyaDistanceSummed = 5;
        public const byte BhattacharyyaDistanceSummedOverCharges = 6;
        public const byte BhattacharyyaDistanceSummedOverTimes = 7;

        public const byte XicCorrMean = 8;
        public const byte XicCorrMin = 9;
        public const byte MzError = 10;
        public const byte TotalMzError = 11;

        public const byte BhattacharyyaDistanceSummedOverEvenCharges = 12;
        public const byte BhattacharyyaDistanceSummedOverOddCharges = 13;

        public const byte AbundanceChangesOverCharges = 14;

        public const byte Count = 15;
    }
}