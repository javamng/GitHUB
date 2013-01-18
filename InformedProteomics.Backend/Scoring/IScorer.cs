using InformedProteomics.Backend.Data.Results;
using InformedProteomics.Backend.Data.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Scoring
{
    public interface IScorer
    {
        float Score { get; }
        Sequence Seq { get; }
        DatabaseMultipleSubTargetResult MatchedResult { get; }

    }
}
