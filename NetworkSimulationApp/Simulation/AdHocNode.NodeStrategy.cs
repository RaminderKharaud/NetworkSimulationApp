 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NetworkSimulationApp.Simulation
{
    /// <summary>
    /// File:                   AdHocNode.NodeStrategy.cs
    /// 
    /// Author:                 Raminderpreet Singh Kharaud
    /// 
    /// Date:       June 2013
    /// 
    /// Revision    1.1         No Revision Yet
    /// 
    /// Purpose:                This is the second part of AdHocNode class which has methods for
    ///                         strategy updates and getter methods
    /// </summary>
    internal partial class AdHocNode
    {
        /// <summary>
        /// This is the main strategy method which calls other methods to update all
        /// information before it check all possible combination of edges to see which combination
        /// gives the best utility. once it finds that combination, it calls method to implements that
        /// utility and store the information.
        /// </summary>
        public bool NodeStrategy()
        {
            lock (_StrategyLock)
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

               // this.WakeUpCall = false;
                //check utility for each combination
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
                            if (j < sourceCount) //check if the current edge in the combination is source or target
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
                            else // if the edge is target
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
                    //calculate utility for current combination
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
                //if the best combination is not current combination, implement it
                if (flag)
                {
                    NodeActivator.NoChangeCounter = 0;
                    for (int index = 0; index < MaxCombination.Length; index++)
                    {
                        this._CurrCombination[index] = MaxCombination[index];
                    }
                    this._UpdateThresholdAndBlockRates(MaxCombination);
                    foreach (int key in this.Targets.Keys)
                    {
                        NodeList.Nodes[key].WakeUpCall = true;
                    }
                    foreach (int key in this.SourcesAndFlowConsumed.Keys)
                    {
                        NodeList.Nodes[key].WakeUpCall = true;
                    }
                    return false;
                }
                else
                {
                  //  int counter = NodeActivator.NoChangeCounter;
                  //  NodeActivator.NoChangeCounter = counter + 1;
                    return true;
                }
            }
        }

        /// <summary>
        /// This method implements the best combination decided by strategy
        /// </summary>
        /// <param name="Combination"></param>
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
        /// <summary>
        /// This method is called by node before it finds the best combination.
        /// It checks for threshold for each target node and if any node has 
        /// block value more than its threshold,it will the cut the edge with that node
        /// </summary>
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
        /// <summary>
        /// updates current flow that this node is sending to 
        /// its destinations according to the flow that is 
        /// reaching to each destination
        /// </summary>
        private void _CurrDemandOptimization()
        {
            float amountReached = 0;
            foreach (int dest in this.MyDestinationsAndCurrentDemands.Keys)
            {
                amountReached = this.FlowReached[dest];
                if (amountReached < 0) amountReached = 0;
                this.MyDestinationsAndCurrentDemands[dest] = amountReached;
                if (checkTarget(dest)) this.MyDestinationsAndCurrentDemands[dest] = this.MyDestinationsAndDemands[dest];
            }
        }
        /// <summary>
        /// intialize array of all possible combinations
        /// </summary>
        private void _intializeCombinations()
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
        /// <summary>
        /// this method has algorithm to returns unique combination
        /// of 0's and 1's for each index
        /// </summary>
        private void _GenerateCombinations(int rank, int n, int index)
        {
            for (int i = n; i >= 1; i--)
            {
                _Combinations[index, (i - 1)] = (byte)(rank % 2);
                rank = rank / 2;
            }
        }
        /// <summary>
        /// This is a getter method
        /// </summary>
        /// <returns>returns the total current successful of this node</returns>
        public float getTotalCurrentFlow()
        {
            float totalFlow = 0;
            this._CurrDemandOptimization();
            foreach (KeyValuePair<int, float> pair in this.MyDestinationsAndCurrentDemands) totalFlow += pair.Value;
            return totalFlow;
        }
        //returns the current utility of this node
        public float getUtility()
        {
            return this._CurrUtility;
        }
        //check if destination is directly connected to this node
        public bool checkTargetDestination(int i)
        {
            foreach (int dest in this.MyDestinationsAndDemands.Keys)
            {
                if (i == dest) return true;
            }
            return false;
        }
        //check if target node is also a destination for this node
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
