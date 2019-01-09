using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BI.Entities;
using System.Net.Mail;
using System.Net;
using DataAccess;

namespace BI.DataAccess
{
    /// <summary>
    /// Clase contenedora de metodos de acceso a datos y conexiones
    /// </summary>
    public abstract class DataAccessComponent : IDisposable
    {

        #region Propiedades y Variables
        private SqlConnection cn = null;
        private string defaultConexionName;     // Nombre de cadena de conexion por default
        private string currentConexionName;     // Nombre de cadena de conexion activa
        private bool IsDisposable;              // Indica si la clase está usando Disposable

        // Variables para Log
        internal DateTime _now;
        internal string _path = Configuration.SQLFilePath + DateTime.Now.ToString("yyyyMMdd") + ".txt";
        internal string _errorPath = Configuration.SQLErrorPath + DateTime.Now.ToString("yyyyMMdd") + ".txt";

        internal string _mailPath = Configuration.SQLLogsPath + DateTime.Now.ToString("yyyyMMdd") + ".txt";


        /// <summary>
        /// Define el tiempo de TimeOut para los comandos.
        /// Su default es 0.
        /// </summary>
        public int TimeOut { get; set; }

        /// <summary>
        /// Define el tipo de comando para los comandos
        /// Su default es Store Procedure
        /// </summary>
        public CommandType CommandType { get; set; }


