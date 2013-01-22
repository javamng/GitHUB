using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Scoring
{
    class SpectrumScorer
    {
        public Dictionary<string, double>[] spectraPerFragment { get; private set; }
        public char precedingAA { get; private set; }
        public char succeedingAA { get; private set; }
        public float Score { get; private set; }

        public SpectrumScorer(Dictionary<string, double>[] spectraPerFragment, char precedingAA, char succeedingAA)
        {
            this.spectraPerFragment = spectraPerFragment;
            this.precedingAA = precedingAA;
            this.succeedingAA = succeedingAA;
            Score = GetScore();
        }

        private float GetScore()
        {
            throw new NotImplementedException();
        }

        internal List<string> GetUsedIonTypes()
        {
            throw new NotImplementedException();
        }



       
    }
}
