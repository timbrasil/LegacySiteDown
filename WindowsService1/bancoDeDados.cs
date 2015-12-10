using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace WindowsService1
{
    class bancoDeDados
    {
        //Pega id de PK pra próxima adição no bd
        public static int get_id_database(MySqlConnection bdConn, String Database, String column)
        {
            //Pegar ID Atual:
            String cmdText = "SELECT " + column + " FROM " + Database + " ORDER BY " + column + " DESC LIMIT 1;";
            MySqlCommand cmd = new MySqlCommand(cmdText, bdConn);
            int id = Convert.ToInt32(cmd.ExecuteScalar());
            id = id + 1;
            return id;
        }

        //Recebe instrução para inserção de dados no BD.
        public static void inserir_dados_bd(MySqlConnection bdConn, EventLog eventLog, string comando)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand(comando, bdConn);
                cmd.ExecuteScalar();
            }
            catch (Exception e)
            {
                eventLog.WriteEntry("Erro ao executar a instrução:\n" + comando + "\nException: " + e, EventLogEntryType.Error);
            }
        }
    }
}
