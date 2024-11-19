using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SY_HMI_InvoiceEDI
{
    public class Utils
    {
        private static DataTable m_dtXML = new DataTable();
        private const string CN_CONFIG_XML_PATH = @"HE_MES_Config.xml";
        public static string XMLPath
        {
            get
            {
                return CN_CONFIG_XML_PATH;
            }
        }
        public static string GetXMLConf(string eleName)
        {
            if (m_dtXML != null)
            {
                if (m_dtXML.Rows.Count > 0)
                {
                    if (m_dtXML.Columns.Contains(eleName))
                    {
                        return m_dtXML.Rows[0][eleName].ToString();
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            return "";
        }
        public static bool GetBoolStr(string strbool)
        {
            strbool = strbool.Replace(" ", "").Trim().ToUpper();

            switch (strbool)
            {
                case "Y":
                case "TRUE":
                case "T":
                case "1":
                case "YES":
                case "01":
                    return true;

            }
            return false;

        }
        public static void SetXMLConf(string path)
        {
            m_dtXML = OpenXML(path);
        }
        public static DataTable OpenXML(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = CN_CONFIG_XML_PATH;
            }
            if (File.Exists(path))
            {
                DataSet ds = new DataSet();
                ds.ReadXml(path);
                if (ds.Tables.Count > 0)
                {
                    for (int row = 0; row < ds.Tables[0].Rows.Count; row++)
                    {
                        for (int col = 0; col < ds.Tables[0].Columns.Count; col++)
                        {
                            ds.Tables[0].Rows[row][col] = ds.Tables[0].Rows[row][col].ToString().Trim();
                        }
                    }
                }
                return ds.Tables[0];
            }
            else
            {
                return new DataTable();
            }        
        }
    }
}
