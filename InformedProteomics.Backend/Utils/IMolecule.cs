using InformedProteomics.Backend.Data.Sequence;

namespace InformedProteomics.Backend.Utils
{
    public interface IMolecule : IMatter
    {
        Composition GetComposition();
    }
}
