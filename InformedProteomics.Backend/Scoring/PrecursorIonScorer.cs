using InformedProteomics.Backend.Data.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Scoring
{
    class PrecursorIonScorer
    {
        public List<DatabaseSubTargetResult> PrecursorResults { get; private set; }
        public DatabaseSubTargetResult PrecursorResultRep { get; private set; }
        public List<int> ChargeStateList { get; private set; }
        public float Score { get; private set; }

        public PrecursorIonScorer(DatabaseMultipleSubTargetResult matchedResult)
        {
            PrecursorResults = matchedResult.SubTargetResultList;
            PrecursorResultRep = matchedResult.PrecursorResultRep;
            ChargeStateList = matchedResult.ChargeStateList;
        }

       
    }
}
