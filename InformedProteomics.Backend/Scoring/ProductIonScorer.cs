using InformedProteomics.Backend.Data.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Scoring
{
    class ProductIonScorer
    {
        public Dictionary<int, Dictionary<string, double[]>> XICperIonType { get; private set; }
        public Dictionary<int, Dictionary<string, double>[]> spectra { get; private set; }
        public float Score { get; private set; }
        public string sequence { get; private set; }
        public DatabaseSubTargetResult PrecursorResultRep { get; private set; }
        
        public ProductIonScorer(DatabaseMultipleSubTargetResult matchedResult)
        {
            GetFragmentResults(matchedResult);
            PrecursorResultRep = matchedResult.PrecursorResultRep;
            sequence = matchedResult.FragmentResultList.ElementAt(0).DatabaseFragmentTarget.Code;
            Score = GetScore();
        }

        private void GetFragmentResults(DatabaseMultipleSubTargetResult matchedResult)
        {
            XICperIonType = new Dictionary<int, Dictionary<string, double[]>>();
            spectra = new Dictionary<int, Dictionary<string, double>[]>();

            foreach (var fragmentTargetResult in matchedResult.FragmentResultList)
            {
                int residueNumber = fragmentTargetResult.DatabaseFragmentTarget.Fragment.ResidueNumber;
                string ion = fragmentTargetResult.DatabaseFragmentTarget.Fragment.IonType;

                if (!XICperIonType.ContainsKey(residueNumber))
                {
                    XICperIonType.Add(residueNumber, new Dictionary<string, double[]>());
                    spectra.Add(residueNumber, new Dictionary<string, double>[fragmentTargetResult.XYData.Yvalues.Length]);
                }

                XICperIonType[residueNumber].Add(ion, fragmentTargetResult.XYData.Yvalues);
                Dictionary<string, double>[] spectraPerFragment = spectra[residueNumber];
                for (int i = 0; i < fragmentTargetResult.XYData.Xvalues.Length;i++)
                {
                    spectraPerFragment[i] = new Dictionary<string, double>();
                    spectraPerFragment[i].Add(ion, fragmentTargetResult.XYData.Yvalues[i]);
                }
            }
        }


        private float GetScore()
        {
            float score = 0;
            foreach(int residueNumber in XICperIonType.Keys)
            {
                SpectrumScorer specScorer = new SpectrumScorer(spectra[residueNumber], sequence[residueNumber], sequence[residueNumber+1]); // check if +1 or -1..
                FragmentXICScorer fragXICScorer = new FragmentXICScorer(XICperIonType[residueNumber], specScorer.GetUsedIonTypes());
                score = specScorer.Score + fragXICScorer.Score;
            }
            return score;                        
        }

    }
}
