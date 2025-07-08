using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.Xna.Framework;
namespace GraphPlotter
{
    public struct TireGripAngleData
    {
        public float SeeringAngle;
        public float MinSpeed, MaxSpeed;
        public float MinYawRate, MaxYawRate;
        public int YawRateChangePoint;
        public float YawRateChangePointValue;

        private float _exponate;
        public float Exponate
        { get => _exponate;
            set
            {
                _exponate = Math.Clamp(value,0,1);

                ExponatePoint = MaxYawRate * _exponate + YawRateChangePointValue * (1 - _exponate);

            }
        
        }
        
        public float ExponatePoint;


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

            if (speed < YawRateChangePoint)
            {
                per = (speed - MinSpeed) / (YawRateChangePoint - MinSpeed);
                return MathHelper.Lerp(MinYawRate, YawRateChangePointValue, per);
            }

            per = (speed - YawRateChangePoint) / (MaxSpeed - YawRateChangePoint);

            return QuadraticBezier(YawRateChangePointValue, ExponatePoint, MaxYawRate, per);
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

                        if (MathHelper.Distance(newChangeInYawRate, changeInYawRate) > .006) //start of drop off
                        {
                            tgd.YawRateChangePoint = i;
                            tgd.YawRateChangePointValue = dp.YawRates[i];
                            changeInYawRate = -1; //stop flag
                        }
                    }
                    
                }

                tgd.MaxSpeed = index; //set max speed //we subtract 1 becase ther 
                tgd.MaxYawRate = dp.YawRates[index]; //set max yawrate

                //now lets work out margin of error
                
                tgd.Exponate = 0f;

                float bestMOE = float.PositiveInfinity;
                float bestExponate = 0;
                while (tgd.Exponate < 1)
                {
                    float moe = 0;

                    for (int i = tgd.YawRateChangePoint; i < tgd.MaxSpeed; i++)
                    {
                        moe += Math.Abs(1 - (tgd.Predict(i) / dp.YawRates[i]));
                    }

                    if (moe < bestMOE)
                    {
                        bestMOE = moe;
                        bestExponate = tgd.Exponate;
                    }

                    tgd.Exponate += .01f;
                }

                tgd.Exponate = bestExponate;
                TireData.Add(tgd);



            }

        }


        public float Predict(float speed, float steeringAngle)
        {
            int index = TireData.FindIndex(x => x.SeeringAngle == steeringAngle);
            if (index == -1)
                return 0;

            return TireData[index].Predict(speed);


            
        }
    }
}
