﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.Backend.Utils;
using MathNet.Numerics.Statistics;

namespace InformedProteomics.Backend.Data.Spectrometry
{
    public class Ms1FeatureCluster : Ms1Feature
    {
        public Ms1FeatureCluster(LcMsRun run, byte minCharge, IsotopeList isoList, double repMass, int repCharge, double repMz, int repScanNum)
            : base(run)
        {
            _minCharge = minCharge;
            TheoreticalEnvelope = isoList;
            Envelopes = new List<ObservedEnvelope>();
            _scores = new double[Ms1FeatureScore.Count];

            MaxCol = 0;
            MinCol = 0;
            SummedEnvelope = new double[isoList.Count];
            Flag = 0;

            RepresentativeMass = repMass;
            RepresentativeCharge = repCharge;
            RepresentativeMz = repMz;
            RepresentativeScanNum = repScanNum;
            _scoreInit = false;
        }
        
        public IsotopeList TheoreticalEnvelope { get; private set; }
        public double Probability { get; private set; }

        public int GoodEnvelopeCount { get; internal set; }
        public double GetScore(byte scoreType) { return _scores[scoreType]; }
        public double[] SummedEnvelope { get; private set; }
        
        public List<ObservedEnvelope> Envelopes { get; private set; }
        public int MaxCol { get; private set; }
        public int MinCol { get; private set; }

        public int MaxRow { get { return MaxCharge - _minCharge; } }
        public int MinRow { get { return MinCharge - _minCharge; } }

        public void AddMember(ObservedEnvelope envelope)
        {
            var ms1ScanNums = Run.GetMs1ScanVector();

            if (MaxScanNum == 0 || envelope.Col > MaxCol)
            {
                MaxCol = envelope.Col;
                MaxScanNum = ms1ScanNums[MaxCol];
            }
            if (MinScanNum == 0 || envelope.Col < MinCol)
            {
                MinCol = envelope.Col;
                MinScanNum = ms1ScanNums[MinCol];
            }
            if (MaxCharge == 0 || envelope.Row + _minCharge > MaxCharge)
            {
                MaxCharge = envelope.Row + _minCharge;
            }
            if (MinCharge == 0 || envelope.Row +_minCharge < MinCharge)
            {
                MinCharge = envelope.Row + _minCharge;
            }
            Envelopes.Add(envelope);
        }

        public void ClearMember()
        {
            MaxCol = 0;
            MinCol = 0;
            MaxScanNum = 0;
            MinScanNum = 0;
            MinCharge = 0;
            MaxCharge = 0;

            Envelopes.Clear();
        }

        public byte Flag;
    
        public double GetProbabilityByLogisticRegression()
        {
            var eta = LogisticRegressionBetaVector[0];
            for (var i = 1; i < LogisticRegressionBetaVector.Length; i++)
                eta += _scores[i - 1] * LogisticRegressionBetaVector[i];

            var pi = Math.Exp(eta);
            return pi / (pi + 1);
        }
        
        public void UpdateAbundance()
        {
            Abundance = 0d;
            foreach (var envelope in Envelopes)
            {
                foreach (var peak in envelope.Peaks)
                {
                    if (peak != null) Abundance += peak.Intensity;
                }
            }
        }

        public int ColumnLength { get { return (MaxScanNum == 0) ? 0 : MaxCol - MinCol + 1; } }

        public void ExpandElutionRange()
        {
            // considering DDA instrument
            if (Run.MaxMsLevel > 1 && NetLength < 0.01)
            {
                var ms1ScanNums = Run.GetMs1ScanVector();
                for (var i = MinCol - 1; i >= 0; i--)
                {
                    if (ms1ScanNums[MinCol] - ms1ScanNums[i] > (MinCol - i))
                    {
                        MinScanNum = ms1ScanNums[i];
                        break;
                    }
                }
                for (var i = MaxCol + 1; i < ms1ScanNums.Length; i++)
                {
                    if (ms1ScanNums[i] - ms1ScanNums[MaxCol] > (i - MaxCol))
                    {
                        MaxScanNum = ms1ScanNums[i];
                        break;
                    }
                }
            }
        }

        public IEnumerable<Ms1Peak> GetMajorPeaks()
        {
            return from envelope in Envelopes.Where(e => e.GoodEnough) from peak in envelope.Peaks where peak != null select peak;
        }

        public IEnumerable<Ms1Peak> GetMinorPeaks()
        {
            return from envelope in Envelopes.Where(e => !e.GoodEnough) from peak in envelope.Peaks where peak != null select peak;
        }

