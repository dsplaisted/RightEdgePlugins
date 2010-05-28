using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RightEdge.Common;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace RightEdge.Optimization
{
	[DisplayName("Hill Climbing Optimization Plugin")]
#if !DEBUG
	[ExcludedPlugin]
#endif
	public class HillClimbingOptimizationPlugin : OptimizationPlugin
	{
		public int MaxRuns { get; set; }

		private Random _rnd = new Random();

		List<SystemParameterInfo> _parametersToOptimize;
		List<SystemParameterInfo> _unOptimizedParameters;

		public HillClimbingOptimizationPlugin()
		{
			MaxRuns = 25;
		}

		void SetupOptimization()
		{
			_parametersToOptimize = OptimizationParameters.Where(p => p.NumSteps > 1).ToList();
			_unOptimizedParameters = OptimizationParameters.Where(p => p.NumSteps <= 1).ToList();
		}

		protected override bool ShowOptimizationSettings(SystemRunSettings runSettings, System.Windows.Forms.IWin32Window owner)
		{
			//	Don't pop up a dialog, just start the optimization
			return true;
			//return base.ShowOptimizationSettings(runSettings, owner);
		}

		public override List<OptimizationResult> RunOptimization(SystemRunSettings runSettings)
		{
			SetupOptimization();

			List<OptimizationResult> ret = new List<OptimizationResult>();

			double overallProgress = 0;
			int runNo = 0;

			SystemProgressUpdate progressCallback = (currentItem, totalItems, currentTime) =>
			{
				double currentRunProgress = (double)currentItem / totalItems;
				string currentRunProgressText = null;
				if (currentTime != DateTime.MinValue)
				{
					CultureInfo culture = BarUtils.GetCurrencyCulture(runSettings.AccountCurrency);
					currentRunProgressText = "Current Run Progress: " + currentTime.ToString(culture.DateTimeFormat);
				}
				UpdateProgress(null, overallProgress + (currentRunProgress / MaxRuns), currentRunProgressText, currentRunProgress);
			};

			//	Get baseline results
			//runNo++;
			//OptimizationResult currentBest = RunSystem(runSettings, progressCallback);
			//ret.Add(currentBest);
			
			OptimizationResult currentBest = null;

			double[] currentParameters = _parametersToOptimize.Select(p => p.Value).ToArray();

			while (runNo < MaxRuns)
			{
				runNo++;

				string overallProgressText = string.Format("Optimization run {0} of {1}", runNo, MaxRuns);
				overallProgress = ((double)runNo - 1) / MaxRuns;
				UpdateProgress(overallProgressText, overallProgress, "Initializing...", 0);

				if (currentBest != null)
				{
					//	Choose parameters to use for next run
					double[] newParameters = GetRandomNeighbor(currentParameters, 1.0);
					runSettings.SystemParameters = new List<KeyValuePair<string, double>>();
					for (int i = 0; i < _parametersToOptimize.Count; i++)
					{
						runSettings.SystemParameters.Add(new KeyValuePair<string, double>(_parametersToOptimize[i].Name, newParameters[i]));
					}
					runSettings.SystemParameters.AddRange(_unOptimizedParameters.Select(p => new KeyValuePair<string, double>(p.Name, p.Value)));
				}

				OptimizationResult newResults = RunSystem(runSettings, progressCallback);

				if (currentBest == null ||
					EvaluateResults(newResults) >= EvaluateResults(currentBest))
				{
					currentBest = newResults;
					ret.Add(currentBest);
				}
				else
				{
					File.Delete(newResults.ResultsFile);
				}
			}

			//	Remove parameters that weren't optimized from the list so that they don't show up as columns in the optimization results
			HashSet<string> unoptimizedParameters = new HashSet<string>(OptimizationParameters.Where(p => p.NumSteps <= 1).Select(p => p.Name));
			foreach (var result in ret)
			{
				result.ParameterValues = result.ParameterValues.Where(p => !unoptimizedParameters.Contains(p.Key)).ToList();
			}

			return ret;
		}

		//	This will be the value that the optimization process optimizes.
		//	It will search for the set of parameters that gives the highest result for this number.
		public virtual double EvaluateResults(OptimizationResult result)
		{
			//	By default, just use the APR.
			return result.FinalStatistic.APR;
		}

		public virtual double[] GetRandomNeighbor(double[] current, double velocity)
		{
			//	Pick a random direction in n-dimensional space
			//	http://mathworld.wolfram.com/HyperspherePointPicking.html
			double[] vector = new double[_parametersToOptimize.Count].Select(d => GaussianRand()).ToArray();
			double length = Math.Sqrt(vector.Select(d => d * d).Sum());
			vector = vector.Select(d => d / length).ToArray();

			List<KeyValuePair<string, double>> ret = new List<KeyValuePair<string, double>>();
			for (int i = 0; i < _parametersToOptimize.Count; i++)
			{
				var parameter = _parametersToOptimize[i];
				double stepSize = (parameter.High - parameter.Low) / parameter.NumSteps;
				double newValue = current[i] + (stepSize * vector[i] * velocity);

				if (newValue > parameter.High)
				{
					newValue = parameter.High;
				}
				if (newValue < parameter.Low)
				{
					newValue = parameter.Low;
				}

				vector[i] = newValue;
			}

			return vector;
		}

		//	http://stackoverflow.com/questions/218060/random-gaussian-variables
		private double GaussianRand()
		{
			double u1 = _rnd.NextDouble(); //these are uniform(0,1) random doubles
			double u2 = _rnd.NextDouble();
			double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
						 Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
			//double randNormal =
			//             mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
			return randStdNormal;
		}
	}
}
