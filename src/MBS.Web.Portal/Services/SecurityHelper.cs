using MBS.DomainModel;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MBS.Web.Portal.Services
{
    public class SecurityHelper
	{
		private static string _secretKey = "hgCtO2HQbXdKR7qiYpcYp7euL";
        
		public static string EncryptDataOld(string dataToEncrypt)
		{
			byte[] results;
			var encodingUTF8 = new UTF8Encoding();
			var hashProvider = new MD5CryptoServiceProvider();
			var tdesKey = hashProvider.ComputeHash(encodingUTF8.GetBytes(_secretKey));

			var tdesAlgorithm = new TripleDESCryptoServiceProvider();
			tdesAlgorithm.Key = tdesKey;
			tdesAlgorithm.Mode = CipherMode.ECB;
			tdesAlgorithm.Padding = PaddingMode.PKCS7;

			var encryptBytes = encodingUTF8.GetBytes(dataToEncrypt);

			try
			{
				var encryptor = tdesAlgorithm.CreateEncryptor();
				results = encryptor.TransformFinalBlock(encryptBytes, 0, encryptBytes.Length);
			}
			finally
			{
				tdesAlgorithm.Clear();
				hashProvider.Clear();
			}

			return Convert.ToBase64String(results);
		}

		public static string DecryptStringOld(string dataToDecrypt)
		{
			byte[] results;
			var encodingUTF8 = new System.Text.UTF8Encoding();
			var hashProvider = new MD5CryptoServiceProvider();
			var tdesKey = hashProvider.ComputeHash(encodingUTF8.GetBytes(_secretKey));
			var tdesAlgorithm = new TripleDESCryptoServiceProvider();
			tdesAlgorithm.Key = tdesKey;
			tdesAlgorithm.Mode = CipherMode.ECB;
			tdesAlgorithm.Padding = PaddingMode.PKCS7;

			var decryptBytes = Convert.FromBase64String(dataToDecrypt);
			
			try
			{
				var decryptor = tdesAlgorithm.CreateDecryptor();
				results = decryptor.TransformFinalBlock(decryptBytes, 0, decryptBytes.Length);
			}
			finally
			{
				tdesAlgorithm.Clear();
				hashProvider.Clear();
			}

			return encodingUTF8.GetString(results);
		}

        public static IEnumerable<ServiceRecord> GetDecryptListOld(IEnumerable<ServiceRecord> serviceRecordList)
        {
            var result = new List<ServiceRecord>();

            foreach (var serviceRecord in serviceRecordList)
            {
                serviceRecord.PatientFirstName = SecurityHelper.DecryptStringOld(serviceRecord.PatientFirstName);
                serviceRecord.PatientLastName = SecurityHelper.DecryptStringOld(serviceRecord.PatientLastName);
                serviceRecord.HospitalNumber = SecurityHelper.DecryptStringOld(serviceRecord.HospitalNumber);                
                result.Add(serviceRecord);
            }

            return result;
        }
	}
}