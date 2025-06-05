using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;

namespace MBS.Web.Portal.Services
{
	/// <summary>
	/// Summary description for RTFCreator
	/// </summary>
	public class RTFCreator
	{
		private StringBuilder myRTFBuilder = new StringBuilder();
		private string myClaimsContent = string.Empty;
		private readonly int myServiceRecordCellNum = 14;
		private int myNumCommentRecord = 0;
		private int myNumServiceRecord = 0;
		private readonly string myEndTag = "<name_ended12345>".ToUpper();

		public RTFCreator(string myClaimsInContent)
		{
			myClaimsContent = myClaimsInContent;
			myNumCommentRecord = 0;
			myRTFBuilder.Remove(0, myRTFBuilder.Length);
		}

		private void RTF_BREAK()
		{
			myRTFBuilder.Append(@"\par").Append(System.Environment.NewLine);
		}

		private void RTF_ADD_ROW_HEADER()
		{
			myRTFBuilder.Append(@"\row\trowd\trgaph108\trleft-108\trpaddl108\trpaddr108\trpaddfl3\trpaddfr3 \n");
		}    

		private void RTF_ADD_CELL_HEADER()
		{
			myRTFBuilder.Append(@"\cellx843\cellx1795\cellx2746\cellx3697\cellx4648\cellx5599\cellx6550\cellx7501\cellx8452\cellx9406\cellx10360\cellx11314\cellx12165\cellx13071\pard\intbl\nowidctlpar ");
		}

		private void RTF_ADD_CELL_COMMENT_HEADER()
		{
			myRTFBuilder.Append(@"\cellx843\cellx13071\pard\intbl\nowidctlpar ");
		}

		private void CreateServiceRecordTable(string[] myRecords)
		{
			//table header
			myRTFBuilder.Append(@"\trowd\trgaph108\trleft-108\trpaddl108\trpaddr108\trpaddfl3\trpaddfr3 \n");
			RTF_ADD_CELL_HEADER();

			string[] myColNames = this.ReturnColNames().Split('|');

			foreach(string myColName in myColNames)
			{
				myRTFBuilder.Append(myColName).Append(@"\cell ");
			}

			string[] mySplitRecords = null;
			for(int k = 1; k < myRecords.Length - 1; k++)
			{
				RTF_ADD_ROW_HEADER();

				string myTemp = myRecords[k];

				if (myTemp.StartsWith("60"))
				{
					RTF_ADD_CELL_COMMENT_HEADER();
                    myNumCommentRecord++;
					mySplitRecords = GetSplittedCommentRecord(myTemp);
				}
				else
				{
					RTF_ADD_CELL_HEADER();   
					myNumServiceRecord++;
					if (myTemp.StartsWith("50"))
						mySplitRecords = GetSplittedRecords(myTemp);
					else
						mySplitRecords = GetSplittedOutOfProvince(myTemp);
				}

				foreach(string myCell in mySplitRecords)
				{
					myRTFBuilder.Append(myCell).Append(@"\cell ");
				}            
			}

			myRTFBuilder.Append(@"\row\pard\sa200\sl276\slmult1");
			RTF_BREAK();
		}
    
		private string[] GetSplittedCommentRecord(string mySingleRecord)
		{
			string[] myRecords = new string[2];

			myRecords[0] = mySingleRecord.Substring(12, 9);
			myRecords[1] = mySingleRecord.Substring(21, 77).Trim();

			return myRecords;

		}

		private string[] GetSplittedOutOfProvince(string mySingleRecord)
		{
			string[] myRecords = new string[myServiceRecordCellNum];

			string myTemp = mySingleRecord.Substring(23, mySingleRecord.IndexOf(myEndTag) - 23);

			mySingleRecord = mySingleRecord.Substring(0, 23) + " ".PadRight(27, ' ') + mySingleRecord.Substring(mySingleRecord.IndexOf(myEndTag) + myEndTag.Length);

			myRecords[0] = mySingleRecord.Substring(51, 12);
			myRecords[3] = myTemp;
			myRecords[4] = mySingleRecord.Substring(6, 5);
			myRecords[5] = mySingleRecord.Substring(11, 1);      

			return myRecords;
		}

