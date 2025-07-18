using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;
namespace GraphPlotter
{
    [XmlType("TireGripData")]
    public struct TireGripAngleData
    {

        [XmlIgnore]
        public float MarginOfError;
        [XmlIgnore]
        public float MaxMarginOfError;

        [XmlElement("SteeringAngle")]
        public float SeeringAngle;
        [XmlElement("MinimumSpeed")]
        public float MinSpeed;
        [XmlElement("MaximumSpeed")]
        public float MaxSpeed;

        [XmlElement("MinimumYawRate")]
        public float MinYawRate;
        [XmlElement("MaximumYawRate")]
        public float MaxYawRate;

        [XmlElement("SeperationPoint")]
        public float SeperationPoint;
        [XmlElement("SeperationValue")]
        public float SeperationValue;

        //for the second part of the curve
        [XmlIgnore]
        public float QuadraticLineTwoLocation;
        [XmlIgnore]
        private float _quadraticLineTwoValue;
        
        public float QuadraticLineTwoValue
        { get => _quadraticLineTwoValue;
            set
            {
                _quadraticLineTwoValue = Math.Clamp(value,-1,1);

                QuadraticLineTwoLocation = MaxYawRate * _quadraticLineTwoValue + SeperationValue * (1 - _quadraticLineTwoValue);

            }
        
        }



        //for the first part of the curve
        [XmlIgnore]
        public float QuadraticLineOneLocation;
        [XmlIgnore]
        private float _quadraticLineOneValue;

        public float QuadraticLineOneValue
        {
            get => _quadraticLineOneValue;
            set
            {
                _quadraticLineOneValue = Math.Clamp(value, -1, 1);

                QuadraticLineOneLocation =SeperationValue * _quadraticLineOneValue;

            }

        }

        


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float QuadraticBezier(float start, float mid, float end, float t)
        {
            float u = 1 - t;
            return u * u * start + 2 * u * t * mid + t * t * end;
        }

        public float Predict(float speed)
        {
            //max sure speed in within range of the array
            speed = Math.Clamp(speed, MinSpeed, MaxSpeed);

            //get pos withing range
            float per = 0;
           
            if (speed < SeperationPoint)
            {
             //   return 0;
                per = (speed - MinSpeed) / (SeperationPoint - MinSpeed);
                return QuadraticBezier(MinYawRate, QuadraticLineOneLocation, SeperationValue, per);
            }
            //return 0;
            per = (speed - SeperationPoint) / (MaxSpeed - SeperationPoint);

            return QuadraticBezier(SeperationValue, QuadraticLineTwoLocation, MaxYawRate, per);
        }
    }

    [XmlRoot("TireGripPrediction")]
    public class TireGripPrediction
    {
        [XmlElement("TireGripData")]
        public List<TireGripAngleData> TireData { get; set; } = new List<TireGripAngleData>();

        [XmlElement("Name")]
        public string Name { get; set; } = string.Empty;

        public TireGripPrediction() { }

        private float CalChangeInYawRate(GraphDataPoint dp, int a, int b)
        {
            if (dp.SteeringAngle < 0)
                return dp.YawRates[a] - dp.YawRates[b];
            else
                return dp.YawRates[b] - dp.YawRates[a];
        }
        [XmlIgnore]
        public static float ChangeInThreshold = .4f; //this is the change in yaw rate that we will use to determine the start of the drop off in yaw rate



