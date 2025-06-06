using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using MBS.DomainModel;
using System.Collections.Generic;

namespace MBS.ReconcileClaims
{
	public class SecurityHelper
	{
		private static string _secretKey = "hgCtO2HQbXdKR7qiYpcYp7euL";

		public static string EncryptData(string dataToEncrypt)
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

		public static string DecryptString(string dataToDecrypt)
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

        public static IEnumerable<ServiceRecord> GetDecryptList(IEnumerable<ServiceRecord> serviceRecordList)
        {
            var result = new List<ServiceRecord>();

            foreach (var serviceRecord in serviceRecordList)
            {
                serviceRecord.PatientFirstName = SecurityHelper.DecryptString(serviceRecord.PatientFirstName);
                serviceRecord.PatientLastName = SecurityHelper.DecryptString(serviceRecord.PatientLastName);
                serviceRecord.HospitalNumber = SecurityHelper.DecryptString(serviceRecord.HospitalNumber);                
                result.Add(serviceRecord);
            }

            return result;
        }
	}
}