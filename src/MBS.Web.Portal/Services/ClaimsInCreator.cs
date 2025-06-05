using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using MBS.DomainModel;
using MBS.Web.Portal.Models;
using MBS.DataCache;

namespace MBS.Web.Portal.Services
{
    /// <summary>
    /// Summary description for ClaimInGeneration
    /// </summary>
    public class ClaimsInCreator
    {
        private StringBuilder myClaimsInBuilder = new StringBuilder();
        private IEnumerable<ServiceRecord> myServiceRecordList;
        private IEnumerable<UnitRecord> myUnitRecordList;
        private IEnumerable<string> myCareCodeList;
        private UserProfiles myProfile = null;
        private int myRecordNumber = 0;
        private int myCommentRecordNumber = 0;
        private int myServiceRecordNumber = 0;
        private int myClaimSequence = 0;
        private double myTotalAmount = 0;
        private bool isForRTFGeneration = false;
        private ClaimsInReportViewModel reportModel;
        private IEnumerable<string> codesRequireTime;

        public ClaimsInCreator(UserProfiles personProfile, IEnumerable<ServiceRecord> serviceRecords, IEnumerable<UnitRecord> unitRecords, IEnumerable<string> careCodeList)
        {
            myProfile = personProfile;
            myServiceRecordList = serviceRecords;
            myUnitRecordList = unitRecords;
            myClaimsInBuilder.Remove(0, myClaimsInBuilder.Length);
            myCareCodeList = careCodeList;
            reportModel = new ClaimsInReportViewModel();
            reportModel.Header = new HeaderModel();
            reportModel.Footer = new FooterModel();
            reportModel.RecordList = new List<RecordItem>();
            codesRequireTime = StaticCodeList.MyFeeCodeList.Where(x => !string.IsNullOrEmpty(x.Value.RequiredStartTime)).Select(x => x.Key).ToList();
        }

        private void CreateHeader()
        {
            myRecordNumber++;
            myClaimsInBuilder.Append(10);
            reportModel.Header.DocNumber = AdjustNumberValue(myProfile.DoctorNumber.ToString(), 4);

            myClaimsInBuilder.Append(reportModel.Header.DocNumber);

            myClaimsInBuilder.Append(AdjustNumberValue(myProfile.GroupNumber, 3));

            myClaimsInBuilder.Append(ValueFiller('0', 3));
            
            reportModel.Header.ClinicNumber = AdjustNumberValue(myProfile.ClinicNumber.ToString(), 3);
            myClaimsInBuilder.Append(reportModel.Header.ClinicNumber);

            reportModel.Header.Name = myProfile.DoctorName;

            if (isForRTFGeneration)
            {
                myClaimsInBuilder.Append(reportModel.Header.Name).Append("<name_ended12345>");
            }
            else
            {
                myClaimsInBuilder.Append(AdjustTextValue(reportModel.Header.Name, 25));
            }

            myClaimsInBuilder.Append(AdjustTextValue(myProfile.Street, 25));
            myClaimsInBuilder.Append(AdjustTextValue(myProfile.City.Trim() + " " + myProfile.Province.Trim(), 25));
            myClaimsInBuilder.Append(AdjustTextValue(myProfile.PostalCode, 6));
            myClaimsInBuilder.Append(8);

            reportModel.Header.Mode = myProfile.CorporationIndicator;
            myClaimsInBuilder.Append(myProfile.CorporationIndicator);
            myClaimsInBuilder.Append(System.Environment.NewLine);
        }

