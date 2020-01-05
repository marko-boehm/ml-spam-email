using System;
using System.Linq;
using Accord.MachineLearning;
using Accord.MachineLearning.Bayes;
using Accord.MachineLearning.Performance;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions.Univariate;
using Deedle;

namespace SPAMFiltering
{
    public class Classifier
    {
        private Frame<string, string> SpamTermFrequencies;
        private Frame<int, string> TransformedMailData;
        private Frame<int, string> TransformedMailSubjects;

        public double[][] InputVariables { get; private set; }
        public int[] OutputVariables { get; private set; }
        public Series<int, int> TargetVariables { get; private set; }
        public  CrossValidationResult<NaiveBayes<BernoulliDistribution>, double[], int> Result { get; private set; }

        public Classifier(
            Frame<string, string> spamTermFrequencies,
            Frame<int, string> transformedMailData,
            Frame<int, string> transformedMailSubjects)
        {
            SpamTermFrequencies = spamTermFrequencies;
            TransformedMailData = transformedMailData;
            TransformedMailSubjects = transformedMailSubjects;
        }
        
        private string[] ReduceOverfitting(int minOccurences = 1)
        {
            string[] wordFeatures = SpamTermFrequencies
                .Where(x => x.Value.GetAs<int>("num_occurences") >= minOccurences)
                .RowKeys
                .ToArray();

            return wordFeatures;
        }

        private void CalcInputAndOutputVariables(int minOccurences = 1)
        {
            // subtracting "is_ham" values from 1 to encode this target variable with 1 for spam emails 
            TargetVariables = 1 - TransformedMailData.GetColumn<int>("is_ham");

            // Create input and output variables from data frames, so that we can use them for Accord.NET MachineLearning models
            InputVariables = TransformedMailSubjects.Columns[ReduceOverfitting(minOccurences)]
                .Rows.Select(x => Array.ConvertAll<object, double>(x.Value.ValuesAll.ToArray(), o => Convert.ToDouble(o)))
                .ValuesAll.ToArray();
            
            OutputVariables = TargetVariables.Values.ToArray();
        }

        public void ClassifyData(int numOfFolds = 3, int minOccurences = 1)
        {
            CalcInputAndOutputVariables(minOccurences);

            var cvNaiveBayesClassifier = CrossValidation.Create<NaiveBayes<BernoulliDistribution>, NaiveBayesLearning<BernoulliDistribution>, double[], int>(
                // number of folds
                k: numOfFolds,
                // Naive Bayes Classifier with Binomial Distribution
                learner: (p) => new NaiveBayesLearning<BernoulliDistribution>(),
                // Using Zero-One Loss Function as a Cost Function
                loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                // Fitting a classifier
                fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                // Input with Features
                x: InputVariables,
                // Output
                y: OutputVariables
            );

            // Run Cross-Validation
            Result = cvNaiveBayesClassifier.Learn(InputVariables, OutputVariables);
        }

        public void PrintMatrix()
        {
            Console.WriteLine("{0} spams vs. {1} hams", TargetVariables.NumSum(), (TargetVariables.KeyCount - TargetVariables.NumSum()));
            
            Console.WriteLine("\n---- Confusion Matrix ----");
            GeneralConfusionMatrix gcm = Result.ToConfusionMatrix(InputVariables, OutputVariables);
            Console.WriteLine("");
            Console.Write("\t\tActual 0\t\tActual 1\n");
            for (int i = 0; i < gcm.Matrix.GetLength(0); i++)
            {
                Console.Write("Pred {0} :\t", i);
                for (int j = 0; j < gcm.Matrix.GetLength(1); j++)
                {
                    Console.Write(gcm.Matrix[i, j] + "\t\t\t");
                }
                Console.WriteLine();
            }

            Console.WriteLine("\n---- Sample Size ----");
            Console.WriteLine("# samples: {0}, # inputs: {1}, # outputs: {2}", Result.NumberOfSamples, Result.NumberOfInputs, Result.NumberOfOutputs);
            Console.WriteLine("training error: {0}", Result.Training.Mean);
            Console.WriteLine("validation error: {0}\n", Result.Validation.Mean);

            Console.WriteLine("\n---- Calculating Accuracy, Precision, Recall ----");

            float truePositive = (float)gcm.Matrix[1, 1];
            float trueNegative = (float)gcm.Matrix[0, 0];
            float falsePositive = (float)gcm.Matrix[1, 0];
            float falseNegative = (float)gcm.Matrix[0, 1];

            // Accuracy
            Console.WriteLine(
                "Accuracy: {0}",
                (truePositive + trueNegative) / Result.NumberOfSamples
            );
            // True-Positive / (True-Positive + False-Positive)
            Console.WriteLine("Precision: {0}", (truePositive / (truePositive + falsePositive)));
            // True-Positive / (True-Positive + False-Negative)
            Console.WriteLine("Recall: {0}", (truePositive / (truePositive + falseNegative)));
        }
    }
}