        //create a new tire grip prediction from the data
        public TireGripAngleData CreateTireGripPrediction(GraphDataPoint dp, int qLocation)
        {

            if (dp.YawRates == null || dp.YawRates.Count == 0)
                return new TireGripAngleData() { SeeringAngle = int.MaxValue };

            TireGripAngleData tgd = new TireGripAngleData()
            {
                SeeringAngle = dp.SteeringAngle,
                MinSpeed = 0,
                MinYawRate = dp.YawRates[0],


            };

            float changeInYawRate = CalChangeInYawRate(dp, 1, 0);



            int index = -1;
            float high = float.MinValue;
            //get hights point of yaw rate
            for (int i = 0; i < dp.YawRates.Count - 1; i++)
            {
                if (Math.Abs(dp.YawRates[i]) > high)
                {
                    index = i;
                    high = Math.Abs(dp.YawRates[i]);
                }

            }

            tgd.SeperationPoint = qLocation;
            tgd.SeperationValue = dp.YawRates[qLocation];

            tgd.MaxSpeed = index; //set max speed //we subtract 1 becase ther 
            tgd.MaxYawRate = dp.YawRates[index]; //set max yawrate
            tgd.MaxMarginOfError = 0;
            //work out margin of error for the first part of the curve

            tgd.QuadraticLineOneValue = -1f;
            float bestMOE = float.PositiveInfinity;
            float bestExponate = 0;

            while (tgd.QuadraticLineOneValue < 1)
            {
                float moe = 0;

                for (int i = (int)tgd.MinSpeed; i < tgd.SeperationPoint; i++)
                {
                    if (dp.YawRates[i] == 0) //no yaw rate at this speed
                        continue;
                    moe += MathHelper.Distance(tgd.Predict(i), dp.YawRates[i]);
                }

                if (moe < bestMOE)
                {
                    bestMOE = moe;
                    bestExponate = tgd.QuadraticLineOneValue;
                }


                tgd.QuadraticLineOneValue += .001f;
            }
            tgd.MaxMarginOfError += bestMOE; //set max margin of error for the first part of the curve
            tgd.MarginOfError += bestMOE;
            tgd.QuadraticLineOneValue = bestExponate;
            //now lets work out margin of error for the second part of the curve

            tgd.QuadraticLineTwoValue = -1f;

            bestMOE = float.PositiveInfinity;
            bestExponate = 0;
            while (tgd.QuadraticLineTwoValue < 1)
            {
                float moe = 0;

                for (int i = (int)tgd.SeperationPoint; i < tgd.MaxSpeed; i++)
                {
                    if (dp.YawRates[i] == 0) //no yaw rate at this speed
                        continue;
                    moe += MathHelper.Distance(tgd.Predict(i), dp.YawRates[i]);
                }

                if (moe < bestMOE)
                {
                    bestMOE = moe;
                    bestExponate = tgd.QuadraticLineTwoValue;


                }

                tgd.QuadraticLineTwoValue += .001f;
            }
            tgd.MaxMarginOfError += bestMOE; //set max margin of error for the second part of the curve
            tgd.QuadraticLineTwoValue = bestExponate;
            tgd.MarginOfError += bestMOE;

            tgd.MarginOfError /= dp.YawRates.Count;
            //   tgd.MarginOfError /= dp.YawRates.Count;
            if (dp.SteeringAngle == 0) //no angle
            {
                tgd.MaxSpeed = 100;
                tgd.MaxYawRate = 0;
                tgd.MinYawRate = 0;
                tgd.SeperationPoint = 50;
                tgd.SeperationValue = 0;
                tgd.QuadraticLineOneValue = 0;
            }


            return tgd;

        }

        public static float ProcessingProgress { get; private set; }
        public TireGripPrediction(GraphData data, bool quick = true)
        {
            ProcessingProgress = 0;
            float pro = 0;
            foreach (GraphDataPoint dp in data.Points)
            {
                ProcessingProgress = pro / (float)data.Points.Count;
                pro++;
                TireGripAngleData tgd = new TireGripAngleData();

                float bestMOE = float.PositiveInfinity;
                int bestIndex = -1;
                int offset = (int)(dp.YawRates.Count * .2f);

                if (quick)
                    offset = dp.YawRates.Count / 2 - 1; //if quick we only want to use the middle of the data

                for (int i = offset; i < dp.YawRates.Count - offset; i++)
                {

                    tgd = CreateTireGripPrediction(dp, i);

                    if (tgd.MarginOfError < bestMOE)
                    {
                        bestMOE = tgd.MarginOfError;
                        bestIndex = i;
                    }

                    if (tgd.SeeringAngle == int.MaxValue) //no data for this angle
                        continue;
                }

                if (bestIndex == -1) //no data for this angle
                    continue;
                tgd = CreateTireGripPrediction(dp, bestIndex);
                TireData.Add(tgd);
            }
        }


