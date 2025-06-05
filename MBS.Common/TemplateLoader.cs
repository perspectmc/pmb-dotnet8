using System;
using System.IO;
using System.Web;

namespace MBS.Common
{
    public static class TemplateLoader
    {
        public static string LoadFile(string fileName)
        {
            var result = string.Empty;
            FileStream myStream = null;
            TextReader myTextReader = null;
            try
            {
                var myFile = HttpContext.Current.Server.MapPath("~/App_Data/" + fileName);
                myStream = new FileStream(myFile, FileMode.Open, FileAccess.Read);
                myTextReader = new StreamReader(myStream);
                result = myTextReader.ReadToEnd();                
            }  
            finally  
            {   
                if (myStream != null)                   
                {    
                    myStream.Close();
                    myStream.Dispose();

                    if (myTextReader != null)
                    {
                        myTextReader.Close();
                        myTextReader.Dispose();
                    }
                }  
            } 

            return result;
        }
    }
}
