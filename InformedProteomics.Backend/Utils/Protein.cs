using System.Collections.Generic;
using InformedProteomics.Backend.Data.Sequence;

namespace InformedProteomics.Backend.Utils
{
    public class Protein : Sequence
    {
        public Protein(IEnumerable<AminoAcid> aaArr) : base(aaArr)
        {
        }
    }
}
