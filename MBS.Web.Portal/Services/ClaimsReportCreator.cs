using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using MBS.DomainModel;
using MBS.Web.Portal.Constants;
using MBS.Web.Portal.Repositories;
using MBS.Web.Portal.Models;

namespace MBS.Web.Portal.Services
{
	public class ClaimsReportCreator
	{
		private IEnumerable<ServiceRecord> myRecords = null;
		private StringBuilder myRTFBuilder = new StringBuilder();
		private ReportType myReportClaimType = ReportType.Both;		
		private double myTotalPaidAmount = 0;
        private int myTimeZoneOffset = -6;
		private string myDoctorNumber = string.Empty;
        private DateTime myStartDate;
        private DateTime myEndDate;
        private ClaimsPDFReportViewModel reportModel;

		public ClaimsReportCreator(IEnumerable<ServiceRecord> records, string doctorNumber, int timeZoneOffset, DateTime startDate, DateTime endDate, ReportType reportClaimType)
		{
			myRecords = records;
			myRTFBuilder.Remove(0, myRTFBuilder.Length);
            myDoctorNumber = doctorNumber;
			myReportClaimType = reportClaimType;
            myTimeZoneOffset = timeZoneOffset;
            myStartDate = startDate;
            myEndDate = endDate;

            reportModel = new ClaimsPDFReportViewModel();
            reportModel.DoctorNumber = doctorNumber;
            reportModel.TimeZoneOffset = timeZoneOffset;
            reportModel.StartDate = startDate;
            reportModel.EndDate = endDate;
            reportModel.RecordList = new List<SimpleServiceRecord>();
            reportModel.UnitRecordUsedList = new List<UnitCodeUsed>();
		}

		private void RTF_BREAK()
		{
			myRTFBuilder.Append(@"\par").Append("\n");
		}

		private void RTF_ADD_ROW_HEADER()
		{
			myRTFBuilder.Append(@"\trowd\trgaph108\trleft-108\trpaddl108\trpaddr108\trpaddfl3\trpaddfr3 ");
		}

		private void RTF_ADD_CELL_HEADER()
		{
			myRTFBuilder.Append(@"\cellx3084\cellx6276\cellx9468\pard\intbl ");
		}

        private void RTF_ADD_CELL_HEADER_SUB()
        {
            myRTFBuilder.Append(@"\cellx3084\cellx6276\pard\intbl ");
        }

        private void RTF_ADD_CELL_HEADER_SUMMARY()
        {
            myRTFBuilder.Append(@"\cellx3000\cellx6000\cellx7300\cellx8800\cellx10800\cellx12400\cellx14300\pard\intbl ");
        }

        private void RTF_ADD_CELL()
		{
			myRTFBuilder.Append(@"\cell ");
		}

		private void CreateClaimsReport(IEnumerable<ServiceRecord> filterRecords)
		{ 
			//table header
			RTF_ADD_ROW_HEADER();
            myRTFBuilder.Append(@"\cellx3084\cellx6276\cellx9468\pard\intbl\f1\fs22 ").Append(System.Environment.NewLine);

			string[] myColNames = "Claim Number|Date of Service|Paid Amount".Split('|');

			foreach(string myColName in myColNames)
			{
				myRTFBuilder.Append(myColName);
				RTF_ADD_CELL();
			}
            
            double sectionTotal = 0;
            foreach (var myRecord in filterRecords)
			{
                myRTFBuilder.Append(@"\row");

				RTF_ADD_ROW_HEADER();
				RTF_ADD_CELL_HEADER();

                var record = new SimpleServiceRecord();
                record.ServiceDate = myRecord.ServiceDate;
                record.ClaimNumber = myRecord.ClaimNumber;
                
				myRTFBuilder.Append(myRecord.ClaimNumber);
				RTF_ADD_CELL();

				myRTFBuilder.Append(myRecord.ServiceDate.ToString("dd/MM/yyyy"));
				RTF_ADD_CELL();

                double myTotal = 0;
                if (myRecord.PaidClaimId.HasValue)
                {
                    myTotal = myRecord.PaidAmount;
                }
                else if (myRecord.RejectedClaimId.HasValue)
                {
                    myTotal = myRecord.ClaimAmount;
                }
                    				
                myRTFBuilder.Append(string.Format("${0:F2}", myTotal));
				RTF_ADD_CELL();

                record.PaidAmount = myTotal;
                record.WCBStatus = myRecord.WCBFaxStatus;

                reportModel.RecordList.Add(record);

                sectionTotal += myTotal;
			}
                        
			myRTFBuilder.Append(@"\row\pard\sa200\sl276\slmult1\par").Append(System.Environment.NewLine);
            
            myRTFBuilder.Append("TOTAL NO OF CLAIMS = ").Append(filterRecords.Count());
            RTF_BREAK();

            myRTFBuilder.Append("TOTAL PAID AMOUNT = $").Append(string.Format("{0:F2}", sectionTotal));
            RTF_BREAK();
		}

