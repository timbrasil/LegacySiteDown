using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsService1
{
    class timer
    {
        public static void monitora_arquivo_timer(string dir, int time, string extension = "*.*")
        {
            try
            {
                string[] files = Directory.GetFiles(dir, extension);
                int maxParallel = Convert.ToInt32(ConfigurationManager.AppSettings["MaxParallel"]);
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = maxParallel }, file =>
                {
                    monitorDeArquivos.ler_arquivo(file);
                });
            }
            catch (Exception e)
            {

            }
            finally
            {
                Thread.Sleep(time);
                Thread threadTimer = new Thread(() => timer.monitora_arquivo_timer(dir, time));
                threadTimer.Start();
            }
            
        }

    }
}
