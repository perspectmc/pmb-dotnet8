using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using iTextSharp.text.pdf;
using MBS.DomainModel;
using MBS.Common;
using System.Text;
using MBS.DataCache;

namespace MBS.Web.Portal.Services
{
    public class WCBPdfCreator
    {
        private string _pdfFilePath;
        private byte[] _pdfContent;

        public WCBPdfCreator(string pdfFilePath)
        {
            _pdfFilePath = pdfFilePath.Replace("\\ServiceRecord", string.Empty);
        }

        public long SendPDF(UserProfiles userProfile, ServiceRecord serviceRecord, IEnumerable<UnitRecord> unitRecords, string referDoctor, string userName, string password, string faxNumber)
        {
            _pdfContent = GetPDF(userProfile, serviceRecord, unitRecords, referDoctor);
            return SendFaxToProvider(_pdfContent, userName, password, faxNumber, userProfile.DoctorNumber.ToString(), serviceRecord.ClaimNumber.ToString());
        }

        public long SavePDFToLocal(UserProfiles userProfile, ServiceRecord serviceRecord, IEnumerable<UnitRecord> unitRecords, string referDoctor, string userName, string password, string faxNumber)
        {
            SavePDFToLocal(userProfile, serviceRecord, unitRecords, referDoctor);
            return long.MaxValue;
        }

        private void SavePDFToLocal(UserProfiles userProfile, ServiceRecord serviceRecord, IEnumerable<UnitRecord> unitRecords, string referDoctor)
        {
            _pdfContent = GetPDF(userProfile, serviceRecord, unitRecords, referDoctor);
            File.WriteAllBytes("C:\\Personal\\MBS\\Medical Billing\\Production\\MBS.Web.Portal\\bin\\output-" + DateTime.UtcNow.ToFileTime() + ".pdf", _pdfContent);
        }

        private long SendFaxToProvider(byte[] wcbPDF, string userName, string password, string faxNumber, string doctorNumber, string claimNumber)
        {
            long result = 0;
            var chunkSize = 30000;
            var sessionId = string.Empty;

            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                var client = new InterfaxService.InterFaxSoapClient("InterFaxSoap12");
               
                client.StartFileUpload(userName, password, ref sessionId);

                var numberOfUpload = wcbPDF.Length / chunkSize;
                var remindBytes = wcbPDF.Length % chunkSize;

                for (var i = 0; i < numberOfUpload; i++)
                {
                    byte[] buffer = new byte[chunkSize];	//this is max buffer size

                    for (var j = 0; j < chunkSize; j++)
                    {
                        buffer[j] = wcbPDF[j + i * chunkSize];
                    }

                    client.UploadFileChunk(sessionId, buffer, false);
                }

                if (remindBytes > 0)
                {
                    byte[] buffer = new byte[remindBytes];	//this is max buffer size

                    var targetStart = wcbPDF.Length - remindBytes;
                    for (var j = 0; j < remindBytes; j++)
                    {
                        buffer[j] = wcbPDF[targetStart + j];
                    }

                    client.UploadFileChunk(sessionId, buffer, true);
                }

                result = client.SendfaxEx_2(userName, password, faxNumber, "", null, "pdf", wcbPDF.Length.ToString() + "/sessionID=" + sessionId, DateTime.MinValue, 1, 
                                "CSID", "", "", "WCB Claims for " + doctorNumber + " - " + claimNumber, "", "A4", "Portrait", false, true);

                client.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
            }

            return result;
        }

        public bool IsFaxSuccessful(IEnumerable<long> transactionIds, string userName, string password)
        {
            var result = false;

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11;

            using (var client = new InterfaxService.InterFaxSoapClient("InterFaxSoap12"))
            {
                var verbData = string.Join(",", transactionIds.Select(x => x.ToString()));
                var resultCode = 0;
                var faxResults = client.FaxQuery(userName, password, "IN", verbData, transactionIds.Count(), ref resultCode);

                result = faxResults.Count(x => x.Status == 0) > 0;
            }

            return result;
        }
        
