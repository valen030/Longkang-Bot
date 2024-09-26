using System;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using Discord.Audio;
using LKGMusicBot.MainEntry;

namespace LKGMusicBot
{
    internal static class Program
    {
        /// <summary>
        /// Create a mutex for a single instance.
        /// </summary>
        private static Mutex INSTANCE_MUTEX = new Mutex(true, "LongkangGang_DiscordMusicBot");
        private static MusicBot BOT = new MusicBot();
        public static FormMain MainEntry = new FormMain(BOT);

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Check if an instance is already running. Remove this block if you want to run multiple instances.
            if (!INSTANCE_MUTEX.WaitOne(TimeSpan.Zero, false))
            {
                MessageBox.Show("The application is already running.");
                return;
            }

            var success = Server.ServerStartup();

            if (!success)
                return;

            try 
            { 
                Application.Run(MainEntry); 
            }
            catch 
            { 
                Console.WriteLine("Failed to run."); 
            }
        }

        // Connect to the bot, or cancel before the connection happens.
        public static void Run() => Task.Run(() => BOT.RunAsync());
        public static void Cancel() => Task.Run(() => BOT.CancelAsync());
        public static void Stop() => Task.Run(() => BOT.StopAsync());
    }
}