﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Database;
using NUnit.Framework;
using SuffixArray;

namespace InformedProteomics.Test
{
    [TestFixture]
    public class TestSuffixArray
    {

        [Test]
        public void TestReadingIndexedDatabase()
        {
            //const string dbFile = @"C:\cygwin\home\kims336\Research\SuffixArray\uniprot_sprot_bacterial_ALLEntries_fungal_decoy_2009-05-28.fasta";
            //const string dbFile = @"D:\Research\Data\CompRef\H_sapiens_M_musculus_Trypsin_NCBI_Build37_2011-12-02.fasta";
            const string dbFile = @"..\..\..\TestFiles\BSA.fasta";

            var database = new FastaDatabase(dbFile);
            database.Read();
            database.PrintSequence();
            Console.WriteLine("Done");
        }

        [Test]
        public void TestReadingBigFile()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            const string bigDbFile = @"C:\cygwin\home\kims336\Research\SuffixArray\uniprot2012_7_ArchaeaBacteriaFungiSprotTrembl_2012-07-11.fasta";
            var lastLine = File.ReadLines(bigDbFile).Last();
            sw.Stop();

            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            System.Console.WriteLine(@"{0:f4} sec", sec);
            System.Console.WriteLine(lastLine);
        }

        [Test]
        public void TestSuffixArrayGeneration()
        {
            const string dbFile = @"C:\cygwin\home\kims336\Data\SuffixArray\test.fasta";
            //const string dbFile = @"D:\Research\Data\CompRef\H_sapiens_M_musculus_Trypsin_NCBI_Build37_2011-12-02.fasta";
            //const string dbFile = @"..\..\..\TestFiles\BSA.fasta";

            var fileStream = new FileStream(dbFile, FileMode.Open, FileAccess.Read);
            var text = new byte[fileStream.Length];
            var suffixArray = new int[fileStream.Length];

            fileStream.Read(text, 0, text.Length);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            SAIS.sufsort(text, suffixArray, text.Length);
            sw.Stop();

            Console.WriteLine("Text: " + Encoding.UTF8.GetString(text));
            foreach (var index in suffixArray)
            {
                Console.WriteLine(Encoding.ASCII.GetString(text, index, text.Length-index));
            }

            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            System.Console.WriteLine(@"{0:f4} sec", sec);
        }

        [Test]
        public void TestDatabaseIndexing()
        {
            const string dbFile = @"C:\cygwin\home\kims336\Data\SuffixArray\BSA.fasta";
            //const string dbFile = @"C:\cygwin\home\kims336\Data\SuffixArray\uniprot_sprot.56.6_withContam.fasta";
            //const string dbFile = @"C:\cygwin\home\kims336\Data\SuffixArray\H_sapiens_Uniprot_SPROT_2013-05-01_withContam.fasta";

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var db = new FastaDatabase(dbFile);
            //db.Read();

            var indexedDb = new IndexedDatabase(db);
            //indexedDb.Read();
            //foreach (var seqAndLcp in indexedDb.SequenceLcpPairs(6,30))
            //{
            //    var seq = Encoding.ASCII.GetString(seqAndLcp.Item1);
            //    var lcp = seqAndLcp.Item2;
            //    //Console.WriteLine("{0}\t{1}", seq, lcp);
            //}

            foreach (var peptide in indexedDb.SequencesAsStrings(
                minLength: 6, 
                maxLength: 40, 
                numTolerableTermini: 1, 
                numMissedCleavages: 2, 
                enzyme: Enzyme.Trypsin)
                )
            {
                Console.WriteLine(peptide);
            }
            sw.Stop();
            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            System.Console.WriteLine(@"{0:f4} sec", sec);
        }

        [Test]
        public void TestGeneratingDecoyDatabase()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            const string dbFile = @"C:\cygwin\home\kims336\Data\SuffixArray\BSA.fasta";
            var db = new FastaDatabase(dbFile);
            var decoyDb = db.Decoy(Enzyme.Trypsin);
            sw.Stop();
            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            System.Console.WriteLine(@"{0:f4} sec", sec);
        }
    }
}