using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MultiDimensionalPeakFinding;
using MultiDimensionalPeakFinding.PeakDetection;
using UIMFLibrary;

namespace InformedProteomics.Backend.IMS
{
    [Serializable]
    public class XIC
    {
        private List<Feature> _featureList;

        public XIC(List<IntensityPoint> intensityPointList)
        {
            IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityPointList);
            Smoother.Smooth(ref pointList);
            FindFeatures(pointList);
        }

        public XIC(double[,] intensityBlock)
        {
            Smoother.Smooth(ref intensityBlock); 
            FindFeatures(intensityBlock);
        }

        public IEnumerable<Feature> GetFeatures()
        {
            return _featureList;
        }

        public IEnumerable<FeatureBlobStatistics> GetFeatures(int frameIndexMin, int frameIndexMax, int scanMin, int scanMax)
        {
            return null;
        }

        #region Private Methods

        public void FindFeatures(IEnumerable<Point> pointList)
        {
            IEnumerable<FeatureBlob> featureBlobList = FeatureDetection.DoWatershedAlgorithm(pointList);
            IEnumerable<FeatureBlobStatistics> featureBlobStatList = featureBlobList.Select(featureBlob => featureBlob.Statistics).ToList();

            _featureList = featureBlobStatList.Select(featureBlobStatistics => new Feature(featureBlobStatistics)).ToList();
        }
        
        public void FindFeatures(double[,] intensityBlock)
        {
            IEnumerable<Point> pointList = WaterShedMapUtil.BuildWatershedMap(intensityBlock);
            IEnumerable<FeatureBlob> featureBlobList = FeatureDetection.DoWatershedAlgorithm(pointList);
            IEnumerable<FeatureBlobStatistics> featureBlobStatList = featureBlobList.Select(featureBlob => featureBlob.Statistics).ToList();

            _featureList = featureBlobStatList.Select(featureBlobStatistics => new Feature(featureBlobStatistics)).ToList();
        }

        #endregion

        private static readonly SavitzkyGolaySmoother Smoother = new SavitzkyGolaySmoother(5, 2);
    }
}
