using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseDNSGeolocation.Classification
{
    public interface ITrainingDataSampler
    {
        IEnumerable<TrainingDataSample> Sample();
    }
}