        private void CreateFooter()
        {
            myRecordNumber++;
            myClaimsInBuilder.Append(90);

            myClaimsInBuilder.Append(AdjustNumberValue(myProfile.DoctorNumber.ToString(), 4));
            myClaimsInBuilder.Append(ValueFiller('9', 6));

            reportModel.Footer.TotalRecords = myRecordNumber;
            myClaimsInBuilder.Append(AdjustNumberValue(myRecordNumber.ToString(), 5));

            reportModel.Footer.NumberOfServiceRecords = myServiceRecordNumber;
            myClaimsInBuilder.Append(AdjustNumberValue(myServiceRecordNumber.ToString(), 5));

            reportModel.Footer.NumberOfCommentRecords = myCommentRecordNumber;

            if (myTotalAmount > 99999.99)
                myTotalAmount = 99999.99f;

            reportModel.Footer.TotalAmount = string.Format("{0:0.00}", myTotalAmount);
            myClaimsInBuilder.Append(AdjustNumberValue(RoundUpValue(myTotalAmount).Replace(".", ""), 7));
            myClaimsInBuilder.Append(ValueFiller(' ', 69));
        }

        private void CreateReciprocal(ServiceRecord myServiceRecord, string myHSN)
        {
            myRecordNumber++;

            var record = new RecordItem();
            record.Type = RecordType.RECIPROCAL;
            record.HospitalNumber = AdjustTextValue(myHSN, 12);

            record.ClaimNumber = myServiceRecord.ClaimNumber.ToString();
            record.Location = myServiceRecord.Province;
            record.PatientName = myServiceRecord.PatientLastName + ", " + myServiceRecord.PatientFirstName;

            myClaimsInBuilder.Append(89);
            myClaimsInBuilder.Append(AdjustNumberValue(myProfile.DoctorNumber.ToString(), 4));
            myClaimsInBuilder.Append(record.ClaimNumber);
            myClaimsInBuilder.Append(0);
            myClaimsInBuilder.Append(ValueFiller(' ', 9));
            myClaimsInBuilder.Append(record.Location);

            if (isForRTFGeneration)
            {
                myClaimsInBuilder.Append(record.PatientName).Append("<name_ended12345>");
            }
            else
            {
                myClaimsInBuilder.Append(AdjustTextValue(myServiceRecord.PatientLastName, 18));
                myClaimsInBuilder.Append(AdjustTextValue(myServiceRecord.PatientFirstName, 9));
            }

            myClaimsInBuilder.Append(" ");
            myClaimsInBuilder.Append(record.HospitalNumber);
            myClaimsInBuilder.Append(ValueFiller(' ', 35));
            myClaimsInBuilder.Append(System.Environment.NewLine);

            reportModel.RecordList.Add(record);
        }

