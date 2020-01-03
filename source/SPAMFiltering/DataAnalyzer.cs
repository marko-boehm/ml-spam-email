﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Accord.Statistics.Kernels;
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

        public Series<string, double> HamTermFrequencies()
        {
            var freq = mailSubjectWords.Where(x => x.Value.GetAs<int>("is_ham") == 1)
                .Sum()
                .Sort()
                .Reversed.Where(x => x.Key != "is_ham");

            return freq;
        }

        public Series<string, double> HamTermProportions()
        {
            return HamTermFrequencies() / HamEmailCount();
        }
        
        public int SpamEmailCount()
        {
            return mailSubjectWords.RowCount - HamEmailCount();
        }

        public Series<string, double> SpamTermFrequencies()
        {
            var freq = mailSubjectWords.Where(x => x.Value.GetAs<int>("is_ham") == 0)
                .Sum()
                .Sort()
                .Reversed;

            return freq;
        }

        public Series<string, double> SpamTermProportions()
        {
            return SpamTermFrequencies() / SpamEmailCount();
        }
        
        
        
        
        
        
        
    }
}