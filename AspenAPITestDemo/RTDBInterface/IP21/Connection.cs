﻿using System;
using System.Collections.Generic;
using System.Text;
using RTDB.IP21;
using System.Collections;

namespace RTDB.IP21
{
    public class Connection
    {
        private string serverIp;
        private int serverIndex;
        public static string serverGroup = "200";
        private static Dictionary<string, int> serverPool = new Dictionary<string, int>();
        private static string currentServerIp;

        public int ServerIndex
        {
            get { return this.serverIndex; }
            set { this.serverIndex = value; }
        }

        private Connection(string serverIp)
        {
            this.serverIp = serverIp;
        }

        public static Connection OpenConnection(string serverIp)
        {
            Connection conn = new Connection(serverIp);

            conn.Init();

            return conn;
        }

        public void Close() {
            serverPool.Remove(serverIp);
            if (serverPool.Count == 0)
                infoplus21_api.DaShutdown();
        }

        private void Init()
        {
            infoplus21_api.ERRBLOCK errMsg; // = new infoplus21_api.ERRBLOCK();
            if (!serverPool.ContainsKey(serverIp))
            {
                //TDaBoolean DaInitialize(TDaBoolean ReadConfigFile);
                int res = infoplus21_api.DaInitialize(infoplus21_api.C_FALSE);
                if (res != infoplus21_api.C_TRUE)
                {
                    throw new IP21Exception("failed to initialize da without reading config file");
                }

                string connectString = this.serverIp + " " + serverGroup + " " + "/FATAL";
                Util.writestr(connectString);
                byte[] bpara = Encoding.Default.GetBytes(connectString);
                this.serverIndex = infoplus21_api.DaAddServer(bpara, out errMsg);

                Util.CheckResult(errMsg);
                serverPool.Add(serverIp, serverIndex);
            }
            else
            {
                infoplus21_api.DaSelectServer((int)serverPool[serverIp], out errMsg);
                Util.CheckResult(errMsg);
            }
            currentServerIp = serverIp;
        }

        private bool SwitchToServerIp() {
            if (!currentServerIp.Equals(serverIp)) {
                Init();
            }
            return true;
        }

        // 从历史数据库取实际值
        public double GetActuals(string tag, DateTime time, int span) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return HistoryReader.ReadActuals(tag, time, span);
        }

        // 从历史数据库取插值
        public double GetInterpolated(string tag, DateTime time) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return HistoryReader.ReadInterpolated(tag, time);
        }

        // 从历史数据库取时间和实际值
        public IList<TimeValuePair> ListActuals(string tag, DateTime time, int span) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return HistoryReader.ListAllHistory(infoplus21_api.H21_GET_ACTUALS, tag, time, span);
        }

        // 从历史数据库取时间和实际值
        public IList<TimeValuePair> ListInterpolated(string tag, DateTime time, int span) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return HistoryReader.ListAllHistory(infoplus21_api.H21_GET_TIMES, tag, time, span);
        }

       
        

        // 从实时数据库取数
        public string GetStringValue(string tag, string property) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            infoplus21_api.ERRBLOCK errMsg;

            byte[] des = new byte[200];

            infoplus21_api.DB2CHBF(Util.TagId(tag), Util.FieldId(property), des, des.Length, out errMsg);
            Util.CheckResult(errMsg);

            return Encoding.Default.GetString(des).Trim();
        }

        // 从实时数据库取数
        public Single GetNumValue(string tag, string property) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            infoplus21_api.ERRBLOCK errMsg;
            int recid = Util.TagId(tag);

            Single value = 0;
            infoplus21_api.DB2REAL(recid, Util.FieldId(property), out value, out errMsg);
            Util.CheckResult(errMsg);

            return value;
        }

        // 直接读取实时值
        public Single GetValue(string tag) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return GetNumValue(tag, "IP_VALUE");
        }
        /// <summary>
        /// 返回输入点位号列表当前值的键值对
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetCurrentData(IList tags) {
            SwitchToServerIp();
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string tag in tags) {
                if (!result.ContainsKey(tag)) {
                    double value = GetNumValue(tag, "IP_VALUE");
                    result.Add(tag, value.ToString());
                }
            }
            return result;
        }

        public double GetAveragedData(string tag, DateTime timeOld, DateTime timeNew) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return AggreationReader.GetAggreagedData(tag, timeOld, timeNew, infoplus21_api.AG_DBL_AVG);
        }
        public double GetAveragedDataSTD(string tag, DateTime timeOld, DateTime timeNew)
        {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return AggreationReader.GetAggreagedData(tag, timeOld, timeNew, infoplus21_api.AG_DBL_AVG);
        }

        public double  GetAveragedDataFor(string tag, DateTime timeOld, DateTime timeNew)
        {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return AggreationReader.GetAggreagedData(tag, timeOld, timeNew, infoplus21_api.AG_DBL_AVG);
        }

        public Dictionary<string, string> GetAveragedData(IList tags, DateTime timeOld, DateTime timeNew) {
            SwitchToServerIp();
            return AggreationReader.GetAggreagedData(tags, timeOld, timeNew, infoplus21_api.AG_DBL_AVG);
        }
        public Dictionary<string, string> GetAveragedSTD(IList tags, DateTime timeOld, DateTime timeNew)
        {
            if(tags.Count>0)
            {
                this.serverIp = Util.getAspenIPfromPointReg(tags[0].ToString());
            }
            SwitchToServerIp();
            return AggreationReader.GetAggreagedData(tags, timeOld, timeNew, infoplus21_api.AG_DBL_STD);
        }

        public IList<TimeAggreationPair> GetMaxMinAvgAtOnce(string tag, DateTime timeOld, DateTime timeNew, int interval) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return AggreationReader.GetMaxMinAvgAtOnce(tag, timeOld, timeNew, interval);
        } 

        public IList<TimeValuePair> GetAveragedData(string tag, DateTime timeOld, DateTime timeNew, int interval) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return AggreationReader.GetAggreagedData(tag, timeOld, timeNew, interval, infoplus21_api.AG_DBL_AVG);
        }

        public IList<TimeValuePair> GetCurrentData(string tag, DateTime timeOld, DateTime timeNew, int interval) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);//这里强行换掉原来的ip地址
            //注意要在SwitchToServerIp之前换掉，SwitchToServerIp本身也是换ip的方式
            SwitchToServerIp();        
            return HistoryReader.ListHistoryDataByInterval(tag, timeOld, timeNew, interval);
        }
        public IList<TimeValuePair> GetCurrentData(string tag, DateTime timeOld, DateTime timeNew, string interval)
        {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            
            return HistoryReader.ListHistoryDataByInterval(tag, timeOld, timeNew, interval);
        } 

        public double GetMaxData(string tag, DateTime timeOld, DateTime timeNew) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return AggreationReader.GetAggreagedData(tag, timeOld, timeNew, infoplus21_api.AG_DBL_MAX);
        }

        public double GetMinData(string tag, DateTime timeOld, DateTime timeNew) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return AggreationReader.GetAggreagedData(tag, timeOld, timeNew, infoplus21_api.AG_DBL_MIN);
        }

        public double GetDailyAverage(string tag, DateTime date) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return AggreationReader.GetDailyAverage(tag, date);
        }

        public double GetDailyMax(string tag, DateTime date) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return AggreationReader.GetDailyMax(tag, date);
        }

        public double GetDailyMin(string tag, DateTime date) {
            this.serverIp = Util.getAspenIPfromPointReg(tag);
            SwitchToServerIp();
            return AggreationReader.GetDailyMin(tag, date);
        }
    }
}