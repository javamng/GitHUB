using System;
using InformedProteomics.Backend.Data.Results;
using System.Collections.Generic;
using System.Linq;

namespace InformedProteomics.Backend.Scoring
{
    class ProductIonScorer
    {
        public Dictionary<int, Dictionary<string, double[]>> IonXICs { get; private set; }
        public Dictionary<int, Dictionary<string, double>[]> Spectra { get; private set; }
        public float Score { get; private set; }
        public string Sequence { get; private set; }
        public DatabaseSubTargetResult PrecursorResultRep { get; private set; }
        
        public ProductIonScorer(DatabaseMultipleSubTargetResult matchedResult)
        {
            GetFragmentResults(matchedResult);
            PrecursorResultRep = matchedResult.PrecursorResultRep;
            Sequence = matchedResult.FragmentResultList.ElementAt(0).DatabaseFragmentTarget.Code;
            Score = GetScore();
        }

        private void GetFragmentResults(DatabaseMultipleSubTargetResult matchedResult)
        {
            IonXICs = new Dictionary<int, Dictionary<string, double[]>>();
            Spectra = new Dictionary<int, Dictionary<string, double>[]>();

            foreach (var fragmentTargetResult in matchedResult.FragmentResultList)
            {
                var residueNumber = fragmentTargetResult.DatabaseFragmentTarget.Fragment.ResidueNumber;
                var ion = fragmentTargetResult.DatabaseFragmentTarget.Fragment.IonType;

                if (!IonXICs.ContainsKey(residueNumber))
                {
                    IonXICs.Add(residueNumber, new Dictionary<string, double[]>());
                    Spectra.Add(residueNumber, new Dictionary<string, double>[fragmentTargetResult.XYData.Yvalues.Length]);
                }

                IonXICs[residueNumber].Add(ion, fragmentTargetResult.XYData.Yvalues);
                var spectraPerFragment = Spectra[residueNumber];
                for (var i = 0; i < fragmentTargetResult.XYData.Xvalues.Length;i++)
                {
                    spectraPerFragment[i] = new Dictionary<string, double>
                        {
                            {ion, fragmentTargetResult.XYData.Yvalues[i]}
                        };
                }
            }
        }


        private float GetScore()
        {
            var score = 0f;
            foreach(var residueNumber in IonXICs.Keys)
            {
                var specScorer = new SpectrumScorer(Spectra[residueNumber], Sequence[residueNumber], Sequence[residueNumber+1]); // check if +1 or -1..
                var fragXICScorer = new FragmentXICScorer(IonXICs[residueNumber], specScorer.GetUsedIonTypes(), PrecursorResultRep.XYData.Yvalues);
                score = specScorer.Score + fragXICScorer.Score;
                Console.WriteLine(score);
            }
            return score;                        
        }

    }
}
