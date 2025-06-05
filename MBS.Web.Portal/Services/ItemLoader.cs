using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MBS.Web.Portal.Models;
using MBS.Web.Portal.Constants;

namespace MBS.Web.Portal.Services
{
	public class ItemLoader
	{
		public static List<Fee> LoadFee(string myFilePath)
		{
			List<Fee> myFees = new List<Fee>();

			string[] myRows = GetContent(myFilePath);

			if (myRows != null)
			{
				foreach (string myRow in myRows)
				{
					if (myRow.Length >= 10)
					{
						myFees.Add(new Fee(myRow.Substring(0, 4).Trim().TrimStart('0'), Convert.ToSingle(myRow.Substring(6, 7).Trim())));
					}
				}
			}
			return myFees.OrderBy(f => f.myCode).ToList();
		}

		public static List<ICD> LoadICD(string myFilePath)
		{
			List<ICD> myICDs = new List<ICD>();

			string[] myRows = GetContent(myFilePath);

			if (myRows != null)
			{
				foreach (string myRow in myRows)
				{
					if (myRow.Length >= 3)
					{
						myICDs.Add(new ICD(myRow.Substring(0, 3).ToUpper().TrimStart('0'), myRow.Substring(3).Trim()));
					}
				}
			}
			return myICDs.OrderBy(f => f.myCode).ToList();
		}

		public static List<RefDoc> LoadRefDoc(string myFilePath)
		{
			List<RefDoc> myRefDocs = new List<RefDoc>();
			string myNumber;
			string myLastName;
			string myFirstName;
			string myCity;
			string[] myRows = GetContent(myFilePath);

			if (myRows != null)
			{
				foreach (string myRow in myRows)
				{
					if (myRow.Length > 10)
					{
						int myIndex = myRow.IndexOf(',');
						myNumber = myRow.Substring(0, 4);
						if (myIndex > 0)
						{
							myLastName = myRow.Substring(4, (myIndex - 4));

							if ((myIndex + 2) < 23)
								myFirstName = myRow.Substring((myIndex + 2), (23 - (myIndex + 2)));
							else
								myFirstName = string.Empty;
							myCity = myRow.Substring(23);

						}
						else
						{
							myLastName = myRow.Substring(4);
							myFirstName = string.Empty;
							myCity = string.Empty;
						}
						myRefDocs.Add(new RefDoc(int.Parse(myNumber.Trim()), myFirstName.Trim(), myLastName.Trim(), myCity.Trim()));
					}
				}
			}
			return myRefDocs.OrderBy(f => f.myName).ToList();
		}

		public static List<ExplainCode> LoadExplainCode(string myFilePath)
		{
			List<ExplainCode> myExplains = new List<ExplainCode>();

			string[] myRows = GetContent(myFilePath);

			if (myRows != null)
			{
				foreach (string myRow in myRows)
				{
					myExplains.Add(new ExplainCode(myRow.Substring(0, 2), myRow.Substring(3).Trim()));
				}
			}
			return myExplains.OrderBy(f => f.myCode).ToList();
		}

        public static List<string> LoadRunSchedule(string myFilePath)
        {
            var mySchedules = new List<string>();

            string[] myRows = GetContent(myFilePath);

            if (myRows != null)
            {
                foreach (string myRow in myRows)
                {
                    mySchedules.Add(myRow.Trim());
                }
            }

            return mySchedules;
        }

        public static List<string> LoadCareCodes(string myFilePath)
        {
            var myCodes = new List<string>();

            string[] myRows = GetContent(myFilePath);

            if (myRows != null)
            {
                foreach (string myRow in myRows)
                {
                    myCodes.Add(myRow.Trim());
                }
            }

            return myCodes;
        }

        public static List<Fee> LoadWCBFee(string myFilePath)
        {
            List<Fee> myFees = new List<Fee>();

            string[] myRows = GetContent(myFilePath);

            if (myRows != null)
            {
                foreach (string myRow in myRows)
                {
                    var temp = myRow.Trim().Split(',');
                    myFees.Add(new Fee(temp[0] + " - WCB", Convert.ToSingle(temp[1])));
                }
            }

            return myFees.OrderBy(f => f.myCode).ToList();
        }

		private static string[] GetContent(string myFilePath)
		{
			string[] myRows = null;
			if (File.Exists(myFilePath))
			{
				//Create the TextReader object
				using (TextReader myReader = new StreamReader(myFilePath))
				{
					string myContent = myReader.ReadToEnd();

					myRows = myContent.Split('\n');
				}
			}
			return myRows;
		}
	}
}