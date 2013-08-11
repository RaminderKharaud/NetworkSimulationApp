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
            bool flag = false;
            int sourceCount = Sources.Count;
            int[] MaxCombination = new int[_Combinations.GetLength(1)];
            int[] CurrCombination = new int[_Combinations.GetLength(1)];
            string[] IDs = null;
            double MaxUtility = double.MinValue;
            double Utility = 0;

            this._CurrDemandOptimization();
            this._ReArrange();
         //   this._CurrUtility = this._CalCurrUtility();
         //   Console.WriteLine(this.ID + ": My CurrUtility is: " + this._CurrUtility);

            this.WakeUpCall = false;
            for (int i = 0; i < this._Combinations.GetLength(0); i++)
            {
                this._TotalFlowSendAndReached = 0;
                this._TotalFlowConsumed = 0;
                this._TotalFlowSent = 0;
                this._TotalFlowForwarded = 0;
                
                for (int x = 0; x < this._Combinations.GetLength(1); x++)
                {
                    CurrCombination[x] = this._Combinations[i, x];
                }

                _UpdateThresholdAndBlockRates(CurrCombination);

                for (int j = 0; j < this._Combinations.GetLength(1); j++)
                {
                    if (this._Combinations[i, j] == 1)
                    {
                        if (j < sourceCount)
                        {
                            int sourceID = Sources.ElementAt(j);
                            double TotalVal = 0;
                            KeyValuePair<string, double>[] Fpairs = NodeList.Nodes[sourceID].TargetsAndFlowForwarded[this.ID].ToArray();
                            KeyValuePair<int, double>[] Mpairs = NodeList.Nodes[sourceID].TargetsAndMyFlowSent[this.ID].ToArray();

                            foreach (KeyValuePair<string, double> pair in Fpairs)
                            {
                                IDs = pair.Key.Split(':');
                                int DestID = int.Parse(IDs[1]);
                                if (this.ID == DestID)
                                {
                                    this._TotalFlowConsumed += pair.Value;
                                }
                                else
                                {
                                    TotalVal += pair.Value;
                                }
                            }
                            foreach (KeyValuePair<int, double> pair in Mpairs)
                            {
                                if (this.ID == pair.Key)
                                {
                                    _TotalFlowConsumed += pair.Value;
                                }
                                else
                                {
                                    TotalVal += pair.Value;
                                }
                            }
                            this._TotalFlowForwarded += TotalVal - this.FlowBlockValueForSources[sourceID];
                        }
                        else
                        {
                            int x = j - sourceCount;
                            int targetID = Targets.ElementAt(x).Key;
                            foreach (KeyValuePair<int, double> pair in this.FlowReached)
                            {
                                if (this.ForwardingTable[pair.Key] == targetID) this._TotalFlowSendAndReached += pair.Value;
                            }
                            foreach (KeyValuePair<int, double> pair in this.TargetsAndMyFlowSent[targetID])
                            {
                                this._TotalFlowSent += pair.Value;
                               // this._TotalFlowSendAndReached += pair.Value - this.MyTargetThresholds[targetID]; //-------try this
                            }
                        }
                    }
                }
                Utility = ((this._TotalFlowSendAndReached * this.W) + this._TotalFlowConsumed) - (this._TotalFlowSent + this._TotalFlowForwarded);
                if (Utility > MaxUtility)
                {
                    MaxUtility = Utility;
                    for (int x = 0; x < this._Combinations.GetLength(1); x++)
                    {
                        MaxCombination[x] = this._Combinations[i, x];
                    }
                }
            }

           // Console.WriteLine(this.ID + ": My MaxUtility is: " + MaxUtility);
            this.CurrUtility = MaxUtility;
            for (int index = 0; index < MaxCombination.Length; index++)
            {
                if (MaxCombination[index] != this._CurrCombination[index])
                {
                    flag = true;
                    break;
                }
            }
            if(flag)
            {
                NodeActivator.NoChangeCounter = 0;
                this._CurrCombination = MaxCombination;
                this._UpdateThresholdAndBlockRates(MaxCombination);
            }
            else
            {
               int counter = NodeActivator.NoChangeCounter;
               NodeActivator.NoChangeCounter = counter + 1;
            } 
        }
        private void _UpdateThresholdAndBlockRates(int[] CurrCombination)
        {
            int x = 0,targetID = 0,sourceID = 0;
            double threshold = 0, BlockRate = 0;

            for (int j = 0; j < CurrCombination.Length; j++)
            {
                    
                if (j < Sources.Count)
                {
                    sourceID = Sources.ElementAt(j);
                    threshold = NodeList.Nodes[sourceID].MyTargetThresholds[this.ID];
                    if (CurrCombination[j] == 1)
                    {
                        this.FlowBlockValueForSources[sourceID] = threshold;
                    }
                    else
                    {
                        this.FlowBlockValueForSources[sourceID] = threshold + this._MinChange;
                    }
                }
                else
                {
                    x = j - Sources.Count;
                    targetID = Targets.ElementAt(x).Key;
                    BlockRate = NodeList.Nodes[targetID].FlowBlockValueForSources[this.ID];
                    if (CurrCombination[j] == 1)
                    {
                        this.MyTargetThresholds[targetID] = BlockRate;
                        this.Targets[targetID] = true;
                    }
                    else
                    {
                        this.MyTargetThresholds[targetID] = BlockRate - this._MinChange;
                        this.Targets[targetID] = false;
                    }
                }
            }
        }
        private void _ReArrange()
        {
            foreach (int i in this.Targets.Keys)
            {
                if (NodeList.Nodes[i].FlowBlockValueForSources[this.ID] > this.MyTargetThresholds[i])
                {
                    this.Targets[i] = false;
                }
                else
                {
                    this.Targets[i] = true;
                }
            }
        }

        private void _CurrDemandOptimization()
        {
            double amountReached = 0;     
            foreach (int dest in this.MyDestinationsAndCurrentDemands.Keys)
            {        
                amountReached = this.FlowReached[dest];
                if (amountReached < 0) amountReached = 0;
                this.MyDestinationsAndCurrentDemands[dest] = amountReached;
            }
        }
            
      /*  private double _CalCurrUtility()
        {
            double TotalFlowSendAndReached = 0, TotalFlowConsumed = 0, TotalFlowSent = 0, TotalFlowForwarded = 0;
            double CurrVal = 0;

            foreach (int i in Sources)
            {
                if (NodeList.Nodes[i].Targets[this.ID])
                {
                    foreach (KeyValuePair<int, double> flow in SourcesAndFlowConsumed[i])
                    {
                        TotalFlowConsumed += flow.Value;
                    }
                    foreach (KeyValuePair<string, double[]> flow in SourcesAndFlowForwarded[i])
                    {
                        TotalFlowForwarded += flow.Value[0];
                    }
                }
            }
            foreach (int i in Targets.Keys)
            {
                if (Targets[i])
                {
                    foreach (KeyValuePair<int, double> pair in TargetsAndMyFlowSent[i])
                    {
                        TotalFlowSent += pair.Value;
                    }
                }
            }
            foreach (KeyValuePair<int, double> pair in FlowReached)
            {
                if (Targets[ForwardingTable[pair.Key]])
                {
                    TotalFlowSendAndReached += pair.Value;
                }
            }

            CurrVal = ((TotalFlowSendAndReached * this.W) + TotalFlowConsumed) - (TotalFlowSent + TotalFlowForwarded);
            
            return CurrVal;
        } */

       private void _intializeCombinations()
       {
           int length = Sources.Count + Targets.Count;
           this._CurrCombination = new int[length];
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
       public double getTotalCurrentFlow()
       {
           double totalFlow = 0;
           foreach(KeyValuePair<int, double> pair in this.MyDestinationsAndCurrentDemands)
           {
               if(this.Targets[ForwardingTable[pair.Key]])
               {
                   totalFlow += pair.Value;
               }
           }
           return totalFlow;
       }
       public double getNumberOfEdgesAlive()
       {
           double edgesNum = 0;
           foreach (KeyValuePair<int, bool> pair in this.Targets)
           {
               if (pair.Value) edgesNum++;
           }
           return edgesNum;
       }
    }
}
