﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.Backend.Utils;
using NUnit.Framework;
using InformedProteomics.Backend.SequenceTag;

namespace InformedProteomics.Test
{
    [TestFixture]
    public class TestSequenceTagFinder
    {
        [Test]
        public void TestSequenceTag()
        {
            var methodName = MethodBase.GetCurrentMethod().Name;
            TestUtils.ShowStarting(methodName);

            //const string TestRawFile = @"D:\\Vlad_TopDown\\raw\\yufeng_column_test2.raw";
            //const string TestResultFile = @"D:\\Vlad_TopDown\\results\\yufeng_column_test2_IcTda.tsv";
            //const string TestRawFile = @"D:\MassSpecFiles\training\QC_Shew_Intact_26Sep14_Bane_C2Column3.pbf";
            //const string TestResultFile = @"D:\MassSpecFiles\results\ProMex\QC_Shew_Intact_26Sep14_Bane_C2Column3_IcTda.tsv";

            const string TestRawFile = @"D:\MassSpecFiles\Lewy\Lewy_intact_01.raw";
            const string TestResultFile = @"D:\MassSpecFiles\Lewy\Lewy_intact_01_IcTda.tsv";

            if (!File.Exists(TestRawFile))
            {
                Console.WriteLine(@"Warning: Skipping test {0} since file not found: {1}", methodName, TestRawFile);
                return;
            }

            if (!File.Exists(TestResultFile))
            {
                Console.WriteLine(@"Warning: Skipping test {0} since file not found: {1}", methodName, TestResultFile);
                return;
            }

            //const int MaxTags = 100000;
            var tsvParser = new TsvFileParser(TestResultFile);
            var headerList = tsvParser.GetHeaders();
            var tsvData = tsvParser.GetAllData();
            var ms2ScanNumbers = tsvData["Scan"];
        
            var run = PbfLcMsRun.GetLcMsRun(TestRawFile, 0, 0);
            var nSpec = 0;
            var nHitSpec = 0;

            //var targetScans = new int[] {6681};
            

            for (var i = 0; i < ms2ScanNumbers.Count; i++)
            //foreach(var scanNum in targetScans)
            {
                var scanNum = Int32.Parse(ms2ScanNumbers[i]);
                
                var spectrum = run.GetSpectrum(scanNum) as ProductSpectrum;

                int tsvIndex = ms2ScanNumbers.FindIndex(x => Int32.Parse(x) == scanNum);
                var qValue = Double.Parse(tsvData["QValue"].ElementAt(tsvIndex));
                //if (qValue > 0.01) continue;

                var seqStr = tsvData["Sequence"].ElementAt(tsvIndex).Trim();
                var modStr = tsvData["Modifications"].ElementAt(tsvIndex).Trim();
                
                var tagFinder = new SequenceTagFinder(spectrum, new Tolerance(5), 5);
                var nTags = 0;
                var nHit = 0;
                foreach (var tag in tagFinder.FindSequenceTags())
                {
                    nTags++;

                    double[] rmse;
                    foreach (var tagStr in tag.GetTagStrings(out rmse))
                    {
                        //Console.WriteLine(tagStr);
                        if (seqStr.Contains(tagStr) || seqStr.Contains(Reverse(tagStr))) nHit++;
                    }
                }
                nSpec++;
                if (nHit > 0) nHitSpec++;

                Console.WriteLine(@"[{0}]seqLen = {1}: {2}/{3}", scanNum, seqStr.Length, nHit, nTags);
            }
            //var existingTags = tagFinder.ExtractExistingSequneceTags(sequence);
            Console.Write("{0}/{1}", nHitSpec, nSpec);
        }

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        
        [Test]
        public void TestSequenceTagGlycoData()
        {
            var methodName = MethodBase.GetCurrentMethod().Name;
            TestUtils.ShowStarting(methodName);

            //const string rawFile = @"D:\MassSpecFiles\Glyco\User_sample_test_02252015.raw";
            //const string rawFile = @"D:\MassSpecFiles\CPTAC_Intact_CR33_5_29Jun15_Bane_15-02-01RZ.raw";
            const string rawFile = @"D:\MassSpecFiles\training\raw\QC_Shew_Intact_26Sep14_Bane_C2Column3.pbf";

            if (!File.Exists(rawFile))
            {
                Console.WriteLine(@"Warning: Skipping test {0} since file not found: {1}", methodName, rawFile);
                return;
            }

            //var run = PbfLcMsRun.GetLcMsRun(rawFile, rawFile.EndsWith(".mzML") ? MassSpecDataType.MzMLFile : MassSpecDataType.XCaliburRun, 1.4826, 1.4826);
            var run = PbfLcMsRun.GetLcMsRun(rawFile, 0, 0);
            var ms2ScanNums = run.GetScanNumbers(2);
            
            foreach(var scanNum in ms2ScanNums)
            {
                var spectrum = run.GetSpectrum(scanNum) as ProductSpectrum;

                Console.WriteLine(@"ScanNum = {0}; # of Peaks = {1}", scanNum, spectrum.Peaks.Length);
                Console.WriteLine(@"{0}", spectrum.ActivationMethod != ActivationMethod.ETD ? "ETD" : "HCD"); 

                var tagFinder = new SequenceTagFinder(spectrum, new Tolerance(5));
                var n = 0;
                foreach (var tag in tagFinder.FindSequenceTags())
                {
                    double[] rmse;
                    var seqTags = tag.GetTagStrings(out rmse);
                    n += seqTags.Length;
                }
                Console.WriteLine(n);
            }
            //var existingTags = tagFinder.ExtractExistingSequneceTags(sequence);
            //Console.Write(scanNum + "\t" + existingTags.Count);
        }    
    }
}