        private void CreateProcedureServiceRecord(UnitRecord myUnitRecord, int previousUnitNumber = 0)
        {
            myServiceRecordNumber++;
            myRecordNumber++;
            myClaimSequence++;

            var record = new RecordItem();
            record.Type = RecordType.NORMAL;
            record.SeqNumber = myClaimSequence.ToString();
            record.ClaimNumber = myUnitRecord.ServiceRecord.ClaimNumber.ToString();
            record.HospitalNumber = AdjustTextValue(myUnitRecord.ServiceRecord.HospitalNumber, 9);

            record.BirthDate = myUnitRecord.ServiceRecord.DateOfBirth.ToString("MMyy");

            record.Sex = AdjustTextValue(myUnitRecord.ServiceRecord.Sex.ToString(), 1);
            record.PatientName = myUnitRecord.ServiceRecord.PatientLastName + ", " + myUnitRecord.ServiceRecord.PatientFirstName;

            if (myUnitRecord.DiagCode == null)
            {
                throw new ArgumentException(myUnitRecord.ServiceRecordId.ToString());
            }
            else
            {
                record.Diag = myUnitRecord.DiagCode.PadLeft(3, '0');
            }

            record.ServiceStartDate = myUnitRecord.ServiceRecord.ServiceDate.ToString("ddMMyy");
            record.DischargeDate = myUnitRecord.ServiceRecord.DischargeDate.HasValue ? myUnitRecord.ServiceRecord.DischargeDate.Value.ToString("ddMMyy") : record.ServiceStartDate;
            record.UnitNumber = AdjustNumberValue(myUnitRecord.UnitNumber.ToString(), 2);
            record.FeeCode = AdjustNumberValue(myUnitRecord.UnitCode, 4);
            record.Location = myUnitRecord.UnitPremiumCode;

            var isRecordType57 = myCareCodeList.Contains(myUnitRecord.UnitCode.ToUpper());

            if (isRecordType57)
            {
                myClaimsInBuilder.Append(57);
            }
            else
            {
                myClaimsInBuilder.Append(50);
            }

            myClaimsInBuilder.Append(AdjustNumberValue(myProfile.DoctorNumber, 4));
            myClaimsInBuilder.Append(record.ClaimNumber);
            myClaimsInBuilder.Append(record.SeqNumber);
            myClaimsInBuilder.Append(record.HospitalNumber);
            myClaimsInBuilder.Append(record.BirthDate);
            myClaimsInBuilder.Append(record.Sex);

            if (isForRTFGeneration)
            {
                myClaimsInBuilder.Append(record.PatientName).Append("<name_ended12345>");
            }
            else
            {
                myClaimsInBuilder.Append(AdjustTextValue(record.PatientName, 25));
            }

            myClaimsInBuilder.Append(record.Diag);

            if (!string.IsNullOrEmpty(myUnitRecord.ServiceRecord.ReferringDoctorNumber))
            {
                record.ReferringDoctor = AdjustNumberValue(myUnitRecord.ServiceRecord.ReferringDoctorNumber, 4);
                myClaimsInBuilder.Append(AdjustNumberValue(myUnitRecord.ServiceRecord.ReferringDoctorNumber, 4));
            }
            else
            {
                myClaimsInBuilder.Append(ValueFiller(' ', 4));
            }           

            if (isRecordType57)
            {
                var startDate = myUnitRecord.ServiceRecord.ServiceDate.AddDays(previousUnitNumber);
                var endDate = startDate.AddDays(myUnitRecord.UnitNumber - 1);
                                                
                myClaimsInBuilder.Append(startDate.ToString("ddMMyy"));
                myClaimsInBuilder.Append(endDate.ToString("ddMMyy"));
                myClaimsInBuilder.Append(record.UnitNumber);
            }
            else
            {
                myClaimsInBuilder.Append(record.ServiceStartDate);
                myClaimsInBuilder.Append(record.UnitNumber);
            }

            if (!isRecordType57)
            {
                myClaimsInBuilder.Append(record.Location);
            }

            myClaimsInBuilder.Append(record.FeeCode);

            double myAmount = myUnitRecord.UnitAmount;
            if (myAmount > 9999.99)
                myAmount = 9999.99f;

            record.Amount = string.Format("{0:0.00}", myAmount);
            myClaimsInBuilder.Append(AdjustNumberValue(RoundUpValue(myAmount).Replace(".", ""), 6));
            myClaimsInBuilder.Append(myProfile.ClaimMode);
            myClaimsInBuilder.Append(8);

            if (string.IsNullOrEmpty(myUnitRecord.SpecialCircumstanceIndicator))
            {
                myClaimsInBuilder.Append(ValueFiller(' ', 2));
            }
            else
            {
                myClaimsInBuilder.Append(myUnitRecord.SpecialCircumstanceIndicator);
            }
            
            if (isRecordType57)
            {
                if (string.IsNullOrEmpty(myUnitRecord.ServiceRecord.FacilityNumber))
                {
                    myClaimsInBuilder.Append(ValueFiller(' ', 5));
                }
                else
                {
                    myClaimsInBuilder.Append(myUnitRecord.ServiceRecord.FacilityNumber.PadLeft(5, '0'));
                }

                if (string.IsNullOrEmpty(myUnitRecord.RecordClaimType))
                {
                    myClaimsInBuilder.Append(ValueFiller(' ', 1));
                }
                else
                {
                    myClaimsInBuilder.Append(myUnitRecord.RecordClaimType);
                }

                if (string.IsNullOrEmpty(myUnitRecord.ServiceRecord.ServiceLocation))
                {
                    myClaimsInBuilder.Append(ValueFiller(' ', 1));
                }
                else
                {
                    myClaimsInBuilder.Append(myUnitRecord.ServiceRecord.ServiceLocation);
                }

                myClaimsInBuilder.Append(ValueFiller(' ', 5));
            }
            else
            {
                if (string.IsNullOrEmpty(myUnitRecord.BilateralIndicator))
                {
                    myClaimsInBuilder.Append(ValueFiller(' ', 1));
                }
                else
                {
                    myClaimsInBuilder.Append(myUnitRecord.BilateralIndicator);
                }

                if (codesRequireTime.Contains(myUnitRecord.UnitCode))
                {
                    if (myUnitRecord.StartTime.HasValue)
                    {
                        myClaimsInBuilder.Append(myUnitRecord.StartTime.Value.ToString("hhmm"));
                    }
                    else
                    {
                        myClaimsInBuilder.Append(ValueFiller(' ', 4));
                    }

                    if (myUnitRecord.EndTime.HasValue)
                    {
                        myClaimsInBuilder.Append(myUnitRecord.EndTime.Value.ToString("hhmm"));
                    }
                    else
                    {
                        myClaimsInBuilder.Append(ValueFiller(' ', 4));
                    }
                }
                else
                {
                    myClaimsInBuilder.Append(ValueFiller(' ', 4));
                    myClaimsInBuilder.Append(ValueFiller(' ', 4));
                }

                if (string.IsNullOrEmpty(myUnitRecord.ServiceRecord.FacilityNumber))
                {
                    myClaimsInBuilder.Append(ValueFiller(' ', 5));
                }
                else
                {
                    myClaimsInBuilder.Append(myUnitRecord.ServiceRecord.FacilityNumber.PadLeft(5, '0'));
                }

                if (string.IsNullOrEmpty(myUnitRecord.RecordClaimType))
                {
                    myClaimsInBuilder.Append(ValueFiller(' ', 1));
                }
                else
                {
                    myClaimsInBuilder.Append(myUnitRecord.RecordClaimType);
                }

                if (string.IsNullOrEmpty(myUnitRecord.ServiceRecord.ServiceLocation))
                {
                    myClaimsInBuilder.Append(ValueFiller(' ', 1));
                }
                else
                {
                    myClaimsInBuilder.Append(myUnitRecord.ServiceRecord.ServiceLocation);
                }

                myClaimsInBuilder.Append(ValueFiller(' ', 1));
            }

            myClaimsInBuilder.Append(System.Environment.NewLine);

            reportModel.RecordList.Add(record);
        }

