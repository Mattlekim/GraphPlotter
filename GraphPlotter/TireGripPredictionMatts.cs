using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;
namespace GraphPlotter
{
    
    public class TireGripAngleDataMatts: TireGripAngleData
    {


       
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

        public override float Predict(float speed)
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
    public class TireGripPredictionMatts: TireGripPrediction
    {
     
       // public override List<TireGripAngleDataMatts> TireData = new List<TireGripAngleDataMatts>();

        public TireGripPredictionMatts() { }


        //create a new tire grip prediction from the data
        public TireGripAngleDataMatts CreateTireGripPrediction(GraphDataPoint dp, int qLocation)
        {
            
            if (dp.YawRates == null || dp.YawRates.Count == 0)
                return new TireGripAngleDataMatts() { SeeringAngle = int.MaxValue };

            TireGripAngleDataMatts tgd = new TireGripAngleDataMatts()
            {
                SeeringAngle = dp.SteeringAngle,
                MinSpeed = 0,
                MinYawRate = dp.YawRates[0],


            };

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

        public TireGripPredictionMatts(GraphData data, bool quick = true)
        {
            ProcessingProgress = 0;
            float pro = 0;
            foreach (GraphDataPoint dp in data.Points)
            {
                ProcessingProgress = pro / (float)data.Points.Count;
                pro++;
                TireGripAngleDataMatts tgd = new TireGripAngleDataMatts();

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


        public override float Predict(float speed, float steeringAngle)
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
            TireGripAngleDataMatts tgad = new TireGripAngleDataMatts();

            float differnce = Math.Clamp((MathHelper.Distance(steeringAngle, TireData[index].SeeringAngle) / 10f), 0, 1);

            TireGripAngleDataMatts tg1 = TireData[index] as TireGripAngleDataMatts;
            TireGripAngleDataMatts tg2 = TireData[index - 1] as TireGripAngleDataMatts;

            tgad.SeperationValue = MathHelper.Lerp(tg1.SeperationValue, tg2.SeperationValue, differnce);
            tgad.SeperationPoint = MathHelper.Lerp(tg1.SeperationPoint, tg2.SeperationPoint, differnce);
            tgad.MinYawRate = MathHelper.Lerp(tg1.MinYawRate, tg2.MinYawRate, differnce);
            tgad.MaxYawRate = MathHelper.Lerp(tg1.MaxYawRate, tg2.MaxYawRate, differnce);
            tgad.MinSpeed = MathHelper.Lerp(tg1.MinSpeed, tg2.MinSpeed, differnce);
            tgad.MaxSpeed = MathHelper.Lerp(tg1.MaxSpeed, tg2.MaxSpeed, differnce);

            tgad.QuadraticLineOneLocation = MathHelper.Lerp(tg1.QuadraticLineOneLocation, tg2.QuadraticLineOneLocation, differnce);
            tgad.QuadraticLineTwoLocation = MathHelper.Lerp(tg1.QuadraticLineTwoLocation, tg2.QuadraticLineTwoLocation, differnce);

            tgad.QuadraticLineOneValue = MathHelper.Lerp(tg1.QuadraticLineOneValue, tg2.QuadraticLineOneValue, differnce);
            tgad.QuadraticLineTwoValue = MathHelper.Lerp(tg1.QuadraticLineTwoValue, tg2.QuadraticLineTwoValue, differnce);

            return tgad.Predict(speed);



        }


        public override void SavedToTFP()
        {
            var serializer = new XmlSerializer(typeof(TireGripPredictionMatts));
           
            using (var writer = new StreamWriter($"{Name}.xml"))
            {
                serializer.Serialize(writer, this);
            }

        }

      

        public override void LoadFromTFP(string fileName)
        {
            var serializer = new XmlSerializer(typeof(TireGripPredictionMatts));
            using (var reader = new StreamReader(fileName))
            {
                TireGripPredictionMatts loadedData = (TireGripPredictionMatts)serializer.Deserialize(reader);
                this.TireData = loadedData.TireData;

            }
        }
    }
}
