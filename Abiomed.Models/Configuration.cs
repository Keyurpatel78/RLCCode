﻿/*
 * Remote Link - Copyright 2017 ABIOMED, Inc.
 * --------------------------------------------------------
 * Description:
 * Configuration.cs: Configuration Reader
 * --------------------------------------------------------
 * Author: Alessandro Agnello 
*/

using System;
using System.Configuration;
using System.Text;

namespace Abiomed.Models
{
    public class Configuration
    {
        // Default strings to localhost
        private const string localhost = @"localhost";
        private string _deviceStatus = @"http://localhost/api/DeviceStatus";
        private string _imageSend = @"http://localhost/api/Image";
        private int _keepAliveTimer = 5000;
        private int _imageCountdownTimer = 600000;        
        private string _type = @"localhost";
        private string _certLocation = "";
        private int _tcpPort;
        private string _documentDBConnection = @"https://localhost:8081";
        private string _documentDBConnectionPassword = @"C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private string _redisConnect = @"localhost";
        private bool _security = false;
        private string _signalRConnection = @"http://localhost:8080";
        
        #region Constructor
        public Configuration()
        {
            // Configure Sections
            connectionManager();
            optionsManager();
        }
        #endregion

        private void connectionManager()
        {
            var connectionManager = ConfigurationManager.GetSection("ConnectionManager") as System.Collections.Specialized.NameValueCollection;
            _type = connectionManager["RUN"].ToString();
            string WOWZA = connectionManager["WOWZA"].ToString();
            // Configure WOWZA Url
            //rtmp://rlv.abiomed.com
            byte WOWZALength = Convert.ToByte(WOWZA.Length);
            var WOWZABytes = Encoding.ASCII.GetBytes(WOWZA);
            Definitions.StreamVideoControlIndication.Insert(14, WOWZALength);
            Definitions.StreamVideoControlIndication.InsertRange(15, WOWZABytes);
            
            string WEB = connectionManager["WEB"].ToString();
            string RLR = connectionManager["RLR"].ToString();
            string DocDbConnectionUri = connectionManager["DocDBUri"].ToString();
            string DocDbConnectionPwd = connectionManager["DocDBPWD"].ToString();
            string RedisCon = connectionManager["RedisConnect"].ToString();
            bool SecurityStatus = false;
            bool.TryParse(connectionManager["Security"].ToString(), out SecurityStatus);

            _security = SecurityStatus;

            if (_type != @"localhost")
            {
                // Update Strings
                StringBuilder str = new StringBuilder(_deviceStatus);
                str.Replace(localhost, WEB);

                _deviceStatus = str.ToString();

                str = new StringBuilder(_imageSend);
                str.Replace(localhost, WEB);

                _imageSend = str.ToString();

                _documentDBConnection = DocDbConnectionUri;
                _documentDBConnectionPassword = DocDbConnectionPwd;
                _redisConnect = RedisCon;
                _signalRConnection = @"http://13.82.178.248:80";
            }
        }

        private void optionsManager()
        {

            var optionsManager = ConfigurationManager.GetSection("OptionsManager") as System.Collections.Specialized.NameValueCollection;
            _keepAliveTimer = Convert.ToInt32(optionsManager["KeepAliveTimer"].ToString());
            _certLocation = optionsManager["CertKey"].ToString();
            _tcpPort = Convert.ToInt32(optionsManager["TcpPort"].ToString());
            _imageCountdownTimer = Convert.ToInt32(optionsManager["ImageCountdownTimer"].ToString());
        }

        public string DeviceStatus
        {
            get { return _deviceStatus; }
            set { _deviceStatus = value; }
        }

        public string ImageSend
        {
            get { return _imageSend; }
            set { _imageSend = value; }
        }

        public int KeepAliveTimer
        {
            get { return _keepAliveTimer; }
            set { _keepAliveTimer = value; }
        }

        public int ImageCountDownTimer
        {
            get { return _imageCountdownTimer; }
            set { _imageCountdownTimer = value; }
        }

        public string CertLocation
        {
            get { return _certLocation; }
            set { _certLocation = value; }
        }

        public int TcpPort
        {
            get { return _tcpPort; }
            set { _tcpPort = value; }
        }

        public string DocumentDBConnection
        {
            get { return _documentDBConnection; }
            set { _documentDBConnection = value; }
        }

        public string DocumentDBConnectionPassword
        {
            get { return _documentDBConnectionPassword; }
            set { _documentDBConnectionPassword = value; }
        }

        public string RedisConnect
        {
            get { return _redisConnect; }
            set { _redisConnect = value; }
        }

        public bool Security
        {
            get { return _security; }
            set { _security = value; }
        }

        public string SignalRConnection
        {
            get { return _signalRConnection; }
            set { _signalRConnection = value; }
        }
    }
}
