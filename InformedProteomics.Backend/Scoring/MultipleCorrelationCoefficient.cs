using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Scoring
{
    public class MultipleCorrelationCoefficient
    {
        private DenseMatrix X { get; set; }
        private DenseMatrix Y { get; set; }
        private DenseMatrix Rxx { get; set; }
        private DenseMatrix Rxy { get; set; }

        public MultipleCorrelationCoefficient(DenseMatrix x, DenseMatrix y, DenseMatrix rxx, DenseMatrix rxy)// x should be appended to 1s
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


            var ones = new DenseMatrix(N, 1, 1);
           
            var estY = X.Multiply(Rxx.Inverse().Multiply(Rxy));
            float yvar = ones.Transpose().Multiply(Y).At(0,0);
            yvar = yvar * yvar / N;

            float ssr = estY.Transpose().Multiply(Y).At(0,0) - yvar;
            float sst = Y.Transpose().Multiply(Y).At(0, 0) - yvar;

            float r2 = ssr / sst;
            
            return 1 - ((1 - r2) * (N - 1) / (N - J - 1));
        }


        static public void Main(String[] args)
        {
            var x = new DenseMatrix(18, 2, new float[] { 2, 2, 2, 2, 2, 2, 4, 4, 4, 4, 4, 4, 8, 8, 8, 8, 8, 8, 2, 2, 4, 4, 8, 8, 2, 2, 4, 4, 8, 8, 2, 2, 4, 4, 8, 8});//{ 2, 2, 2, 2, 2, 4, 2, 4, 2, 8, 2, 8, 4, 2, 4, 2, 4, 4, 4, 4, 4, 8, 4, 8, 8, 2, 8, 2, 8, 4, 8, 4, 8, 8, 8, 8 });
            var y = new DenseMatrix(18, 1, new float[] { 35, 39, 21, 31, 6, 8, 40, 52, 34, 42, 18, 26, 61, 73, 58, 66, 46, 52 });
            int N = y.RowCount;
            int J = x.ColumnCount;
            var ones = new DenseMatrix(N, 1, 1);
            DenseMatrix newX = new DenseMatrix(N, J + 1, 0);
            ones.Append(x, newX);

            var rxx = (DenseMatrix)newX.TransposeThisAndMultiply(newX);
            var rxy = (DenseMatrix)newX.TransposeThisAndMultiply(y);
            float r2 = new MultipleCorrelationCoefficient(newX, y, rxx, rxy).Get();
            Console.Write(r2);
        }

    }
}
