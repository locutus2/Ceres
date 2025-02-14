#region License notice

/*
  This file is part of the Ceres project at https://github.com/dje-dev/ceres.
  Copyright (C) 2020- by David Elliott and the Ceres Authors.

  Ceres is free software under the terms of the GNU General Public License v3.0.
  You should have received a copy of the GNU General Public License
  along with Ceres. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#region Using directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Ceres.Base.Benchmarking;
using Ceres.Base.DataTypes;
using Ceres.Base.Math;

using Ceres.Chess.EncodedPositions;
using Ceres.Chess.EncodedPositions.Basic;
using Ceres.Chess.LC0.Batches;
using Ceres.Chess.MoveGen.Converters;
using Ceres.Chess.NetEvaluation.Batch;

#endregion

namespace Ceres.Chess.NNEvaluators
{
  /// <summary>
  /// Subclass of NNEvaluatorCompound which implements a weighted
  /// average combination of output heads (possibly each with a different weight).
  /// </summary>
  public class NNEvaluatorLinearCombo :  NNEvaluatorCompound
  {
    const PolicyAveragingType DEFAULT_POLICY_AVERAGING_TYPE = PolicyAveragingType.Arithmetic;

    /// <summary>
    /// Method used for averaging policy probabilities.
    /// </summary>
    public enum PolicyAveragingType
    {
      /// <summary>
      /// Simple arithmetic averaging.
      /// </summary>
      Arithmetic,

      /// <summary>
      /// Geometric averaging.
      /// </summary>
      Geometric
    }

    public delegate float[] WeightsOverrideDelegate(in Position pos);

    public readonly float[] WeightsValue;
    public readonly float[] WeightsPolicy;
    public readonly float[] WeightsM;

    /// <summary>
    /// Method to be used for combining policy values.
    /// </summary>
    public PolicyAveragingType PolicyAveragingMethod;

    /// <summary>
    /// Optional delegate called for each position to determine weights to use for value head for that position.
    /// </summary>
    public readonly WeightsOverrideDelegate WeightsValueOverrideFunc;

    /// <summary>
    /// Optional delegate called for each position to determine weights to use for MLH head for that position.
    /// </summary>
    public readonly WeightsOverrideDelegate WeightsMOverrideFunc;

    /// <summary>
    /// Optional delegate called for each position to determine weights to use for policy head for that position.
    /// </summary>
    public readonly WeightsOverrideDelegate WeightsPolicyOverrideFunc;

    /// <summary>
    /// If evaluators should be run in parallel.
    /// This can improve perforamance, but may not be safe if evaluators have dependencies.
    /// </summary>
    public bool RunParallel = true;

    protected IPositionEvaluationBatch[] subResults;

    /// <summary>
    /// Constructor (for case where weights are the same across the output heads).
    /// </summary>
    /// <param name="evaluators"></param>
    /// <param name="weights"></param>
    /// <param name="policyAveragingMethod"></param>
    public NNEvaluatorLinearCombo(NNEvaluator[] evaluators, 
                                  IList<float> weights = null, 
                                  PolicyAveragingType policyAveragingMethod = DEFAULT_POLICY_AVERAGING_TYPE) 
      : base(evaluators)
    {
      float[] weightsArray;
      if (weights == null)
      {
        // Default equal weight
        weightsArray = MathUtils.Uniform(evaluators.Length);
      }
      else
      {
        if (weights.Count != evaluators.Length) throw new ArgumentException("Number of weights specified does not match number of evaluators");
        weightsArray = weights.ToArray();
      }

      WeightsValue = WeightsPolicy = WeightsM = weightsArray;
      PolicyAveragingMethod = policyAveragingMethod;
    }


    /// <summary>
    /// Constructor (for case where weights are different across the output heads).
    /// </summary>
    /// <param name="evaluators"></param>
    /// <param name="weightsValue"></param>
    /// <param name="weightsPolicy"></param>
    /// <param name="weightsM"></param>
    /// <param name="weightsValueOverrideFunc"></param>
    /// <param name="weightsMOverrideFunc"></param>
    /// <param name="weightsPolicyOverrideFunc"></param>
    /// <param name="policyAveragingMethod"></param>
    public NNEvaluatorLinearCombo(NNEvaluator[] evaluators, 
                                IList<float> weightsValue, 
                                IList<float> weightsPolicy, 
                                IList<float> weightsM,
                                WeightsOverrideDelegate weightsValueOverrideFunc = null,
                                WeightsOverrideDelegate weightsMOverrideFunc = null,
                                WeightsOverrideDelegate weightsPolicyOverrideFunc = null,
                                PolicyAveragingType policyAveragingMethod = DEFAULT_POLICY_AVERAGING_TYPE) 
      : base(evaluators)
    {
      WeightsValue = weightsValue  != null ? weightsValue.ToArray()  : MathUtils.Uniform(evaluators.Length);
      WeightsPolicy = weightsPolicy != null ? weightsPolicy.ToArray() : MathUtils.Uniform(evaluators.Length);
      WeightsM      = weightsM      != null ? weightsM.ToArray()      : MathUtils.Uniform(evaluators.Length);

      WeightsValueOverrideFunc = weightsValueOverrideFunc;
      WeightsMOverrideFunc = weightsMOverrideFunc;
      WeightsPolicyOverrideFunc = weightsPolicyOverrideFunc;
      PolicyAveragingMethod = policyAveragingMethod;
    }


    /// <summary>
    /// The maximum number of positions that can be evaluated in a single batch.
    /// </summary>
    public override int MaxBatchSize => MinBatchSizeAmongAllEvaluators;



    object execLockObj = new object();


    /// <summary>
    /// Implementation of virtual method that actually evaluates a batch.
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="retrieveSupplementalResults"></param>
    /// <returns></returns>
    public override IPositionEvaluationBatch DoEvaluateIntoBuffers(IEncodedPositionBatchFlat positions, bool retrieveSupplementalResults = false)
    {
      lock (execLockObj)
      {
        subResults = new IPositionEvaluationBatch[Evaluators.Length];

        // Ask all constituent evaluators to evaluate this batch (in parallel)
        if (RunParallel)
        {
          Parallel.For(0, Evaluators.Length, i => subResults[i] = Evaluators[i].EvaluateIntoBuffers(positions, retrieveSupplementalResults));           
        }
        else
        {
          for (int i=0; i<Evaluators.Length;i++)
          {
            subResults[i] = Evaluators[i].EvaluateIntoBuffers(positions, retrieveSupplementalResults);
          }
        }

        if (retrieveSupplementalResults) throw new NotImplementedException();
        float[] valueHeadConvFlat = null;

        // Extract the combined policies
        CompressedPolicyVector[] policies = ExtractComboPolicies(positions);

        // Compute average value result
        FP16[] w = null;
        FP16[] l = null;
        FP16[] m = null;

        // TODO: also compute and pass on the averaged Activations
        Memory<NNEvaluatorResultActivations> activations = new Memory<NNEvaluatorResultActivations>();

        w = WeightsValueOverrideFunc == null ? AverageFP16(positions.NumPos, subResults, (e, i) => e.GetWinP(i), WeightsValue)
                                             : AverageFP16(positions.NumPos, subResults, (e, i) => e.GetWinP(i), WeightsValueOverrideFunc, positions);

        if (IsWDL)
        {
          l = WeightsValueOverrideFunc == null ? AverageFP16(positions.NumPos, subResults, (e, i) => e.GetLossP(i), WeightsValue)
                                               : AverageFP16(positions.NumPos, subResults, (e, i) => e.GetLossP(i), WeightsValueOverrideFunc, positions);
        }

        if (HasM)
        { 
          m = WeightsValueOverrideFunc == null ? AverageFP16(positions.NumPos, subResults, (e, i) => e.GetM(i), WeightsM)
                                               : AverageFP16(positions.NumPos, subResults, (e, i) => e.GetM(i), WeightsMOverrideFunc, positions);
        }

        TimingStats stats = new TimingStats();
        return new PositionEvaluationBatch(IsWDL, HasM, positions.NumPos, policies, w, l, m, activations, stats);
      }
    }


    private CompressedPolicyVector[] ExtractComboPolicies(IEncodedPositionBatchFlat positions)
    {
      Span<float> policyAverages = stackalloc float[EncodedPolicyVector.POLICY_VECTOR_LENGTH];

      // Compute average policy result for all positions
      CompressedPolicyVector[] policies = new CompressedPolicyVector[positions.NumPos];
      for (int posIndex = 0; posIndex < positions.NumPos; posIndex++)
      {
        policyAverages.Clear();

        float[] weights = WeightsPolicyOverrideFunc == null ? WeightsPolicy 
                                                            : WeightsPolicyOverrideFunc(MGChessPositionConverter.PositionFromMGChessPosition(in positions.Positions[posIndex]));

        for (int evaluatorIndex = 0; evaluatorIndex < Evaluators.Length; evaluatorIndex++)
        {
          (Memory<CompressedPolicyVector> policiesArray, int policyIndex) = subResults[evaluatorIndex].GetPolicy(posIndex);
          CompressedPolicyVector thesePolicies = policiesArray.Span[policyIndex];
          foreach ((EncodedMove move, float probability) moveInfo in thesePolicies.ProbabilitySummary())
          {
            if (moveInfo.move.RawValue == CompressedPolicyVector.SPECIAL_VALUE_RANDOM_NARROW ||
                moveInfo.move.RawValue == CompressedPolicyVector.SPECIAL_VALUE_RANDOM_WIDE)
            {
              throw new NotImplementedException("Mixing NNEvaluatorLinearCombo and random evaluator probably not yet supported");
            }

            switch (PolicyAveragingMethod)
            {
              case PolicyAveragingType.Arithmetic:
                policyAverages[moveInfo.move.IndexNeuralNet] += moveInfo.probability * weights[evaluatorIndex];
                break;
              case PolicyAveragingType.Geometric:
                float rawContrib = MathF.Pow(moveInfo.probability, Evaluators.Length);
                float thisContribution = rawContrib * weights[evaluatorIndex];
                if (evaluatorIndex == 0)
                {
                  policyAverages[moveInfo.move.IndexNeuralNet] = thisContribution;
                }
                else
                {
                  policyAverages[moveInfo.move.IndexNeuralNet] *= thisContribution;
                }
                break;
              default:
                throw new NotImplementedException("Unknown policy averaging method " + PolicyAveragingMethod);
            }

          }

          // Finalize geometric averaging if in use.
          if (PolicyAveragingMethod == PolicyAveragingType.Geometric)
          {
            FinalizeGeometricAverages(policyAverages);
          }
        }

        CompressedPolicyVector.Initialize(ref policies[posIndex], policyAverages, false);
      }

      return policies;
    }


    private void FinalizeGeometricAverages(Span<float> policyAverages)
    {
      float sum = 0;
      for (int i = 0; i < policyAverages.Length; i++)
      {
        if (policyAverages[i] > 0)
        {
          float adjusted = MathF.Pow(policyAverages[i], 1.0f / Evaluators.Length);
          policyAverages[i] = adjusted;
          sum += adjusted;
        }
      }

      Debug.Assert(sum > 0);

      // Normalize to sum to 1.0;
      for (int i = 0; i < policyAverages.Length; i++)
      {
        policyAverages[i] /= sum;
      }
    }

    #region Utility methods


    // --------------------------------------------------------------------------------------------
    static FP16[] AverageFP16(int numPos, IPositionEvaluationBatch[] batches,
                              Func<IPositionEvaluationBatch, int, float> getValueFunc, float[] weights)
    {
      FP16[] ret = new FP16[numPos];
      for (int i = 0; i < numPos; i++)
      {
        for (int evaluatorIndex = 0; evaluatorIndex < batches.Length; evaluatorIndex++)
        {
          ret[i] += (FP16)(weights[evaluatorIndex] * getValueFunc(batches[evaluatorIndex], i));
        }
      }

      return ret;
    }

    // --------------------------------------------------------------------------------------------
    static FP16[] AverageFP16(int numPos, IPositionEvaluationBatch[] batches,
                              Func<IPositionEvaluationBatch, int, float> getValueFunc, 
                              WeightsOverrideDelegate weightFunc, IEncodedPositionBatchFlat positions)
    {
      FP16[] ret = new FP16[numPos];
      for (int i = 0; i < numPos; i++)
      {
        for (int evaluatorIndex = 0; evaluatorIndex < batches.Length; evaluatorIndex++)
        {
          float[] weight = weightFunc(MGChessPositionConverter.PositionFromMGChessPosition(in positions.Positions[i]));
          ret[i] += (FP16)(weight[evaluatorIndex] * getValueFunc(batches[evaluatorIndex], i));
        }
      }
      return ret;
    }

    #endregion
  }
}
