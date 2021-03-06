﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InformedProteomics.Backend.Utils
{
    public static class ArrayUtil
    {
        /// <summary>
        /// Create a string to display the array values.
        /// </summary>
        /// <param name="array">The array</param>
        /// <param name="delimiter">Delimiter character</param>
        /// <param name="format">Optional. A string to use to format each value. Must contain the colon, so something like ':0.000'</param>
        public static string ToString<T>(T[] array, string delimiter = "\t", string format = "")
        {
            var s = new StringBuilder();
            var formatString = "{0" + format + "}";

            for (var i = 0; i < array.Length; i++)
            {
                if (i < array.Length - 1)
                {
                    s.AppendFormat(formatString + delimiter, array[i]);
                }
                else
                {
                    s.AppendFormat(formatString, array[i]);
                }
            }

            return s.ToString();
        }

        public static string ToString<T>(T[][] array, string deliminator = "\t", string format = "")
        {
            var s = new StringBuilder();
            var formatString = "{0" + format + "}";

            for (var i = 0; i < array.Length; i++)
            {
                for (var j = 0; j < array[i].Length; j++)
                {
                    if (j < array[i].Length - 1)
                    {
                        s.AppendFormat(formatString + deliminator, array[i][j]);
                    }
                    else
                    {
                        s.AppendFormat(formatString, array[i][j]);
                    }                    
                }
                s.Append("\n");
            }

            return s.ToString();
        }

        public static int[] GetRankings(IEnumerable<double> values, double lowerBoundValue = 0.0d)
        {
            var temp = new List<KeyValuePair<double, int>>();
            var i = 0;
            foreach (var v in values)
            {
                if (v > lowerBoundValue) temp.Add(new KeyValuePair<double, int>(v, i));
                i++;
            }

            var ranking = 1;
            var rankingList = new int[i];
            foreach (var t in temp.OrderByDescending(x => x.Key))
            {
                rankingList[t.Value] = ranking++;
            }
            return rankingList;
        }

        public static int[] GetRankings(IEnumerable<double> values, out double median, double lowerBoundValue = 0.0d)
        {
            var temp = new List<KeyValuePair<double, int>>();
            var i = 0;
            foreach (var v in values)
            {
                if (v > lowerBoundValue) temp.Add(new KeyValuePair<double, int>(v, i));
                i++;
            }

            var ranking = 1;
            var rankingList = new int[i];
            var medianRanking = (int)Math.Max(Math.Round(0.5*i), 1);
            median = 0;
            foreach (var t in temp.OrderByDescending(x => x.Key))
            {
                if (ranking == medianRanking) median = t.Key;
                rankingList[t.Value] = ranking++;
            }
            return rankingList;
        }

        // Kadane's algorithm
        public static int MaxSumSubarray(IList<int> a, out int start, out int len)
        {
            start = 0;
            len = 1;
            var sum = a[0];

            var curStart = 0;
            var curLen = 1;
            var curSum = a[0];

            for (var i = 1; i < a.Count; i++)
            {

                if (a[i] >= curSum + a[i])
                {
                    curStart = i;
                    curLen = 1;
                    curSum = a[i];
                }
                else
                {
                    curLen++;
                    curSum += a[i];
                }

                if ((curSum <= sum) && (curSum != sum || curLen >= len) &&
                    (curSum != sum || curLen != len || curStart >= start)) continue;

                start = curStart;
                len = curLen;
                sum = curSum;
            }

            return sum;
        }
        // Kadane's algorithm
        public static double MaxSumSubarray(IList<double> a, out int start, out int len)
        {
            start = 0;
            len = 1;
            var sum = a[0];

            var curStart = 0;
            var curLen = 1;
            var curSum = a[0];

            for (var i = 1; i < a.Count; i++)
            {

                if (a[i] >= curSum + a[i])
                {
                    curStart = i;
                    curLen = 1;
                    curSum = a[i];
                }
                else
                {
                    curLen++;
                    curSum += a[i];
                }

                if ((curSum <= sum) && (Math.Abs(curSum - sum) > 1E-10 || curLen >= len) &&
                    (Math.Abs(curSum - sum) > 1E-10 || curLen != len || curStart >= start)) continue;

                start = curStart;
                len = curLen;
                sum = curSum;
            }

            return sum;
        }      

    }
}