        public void InActivateSignificantPeaks()
        {
            foreach (var peak in GetMajorPeaks()) peak.InActivate();
        }

        public HashSet<Ms1FeatureCluster> OverlappedFeatures
        {
            // definition
            // Ovelapped features = features whose scores are affected by inactivating major peaks of this feature;
            //                    = features that share any major peaks in this features as their peaks
            //                     + features that share any minor peaks in this  feature as their major peaks
            get
            {
                var ret = new HashSet<Ms1FeatureCluster>();
                foreach (var peak in GetMajorPeaks())
                {
                    foreach (var f in peak.GetTaggedAllFeatures()) ret.Add(f);
                }
                foreach (var peak in GetMinorPeaks())
                {
                    foreach (var f in peak.GetTaggedMajorFeatures()) ret.Add(f);
                }

                return ret;
            }
        }

        public void Merge(Ms1FeatureCluster other)
        {
            MaxCol = Math.Max(other.MaxCol, MaxCol);
            MinCol = Math.Min(other.MinCol, MinCol);
            MaxScanNum = Math.Max(other.MaxScanNum, MaxScanNum);
            MinScanNum = Math.Min(other.MinScanNum, MinScanNum);
            MaxCharge = Math.Max(other.MaxCharge, MaxCharge);
            MinCharge = Math.Min(other.MinCharge, MinCharge);
        }

