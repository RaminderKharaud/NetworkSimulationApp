﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSimulationApp
{
    public class AdHocFlow
    {
        public static int TotalFlowBlocked;
        public static int TotalFlowAmount;
        public static AdHocSignal[,] signal;
        public static int VertexCount;
        public AdHocFlow(int VertCount)
        {
            VertexCount = VertCount;
            TotalFlowAmount = 0;
            TotalFlowBlocked = 0;
            signal = new AdHocSignal [VertCount,VertCount];
            int i = 0, j = 0;
            for (i = 0; i < VertCount; i++)
            {
                for (j = 0; j < VertCount; j++)
                {
                    signal[i, j] = new AdHocSignal();
                }
            }
        }
    }
}