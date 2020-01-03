using Deedle;
using EAGetMail;
using System;
using System.IO;
using System.Linq;

namespace SPAMFiltering
{
    public class EmailPreparator
    {
        private struct EMailObject
        {
            public int id;
            public string subject;
            public string body;
        }

        public string RawDataPath { get; set; }

        private Frame<int, string> PrepareMails(string[] files)
        {
            var rows = files.AsEnumerable()
                            .Select((m, i) =>
                            { 
                                var parsedMail =  ParseMail(m);
                                parsedMail.id = i;

                                return parsedMail;
                            });

            return Frame.FromRecords(rows);
        }


        private EMailObject ParseMail(string fileName)
        {
            string EATrialVersionRemark = "(Trial Version)";
            Mail email = new Mail("TryIt");

            email.Load(fileName, false);

            // extract subject and body
            string emailSubject = email.Subject.EndsWith(EATrialVersionRemark) ?
                    email.Subject.Substring(0, email.Subject.Length - EATrialVersionRemark.Length) : 
                    email.Subject;
            string textBody = email.TextBody;

            return new EMailObject { subject = emailSubject, body = textBody };
        }

        public Frame<int, string> ExtractMailContentToFrame()
        {
            if (RawDataPath == null) {
                throw new NullReferenceException("Property 'RawDataPath' may not be null");
            }

            string[] emailFiles = Directory.GetFiles(RawDataPath, "*.eml");

            // Parse out the subject and body from the email files
            var emailDataFrame = PrepareMails(emailFiles);
            // Get the labels (spam vs. ham) for each email
            var labelDataFrame = Frame.ReadCsv(RawDataPath + "\\SPAMTrain.label", hasHeaders: false, separators: " ", schema: "int,string");
            // Add these labels to the email data frame
            emailDataFrame.AddColumn("is_ham", labelDataFrame.GetColumnAt<String>(0));

            return emailDataFrame;
        }
        
    }
}
