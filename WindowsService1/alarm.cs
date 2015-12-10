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
using MySql.Data.MySqlClient;
using System.Xml; //Adicionar referencia a MySql.Data.
using System.Text.RegularExpressions;

namespace WindowsService1
{
    class alarm
    {

        private MySqlConnection bdConn = new MySqlConnection(ConfigurationManager.AppSettings["ConexaoMySQL"]);
        private DataSet bdDataSet = new DataSet();
        private EventLog eventLog = new EventLog();

        public alarm()
        {
            this.eventLog.Source = "Disponibilidade";
        }

        public bool salvar_4GAlarm(String caminho)
        {
            bool success = true;

            try
            {
                //Tenta ler arquivo XML.
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.XmlResolver = null;
                xmlDoc.Load(caminho);

                //Abre conexão com o BD.
                this.bdConn.Open();

                //Declara variaveis.
                String sql;
                MySqlCommand command;
                DateTime horaAtualizacao;
                horaAtualizacao = DateTime.Now;

                MySqlTransaction transaction = this.bdConn.BeginTransaction();
                try
                {
                    string[] caminhoArr = caminho.Split('\\');
                    string file = caminhoArr.Last().ToString();
                    String site = file.Substring(0, file.IndexOf("_ALARM_LOG"));
                    foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                    {
                        if (node.Name == "LogRecord")
                        {
                            Int32 logId = Convert.ToInt32(node.Attributes["number"].InnerText);

                            Int32 year = Convert.ToInt32(node["TimeStamp"]["year"].InnerText);
                            Int32 month = Convert.ToInt32(node["TimeStamp"]["month"].InnerText);
                            Int32 day = Convert.ToInt32(node["TimeStamp"]["day"].InnerText);
                            Int32 hour = Convert.ToInt32(node["TimeStamp"]["hour"].InnerText);
                            Int32 minute = Convert.ToInt32(node["TimeStamp"]["minute"].InnerText);
                            Int32 second = Convert.ToInt32(node["TimeStamp"]["second"].InnerText);
                            DateTime eventTime = new DateTime(year, month, day, hour, minute, second);
                            eventTime = eventTime.AddHours(-3);

                            String recordContent = node["RecordContent"].InnerText;
                            String[] recordContent_split = recordContent.Split(';');

                            String setor = "";
                            try
                            {
                                setor = recordContent_split[4].Substring(recordContent_split[4].IndexOf("EUtranCellFDD=") + "EUtranCellFDD=".Length, 9);
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                setor = site;
                            }
                            String mo_context_id = "";
                            try
                            {
                                Int32 inicio = recordContent_split[13].IndexOf("MeContext=") + "MeContext=".Length;
                                Int32 fim = recordContent_split[13].IndexOf(',', inicio);
                                mo_context_id = recordContent_split[13].Substring(inicio, fim - inicio);
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                try
                                {
                                    Int32 inicio = recordContent_split[13].IndexOf("MeContext=") + "MeContext=".Length;
                                    mo_context_id = recordContent_split[13].Substring(inicio);
                                }
                                catch (ArgumentOutOfRangeException ex1)
                                {
                                    mo_context_id = null;
                                }
                                
                            }
                            Int32 severidade = Convert.ToInt32(recordContent_split[9]);
                            String alarme = recordContent_split[10];
                            String detalhe = "";
                            switch (alarme)
                            {
                                case "Remote IP Address Unreachable":
                                    int index = Array.IndexOf(recordContent_split, "unreachableIpAddress");
                                    detalhe = recordContent_split[index] + ": " + recordContent_split[index+1];
                                    break;
                                default:
                                    detalhe = recordContent_split[12];
                                    break;
                            }

                            //Inserir dados no BD.
                            if (severidade != 6)
                            {
                                sql = "INSERT IGNORE INTO `disponibilidade`.`alarmes` (`logId`, `moContextId`, `eventTime`, `tecnologia`, `site`, `setor`, `alarme`, `severidade`, `detalhe`, `recordContent`, `ultimaAtualizacao`) " +
                                    "VALUES (@logId, @moContextId, @eventTime, '4G', @site, @setor, @alarme, @severidade, @detalhe, @recordContent, @nowTime)";
                                command = new MySqlCommand(sql, this.bdConn, transaction);
                                command.Prepare();
                                command.Parameters.AddWithValue("@logId", logId);
                                command.Parameters.AddWithValue("@moContextId", mo_context_id);
                                command.Parameters.AddWithValue("@eventTime", eventTime.ToString("yyyy-MM-dd HH:mm:ss"));
                                command.Parameters.AddWithValue("@site", site);
                                command.Parameters.AddWithValue("@setor", setor);
                                command.Parameters.AddWithValue("@alarme", alarme);
                                command.Parameters.AddWithValue("@severidade", severidade);
                                command.Parameters.AddWithValue("@detalhe", detalhe);
                                command.Parameters.AddWithValue("@nowTime", horaAtualizacao.ToString("yyyy-MM-dd HH:mm:ss"));
                                command.Parameters.AddWithValue("@recordContent", recordContent);
                                command.ExecuteNonQuery();
                            }//TODO: Else para casos de Severidade 6 (Close Time)    
                        }
                    }
                }
                catch (Exception e)
                {
                    success = false;
                    eventLog.WriteEntry("Erro ao inserir dados do arquivo " + caminho + "!\n" + e);
                }
                finally
                {
                    transaction.Commit();
                }            
            }
            catch (Exception e)
            {
                if (e is XmlException)
                {
                    eventLog.WriteEntry("Erro ao armazenar dados do arquivo " + caminho + "!\nException: " + e, EventLogEntryType.Error);
                    success = true;
                }
                else
                {
                    eventLog.WriteEntry("Erro ao armazenar dados do arquivo " + caminho + "!\nException: " + e, EventLogEntryType.Error);
                    success = false;
                }
            }
            finally
            {
                try
                {
                    this.bdConn.Close();
                }
                catch (Exception e)
                {
                    eventLog.WriteEntry("Erro ao fechar conexão de dados." + e,EventLogEntryType.Error);
                }
            }
            return success;
        }

