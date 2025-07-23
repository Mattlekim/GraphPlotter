using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;
namespace GraphPlotter
{
  
    
    public abstract class TireGripAngleData
    {

        [XmlIgnore]
        public float MarginOfError;
        [XmlIgnore]
        public float MaxMarginOfError;

        [XmlElement("SteeringAngle")]
        public float SeeringAngle;  

        public abstract float Predict(float speed);
    }

    public abstract class TireGripPrediction
    {

        //    [XmlElement("TireGripData")]
        // [XmlIgnore]
        [XmlArrayItem(typeof(TireGripAngleData))]
        [XmlArrayItem(typeof(TireGripAngleDataMatts))]
        public virtual List<TireGripAngleData> TireData { get; protected set; } = new List<TireGripAngleData>();

        public TireGripPrediction() { }

        //name of car
        public string Name { get; set; } = string.Empty;

        public static float ProcessingProgress { get; protected set; }

        public TireGripPrediction(GraphData data, bool quick = true)
        {
            ProcessingProgress = 1;
        }

     


        public abstract float Predict(float speed, float steeringAngle);


        public abstract void SavedToTFP();
       

        public abstract void LoadFromTFP(string fileName);
       
    }
}
