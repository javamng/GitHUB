using InformedProteomics.Backend.Data.Results;
using InformedProteomics.Backend.Data.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Scoring
{
    class Scorer : IScorer
    {
        public float PrecursorIonScore { get; private set; }
        public float ProductIonScore { get; private set; }

        public Scorer(Sequence seq, DatabaseMultipleSubTargetResult matchedResult)
        {
            Seq = seq;
            MatchedResult = matchedResult;
            PrecursorIonScore = new PrecursorIonScorer(MatchedResult).Score;
            ProductIonScore = new ProductIonScorer(MatchedResult).Score;
            Score = PrecursorIonScore + ProductIonScore;
        }
        
   

        public Sequence Seq
        {
            get;
            private set;
        }

        public DatabaseMultipleSubTargetResult MatchedResult
        {
            get;
            private set;
        }

        public float Score
        {
            get;
            private set;
        }

      
    }
}
