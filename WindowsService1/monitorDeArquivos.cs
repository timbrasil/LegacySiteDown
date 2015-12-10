using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsService1
{
    class monitorDeArquivos
    {
        FileSystemWatcher monitor = new FileSystemWatcher();
        private EventLog eventLog = new EventLog();

        public monitorDeArquivos()
        {
            this.eventLog.Source = "Disponibilidade";
        }

        public void monitora_arquivos(string path, string filtro = "*.*"){
            try
            {
                //this.eventLog.WriteEntry("Iniciando monitoramento de arquivo com extensão " + filtro + "\nArquivo: " + path);
                //Variaveis
                string caminho = path;

                // Create a new FileSystemWatcher and set its properties.
                //FileSystemWatcher monitor = new FileSystemWatcher();
                this.monitor.Path = caminho;

                /* Watch for changes in LastAccess and LastWrite times, and
                the renaming of files or directories. */
                this.monitor.NotifyFilter = NotifyFilters.LastWrite;

                // Only watch text files.
                this.monitor.Filter = filtro;

                // Add event handlers.
                this.monitor.Changed += new FileSystemEventHandler(this.OnChanged);                

                // Begin watching.
                this.monitor.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                this.eventLog.WriteEntry("Erro ao abrir monitoramento para a pasta:  " + path + "\nFiltro: "+ filtro +"\nErro: " + e,EventLogEntryType.Error);
            }
        }

        //Especifica a ação em caso de alteração de arquivo especificado por parametro.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            //Especifica quando um arquivo é alterado ou criado.
            FileInfo objFileInfo = new FileInfo(e.FullPath);
            if (!objFileInfo.Exists) return;

            //this.eventLog.WriteEntry(e.FullPath + " " + e.ChangeType, EventLogEntryType.Information);
            ler_arquivo(e.FullPath);
        }

        //Lê o arquivo de texto.
        public static void ler_arquivo(string caminho, int loop = 1)
        {
            EventLog eventLog = new EventLog();
            eventLog.Source = "Disponibilidade";

            FileInfo objFileInfo = new FileInfo(caminho);
            if (!objFileInfo.Exists) return;

            try
            {
                //Objeto que realizara a leitura.
                StreamReader objReader = new StreamReader(caminho);

                //eventLog.WriteEntry("Lendo arquivo " + caminho);

                if (analisar_arquivo(objReader,caminho))
                {
                    objReader.Close();
                    System.IO.File.Delete(caminho);
                }
                else
                {
                    objReader.Close();
                }
            }
            catch (Exception e)
            {
                if (loop <= 100)
                {
                    int sleep = 5000;
                    //eventLog.WriteEntry("Erro ao abrir o arquivo " + caminho + "\nTentando novamente em " + sleep / 1000 + " segundos\n" + e.Message,EventLogEntryType.Warning);
                    System.Threading.Thread.Sleep(sleep);
                    loop++;
                    monitorDeArquivos.ler_arquivo(caminho, loop);
                }
                else
                {
                    eventLog.WriteEntry("Erro ao abrir o arquivo: " + caminho + "\nException: " + e, EventLogEntryType.Error); //Em caso de falha ao tentar ler o arquivo.
                }
            }
        }

        public static Boolean analisar_arquivo(StreamReader objReader, String caminho)
        {
            EventLog eventLog = new EventLog();
            eventLog.Source = "Disponibilidade";

            //Determina o validador que está configurado no arquivo de configurações.
            string validador4G = ConfigurationManager.AppSettings["4GKey"];
            string validadorAlarm = ConfigurationManager.AppSettings["AlarmKey"];
            string validador4GRanSharing = ConfigurationManager.AppSettings["4G_RSKey"];
            string validador3G = ConfigurationManager.AppSettings["3GKey"];
            string validador2G = ConfigurationManager.AppSettings["2GKey"];

            string arrText = objReader.ReadLine();

            //Testa a validação do arquivo XML
            if (arrText.Contains(validador4G))
            {
                //Caso seja 4G, executa o código abaixo.
                //eventLog.WriteEntry("Arquivo 4G validado!", EventLogEntryType.SuccessAudit);

                disponibilidade disponibilidade4G = new disponibilidade();
                return disponibilidade4G.salvar_disponibilidade4G(objReader);
            }

            //Testa a validação do arquivo como 4G Ran Sharing.
            if (arrText.Contains(validador4GRanSharing))
            {
                //Caso seja 4G Inter-as, executa o código abaixo.
                //eventLog.WriteEntry("Arquivo 4G Ran Sharing validado!", EventLogEntryType.SuccessAudit);

                disponibilidade disponibilidade4GRS = new disponibilidade();
                disponibilidade4GRS.salvar_disponibilidade4GRanSharing(objReader);

                //Retorna que foi realizada a extração com sucesso.
                return true;
            }

            //Testa a validação do arquivo como 3G.
            if (arrText.Contains(validador3G))
            {
                //Caso seja 3G, executa o código abaixo.
                //eventLog.WriteEntry("Arquivo 3G validado!", EventLogEntryType.SuccessAudit);

                disponibilidade disponibilidade3G = new disponibilidade();
                disponibilidade3G.salvar_disponibilidade3G(objReader);

                //Retorna que foi realizada a extração com sucesso.
                return true;
            }

            //Testa a validação do arquivo como 2G.
            if (arrText.Contains(validador2G))
            {
                //Caso seja 2G, executa o código abaixo.
                //eventLog.WriteEntry("Arquivo 2G validado!", EventLogEntryType.SuccessAudit);

                disponibilidade disponibilidade2G = new disponibilidade();
                disponibilidade2G.salvar_disponibilidade2G(objReader);

                //Retorna que foi realizada a extração com sucesso.
                return true;
            }

            //Testa a validação do arquivo XML
            if (arrText.Contains("xml"))
            {
                if (objReader.ReadLine().Contains(validadorAlarm))
                {
                    if (caminho.Contains("4G"))
                    {
                        alarm alarme4g = new alarm();
                        Boolean result = alarme4g.salvar_4GAlarm(caminho);
                        if (result)
                        {
                            alarme4g.atualizaConsultaMemoria();
                        }
                        return result;
                    }
                    if (caminho.Contains("3G"))
                    {
                        alarm alarme4g = new alarm();
                        return alarme4g.salvar_3GAlarm(caminho);
                    }

                }
                return false;
            }

            eventLog.WriteEntry("O arquivo "+caminho+" não pode ser validado!", EventLogEntryType.Warning);
            return false;
        }

    }
}
