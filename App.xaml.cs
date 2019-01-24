using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;

namespace MosSTPT
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public EventWaitHandle ProgramStarted { get; set; }

        /// <summary>
        /// 重写OnStartup方法，以控制程序只能同时启动一个实例
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            bool createNew;
            ProgramStarted = new EventWaitHandle(false, EventResetMode.AutoReset, "SFCT095", out createNew);

            if (!createNew)
            {
                MessageBox.Show("系统检测到已有一个程序实例正在运行，不可重复运行，请确认！", "重复运行提示", MessageBoxButton.OK, MessageBoxImage.Information);
                App.Current.Shutdown();
                Environment.Exit(0);
            }
            base.OnStartup(e);
        }

        /// <summary>
        /// 拦截未经处理的异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("系统出现未经处理的异常，请与系统开发人员联系，并提供如下信息：\n" + e.Exception.Message, "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
