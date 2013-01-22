using InformedProteomics.Backend.Data.Results;
using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Scoring
{
    class PrecursorIonScorer
    {
        public List<DatabaseSubTargetResult> PrecursorResults { get; private set; }
        public DatabaseSubTargetResult PrecursorResultRep { get; private set; }
        public List<int> ChargeStateList { get; private set; }
        public float Score { get; private set; }

        public PrecursorIonScorer(DatabaseMultipleSubTargetResult matchedResult)
        {
            PrecursorResults = matchedResult.SubTargetResultList;
            PrecursorResultRep = matchedResult.PrecursorResultRep;
            ChargeStateList = matchedResult.ChargeStateList;
            Score = GetScore();
        }


        private float GetScore()
        {
            if (PrecursorResults.Count == 1) return 0;

            DenseMatrix[] R = GetCorrelationMatrices();

            return new MultipleCorrelationCoefficient(GetX(), GetY(), R[0], R[1]).Get();
        }

        private DenseMatrix GetX()
        {
            int J = PrecursorResults.Count;
            int N = PrecursorResultRep.XYData.Yvalues.Length;

            DenseMatrix X = new DenseMatrix(N, J-1, 0);

            int n = 0;
            for (int i = 0; i < J;i++)
            {
                DatabaseSubTargetResult r = PrecursorResults.ElementAt<DatabaseSubTargetResult>(i);
                if (r.Equals(PrecursorResultRep)) continue;

                float[] s = Standardize(r.XYData.Yvalues);

                for (int j = 0; j < N; j++)
                {
                    X.At(j,n,s[j]);
                }
                n++;
            }

            return X;
        }

        private DenseMatrix GetY()
        {
            int N = PrecursorResultRep.XYData.Yvalues.Length;

            DenseMatrix Y = new DenseMatrix(N, 1, 0);
            float[] s = Standardize(PrecursorResultRep.XYData.Yvalues);

            for (int j = 0; j < N; j++)
            {
                Y.At(j, 0, s[j]);
            }
             
            return Y;
        }

        private DenseMatrix[] GetCorrelationMatrices()
        {
            int J = PrecursorResults.Count;
            DenseMatrix Rxx = new DenseMatrix(J - 1, J - 1, 0);
            DenseMatrix Rxy = new DenseMatrix(J - 1, 1, 0);

            int[] charges = new int[J-1];
            int charge = 0;
            int n = 0;
            for (int i = 0; i < J; i++)
            {
                DatabaseSubTargetResult r = PrecursorResults.ElementAt<DatabaseSubTargetResult>(i);
                if (r.Equals(PrecursorResultRep))
                {
                    charge = ChargeStateList.ElementAt<int>(i);
                    continue;
                }
                charges[n] = ChargeStateList.ElementAt<int>(i);
                n++;
            }

            for (int i = 0; i < J - 1;i++)
            {
                for (int j = 0; j < J - 1; j++)
                {
                    Rxx.At(i,j,ScoreParameter.GetPrecursorIonCorrelationCoefficient(charges[i], charges[j]));
                }
                Rxy.At(i, 0, ScoreParameter.GetPrecursorIonCorrelationCoefficient(charges[i], charge));
            }

            return new DenseMatrix[2] { Rxx, Rxy }; 
        }

      
        private float[] Standardize(double[] x) // return standardized x (i.e., mean = 0, var = 1) 
        {
            float m = GetSampleMean(x);
            float v = GetSampleVariance(x, m);
            float[] sx = new float[x.Length];

            for (int i = 0; i < x.Length; i++)
            {
                sx[i] = ((float)x[i] - m) / (float)Math.Sqrt(v);
            }
            return sx;
        }


        private float GetSampleMean(double[] x)
        {
            float m = 0;
            foreach (float v in x)
            {
                m += v;
            }

            return m/x.Length;
        }

        private float GetSampleVariance(double[] x, float m)
        {       
            float var = 0;

            foreach (float v in x)
            {
                var += (v-m)*(v-m);
            }
            return var/(x.Length-1);
        }


       
    }
}
