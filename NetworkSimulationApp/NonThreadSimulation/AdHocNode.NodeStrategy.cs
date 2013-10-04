using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkSimulationApp.AdHocMessageBox;

namespace NetworkSimulationApp.NonThreadSimulation
{
    /// <summary>
    /// For code comments please refer to the same class under "Simulation" folder
    /// </summary>
    internal partial class AdHocNode
    {
        public void NodeStrategy()
        {
            bool flag = false;
            int sourceCount = Sources.Count;
            int[] MaxCombination = new int[_Combinations.GetLength(1)];
            int[] CurrCombination = new int[_Combinations.GetLength(1)];
            string[] IDs = null;
            float MaxUtility = float.MinValue;
            float Utility = 0;

            this._UpdateSuccessfulFlow();
            this._CurrDemandOptimization();
            this._ReArrange();

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

                for (int j = 0; j < this._Combinations.GetLength(1); j++)
                {
                    if (this._Combinations[i, j] == 1)
                    {
                        if (j < sourceCount)
                        {
                            int sourceID = Sources.ElementAt(j);
                            float TotalVal = 0;
                            float threshold = NodeList.Nodes[sourceID].MyTargetThresholds[this.ID];

                            KeyValuePair<string, float>[] Fpairs = NodeList.Nodes[sourceID].TargetsAndFlowForwarded[this.ID].ToArray();
                            KeyValuePair<int, float>[] Mpairs = NodeList.Nodes[sourceID].TargetsAndMyFlowSent[this.ID].ToArray();

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
                                    TotalVal += pair.Value;
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
                                    TotalVal += pair.Value;
                                }
                            }

                            if (TotalVal > threshold)
                            {
                                this._TotalFlowForwarded += TotalVal - threshold;
                            }
                        }
                        else
                        {
                            int x = j - sourceCount;
                            int targetID = Targets.ElementAt(x).Key;

                            foreach (KeyValuePair<int, float> pair in this.FlowReached)
                            {
                                if (this.ForwardingTable[pair.Key] == targetID)
                                {
                                    if (this.checkTargetDestination(targetID))
                                    {
                                        this._TotalFlowSendAndReached += this.MyDestinationsAndDemands[targetID];
                                    }
                                    else
                                    {
                                        this._TotalFlowSendAndReached += pair.Value;
                                    }
                                }
                            }

                            foreach (KeyValuePair<int, float> pair in this.TargetsAndMyFlowSent[targetID])
                            {
                                this._TotalFlowSent += pair.Value;
                            }
                        }
                    }
                }
                Utility = ((this._TotalFlowSendAndReached * this._W) + this._TotalFlowConsumed) - (this._TotalFlowSent + this._TotalFlowForwarded);

                if (Utility > MaxUtility)
                {
                    MaxUtility = Utility;
                    for (int x = 0; x < this._Combinations.GetLength(1); x++)
                    {
                        MaxCombination[x] = this._Combinations[i, x];
                    }
                }
            }

            this._CurrUtility = MaxUtility;

            for (int index = 0; index < MaxCombination.Length; index++)
            {
                if (MaxCombination[index] != this._CurrCombination[index])
                {
                    flag = true;
                    break;
                }
            }

            if (flag)
            {
                NodeActivator.NoChangeCounter = 0;
                for (int index = 0; index < MaxCombination.Length; index++)
                {
                    this._CurrCombination[index] = MaxCombination[index];
                }
                this._UpdateThresholdAndBlockRates(MaxCombination);
            }
            else
            {
                int counter = NodeActivator.NoChangeCounter;
                NodeActivator.NoChangeCounter = counter + 1;
            }
           
        }


        private void _UpdateThresholdAndBlockRates(int[] Combination)
        {
            int x = 0, targetID = 0, sourceID = 0;
            float threshold = 0, BlockRate = 0;

            for (int j = 0; j < Combination.Length; j++)
            {
                if (j < Sources.Count)
                {
                    sourceID = Sources.ElementAt(j);
                    threshold = NodeList.Nodes[sourceID].MyTargetThresholds[this.ID];
                    if (Combination[j] == 1)
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

                    if (Combination[j] == 1)
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
            int key = 0;
            for (int j = 0; j < this.Targets.Count; j++)
            {
                key = this.Targets.ElementAt(j).Key;

                if (NodeList.Nodes[key].FlowBlockValueForSources[this.ID] > this.MyTargetThresholds[key])
                {
                    this.Targets[key] = false;
                }
                else
                {
                    this.Targets[key] = true;
                }
            }

        }

        private void _CurrDemandOptimization()
        {
            float amountReached = 0;
            foreach (int dest in this.MyDestinationsAndDemands.Keys)
            {
                amountReached = this.FlowReached[dest];
                if (amountReached < 0) amountReached = 0;
                this.MyDestinationsAndCurrentDemands[dest] = amountReached;
                if (checkTarget(dest)) this.MyDestinationsAndCurrentDemands[dest] = this.MyDestinationsAndDemands[dest];
            }
        }

        private void _intializeCombinations()
        {
            try
            {
                int length = 0, j = 0, size = 0;

                length = Sources.Count + Targets.Count;
                this._CurrCombination = new int[length];

                for (int x = 0; x < length; x++) this._CurrCombination[x] = 1;

                size = (int)Math.Pow(2, length);
                this._Combinations = new byte[size, length];

                j = 0;
                for (int r = 0; r <= size - 1; r++)
                {
                    this._GenerateCombinations(r, length, j);
                    j++;
                }
            }
            catch (Exception)
            {
                ExceptionMessage.Show("Thread is out of memory. Canceling Simulation");
                NodeActivator.Cancel = true;
            }
        }

        private void _GenerateCombinations(int rank, int n, int index)
        {
            for (int i = n; i >= 1; i--)
            {
                _Combinations[index, (i - 1)] = (byte)(rank % 2);
                rank = rank / 2;
            }
        }
        public float getTotalCurrentFlow()
        {
            float totalFlow = 0;
            this._CurrDemandOptimization();
            foreach (KeyValuePair<int, float> pair in this.MyDestinationsAndCurrentDemands) totalFlow += pair.Value;
            return totalFlow;
        }

        public float getUtility()
        {
            return this._CurrUtility;
        }

        public bool checkTargetDestination(int i)
        {
            foreach (int dest in this.MyDestinationsAndDemands.Keys)
            {
                if (i == dest) return true;
            }
            return false;
        }
        public bool checkTarget(int i)
        {
            foreach (int key in this.Targets.Keys)
            {
                if (key == i) return true;
            }
            return false;
        }
    }
}
