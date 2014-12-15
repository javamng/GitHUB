/*
              private const int MinXicWindowLength = 10;
              private double CalculateXicCorrelationOverTimeBetweenIsotopes(ChargeLcScanCluster cluster)
              {
                  double ret = 0;
                  var maxCol = cluster.MaxCol;
                  var minCol = cluster.MinCol;
                  var maxRow = cluster.MaxRow;
                  var minRow = cluster.MinRow;
                  var colLen = maxCol - minCol + 1;

                  if (colLen < MinXicWindowLength)
                  {
                      minCol = Math.Max(minCol - (int)((MinXicWindowLength - colLen) * 0.5), 0);

                      if (minCol == 0) maxCol = minCol + MinXicWindowLength - 1;
                      else
                      {
                          maxCol = Math.Min(maxCol + (int)((MinXicWindowLength - colLen) * 0.5), _nScans - 1);
                          if (maxCol == _nScans - 1) minCol = maxCol - MinXicWindowLength + 1;
                      }
                      colLen = maxCol - minCol + 1;
                  }

                  var xicProfile = new double[_topEnvelopes.Length][];
                  for (var i = 0; i < xicProfile.Length; i++) xicProfile[i] = new double[colLen];

                  for (var row = minRow; row <= maxRow; row++)
                  {
                      for (var col = minCol; col <= maxCol; col++)
                      {
                          if (!(_intensityMapFull[row][col] > 0)) continue;

                          for (var k = 0; k < _topEnvelopes.Length; k++)
                          {
                              xicProfile[k][col - minCol] += _featureMatrix[row][col][_topEnvelopes[k]];
                          }
                      }
                  }

                  // smoothing
                  //for (var k = 0; k < _topEnvelopes.Length; k++) xicProfile[k] = _smoother.Smooth(xicProfile[k]);
                  for (var i = 1; i < xicProfile.Length; i++)
                  {
                      for (var j = i + 1; j < xicProfile.Length; j++)
                      {
                          var coeff = FitScoreCalculator.GetPearsonCorrelation(xicProfile[i], xicProfile[j]);
                          if (coeff < 1.0 && coeff > ret) ret = coeff;
                      }
                  }
                  return ret;
              }

              private double CalculateXicCorrelationOverTimeBetweenCharges(ChargeLcScanCluster cluster)
              {
                  var maxCol = cluster.MaxCol;
                  var minCol = cluster.MinCol;
                  var maxRow = cluster.MaxRow;
                  var minRow = cluster.MinRow;

                  var colLen = maxCol - minCol + 1;

                  if (colLen < MinXicWindowLength)
                  {
                      minCol = Math.Max(minCol - (int)((MinXicWindowLength - colLen) * 0.5), 0);

                      if (minCol == 0) maxCol = minCol + MinXicWindowLength - 1;
                      else
                      {
                          maxCol = Math.Min(maxCol + (int)((MinXicWindowLength - colLen) * 0.5), _nScans - 1);
                          if (maxCol == _nScans - 1) minCol = maxCol - MinXicWindowLength + 1;
                      }
                      colLen = maxCol - minCol + 1;
                  }

                  if (Math.Abs(minRow - maxRow) < 5)
                  {
                      minRow = Math.Max(minRow - 3, 0);
                      maxRow = Math.Min(maxRow + 3, _nCharges - 1);
                  }

                  double ret = 0;
                  for (var row1 = minRow; row1 <= maxRow; row1++)
                  {
                      for (var row2 = row1 + 1; row2 <= maxRow; row2++)
                      {
                          var c = FitScoreCalculator.GetPearsonCorrelation(_intensityMapFull[row1], _intensityMapFull[row2], colLen, minCol, minCol);

                          if (c > ret) ret = c;
                      }
                  }
                  return ret;
        }
        private HashSet<int> GetMs2ScanNumbers(ChargeLcScanCluster cluster)
        {
            var result = new HashSet<int>();
            var temp = new List<KeyValuePair<double, int>>();
            var minCol = cluster.MinCol;
            var maxCol = cluster.MaxCol;

            if (cluster.MaxCol == cluster.MinCol)
            {
                minCol = Math.Max(0, cluster.MinCol - 1);
                maxCol = Math.Min(_nScans - 1, cluster.MaxCol + 1);
            }

            for (var row = cluster.MinRow; row <= cluster.MaxRow; row++)
            {
                var charge = _minCharge + row;

                var mostAbundantIsotopeMz = Ion.GetIsotopeMz(_queryMass, charge, _isotopeList.GetMostAbundantIsotope().Index);
                var ms2 = _run.GetFragmentationSpectraScanNums(mostAbundantIsotopeMz);

                var maxAbundance = 0d;
                var selectedMs2ScanNum1 = 0;

                var maxCorr = 0d;
                var selectedMs2ScanNum2 = 0;

                foreach (var ms2ScanNum in ms2)
                {
                    if (ms2ScanNum < _ms1ScanNums[minCol] || ms2ScanNum > _ms1ScanNums[maxCol])
                        continue;

                    var prevMs1ScanNum = _run.GetPrevScanNum(ms2ScanNum, 1);
                    var nextMs1ScanNum = _run.GetNextScanNum(ms2ScanNum, 1);
                    var col1 = _ms1ScanNumToIndexMap[prevMs1ScanNum - _ms1ScanNums.First()];
                    var col2 = _ms1ScanNumToIndexMap[nextMs1ScanNum - _ms1ScanNums.First()];

                    var abundance = _intensityMapFull[row][col1] + _intensityMapFull[row][col2];

                    temp.Add(new KeyValuePair<double, int>(abundance, ms2ScanNum));

                    if (abundance > maxAbundance)
                    {
                        maxAbundance = abundance;
                        selectedMs2ScanNum1 = ms2ScanNum;
                    }

                    var corr = Math.Max(_correlationMap[row][col1], _intensityMapFull[row][col2]);
                    if (corr > maxCorr)
                    {
                        maxCorr = corr;
                        selectedMs2ScanNum2 = ms2ScanNum;
                    }
                }
                if (selectedMs2ScanNum1 > 0) result.Add(selectedMs2ScanNum1);
                if (selectedMs2ScanNum2 > 0) result.Add(selectedMs2ScanNum2);
            }

            const int maxMs2 = 200;
            foreach (var t in temp.OrderByDescending(x => x.Key).TakeWhile(t => result.Count < maxMs2)) result.Add(t.Value);

            return result;
        }*/