        public bool salvar_3GAlarm(String caminho)
        {
            bool success = true;
            try
            {
                //Tenta ler arquivo XML.
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.XmlResolver = null;
                xmlDoc.Load(caminho);

                //Abre conexão com o BD.
                this.bdConn.Open();

                //Declara variaveis.
                String sql;
                MySqlCommand command;
                DateTime horaAtualizacao;
                horaAtualizacao = DateTime.Now;
                MySqlTransaction transaction = this.bdConn.BeginTransaction();
                try
                {
                    string[] caminhoArr = caminho.Split('\\');
                    string file = caminhoArr.Last().ToString();
                    String site = file.Substring(0, file.IndexOf("_ALARM_LOG"));
                    foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                    {
                        if (node.Name == "LogRecord")
                        {
                            Int32 logId = Convert.ToInt32(node.Attributes["number"].InnerText);

                            Int32 year = Convert.ToInt32(node["TimeStamp"]["year"].InnerText);
                            Int32 month = Convert.ToInt32(node["TimeStamp"]["month"].InnerText);
                            Int32 day = Convert.ToInt32(node["TimeStamp"]["day"].InnerText);
                            Int32 hour = Convert.ToInt32(node["TimeStamp"]["hour"].InnerText);
                            Int32 minute = Convert.ToInt32(node["TimeStamp"]["minute"].InnerText);
                            Int32 second = Convert.ToInt32(node["TimeStamp"]["second"].InnerText);
                            DateTime eventTime = new DateTime(year, month, day, hour, minute, second);
                            eventTime = eventTime.AddHours(-3);

                            String recordContent = node["RecordContent"].InnerText;
                            String[] recordContent_split = recordContent.Split(';');

                            String central = "";
                            try
                            {
                                Match match = Regex.Match(recordContent_split[13], "SubNetwork=(.){6},", RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    Int32 inicio = match.Value.IndexOf("SubNetwork=") + "SubNetwork=".Length;
                                    Int32 fim = match.Value.IndexOf(',', inicio);
                                    central = match.Value.Substring(inicio, fim - inicio);
                                }
                                else
                                {
                                    central = null;
                                }
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                central = null;
                            }

                            String mo_context_id = "";
                            try
                            {
                                Int32 inicio = recordContent_split[13].IndexOf("MeContext=") + "MeContext=".Length;
                                Int32 fim = recordContent_split[13].IndexOf(',', inicio);
                                mo_context_id = recordContent_split[13].Substring(inicio, fim - inicio);
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                try
                                {
                                    Int32 inicio = recordContent_split[13].IndexOf("MeContext=") + "MeContext=".Length;
                                    mo_context_id = recordContent_split[13].Substring(inicio);
                                }
                                catch (ArgumentOutOfRangeException ex1)
                                {
                                    mo_context_id = null;
                                }

                            }
                            Int32 severidade = Convert.ToInt32(recordContent_split[9]);
                            String alarme = recordContent_split[10];
                            String detalhe = "";
                            switch (alarme)
                            {
                                case "Remote IP Address Unreachable":
                                    int index = Array.IndexOf(recordContent_split, "unreachableIpAddress");
                                    detalhe = recordContent_split[index] + ": " + recordContent_split[index + 1];
                                    break;
                                default:
                                    detalhe = recordContent_split[12];
                                    break;
                            }

                            //Inserir dados no BD.
                            if (severidade != 6)
                            {
                                sql = "INSERT IGNORE INTO `disponibilidade`.`alarmes` (`logId`, `moContextId`, `eventTime`, `tecnologia`, `site`, `alarme`, `severidade`, `detalhe`,`recordContent`, `central`, `ultimaAtualizacao`) " +
                                    "VALUES (@logId, @moContextId, @eventTime, '3G', @site, @alarme, @severidade, @detalhe, @recordContent, @central, @nowTime)";
                                command = new MySqlCommand(sql, this.bdConn, transaction);
                                command.Prepare();
                                command.Parameters.AddWithValue("@logId", logId);
                                command.Parameters.AddWithValue("@moContextId", mo_context_id);
                                command.Parameters.AddWithValue("@eventTime", eventTime.ToString("yyyy-MM-dd HH:mm:ss"));
                                command.Parameters.AddWithValue("@site", site);
                                command.Parameters.AddWithValue("@central", central);
                                command.Parameters.AddWithValue("@alarme", alarme);
                                command.Parameters.AddWithValue("@severidade", severidade);
                                command.Parameters.AddWithValue("@detalhe", detalhe);
                                command.Parameters.AddWithValue("@nowTime", horaAtualizacao.ToString("yyyy-MM-dd HH:mm:ss"));
                                command.Parameters.AddWithValue("@recordContent", recordContent);
                                command.ExecuteNonQuery();
                            }//TODO: Else para casos de Severidade 6 (Close Time)
                        }
                    }
                }
                catch (Exception e)
                {
                    eventLog.WriteEntry("Erro ao inserir dados do arquivo " + caminho + "!\n" + e);
                    success = false;
                }
                finally
                {
                    transaction.Commit();
                }  
            }
            catch (Exception e)
            {
                if (e is XmlException)
                {
                    eventLog.WriteEntry("Erro ao armazenar dados do arquivo " + caminho + "!\nException: " + e, EventLogEntryType.Error);
                    success = true;
                }
                else
                {
                    eventLog.WriteEntry("Erro ao armazenar dados do arquivo " + caminho + "!\nException: " + e, EventLogEntryType.Error);
                    success = false;
                }
            }
            finally
            {
                eventLog.WriteEntry("Arquivo " + caminho + " inserido!\n");
                try
                {
                    this.bdConn.Close();
                    //eventLog.WriteEntry("Conexão de Dados 4G fechada!");
                }
                catch (Exception e)
                {
                    eventLog.WriteEntry("Erro ao fechar conexão de dados." + e, EventLogEntryType.Error);
                }
            }
            return success;
        }