        private void CreateUnitRecordsSummaryReport(IEnumerable<ServiceRecord> filterRecords)
        {
            myRTFBuilder.Append("\n").Append("SERVICE DATE: ").Append(myStartDate.ToString("dd/MM/yyyy")).Append(" TO ").Append(myEndDate.ToString("dd/MM/yyyy"));
            RTF_BREAK();

            //table header
            RTF_ADD_ROW_HEADER();
            myRTFBuilder.Append(@"\cellx3000\cellx6000\cellx7300\cellx8800\cellx10800\cellx12400\cellx14300\pard\intbl\f1\fs22 ").Append(System.Environment.NewLine);

            string[] myColNames = { "First Name", "Last Name", "Birthdate", "Service Date", "Submission Date", "Service Code", "Total Services" };

            foreach (string myColName in myColNames)
            {
                myRTFBuilder.Append(myColName.ToUpper());
                RTF_ADD_CELL();
            }

            foreach (var myRecord in filterRecords)
            {
                myRecord.PatientFirstName = myRecord.PatientFirstName.ToUpper();
                myRecord.PatientLastName = myRecord.PatientLastName.ToUpper();
            }

            foreach (var myRecord in filterRecords.OrderBy(x => x.ServiceDate).ThenBy(x => x.PatientLastName))
            {
                foreach (var myUnitRecord in myRecord.UnitRecord)
                {
                    var record = new SimpleServiceRecord();
                    record.FirstName = myRecord.PatientFirstName;
                    record.LastName = myRecord.PatientLastName;
                    record.BirthDate = myRecord.DateOfBirth;
                    record.ServiceDate = myRecord.ServiceDate;
                    record.UnitCode = myUnitRecord.UnitCode;
                    record.UnitNumber = myUnitRecord.UnitNumber;


                    myRTFBuilder.Append(@"\row");

                    RTF_ADD_ROW_HEADER();
                    RTF_ADD_CELL_HEADER_SUMMARY();

                    myRTFBuilder.Append(record.FirstName);
                    RTF_ADD_CELL();

                    myRTFBuilder.Append(record.LastName);
                    RTF_ADD_CELL();

                    myRTFBuilder.Append(record.BirthDate.ToString("yyMM"));
                    RTF_ADD_CELL();

                    myRTFBuilder.Append(record.ServiceDate.ToString("dd/MM/yyyy"));
                    RTF_ADD_CELL();

                    if (myRecord.ClaimsIn != null && myRecord.ClaimsIn.DownloadDate.HasValue)
                    {
                        record.SubmissionDate = myRecord.ClaimsIn.DownloadDate.Value.AddHours(myTimeZoneOffset).ToString("dd/MM/yyyy");
                        myRTFBuilder.Append(record.SubmissionDate);
                    }

                    RTF_ADD_CELL();

                    myRTFBuilder.Append(myUnitRecord.UnitCode);
                    RTF_ADD_CELL();

                    myRTFBuilder.Append(myUnitRecord.UnitNumber);
                    RTF_ADD_CELL();

                    reportModel.RecordList.Add(record);
                }
            }

            myRTFBuilder.Append(@"\row\pard\sa200\sl276\slmult1\par").Append(System.Environment.NewLine);
            RTF_BREAK();

            //table header
            RTF_ADD_ROW_HEADER();
            myRTFBuilder.Append(@"\cellx3084\cellx6276\pard\intbl\f1\fs22 ").Append(System.Environment.NewLine);

            myRTFBuilder.Append("SERVICE CODE");
            RTF_ADD_CELL();

            myRTFBuilder.Append("# OF USED");
            RTF_ADD_CELL();

            reportModel.UnitRecordUsedList = filterRecords.SelectMany(x => x.UnitRecord).GroupBy(x => x.UnitCode).Select(x => new UnitCodeUsed { UnitCode = x.Key.PadLeft(4, '0'), TotalUnitNumber = x.Select(y => y.UnitNumber).Sum() }).OrderBy(x => x.UnitCode).ToList();

            foreach (var groupCode in reportModel.UnitRecordUsedList)
            {
                myRTFBuilder.Append(@"\row");

                RTF_ADD_ROW_HEADER();
                RTF_ADD_CELL_HEADER_SUB();

                myRTFBuilder.Append(groupCode.UnitCode.TrimStart('0'));
                RTF_ADD_CELL();

                myRTFBuilder.Append(groupCode.TotalUnitNumber);
                RTF_ADD_CELL();
            }

            myRTFBuilder.Append(@"\row\pard\sa200\sl276\slmult1\par").Append(System.Environment.NewLine);

            RTF_BREAK();
        }

