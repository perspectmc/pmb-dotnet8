using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace MBS.ApplicationInitializer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Sending Request");
            var webRequest = WebRequest.Create("http://medicalbillngs.azurewebsites.net/");
            var webResponse = webRequest.GetResponse();
            var dataStream = webResponse.GetResponseStream();

            // Open the stream using a StreamReader for easy access.
            var reader = new StreamReader(dataStream);

            // Read the content.
            string responseFromServer = reader.ReadToEnd();
                        
            reader.Close();

            webResponse.Close();
        }
    }
}


//test
//test Jun 12 1921