        /// <summary>
        /// Obtiene la cadena de conexion solicitada desde el Web.Config
        /// </summary>
        /// <param name="connectionName">Conexion solicitada. Por default usa DataAccess</param>
        /// <returns>Cadena de conexion solicitada</returns>
        private string ConnectionsString(string name = null)
        {
            // Si no se solicita una en especial, se usa la default            
            if (string.IsNullOrEmpty(name)) name = defaultConexionName;            
            // Se obtiene la conexion            

            /* Esta conexion hace un barrido sobre las conexiones existentes en el archivo de configuracion, toma el nombre que debe traer, revisa que
               estos datos coincidan con los que tiene en el archivo de configuracion esto comparando con su nombre identificador y pasa la cadena de
               conexion de quien lo solocita
            */
            // Get the ConnectionStrings collection.
            ConnectionStringSettingsCollection connections = ConfigurationManager.ConnectionStrings;
            //recibe la cadena de conexion
            string conn = null;

            if (connections.Count != 0)
            {                
                // Obtine la coleccion de conexiones del archivo de configuracion
                int gg = 0;
                foreach (ConnectionStringSettings connection in connections)
                {
                    string namo = connection.Name;
                    string provider = connection.ProviderName;
                    string connectionString = connection.ConnectionString;
                    if (name.Equals(namo))
                        {
                            conn = connectionString;
                        }
                    gg++;

                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("No connection string is defined.");
                Console.WriteLine();
            }

            //AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            //System.IO.File.WriteAllText(@"C:\pruebas\ERROR-3-" + DateTime.Today.ToString("yyyyMMdd") + ".txt", AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            //System.Configuration.ConfigurationFileMap map = new ConfigurationFileMap(@"C:\\Proyectos\\BI\\Robots\\app.config");                        
            //System.IO.File.WriteAllText(@"C:\pruebas\ERROR-4-" + DateTime.Today.ToString("yyyyMMdd") + ".txt", System.Configuration.ConfigurationManager.ConnectionStrings["name"].ProviderName);                        
            //ok string conn = System.Configuration.ConfigurationManager.ConnectionStrings[name].ConnectionString;

            // Se valida
            if (string.IsNullOrEmpty(conn)) throw new Exception("Error 1000. ConnectionString Not Valid");

            // Regresa la cadena
            return conn;
        }

        private string DecryptConnectionsString(string name = null)
        {
            DataAccessCryptography cryptoService = new DataAccessCryptography();
            // Si no se solicita una en especial, se usa la default            
            if (string.IsNullOrEmpty(name)) name = defaultConexionName;
            // Se obtiene la conexion            

            /* Esta conexion hace un barrido sobre las conexiones existentes en el archivo de configuracion, toma el nombre que debe traer, revisa que
               estos datos coincidan con los que tiene en el archivo de configuracion esto comparando con su nombre identificador y pasa la cadena de
               conexion de quien lo solocita
            */
            // Get the ConnectionStrings collection.
            ConnectionStringSettingsCollection connections = ConfigurationManager.ConnectionStrings;
            //recibe la cadena de conexion
            string conn = null;

            if (connections.Count != 0)
            {
                // Obtine la coleccion de conexiones del archivo de configuracion
                int gg = 0;
                foreach (ConnectionStringSettings connection in connections)
                {
                    string namo = connection.Name;
                    string provider = connection.ProviderName;
                    string connectionString = connection.ConnectionString;
                    if (name.Equals(namo))
                    {
                        conn = cryptoService.Decrypt(connectionString);
                    }
                    gg++;

                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("No connection string is defined.");
                Console.WriteLine();
            }

            //AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            //System.IO.File.WriteAllText(@"C:\pruebas\ERROR-3-" + DateTime.Today.ToString("yyyyMMdd") + ".txt", AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            //System.Configuration.ConfigurationFileMap map = new ConfigurationFileMap(@"C:\\Proyectos\\BI\\Robots\\app.config");                        
            //System.IO.File.WriteAllText(@"C:\pruebas\ERROR-4-" + DateTime.Today.ToString("yyyyMMdd") + ".txt", System.Configuration.ConfigurationManager.ConnectionStrings["name"].ProviderName);                        
            //ok string conn = System.Configuration.ConfigurationManager.ConnectionStrings[name].ConnectionString;

            // Se valida
            if (string.IsNullOrEmpty(conn)) throw new Exception("Error 1000. ConnectionString Not Valid");

            // Regresa la cadena
            return conn;
        }
        #endregion

        #region Constructor Destructor

        /// <summary>
        /// Inicializa la clase
        /// </summary>
        /// <param name="disponsable">Indica si se está usando la clase de manera Disposable. [True] Las conexiones se cierran con el dispose; [False] Las conexiones se cierran despues de consultar.</param>
        public DataAccessComponent(bool disponsable = false)
        {
            this.defaultConexionName = "DataAccess";
            this.currentConexionName = this.defaultConexionName;
            this.IsDisposable = disponsable;
            this.TimeOut = 0;
            this.CommandType = CommandType.StoredProcedure;
        }

        /// <summary>
        /// Desecha la clase.
        /// Validando que la conexion se cierre y se deseche
        /// </summary>
        public void Dispose()
        {
            if (cn != null && cn.State != ConnectionState.Closed)
                cn.Close();

            if (cn != null) cn.Dispose();
        }
        #endregion

        #region Metodos

        /// <summary>
        /// Obtiene una conexion definida en el web.config
        /// </summary>
        /// <param name="name">Nombre de la conexion a usar. Por default usa el nombre default de la conexion.</param>
        /// <returns>La conexion solicitada con los parametros definidos en el web.config</returns>
        protected SqlConnection GetConnection(string name = null, bool reuse = false)
        {
            // En caso de tener una conexion abierta, que no haya cambiado y que se pida reusar no se cierra
            if (this.cn != null && cn.State != ConnectionState.Closed)
            {
                // Si no se pidio reusar o cambio de conexion cerramos conexion
                if (!reuse || this.currentConexionName != name)
                {
                    cn.Close();
                    string errorMessage;
                    errorMessage = MethodBase.GetCurrentMethod().Name + " - Se forzó cerrar la conexion.";
                    SaveLineInFile(errorMessage, true);
                    SendErrorMessage(errorMessage, "SQL Error Message");
                }
            }

            // Inicializamos en caso que no haya sido inicializada
            if (cn == null) cn = new SqlConnection();

            // Si se solicita otra conexion o no se tiene
            // Actualizamos la conexion actual o en su caso la solicitada
            if (string.IsNullOrEmpty(cn.ConnectionString) || this.currentConexionName != name)
            {
                this.currentConexionName = name ?? this.defaultConexionName;
                cn.ConnectionString = ConnectionsString(name: this.currentConexionName);
            }

            // Regresamos la referencia de la conexion interna
            return cn;
        }

        /// <summary>
        /// Obtiene una conexion definida en el web.config
        /// </summary>
        /// <param name="name">Nombre de la conexion a usar. Por default usa el nombre default de la conexion.</param>
        /// <returns>La conexion solicitada con los parametros definidos en el web.config</returns>
        protected SqlConnection GetConnectionEncrypt(string name = null, bool reuse = false)
        {
            // En caso de tener una conexion abierta, que no haya cambiado y que se pida reusar no se cierra
            if (this.cn != null && cn.State != ConnectionState.Closed)
            {
                // Si no se pidio reusar o cambio de conexion cerramos conexion
                if (!reuse || this.currentConexionName != name)
                {
                    cn.Close();
                    string errorMessage;
                    errorMessage = MethodBase.GetCurrentMethod().Name + " - Se forzó cerrar la conexion.";
                    SaveLineInFile(errorMessage, true);
                    SendErrorMessage(errorMessage, "SQL Error Message");
                }
            }

            // Inicializamos en caso que no haya sido inicializada
            if (cn == null) cn = new SqlConnection();

            // Si se solicita otra conexion o no se tiene
            // Actualizamos la conexion actual o en su caso la solicitada
            if (string.IsNullOrEmpty(cn.ConnectionString) || this.currentConexionName != name)
            {
                this.currentConexionName = name ?? this.defaultConexionName;
                cn.ConnectionString = DecryptConnectionsString(name: this.currentConexionName);
            }

            // Regresamos la referencia de la conexion interna
            return cn;
        }

        /// ----------------------------------------------------------------------------------------------
        /// Metodos para Consultas -----------------------------------------------------------------------
        /// ----------------------------------------------------------------------------------------------

        /// <summary>
        /// Genera una consulta para obtener una tabla de resultados.
        /// Cierra su conexion automaticamente
        /// </summary>
        /// <param name="cmd">Comando que define la busqueda</param>
        /// <param name="dbEntitie">Clase de servicio DAC</param>
        /// <returns>Tabla de resultados</returns>
        protected DataTable SearchTable(SqlCommand cmd, string connectionName = null, DBEntities dbEntities = null, bool Encrypt = false)
        {
            // Validamos consulta
            int cont=0;
            bool succeeded = false;
            if (!IsValidCommand(cmd.CommandText)) return null;

            // Inicializamos 
            DataTable dt = new DataTable();

            // Asignamos conexion y comando
            cmd.Connection = Encrypt ? GetConnectionEncrypt(connectionName) : GetConnection(connectionName);
            cmd.CommandType = this.CommandType;
            cmd.CommandTimeout = this.TimeOut;

            // Asignamos valores de seguimiento
            this.SetDBValues(cmd, this.currentConexionName, MethodInfo.GetCurrentMethod().Name, dbEntities);
            do
            {
                try
                {
                    SqlDataAdapter parametrosDA = new SqlDataAdapter(cmd);
                    parametrosDA.Fill(dt);
                    EndQuery(dbEntities, dt.Rows.Count);
                    succeeded = true;
                }
                catch (Exception ex)
                {
                    cont++;

                    // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                    if (cmd.Connection.State != ConnectionState.Closed) cmd.Connection.Close();
                    ErrorQuery(dbEntities, ex.Message);
                }
            } while (cont < 3 && !succeeded);
            //finally
            //{
            //    // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
            //    if (!this.IsDisposable && cmd.Connection.State != ConnectionState.Closed)
            //    {
            //        cmd.Connection.Close();
            //    }
            //}

            // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
            if (!this.IsDisposable && cmd.Connection.State != ConnectionState.Closed)
            {
                cmd.Connection.Close();
            }

            return dt;
        }

        /// <summary>
        /// Ejecuta consulta que obtiene más de una tabla y las guarda en un DataSet
        /// </summary>
        /// <param name="cmd">Comando a ejecutar</param>
        /// <param name="connectionName">Nombre de Conexión</param>
        /// <param name="dbEntities">Mensaje que se regresa en caso de haber alguna incidencia en SQL</param>
        /// <returns>DataSet contenedor de las tablas resultado</returns>
        protected DataSet SearchTables(SqlCommand cmd, string connectionName = null, DBEntities dbEntities = null, bool Encrypt = false)
        {
            // Validamos consulta
            if (!IsValidCommand(cmd.CommandText)) return null;

            // Inicializamos DataSet
            DataSet ds = new DataSet();

            // Asignamos conexion y comando
            cmd.Connection = Encrypt ? GetConnectionEncrypt(connectionName) : GetConnection(connectionName);
            cmd.CommandType = this.CommandType;
            cmd.CommandTimeout = this.TimeOut;

            // Asignamos valores de seguimiento
            this.SetDBValues(cmd, this.currentConexionName, MethodInfo.GetCurrentMethod().Name, dbEntities);

            try
            {
                SqlDataAdapter parametrosDA = new SqlDataAdapter(cmd);
                parametrosDA.Fill(ds);
                EndQuery(dbEntities, ds.Tables.Count);
            }
            catch (Exception ex)
            {
                // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                if (cmd.Connection.State != ConnectionState.Closed) cmd.Connection.Close();
                ErrorQuery(dbEntities, ex.Message);
            }
            finally
            {
                // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                if (!this.IsDisposable && cmd.Connection.State != ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
            return ds;
        }

        /// <summary>
        /// Ejecuta consulta que obtiene más de una tabla y las guarda en un DataSet
        /// </summary>
        /// <param name="cmd">Comando a ejecutar</param>
        /// <param name="connectionName">Nombre de Conexión</param>
        /// <param name="dbEntities">Mensaje que se regresa en caso de haber alguna incidencia en SQL</param>
        /// <returns>DataSet contenedor de las tablas resultado</returns>
        protected DataSet LoadTablesCSV(string FileName,string conexionName)
        {
            if (!File.Exists(FileName)) return null;

            DataSet ds = new DataSet();

            System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection("Provider=Microsft.Jet.OleDb.4.0;Data Source=" + Path.GetDirectoryName(FileName) + ";Extended Properties = \"Text;HDR=YES;FMT=Delimited\"");
            conn.Open();

            this.SetCSVValues(FileName, conexionName);

            try
            {
                System.Data.OleDb.OleDbDataAdapter adapter = new System.Data.OleDb.OleDbDataAdapter("Select * from " + Path.GetFileName(FileName), conn);
                adapter.Fill(ds);
                EndCSVLoad(FileName, ds.Tables[0].Rows.Count);
            }
            catch (Exception ex)
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
                ErrorLoadCSV(FileName, ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return ds;
        }

        /// <summary>
        /// Genera una consulta para obtener una lista de resultados
        /// </summary>
        /// <param name="cmd">Comando que define la busqueda</param>
        /// <returns>lista de resultados</returns>
        protected Dictionary<string, object> SearchRow(SqlCommand cmd, string connectionName = null, DBEntities dbEntities = null)
        {
            // Validamos consulta
            if (!IsValidCommand(cmd.CommandText)) return null;

            // Inicializamos 
            Dictionary<string, object> result = null;
            SqlDataReader reader = null;

            // Asignamos conexion y comando
            cmd.Connection = GetConnection(connectionName);
            cmd.CommandType = this.CommandType;
            cmd.CommandTimeout = this.TimeOut;

            // Asignamos valores de seguimiento
            this.SetDBValues(cmd, this.currentConexionName, MethodInfo.GetCurrentMethod().Name, dbEntities);

            try
            {
                cmd.Connection.Open();
                reader = cmd.ExecuteReader(CommandBehavior.SingleRow);
                if (reader.HasRows)
                {
                    reader.Read();
                    result = new Dictionary<string, object>();
                    for (int x = 0; x < reader.FieldCount; x++)
                    {
                        result.Add(reader.GetName(x), reader[x]);
                    }
                }
                EndQuery(dbEntities, (reader.HasRows) ? 1 : 0);
                reader.Close();
            }
            catch (Exception ex)
            {
                // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                if (cmd.Connection.State != ConnectionState.Closed) cmd.Connection.Close();
                ErrorQuery(dbEntities, ex.Message);
            }
            finally
            {
                // Cerramos Reader
                if (reader != null && !reader.IsClosed) reader.Close();

                // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                if (!this.IsDisposable && cmd.Connection.State != ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Ejecuta un comando utilizando la conexion que incluye
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected int ExecuteSQL(SqlCommand cmd, string connectionName = null, DBEntities dbEntities = null, bool Encrypt = false)
        {
            // Validamos consulta
            if (!IsValidCommand(cmd.CommandText)) return -1;

            // Inicializamos
            int result = 0;

            // Asignamos conexion y comando
            cmd.Connection = Encrypt ? GetConnectionEncrypt(connectionName) : GetConnection(connectionName);
            cmd.CommandType = this.CommandType;
            cmd.CommandTimeout = this.TimeOut;

            // Asignamos valores de seguimiento
            this.SetDBValues(cmd, this.currentConexionName, MethodInfo.GetCurrentMethod().Name, dbEntities);

            try
            {
                cmd.Connection.Open();
                result = cmd.ExecuteNonQuery();
                EndQuery(dbEntities, result);
            }
            catch (Exception ex)
            {
                // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                if (cmd.Connection.State != ConnectionState.Closed) cmd.Connection.Close();
                result = -1;
                ErrorQuery(dbEntities, ex.Message);
            }
            finally
            {
                // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                if (!this.IsDisposable && cmd.Connection.State != ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Ejecuta un comando utilizando la conexion que incluye
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected int ExecuteSQLSortOrder(SqlCommand cmd, string connectionName = null, DBEntities dbEntities = null, bool Encrypt = false)
        {
            // Validamos consulta
            if (!IsValidCommand(cmd.CommandText)) return -1;

            // Inicializamos
            int result = 0;

            // Asignamos conexion y comando
            cmd.Connection = Encrypt ? GetConnectionEncrypt(connectionName) : GetConnection(connectionName);
            cmd.CommandType = this.CommandType;
            cmd.CommandTimeout = this.TimeOut;

            // Asignamos valores de seguimiento
            this.SetDBValues(cmd, this.currentConexionName, MethodInfo.GetCurrentMethod().Name, dbEntities);

            try
            {
                cmd.Connection.Open();
                result = cmd.ExecuteNonQuery();
                EndQuery(dbEntities, result);
            }
            catch (Exception ex)
            {
                // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                if (cmd.Connection.State != ConnectionState.Closed) cmd.Connection.Close();
                result = -1;

                try
                {
                    var exx = ex as SqlException;
                    if (exx.Number == 64 || exx.Number == 53) // Error de conexión. SQLException
                        result = -2;
                }
                catch { }

                ErrorQuery(dbEntities, ex.Message);
            }
            finally
            {
                // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                if (!this.IsDisposable && cmd.Connection.State != ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Ejecuta un bulk utilizando la conexion que incluye
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        protected int ExecuteBulkSQL(string tableName, DataTable table, string methodName, string connectionName = null, DBEntities dbEntities = null)
        {
            // Inicializamos
            int result = 0;

            // Validamos consulta
            if (tableName == null || table == null) return -1;

            SqlConnection cn = GetConnection();
            //test
            //string currentDB = System.Configuration.ConfigurationManager.ConnectionStrings.
            SqlConnection _cn = new SqlConnection(cn.ConnectionString);          
            
            // Asignamos valores de seguimiento
            this.SetDBBulkValues(tableName, cn.Database, cn.DataSource, MethodInfo.GetCurrentMethod().Name , methodName, dbEntities);
           
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(_cn))
            {
                bulkCopy.DestinationTableName = tableName;
                bulkCopy.BulkCopyTimeout=0;
                bulkCopy.BatchSize = 1000;
                try
                {
                    // Write from the source to the destination.
                    _cn.Open();
                    bulkCopy.WriteToServer(table);
                    int rowsCopied = SqlBulkCopyHelper.GetRowsCopied(bulkCopy);
                    EndQuery(dbEntities, rowsCopied);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    ErrorQuery(dbEntities, ex.Message);
                    result = -1;
                }

                finally
                {
                    _cn.Close();
                }

            }
            return result;
        }

        /// <summary>
        /// Helper class que procesa SqlBulkCopy class
        /// </summary>
        static class SqlBulkCopyHelper
        {
            static FieldInfo rowsCopiedField = null;

            /// <summary>
            /// Devuelve el numero de filas copiadas de un objeto bulk en cuestion
            /// </summary>
            /// <param name="bulkCopy">The bulk copy.</param>
            /// <returns></returns>
            public static int GetRowsCopied(SqlBulkCopy bulkCopy)
            {
                if (rowsCopiedField == null)
                {
                    rowsCopiedField = typeof(SqlBulkCopy).GetField("_rowsCopied", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
                }

                return (int)rowsCopiedField.GetValue(bulkCopy);
            }
        }

        /// <summary>
        /// Método que ejecuta una consulta escalar
        /// </summary>
        /// <param name="cmd">Objeto Command</param>
        /// <param name="connectionName">Nombre de Cadena de Conexión</param>
        /// <param name="dbEntities">Objeto de Mensajes que regresa la consulta</param>
        /// <returns>Resultado entero de la consulta</returns>
        protected int ExecuteScalar(SqlCommand cmd, string connectionName = null, DBEntities dbEntities = null)
        {
            // Validamos consulta
            if (!IsValidCommand(cmd.CommandText)) return -1;

            // Inicializamos
            int result = 0;

            // Asignamos conexion y comando
            cmd.Connection = GetConnection(connectionName);
            cmd.CommandType = this.CommandType;
            cmd.CommandTimeout = this.TimeOut;

            // Asignamos valores de seguimiento
            this.SetDBValues(cmd, this.currentConexionName, MethodInfo.GetCurrentMethod().Name, dbEntities);

            try
            {
                cmd.Connection.Open();
                result = Convert.ToInt32(cmd.ExecuteScalar());
                EndQuery(dbEntities, result);

            }
            catch (Exception ex)
            {
                // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                if (cmd.Connection.State != ConnectionState.Closed) cmd.Connection.Close();
                result = -1;
                ErrorQuery(dbEntities, ex.Message);
            }
            finally
            {
                // Si no se está usando Disposable y la conexion no esta cerrada, la cerramos
                if (!this.IsDisposable && cmd.Connection.State != ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }

            return result;
        }

        // ----------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------


        #region Query
        /// <summary>
        /// Inicializa los valores de DBEntities y agrega registro al log
        /// </summary>
        /// <param name="dbEntities"></param>
        /// <param name="command"></param>
        /// <param name="conexionName"></param>
        /// <param name="methodName"></param>
        private void SetDBValues(SqlCommand command, string conexionName, string methodName, DBEntities dbEntities = null)
        {
            // Verificamos instancia
            if (dbEntities == null) dbEntities = new DBEntities();

            // Asignamos valores
            dbEntities.Query = GetQuery(command);
            dbEntities.Connection = conexionName;
            dbEntities.Methods.Add(methodName);
            dbEntities.DataBase = (dbEntities.Connection == null) ? string.Empty : command.Connection.Database;

            if (Configuration.SQLSave)
            {
                SaveLineInFile(command.Connection.DataSource + " - " + dbEntities.DataBase + " - " + dbEntities.Query);
            }
        }


        /// <summary>
        /// Inicializa los valores de DBEntities y agrega registro al log
        /// </summary>
        /// <param name="dbEntities"></param>
        /// <param name="dataBase"></param>
        /// <param name="table"></param>
        /// <param name="conexionName"></param>
        /// <param name="methodName"></param>
        private void SetDBBulkValues(string table, string dataBase, string conexionName, string methodName, string methodNameFrom, DBEntities dbEntities = null)
        {
            // Verificamos instancia
            if (dbEntities == null) dbEntities = new DBEntities();

            // Asignamos valores
            dbEntities.Query = table;
            dbEntities.Connection = conexionName;
            dbEntities.Methods.Add(methodName);
            dbEntities.DataBase = (dbEntities.Connection == null) ? string.Empty : dataBase;

            if (Configuration.SQLSave)
            {
                SaveLineInFile(conexionName + " - " + dbEntities.DataBase + " - " + dbEntities.Query + " From: " + methodNameFrom );
            }
        }

        private void SetCSVValues(string FileName,string conexionName)
        {
            if (Configuration.SQLSave)
            {
                SaveLineInFile(conexionName + " - " + FileName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="comm"></param>
        /// <returns></returns>
        private string GetQuery(SqlCommand comm)
        {
            if (!Configuration.SQLQuerysProcessing) return string.Empty;

            if (comm.CommandType != CommandType.StoredProcedure) return comm.CommandText;

            bool isFirstParam = true;
            string command = comm.CommandText;
            foreach (SqlParameter param in comm.Parameters)
            {
                if (!isFirstParam) command += ",";

                switch (param.SqlDbType)
                {
                    case SqlDbType.Char:
                    case SqlDbType.Text:
                    case SqlDbType.VarChar:
                    case SqlDbType.NVarChar:
                        command += string.Format(" {0}='{1}'", param.ParameterName, param.Value);
                        break;

                    case SqlDbType.Int:
                    case SqlDbType.BigInt:
                    case SqlDbType.Decimal:
                    case SqlDbType.Float:
                    case SqlDbType.Money:
                    case SqlDbType.Real:
                    case SqlDbType.SmallInt:
                    case SqlDbType.SmallMoney:
                    case SqlDbType.TinyInt:
                        command += string.Format(" {0}={1}", param.ParameterName, param.Value);
                        break;

                    case SqlDbType.Bit:
                        command += string.Format(" {0}={1}", param.ParameterName, ((bool)param.Value) ? 1 : 0);
                        break;

                    case SqlDbType.DateTime:
                    case SqlDbType.Date:
                    case SqlDbType.SmallDateTime:
                        if (param.Value == null || param.Value.ToString() == "")
                            command += string.Format(" {0}={1}", param.ParameterName, "null");
                        else
                            command += string.Format(" {0}='{1}'", param.ParameterName, (Convert.ToDateTime(param.Value)).ToString("yyyy-MM-dd HH:mm tt")); //((DateTime)param.Value).ToString("yyyy-MM-dd HH:mm tt"));
                        break;

                    case SqlDbType.Structured:
                        command += string.Format(" {0}={1} ", param.ParameterName, "Definido por el usuario");
                        break;
                    default:
                        throw new Exception("Tipo de parametro no soportado. Reportar con Administrador.");
                }

                if (isFirstParam) isFirstParam = false;
            }

            return command;
        }

        /// <summary>
        /// Genera el registro de un error
        /// </summary>
        /// <param name="dbEntities">Clase de servicio de DAC</param>
        /// <param name="error">Error</param>
        private void ErrorQuery(DBEntities dbEntities, string error)
        {
            dbEntities = dbEntities ?? new DBEntities();
            dbEntities.Message = error;

            if (!Configuration.SQLQuerysProcessing) return;

            if (Configuration.SQLSave)
            {
                SaveLineInFile(dbEntities.Message, true);
            }

            if (Configuration.SendError)
            {
                SendErrorMessage(dbEntities.Message, "SQL Error Message");
            }

        }

        private void ErrorLoadCSV(string FileName, string error)
        {
            if (Configuration.SQLSave)
            {
                SaveLineInFile("Error loading csv: " + FileName + " - " + error);
            }
        }

        /// <summary>
        /// Guarda el Log de la consulta SQL realizada.
        /// </summary>
        /// <param name="dbEntities">Entidad de Base de Datos.</param>
        /// <param name="rows">Número de registros afectados.</param>
        private void EndQuery(DBEntities dbEntities, int rows)
        {
            dbEntities = dbEntities ?? new DBEntities();

            if (!Configuration.SQLQuerysProcessing) return;
            if (Configuration.SQLSave)
            {
                string methods = string.Empty;
                foreach (string method in dbEntities.Methods)
                {
                    methods = method + "-";
                }
                string tmp = string.Format("{0} - filas: {1}.",
                        methods,
                        rows);
                SaveLineInFile(tmp);
            }
        }

        private void EndCSVLoad(string FileName, int rows)
        {
            if (Configuration.SQLSave)
            {
                string tmp = string.Format("{0} - filas: {1}.",
                    FileName,
                    rows);
                SaveLineInFile(tmp);
            }
        }

        /// <summary>
        /// Guarda el Log de una petición XML.
        /// </summary>
        /// <param name="url">URL de petición al XML.</param>
        /// <param name="method">Método del XML que será invocado.</param>
        /// <param name="request">Cadena de petición XML.</param>
        protected void StartWSRequest(string url, string method, string request)
        {
            if (Configuration.SQLSave)
            {
                string tmp = string.Format("Start: {0} - {1} - {2}.",
                        url,
                        method,
                        request);
                SaveLineInFile(tmp);
            }
        }

        /// <summary>
        /// Guarda el Log del término de una petición XML.
        /// </summary>
        /// <param name="url">URL de petición al XML.</param>
        /// <param name="method">Método del XML que será invocado.</param>
        /// <param name="response">Cadena de respuesta XML.</param>
        /// <param name="request">Cadena de petición XML.</param>
        /// <param name="error">Si existió error en la petición</param>
        protected void EndWSRequest(string url, string method, string response = "", string request = "", bool error = false)
        {
            if (error)
            {
                if (Configuration.SendError)
                {
                    SaveLineInFile(string.Format("{0} - {1}.\n Request: {2}.\n Response: {3}",
                                                   url,
                                                   method,
                                                   request,
                                                   response), true);
                    SendErrorMessage(string.Format("{0} - {1}.\n\n Request:\n {2}.\n\n Response:\n {3}",
                                                   url,
                                                   method,
                                                   request,
                                                   response), "XML Error Message");
                }
            }
            else
            {
                if (Configuration.SQLSave)
                {
                    SaveLineInFile(string.Format("End: {2} - {0} - {1}.\n {3}",
                                                 url,
                                                 method,
                                                 _now.ToString("yyyyMMdd"),
                                                 response));
                }
            }
        }

        #endregion

        #region Servicio

        /// <summary>
        /// Se revisa el comando para asegurar que no haya inyeccion de SQL
        /// </summary>
        /// <param name="command">Comando actual</param>
        /// <returns>Regresa cierto si el comando es correcto o no</returns>
        public bool IsValidCommand(string command)
        {
            // Para verificar nos basamos en minusculas
            command = command.ToLower();

            
            // Rechazamos caracter (Teo)
            if (command.IndexOf("%3C") >= 0) return false;

            // Rechazamos IFrames
            if (command.IndexOf("iframe") >= 0) return false;

            // En cualquier consulta rechazamos cambios
            if (command.IndexOf("select ") >= 0
                    && (command.IndexOf("insert ") >= 0 ||
                        command.IndexOf("delete ") >= 0 ||
                        command.IndexOf("update ") >= 0)) return false;
            return true;
        }

        /// <summary>
        /// Registra en el archivo de bitacora un evento.
        /// </summary>
        /// <param name="text">mensaje del evento</param>
        /// <param name="error">tipo de Log</param>
        private void SaveLineInFile(string text, bool error = false)
        {
            try
            {
                _now = DateTime.Now;
                text = _now.ToString("yyyyMMdd, h:mm:ss tt") + " - " + text;

                string pathFile = error ? _errorPath : _path;
                string mailPathFile = _mailPath;

                string pathDirectory = error ? Configuration.SQLErrorPath.Substring(0, Configuration.SQLErrorPath.LastIndexOf("\\")) : Configuration.SQLFilePath.Substring(0, Configuration.SQLFilePath.LastIndexOf("\\"));
                string mailPathDirectory = Configuration.SQLLogsPath.Substring(0, Configuration.SQLLogsPath.LastIndexOf("\\"));

                using (CommonDataAccess common = new CommonDataAccess())
                {
                    if (pathDirectory != "")
                        if (!Directory.Exists(pathDirectory))
                            Directory.CreateDirectory(pathDirectory);

                    common.SaveLineInFile(text, pathFile);

                    // Agregar a un solo archivo el log y los errores (si el directorio no existe lo crea).
                    if (mailPathDirectory != "")
                    {
                        if (!Directory.Exists(mailPathDirectory))
                            Directory.CreateDirectory(mailPathDirectory);
                        common.SaveLineInFile(text, mailPathFile);
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Envía correo con el mensaje de error.
        /// </summary>
        /// <param name="text">mensaje del evento.</param>
        /// <param name="subject">Asunto del correo electrónico.</param>
        private void SendErrorMessage(string text, string subject)
        {
            try
            {
                string servername = Configuration.GetServerName;
                _now = DateTime.Now;
                text = _now.ToString("yyyyMMdd, h:mm:ss tt") + " - " + servername + " - " + text;

                MailMessage message = new MailMessage();
                string[] messageToArray = BI.Configuration.eMailList.Split(';');
                foreach (string messageTo in messageToArray)
                {
                    if (!string.IsNullOrEmpty(messageTo))
                    {
                        message.To.Add(new MailAddress(messageTo));
                    }
                }
                message.From = new MailAddress("sistema@bestday.com", "BI Administrator");
                message.Subject = subject + " - " + servername;
                message.IsBodyHtml = false;

                message.Body = text;

                MailValues mailValues = new MailValues { Message = message, SendBySQL = false };

                SmtpClient smtpMail = new SmtpClient("smtp.mailprotector.net", 587) { Credentials = new NetworkCredential("sistema@bestday.com", "bdsys01") };
                smtpMail.Send(mailValues.Message);
            }
            catch
            {
            }
        }

        #endregion
        #endregion

    }
}
