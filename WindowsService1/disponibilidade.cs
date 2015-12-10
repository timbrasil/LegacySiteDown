using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

//Necessário para a chamada da função em um serviço.
using System.Threading;

//Necessárias para trabalhar com arquivos de texto.
using System.IO;
using System.Collections;
using System.Security.Permissions;

//Necessário para pegar as configurações de arquivos
//Obs: Necessário adicionar referência para compilação do projeto.
using System.Configuration;

//Necessário para conexão a base de dados.
using MySql.Data.MySqlClient; //Adicionar referencia a MySql.Data.

namespace WindowsService1
{
    class disponibilidade
    {
        private MySqlConnection bdConn = new MySqlConnection(ConfigurationManager.AppSettings["ConexaoMySQL"]);
        private DataSet bdDataSet = new DataSet();
        private EventLog eventLog = new EventLog();

        public disponibilidade()
        {
            this.eventLog.Source = "Disponibilidade";
        }

        //Extrai os dados do 4G do arquivo e armazena no BD.
        public void salvar_disponibilidade4G(StreamReader objReader)
        {
            try
            {
                this.bdConn.Open();

                //Declaração de variaveis.
                string[] fraseSplit, dataCompleta, horaCompleta; //Vetor de string.
                string frase, falha, site, sql;
                int ano, mes, dia, hora, min, idPrincipal, idSecundario;
                MySqlCommand cmd;

                //Verifica se a conexão está aberta.
                if (bdConn.State == ConnectionState.Open)
                {
                    //Prepara dados para tabela Data.
                    idPrincipal = bancoDeDados.get_id_database(this.bdConn, "disponibilidade.date", "idDate");

                    sql = "INSERT INTO `disponibilidade`.`date`(`idDate`,`Date`,`Time`,`Tecnology_idTecnology`) ";
                    sql += "VALUES(@idPrincipal,@data,@hora,@idTecnology)";
                    cmd = new MySqlCommand(sql, this.bdConn);
                    cmd.Parameters.AddWithValue("@idPrincipal", idPrincipal);
                    cmd.Parameters.AddWithValue("@data", System.DateTime.Now.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@hora", System.DateTime.Now.ToString("HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@idTecnology", ConfigurationManager.AppSettings["id4GTecnology"]);

                    cmd.ExecuteNonQuery();

                    while ((frase = objReader.ReadLine()) != null)
                    {
                        fraseSplit = frase.Split(';');
                        //Divisão das colunas.
                        falha = fraseSplit[0];
                        site = fraseSplit[1];

                        //Tratamento da data.
                        dataCompleta = fraseSplit[2].Split(' ');//[0]AAAA-MM-DD [1]HH:MM

                        horaCompleta = dataCompleta[1].Split(':');//[0]HH [1]MM
                        dataCompleta = dataCompleta[0].Split('-');//[0]AAAA [1]MM [2] DD

                        hora = Convert.ToInt32(horaCompleta[0]);
                        min = Convert.ToInt32(horaCompleta[1]);

                        ano = Convert.ToInt32(dataCompleta[0]);
                        mes = Convert.ToInt32(dataCompleta[1]);
                        dia = Convert.ToInt32(dataCompleta[2]);

                        //Inserir dados no BD.
                        idSecundario = bancoDeDados.get_id_database(this.bdConn, "disponibilidade.detalhes_falha", "idDetalhes_Falha");
                        sql = "INSERT INTO `disponibilidade`.`detalhes_falha`(`idDetalhes_Falha`,`Site`,`Event_Time`,`Tipo_falha`,`Date_idDate`)";
                        sql += "VALUES(@id,@site,@eventTime,@tipoFalha,@idDate)";
                        cmd = new MySqlCommand(sql, this.bdConn);
                        cmd.Parameters.AddWithValue("@id", idSecundario);
                        cmd.Parameters.AddWithValue("@site", site);
                        cmd.Parameters.AddWithValue("@eventTime", ano+"-"+mes+"-"+dia+" "+hora+":"+min);
                        cmd.Parameters.AddWithValue("@tipoFalha", falha);
                        cmd.Parameters.AddWithValue("@idDate", idPrincipal);

                        cmd.ExecuteNonQuery();
                    }
                    contaResultado(idPrincipal);

                    this.eventLog.WriteEntry("Disponibilidade 4G inserido com sucesso!", EventLogEntryType.Information);
                }
                else
                {
                    this.eventLog.WriteEntry("4G: A conexão com o banco de dados não esta aberta!", EventLogEntryType.Error);
                }
            }
            catch (Exception e)
            {
                this.eventLog.WriteEntry("Erro ao armazenar dados do 4G!\nException: " + e, EventLogEntryType.Error);
            }
            finally
            {
                try
                {
                    this.bdConn.Close();
                    eventLog.WriteEntry("Conexão de Dados 4G fechada!");
                }
                catch (Exception e)
                {
                    eventLog.WriteEntry("Erro ao fechar conexão de dados." + e);
                }
            }

        }

        //Extrai os dados do 4G ran Sharing do arquivo e armazena no BD.
        public void salvar_disponibilidade4GRanSharing(StreamReader objReader)
        {
            try
            {
                this.bdConn.Open();

                //Declaração de variaveis.
                string site;
                int idPrincipal, idSecundario;
                string horaP, dataP, cmd;

                //Verifica se a conexão está aberta.
                if (bdConn.State == ConnectionState.Open)
                {
                    //Prepara dados para tabela Data.
                    idPrincipal = bancoDeDados.get_id_database(this.bdConn, "disponibilidade.date_ransharing", "id");
                    horaP = System.DateTime.Now.ToString("HH:mm:ss");
                    dataP = System.DateTime.Now.ToString("yyyy/MM/dd");
                    cmd = "INSERT INTO `disponibilidade`.`date_ransharing`(`id`, `data`, `idTecnology`) ";
                    cmd = cmd + "VALUES('" + idPrincipal + "','" + dataP + " " + horaP + "','" + ConfigurationManager.AppSettings["id4GTecnology"] + "');";

                    //Insere os dados na tabela Data.
                    bancoDeDados.inserir_dados_bd(this.bdConn, this.eventLog, cmd);

                    //Insere os dados na tabela detalhes_falha.
                    while ((site = objReader.ReadLine()) != null)
                    {
                        //Inserir dados no BD.
                        idSecundario = bancoDeDados.get_id_database(this.bdConn, "disponibilidade.detalhes_ransharing", "id");
                        cmd = "INSERT INTO `disponibilidade`.`detalhes_ransharing` (`id`, `site`, `id_date_ransharing`) ";
                        cmd = cmd + "VALUES('" + idSecundario + "','" + site + "','" + idPrincipal + "');";
                        bancoDeDados.inserir_dados_bd(this.bdConn, this.eventLog, cmd);
                    }
                    contaResultado(idPrincipal);
                    this.eventLog.WriteEntry("Salvo com sucesso!", EventLogEntryType.Information);
                }
                else
                {
                    this.eventLog.WriteEntry("4GRS: A conexão com o banco de dados não esta aberta!", EventLogEntryType.Error);
                }

            }
            catch (Exception e)
            {
                this.eventLog.WriteEntry("Erro ao armazenar dados do 3G!\nException: " + e, EventLogEntryType.Error);
            }
            finally
            {
                try
                {
                    this.bdConn.Close();
                    eventLog.WriteEntry("Conexão de Dados 4G fechada!");
                }
                catch (Exception e)
                {
                    eventLog.WriteEntry("Erro ao fechar conexão de dados." + e);
                }
            }
        }

        //Extrai os dados do 3G do arquivo e armazena no BD.
        public void salvar_disponibilidade3G(StreamReader objReader)
        {
            try
            {
                this.bdConn.Open();

                //Declaração de variaveis.
                string[] fraseSplit, dataCompleta, horaCompleta; //Vetor de string.
                string frase, falha, site, rnc, downtime;
                int ano, mes, dia, hora, min, idPrincipal, idSecundario;
                string horaP, dataP, cmd;

                //Verifica se a conexão está aberta.
                if (bdConn.State == ConnectionState.Open)
                {
                    //Prepara dados para tabela Data.
                    idPrincipal = bancoDeDados.get_id_database(this.bdConn, "disponibilidade.date", "idDate");
                    horaP = System.DateTime.Now.ToString("HH:mm:ss");
                    dataP = System.DateTime.Now.ToString("yyyy/MM/dd");
                    cmd = "INSERT INTO `disponibilidade`.`date`(`idDate`,`Date`,`Time`,`Tecnology_idTecnology`)";
                    cmd = cmd + "VALUES('" + idPrincipal + "','" + dataP + "','" + horaP + "','" + ConfigurationManager.AppSettings["id3GTecnology"] + "');";

                    //Insere os dados na tabela Data.
                    bancoDeDados.inserir_dados_bd(this.bdConn, this.eventLog, cmd);

                    //Insere os dados na tabela detalhes_falha.
                    while ((frase = objReader.ReadLine()) != null)
                    {
                        fraseSplit = frase.Split(';');
                        //Divisão das colunas.
                        rnc = fraseSplit[0];
                        site = fraseSplit[1];
                        downtime = fraseSplit[3];
                        falha = fraseSplit[4];


                        //Tratamento da data.
                        dataCompleta = fraseSplit[2].Split(' ');//[0]AAAA-MM-DD [1]HH:MM

                        horaCompleta = dataCompleta[1].Split(':');//[0]HH [1]MM
                        dataCompleta = dataCompleta[0].Split('-');//[0]AAAA [1]MM [2] DD

                        hora = Convert.ToInt32(horaCompleta[0]);
                        min = Convert.ToInt32(horaCompleta[1]);

                        ano = Convert.ToInt32(dataCompleta[0]);
                        mes = Convert.ToInt32(dataCompleta[1]);
                        dia = Convert.ToInt32(dataCompleta[2]);

                        //Inserir dados no BD.
                        idSecundario = bancoDeDados.get_id_database(this.bdConn, "disponibilidade.detalhes_falha", "idDetalhes_Falha");
                        cmd = "INSERT INTO `disponibilidade`.`detalhes_falha`(`idDetalhes_Falha`,`Site`,`Concentrador`,`Event_Time`,`Date_idDate`, `Tipo_falha`)";
                        cmd = cmd + "VALUES('" + idSecundario + "','" + site + "','" + rnc + "','" + ano + "-" + mes + "-" + dia + " " + hora + ":" + min + ":00','" + idPrincipal + "','" + falha + "');";
                        bancoDeDados.inserir_dados_bd(this.bdConn, this.eventLog, cmd);
                    }
                    contaResultado(idPrincipal);
                    this.eventLog.WriteEntry("Disponibilidade 3G inserida com sucesso!", EventLogEntryType.Information);
                }
                else
                {
                    this.eventLog.WriteEntry("3G: A conexão com o banco de dados não esta aberta!", EventLogEntryType.Error);
                }
            }
            catch (Exception e)
            {
                this.eventLog.WriteEntry("Erro ao armazenar dados do 3G!\nException: " + e, EventLogEntryType.Error);
            }
            finally
            {
                try
                {
                    this.bdConn.Close();
                    eventLog.WriteEntry("Conexão de Dados 3G fechada!");
                }
                catch (Exception e)
                {
                    eventLog.WriteEntry("Erro ao fechar conexão de dados." + e);
                }
            }

        }

        //Extrai os dados do 2G do arquivo e armazena no BD.
        public void salvar_disponibilidade2G(StreamReader objReader)
        {
            //Declaração de variaveis.
            string[] fraseSplit, dataCompleta, horaCompleta; //Vetor de string.
            string frase, falha, site, bsc, downtime;
            int ano, mes, dia, hora, min, idPrincipal, idSecundario;
            string horaP, dataP, cmd;

            try
            {
                this.bdConn.Open();

                //Verifica se a conexão está aberta.
                if (bdConn.State == ConnectionState.Open)
                {
                    //Prepara dados para tabela Data.
                    idPrincipal = bancoDeDados.get_id_database(this.bdConn, "disponibilidade.date", "idDate");
                    horaP = System.DateTime.Now.ToString("HH:mm:ss");
                    dataP = System.DateTime.Now.ToString("yyyy/MM/dd");
                    cmd = "INSERT INTO `disponibilidade`.`date`(`idDate`,`Date`,`Time`,`Tecnology_idTecnology`)";
                    cmd = cmd + "VALUES('" + idPrincipal + "','" + dataP + "','" + horaP + "','" + ConfigurationManager.AppSettings["id2GTecnology"] + "');";

                    //Insere os dados na tabela Data.
                    bancoDeDados.inserir_dados_bd(this.bdConn, this.eventLog, cmd);

                    //Insere os dados na tabela detalhes_falha.
                    while ((frase = objReader.ReadLine()) != null)
                    {
                        fraseSplit = frase.Split(';');
                        //Divisão das colunas.
                        bsc = fraseSplit[0];
                        site = fraseSplit[1];
                        downtime = fraseSplit[3];
                        falha = fraseSplit[4];


                        //Tratamento da data.
                        idSecundario = bancoDeDados.get_id_database(this.bdConn, "disponibilidade.detalhes_falha", "idDetalhes_Falha");

                        dataCompleta = fraseSplit[2].Split(' ');//[0]AAAA-MM-DD [1]HH:MM
                        if (!falha.Contains("LOC"))
                        {
                            horaCompleta = dataCompleta[1].Split(':');//[0]HH [1]MM
                            dataCompleta = dataCompleta[0].Split('-');//[0]AAAA [1]MM [2] DD

                            hora = Convert.ToInt32(horaCompleta[0]);
                            min = Convert.ToInt32(horaCompleta[1]);

                            ano = Convert.ToInt32(dataCompleta[0]);
                            mes = Convert.ToInt32(dataCompleta[1]);
                            dia = Convert.ToInt32(dataCompleta[2]);

                            cmd = "INSERT INTO `disponibilidade`.`detalhes_falha`(`idDetalhes_Falha`,`Site`,`Concentrador`,`Event_Time`,`Date_idDate`, `Tipo_falha`)";
                            cmd = cmd + "VALUES('" + idSecundario + "','" + site + "','" + bsc + "','" + ano + "-" + mes + "-" + dia + " " + hora + ":" + min + ":00','" + idPrincipal + "','" + falha + "');";
                        }
                        else
                        {
                            cmd = "INSERT INTO `disponibilidade`.`detalhes_falha`(`idDetalhes_Falha`,`Site`,`Concentrador`,`Event_Time`,`Date_idDate`, `Tipo_falha`)";
                            cmd = cmd + "VALUES('" + idSecundario + "','" + site + "','" + bsc + "','" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + idPrincipal + "','" + falha + "');";
                        }
                        //Inserir dados no BD.
                        bancoDeDados.inserir_dados_bd(this.bdConn, this.eventLog, cmd);
                    }
                    contaResultado(idPrincipal);
                    this.eventLog.WriteEntry("Disponibilidade 2G inserida com sucesso!", EventLogEntryType.Information);
                }
                else
                {
                    this.eventLog.WriteEntry("2G: A conexão com o banco de dados não esta aberta!", EventLogEntryType.Error);
                }
            }
            catch (Exception e)
            {
                this.eventLog.WriteEntry("Erro ao armazenar dados do 2G!\nException: " + e, EventLogEntryType.Error);
            }
            finally
            {
                try
                {
                    this.bdConn.Close();
                    eventLog.WriteEntry("Conexão de Dados 2G fechada!");
                }
                catch (Exception e)
                {
                    eventLog.WriteEntry("Erro ao fechar conexão de dados." + e);
                }
            }

        }

        public void contaResultado(int id)
        {
            String sql;
            MySqlCommand cmd;
            sql = "CALL `disponibilidade`.`calculate_quant_id`(@id);";
            cmd = new MySqlCommand(sql, this.bdConn);
            cmd.Parameters.AddWithValue("@id", id);

            cmd.ExecuteNonQuery();
        }


    }
}
