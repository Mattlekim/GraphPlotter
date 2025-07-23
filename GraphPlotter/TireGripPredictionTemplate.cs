using System;
using System.Collections.Generic;
using System.Text;

namespace GraphPlotter
{
    public class TireDataTemplate: TireGripAngleData
    {
        public override float Predict(float speed)
        {
            throw new NotImplementedException();
        }
    }

    public class TireGripPredictionTemplate : TireGripPrediction
    {
        public TireGripPredictionTemplate() : base(null, true)
        {
            // This constructor is used for serialization purposes
        }

        public TireGripPredictionTemplate(GraphData data, bool quick = true) : base(data, quick)
        {
            // This constructor is used for creating a new instance with data
        }

       

        public override float Predict(float speed, float steeringAngle)
        {
            throw new NotImplementedException();
        }

        public override void SavedToTFP()
        {
          //  throw new NotImplementedException();
        }

        public override void LoadFromTFP(string fileName)
        {
           // throw new NotImplementedException();
        }
    }
}
