using Accord.Controls;
using Deedle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SPAMFiltering
{
    public class DataVisualizer
    {
        public static void HamVsSpamBarChart(int hamEmailCount, int spamEmailCount)
        {
            var barChart = DataBarBox.Show(
                new string[] {"Ham", "Spam"},
                new double[] {hamEmailCount, spamEmailCount}
            );

            barChart.SetTitle("Ham vs. Spam in sample set");
        }

        public static void Top10HamTermsChart(
            IEnumerable<string> topHamTerms, 
            IEnumerable<double> topHamTermsProportions,
            Series<string, double> spamTermProportions)
        {
            var hamBarChart = DataBarBox.Show(
                topHamTerms.ToArray(),
                new double[][] {
                    topHamTermsProportions.ToArray(),
                    spamTermProportions.GetItems(topHamTerms).Values.ToArray()
                }
            );
            hamBarChart.SetTitle("Top 10 Terms in Ham Emails (blue: HAM, red: SPAM)");
            System.Threading.Thread.Sleep(3000);
            hamBarChart.Invoke(
                new Action(() =>
                {
                    hamBarChart.Size = new System.Drawing.Size(5000, 1500);
                })
            );
        }

        
        public static void Top10SpamTermsChart(
            IEnumerable<string> topSpamTerms,
            IEnumerable<double> topSpamTermsProportions,
            Series<string, double> hamTermProportions
            )
        {
            var spamBarChart = DataBarBox.Show(
                topSpamTerms.ToArray(),
                new double[][] {
                    hamTermProportions.GetItems(topSpamTerms).Values.ToArray(),
                    topSpamTermsProportions.ToArray()
                }
            );
            spamBarChart.SetTitle("Top 10 Terms in Spam Emails (blue: HAM, red: SPAM)");
            System.Threading.Thread.Sleep(3000);
            spamBarChart.Invoke(
                new Action(() =>
                {
                    spamBarChart.Size = new System.Drawing.Size(5000, 1500);
                })
            );
        }
    }
}