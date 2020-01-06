using System;
using System.Linq;
using Accord.MachineLearning;
using Accord.MachineLearning.Bayes;
using Accord.MachineLearning.Performance;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using Deedle;

namespace SPAMFiltering
{
    public class Classifier<TModel> where TModel : class, ITransform<double[], int>
    {
        private readonly Frame<string, string> _spamTermFrequencies;
        private readonly Frame<int, string> _transformedMailData;
        private readonly Frame<int, string> _transformedMailSubjects;
        private readonly Type _algorithmType;

        public double[][] InputVariables { get; private set; }
        public int[] OutputVariables { get; private set; }
        public Series<int, int> TargetVariables { get; private set; }
        public  CrossValidationResult<TModel, double[], int> Result { get; private set; }
        

        public Classifier (
            Frame<string, string> spamTermFrequencies,
            Frame<int, string> transformedMailData,
            Frame<int, string> transformedMailSubjects)
        {
            _spamTermFrequencies = spamTermFrequencies;
            _transformedMailData = transformedMailData;
            _transformedMailSubjects = transformedMailSubjects;
            _algorithmType = typeof(TModel);
        }
        
        private string[] ReduceOverfitting(int minOccurences = 1)
        {
            string[] wordFeatures = _spamTermFrequencies
                .Where(x => x.Value.GetAs<int>("num_occurences") >= minOccurences)
                .RowKeys
                .ToArray();

            return wordFeatures;
        }

        private void CalcInputAndOutputVariables(int minOccurences = 1)
        {
            // subtracting "is_ham" values from 1 to encode this target variable with 1 for spam emails 
            TargetVariables = 1 - _transformedMailData.GetColumn<int>("is_ham");

            // Create input and output variables from data frames, so that we can use them for Accord.NET MachineLearning models
            InputVariables = _transformedMailSubjects.Columns[ReduceOverfitting(minOccurences)]
                .Rows.Select(x => Array.ConvertAll<object, double>(x.Value.ValuesAll.ToArray(), o => Convert.ToDouble(o)))
                .ValuesAll.ToArray();
            
            OutputVariables = TargetVariables.Values.ToArray();
        }

        public void ClassifyData(int numOfFolds = 3, int minOccurences = 1, int maxIterations = 100)
        {
            if (_algorithmType == typeof(LogisticRegression)) {
                ClassifyDataByLogisticRegression(numOfFolds, minOccurences, maxIterations);
            }
            else if (_algorithmType == typeof(NaiveBayes<BernoulliDistribution>)) {
                ClassifyDataByNaiveBayes(numOfFolds, minOccurences);
            }
            else {
                throw new NotImplementedException($"Type {_algorithmType} not implemented!");
            }
        }
        
        
        private void ClassifyDataByNaiveBayes(int numOfFolds = 3, int minOccurences = 1)
        {
            CalcInputAndOutputVariables(minOccurences);

            var cvNaiveBayesClassifier = CrossValidation.Create(
                k: numOfFolds,
                learner: p => new NaiveBayesLearning<BernoulliDistribution>(),
                loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                x: InputVariables,
                y: OutputVariables
            );

            // Run Cross-Validation
            Result = cvNaiveBayesClassifier.Learn(InputVariables, OutputVariables) as CrossValidationResult<TModel, double[], int>;
        }

        private void ClassifyDataByLogisticRegression(int numOfFolds = 3, int minOccurences = 1, int maxIterations = 100)
        {
            CalcInputAndOutputVariables(minOccurences);
            
            var cvLogisticRegressionClassifier = CrossValidation.Create(
                k: numOfFolds,
                learner: (p) => new IterativeReweightedLeastSquares<LogisticRegression>()
                {
                    MaxIterations = 100,
                    Regularization = 1e-6
                },
                loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                x: InputVariables,
                y: OutputVariables
            );

            // Run Cross-Validation
            Result = cvLogisticRegressionClassifier.Learn(InputVariables, OutputVariables) as CrossValidationResult<TModel, double[], int>;
        }

        public void PrintMatrix()
        {
            Console.WriteLine("{0} spams vs. {1} hams", TargetVariables.NumSum(), (TargetVariables.KeyCount - TargetVariables.NumSum()));
            
            Console.WriteLine("\n---- Confusion Matrix ----");
            GeneralConfusionMatrix gcm = Result.ToConfusionMatrix(InputVariables, OutputVariables);
            Console.WriteLine("");
            Console.Write("\t\tActual Class - 0\t\tActual Class - 1\n");
            for (int i = 0; i < gcm.Matrix.GetLength(0); i++)
            {
                Console.Write("Pred. Class - {0} :\t", i);
                for (int j = 0; j < gcm.Matrix.GetLength(1); j++)
                {
                    Console.Write(gcm.Matrix[i, j] + "\t\t\t");
                }
                Console.WriteLine();
            }

            Console.WriteLine("\n---- Sample Size ----");
            Console.WriteLine("# samples: {0}, # inputs: {1}, # outputs: {2}", Result.NumberOfSamples, Result.NumberOfInputs, Result.NumberOfOutputs);
            Console.WriteLine("Training error: {0}", Result.Training.Mean);
            Console.WriteLine("Validation error: {0}\n", Result.Validation.Mean);

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