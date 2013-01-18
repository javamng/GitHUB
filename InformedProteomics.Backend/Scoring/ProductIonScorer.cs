using InformedProteomics.Backend.Data.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Scoring
{
    class ProductIonScorer
    {
        public IList<DatabaseFragmentTargetResult> FragmentResultList { get; set; }
        public float Score { get; private set; }

        public ProductIonScorer(DatabaseMultipleSubTargetResult matchedResult)
        {
            FragmentResultList = matchedResult.FragmentResultList;
        }             
    }
}
