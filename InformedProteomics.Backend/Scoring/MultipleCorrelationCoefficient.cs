using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Scoring
{
    class MultipleCorrelationCoefficient
    {
        private Matrix<float> X { get; set; }
        private Matrix<float> Y { get; set; }
        private Matrix<float> Rxx { get; set; }
        private Matrix<float> Rxy { get; set; }

        public MultipleCorrelationCoefficient(Matrix<float> x, Matrix<float> y, Matrix<float> rxx, Matrix<float> rxy)
        {
            X = x;
            Y = y;
            Rxx = rxx;
            Rxy = rxy;
        }

        public float Get()
        {
            int N = Y.RowCount;
            int J = X.ColumnCount;

            Matrix<float> ones = new DenseMatrix(N, 1, 1);
            Matrix<float> newX = null;
            ones.Append(X, newX);

            Matrix<float> estY = X.Multiply(Rxx.Inverse()).Multiply(Rxy);
            float yvar = ones.Transpose().Multiply(Y).At(0,0);
            yvar = yvar * yvar / N;

            float ssr = estY.Transpose().Multiply(Y).At(0,0) - yvar;
            float sst = Y.Transpose().Multiply(Y).At(0, 0) - yvar;

            float r2 = ssr / sst;

            return 1 - ((1 - r2) * (N - 1) / (N - J - 1));
        }

       


    }
}