        private void CreateHeader(bool isLandScape)
		{
			myRTFBuilder.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang4105\deflangfe4105{\fonttbl{\f0\froman\fprq2\fcharset0 Times New Roman;}{\f1\fswiss\fprq2\fcharset0 Calibri;}}\r\n");

            if (isLandScape)
            {
                myRTFBuilder.Append(@"\landscape\paperw15840\paperh12240");
            }

			myRTFBuilder.Append(@"{\*\generator Msftedit 5.41.21.2509;}\viewkind4\uc1\pard\sa200\sl276\slmult1\f0\fs24 ");
			myRTFBuilder.Append("REPORT - ");

            reportModel.ReportType = string.Empty;
            if (myReportClaimType == ReportType.Both)
                reportModel.ReportType = "PAID AND REJECTED CLAIMS";
            else if (myReportClaimType == ReportType.Paid)
                reportModel.ReportType = "PAID CLAIMS";
            else if (myReportClaimType == ReportType.Rejected)
				reportModel.ReportType = "REJECTED CLAIMS";
            else if (myReportClaimType == ReportType.UnitRecordWithPaidClaim)
                reportModel.ReportType = "UNIT RECORDS SUMMARY - PAID CLAIMS";
            else if (myReportClaimType == ReportType.UnitRecordWithRejectedClaim)
                reportModel.ReportType = "UNIT RECORDS SUMMARY - REJECTED CLAIMS";
            else
                reportModel.ReportType = "UNIT RECORD SUMMARY";

            myRTFBuilder.Append(reportModel.ReportType);
            RTF_BREAK();

            myRTFBuilder.Append("     DOWNLOAD DATE: ").Append(DateTime.UtcNow.AddHours(-6).ToString("yyyy-MM-dd HH:mm"));
			RTF_BREAK();
        
			myRTFBuilder.Append("\n").Append("PRACTITIONER # ").Append(myDoctorNumber);
			RTF_BREAK();
        }        

		private void CreateFooter()
		{
            reportModel.NumberOfClaims = reportModel.RecordList.Where(x => string.IsNullOrEmpty(x.WCBStatus)).Count();
            reportModel.TotalPaidAmount = reportModel.RecordList.Where(x => string.IsNullOrEmpty(x.WCBStatus)).Sum(x => x.PaidAmount);
            reportModel.WCBNumberOfClaims = reportModel.RecordList.Where(x => !string.IsNullOrEmpty(x.WCBStatus)).Count();
            reportModel.WCBTotalPaidAmount = reportModel.RecordList.Where(x => !string.IsNullOrEmpty(x.WCBStatus)).Sum(x => x.PaidAmount);

			myRTFBuilder.Append("}");
		}

        private void CreateFooterSummary()
        {
            myRTFBuilder.Append("}");
        }

        public string GetRTFContent()
		{
			CreateHeader(false);

            //MSB
            CreateClaimsReport(myRecords.Where(x => string.IsNullOrEmpty(x.WCBFaxStatus)).ToList());
            
            //WCB
            var wcbRecords = myRecords.Where(x => !string.IsNullOrEmpty(x.WCBFaxStatus) && x.PaidClaimId.HasValue).ToList();
            if (wcbRecords.Count() > 0)
            {
                myRTFBuilder.Append("WCB CLAIMS NOT VALIDATED");
                RTF_BREAK();

                myRTFBuilder.Append("\n");
                CreateClaimsReport(wcbRecords);
            }

			CreateFooter();

			return myRTFBuilder.ToString();
		}

        public string GetRTFUnitRecordSummaryContent()
        {
            CreateHeader(true);

            CreateUnitRecordsSummaryReport(myRecords);

            CreateFooterSummary();

            return myRTFBuilder.ToString();

        }

        public ClaimsPDFReportViewModel GetModel()
        {
            return reportModel;
        }
	}
}
