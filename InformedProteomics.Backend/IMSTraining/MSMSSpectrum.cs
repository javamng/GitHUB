﻿using System;
using System.Collections.Generic;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.IMSScoring;

namespace InformedProteomics.Backend.IMSTraining
{
    public class MSMSSpectrum : List<MSMSSpectrumPeak>
    {
        public Sequence Annotation { get; private set; }
        public int Charge { get; private set; }
        public double PrecursorMz { get; private set; }

        public MSMSSpectrum(int charge, double precursorMz)
        {
            Charge = charge;
            PrecursorMz = precursorMz;
        }

        public MSMSSpectrum(int charge, double precursorMz, Sequence annotation) : this(charge, precursorMz)
        {
            Annotation = annotation;
        }

        public MSMSSpectrumPeak GetPeak(double mz, Tolerance tolerance)
        {
            var matchList = GetPeaks(mz, tolerance);
            if (matchList.Count == 0) return new MSMSSpectrumPeak(mz, 0);

            var maxPeak = matchList[0];
            var intensityComparer = new MSMSSpectrumPeak.IntensityComparer();
            for (var i = 1; i < matchList.Count; i++)
            {
                var p = matchList[i];
                if (intensityComparer.Compare(maxPeak, p) < 0)
                    maxPeak = p;
            }
            return maxPeak;
        }

        public List<MSMSSpectrumPeak> GetPeaks(double mz, Tolerance tolerance)
        {
            var minMz = mz - tolerance.GetToleranceAsTh(mz);
            var maxMz = mz + tolerance.GetToleranceAsTh(mz);
            var matchList = new List<MSMSSpectrumPeak>();
            var start = BinarySearch(new MSMSSpectrumPeak(minMz, 0));
            if (start < 0) start = -start - 1;
            for (var i = start; i < Count; i++)
            {
                var p = this[i];
                if (p.Mz > maxMz) break;
                matchList.Add(p);
            }
            return matchList;
        }


        public GroupParameter GetGroupParameter(Sequence annotation, int cutNumber)
        {
            var precursorIon = new Ion(annotation.Composition + Composition.H2O, Charge);
            var prefixComposition = annotation.GetComposition(0, cutNumber);
            return new GroupParameter(prefixComposition, annotation[cutNumber - 1].Residue, annotation[cutNumber].Residue, precursorIon); 
        }

        public Tuple<List<MSMSSpectrumPeak>, float[]> GetIsotopomerEnvelop(Sequence annotation, int cutNumber, IonType ionType, Tolerance tolerance)
        {
            var prefixComposition = annotation.GetComposition(0, cutNumber);
            var suffixCompostion = annotation.Composition - prefixComposition;
            var isotopomerEnvelop = new List<MSMSSpectrumPeak>();
            var composition = ionType.IsPrefixIon ? prefixComposition : suffixCompostion;
            var ion = ionType.GetIon(composition);
            for (var i = -FeatureNode.NumMinusIsotope; i < FeatureNode.NumSupport - FeatureNode.NumMinusIsotope; i++)
            {
                var mz = ion.GetIsotopeMz(i);
                isotopomerEnvelop.Add(GetPeak(mz, tolerance));
            }
            return new Tuple<List<MSMSSpectrumPeak>, float[]>(isotopomerEnvelop, ion.Composition.GetApproximatedIsotopomerEnvelop());
        } 

        public List<MSMSSpectrumPeak> GetExplainedPeaks(Sequence annotation, int cutNumber, List<IonType> ionTypes, Tolerance tolerance)
        {  
            var prefixComposition = annotation.GetComposition(0, cutNumber);
            var suffixCompostion = annotation.Composition - prefixComposition;
            var peakList = new List<MSMSSpectrumPeak>();
            foreach (var ionType in ionTypes)
            {
                var composition = ionType.IsPrefixIon ? prefixComposition : suffixCompostion;
                var ion = ionType.GetIon(composition);
                peakList.Add(GetPeak(ion.GetMz(), tolerance));
            }
            return peakList;
        }

        public List<IonType> GetExplainingIonTypes(Sequence annotation, int cutNumber, List<IonType> allKnownIonTypes, Tolerance tolerance)
        {
            var ionTypes = new List<IonType>();
            var peaks = GetExplainedPeaks(annotation, cutNumber, allKnownIonTypes, tolerance);
            for (var i = 0; i < peaks.Count; i++)
            {
                if(peaks[i].Intensity>0)
                    ionTypes.Add(allKnownIonTypes[i]);
            }
            return ionTypes;
        }
    }
}
