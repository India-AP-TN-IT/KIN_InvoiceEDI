/*
 * 
 * Copyright (C) 주식회사 서연이화 1980-2015. All right reserved.
 * 
 * Written by Yongshin
 * 
 * 이 코드는 (주) 서연이화의 자산입니다. 무단으로 이 코드의 전체 혹은
 * 일부를 복제, 수정하거나 공개하는 것은 저작권 위반입니다.
 *
 * 이 코드는 (주) 서연이화 제품의 일부로서 사용될 때만이 유효하며
 * 그 외의 사용은 금지되어 있습니다.
 * 
 */

using System;
using System.Threading;
using System.Diagnostics;
using System.ServiceProcess;
using System.Configuration;
using System.Configuration.Install;
using System.Collections;
using SY_HMI_InvoiceEDI;

namespace SY_HMI_InvoiceEDI
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// 2019.04.02 args 파라메터 추가 ( command line 용 )
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Utils.SetXMLConf(Utils.XMLPath);
            bool bDebug = Utils.GetBoolStr(Utils.GetXMLConf("DEBUG"));
            if (bDebug)
            {
                // 개발모드 ( 디버깅을 위한 )
                KIN_IV_Service test = new KIN_IV_Service();
                test.DebugStart();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
            else
            {
                // 운영모드
                if (args.Length == 1)
                {
                    try
                    {
                        // 서비스 등록 및 해제부
                        using (TransactedInstaller ti = new TransactedInstaller())
                        {
                            using (WindowsServiceInstaller pi = new WindowsServiceInstaller())
                            {
                                ti.Installers.Add(pi);

                                string[] cmdline = { string.Format("/assemblypath={0}", System.Reflection.Assembly.GetExecutingAssembly().Location) };
                                pi.Context = new InstallContext(null, cmdline);

                                // 서비스 등록/해제
                                if (args[0].ToLower() == "/i" || args[0].ToLower() == "-i")
                                    pi.Install(new Hashtable());
                                else if (args[0].ToLower() == "/u" || args[0].ToLower() == "-u")
                                    pi.Uninstall(null);
                                else
                                    throw new Exception("Seoyon E-Hwa HMI Invoice I/F : Uninstall for windows service.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.Message);
                    }
                }
                else
                {
                    // 서비스 구동 코드 단순화
                    ServiceBase.Run(new KIN_IV_Service());
                }
            }
        }
    }
}
