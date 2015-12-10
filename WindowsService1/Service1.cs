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
    public partial class Service1 : ServiceBase
    {

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public Service1()
        {
            InitializeComponent();
        }

        //Cógigo que é executado ao solicitar que o serviço inicie.
        protected override void OnStart(string[] args)
        {
            // Create the source, if it does not already exist.
            if (!EventLog.SourceExists("Disponibilidade"))
            {
                EventLog.CreateEventSource("Disponibilidade", "Disponibilidade_log");
            }

            //Monitor de arquivos - File Watch
            monitorDeArquivos monitor2g = new monitorDeArquivos();
            Thread threadMonitor2g = new Thread(() => monitor2g.monitora_arquivos(ConfigurationManager.AppSettings["Path2G"],ConfigurationManager.AppSettings["Filter"]));

            monitorDeArquivos monitor3g = new monitorDeArquivos();
            Thread threadMonitor3g = new Thread(() => monitor3g.monitora_arquivos(ConfigurationManager.AppSettings["Path3G"],ConfigurationManager.AppSettings["Filter"]));

            monitorDeArquivos monitor4g = new monitorDeArquivos();
            Thread threadMonitor4g = new Thread(() => monitor4g.monitora_arquivos(ConfigurationManager.AppSettings["Path4G"],ConfigurationManager.AppSettings["Filter"]));

            threadMonitor2g.Start();
            threadMonitor3g.Start();
            threadMonitor4g.Start();

            //Monitor de arquivos - Timer
            Thread threadTimer4G = new Thread(() => timer.monitora_arquivo_timer(ConfigurationManager.AppSettings["Path4G"], 60000,"*.*"));
            threadTimer4G.Start();

            Thread threadTimer3G = new Thread(() => timer.monitora_arquivo_timer(ConfigurationManager.AppSettings["Path3G"], 60000,"*.*"));
            threadTimer3G.Start();

            Thread threadTimer2G = new Thread(() => timer.monitora_arquivo_timer(ConfigurationManager.AppSettings["Path2G"], 60000, "*.*"));
            threadTimer2G.Start();

        }

        //Código que é executado ao solicitar que o serviço pare.
        protected override void OnStop()
        {
            //Registra o texto no Log de serviço do sistema.
            EventLog.WriteEntry("Meu serviço foi finalizado!!!", EventLogEntryType.Warning);
        }

    }
}
