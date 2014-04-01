﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms.VisualStyles;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Sequence;
using SuffixArray;

namespace InformedProteomics.Backend.Database
{
    public class IndexedDatabase
    {
        public static readonly string PermutedLongestCommonPrefixFileExtension = ".icplcp";

        private readonly string _pLcpFilePath;

        private readonly FastaDatabase _fastaDatabase;
        private byte[] _pLcp;        

        public IndexedDatabase(FastaDatabase fastaDatabase)
        {
            _fastaDatabase = fastaDatabase;
            var databaseFilePath = _fastaDatabase.GetFastaFilePath();
            var databaseFilePathNoExt = Path.GetDirectoryName(databaseFilePath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(databaseFilePath);

            _pLcpFilePath = databaseFilePathNoExt + PermutedLongestCommonPrefixFileExtension;
            var lastWriteTimeHash = _fastaDatabase.GetLastWriteTimeHash();

            if (!File.Exists(_pLcpFilePath) || !FastaDatabase.CheckHashCodeBinaryFile(_pLcpFilePath, lastWriteTimeHash))
            {
                Console.Write("Generating " + _pLcpFilePath + "...");
                CreatePermutedLongestCommonPrefixFile();
                Console.WriteLine("\tDone.");
            }
        }

        public void Read()
        {
            using (var fileStream = new FileStream(_pLcpFilePath, FileMode.Open, FileAccess.Read))
            {
                _pLcp = new byte[fileStream.Length - sizeof(int)];
                fileStream.Read(_pLcp, 0, _pLcp.Length);
            }
        }

        public IEnumerable<byte> PLcps()
        {
            if (_pLcp == null)
            {
                const int bufferSize = 1 << 16;
                var buffer = new byte[bufferSize];
                var count = bufferSize;
                var numBytesRead = 0;

                using (var fileStream = new FileStream(_pLcpFilePath, FileMode.Open, FileAccess.Read))
                {
                    var numBytesToRead = fileStream.Length - sizeof(int);
                    while (count > 0)
                    {
                        count = fileStream.Read(buffer, 0, bufferSize);
                        for (var i = 0; i < count && numBytesRead++ < numBytesToRead; i++)
                        {
                            yield return buffer[i];
                        }
                    }
                }                
            }
            else
            {
                foreach (var lcp in _pLcp) yield return lcp;
            }
        }

        public IEnumerable<SequenceAndOffset> SequencesWithOffsetNoCleavage()
        {
            List<byte> buf = null;
            var curOffset = 0L;

            var offset = -1L;
            foreach (var residue in _fastaDatabase.Characters())
            {
                ++offset;
                if (residue == FastaDatabase.Delimiter)
                {
                    if (buf != null && buf.Count > 0) yield return new SequenceAndOffset(buf.ToArray(), curOffset);
                    buf = new List<byte>();
                    curOffset = offset;
                }
                else
                {
                    if (buf != null) buf.Add(residue);
                }
            }
        }

        public IEnumerable<AnnotationAndOffset> FullSequenceAnnotationsAndOffsets(int minLength, int maxLength, int numTolerableTermini,
                                                      int numMissedCleavages, Enzyme enzyme)
        {
            return AnnotationsAndOffsets(minLength, maxLength, numTolerableTermini, numMissedCleavages, enzyme.Residues,
                               enzyme.IsNTerm);
        }



        public IEnumerable<AnnotationAndOffset> AnnotationsAndOffsets(int minLength, int maxLength, int numTolerableTermini,
                                                      int numMissedCleavages, char[] enzymaticResidues,
                                                      bool isNTermEnzyme)
        {
            var isCleavable = new bool[128];
            if (enzymaticResidues != null)
            {
                foreach (var residue in enzymaticResidues)
                {
                    isCleavable[residue] = true;
                    if (isCleavable.Length > FastaDatabase.Delimiter)
                        isCleavable[FastaDatabase.Delimiter] = true;
                }
            }

            var isStandardAminoAcid = new bool[256];
            foreach (var residue in AminoAcid.StandardAminoAcidCharacters)
            {
                isStandardAminoAcid[residue] = true;
            }

            var encoding = Encoding.ASCII;
            // pre, peptide sequence, next
            foreach (var seqAndLcp in SequencesWithLcpAndOffset(minLength, maxLength+2))
            {
                var seqArr = seqAndLcp.Sequence;
                var lcp = seqAndLcp.Lcp;
                var offset = seqAndLcp.Offset;

                if (enzymaticResidues != null)  
                {
                    var ntt = 0;
                    var nmc = 0;
                    if (!isNTermEnzyme) // C-term enzyme 
                    {
                        if (isCleavable[seqArr[0]]) ++ntt;
                        if (ntt < numTolerableTermini-1) continue;

                        for (var i = 1; i < seqArr.Length-1; i++)
                        {
                            var code = seqArr[i];
                            if (!isStandardAminoAcid[code]) break;
                            if (isCleavable[code]) ++nmc;

                            if (i >= minLength && i >= lcp)
                            {
                                if (ntt + (isCleavable[code] || seqArr[i+1] == FastaDatabase.Delimiter ? 1 : 0) >= numTolerableTermini)
                                {
                                    yield return new AnnotationAndOffset(offset, string.Format("{0}.{1}.{2}", (char)seqArr[0], encoding.GetString(seqArr, 1, i), (char)seqArr[i + 1]));
                                }
                            }
                            if (nmc > numMissedCleavages) break;
                        }
                    }
                    else // N-term enzyme
                    {
                        if (seqArr[0] == FastaDatabase.Delimiter || isCleavable[seqArr[1]]) ++ntt;
                        if (ntt < numTolerableTermini - 1) continue;

                        for (var i = 2; i < seqArr.Length - 1; i++)
                        {
                            var code = seqArr[i];
                            if (!isStandardAminoAcid[code]) break;
                            if (isCleavable[code]) ++nmc;
                            if (nmc > numMissedCleavages) break;

                            if (i >= minLength && i >= lcp)
                            {
                                if (ntt + (isCleavable[seqArr[i + 1]] ? 1 : 0) >= numTolerableTermini)
                                {
                                    yield return new AnnotationAndOffset(offset, string.Format("{0}.{1}.{2}", (char)seqArr[0], encoding.GetString(seqArr, 1, i), (char)seqArr[i + 1]));
                                }
                            }
                        }
                    }
                }
                else    // No enzyme
                {
                    for (var i = 1; i < seqArr.Length-1; i++)
                    {
                        var code = seqArr[i];

                        // TODO: code = 185
                        //if (code > isStandardAminoAcid.Length)
                        //{
                        //    Console.WriteLine("Debug : " + code);
                        //}
                        //if (encoding.GetString(seqArr, 1, i).Equals("QQTIAAN_VD"))
                        //{
                        //        Console.WriteLine("Debug : " + code);
                        //}
                        if (!isStandardAminoAcid[code]) break;
                        if (i >= minLength && i >= lcp)
                        {
                            yield return new AnnotationAndOffset(offset, string.Format("{0}.{1}.{2}", (char)seqArr[0], encoding.GetString(seqArr, 1, i), (char)seqArr[i + 1]));
                        }
                    }
                }
            }
        }

        public IEnumerable<AnnotationAndOffset> AnnotationsAndOffsetsNoEnzyme(int minLength, int maxLength)
        {
            return AnnotationsAndOffsets(minLength, maxLength, 0, 0, null, false);
        }

        public IEnumerable<AnnotationAndOffset> IntactSequenceAnnotationsAndOffsets(int minLength, int maxLength)
        {
            return IntactSequenceAnnotationsAndOffsets(minLength, maxLength, 0);
        }

        public int GetLongestSequenceLength()
        {
            return SequencesWithOffsetNoCleavage().Select(seqWithOffset => seqWithOffset.Sequence.Length).Max();
        }

        private IEnumerable<SequenceLcpAndOffset> SequencesWithLcpAndOffset(int minLength, int maxLength)
        {
            var curSequence = new LinkedList<byte>();
            var lcpList = new LinkedList<byte>();
            var lcpEnum = PLcps().GetEnumerator();

            var offset = -1L;
            foreach (var residue in _fastaDatabase.Characters())
            {
                lcpEnum.MoveNext();
                curSequence.AddLast(residue);
                lcpList.AddLast(lcpEnum.Current);

                if (curSequence.Count < maxLength) continue;

                var seqArr = new byte[curSequence.Count];
                curSequence.CopyTo(seqArr, 0);

                yield return new SequenceLcpAndOffset(seqArr, lcpList.First.Value, ++offset);
                curSequence.RemoveFirst();
                lcpList.RemoveFirst();
            }

            while (curSequence.Count >= minLength)
            {
                var seqArr = new byte[curSequence.Count];
                curSequence.CopyTo(seqArr, 0);
                yield return new SequenceLcpAndOffset(seqArr, lcpList.First.Value, ++offset);
                curSequence.RemoveFirst();
                lcpList.RemoveFirst();
            }
        }

        public IEnumerable<AnnotationAndOffset> IntactSequenceAnnotationsAndOffsets(int minLength, int maxLength, int numCTermCleavages)
        {
            var encoding = Encoding.ASCII;

            foreach (var seqWithOffset in SequencesWithOffsetNoCleavage())
            {
                var seqArr = seqWithOffset.Sequence;
                var offset = seqWithOffset.Offset;
                for (var i = 0; i <= numCTermCleavages; i++)
                {
                    var length = seqArr.Length - i;
                    if (length >= minLength && length <= maxLength)
                    {
                        yield return new AnnotationAndOffset(
                            offset,
                            string.Format("{0}.{1}.{2}",
                            "_",
                            encoding.GetString(seqArr, 0, length),
                            (i == 0 ? "_" : encoding.GetString(seqArr, length, 1)))
                            );
                    }
                }
            }
        }

        private void CreatePermutedLongestCommonPrefixFile()
        {
            if (File.Exists(_pLcpFilePath))
                File.Delete(_pLcpFilePath);

            var sequence = _fastaDatabase.GetSequence();
            //Console.WriteLine("Annotation: {0}", System.Text.Encoding.ASCII.GetString(sequence));
            var suffixArray = new int[sequence.Length-1];
            SAIS.sufsort(sequence, suffixArray, sequence.Length-1);

            var prevIndex = sequence.Length - 1;

            var pLcp = new byte[suffixArray.Length];

            foreach (var index in suffixArray)
            {
                var lcp = GetLcp(sequence, prevIndex, index);
                pLcp[index] = lcp;
                //Console.WriteLine("{0}\t{1}", System.Text.Encoding.ASCII.GetString(sequence, offset, sequence.Length - offset - 1), lcp);
                prevIndex = index;
            }

            using (var fs = new FileStream(_pLcpFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                foreach (var lcp in pLcp)
                {
                    //Console.WriteLine("LCP: {0}", lcp);
                    fs.WriteByte(lcp);
                }
                fs.Write(BitConverter.GetBytes(FastaDatabase.FileFormatId), 0, sizeof(int));
                fs.Write(BitConverter.GetBytes(_fastaDatabase.GetLastWriteTimeHash()), 0, sizeof(int));
            }
        }

        private static byte GetLcp(IList<byte> sequence, int index1, int index2)
        {
            var lcp = (byte)0;

            while (sequence[index1+lcp] == sequence[index2+lcp])
            {
                ++lcp;
                if (lcp == byte.MaxValue) break;
            }

            return lcp;
        }

        //public long NumSequences(int minLength, int maxLength, int numTolerableTermini,
        //                         int numMissedCleavages, char[] enzymaticResidues,
        //                         bool isNTermEnzyme)
        //{
        //    var numSequences = 0L;

        //    {
        //        var isCleavable = new bool[128];
        //        if (enzymaticResidues != null)
        //        {
        //            foreach (var residue in enzymaticResidues)
        //            {
        //                isCleavable[residue] = true;
        //                isCleavable[FastaDatabase.Delimiter] = true;
        //            }
        //        }

        //        const string standardAAStr = "ACDEFGHIKLMNPQRSTVWY";
        //        var isStandardAminoAcid = new bool[128];
        //        foreach (var residue in standardAAStr)
        //        {
        //            isStandardAminoAcid[residue] = true;
        //        }

        //        // pre, peptide sequence, next
        //        foreach (var seqAndLcp in SequencesWithLcpAndOffset(minLength, maxLength + 2))
        //        {
        //            var seqArr = seqAndLcp.Annotation;
        //            var lcp = seqAndLcp.Lcp;
        //            var ntt = 0;
        //            var nmc = 0;

        //            if (enzymaticResidues != null)
        //            {
        //                if (!isNTermEnzyme) // C-term enzyme 
        //                {
        //                    if (isCleavable[seqArr[0]]) ++ntt;
        //                    if (ntt < numTolerableTermini - 1) continue;

        //                    for (var i = 1; i < seqArr.Length - 1; i++)
        //                    {
        //                        var code = seqArr[i];
        //                        if (!isStandardAminoAcid[code]) break;
        //                        if (isCleavable[code]) ++nmc;

        //                        if (i >= minLength && i >= lcp)
        //                        {
        //                            if (ntt + (isCleavable[code] || i < seqArr.Length - 1 && seqArr[i + 1] == FastaDatabase.Delimiter ? 1 : 0) >= numTolerableTermini)
        //                            {
        //                                numSequences++;
        //                            }
        //                        }
        //                        if (nmc > numMissedCleavages) break;
        //                    }
        //                }
        //                else // N-term enzyme
        //                {

        //                }
        //            }
        //            else // no enzyme
        //            {
        //            }
        //        }
        //    }
        //    return numSequences;
        //}

        //public long NumSequences(int minLength, int maxLength, int numTolerableTermini,
        //                                              int numMissedCleavages, Enzyme enzyme)
        //{
        //    return NumSequences(minLength, maxLength, numTolerableTermini, numMissedCleavages, enzyme.Residues,
        //                       enzyme.IsNTerm);
        //}


    }

    internal class SequenceLcpAndOffset
    {
        public SequenceLcpAndOffset(byte[] sequence, short lcp, long offset)
        {
            Sequence = sequence;
            Lcp = lcp;
            Offset = offset;
        }

        public byte[] Sequence { set; get; }
        public short Lcp { set; get; }
        public long Offset { set; get; }
    }

    public class SequenceAndOffset
    {
        public SequenceAndOffset(byte[] sequence, long offset)
        {
            Sequence = sequence;
            Offset = offset;
        }

        public byte[] Sequence { set; get; }
        public long Offset { set; get; }
    }

}