        private void CreateCommentRecord(ServiceRecord myServiceRecord)
        {
            var wholeComment = myServiceRecord.Comment.Replace("\r", "").Replace("\n", " ");
            var commentSequence = 0;

            do
            {
                var partialComment = string.Empty;
                if (wholeComment.Length > 77)
                {
                    partialComment = wholeComment.Substring(0, 77);
                    wholeComment = wholeComment.Substring(77);
                }
                else
                {
                    partialComment = AdjustTextValue(wholeComment.Substring(0), 77);
                    wholeComment = string.Empty;
                }

                myRecordNumber++;

                myCommentRecordNumber++;
                myClaimsInBuilder.Append(60);

                var record = new RecordItem();
                record.Type = RecordType.COMMENT;
                record.HospitalNumber = AdjustTextValue(myServiceRecord.HospitalNumber, 9);
                record.PatientName = partialComment;

                myClaimsInBuilder.Append(AdjustNumberValue(myProfile.DoctorNumber.ToString(), 4));
                myClaimsInBuilder.Append(myServiceRecord.ClaimNumber);
                myClaimsInBuilder.Append(commentSequence);
                myClaimsInBuilder.Append(record.HospitalNumber);
                myClaimsInBuilder.Append(record.PatientName);
                myClaimsInBuilder.Append(System.Environment.NewLine);

                reportModel.RecordList.Add(record);
                commentSequence++;

            } while (wholeComment.Length > 0);

        }