        public float Predict(float speed, float steeringAngle)
        {
            int index = TireData.FindIndex(x => x.SeeringAngle >= steeringAngle);

            if (index == -1)
            {

                //=========NOTE this code need to be better by trying to extraperlate the angle of steering when outside the range of data
                //for now we will just use the last data reconrd we have that is nearest match

                //find nearest angle
                //has to be first record or last record

                float dist = MathHelper.Distance(TireData[0].SeeringAngle, steeringAngle);

                if (MathHelper.Distance(TireData[TireData.Count - 1].SeeringAngle, steeringAngle) < dist)
                {
                    return TireData[TireData.Count - 1].Predict(speed);
                }
                return TireData[0].Predict(speed);
            }


            if (index == 0 || index >= TireData.Count) //check if data we need is bondry
                return TireData[index].Predict(speed);

            //now lerp between the 2 valuves
            TireGripAngleData tgad = new TireGripAngleData();

            float differnce = Math.Clamp((MathHelper.Distance(steeringAngle, TireData[index].SeeringAngle) / 10f), 0, 1);

            tgad.SeperationValue = MathHelper.Lerp(TireData[index].SeperationValue, TireData[index - 1].SeperationValue, differnce);
            tgad.SeperationPoint = MathHelper.Lerp(TireData[index].SeperationPoint, TireData[index - 1].SeperationPoint, differnce);
            tgad.MinYawRate = MathHelper.Lerp(TireData[index].MinYawRate, TireData[index - 1].MinYawRate, differnce);
            tgad.MaxYawRate = MathHelper.Lerp(TireData[index].MaxYawRate, TireData[index - 1].MaxYawRate, differnce);
            tgad.MinSpeed = MathHelper.Lerp(TireData[index].MinSpeed, TireData[index - 1].MinSpeed, differnce);
            tgad.MaxSpeed = MathHelper.Lerp(TireData[index].MaxSpeed, TireData[index - 1].MaxSpeed, differnce);

            tgad.QuadraticLineOneLocation = MathHelper.Lerp(TireData[index].QuadraticLineOneLocation, TireData[index - 1].QuadraticLineOneLocation, differnce);
            tgad.QuadraticLineTwoLocation = MathHelper.Lerp(TireData[index].QuadraticLineTwoLocation, TireData[index - 1].QuadraticLineTwoLocation, differnce);

            tgad.QuadraticLineOneValue = MathHelper.Lerp(TireData[index].QuadraticLineOneValue, TireData[index - 1].QuadraticLineOneValue, differnce);
            tgad.QuadraticLineTwoValue = MathHelper.Lerp(TireData[index].QuadraticLineTwoValue, TireData[index - 1].QuadraticLineTwoValue, differnce);



            return tgad.Predict(speed);



        }


        public void SavedToTFP()
        {
            var serializer = new XmlSerializer(typeof(TireGripPrediction));
            using (var writer = new StreamWriter($"{Name}.xml"))
            {
                serializer.Serialize(writer, this);
            }

        }

        public void LoadFromTFP(string fileName)
        {
            var serializer = new XmlSerializer(typeof(TireGripPrediction));
            using (var reader = new StreamReader(fileName))
            {
                TireGripPrediction loadedData = (TireGripPrediction)serializer.Deserialize(reader);
                this.TireData = loadedData.TireData;

            }
        }
    }
}
