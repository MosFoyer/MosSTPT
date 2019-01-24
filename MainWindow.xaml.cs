using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MosSTPT
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowMain_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }

            if (File.Exists("Result.log"))
            {
                string dstFileName = AppDomain.CurrentDomain.BaseDirectory + "Logs\\Result_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".log";
                File.Move("Result.log", dstFileName);
            }


        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "S19文件|*.s19|所有文件|*.*";
            if (fd.ShowDialog() == true)
            {
                TextBoxFWFileName.Text = fd.FileName;

                TextBlockChecksum.Text = GetS19FileChecksum(fd.FileName);

            }
        }

        private void ButtonProgram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (String.IsNullOrEmpty(TextBoxFWFileName.Text))
                {
                    MessageBox.Show("请先添加固件文件！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    ButtonProgram.IsEnabled = true;
                    return;
                }

                ButtonProgram.IsEnabled = false;

                UiUpdate("TESTING", "正在烧录");

                using (Process p = new Process())
                {
                    p.StartInfo.CreateNoWindow = true;            //不创建新窗口    
                    p.StartInfo.UseShellExecute = false;          //不启用shell启动进程  
                    p.StartInfo.RedirectStandardInput = true;     //重定向输入流  
                    p.StartInfo.RedirectStandardOutput = true;    //重定向输出流  
                    p.StartInfo.RedirectStandardError = true;     //重定向错误流  

                    p.StartInfo.FileName = "STVP_CmdLine.exe";
                    p.StartInfo.Arguments = "-BoardName=ST-LINK -Tool_ID=0 -Device=STM8S003F3 -Port=USB -ProgMode=SWIM -no_loop -log -FileProg=" + TextBoxFWFileName.Text;
                    p.Start();

                    // 异步获取命令行内容 
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    // 为异步获取订阅事件  
                    p.OutputDataReceived += new DataReceivedEventHandler(Process_OutputDataReceived);
                    p.ErrorDataReceived += new DataReceivedEventHandler(Process_OutputDataReceived);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("引发异常：" + ex.Message, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
                UiUpdate("Fail", ex.Message);
            }


        }

        /// <summary>
        /// 获取.s19文件的Checksum值(与STVP工具获得的文件Checksum值一致)
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <returns>十六进制表示的.s19文件的Checksum值</returns>
        private string GetS19FileChecksum(string filename)
        {
            int checksum = 0;
            string hexText;
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                hexText = Encoding.UTF8.GetString(buffer);
            }
            string[] hexLines = hexText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < hexLines.Count(); i++)
            {
                string hexLine = hexLines[i].Substring(8, hexLines[i].Length - 8);

                if ((hexLine.Length % 2) != 0)
                {
                    hexLine = hexLine + "0";
                }

                for (int j = 0; j < hexLine.Length - 2; j = j + 2)
                {
                    int hex = Convert.ToInt32(hexLine.Substring(j, 2), 16);
                    checksum = checksum + hex;
                }
            }

            return "0x" + checksum.ToString("X2");
        }

        /// <summary>
        /// Process类的标准输出接收处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                if (e.Data.IndexOf("fail") >= 0 || e.Data.IndexOf("ERROR") >= 0)
                {
                    UiUpdate("FAIL", "固件烧录失败，详情查看Result.log");
                    return;
                }

                // STVP_CmdLine Return:
                // >>> Filling PROGRAM MEMORY image in computer with Blank Value
                // <<< Filling PROGRAM MEMORY image in computer succeeds
                if (e.Data.IndexOf(">>> Filling PROGRAM MEMORY image in computer with Blank Value") >= 0)
                {
                    UiUpdate("TESTING", "正在清空程序区");
                }
                if (e.Data.IndexOf("<<< Filling PROGRAM MEMORY image in computer succeeds") >= 0)
                {
                    UiUpdate("TESTING", "清空程序区成功");
                }

                // STVP_CmdLine Return:
                // >>> Loading file 66.s19 in PROGRAM MEMORY image in computer
                // <<< Loading file succeeds
                if (e.Data.IndexOf(">>> Loading file") >= 0 && e.Data.IndexOf("in computer") > 0)
                {
                    UiUpdate("TESTING", "正在加载固件");

                }
                if (e.Data.IndexOf("<<< Loading file succeeds") >= 0)
                {
                    UiUpdate("TESTING", "固件加载成功");

                }

                // STVP_CmdLine Return:
                // >>> Programming PROGRAM MEMORY
                // Cut Version and Revision of device: 1.2
                // <<< Programming PROGRAM MEMORY succeeds
                if (e.Data.IndexOf(">>> Programming PROGRAM MEMORY") >= 0)
                {
                    UiUpdate("TESTING", "正在写入固件");

                }
                if (e.Data.IndexOf("<<< Programming PROGRAM MEMORY succeeds") >= 0)
                {
                    UiUpdate("TESTING", "固件写入成功");

                }

                // STVP_CmdLine Return:
                // >>> Verifying PROGRAM MEMORY
                // Cut Version and Revision of device: 1.2
                // <<< Verifying PROGRAM MEMORY succeeds
                if (e.Data.IndexOf(">>> Verifying PROGRAM MEMORY") >= 0)
                {
                    UiUpdate("TESTING", "正在校验固件");
                }
                if (e.Data.IndexOf("<<< Verifying PROGRAM MEMORY succeeds") >= 0)
                {
                    UiUpdate("PASS", "固件烧录成功");
                }




            }

        }

        /// <summary>
        /// UI信息更新的方法
        /// </summary>
        /// <param name="uiStatus">
        /// 需要显示的状态信息，有以下机种状态：
        /// 1.JUMP：表示跳过更新,若为JUMP则uiScanInfo参数可以给任意string类型的值
        /// 2.WAIT：表示等待
        /// 3.PASS：表示测试正常
        /// 4.FAIL：表示测试失败
        /// 5.INPUT：表示输入数据中
        /// 7.TESTING：表示测试中
        ///</param>
        /// <param name="uiProgramInfo"></param>
        private void UiUpdate(string uiStatus, string uiProgramInfo)
        {
            //线程安全的执行更新Ui状态栏信息
            Dispatcher.Invoke(new Action(() =>
            {
                TextBlockProgramInfo.Text = uiProgramInfo;

                if (uiStatus == "JUMP" || String.IsNullOrEmpty(uiStatus))
                {
                    return;
                }

                TextBlockProgramInfo.Text = uiProgramInfo;

                TextBlockScanStatus.Text = uiStatus;
                switch (uiStatus)
                {
                    default:
                        TextBlockScanStatus.Foreground = Brushes.DodgerBlue;
                        TextBlockProgramInfo.Foreground = Brushes.DodgerBlue;
                        ButtonProgram.IsEnabled = true;
                        break;
                    case "WAIT":
                        TextBlockScanStatus.Foreground = Brushes.DodgerBlue;
                        TextBlockProgramInfo.Foreground = Brushes.DodgerBlue;
                        ButtonProgram.IsEnabled = true;
                        break;
                    case "PASS":
                        TextBlockScanStatus.Foreground = Brushes.Green;
                        TextBlockProgramInfo.Foreground = Brushes.Green;
                        ButtonProgram.IsEnabled = true;
                        break;
                    case "FAIL":
                        TextBlockScanStatus.Foreground = Brushes.Red;
                        TextBlockProgramInfo.Foreground = Brushes.Red;
                        ButtonProgram.IsEnabled = true;
                        break;
                    case "TESTING":
                        TextBlockScanStatus.Foreground = Brushes.Orange;
                        TextBlockProgramInfo.Foreground = Brushes.Orange;
                        break;
                    case "INPUT":
                        TextBlockScanStatus.Foreground = Brushes.LimeGreen;
                        TextBlockProgramInfo.Foreground = Brushes.LimeGreen;
                        ButtonProgram.IsEnabled = true;
                        break;

                }

            }));

        }

    }
}
