using System.Collections.Generic;
using Deedle;

namespace SPAMFiltering
{
    public class FrameFileManager
    {
        public static void SaveToCsv(Frame<int, string> dataFrame, string location)
        {
            //ToDo: Include location validation
            dataFrame.SaveCsv(location);
        }

        public static void SaveToCsv(IEnumerable<string> data, string location)
        {
            System.IO.File.WriteAllLines(location, data);
        }

        public static Frame<int, string> ReadFromCsv(string location, string schema, bool hasHeaders = true, bool inferTypes = false)
        {
            var dataFrame = Frame.ReadCsv(location, 
                hasHeaders: hasHeaders, 
                inferTypes: inferTypes, 
                schema: schema);

            return dataFrame;
        }
        
    }
}