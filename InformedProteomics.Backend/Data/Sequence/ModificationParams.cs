﻿using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Utils;

namespace InformedProteomics.Backend.Data.Sequence
{
    /// <summary>
    /// This class cataluges all possible combinations of modifications
    /// </summary>
    public class ModificationParams
    {
        private readonly Modification[] _modifications;
        private ModificationCombination[] _modificationCombinations;
        private readonly int _maxNumDynModsPerSequence;
        private Dictionary<int, int> _modCombMap;   
        private Dictionary<int, int> _modCombsToModMap;

        /// <summary>
        /// No modification
        /// </summary>
        public ModificationParams(): this(new Modification[0], 0)
        {
        }

        /// <summary>
        /// Storing all possible combinations of modifications up to MaxNumDynModsPerSequence
        /// </summary>
        /// <param name="modifications">array of modifications</param>
        /// <param name="maxNumDynModsPerSequence">number of maximum modifications</param>
        public ModificationParams(Modification[] modifications, int maxNumDynModsPerSequence)
        {
            _modifications = modifications;
            _maxNumDynModsPerSequence = maxNumDynModsPerSequence;
            CataloguePossibleModificationCombinations();
            GenerateModCombMap();
        }

        public int MaxNumDynModsPerSequence 
        {
            get { return _maxNumDynModsPerSequence; }
        }

        /// <summary>
        /// Gets the number of all possible modification instances
        /// </summary>
        /// <returns>the number of modification instances</returns>
        public int NumModificationCombinations
        {
            get { return _modificationCombinations.Length; }
        }

        /// <summary>
        /// Gets the modificatino combination with the specified modCombIndex
        /// </summary>
        /// <param name="modCombIndex">modification combination modCombIndex</param>
        /// <returns>modification combination</returns>
        public ModificationCombination GetModificationCombination(int modCombIndex)
        {
            return _modificationCombinations[modCombIndex];
        }

        public Modification GetModification(int modIndex)
        {
            return _modifications[modIndex];
        }

        public int GetModificationCombinationIndex(int prevModCombIndex, int modIndex)
        {
            int newModCombIndex;
            if (_modCombMap.TryGetValue(prevModCombIndex * _modifications.Length + modIndex, out newModCombIndex))
            {
                return newModCombIndex;
            }
            return -1;
        }

        public Modification GetModificationIndexBetween(int prevModCombIndex, int curModCombIndex)
        {
            int modIndex;
            if (_modCombsToModMap.TryGetValue(prevModCombIndex * _modificationCombinations.Length + curModCombIndex, out modIndex))
            {
                return _modifications[modIndex];
            }
            return null;
        }

        public IEnumerable<ModificationCombination> GetModificationCombinations()
        {
            return _modificationCombinations;
        }

        #region Private members

        private Dictionary<long, int> _hashValueToModCombIndex;
        private Dictionary<int, long> _indexToHashValue;

        #endregion

        #region Private methods

        private void CataloguePossibleModificationCombinations()
        {
            _indexToHashValue = new Dictionary<int, long>();
            _hashValueToModCombIndex = new Dictionary<long, int>();

            var combinations = SimpleMath.GetCombinationsWithRepetition(_modifications.Length+1, _maxNumDynModsPerSequence);
            _modificationCombinations = new ModificationCombination[combinations.Length];
            var index = -1;
            foreach (var combination in combinations)
            {
                var modList = (from i in combination where i > 0 select _modifications[i - 1]).ToList();
                var modComb = new ModificationCombination(modList);
                _modificationCombinations[++index] = modComb;
                var hashValue = ToHash(combination);
                _indexToHashValue[index] = hashValue;
                _hashValueToModCombIndex[hashValue] = index;
            }
        }

        private void GenerateModCombMap()
        {
            _modCombMap = new Dictionary<int, int>();
            _modCombsToModMap = new Dictionary<int, int>();
            for (var modCombIndex = 0; modCombIndex < _modificationCombinations.Length; modCombIndex++)
            {
                var hashValue = _indexToHashValue[modCombIndex];
                var modArray = ToModArray(hashValue);
                if (modArray.Length == 0)
                    continue;
                if (modArray[0] != 0) // this ModificationCombination has _maxNumDynModsPerSequence modifications
                    continue;
                for (var modIndex = 0; modIndex < _modifications.Length; modIndex++)
                {
                    var newArray = new int[modArray.Length];
                    Array.Copy(modArray, newArray, modArray.Length);
                    newArray[0] = modIndex + 1;
                    Array.Sort(newArray);
                    var newHashValue = ToHash(newArray);
                    var newModCombIndex = _hashValueToModCombIndex[newHashValue];
                    _modCombMap[modCombIndex * _modifications.Length + modIndex] = newModCombIndex;
                    _modCombsToModMap[modCombIndex * _modificationCombinations.Length + newModCombIndex] = modIndex;
                    //Console.WriteLine("{0},{1} -> {2}", _modificationCombinations[modCombIndex],
                    //    _modifications[modIndex], _modificationCombinations[newIndex]);
                }
            }
            _hashValueToModCombIndex = null;
            _indexToHashValue = null;
        }

        private int[] ToModArray(long hashValue)
        {
            int digit = _modifications.Length + 1;
            var arr = new int[_maxNumDynModsPerSequence];
            long val = hashValue;
            for (int i = 0; i < _maxNumDynModsPerSequence; i++)
            {
                arr[_maxNumDynModsPerSequence-1-i] = (int)(val % digit);
                val /= digit;
            }
            return arr;
        }

        private long ToHash(IEnumerable<int> combination)
        {
            int digit = _modifications.Length + 1;
            return combination.Aggregate<int, long>(0, (current, i) => digit * current + i);
        }

        #endregion

    }
}
