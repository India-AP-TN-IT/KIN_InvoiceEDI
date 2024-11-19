using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace SY_HMI_InvoiceEDI
{
    /// <summary>
    /// 윈도우 서비스 인스톨러
    /// </summary>
    [RunInstaller(true)]
    public class WindowsServiceInstaller : Installer
    {
        /// <summary>
        /// 윈도우 서비스 인스톨러 생성자
        /// </summary>
        public WindowsServiceInstaller()
        {
            // 서비스 동작 계정 정보
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;
            this.Installers.Add(serviceProcessInstaller);

            // 서비스 정보
            ServiceInstaller serviceInstaller = new ServiceInstaller();
            serviceInstaller.DisplayName = KIN_IV_Service.__SERVICE_NAME__;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = serviceInstaller.DisplayName;
            serviceInstaller.Description = KIN_IV_Service.__SERVICE_DESCRIPTION__;
            this.Installers.Add(serviceInstaller);
        }

        /// <summary>
        /// 서비스 인스톨후 서비스 즉시 실행 시작
        /// </summary>
        /// <param name="savedState"></param>
        protected override void OnAfterInstall(System.Collections.IDictionary savedState)
        {
            base.OnAfterInstall(savedState);

            // 서비스를 찾아 Start
            System.ServiceProcess.ServiceController svr = new ServiceController(KIN_IV_Service.__SERVICE_NAME__);

            if (svr != null) svr.Start();
        }

        /// <summary>
        /// 서비스 언인스톨전 서비스 실행 중지
        /// </summary>
        /// <param name="savedState"></param>
        protected override void OnBeforeUninstall(System.Collections.IDictionary savedState)
        {
            // 서비스를 찾아 Stop
            System.ServiceProcess.ServiceController svr = new ServiceController(KIN_IV_Service.__SERVICE_NAME__);

            if (svr != null && svr.Status != ServiceControllerStatus.Stopped && svr.Status != ServiceControllerStatus.StopPending) svr.Stop();

            base.OnBeforeUninstall(savedState);
        }
    }
}
