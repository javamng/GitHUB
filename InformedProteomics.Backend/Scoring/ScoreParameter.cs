using System;

namespace InformedProteomics.Backend.Scoring
{
    public static class ScoreParameter
    {
        public static void Read(string fileName)
        {
        }

        internal static float GetPrecursorIonCorrelationCoefficient(int c1, int c2)
        {
            return 0.8f;
        }

        internal static float GetProductIonCorrelationCoefficient(string ion1, string ion2)
        {
            return 0.8f;
        }

        internal static float GetProductIonCorrelationCoefficient(string ion)
        {
            return 0.8f;
        }
    }
}
