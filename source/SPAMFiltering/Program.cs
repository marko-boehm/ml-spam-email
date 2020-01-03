using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deedle;

namespace SPAMFiltering
{
    class Program
    {
        static void Main(string[] args)
        {
            var workingDirectory = @"C:\Projects\ml-spam-email\data";//Directory.GetCurrentDirectory();
            
            Console.WriteLine("Starting Data Preparation.");
            // Prepare raw data
            var preparationFileLocation = Path.Combine(workingDirectory, "data-preparation\\transformedMails.csv");
            Frame<int, string> mailDataFrame;
            bool loadFileFromDisk = false;
            
            // Check if preparation file already exists
            if (File.Exists(preparationFileLocation)) {
                Console.Write("Preparation file found. Load this file (Y/N)?");
                var keyInfo = Console.ReadLine();

                if (keyInfo == "Y" || keyInfo == "y") {
                    loadFileFromDisk = true;
                }
            }

            if (loadFileFromDisk) {
                mailDataFrame = FrameFileManager.ReadFromCsv(
                    preparationFileLocation,
                    hasHeaders: true,
                    inferTypes: false,
                    schema: "int,string,string,int"
                ).IndexRows<int>("id").SortRowsByKey();
            }
            else {
                var mailPreparator = new EmailPreparator()
                {
                    RawDataPath = Path.Combine(workingDirectory, "raw-data"),
                };
                mailDataFrame = mailPreparator.ExtractMailContentToFrame();

                // Make frame persistent
                FrameFileManager.SaveToCsv(mailDataFrame, preparationFileLocation);
            }

            Console.WriteLine("Data Preparation step done!"); 
            
            
            // Analyse data
            Console.WriteLine("Start Data Anlayzer ...");
            var dataAnalyzer = new DataAnalyzer(mailDataFrame);
            var transformedMailSubjects = dataAnalyzer.TransformMailSubjectsToWords();
            Console.WriteLine("* Subject Word Transformation: Row Count {0}, Column Count {1}", transformedMailSubjects.RowCount, transformedMailSubjects.ColumnCount);
            
            FrameFileManager.SaveToCsv(transformedMailSubjects, Path.Combine(workingDirectory, "data-preparation\\subjectWordFrame-alphaonly.csv"));

            Console.WriteLine("* Starting analyzing words ...");
            // Look at Top 10 terms that appear in Ham vs. Spam emails
            var topN = 10;
            // Load stop word list
            ISet<string> stopWords = new HashSet<string>(File.ReadLines(Path.Combine(workingDirectory, "data-preparation\\stopwords.txt")));
            
            var hamTermFrequencies = dataAnalyzer.HamTermFrequencies(stopWords);
            var hamTermProportions = dataAnalyzer.HamTermProportions(stopWords);
            var topHamTerms = hamTermProportions.Keys.Take(topN);
            var topHamTermsProportions = hamTermProportions.Values.Take(topN);
            var hamEmailCount = dataAnalyzer.HamEmailCount();

            var spamTermFrequencies = dataAnalyzer.SpamTermFrequencies(stopWords);
            var spamTermProportions = dataAnalyzer.SpamTermProportions(stopWords);
            var topSpamTerms = spamTermProportions.Keys.Take(topN);
            var topSpamTermsProportions = spamTermProportions.Values.Take(topN);
            var spamEmailCount = dataAnalyzer.SpamEmailCount();
            
            Console.WriteLine("* Save term frequencies to disk.");
            // Save frequencies to disk
            FrameFileManager.SaveToCsv(
                hamTermFrequencies.Keys.Zip(hamTermFrequencies.Values, (a, b) => $"{a},{b}"),
                Path.Combine(workingDirectory,  "data-preparation\\ham-frequencies.csv")
                );

            FrameFileManager.SaveToCsv(
                spamTermFrequencies.Keys.Zip(spamTermFrequencies.Values, (a, b) => $"{a},{b}"),
                Path.Combine(workingDirectory, "data-preparation\\spam-frequencies.csv")
                );

            // Show visuals
            DataVisualizer.HamVsSpamBarChart(hamEmailCount, spamEmailCount);
            DataVisualizer.Top10HamTermsChart(topHamTerms, topHamTermsProportions, spamTermProportions);
            DataVisualizer.Top10SpamTermsChart(topSpamTerms, topSpamTermsProportions, hamTermProportions);
            
            Console.WriteLine("Data Analyzing step done!");
            Console.ReadKey();
        }
    }
}
