using System;
using System.IO;
using System.Net.Mail;
using System.Data;
using System.Data.SqlClient;

using BI.Entities;

namespace BI.DataAccess
{
    public class CommonDataAccess:DataAccessComponent
    {
        public int SendMail(MailValues mailValues)
        {
            int result = -1;
            if (mailValues != null && mailValues.Message != null && mailValues.Message.To != null)
            {
                string sCmd = "insert into mailqueue(fecha, clav_sitio, clav_tipomail, clav_asociado, Solicita, Folio, eMail, Fecha_Envio, Status, Error, Mensaje, Subject)";
                sCmd += "values(getdate(), @Site, @MailType, @Asoc, @From, @Folio, @To, null, 'P', null, @Message, @Subject)";

                SqlCommand comm = new SqlCommand(sCmd);
                comm.Parameters.Add(new SqlParameter("@Site", SqlDbType.VarChar, 250)).Value = mailValues.Site;
                comm.Parameters.Add(new SqlParameter("@MailType", SqlDbType.VarChar, 6)).Value = mailValues.MailType;
                comm.Parameters.Add(new SqlParameter("@Asoc", SqlDbType.VarChar, 10)).Value = mailValues.AssociatteId;
                comm.Parameters.Add(new SqlParameter("@From", SqlDbType.VarChar, 250)).Value = mailValues.Message.From.Address;
                comm.Parameters.Add(new SqlParameter("@Folio", SqlDbType.VarChar, 500)).Value = mailValues.Folio;


                string tmp = string.Empty;
                foreach (MailAddress mailAddress in mailValues.Message.To)
                {
                    if (!string.IsNullOrEmpty(tmp)) tmp += "; ";
                    tmp += mailAddress.Address;
                }
                comm.Parameters.Add(new SqlParameter("@To", SqlDbType.VarChar, 250)).Value = tmp;
                comm.Parameters.Add(new SqlParameter("@Message", SqlDbType.VarChar, 5000)).Value = mailValues.Message.Body;
                comm.Parameters.Add(new SqlParameter("@Subject", SqlDbType.VarChar, 100)).Value = mailValues.Message.Subject;

                comm.CommandType = CommandType.Text;
                comm.CommandTimeout = 0;
                comm.Connection = this.GetConnection("MailDataAccess");

                result = this.ExecuteSQL(comm);
            }
            return result;
        }

        /// <summary>
        /// Registra en el archivo de bitacora un evento.
        /// </summary>
        /// <param name="text">Mensaje del evento</param>
        /// <param name="pathFile">Ruta del archivo</param>
        public void SaveLineInFile(string text, string pathFile)
        {
            try
            {
                if (File.Exists(pathFile))
                {
                    using (StreamWriter sw = File.AppendText(pathFile))
                    {
                        sw.WriteLine(text);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.CreateText(pathFile))
                    {
                        sw.WriteLine(text);
                    }
                }
            }
            catch { }
        }
    }
}
