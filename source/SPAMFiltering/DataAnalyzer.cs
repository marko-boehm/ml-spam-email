using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Deedle;

namespace SPAMFiltering
{
    public class DataAnalyzer
    {
        private Frame<int, string> mailSubjectWords;
        private Frame<int, string> transformedMails { get; set; }
       

        public DataAnalyzer(Frame<int, string> transformedMails)
        {
           this.transformedMails = transformedMails;
        }
       
        private Frame<int, string> CreateWordFrame(Series<int, string> rows)
        {
            var wordsByRows = rows.GetAllValues()
                .Select((x, i) =>
                {
                    var sb = new SeriesBuilder<string, int>();

                    ISet<string> words = new HashSet<string>(
                        Regex.Matches(x.Value, "[a-zA-Z]+('(s|d|t|ve|m))?")
                            .Cast<Match>()
                            .Select(y => y.Value.ToLower())
                            .ToArray()
                    );

                    // Encode words appeared in each row with 1
                    foreach (string w in words)
                    {
                        sb.Add(w, 1);
                    }

                    return KeyValue.Create(i, sb.Series);
                });

            // Create a data frame from the rows we just created
            // And encode missing values with 0
            var wordFrame = Frame.FromRows(wordsByRows)
                .FillMissing(0);

            return wordFrame;
        }

        public Frame<int, string> TransformMailSubjectsToWords()
        {
            mailSubjectWords =  CreateWordFrame(transformedMails.GetColumn<string>("subject"));
            mailSubjectWords.AddColumn("is_ham", transformedMails.GetColumn<int>("is_ham"));
            
            return mailSubjectWords;
        }

        public int HamEmailCount()
        {
            return transformedMails.GetColumn<int>("is_ham").NumSum();
        }

        public Series<string, double> HamTermFrequencies(ISet<string> stopWords)
        {
            var freq = mailSubjectWords.Where(x => x.Value.GetAs<int>("is_ham") == 1)
                .Sum()
                .Sort()
                .Reversed.Where(x => x.Key != "is_ham");

            return FilterOutStopWords(freq, stopWords);
        }

        public Series<string, double> HamTermProportions(ISet<string> stopWords)
        {
            return HamTermFrequencies(stopWords) / HamEmailCount();
        }
        
        public int SpamEmailCount()
        {
            return mailSubjectWords.RowCount - HamEmailCount();
        }

        public Series<string, double> SpamTermFrequencies(ISet<string> stopWords)
        {
            var freq = mailSubjectWords.Where(x => x.Value.GetAs<int>("is_ham") == 0)
                .Sum()
                .Sort()
                .Reversed;

            return FilterOutStopWords(freq, stopWords);
        }

        public Series<string, double> SpamTermProportions(ISet<string> stopWords)
         => SpamTermFrequencies(stopWords) / SpamEmailCount();


        private Series<string, double> FilterOutStopWords(Series<string, double> series, ISet<string> stopWords)
        {
            if (stopWords != null) {
                return series.Where(x => !stopWords.Contains(x.Key));
            }

            return series;
        }

        public Frame<string, string> ConvertSpamFrequenciesToFrame(Series<string, double> spamFrequSeries)
        {
            var dataFrame = Frame.FromRowKeys(spamFrequSeries.Keys);
            dataFrame.AddColumn("num_occurences", spamFrequSeries.Values);

            return dataFrame;
        }
        
        
        
        
        
        
        
        
    }
}