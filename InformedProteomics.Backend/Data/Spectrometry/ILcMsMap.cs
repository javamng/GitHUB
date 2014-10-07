using System.Collections.Generic;

namespace InformedProteomics.Backend.Data.Spectrometry
{
    public interface ILcMsMap
    {
        IEnumerable<List<ChargeScanRange>> GetProbableChargeScanRegions(double monoIsotopicMass);
    }
}
