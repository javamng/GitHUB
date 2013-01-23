using System.Collections.Generic;

namespace InformedProteomics.Backend.Scoring
{
    class SpectrumScorer
    {
        public Dictionary<string, double>[] SpectraPerFragment { get; private set; }
        public char PrecedingAa { get; private set; }
        public char SucceedingAa { get; private set; }
        public float Score { get; private set; }

        public SpectrumScorer(Dictionary<string, double>[] spectraPerFragment, char precedingAA, char succeedingAA)
        {
            SpectraPerFragment = spectraPerFragment;
            PrecedingAa = precedingAA;
            SucceedingAa = succeedingAA;
            Score = GetScore();
        }

        private float GetScore()
        {
            return 0;
        }

        internal List<string> GetUsedIonTypes()
        {
            return new List<string>(SpectraPerFragment[0].Keys);
        }



       
    }
}
