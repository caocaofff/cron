using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Timers;
using System.Windows.Forms;

namespace cron
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        string MyHour, MyMinute, MyProcessName, MyExePath;

        private void FormMain_Load(object sender, EventArgs e)
        {
            //指定config文件读取
            string file = System.Windows.Forms.Application.ExecutablePath;
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(file);
            MyHour = config.AppSettings.Settings["Hour"].Value.ToString();
            MyMinute = config.AppSettings.Settings["Minute"].Value.ToString();
            MyProcessName = config.AppSettings.Settings["ProcessName"].Value.ToString();
            MyExePath = config.AppSettings.Settings["ExePath"].Value.ToString();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = 60000;//执行间隔时间,单位为毫秒;此时时间间隔为1分钟  
            timer.Start();
            timer.Elapsed += new ElapsedEventHandler(restart);
            textBox1.Text = "本程序指定到每天特定分钟重新运行指定命令。\r\n每分钟进行判断是否符合设定时间。\r\n关闭本窗口后本程序会自动最小化到托盘，双击托盘显示，右键托盘退出。\r\n";

        }


        private void restart(object source, ElapsedEventArgs e)
        {
            
            if (File.Exists(@MyExePath))
            {
                if (DateTime.Now.Hour.ToString() == MyHour && DateTime.Now.Minute.ToString() == MyMinute)   //如果当前时间是xx点xx分
                {
                    try
                    { 
                    //textBox1.Text += DateTime.Now.ToString() + "：准备杀死进程：" + MyProcessName + "\r\n";
                    System.Diagnostics.Process[] processes = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(MyProcessName));
                    foreach (Process p in processes)
                    {
                        if (MyExePath == p.MainModule.FileName)
                        {
                            textBox1.Text += DateTime.Now.ToString() + "：进程完整路径与配置启动路径一致，执行杀进程动作。\r\n";
                            p.Kill();
                            p.Close();
                        }
                    }
                    RunCmd.run("ping 127.0.0.1");
                    if (!CheckProcessExists())
                    {
                            textBox1.Text += DateTime.Now.ToString() + "：进程已确认不存在，执行启动进程动作。\r\n";
                            Process p = new Process();
                            p.StartInfo.FileName = MyExePath;
                            //p.StartInfo.Arguments = "进程参数";
                            p.StartInfo.UseShellExecute = true;
                            p.Start();
                            //使 Process 组件在指定的毫秒数内等待关联进程进入空闲状态。此重载仅适用于具有用户界面并因此具有消息循环的进程。
                            p.WaitForInputIdle(10000);

                    }
                    //cmd方法
                        /*
                        textBox1.Text += DateTime.Now.ToString() + "：开始杀死进程：taskkill /im " + MyProcessName + "\r\n";
                        RunCmd.run("taskkill /im " + Path.GetFileName(@MyExePath));
                        textBox1.Text += DateTime.Now.ToString() + "：开始执行任务：start " + @MyExePath + "\r\n";
                        RunCmd.run("start " + @MyExePath);
                        */
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Source + " " + ex.Message);
                    }
                }
            }
            else textBox1.Text += DateTime.Now.ToString() + "：指定的路径：" + MyExePath + "不存在。\r\n";
        }

        private bool CheckProcessExists()
        {
            Process[] processes = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(MyProcessName));
            foreach (Process p in processes)
            {
                if (MyExePath == p.MainModule.FileName)
                    return true;
            }
            return false;
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 注意判断关闭事件reason来源于窗体按钮，否则用菜单退出时无法退出!
            if (e.CloseReason == CloseReason.UserClosing)
            {
                //取消"关闭窗口"事件
                e.Cancel = true; // 取消关闭窗体 

                //使关闭时窗口向右下角缩小的效果
                this.WindowState = FormWindowState.Minimized;
                this.mainNotifyIcon.Visible = true;
                //this.m_cartoonForm.CartoonClose();
                this.Hide();
                return;
            }
        }

        private void mainNotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.Visible)
            {
                this.WindowState = FormWindowState.Minimized;
                this.mainNotifyIcon.Visible = true;
                this.Hide();
            }
            else
            {
                this.Visible = true;
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你确定要退出？", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
            {

                this.mainNotifyIcon.Visible = false;
                this.Close();
                this.Dispose();
                System.Environment.Exit(System.Environment.ExitCode);

            }
        }
    }


    public class RunCmd
    {
        public RunCmd()
        {

        }
        private static string CmdPath = @"C:\Windows\System32\cmd.exe";

        /// <summary>
            /// 执行cmd命令
            /// 多命令请使用批处理命令连接符：
            /// <![CDATA[
            /// &:同时执行两个命令
            /// |:将上一个命令的输出,作为下一个命令的输入
            /// &&：当&&前的命令成功时,才执行&&后的命令
            /// ||：当||前的命令失败时,才执行||后的命令]]>
            /// 其他请百度
            /// </summary>
            /// <param name="cmd"></param>
            /// <param name="output"></param>
        public static void run(string cmd)
        {
            cmd = cmd.Trim().TrimEnd('&') + "&exit";//说明：不管命令是否成功均执行exit命令，否则当调用ReadToEnd()方法时，会处于假死状态
            using (Process p = new Process())
            {
                p.StartInfo.FileName = CmdPath;
                p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
                p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
                p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
                p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
                p.StartInfo.CreateNoWindow = false;          //不显示程序窗口
                p.Start();//启动程序
                //向cmd窗口写入命令
                p.StandardInput.WriteLine(cmd);
                p.StandardInput.AutoFlush = true;
                //p.StandardInput.WriteLine("exit");
                //向标准输入写入要执行的命令。这里使用&是批处理命令的符号，表示前面一个命令不管是否执行成功都执行后面(exit)命令，如果不执行exit命令，后面调用ReadToEnd()方法会假死
                //同类的符号还有&&和||前者表示必须前一个命令执行成功才会执行后面的命令，后者表示必须前一个命令执行失败才会执行后面的命令

                //获取cmd窗口的输出信息
                //output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();//等待程序执行完退出进程
                p.Close();
            }
        }
    }

}