        public void UpdateScores(IList<Ms1Spectrum> spectra)
        {
/*            
            if (Envelopes.Count < 1) return;

            var bestEnvelopeCorrelation = 0.0d;
            var bestBhattacharyyaDistance = 1.0d;
            var bestRankSumScore = 1.0d;
            var bestPoissonScore = 1.0d;

            double goodEnvCorrTh;
            double goodEnvBcTh;

            if (RepresentativeMass < 15000)
            {
                goodEnvCorrTh = 0.6;
                goodEnvBcTh = 0.25;
            }
            else if (RepresentativeMass < 25000)
            {
                goodEnvCorrTh = 0.4;
                goodEnvBcTh = 0.3;
            }
            else
            {
                goodEnvCorrTh = 0.3;
                goodEnvBcTh = 0.3;
            }

            ObservedEnvelope repEnvelope = null;
            var envelopePerTime = new double[ColumnLength][];
            var envelopePerCharge = new double[ChargeLength][];
            for (var i = 0; i < ColumnLength; i++) envelopePerTime[i] = new double[TheoreticalEnvelope.Count];
            for (var i = 0; i < ChargeLength; i++) envelopePerCharge[i] = new double[TheoreticalEnvelope.Count];
            GoodEnvelopeCount = 0;

            var memberCorr = new double[Envelopes.Count];
            var memberBc = new double[Envelopes.Count];

            for (var e = 0; e < Envelopes.Count; e++)
            {
                var envelope = Envelopes[e];
                var spectrum = spectra[envelope.Col];
                var statSigTestResult = spectrum.TestStatisticalSignificance(TheoreticalEnvelope, envelope);
                var envCorr = envelope.GetPearsonCorrelation(TheoreticalEnvelope.Envelope);
                var bcDistance = envelope.GetBhattacharyyaDistance(TheoreticalEnvelope.EnvelopePdf);

                for (var i = 0; i < TheoreticalEnvelope.Count; i++)
                {
                    if (envelope.Peaks[i] == null || !envelope.Peaks[i].Active) continue;
                    envelopePerTime[envelope.Col - MinCol][i] += envelope.Peaks[i].Intensity;
                    envelopePerCharge[envelope.Row - MinRow][i] += envelope.Peaks[i].Intensity;
                }

                if (envCorr > goodEnvCorrTh && bcDistance < goodEnvBcTh && statSigTestResult.PoissonScore > LogP2 && statSigTestResult.RankSumScore > LogP2)
                {
                    if (!_scoreInit)
                    {
                        if (spectrum.CorrectChargeState(envelope, _minCharge))
                        {
                            if (envCorr > 0.6 && bcDistance < 0.2) envelope.GoodEnough = true;
                            GoodEnvelopeCount++;
                        }
                    }
                    else
                    {
                        GoodEnvelopeCount++;
                    }

                    if (bestPoissonScore < statSigTestResult.PoissonScore) bestPoissonScore = statSigTestResult.PoissonScore;
                    if (bestRankSumScore < statSigTestResult.RankSumScore) bestRankSumScore = statSigTestResult.RankSumScore;
                    if (bestEnvelopeCorrelation < envCorr) bestEnvelopeCorrelation = envCorr;
                    if (bcDistance < bestBhattacharyyaDistance)
                    {
                        bestBhattacharyyaDistance = bcDistance;
                        repEnvelope = envelope;
                    }
                }

                memberCorr[e] = envCorr;
                memberBc[e] = bcDistance;
            }

            if (GoodEnvelopeCount < 1 || repEnvelope == null) return;

            var repPeak = repEnvelope.Peaks[repEnvelope.RefIsotopeInternalIndex];
            RepresentativeCharge = _minCharge + repEnvelope.Row;
            RepresentativeMass = Ion.GetMonoIsotopicMass(repPeak.Mz, RepresentativeCharge, TheoreticalEnvelope[repEnvelope.RefIsotopeInternalIndex].Index);
            RepresentativeMz = repPeak.Mz;
            RepresentativeScanNum = Run.GetMs1ScanVector()[repEnvelope.Col];

            var bestBcPerTime = 10d;
            for (var col = MinCol; col <= MaxCol; col++)
            {
                var bc = TheoreticalEnvelope.GetBhattacharyyaDistance(envelopePerTime[col - MinCol]);
                if (bc < bestBcPerTime) bestBcPerTime = bc;
            }

            var bestBcPerCharge = 10d;
            var bestBcEvenCharge = 10d;
            var bestBcOddCharge = 10d;
            var abuPerCharge = new double[ChargeLength];
            var maxAbuRow = MinRow;

            //if (BcDistPerCharge == null || BcDistPerCharge.Length != ChargeLength) BcDistPerCharge = new double[ChargeLength];
            //else Array.Clear(BcDistPerCharge, 0, BcDistPerCharge.Length);

            for (var row = MinRow; row <= MaxRow; row++)
            {
                var bc = TheoreticalEnvelope.GetBhattacharyyaDistance(envelopePerCharge[row - MinRow]);
                //BcDistPerCharge[row - MinRow] = bc;
                if (bc < bestBcPerCharge) bestBcPerCharge = bc;

                if ((row + _minCharge) % 2 == 0)
                {
                    if (bc < bestBcEvenCharge) bestBcEvenCharge = bc;
                }
                else
                {
                    if (bc < bestBcOddCharge) bestBcOddCharge = bc;
                }
                abuPerCharge[row - MinRow] = envelopePerCharge[row - MinRow].Sum();

                if (abuPerCharge[maxAbuRow - MinRow] < abuPerCharge[row - MinRow]) maxAbuRow = row;
            }

            if (ChargeLength > 1)
            {
                var meanStd = abuPerCharge.MeanStandardDeviation();
                SetScore(Ms1FeatureScore.AbundanceChangesOverCharges, meanStd.Item2 / meanStd.Item1);
                SetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverEvenCharges, bestBcEvenCharge);
                SetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverOddCharges, bestBcOddCharge);
            }
            else
            {
                SetScore(Ms1FeatureScore.AbundanceChangesOverCharges, 0);
                SetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverEvenCharges, bestBcPerCharge);
                SetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverOddCharges, bestBcPerCharge);
            }
            
            var bestMzErrorPpm = 10d;
            var totalMzErrorPpm = 0d;
            var totalMzPairCount = 0;
            for (var i = 0; i < Envelopes.Count; i++)
            {
                if (memberBc[i] > 0.3 && memberCorr[i] < 0.5) continue;
                var envelope = Envelopes[i];
                var mzErrorPpm = 0d;
                var n = 0;
                var charge = _minCharge + envelope.Row;
                for (var j = 0; j < TheoreticalEnvelope.Count; j++)
                {
                    if (envelope.Peaks[j] == null || !envelope.Peaks[j].Active) continue;
                    var theoreticalMz = Ion.GetIsotopeMz(RepresentativeMass, charge, TheoreticalEnvelope[j].Index);
                    mzErrorPpm += (Math.Abs(envelope.Peaks[j].Mz - theoreticalMz) * 1e6) / theoreticalMz;
                    n++;
                }

                totalMzErrorPpm += mzErrorPpm;
                totalMzPairCount += n;
                mzErrorPpm /= n;
                if (mzErrorPpm < bestMzErrorPpm) bestMzErrorPpm = mzErrorPpm;
            }

            SetScore(Ms1FeatureScore.TotalMzError, totalMzErrorPpm / totalMzPairCount);
            SetScore(Ms1FeatureScore.EnvelopeCorrelation, bestEnvelopeCorrelation);
            SetScore(Ms1FeatureScore.RankSum, bestRankSumScore);
            SetScore(Ms1FeatureScore.Poisson, bestPoissonScore);
            SetScore(Ms1FeatureScore.BhattacharyyaDistance, bestBhattacharyyaDistance);
            SetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverCharges, bestBcPerCharge);
            SetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverTimes, bestBcPerTime);
            SetScore(Ms1FeatureScore.MzError, bestMzErrorPpm);
            SetSummedEnvelope(memberCorr, memberBc);
            Probability = GetProbabilityByLogisticRegression();

            _scoreInit = true;
 */
        }

