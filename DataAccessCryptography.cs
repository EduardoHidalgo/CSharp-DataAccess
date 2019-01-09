using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace DataAccess
{
    public class DataAccessCryptography : BI.DataAccess.DataAccessComponent
    {
        #region Properties

        private Entities.Cryptography _cryptoInformation;
        private DateTime _Today = DateTime.Now;

        #endregion

        #region Contructor
        public DataAccessCryptography()
        {
            _cryptoInformation = new Entities.Cryptography();
        }
        public DataAccessCryptography(String applicationName)
        {
            _cryptoInformation = new Entities.Cryptography() { ApplicationName = applicationName };

        }
        #endregion

        #region Data
        private void SetCryptoConfiguration()
        {
            DataTable table = new DataTable();

            SqlCommand cmd = new SqlCommand("spw_ObtenerCertificados");
            cmd.Parameters.Add("@Application", SqlDbType.VarChar).Value = _cryptoInformation.ApplicationName;
            cmd.Parameters.Add("@Date", SqlDbType.Date).Value = _Today.ToString("yyyy/MM/dd");

            table = SearchTable(cmd);

            if (table != null && table.Rows.Count > 0)
            {
                _cryptoInformation = table.AsEnumerable().Select(row =>
                {
                    Entities.Cryptography cryptoInformation = new Entities.Cryptography();
                    cryptoInformation.Key = Encoding.ASCII.GetBytes(row["Key"].ToString());
                    cryptoInformation.Vector = Encoding.ASCII.GetBytes(row["Vector"].ToString());
                    cryptoInformation.ExpirationFrom = Convert.ToDateTime(row["FechaInicio"]);
                    cryptoInformation.ExperitationTo = Convert.ToDateTime(row["FechaFin"]);

                    //Metodo de Encriptacion

                    return cryptoInformation;
                }).First();
            }

        }
        #endregion

        #region Methods
        public string Encrypt(string textToConvert)
        {
            string convertedText = textToConvert;
            try
            {
                convertedText = RijndaelEncrypt(textToConvert);
            }
            catch 
            {
                return convertedText;
            }
            return convertedText;
        }
        public string Decrypt(string textToDecrypt)
        {
            string decryptText = textToDecrypt;

            try
            {
                decryptText = RinjndaelDecrypt(textToDecrypt);
            }
            catch
            {
                return decryptText;
            }
            return decryptText;
        }
        #endregion

        #region Cryptographic Algorithms

        #region RijdaelEncrypt
        private string RijndaelEncrypt(string textToConvert)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(textToConvert);
            byte[] encryptValue = null;
            RijndaelManaged crypto = new RijndaelManaged();

            try
            {
                MemoryStream memory = new MemoryStream(inputBytes.Length);
                using (CryptoStream cryptoStreaming = new CryptoStream(memory, crypto.CreateEncryptor(_cryptoInformation.Key, _cryptoInformation.Vector), CryptoStreamMode.Write))
                {
                    cryptoStreaming.Write(inputBytes, 0, inputBytes.Length);
                    cryptoStreaming.FlushFinalBlock();
                    cryptoStreaming.Close();
                    encryptValue = memory.ToArray();
                }
                return Convert.ToBase64String(encryptValue);
            }
            catch
            {
                return textToConvert;
            }
        }
        #endregion

        #region RijndaelDecrypt
        private String RinjndaelDecrypt(String textToDecrypt)
        {
            byte[] inputBytes = Convert.FromBase64String(textToDecrypt);
            byte[] resultBytes = new byte[inputBytes.Length + 1];

            String decryptResult = String.Empty;
            RijndaelManaged cryptoRijndael = new RijndaelManaged();
            try
            {
                MemoryStream memory = new MemoryStream(inputBytes);
                using (CryptoStream cryptoStreaming = new CryptoStream(memory, cryptoRijndael.CreateDecryptor(_cryptoInformation.Key, _cryptoInformation.Vector), CryptoStreamMode.Read))
                {
                    using (StreamReader reader = new StreamReader(cryptoStreaming, true))
                    {
                        decryptResult = reader.ReadToEnd();
                    }
                }
                return decryptResult;
            }
            catch
            {
                return textToDecrypt;
            }
        }
        #endregion

        #endregion

    }
}
