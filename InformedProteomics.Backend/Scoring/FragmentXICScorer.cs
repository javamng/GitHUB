using MathNet.Numerics.LinearAlgebra.Single;
using System.Collections.Generic;

namespace InformedProteomics.Backend.Scoring
{
    class FragmentXICScorer
    {
        public Dictionary<string, double[]> IonXICsPerFragment { get; private set; }
        public double[] PrecursorXIC { get; private set; }
        public List<string> UsedIonTypes { get; private set; }

        public float Score { get; private set; }

        public FragmentXICScorer(Dictionary<string, double[]> ionXICsPerFragment, List<string> usedIonTypes, double[] precursorXIC)
        {
            IonXICsPerFragment = ionXICsPerFragment;
            UsedIonTypes = usedIonTypes;
            PrecursorXIC = precursorXIC;
            Score = GetScore();
        }

        private float GetScore()
        {
            var r = GetCorrelationMatrices();

            return new MultipleCorrelationCoefficient(GetX(), GetY(), r[0], r[1]).Get();
        }

        private DenseMatrix GetX()
        {
            var x = new DenseMatrix(PrecursorXIC.Length, UsedIonTypes.Count);
            for (var i = 0; i < UsedIonTypes.Count; i++)
            {
                var vs = IonXICsPerFragment[UsedIonTypes[i]];
                for (var j=0;j<vs.Length;j++)
                {
                    x.At(j, i, (float)vs[j]);
                }
            }
            return x;
        }

        private DenseMatrix GetY()
        {
            var y = new DenseMatrix(PrecursorXIC.Length, 1);
            for (var i = 0; i < PrecursorXIC.Length; i++)
            {
                y.At(i, 0, (float)PrecursorXIC[i]);
            }
                return y;
        }

        private DenseMatrix[] GetCorrelationMatrices()
        {
            var j = UsedIonTypes.Count;
            var rxx = new DenseMatrix(j, j, 0);
            var rxy = new DenseMatrix(j, 1, 0);
 
            for (var i = 0; i < j; i++)
            {
                for (var k = 0; k < j; k++)
                {
                    rxx.At(i, k, ScoreParameter.GetProductIonCorrelationCoefficient(UsedIonTypes[i], UsedIonTypes[k]));
                }
                rxy.At(i, 0, ScoreParameter.GetProductIonCorrelationCoefficient(UsedIonTypes[i]));
            }

            return new[] { rxx, rxy };
        }

    }
}
