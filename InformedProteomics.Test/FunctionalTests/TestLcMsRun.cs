﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend.Runs;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using NUnit.Framework;

namespace InformedProteomics.Test.FunctionalTests
{
    [TestFixture]
    public class TestLcMsRun
    {
        public const string TestRawFilePath = @"\\protoapps\UserData\Sangtae\TestData\QC_Shew_12_02_2_1Aug12_Cougar_12-06-11.raw";
        public const string TestTopDownRawFilePath = @"\\protoapps\UserData\Sangtae\TestData\E_coli_iscU_60_mock.raw";

        public void TestReadingScanNums()
        {
            var reader = new XCaliburReader(TestRawFilePath);
            var run = new LcMsRun(reader);

            var msLevel = new Dictionary<int, int>();

            for (var scanNum = run.MinLcScan; scanNum <= run.MaxLcScan; scanNum++)
            {
                msLevel[scanNum] = run.GetMsLevel(scanNum);
            }

            for (var scanNum = run.MinLcScan; scanNum <= run.MaxLcScan; scanNum++)
            {
                var spec = run.GetSpectrum(scanNum);
                Assert.True(spec.MsLevel == msLevel[scanNum]);

                if (spec.MsLevel == 2)
                {
                    var precursorScanNum = 0;
                    for (var prevScan = scanNum - 1; prevScan >= run.MinLcScan; prevScan--)
                    {
                        if (run.GetMsLevel(prevScan) == 1)
                        {
                            precursorScanNum = prevScan;
                            break;
                        }
                    }
                    Assert.True(run.GetPrecursorScanNum(scanNum) == precursorScanNum);

                    var nextScanNum = run.MaxLcScan+1;
                    for (var nextScan = scanNum + 1; nextScan <= run.MaxLcScan; nextScan++)
                    {
                        if (run.GetMsLevel(nextScan) == 1)
                        {
                            nextScanNum = nextScan;
                            break;
                        }
                    }
                    if (run.GetNextScanNum(scanNum) != nextScanNum)
                    {
                        Console.WriteLine("{0}\t{1}\t{2}", scanNum, run.GetNextScanNum(scanNum), nextScanNum);
                    }
                    Assert.True(run.GetNextScanNum(scanNum) == nextScanNum);
                }
            }
            Assert.True(run.GetNextScanNum(31151) == 31153);
            Console.WriteLine(run.GetNextScanNum(89));
        }
    }
}
