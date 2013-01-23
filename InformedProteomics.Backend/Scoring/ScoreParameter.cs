using System;

namespace InformedProteomics.Backend.Scoring
{
    public static class ScoreParameter
    {
        public static void Read(string fileName)
        {
        }

        internal static float GetPrecursorIonLikelihoodRatioScore(float rawScore) // spectrum para
        {
            
        }

        internal static float GetProductIonLikelihoodRatioScore(float rawScore) // spectrum para
        {

        }

        internal static float GetPrecursorIonCorrelationCoefficient(int c1, int c2) // spectrum para
        {
            return 0.8f;
        }

        internal static float GetProductIonCorrelationCoefficient(string ion1, string ion2) // spectrum para, peak para (of ion1)
        {
            return 0.8f;
        }

        internal static float GetProductIonCorrelationCoefficient(string ion) // spectrum para, peak para
        {
            return 0.8f;
        }
    }
}