		private string[] GetSplittedRecords(string mySingleRecord)
		{
			string[] myRecords = new string[myServiceRecordCellNum];
			myRecords[0] = mySingleRecord.Substring(12, 9);
			myRecords[1] = mySingleRecord.Substring(25, 1);
			myRecords[2] = mySingleRecord.Substring(21, 4);
			myRecords[3] = mySingleRecord.Substring(26, mySingleRecord.IndexOf(myEndTag) - 26);

			mySingleRecord = mySingleRecord.Substring(0, 26) + " ".PadRight(25, ' ') + mySingleRecord.Substring(mySingleRecord.IndexOf(myEndTag) + myEndTag.Length);

			myRecords[4] = mySingleRecord.Substring(6, 5);
			myRecords[5] = mySingleRecord.Substring(11, 1);
			myRecords[6] = mySingleRecord.Substring(51, 3);
			myRecords[7] = mySingleRecord.Substring(58, 6);
			myRecords[8] = string.Empty;
			myRecords[9] = mySingleRecord.Substring(64, 2);
			myRecords[10] = mySingleRecord.Substring(67, 4);

			string myAmount = mySingleRecord.Substring(71, 6).TrimStart('0');

			myRecords[11] = myAmount.Substring(0, myAmount.Length - 2) + "." + myAmount.Substring(myAmount.Length - 2);
			myRecords[12] = mySingleRecord.Substring(54, 4);
			myRecords[13] = mySingleRecord.Substring(66, 1);

			return myRecords;
		}

		private void CreateHeader(string myHeaderRecord)
		{   
			myRTFBuilder.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang4105\deflangfe4105{\fonttbl{\f0\fswiss\fprq2\fcharset0 Calibri;}{\f1\froman\fprq2\fcharset0 Times New Roman;}}\r\n");
			myRTFBuilder.Append(@"\landscape").Append("\r\n").Append(@"\paperw15840\paperh12240\margl720\margr720\margt720\margb720").Append("\r\n");
			myRTFBuilder.Append(@"{\*\generator Msftedit 5.41.21.2509;}\viewkind4\uc1\pard\nowidctlpar\sa200\sl276\slmult1\f0\fs16 ");
			myRTFBuilder.Append("SASKATCHEWAN MEDICAL CLINIC                  ").Append(DateTime.UtcNow.AddHours(-6).ToString("dd-MMM-yy"));
			RTF_BREAK();       
			myRTFBuilder.Append(myHeaderRecord.Substring(15, myHeaderRecord.IndexOf(myEndTag) - 15)).Append("          ");

			myRTFBuilder.Append("PRACTITIONER # ").Append(myHeaderRecord.Substring(2, 4));
			myRTFBuilder.Append("      CLINIC # ").Append(myHeaderRecord.Substring(12, 3));
			myRTFBuilder.Append("                   MODE ").Append(1);
			RTF_BREAK();
		}

		private string ReturnColNames()
		{
			return "PATIENT|SEX|BIRTH|SURNAME,GIVEN|CLAIM NUM|SEQ NUM|DIAG|SERVICE START DATE|SERVICE END DATE|VISITS/UNITS|FEE CODE|AMOUNT|REFERRING DOCTOR|LOCATION";
		}

		private void CreateFooter(string myTrailerRecord)
		{
			myRTFBuilder.Append("TRAILER INFORMATION");
			RTF_BREAK();
			myRTFBuilder.Append("PRACTITIONER # ").Append(myTrailerRecord.Substring(2, 4)).Append("      ");
			myRTFBuilder.Append("CLINIC # 000");
			RTF_BREAK();
			myRTFBuilder.Append("NO OF SERVICE RECORDS = ").Append(myNumServiceRecord);
			RTF_BREAK();
			myRTFBuilder.Append("NO OF COMMENT RECORDS = ").Append(myNumCommentRecord);
			RTF_BREAK();
			myRTFBuilder.Append("TOTAL NO OF RECORDS = ").Append(myTrailerRecord.Substring(12, 5));
			RTF_BREAK();
			string myAmount = myTrailerRecord.Substring(22, 7).TrimStart('0');
			myRTFBuilder.Append("TOTAL AMOUNT CLAIMED = $").Append(myAmount.Substring(0, myAmount.Length - 2)).Append(".").Append(myAmount.Substring(myAmount.Length - 2));
			RTF_BREAK();
			RTF_BREAK();
			myRTFBuilder.Append("I CERTIFY, TO THE BEST OF MY KNOWLEDGE, THAT THE CLAIMS");
			RTF_BREAK();
			myRTFBuilder.Append("LISTED ABOVE ARE A TRUE AND ACCURATE ACCOUNTING OF SERVICES");
			RTF_BREAK();
			myRTFBuilder.Append("PROVIDED TO THE PATIENTS INDICATED, THAT THESE CLAIMS HAVE"); 
			RTF_BREAK();
			myRTFBuilder.Append("NOT BEEN PREVIOUSLY PAID"); 
			RTF_BREAK();
			myRTFBuilder.Append(@"AUTHORIZED SIGNATURE    ____________________________________________________________\f1\fs22"); 
			RTF_BREAK();
			myRTFBuilder.Append("}");
        
		}

		public string GetRTFReport()
		{
			string[] myClaimsInRecords = myClaimsContent.Split('\n');

			CreateHeader(myClaimsInRecords[0].Trim());

			myClaimsInRecords[0] = ReturnColNames();

			CreateServiceRecordTable(myClaimsInRecords);
        
			CreateFooter(myClaimsInRecords[myClaimsInRecords.Length - 1].Trim());

			return myRTFBuilder.ToString();
		}

	}
}