        private void CreateClaimsInContent()
        {
            CreateHeader();

            var mySortedList = SortedListFromProvince(myServiceRecordList);
            var myCacheHSN = string.Empty;

            foreach (var myServiceRecord in mySortedList)
            {
                myClaimSequence = -1;
                myCacheHSN = string.Empty;

                if (myServiceRecord.Province != "SK")
                {
                    myCacheHSN = myServiceRecord.HospitalNumber;
                    myServiceRecord.HospitalNumber = string.Empty;
                }

                var myUnitRecords = myUnitRecordList.Where(x => x.ServiceRecordId == myServiceRecord.ServiceRecordId).OrderBy(r => r.RecordIndex);

                foreach (var myUnitRecord in myUnitRecords.Where(x => !myCareCodeList.Contains(x.UnitCode.ToUpper())))
                {
                    CreateProcedureServiceRecord(myUnitRecord);
                    myTotalAmount += myUnitRecord.UnitAmount;
                }

                var record57List = myUnitRecords.Where(x => myCareCodeList.Contains(x.UnitCode.ToUpper())).OrderBy(x => x.UnitCode).ToList();
                for(var i = 0; i < record57List.Count(); i++)                
                {
                    var myUnitRecord = record57List.ElementAt(i);

                    var previousUnitRecordUnitNumber = 0;
                    if (i > 0)
                    {
                        previousUnitRecordUnitNumber = record57List[i - 1].UnitNumber;
                    }
                    
                    CreateProcedureServiceRecord(myUnitRecord, previousUnitRecordUnitNumber);
                    myTotalAmount += myUnitRecord.UnitAmount;
                }

                if (!string.IsNullOrEmpty(myServiceRecord.Comment))
                {
                    CreateCommentRecord(myServiceRecord);
                }

                if (myServiceRecord.Province != "SK")
                {
                    CreateReciprocal(myServiceRecord, myCacheHSN);
                }
            }

            CreateFooter();
        }

        private string AdjustTextValue(string myValue, int myLength)
        {
            if (myValue.Length > myLength)
            {
                myValue = myValue.Remove(myLength);
            }
            else if (myValue.Length < myLength)
            {
                myValue = myValue.PadRight(myLength, ' ');
            }
            return myValue;
        }

        private string AdjustNumberValue(string myValue, int myLength)
        {
            return myValue.PadLeft(myLength, '0').ToUpper();
        }

        private string ValueFiller(char myFiller, int myLength)
        {
            return String.Empty.PadLeft(myLength, myFiller);
        }

        private string RoundUpValue(double myValue)
        {
            return string.Format("{0:F2}", Math.Round(Convert.ToDecimal(myValue), 2));
        }

        private IEnumerable<ServiceRecord> SortedListFromProvince(IEnumerable<ServiceRecord> myList)
        {
            var mySortedList = myList.Where(x => x.Province == "SK").ToList();
            var myOutList = myList.Where(x => x.Province != "SK").OrderBy(x => x.ClaimNumber).ToList();
            return mySortedList.Concat(myOutList);
        }

        public ClaimsInReportViewModel GetReportViewModel()
        {
            return reportModel;
        }

        public string GetClaimsIn(bool isUsedForRTF)
        {
            this.isForRTFGeneration = isUsedForRTF;
            return GetClaimsIn();
        }

        public string GetClaimsIn()
        {
            CreateClaimsInContent();
            return myClaimsInBuilder.ToString().ToUpper();
        }
    }
}