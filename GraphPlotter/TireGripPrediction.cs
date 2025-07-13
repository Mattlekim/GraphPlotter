using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.Xna.Framework;
namespace GraphPlotter
{
    public struct TireGripAngleData
    {
        public float MarginOfError;

        public float SeeringAngle;
        public float MinSpeed, MaxSpeed;
        public float MinYawRate, MaxYawRate;
        public float QuadraticLocation;
        public float QuadraticValue;

        //for the second part of the curve
        public float ExponatePointEnd;
        private float _exponateEnd;
        public float ExponateEnd
        { get => _exponateEnd;
            set
            {
                _exponateEnd = Math.Clamp(value,-1,1);

                ExponatePointEnd = MaxYawRate * _exponateEnd + QuadraticValue * (1 - _exponateEnd);

            }
        
        }



        //for the first part of the curve
        public float ExponatePointStart;
        private float _exponateStartValue;
        public float ExponateStartValue
        {
            get => _exponateStartValue;
            set
            {
                _exponateStartValue = Math.Clamp(value, -1, 1);

                ExponatePointStart =QuadraticValue * _exponateStartValue;

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
           
            if (speed < QuadraticLocation)
            {
             //   return 0;
                per = (speed - MinSpeed) / (QuadraticLocation - MinSpeed);
                return QuadraticBezier(MinYawRate, ExponatePointStart, QuadraticValue, per);
            }
            //return 0;
            per = (speed - QuadraticLocation) / (MaxSpeed - QuadraticLocation);

            return QuadraticBezier(QuadraticValue, ExponatePointEnd, MaxYawRate, per);
        }
    }

    public class TireGripPrediction
    {
        public List<TireGripAngleData> TireData { get; set; } = new List<TireGripAngleData>();

        private float CalChangeInYawRate(GraphDataPoint dp, int a, int b)
        {
            if (dp.SteeringAngle < 0)
                return dp.YawRates[a] - dp.YawRates[b];
            else
                return dp.YawRates[b] - dp.YawRates[a];
        }

        public static float ChangeInThreshold = .4f; //this is the change in yaw rate that we will use to determine the start of the drop off in yaw rate

        public TireGripPrediction(GraphData data)
        {



            foreach(GraphDataPoint dp in data.Points)
            {
                if (dp.YawRates == null || dp.YawRates.Count == 0)
                    continue;

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

                    if (changeInYawRate > 0)
                    {
                        float newChangeInYawRate = CalChangeInYawRate(dp, i + 1, i);

                        if (MathHelper.Distance(newChangeInYawRate, changeInYawRate) > ChangeInThreshold) //start of drop off
                        {
                            tgd.QuadraticLocation = i;
                            tgd.QuadraticValue = dp.YawRates[i];
                            changeInYawRate = -1; //stop flag
                        }
                    }
                    
                }

                tgd.MaxSpeed = index; //set max speed //we subtract 1 becase ther 
                tgd.MaxYawRate = dp.YawRates[index]; //set max yawrate

                //work out margin of error for the first part of the curve

                tgd.ExponateStartValue = -1f;
                float bestMOE = float.PositiveInfinity;
                float bestExponate = 0;
                
                while (tgd.ExponateStartValue < 1)
                {
                    float moe = 0;

                    for (int i = (int)tgd.MinSpeed; i < tgd.QuadraticLocation; i++)
                    {
                        moe += Math.Abs(1 - (tgd.Predict(i) / dp.YawRates[i]));
                    }

                    if (moe < bestMOE)
                    {
                        bestMOE = moe;
                        bestExponate = tgd.ExponateStartValue;
                    }

                    
                    tgd.ExponateStartValue += .001f;
                }
                tgd.MarginOfError += bestMOE;
                tgd.ExponateStartValue = bestExponate;
                //now lets work out margin of error for the second part of the curve

                tgd.ExponateEnd = -1f;

                bestMOE = float.PositiveInfinity;
                bestExponate = 0;
                while (tgd.ExponateEnd < 1)
                {
                    float moe = 0;

                    for (int i = (int)tgd.QuadraticLocation; i < tgd.MaxSpeed; i++)
                    {
                        moe += Math.Abs(1 - (tgd.Predict(i) / dp.YawRates[i]));
                    }

                    if (moe < bestMOE)
                    {
                        bestMOE = moe;
                        bestExponate = tgd.ExponateEnd;
                    }

                    tgd.ExponateEnd += .001f;
                }

                tgd.ExponateEnd = bestExponate;
                tgd.MarginOfError += bestMOE;

             //   tgd.MarginOfError /= dp.YawRates.Count;
                if (dp.SteeringAngle == 0) //no angle
                {
                    tgd.MaxSpeed = 100;
                    tgd.MaxYawRate = 0;
                    tgd.MinYawRate = 0;
                    tgd.QuadraticLocation = 50;
                    tgd.QuadraticValue = 0;
                    tgd.ExponateStartValue = 0;
                }

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

            float differnce = Math.Clamp((MathHelper.Distance(steeringAngle, TireData[index].SeeringAngle) / 10f),0,1);
           
            tgad.QuadraticValue = MathHelper.Lerp(TireData[index].QuadraticValue, TireData[index - 1].QuadraticValue, differnce);
            tgad.QuadraticLocation = MathHelper.Lerp(TireData[index].QuadraticLocation, TireData[index - 1].QuadraticLocation, differnce);
            tgad.MinYawRate = MathHelper.Lerp(TireData[index].MinYawRate, TireData[index - 1].MinYawRate, differnce);
            tgad.MaxYawRate = MathHelper.Lerp(TireData[index].MaxYawRate , TireData[index - 1].MaxYawRate, differnce);
            tgad.MinSpeed = MathHelper.Lerp(TireData[index].MinSpeed, TireData[index - 1].MinSpeed, differnce);
            tgad.MaxSpeed = MathHelper.Lerp(TireData[index].MaxSpeed, TireData[index - 1].MaxSpeed, differnce);

            tgad.ExponatePointStart = MathHelper.Lerp(TireData[index].ExponatePointStart, TireData[index - 1].ExponatePointStart, differnce);
            tgad.ExponatePointEnd = MathHelper.Lerp(TireData[index].ExponatePointEnd, TireData[index - 1].ExponatePointEnd, differnce);

            tgad.ExponateStartValue = MathHelper.Lerp(TireData[index].ExponateStartValue, TireData[index - 1].ExponateStartValue, differnce);
            tgad.ExponateEnd = MathHelper.Lerp(TireData[index].ExponateEnd, TireData[index - 1].ExponateEnd, differnce);

            

            return tgad.Predict(speed);


            
        }
    }
}