        public void SetScore(byte scoreType, double scoreValue)
        {
            _scores[scoreType] = scoreValue;
        }


        public bool GoodEnough
        {
            get
            {
                if (GoodEnvelopeCount < 1) return false;

                // basic filtering conditions
                if (GetScore(Ms1FeatureScore.RankSum) < LogP2) return false;
                if (GetScore(Ms1FeatureScore.Poisson) < LogP2) return false;

                if (RepresentativeMass < 8000)
                {
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverEvenCharges) > 0.2) return false;
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverOddCharges) > 0.15) return false;
                    if (GetScore(Ms1FeatureScore.AbundanceChangesOverCharges) > 1.75) return false;
                }
                else if (RepresentativeMass < 15000)
                {
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverEvenCharges) > 0.1) return false;
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverOddCharges) > 0.08) return false;
                    if (GetScore(Ms1FeatureScore.AbundanceChangesOverCharges) > 1.5) return false;
                }
                else
                {
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverEvenCharges) > 0.1) return false;
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverOddCharges) > 0.08) return false;
                    if (GetScore(Ms1FeatureScore.AbundanceChangesOverCharges) > 1.3) return false;
                }

                if (RepresentativeMass < 15000)
                {
                    if (GetScore(Ms1FeatureScore.EnvelopeCorrelation) < 0.6) return false;
                    if (GetScore(Ms1FeatureScore.EnvelopeCorrelationSummed) < 0.83) return false; //0.85
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistance) > 0.15) return false; // 0.25
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummed) > 0.04) return false; // 0.02
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverCharges) > 0.08) return false; //0.08
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverTimes) > 0.1) return false; //0.1
                    if (GetScore(Ms1FeatureScore.XicCorrMean) > 0.2) return false;
                    if (GetScore(Ms1FeatureScore.XicCorrMin) > 0.15) return false;
                    if (GetScore(Ms1FeatureScore.MzError) > 3) return false;
                    if (GetScore(Ms1FeatureScore.TotalMzError) > 6) return false;
                }
                else
                {
                    if (GetScore(Ms1FeatureScore.EnvelopeCorrelation) < 0.3) return false;
                    if (GetScore(Ms1FeatureScore.EnvelopeCorrelationSummed) < 0.9) return false;
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistance) > 0.3) return false;
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummed) > 0.02) return false;
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverCharges) > 0.06) return false;
                    if (GetScore(Ms1FeatureScore.BhattacharyyaDistanceSummedOverTimes) > 0.07) return false;
                    if (GetScore(Ms1FeatureScore.XicCorrMean) > 0.4) return false;
                    if (GetScore(Ms1FeatureScore.XicCorrMin) > 0.25) return false;
                    if (GetScore(Ms1FeatureScore.MzError) > 4) return false;
                    if (GetScore(Ms1FeatureScore.TotalMzError) > 6) return false;
                }

                // further considerations: 
                // short features must have a strong evidence
                // high charge molecules should have high "summed" scores

                return true;
            }
        }

        
        private readonly double[] _scores;
        private readonly byte _minCharge;
        private bool _scoreInit;

        private static readonly double LogP1 = -Math.Log(0.01, 2);
        private static readonly double LogP2 = -Math.Log(0.02, 2);
        private static readonly double LogP5 = -Math.Log(0.05, 2);
        private static readonly double[] LogisticRegressionBetaVector = new double[]
        {
            -13.2533634280100,
            -1.15180441819635,
            12.7696665111291,
            0.0252168712214531,
            0.0345913636891164,
            -6.70169446935268,
            0.594148009986426,
            -2.54090490836123,
            -15.7574867934127,
            -3.96462362695165,
            -6.84017486290071,
            0.697533501805824,
            0.282385690399132,
            -3.98134292531727,
            -12.6184672341575,
            1.04475408931452
        };

        private void SetSummedEnvelope(double[] cellCorr, double[] cellBc)
        {
            if (Envelopes.Count == 1)
            {
                Array.Clear(SummedEnvelope, 0, TheoreticalEnvelope.Count);
                Envelopes[0].SumEnvelopeTo(SummedEnvelope);
                SetScore(Ms1FeatureScore.BhattacharyyaDistanceSummed, GetScore(Ms1FeatureScore.BhattacharyyaDistance));
                SetScore(Ms1FeatureScore.EnvelopeCorrelationSummed, GetScore(Ms1FeatureScore.EnvelopeCorrelation));
                return;
            }

            Array.Clear(SummedEnvelope, 0, TheoreticalEnvelope.Count);
            var tempEnvelop = new double[TheoreticalEnvelope.Count];
            var summedBc = 99999d;
            var index2 = Enumerable.Range(0, Envelopes.Count).ToArray();
            Array.Sort(cellBc, index2);
            foreach (var j in index2)
            {
                Array.Copy(SummedEnvelope, tempEnvelop, tempEnvelop.Length);
                Envelopes[j].SumEnvelopeTo(tempEnvelop);

                var tempBc = TheoreticalEnvelope.GetBhattacharyyaDistance(tempEnvelop);

                if (tempBc > summedBc) continue;
                summedBc = tempBc;

                //seleted[j] = true;
                Array.Copy(tempEnvelop, SummedEnvelope, tempEnvelop.Length);
            }
            SetScore(Ms1FeatureScore.BhattacharyyaDistanceSummed, summedBc);

            var index = Enumerable.Range(0, Envelopes.Count).ToArray();
            Array.Sort(cellCorr, index);
            Array.Reverse(index);
            var summedCorr = 0d;
            Array.Clear(SummedEnvelope, 0, SummedEnvelope.Length);
            foreach (var j in index)
            {
                Array.Clear(tempEnvelop, 0, tempEnvelop.Length);
                Array.Copy(SummedEnvelope, tempEnvelop, tempEnvelop.Length);
                Envelopes[j].SumEnvelopeTo(tempEnvelop);

                var tempCorr = TheoreticalEnvelope.GetPearsonCorrelation(tempEnvelop);
                if (tempCorr < summedCorr) continue;

                summedCorr = tempCorr;
                Array.Copy(tempEnvelop, SummedEnvelope, tempEnvelop.Length);
            }
            SetScore(Ms1FeatureScore.EnvelopeCorrelationSummed, summedCorr);
        }
    }


    public class ObservedEnvelope
    {
        public ObservedEnvelope(int row, int col, Ms1Peak[] peaks, IsotopeList isotopeList)
        {
            Row = row;
            Col = col;
            Peaks = new Ms1Peak[isotopeList.Count];
            Array.Copy(peaks, Peaks, isotopeList.Count);
            GoodEnough = false;
            foreach(var i in isotopeList.SortedIndexByIntensity)
            {
                if (Peaks[i] != null && Peaks[i].Active)
                {
                    RefIsotopeInternalIndex = i;
                    break;                    
                }
            }
        }

        public Ms1Peak[] Peaks { get; private set; }
        public Ms1Peak MinMzPeak
        {
            get
            {
                for (var i = 0; i < Peaks.Length; i++) if (Peaks[i] != null && Peaks[i].Active) return Peaks[i];
                return null;
            }
        }

        public Ms1Peak MaxMzPeak
        {
            get
            {
                for (var i = Peaks.Length-1; i >= 0; i--) if (Peaks[i] != null && Peaks[i].Active) return Peaks[i];
                return null;
            }
        }

        public int NumberOfPeaks
        {
            get { return Peaks.Count(x => x != null && x.Active); }
        }

        public double Abundance
        {
            get
            {
                return Peaks.Where(p => p != null && p.Active).Sum(p => p.Intensity);
            }
        }

        public void SumEnvelopeTo(double[] targetEnvelope)
        {
            Peaks.SumEnvelopeTo(targetEnvelope);
        }

        public double GetPearsonCorrelation(double[] theoreticalEnvelope)
        {
            return Peaks.GetPearsonCorrelation(theoreticalEnvelope);
        }

        public double GetBhattacharyyaDistance(double[] theoreticalEnvelope)
        {
            return Peaks.GetBhattacharyyaDistance(theoreticalEnvelope);
        }

        public int Row { get; private set; }
        public int Col { get; private set; }
        public bool GoodEnough { get; set; }
        public byte RefIsotopeInternalIndex { get; private set; }
    }
}