        private byte[] GetPDF(UserProfiles userProfile, ServiceRecord serviceRecord, IEnumerable<UnitRecord> unitRecords, string referDoctor)
        {
            var pdfReader = new PdfReader(_pdfFilePath);
            var memoryStream = new MemoryStream();
            var pdfStamper = new PdfStamper(pdfReader, memoryStream);
            var pdfFormFields = pdfStamper.AcroFields;
            pdfFormFields.GenerateAppearances = true;
            
            pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformHeader[0].TableAddresses[0].Row2[0].WorkerAddress[0].FragmentCorrespondingAddressInteractive[0].subformsetAddress[0].SubformBlankAddress[0].BlankInput[0]", serviceRecord.PatientFirstName + " " + serviceRecord.PatientLastName);

            if (!string.IsNullOrEmpty(referDoctor))
            {
                pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformDiagnosis[0].ReferredFrom[0]", referDoctor);
            }

            if (!string.IsNullOrEmpty(serviceRecord.Comment))
            {
                pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Commentsinteractive[0].Comments[0]", System.Environment.NewLine + serviceRecord.Comment);
            }

            pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformHeader[0].SubformInfo[0].SubformClinic[0].ClinicType[0]", userProfile.ClinicNumber.Trim());

            pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformHeader[0].SubformInfo[0].SubformPhoneNum[0].PhoneNumber[0].AreaCode[0]", "(" + userProfile.PhoneNumber.Substring(0, 3) + ")");
            pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformHeader[0].SubformInfo[0].SubformPhoneNum[0].PhoneNumber[0].Number[0]", userProfile.PhoneNumber.Substring(4));

            pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformHeader[0].SubformInfo[0].SubformDoctor[0].CaregiverType[0]", userProfile.DoctorNumber.ToUpper());

            pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformHeader[0].SubformInfo[0].SubformDOB[0].WorkerDOB[0]", serviceRecord.DateOfBirth.ToString("yyyy-MM-dd"), serviceRecord.DateOfBirth.ToString("MMMM, yyyy"));
            pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformHeader[0].SubformInfo[0].PersonalHealthNumber[0]", serviceRecord.HospitalNumber);
            pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformHeader[0].SubformInfo[0].ClinicName[0]", userProfile.DoctorName);

            var address = new StringBuilder();
            address.Append(userProfile.DoctorName).Append(System.Environment.NewLine);
            address.Append(userProfile.Street).Append(System.Environment.NewLine);
            address.Append(userProfile.City).Append(", ").Append(userProfile.Province).Append(System.Environment.NewLine);
            address.Append(userProfile.PostalCode.ToUpper());

            pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformHeader[0].TableAddresses[0].Row2[0].PhysicianAddress[0].FragmentCorrespondingAddressInteractive[0].subformsetAddress[0].SubformBlankAddress[0].BlankInput[0]", address.ToString());

            var diagCodes = unitRecords.Select(x => x.DiagCode).Distinct();
            pdfFormFields.SetField("TemplateCGVEBilling-CgvFrm[0].Content[0].SubformDiagnosis[0].Diagnosis[0]", string.Join(", ", diagCodes));

            var index = 0;
            var totalAmount = 0.0d;
            foreach (var unitRecord in unitRecords.OrderBy(x => x.UnitCode))
            {
                pdfFormFields.SetField(string.Format("TemplateCGVEBilling-CgvFrm[0].Content[0].TableTeatmentDetails[0].RowTreatment[{0}].ServiceDate[0]", index), serviceRecord.ServiceDate.ToString("yyyy-MM-dd"), serviceRecord.ServiceDate.ToString("MM-dd-yyyy"));
                pdfFormFields.SetField(string.Format("TemplateCGVEBilling-CgvFrm[0].Content[0].TableTeatmentDetails[0].RowTreatment[{0}].FeeAmount[0]", index), string.Format("{0:C}", unitRecord.UnitAmount));
                pdfFormFields.SetField(string.Format("TemplateCGVEBilling-CgvFrm[0].Content[0].TableTeatmentDetails[0].RowTreatment[{0}].FeeCode[0]", index), unitRecord.UnitCode.ToUpper());
                pdfFormFields.SetField(string.Format("TemplateCGVEBilling-CgvFrm[0].Content[0].TableTeatmentDetails[0].RowTreatment[{0}].Units[0]", index), unitRecord.UnitNumber.ToString());
                totalAmount += unitRecord.PaidAmount;
                index++;
            }

            var premCodeList = unitRecords.Where(x => x.UnitPremiumCode.Equals("b", StringComparison.OrdinalIgnoreCase) || x.UnitPremiumCode.Equals("k", StringComparison.OrdinalIgnoreCase)).Select(x => x.UnitPremiumCode).Distinct().ToList();
            if (premCodeList.Any())
            {
                var premCode = premCodeList.FirstOrDefault();
                if (premCode.Equals("b", StringComparison.OrdinalIgnoreCase) || premCode.Equals("k", StringComparison.OrdinalIgnoreCase))
                {
                    var wcbPremiumCode = premCode.Equals("b", StringComparison.OrdinalIgnoreCase) ? "897H" : "899H";

                    //var wantedRecords = unitRecords.Where(x => _premiumCodeList.IndexOf("," + x.UnitCode.ToUpper() + ",") == -1);
                    var wantedRecords = unitRecords.Where(x => !StaticCodeList.MyPremiumCodeList.Contains(x.UnitCode));

                    var wcbPremiumAmount = wantedRecords.Sum(x => (x.PaidAmount - x.UnitAmount));
                    var wcbPremiumUnitNumber = wantedRecords.Where(x => x.UnitPremiumCode.Equals(premCode, StringComparison.OrdinalIgnoreCase)).Sum(x => x.UnitNumber);
                    totalAmount += wcbPremiumAmount;

                    pdfFormFields.SetField(string.Format("TemplateCGVEBilling-CgvFrm[0].Content[0].TableTeatmentDetails[0].RowTreatment[{0}].ServiceDate[0]", index), serviceRecord.ServiceDate.ToString("yyyy-MM-dd"), serviceRecord.ServiceDate.ToString("MM-dd-yyyy"));
                    pdfFormFields.SetField(string.Format("TemplateCGVEBilling-CgvFrm[0].Content[0].TableTeatmentDetails[0].RowTreatment[{0}].FeeAmount[0]", index), string.Format("{0:C}", wcbPremiumAmount));
                    pdfFormFields.SetField(string.Format("TemplateCGVEBilling-CgvFrm[0].Content[0].TableTeatmentDetails[0].RowTreatment[{0}].FeeCode[0]", index), wcbPremiumCode);
                    pdfFormFields.SetField(string.Format("TemplateCGVEBilling-CgvFrm[0].Content[0].TableTeatmentDetails[0].RowTreatment[{0}].Units[0]", index), wcbPremiumUnitNumber.ToString());
                }
            }

            pdfFormFields.SetField(string.Format("TemplateCGVEBilling-CgvFrm[0].Content[0].TableTeatmentDetails[0].FooterRow[0].TotalAmount[0]", index), string.Format("{0:C}", totalAmount));

            pdfStamper.FormFlattening = true;

            pdfStamper.Close();
            pdfReader.Close();

            var result = memoryStream.ToArray();

            memoryStream.Close();

            return result;
        }

        private static bool WriteFile(byte[] fileData, string filePath)
        {
            try
            {
                File.WriteAllBytes(filePath, fileData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetBase64PDFContent()
        {
            if (_pdfContent.Any())
            {
                return Convert.ToBase64String(_pdfContent);
            }
            else
            {
                return null;
            }
        }
    }
}