using System.Collections.Generic;

namespace InformedProteomics.Backend.Scoring
{
    public class FragmentSpectrum : Dictionary<IonType, double>
    {
        public double GetIntensity(IonType ion)
        {
            return !ContainsKey(ion) ? 0 : this[ion];
        }

        public void AddIntensity(IonType ion, double intensity)
        {
            var prevIntensity = 0.0;
            if (ContainsKey(ion)) prevIntensity = this[ion];
            this[ion] = prevIntensity + intensity;
        }
    }
}
