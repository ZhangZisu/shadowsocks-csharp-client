﻿using System;
using System.Collections.Generic;
using System.IO;

using Shadowsocks.Controller;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.Windows.Forms;

namespace Shadowsocks.Model
{
    [Serializable]
    public class Configuration
    {
        public List<Server> configs;

        // when strategy is set, index is ignored
        public string strategy;
        public int index;
        public bool shareOverLan;
        public bool isDefault;
        public int localPort;
        public bool availabilityStatistics;
        public bool autoCheckUpdate;
        public bool autoUpdateFeeds;
        public bool checkPreRelease;
        public bool isVerboseLogging;
        public LogViewerConfig logViewer;
        public ProxyConfig proxy;

        //private static string CONFIG_FILE = "config.json";
        private static string CONFIG_KEY = "config" + Application.StartupPath.GetHashCode();

        public Server GetCurrentServer()
        {
            if (index >= 0 && index < configs.Count)
                return configs[index];
            else
                return GetDefaultServer();
        }

        public static void CheckServer(Server server)
        {
            CheckPort(server.server_port);
            CheckPassword(server.password);
            CheckServer(server.server);
            CheckTimeout(server.timeout, Server.MaxServerTimeoutSec);
        }

        public static Configuration Load()
        {
            try
            {
                //string configContent = File.ReadAllText(CONFIG_FILE);
                RegistryKey key = Util.Utils.OpenRegKey("shadowsocks", false);
                string configContent = (string)key.GetValue(CONFIG_KEY);
                if (configContent == null) configContent = "";
                Configuration config = JsonConvert.DeserializeObject<Configuration>(configContent);
                if (config == null) config = new Configuration()
                {
                    isDefault = true
                };
                else config.isDefault = false;

                if (config.configs == null)
                    config.configs = new List<Server>();
                if (config.configs.Count == 0)
                    config.configs.Add(GetDefaultServer());
                if (config.localPort == 0)
                    config.localPort = 1080;
                if (config.index == -1 && config.strategy == null)
                    config.index = 0;
                if (config.logViewer == null)
                    config.logViewer = new LogViewerConfig();
                if (config.proxy == null)
                    config.proxy = new ProxyConfig();

                config.proxy.CheckConfig();

                return config;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                    Logging.LogUsefulException(e);
                return new Configuration
                {
                    index = 0,
                    isDefault = true,
                    localPort = 1080,
                    autoCheckUpdate = true,
                    configs = new List<Server>()
                    {
                        GetDefaultServer()
                    },
                    logViewer = new LogViewerConfig(),
                    proxy = new ProxyConfig()
                };
            }
        }

        public static void Save(Configuration config)
        {
            if (config.index >= config.configs.Count)
                config.index = config.configs.Count - 1;
            if (config.index < -1)
                config.index = -1;
            if (config.index == -1 && config.strategy == null)
                config.index = 0;
            config.isDefault = false;
            try
            {
                RegistryKey key = Util.Utils.OpenRegKey("shadowsocks", true);
                key.SetValue(CONFIG_KEY, JsonConvert.SerializeObject(config, Formatting.None));
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public static Server GetDefaultServer()
        {
            return new Server();
        }

        private static void Assert(bool condition)
        {
            if (!condition)
                throw new Exception(I18N.GetString("assertion failure"));
        }

        public static void CheckPort(int port)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentException(I18N.GetString("Port out of range"));
        }

        public static void CheckLocalPort(int port)
        {
            CheckPort(port);
            if (port == 8123)
                throw new ArgumentException(I18N.GetString("Port can't be 8123"));
        }

        private static void CheckPassword(string password)
        {
            if (password.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Password can not be blank"));
        }

        public static void CheckServer(string server)
        {
            if (server.IsNullOrEmpty())
                throw new ArgumentException(I18N.GetString("Server IP can not be blank"));
        }

        public static void CheckTimeout(int timeout, int maxTimeout)
        {
            if (timeout <= 0 || timeout > maxTimeout)
                throw new ArgumentException(string.Format(
                    I18N.GetString("Timeout is invalid, it should not exceed {0}"), maxTimeout));
        }
    }
}
