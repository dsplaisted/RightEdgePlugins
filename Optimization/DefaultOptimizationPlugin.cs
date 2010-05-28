using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using RightEdge.Common;
using System.Globalization;
using System.IO;

namespace RightEdge.Optimization
{
	[DisplayName("Default (Brute Force) Optimization Plugin")]
	public class DefaultOptimizationPlugin : OptimizationPlugin
	{
		protected override bool ShowOptimizationSettings(SystemRunSettings runSettings, System.Windows.Forms.IWin32Window owner)
		{
			return base.ShowOptimizationSettings(runSettings, owner);
		}

		protected override void LoadOptimizationSettingsFromFile(SystemRunSettings runSettings, string filename)
		{
			base.LoadOptimizationSettingsFromFile(runSettings, filename);
		}

		public override List<OptimizationResult> RunOptimization(SystemRunSettings runSettings)
		{
			var optimizationResults = new List<OptimizationResult>();
			int[] optProgress = new int[OptimizationParameters.Count];
			Dictionary<string, double> paramDict = new Dictionary<string, double>(OptimizationParameters.Count);
			int runNo = 0;
			while (true)
			{
				runNo++;

				int totalRuns = 1;

				for (int i = 0; i < OptimizationParameters.Count; i++)
				{
					SystemParameterInfo param = OptimizationParameters[i];
					totalRuns *= param.NumSteps;
					double currentValue;
					if (param.NumSteps > 1)
					{
						double stepSize = (double)((Decimal)(param.High - param.Low) / (Decimal)(param.NumSteps - 1));
						currentValue = param.Low + optProgress[i] * stepSize;
					}
					else
					{
						currentValue = param.Low;
					}
					paramDict[param.Name] = currentValue;
				}

				string overallProgressText = string.Format("Optimization run {0} of {1}", runNo, totalRuns);

				double overallProgress = ((double)runNo - 1) / totalRuns;
				UpdateProgress(overallProgressText, overallProgress, "Initializing...", 0);

				runSettings.SystemParameters = paramDict.ToList();
				OptimizationResult result = RunSystem(runSettings, (currentItem, totalItems, currentTime) =>
				{
					double currentRunProgress = (double)currentItem / totalItems;
					string currentRunProgressText = null;
					if (currentTime != DateTime.MinValue)
					{
						CultureInfo culture = BarUtils.GetCurrencyCulture(runSettings.AccountCurrency);
						currentRunProgressText = "Current Run Progress: " + currentTime.ToString(culture.DateTimeFormat);
					}
					UpdateProgress(null, overallProgress + (currentRunProgress / totalRuns), currentRunProgressText, currentRunProgress);
				});

				if (!runSettings.SaveOptimizationResults)
				{
					File.Delete(result.ResultsFile);
				}

				optimizationResults.Add(result);

				bool done = true;
				for (int i = 0; i < optProgress.Length; i++)
				{
					optProgress[i]++;
					if (optProgress[i] < OptimizationParameters[i].NumSteps)
					{
						done = false;
						break;
					}
					else
					{
						optProgress[i] = 0;
					}
				}

				if (done)
				{
					break;
				}
			}

			//	Remove parameters that weren't optimized from the list so that they don't show up as columns in the optimization results
			HashSet<string> unoptimizedParameters = new HashSet<string>(OptimizationParameters.Where(p => p.NumSteps <= 1).Select(p => p.Name));
			foreach (var result in optimizationResults)
			{
				result.ParameterValues = result.ParameterValues.Where(p => !unoptimizedParameters.Contains(p.Key)).ToList();
			}


			return optimizationResults;
		}
	}
}