        public void atualizaConsultaMemoria()
        {
            try
            {
                //Abre conexão com o BD.
                this.bdConn.Open();

                String sql;
                MySqlCommand command;

                sql = "TRUNCATE `disponibilidade`.`alarmes_ip_address_unreachable`;";
                command = new MySqlCommand(sql, this.bdConn);
                command.ExecuteNonQuery();

                sql = "INSERT INTO disponibilidade.alarmes_ip_address_unreachable (eventTime,site,cidade,ip_mme,nome_mme) SELECT eventTime, "+
                    "alarmes.site, cidade.nome as 'cidade', TRIM(LEADING 'unreachableIpAddress: ' FROM alarmes.detalhe), mme.nome "+
                    "FROM disponibilidade.alarmes alarmes left join central.sites sites on alarmes.site = sites.nome left join central.cidade cidade "+
                    "on sites.cidade = cidade.id left join central.mme mme on TRIM(LEADING 'unreachableIpAddress: ' FROM alarmes.detalhe)=mme.ip WHERE "+
                    "alarmes.alarme LIKE 'Remote IP Address Unreachable' and eventTime > now() - interval 1 month order by eventTime;";
                command = new MySqlCommand(sql, this.bdConn);
                command.ExecuteNonQuery();

            }
            catch (Exception e)
            {
                eventLog.WriteEntry("Erro ao atualizar tabela de memória.\n"+e.ToString(),EventLogEntryType.Error);
            }

        }
    }
}
