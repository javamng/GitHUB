using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Scoring
{
    class FragmentXICScorer
    {
        private Dictionary<string, double[]> dictionary;
        private List<string> list;

        public FragmentXICScorer(Dictionary<string, double[]> dictionary, List<string> list)
        {
            // TODO: Complete member initialization
            this.dictionary = dictionary;
            this.list = list;
        }
        public float Score { get; private set; }
    }
}
