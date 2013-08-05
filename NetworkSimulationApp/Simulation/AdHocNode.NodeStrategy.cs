using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NetworkSimulationApp.Simulation
{
    public partial class AdHocNode
    {
        private void _NodeStrategy()
        {
            
            this._CurrUtility = this._CalCurrUtility();
            int sourceCount = Sources.Count;
            int[] MaxCombination = null;
            string[] IDs = null;
            float MaxUtility = float.MinValue;
            float Utility = 0;
          //  if(_CurrUtility != 0)
            Console.WriteLine(this.ID + ": My Utility is: " + _CurrUtility);
            this.WakeUpCall = false;
            for (int i = 0; i < _Combinations.GetLength(0); i++)
            {
                this._TotalFlowSendAndReached = 0;
                this._TotalFlowConsumed = 0;
                this._TotalFlowSent = 0;
                this._TotalFlowForwarded = 0;
                for (int j = 0; j < _Combinations.GetLength(1); j++)
                {
                    if (_Combinations[i, j] == 1)
                    {
                        if (j < sourceCount)
                        {
                            float thresholdVal = NodeList.Nodes[Sources.ElementAt(j)].MyTargetThresholds[this.ID];
                            if (NodeList.Nodes[Sources.ElementAt(j)].Targets[this.ID])
                            {
                                foreach (KeyValuePair<int, float> flow in SourcesAndFlowConsumed[Sources.ElementAt(j)])
                                {
                                    this._TotalFlowConsumed += flow.Value;
                                }
                                foreach (KeyValuePair<string, float[]> flow in SourcesAndFlowForwarded[Sources.ElementAt(j)])
                                {
                                    float val = flow.Value[1] - (flow.Value[1] * thresholdVal);
                                    IDs = flow.Key.Split(':');
                                    int DestID = int.Parse(IDs[1]);
                                    this._UpdateFlowForwarded(val, DestID,i);
                                    
                                  //  TotalFlowForwarded += val;
                                }
                            }
                            else
                            {
                                KeyValuePair<string, float>[] Fpairs = NodeList.Nodes[Sources.ElementAt(j)].TargetsAndFlowForwarded[this.ID].ToArray();
                                KeyValuePair<int, float>[] Mpairs = NodeList.Nodes[Sources.ElementAt(j)].TargetsAndMyFlowSent[this.ID].ToArray();

                                foreach (KeyValuePair<string, float> pair in Fpairs)
                                {
                                    IDs = pair.Key.Split(':');
                                    int DestID = int.Parse(IDs[1]);
                                    if (this.ID == DestID)
                                    {
                                        this._TotalFlowConsumed += pair.Value;
                                    }
                                    else
                                    {
                                        float val = pair.Value - (pair.Value * thresholdVal);
                                        IDs = pair.Key.Split(':');
                                        DestID = int.Parse(IDs[1]);
                                        this._UpdateFlowForwarded(val, DestID, i);
                                    }
                                }
                                foreach (KeyValuePair<int, float> pair in Mpairs)
                                {
                                    if (this.ID == pair.Key)
                                    {
                                        _TotalFlowConsumed += pair.Value;
                                    }
                                    else
                                    {
                                        float val = pair.Value - (pair.Value * thresholdVal);
                                        int DestID = this.ForwardingTable[pair.Key];
                                        this._UpdateFlowForwarded(val, DestID, i);
                                    }
                                }
                            }
                        }
                        else
                        {
                            int x = j - sourceCount;
                            foreach (KeyValuePair<int, float> pair in this.FlowReached)
                            {
                                if (this.ForwardingTable[pair.Key] == Targets.ElementAt(x).Key) this._TotalFlowSendAndReached += pair.Value;
                            }
                            foreach (KeyValuePair<int, float> pair in this.TargetsAndMyFlowSent[Targets.ElementAt(x).Key])
                            {
                                this._TotalFlowSent += pair.Value;
                            }
                        }
                    }
                }
                Utility = ((this._TotalFlowSendAndReached * this.W) + this._TotalFlowConsumed) - (this._TotalFlowSent + this._TotalFlowForwarded);
                if (Utility > MaxUtility)
                {
                    MaxUtility = Utility;
                    MaxCombination = new int[_Combinations.GetLength(1)];
                    for (int x = 0; x < _Combinations.GetLength(1); x++)
                    {
                        MaxCombination[x] = _Combinations[i, x];
                    }
                }
            }
            if (MaxUtility > this._CurrUtility)
            {
                NodeActivator.NoChangeCounter = 0;
                this._ImplementMaxCombination(MaxCombination);
          //      _OptimizeStrategy(MaxCombination);
            }
            else
            {
               int counter = NodeActivator.NoChangeCounter;
               NodeActivator.NoChangeCounter = counter + 1;
            }
            NodeActivator.NodeDone = true;
        }
        private void _ImplementMaxCombination(int[] MaxCombination)
        {
            int sourceCount = this.Sources.Count;
            for (int i = 0; i < MaxCombination.Length; i++)
            {
                if (i < sourceCount)
                {
                    if (MaxCombination[i] == 0)
                    {
                        if (NodeList.Nodes[this.Sources.ElementAt(i)].Targets[this.ID])
                        {
                            float threshold = NodeList.Nodes[this.Sources.ElementAt(i)].MyTargetThresholds[this.ID];
                            threshold = threshold + this._MinChange;
                            this.FLowBlockRateForSources[this.Sources.ElementAt(i)] = threshold;
                        }
                    }
                    else
                    {
                        if (!NodeList.Nodes[this.Sources.ElementAt(i)].Targets[this.ID])
                        {
                            float threshold = NodeList.Nodes[this.Sources.ElementAt(i)].MyTargetThresholds[this.ID];
                            this.FLowBlockRateForSources[this.Sources.ElementAt(i)] = threshold;
                        }
                    }
                }
                else
                {
                    int x = i - sourceCount;
                    if (MaxCombination[i] == 0)
                    {
                        if (this.Targets.ElementAt(x).Value)
                        {
                            float threshold = NodeList.Nodes[this.Targets.ElementAt(x).Key].FLowBlockRateForSources[this.ID];
                            this.Targets[this.Targets.ElementAt(x).Key] = false;
                            this.MyTargetThresholds[this.Targets.ElementAt(x).Key] = threshold + _MinChange;
                        }
                    }
                    else
                    {
                        if (!this.Targets.ElementAt(x).Value)
                        {
                            float threshold = NodeList.Nodes[this.Targets.ElementAt(x).Key].FLowBlockRateForSources[this.ID];
                            this.MyTargetThresholds[this.Targets.ElementAt(x).Key] = threshold;
                            this.Targets[this.Targets.ElementAt(x).Key] = true;
                        }
                    }
                }
            }
        }
        private void _OptimizeStrategy(int[] MaxCombination)
        {
            int sourceCount = this.Sources.Count;
            int x = 0;
            int TargetID = 0;
            int DestID = 0;
            float totalFlowTargetConsume = 0, totalFlowTargetForward = 0;
            for (int i = 0; i < MaxCombination.Length; i++)
            {
                if (MaxCombination[i] == 1)
                {
                    if (i < sourceCount)
                    {

                    }
                    else
                    {
                        x = i - sourceCount;
                        TargetID = Targets.ElementAt(x).Key;
                        totalFlowTargetConsume = 0;
                        totalFlowTargetForward = 0;
                        foreach (KeyValuePair<string, float> pair in this.TargetsAndFlowForwarded[TargetID])
                        {
                            string[] IDs = pair.Key.Split(':');
                            DestID = int.Parse(IDs[1]);
                            if (DestID == TargetID)
                            {
                                totalFlowTargetConsume += pair.Value;
                            }
                            else
                            {
                                totalFlowTargetForward += pair.Value;
                            }
                        }
                        foreach (KeyValuePair<int, float> pair in this.TargetsAndMyFlowSent[TargetID])
                        {
                            if (pair.Key == TargetID)
                            {
                                totalFlowTargetConsume += pair.Value;
                            }
                            else
                            {
                                totalFlowTargetForward += pair.Value;
                            }
                        }
                        float threshold = NodeList.Nodes[TargetID].FLowBlockRateForSources[this.ID];
                        float forwardAmount = totalFlowTargetForward - (totalFlowTargetForward * threshold);
                        if ((totalFlowTargetConsume > forwardAmount) && (totalFlowTargetForward > totalFlowTargetConsume))
                        {
                            float rate = (totalFlowTargetForward - totalFlowTargetConsume) / totalFlowTargetForward;
                            rate += this._MinChange;
                            if (rate < threshold) this.MyTargetThresholds[TargetID] = rate;
                        }
                    }
                }
            }
        }
        private float _CalCurrUtility()
        {
            float TotalFlowSendAndReached = 0, TotalFlowConsumed = 0, TotalFlowSent = 0, TotalFlowForwarded = 0;
            float CurrVal = 0;

            foreach (int i in Sources)
            {
                if (NodeList.Nodes[i].Targets[this.ID])
                {
                    foreach (KeyValuePair<int, float> flow in SourcesAndFlowConsumed[i])
                    {
                        TotalFlowConsumed += flow.Value;
                    }
                    foreach (KeyValuePair<string, float[]> flow in SourcesAndFlowForwarded[i])
                    {
                        string[] IDs = flow.Key.Split(':');
                        int destID = int.Parse(IDs[1]);
                        if (Targets[destID])
                        {
                            TotalFlowForwarded += flow.Value[0];
                        }
                    }
                }
            }
            foreach (int i in Targets.Keys)
            {
                if (Targets[i])
                {
                    foreach (KeyValuePair<int, float> pair in TargetsAndMyFlowSent[i])
                    {
                        TotalFlowSent += pair.Value;
                    }
                }
            }
            foreach (KeyValuePair<int, float> pair in FlowReached)
            {
                if (Targets[ForwardingTable[pair.Key]])
                {
                    TotalFlowSendAndReached += pair.Value;
                }
            }

            CurrVal = ((TotalFlowSendAndReached * this.W) + TotalFlowConsumed) - (TotalFlowSent + TotalFlowForwarded);
            return CurrVal;
        }

       private void _intializeCombinations()
       {
           int length = Sources.Count + Targets.Count;
           int size = (int) Math.Pow(2, length);
           _Combinations = new int[size, length];
           int j = 0;
           for (int r = 0; r <= size - 1; r++)
           {
               this._GenerateCombinations(r, length, j);
               j++;
           }
       }
      
       private void _GenerateCombinations(int rank, int n, int index)
       {
           int i;
           for (i = n; i >= 1; i--)
           {
               _Combinations[index,(i - 1)] = rank % 2;
               rank = rank / 2;
           }
       }

       private void _UpdateFlowForwarded(float val, int DestID, int i)
       {
           for (int index = 0; index < Targets.Count; index++)
           {
               if (DestID == Targets.ElementAt(index).Key)
               {
                   if (_Combinations[i, (Sources.Count - 1) + index] == 1) _TotalFlowForwarded += val;
               }
           }
       }
    }
}
