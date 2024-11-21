using KIN_InvoiceEDI.KIN_Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace SY_HMI_InvoiceEDI
{
    public partial class KIN_IV_Service : ServiceBase
    {
        //sc create "SYMailSMS_Service" binPath= "C:\inetpub\WS_SYMailSMS_Service\SYMailSMS_Service.exe"
        public const string __SERVICE_NAME__ = "KIN-Invoice IF";
        public const string __SERVICE_DESCRIPTION__ = "Seoyon E-Hwa HMI E-Invoice Data IF";
        YMES.FX.DB.OracleHelper m_fxDB = new YMES.FX.DB.OracleHelper();
        bool m_bStop = false;
        DateTime m_oldTimeMail = new DateTime();
        DateTime m_oldTimeSMS = new DateTime();

        private System.Timers.Timer m_timer = new System.Timers.Timer();
        private const string CN_CONFIG_XML_PATH = @"HE_MES_Config.xml";

        public KIN_IV_Service()
        {            
            try
            {
                this.ServiceName = __SERVICE_NAME__;
                this.EventLog.Log = "Application";

                this.CanHandlePowerEvent = true;
                this.CanHandleSessionChangeEvent = true;
                this.CanPauseAndContinue = false;
                this.CanShutdown = true;
                this.CanStop = true;
                InitializeComponent();
            }
            catch (Exception eLog)
            {
                WriteToFile("KIN_IV_Service()/" + eLog.Message);
                this.EventLog.WriteEntry("KIN_IV_Service()/" + eLog.ToString(), EventLogEntryType.Error);
            }
        }
        /// <summary>
        /// 디버깅용 진입부
        /// </summary>
        public void DebugStart()
        {
            this.OnStart(null);
        }
        private void WriteToFile(string Message)
        {
            try
            {

                string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";

                if (!Directory.Exists(path))
                {

                    Directory.CreateDirectory(path);

                }

                string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";

                if (!File.Exists(filepath))
                {

                    // Create a file to write to.   

                    using (StreamWriter sw = File.CreateText(filepath))
                    {

                        sw.WriteLine(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + Message);

                    }

                }
                else
                {

                    using (StreamWriter sw = File.AppendText(filepath))
                    {

                        sw.WriteLine(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + Message);

                    }

                }
            }
            catch (Exception eLog)
            {
                this.EventLog.WriteEntry(eLog.ToString(), EventLogEntryType.Warning);
            }

        }
        private bool IsDBWorking()
        {
            DataTable dt = m_fxDB.ExecuteQuery("SELECT SYSDATE DT FROM DUAL", null);
            if(dt.Rows.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void DebugTest()
        {
            string corcd = Utils.GetXMLConf("CORCD");
            string bizcd = Utils.GetXMLConf("BIZCD");
            
            WriteToFile("1)XmlReading (CORCD/BIZCD): " + corcd + "/" + bizcd);
            WriteToFile("2)Xmlpath: " + CN_CONFIG_XML_PATH);
            WriteToFile("3)Excute File: " + Application.ExecutablePath);
        }
      
        protected override void OnStart(string[] args)
        {
            try
            {

                Utils.SetXMLConf(AppDomain.CurrentDomain.BaseDirectory + Utils.XMLPath);
                m_fxDB.XMLConfigPath = AppDomain.CurrentDomain.BaseDirectory + Utils.XMLPath;
                m_fxDB.SetXMLName("DBKIND", "DBNAME", "DBUID", "DBPWD", "DBSERVICE", "DBPORT");
                
                DebugTest();
                if (IsDBWorking() == false)
                {
                    System.Threading.Thread.Sleep(10000);   //Because of Oracle Start Delay
                    WriteToFile("DB Connection : NG");
                }
                else
                {
                    WriteToFile("DB Connection : OK");
                }
                //System.Threading.ThreadPool.QueueUserWorkItem(ThreadRun);
                WriteToFile("START!!");
                int timeTick = Convert.ToInt32(Utils.GetXMLConf("TIME_SPAN_SEC")) *1000;
                m_timer.Interval = timeTick;
                m_timer.Elapsed += TimerRun;
                m_timer.AutoReset = false;
                m_timer.Start();

                /*
                TimeSpan tsInterval = new TimeSpan(0, 0, timeTick);
                IntervalTimer = new System.Threading.Timer(
                    new System.Threading.TimerCallback(TimerRun)
                    , null, tsInterval, tsInterval);
                */
            }
            catch (Exception eLog)
            {
                WriteToFile("OnStart()/" + eLog.Message);
                this.EventLog.WriteEntry("OnStart()/" + eLog.ToString(), EventLogEntryType.Error);
            }
        }
        private void TimerRun(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                m_timer.Stop();
                DataIF_Process();
            }
            catch(Exception eLog)
            {
                WriteToFile("TimerRun()/" + eLog.Message);
            }
            finally
            {
                m_timer.Start();
            }
            
        }
        private void SetWaitData(string corcd, string bizcd, KIN_InvoiceEDI.KIN_Service.O_DATA rslt, List<KIN_InvoiceEDI.KIN_Service.TAB_DATA_HEADER> data)
        {
            for (int i = 0; i < rslt.GET_DATA_01.Length; i++)
            {
                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("IN_CORCD", corcd);
                param.Add("IN_BIZCD", bizcd);
                param.Add("IN_IVNUM", rslt.GET_DATA_01[i].IVNUM);
                param.Add("IN_MATNR", data[i].MATNR);
                param.Add("IN_KIN_MSG", rslt.GET_DATA_01[i].IFFAILMSG);
                param.Add("IN_KIN_RSLT", rslt.GET_DATA_01[i].IFRESULT);
                m_fxDB.ExecuteNonQuery("ZPG_ZSD02610.SET_WAIT_DATA", param);
            }
        }

        private void DataIF_Process()
        {
            try
            {
               
                string corcd = Utils.GetXMLConf("CORCD");
                string bizcd = Utils.GetXMLConf("BIZCD");
                KIN_InvoiceEDI.KIN_Service.O_DATA rslt = new KIN_InvoiceEDI.KIN_Service.O_DATA();
                List<KIN_InvoiceEDI.KIN_Service.TAB_DATA_HEADER> lstHeader = new List<KIN_InvoiceEDI.KIN_Service.TAB_DATA_HEADER>();
                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("IN_CORCD", corcd);
                param.Add("IN_BIZCD", bizcd);
                DataTable dt = m_fxDB.ExecuteQuery("ZPG_ZSD02610.GET_WAIT_DATA", param);

                string prvInvoice = "";
                foreach (DataRow dr in dt.Rows)
                {
                    if (string.IsNullOrEmpty(prvInvoice) || prvInvoice == dr["IVNUM"].ToString())
                    {
                        prvInvoice = dr["IVNUM"].ToString();
                        TAB_DATA_HEADER oTAB_DATA_HEADER = new TAB_DATA_HEADER();

                        oTAB_DATA_HEADER.IVNUM = dr["IVNUM"].ToString();
                        oTAB_DATA_HEADER.IVDAT = dr["IVDAT"].ToString();
                        oTAB_DATA_HEADER.LIFNR = dr["LIFNR"].ToString();
                        oTAB_DATA_HEADER.ZSHOP = dr["ZSHOP"].ToString();
                        oTAB_DATA_HEADER.EBELN = dr["EBELN"].ToString();


                        oTAB_DATA_HEADER.MATNR = dr["MATNR"].ToString().Replace("-", "");

                        oTAB_DATA_HEADER.IVQTY = dr["IVQTY"].ToString();
                        oTAB_DATA_HEADER.ZAIVAMT = dr["ZAIVAMT"].ToString();
                        oTAB_DATA_HEADER.ZANETPR = dr["ZANETPR"].ToString();
                        oTAB_DATA_HEADER.ZANETWR = Math.Round(Convert.ToDecimal(dr["ZANETWR"]), 3).ToString();
                        oTAB_DATA_HEADER.ZCGST = Math.Round(Convert.ToDecimal(dr["ZCGST"]), 2).ToString();
                        oTAB_DATA_HEADER.ZSGST = Math.Round(Convert.ToDecimal(dr["ZSGST"]), 2).ToString();
                        oTAB_DATA_HEADER.ZIGST = Math.Round(Convert.ToDecimal(dr["ZIGST"]), 2).ToString();
                        oTAB_DATA_HEADER.ZUGST = "0.00";
                        oTAB_DATA_HEADER.IRN = dr["IRN"].ToString();
                        oTAB_DATA_HEADER.ZHSNSAC = dr["ZHSNSAC"].ToString();
                        oTAB_DATA_HEADER.ZGSTIN = dr["ZGSTIN"].ToString();
                        oTAB_DATA_HEADER.VEHNO = dr["VEHNO"].ToString().ToUpper().Trim();


                        oTAB_DATA_HEADER.ZATCS = Math.Round(Convert.ToDecimal(dr["ZATCS"]), 2).ToString();

                        oTAB_DATA_HEADER.EWAYBILL = dr["EWAYBILL"].ToString();


                        oTAB_DATA_HEADER.ZNUM1 = dr["ZNUM1"].ToString();
                        oTAB_DATA_HEADER.ZNUM2 = dr["ZNUM2"].ToString();
                        oTAB_DATA_HEADER.ZNUM3 = dr["ZNUM3"].ToString();
                        oTAB_DATA_HEADER.ZNUM4 = dr["ZNUM4"].ToString();
                        oTAB_DATA_HEADER.ZNUM5 = dr["ZNUM5"].ToString();
                        oTAB_DATA_HEADER.ZCHAR2 = dr["ZCHAR2"].ToString();
                        oTAB_DATA_HEADER.ZCHAR3 = dr["ZCHAR3"].ToString();
                        oTAB_DATA_HEADER.ZCHAR4 = dr["ZCHAR4"].ToString();
                        oTAB_DATA_HEADER.ZCHAR5 = dr["ZCHAR5"].ToString();

                        lstHeader.Add(oTAB_DATA_HEADER);
                    }
                }
                if (lstHeader.Count > 0)
                {
                    


                    KIN_InvoiceEDI.KIN_Service.INUPUT_DATA reqData = new INUPUT_DATA();
                    reqData.GET_DATA = lstHeader.ToArray();
                    string strPDF = GetPDFFile(corcd, bizcd, prvInvoice);
                    KIN_InvoiceEDI.KIN_Service.ServiceSoapClient client = new ServiceSoapClient();
                    KIN_InvoiceEDI.KIN_Service.O_DATA rsltMSG = client.getData(strPDF, reqData);
                    SetWaitData(corcd, bizcd, rsltMSG, lstHeader);
                }
            }
            catch(Exception eLog)
            {
                WriteToFile("DataIF_Process()/" + eLog.Message);
            }
        }
        private string GetPDFFile(string corcd, string bizcd, string invoice)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("IN_CORCD", corcd);
            param.Add("IN_BIZCD", bizcd);
            param.Add("IN_IVNUM", invoice);
            DataTable dt = m_fxDB.ExecuteQuery("ZPG_ZSD02610.GET_PDF_FILE", param);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["PDF_FILE"].ToString();
            }
            return "";
        }
        protected override void OnStop()
        {
            try
            {
                m_timer.Stop();
                WriteToFile("END!!");
            }
            catch (Exception eLog)
            {
                WriteToFile(eLog.Message);
                this.EventLog.WriteEntry(eLog.ToString(), EventLogEntryType.Error);
            }
        }

    }
}

