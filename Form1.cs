using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Web;
using System.IO;
using System.Diagnostics;


namespace LengthSlope
{
    public partial class Form1 : Form
    {
        //public string inPath;
        public string[] inPath; // zhj.将单个文件路径改为文件路径数组
        public string outPath;
        public string qianzhui;
        public float xiaoyu5;
        public float dayu5;
        public float threshold;
        public bool guodu = false;//默认为不产生过度文件
        public bool xiufu = true;//默认为修复文件
        public bool unit = true;//默认单位为m
        public bool fillWay = true;//默认为平均值填充
        public bool CumulatWay = true;//默认为周围最大值
        public bool Channel_Consider = false;//默认为不考虑河网
        public bool flowcut = true;//默认有截断
        public bool fillsink = false;//默认不进行洼地填充
        public bool Profix_YN;//前缀不递增
        public Form2 form2;
        private bool Forbiden;//程序是否可用
        private bool Overdue;//是否设置过期
        private bool ExportAll;
        private int RUSLE_CSLE=0;//用的是哪一个模型0为csle，1为rusle
        private int ExportNum;
        private int Limitnum;//使用次数
        private int Settingfile_lines;//文件行数
        private int FileNum; //设置的关联文件数量
        public string Filepath;//设置存储位置的路径
        private int x1;//使用次数限制1
        private int x2;//使用次数限制2
        private int x3;//使用次数限制3
        private int date1;//使用日期限制1
        private int date2;//使用日期限制2
        private int date3;//使用日期限制3
        private int datenow;//当前日期
        private string[,] Settingfile_line;
        private string StringPathLoad;
        private string StringPathSave;    //保存路径
        private string Profix_str;
        private System.Windows.Forms.CheckBox[] MyCheckBox = new CheckBox[6];
        int dateline;//日期所在行
        int usecount;//使用次数所在行

        string[] outDir; // zhj.结果输出目录

        OpenFileDialog openFileDialog = new OpenFileDialog();
        //FolderDialog f = new FolderDialog();
        FolderBrowserDialog f = new FolderBrowserDialog();
        public Form1()
        {
            Overdue = false;//使用过期设置
            //Overdue = false;//不使用过期设置
            //guodu = true;//保存过渡文件,2010.05.26去掉了单选按钮不需要该变量了
            ExportNum = 5;//总共输出项（复选按钮）6项
            Limitnum = 11;//设置使用次数
            dateline = 7;//日期所在行
            usecount = 9;//使用次数所在行
            Forbiden = true;//程序是否可用
            Profix_YN = true;//！前缀不递增，true：前缀递增
            ExportAll = false;//！是否输出所有数据结果；false：只输出选则的，true输出选择的和中间过渡文件
            FileNum = 3;//限制文件数量
            Settingfile_lines = 21;//文件内容存储行数
            Settingfile_line = new string[FileNum, Settingfile_lines];
            StringPathSave = null;//初始路径为空
            new Form3().ShowDialog();
            InitializeComponent();
            MyCheckBox[0] = checkBox_Save_SelectAll;
            MyCheckBox[1] = checkBox_Save_SlopLength;
            MyCheckBox[2] = checkBox_Save_SlopAngle;
            MyCheckBox[3] = checkBox_Save_LengthFactor;
            MyCheckBox[4] = checkBox_Save_SlopFactor;
            MyCheckBox[5] = checkBox_Save_LSFactor;

            groupBox_flowdirsgn_select.Visible = true;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            button_start.Enabled = false;
            //openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            /*openFileDialog.InitialDirectory = StringPathLoad;//上次目录
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Filter = "ASCII 文件 (*.txt)|*.txt|ANUDEM 文件 (*.DEM)|*.DEM|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string FileName = openFileDialog.FileName;
                StringPathLoad = FileName.Substring(0, FileName.IndexOf("\\") - 1) + "\\";
                textBox_inputpath.Text = FileName;
                inPath = FileName;
            }
            if (textBox_inputpath.Text.Length != 0 && textBox_outputpath.Text.Length != 0)
            {
                button_start.Enabled = true;
            }*/

            // zhj.改为多选对话框
            openFileDialog.InitialDirectory = StringPathLoad;  //上次目录
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "ASCII 文件 (*.txt)|*.txt|ANUDEM 文件 (*.DEM)|*.DEM|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                inPath = new string[openFileDialog.FileNames.Length];
                outDir = new string[openFileDialog.FileNames.Length];

                for (int i=0; i<openFileDialog.FileNames.Length; ++i)
                {
                    inPath[i] = openFileDialog.FileNames[i];
                    outDir[i] = System.IO.Path.GetFileName(inPath[i]);
                    outDir[i] = outDir[i].Substring(0, outDir[i].LastIndexOf("."));
                }

                string FileName = inPath[0];
                StringPathLoad = FileName.Substring(0, FileName.IndexOf("\\") - 1) + "\\";
                for(int i=0; i<openFileDialog.FileNames.Length; ++i)
                {
                    textBox_inputpath.Text =textBox_inputpath.Text+ inPath[i]+";";
                }
            }

            if (textBox_inputpath.Text.Length != 0 && textBox_outputpath.Text.Length != 0)
            {
                button_start.Enabled = true;
            }

        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox_inputpath.Text.Length == 0 || textBox_outputpath.Text.Length == 0)
            {
                button_start.Enabled = false;
            }
            //f.Path.Equals(StringPathSave);//上次路径
            if (StringPathSave != null)
            {
                f.SelectedPath= StringPathSave;
            }
            if (f.ShowDialog()!= DialogResult.OK)
            {
                return;
            }

            textBox_outputpath.Text = f.SelectedPath;
            StringPathSave = f.SelectedPath;//获得保存路径
            outPath = f.SelectedPath;


            if (Profix_YN)
            {
                string m_Prefix = "LS0";
                int i = 1;
                //DirectoryInfo di = new DirectoryInfo(outPath);
                //DirectoryInfo[] dirs = di.GetFiles(m_Prefix + "*");

                string[] files;
                if (Directory.Exists(outPath))
                {
                    files = Directory.GetFiles(outPath, "LS**.txt");

                    foreach (string fiTemp in files)
                    {
                        // Debug.WriteLine("name = "+fiTemp.Name);
                        int ilength = fiTemp.LastIndexOf('\\');
                        string temp = fiTemp.Substring(ilength + 1);
                        //Debug.WriteLine("the temp is :" + temp);
                        //Debug.WriteLine(m_Prefix + i.ToString() + "CSLE_LS.txt");
                        if (i < 10)
                            m_Prefix = "LS0";
                        else
                            m_Prefix = "LS";
                        if (temp == (m_Prefix + i.ToString() + "CSLE_LS.txt"))
                        {
                            i++;
                            continue;
                        }
                        else if (temp == (m_Prefix + i.ToString() + "CSLE_S.txt"))
                        {
                            i++;
                            continue;
                        }
                        else if (temp == (m_Prefix + i.ToString() + "CSLE_L.txt"))
                        {
                            i++;
                            continue;
                        }
                        else if (temp == (m_Prefix + i.ToString() + "LogFile.txt"))
                        {
                            i++;
                            continue;
                        }
                        else if (temp == (m_Prefix + i.ToString() + "Cell_Len.txt"))
                        {
                            i++;
                            continue;
                        }
                        else if (temp == (m_Prefix + i.ToString() + "Slp_Ang.txt"))
                        {
                            i++;
                            continue;
                        }
                        else
                        {
                            //break;
                        }
                    }
                    qianzhui = m_Prefix + i.ToString();
                    textBox_profix.Text = qianzhui;
                    //Debug.WriteLine("textBox5 = " + textBox5.Text);
                    if (textBox_inputpath.Text.Length != 0 && textBox_outputpath.Text.Length != 0)
                    {
                        button_start.Enabled = true;
                    }
                }
            }
            else
            {
                string m_Prefix = "LS";
                //int i = 1;
                qianzhui = m_Prefix; //+ i.ToString();
                textBox_profix.Text = qianzhui;
                //Debug.WriteLine("textBox5 = " + textBox5.Text);
                if (textBox_inputpath.Text.Length != 0 && textBox_outputpath.Text.Length != 0)
                {
                    button_start.Enabled = true;
                }
            }
        }
        public class FolderDialog : FolderNameEditor
        {
            FolderNameEditor.FolderBrowser fDialog = new System.Windows.Forms.Design.FolderNameEditor.FolderBrowser();
            public FolderDialog()
            {
            }
            public DialogResult DisplayDialog()
            {
                return DisplayDialog("Please select a folder");
            }

            public DialogResult DisplayDialog(string description)
            {
                fDialog.Description = description;
                return fDialog.ShowDialog();
            }
            public string Path
            {
                get
                {
                    return fDialog.DirectoryPath;
                }
            }

            ~FolderDialog()
            {
                fDialog.Dispose();
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
           
            //form2.Location = new Point(this.Location.X+100,this.Location.Y+150);
            // zhj.修改输入路径判空条件
            if (inPath.Length == 0)
            {
                MessageBox.Show("Please select a DEM file");
                return;
            }
            /*if (inPath == null)
            {
                MessageBox.Show("Please select a DEM file");
                return;
            }*/

            if (outPath == null)
            {
                MessageBox.Show("Output path can not be empty!");
                return;
            }

            if (radioButton_nodata_repair_no.Checked)
            {
                xiufu = false;//修复无值栅格
            }

            //if (radioButton1.Checked)
            //{
            //    guodu = true;
            //}//2010.01.05修改,以前没有else语句，存在问题
            //else
            //{
            //    guodu = false;
            //}
            if (radioButton_fill_min.Checked)
            {
                fillWay = false;
            }
            //2010.6.8改，无须选择单位了
            //if (radioButton_mul_dir.Checked)
            //{
            //    unit = false;
            //}            
            //if (radioButton7.Checked)
            //{
            //    unit = true;
            //}
            if (radioButton_channel_no.Checked)
            {
                //CumulatWay = false;
                Channel_Consider = false;
                threshold = 0;
            }
            else
            {
                //CumulatWay = true;
                Channel_Consider = true;
                threshold = Convert.ToSingle(textBox_threshold.Text);//2011.04.20目前只有D8和MS多流向的方法中涉及到了河网的问题
            }
            if (radioButton_cutoff_no.Checked)
            {
                flowcut = false;                
            }
            else
            {
                flowcut = true;
            }
            if (radioButton_sinkfill_no.Checked)
            {
                fillsink = false; 
            }
            else
            {
                fillsink = true;
            }
            if (radioButton_Cumulated_Max.Checked)
            {
                CumulatWay = true;
            }
            else
            {
                CumulatWay = false;
            }
            if (radioButton_model_CSLE.Checked)
            {
                RUSLE_CSLE = 1;//CSLE
            }
            else
            {
                RUSLE_CSLE = 0;//RUSLE
            }
            button_start.Enabled = false;
            qianzhui = textBox_profix.Text;
            xiaoyu5 = float.Parse(textBox_cutoff_slopeless.Text);
            dayu5 = float.Parse(textBox_cutoff_slopegreat.Text);

            form2 = new Form2();
            form2.progressTextBox.Text = "OK";
            // zhj.开始计算
            for (int fileCount = 0; fileCount < inPath.Length; ++fileCount)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                string form2Title = "正在计算 Calculating:  " 
                    + (fileCount) + "/" + inPath.Length + " missions are completed";
                form2.Text = form2Title;
                //form2.progressTextBox
                form2.progressBar.Value = 0;
                form2.progressBar.Maximum = 28;
                form2.progressTextBox.Update();
                form2.Owner = this;
                //form2.StartPosition = this.StartPosition;
                form2.StartPosition = FormStartPosition.CenterScreen;
                form2.Show();
                //2010.06.06添加单流向及多流向的选择
                //string tempOutPath = outPath;
                //string tempQianZhui = "";
                string tempOutPath = outPath;
                string tempQianZhui = outDir[fileCount];
                try
                {
                    //tempOutPath += ("\\" + outDir[fileCount]);
                    //tempOutPath += DateTime.Now.ToString(" yyyy_MM_dd HH_mm_ss");
                    //Directory.CreateDirectory(tempOutPath);
                    if (radioButton_flowdir_mul.Checked)
                    {
                        if (radioButton_Mul_MS.Checked)
                        {
                            LS_MultDir_MSClass lsClass = new LS_MultDir_MSClass();
                            //lsClass.LSBegin(inPath, outPath, qianzhui, xiufu, xiaoyu5, dayu5, unit, fillWay, flowcut, fillsink, threshold, ExportAll, MyCheckBox, this);
                            lsClass.LSBegin(inPath[fileCount], tempOutPath, tempQianZhui, xiufu, xiaoyu5, dayu5, unit, fillWay, flowcut, fillsink, threshold, CumulatWay, RUSLE_CSLE, Channel_Consider, ExportAll, MyCheckBox, this);
                            lsClass = null;
                        }
                        else if (radioButton_Mul_FMFD.Checked)
                        {
                            LS_MultDir_FMFDClass lsClass = new LS_MultDir_FMFDClass();
                            // 显示一个窗口
                            lsClass.LSBegin(inPath[fileCount], tempOutPath, tempQianZhui, xiufu, xiaoyu5, dayu5, unit, fillWay, flowcut, fillsink, ExportAll, MyCheckBox, float.Parse(textBox_slope_exp.Text), this);
                            lsClass = null;
                        }
                        else if (radioButton_Mul_DEMON.Checked)
                        {
                            LS_MultDir_DEMONClass lsClass = new LS_MultDir_DEMONClass();
                            lsClass.LSBegin(inPath[fileCount], tempOutPath, tempQianZhui, xiufu, xiaoyu5, dayu5, unit, fillWay, CumulatWay, ExportAll, MyCheckBox, this);
                            lsClass = null;

                        }
                        else
                        {
                            LS_MultDir_PilesjClass lsClass = new LS_MultDir_PilesjClass();
                            lsClass.LSBegin(inPath[fileCount], tempOutPath, tempQianZhui, xiufu, xiaoyu5, dayu5, unit, fillWay, CumulatWay, ExportAll, MyCheckBox, this);
                            lsClass = null;
                        }

                    }
                    else
                    {
                        if (radioButton_Sgn_D8.Checked)
                        {
                            LS_SingleDir_D8Class lsClass = new LS_SingleDir_D8Class();
                            lsClass.LSBegin(inPath[fileCount], tempOutPath, tempQianZhui, xiufu, xiaoyu5, dayu5, unit, fillWay, flowcut, fillsink, threshold, CumulatWay, RUSLE_CSLE, Channel_Consider, ExportAll, MyCheckBox, this);
                            //导入路径、输出路径、前缀、是否修复、小于5%、大于5%、单位、填充方式、是否截断、是否进行洼地填充、阈值、是否考虑河网、哪一个模型、是否所有都输出、输出的复选框选择
                            lsClass = null;
                        }
                        else if (radioButton_Sgn_Rho4.Checked)
                        {
                            LS_SingleDir_Rho4Class lsClass = new LS_SingleDir_Rho4Class();
                            lsClass.LSBegin(inPath[fileCount], tempOutPath, tempQianZhui, xiufu, xiaoyu5, dayu5, unit, fillWay, CumulatWay, ExportAll, MyCheckBox, this);
                            lsClass = null;
                        }
                        else if (radioButton_Sgn_Rho8.Checked)
                        {
                            LS_SingleDir_Rho8Class lsClass = new LS_SingleDir_Rho8Class();
                            lsClass.LSBegin(inPath[fileCount], tempOutPath, tempQianZhui, xiufu, xiaoyu5, dayu5, unit, fillWay, CumulatWay, ExportAll, MyCheckBox, this);
                            lsClass = null;

                        }
                        else if (radioButton_Sgn_Rho.Checked)
                        {
                            LS_SingleDir_RhoClass lsClass = new LS_SingleDir_RhoClass();
                            lsClass.LSBegin(inPath[fileCount], tempOutPath, tempQianZhui, xiufu, xiaoyu5, dayu5, unit, fillWay, CumulatWay, ExportAll, MyCheckBox, this);
                            lsClass = null;

                        }//以上是单流向,以下是多流向

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("物理内存空间不足，无法完成计算！" + ex.ToString(), "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    form2.Close();
                    form2.Dispose();
                }
            }
            form2.Text = "全部文件计算完成(All missions are completed)";
            form2.button_form2_ok.Visible = true;
            //form2.Dispose();
            button_start.Enabled = false;
            textBox_outputpath.Text = "";
            textBox_inputpath.Text = "";
            // 清空路径数组 
            inPath = null;
            outPath = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            string pattern = @"^[0-9]|\.$";
            Regex reg = new Regex(pattern);
            if ((!reg.Match(e.KeyChar.ToString()).Success) && (e.KeyChar.ToString() != "\b"))
            {
                e.Handled = true;
            }
            else if (e.KeyChar.ToString() == "." && (sender as TextBox).Text.IndexOf('.') > 0)
            {
                e.Handled = true;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            string pattern = @"^[0-9]|\.$";
            Regex reg = new Regex(pattern);
            if ((!reg.Match(e.KeyChar.ToString()).Success) && (e.KeyChar.ToString() != "\b"))
            {
                e.Handled = true;
            }
            else if (e.KeyChar.ToString() == "." && (sender as TextBox).Text.IndexOf('.') > 0)
            {
                e.Handled = true;
            }
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void toolStripProgressBar1_Click(object sender, EventArgs e)
        {

        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //if (Overdue)
            SettingFile_LookOver(true);

            if (Profix_YN)//修改前缀文本框中的内容
            {
                Profix_str = "LS01";
                textBox_profix.Text = Profix_str;
            }
            else
            {
                Profix_str = "LS";
                textBox_profix.Text = Profix_str;
            }
            groupBox_flowdirsgn_select.Left = groupBox_flowdirmul_select.Left;
            groupBox_flowdirsgn_select.Top = groupBox_flowdirmul_select.Top;
        }
        private void SettingFile_LookOver(bool Read_Write)
        {
            //Read_Write->true/read,false/write
            string strUserTempPath;
            string strSysTempPath;
            string strPersTempPath;
            System.IO.DirectoryInfo DirInfo_User;
            System.IO.DirectoryInfo DirInfo_Sys;
            System.IO.DirectoryInfo DirInfo_Pers;
            strUserTempPath = Path.GetTempPath().ToString();
            strSysTempPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.System);
            strPersTempPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            //MessageBox.Show(strUserTempPath);
            //MessageBox.Show(strSysTempPath);
            //MessageBox.Show(strPersTempPath);
            //Temp_Dir = strWSName;
            DirInfo_User = new System.IO.DirectoryInfo(strUserTempPath);
            DirInfo_Sys = new System.IO.DirectoryInfo(strSysTempPath);
            DirInfo_Pers = new System.IO.DirectoryInfo(strPersTempPath);
            string FileFullNameUser = strUserTempPath + "\\User.log";
            string FileFullNameSys = strSysTempPath + "\\Sys.log";
            string FileFullNamePers = strPersTempPath + "\\Pers.log";
            if (Read_Write)//读文件
            {
                if (System.IO.File.Exists(FileFullNameUser)
                    && System.IO.File.Exists(FileFullNameSys)
                    && System.IO.File.Exists(FileFullNamePers))
                {//如果三个文件都存在
                    StreamReader sr1 = new StreamReader(FileFullNameUser);
                    StreamReader sr2 = new StreamReader(FileFullNameSys);
                    StreamReader sr3 = new StreamReader(FileFullNamePers);
                    //BinaryWriter w = new BinaryWriter(fs);                    
                    //for(int i=0;i<FileNum;i++)
                    for (int j = 0; j < Settingfile_lines; j++)
                    {
                        Settingfile_line[0, j] = sr1.ReadLine();
                        Settingfile_line[1, j] = sr2.ReadLine();
                        Settingfile_line[2, j] = sr3.ReadLine();
                    }
                    sr1.Close();
                    sr2.Close();
                    sr3.Close();
                    if(Overdue)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            if (Settingfile_line[0, i] != Settingfile_line[1, i] || Settingfile_line[0, i] != Settingfile_line[2, i])
                            {
                                Forbiden = true;
                                MessageBox.Show("日志文件使用次数参数配置不正确！", "系统错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.Close();

                            }
                        }
                        date1 = Convert.ToInt32(Settingfile_line[0, dateline]);
                        date2 = Convert.ToInt32(Settingfile_line[1, dateline]);
                        date3 = Convert.ToInt32(Settingfile_line[2, dateline]);
                        datenow = DateTime.Today.Date.DayOfYear;
                        int datedif = datenow - date1;
                        int dateresult = date1 * date1 - 30 * date1 + date2;//修改日期文件            
                        if (date3 == dateresult && Math.Abs(datedif) < 30)
                        {
                            x1 = Convert.ToInt32(Settingfile_line[0, usecount]);
                            x2 = Convert.ToInt32(Settingfile_line[1, usecount]);
                            x3 = Convert.ToInt32(Settingfile_line[2, usecount]);
                            if (x1 * x1 + 2 * x1 + 3 + x2 * x2 + 2 * x2 + 3 == x3)
                            {
                                Forbiden = false;
                                if (Math.Log(x1, 2) < Limitnum)
                                    Setting_Status();
                                else
                                {
                                    Forbiden = true;
                                    MessageBox.Show("您已超过使用次数！", "警告！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    this.Close();
                                }
                            }
                            else
                            {
                                Forbiden = true;
                                MessageBox.Show("日志文件使用次数参数配置不正确！", "系统错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.Close();
                            }
                            Forbiden = false;
                        }
                        else
                        {
                            Forbiden = true;
                            MessageBox.Show("日志文件日期参数配置不正确！", "系统错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            this.Close();
                        }
                    }
                    else
                    {
                        Setting_Status();
                    }
                }
                else//如果三个文件其中某些不存在
                {
                    if (!Overdue)
                    {//如果三个文件都不存在
                        Forbiden = false;
                        x1 = 1;//第一次使用的值，以后为*2
                        x2 = 2;//第一次使用的值，以后为平方
                        x3 = x1 * x1 + 2 * x1 + 3 + x2 * x2 + 2 * x2 + 3;
                        for (int i = 0; i < FileNum; i++)
                            for (int j = 0; j < Settingfile_lines; j++)
                            {
                                Settingfile_line[i, 0] = "LS日志文件 V 3.0";
                                Settingfile_line[i, 1] = "软件设计完成人：杨勤科、张宏鸣";
                                Settingfile_line[i, 2] = "上次最后打开文件的路径为：";
                                Settingfile_line[i, 3] = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                                Settingfile_line[i, 4] = "上次最后保存文件的路径为：";
                                Settingfile_line[i, 5] = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                                Settingfile_line[i, 6] = "创建日期";
                                //Settingfile_line[i, 7] = DateTime.Today.Date.DayOfYear.ToString();
                                Settingfile_line[i, 8] = "使用次数";
                                Settingfile_line[i, 10] = "是";
                                Settingfile_line[i, 11] = "0.7";
                                Settingfile_line[i, 12] = "0.5";
                                Settingfile_line[i, 13] = "是";
                                Settingfile_line[i, 14] = "是";
                                Settingfile_line[i, 15] = "平均值";
                                Settingfile_line[i, 16] = "考虑沟道";
                                Settingfile_line[i, 17] = "单流向";
                                Settingfile_line[i, 18] = "100000";
                                Settingfile_line[i, 19] = "CSLE";
                                Settingfile_line[i, 20] = "MAX";

                            }

                        x1 = x1 * 2;
                        x2 = x2 * x2;
                        x3 = x1 * x1 + 2 * x1 + 3 + x2 * x2 + 2 * x2 + 3;
                        date1 = DateTime.Today.Date.DayOfYear;
                        date2 = 1;//*3
                        date3 = date1 * date1 - 30 * date1 + date2;
                        Setting_Status();
                        return;
                    }
                    else
                    {
                        if (!System.IO.File.Exists(FileFullNameUser)
                        && !System.IO.File.Exists(FileFullNameSys)
                        && !System.IO.File.Exists(FileFullNamePers))
                        {//如果三个文件都不存在
                            Forbiden = false;
                            x1 = 1;//第一次使用的值，以后为*2
                            x2 = 2;//第一次使用的值，以后为平方
                            x3 = x1 * x1 + 2 * x1 + 3 + x2 * x2 + 2 * x2 + 3;
                            for (int i = 0; i < FileNum; i++)
                                for (int j = 0; j < Settingfile_lines; j++)
                                {
                                    Settingfile_line[i, 0] = "LS日志文件 V 3.0";
                                    Settingfile_line[i, 1] = "软件设计完成人：杨勤科、张宏鸣";
                                    Settingfile_line[i, 2] = "上次最后打开文件的路径为：";
                                    Settingfile_line[i, 3] = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                                    Settingfile_line[i, 4] = "上次最后保存文件的路径为：";
                                    Settingfile_line[i, 5] = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                                    Settingfile_line[i, 6] = "创建日期";
                                    Settingfile_line[i, 7] = DateTime.Today.Date.DayOfYear.ToString();
                                    Settingfile_line[i, 8] = "使用次数";
                                    Settingfile_line[i, 10] = "是";
                                    Settingfile_line[i, 11] = "0.7";
                                    Settingfile_line[i, 12] = "0.5";
                                    Settingfile_line[i, 13] = "是";
                                    Settingfile_line[i, 14] = "是";
                                    Settingfile_line[i, 15] = "平均值";
                                    Settingfile_line[i, 16] = "考虑沟道";
                                    Settingfile_line[i, 17] = "单流向";
                                    Settingfile_line[i, 18] = "100000";
                                    Settingfile_line[i, 19] = "CSLE";
                                    Settingfile_line[i, 20] = "MAX";
                                }

                            x1 = x1 * 2;
                            x2 = x2 * x2;
                            x3 = x1 * x1 + 2 * x1 + 3 + x2 * x2 + 2 * x2 + 3;
                            date1 = DateTime.Today.Date.DayOfYear;
                            date2 = 1;//*3
                            date3 = date1 * date1 - 30 * date1 + date2;
                            Setting_Status();
                            return;
                        }
                        else
                        {
                            Forbiden = true;
                            MessageBox.Show("日志文件参数配置不正确！", "系统错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            this.Close();
                        }
                    }
                }

            }
            else//写文件
            {              
                    System.IO.File.Delete(FileFullNameUser);
                    System.IO.File.Delete(FileFullNameSys);
                    System.IO.File.Delete(FileFullNamePers);
                    StreamWriter sw1 = File.CreateText(FileFullNameUser);
                    StreamWriter sw2 = File.CreateText(FileFullNameSys);
                    StreamWriter sw3 = File.CreateText(FileFullNamePers);
                    //Remember_Status();  
                    for (int i = 0; i < FileNum; i++)
                        for (int j = 0; j < Settingfile_lines; j++)
                        {
                            Settingfile_line[i, 0] = "LS日志文件";
                            Settingfile_line[i, 1] = "软件设计完成人：杨勤科、张宏鸣";
                            Settingfile_line[i, 2] = "上次最后打开文件的路径为：";
                            Settingfile_line[i, 3] = StringPathLoad;
                            Settingfile_line[i, 4] = "上次最后保存文件的路径为：";
                            Settingfile_line[i, 5] = StringPathSave;
                            Settingfile_line[i, 6] = "创建日期";
                            Settingfile_line[i, 7] = DateTime.Today.Date.ToString();
                            Settingfile_line[i, 8] = "使用次数";
                        }
                    x1 = x1 * 2;
                    x2 = x2 * x2;
                    x3 = x1 * x1 + 2 * x1 + 3 + x2 * x2 + 2 * x2 + 3;
                    //Settingfile_line[0, usecount] = x1.ToString();
                    //Settingfile_line[1, usecount] = x2.ToString();
                    //Settingfile_line[2, usecount] = x3.ToString();
                    //date1 = DateTime.Today.Date.DayOfYear;
                    date2 = date2 * 3;//*3
                    date3 = date1 * date1 - 30 * date1 + date2;
                    Remember_Status();
                    for (int j = 0; j < Settingfile_lines; j++)
                    {
                        sw1.WriteLine(Settingfile_line[0, j]);
                        sw2.WriteLine(Settingfile_line[1, j]);
                        sw3.WriteLine(Settingfile_line[2, j]);
                    }
                    sw1.Close();
                    sw2.Close();
                    sw3.Close();
                }
            
        }
        private void Setting_Status()
        {
            if (Overdue)
            {
                Settingfile_line[0, usecount] = x1.ToString();
                Settingfile_line[1, usecount] = x2.ToString();
                Settingfile_line[2, usecount] = x3.ToString();
                int x = Convert.ToInt16(10 - Math.Log(x1, 2));
                this.Text = "区域LS因子计算工具(试用版您还可以使用" + x.ToString() + "次）";
            }
            Settingfile_line[0, dateline] = date1.ToString();
            Settingfile_line[1, dateline] = date2.ToString();
            Settingfile_line[2, dateline] = date3.ToString();
            openFileDialog.InitialDirectory = Settingfile_line[0, 3];
            StringPathSave=Settingfile_line[0, 5] ;
            if (Settingfile_line[0, usecount + 1] == "否")
            {
                radioButton_cutoff_yes.Checked = false;
                radioButton_cutoff_no.Checked = true;
            }
            else
            {
                radioButton_cutoff_yes.Checked = true;
                radioButton_cutoff_no.Checked = false;
            }
            textBox_cutoff_slopeless.Text = Settingfile_line[0, usecount + 2];
            textBox_cutoff_slopegreat.Text = Settingfile_line[0, usecount + 3];
            if (Settingfile_line[0, usecount + 4] == "是")
            {
                radioButton_sinkfill_yes.Checked = true;
                radioButton_sinkfill_no.Checked = false;
            }
            else
            {
                radioButton_sinkfill_yes.Checked = false;
                radioButton_sinkfill_no.Checked = true;
            }
            if (Settingfile_line[0, usecount + 5] == "是")
            {
                radioButton_nodata_repair_yes.Checked = true;
                radioButton_nodata_repair_no.Checked = false;
            }
            else
            {
                radioButton_nodata_repair_yes.Checked = false;
                radioButton_nodata_repair_no.Checked = true;
            }
            if (Settingfile_line[0, usecount + 6] == "平均值")
            {
                radioButton_fill_aver.Checked = true;
                radioButton_fill_min.Checked = false;
            }
            else
            {
                radioButton_fill_aver.Checked = false;
                radioButton_fill_min.Checked = true;
            }
            if (Settingfile_line[0, usecount + 8] == "单流向")
            {
                radioButton_flowdir_sgn.Checked = true;
                radioButton_flowdir_mul.Checked = false;
                groupBox_Cumulated_Way.Enabled = true;
            }
            else
            {
                radioButton_flowdir_sgn.Checked = false;
                radioButton_flowdir_mul.Checked = true;
                groupBox_Cumulated_Way.Enabled = false;
            }
            if (Settingfile_line[0, usecount + 7] == "考虑沟道")
            {
                radioButton_channel_no.Checked = false;
                radioButton_channel_yes.Checked = true;                
            }
            else
            {
                radioButton_channel_yes.Checked = false;
                radioButton_channel_no.Checked = true;
            }            
           
            textBox_threshold.Text=Settingfile_line[0, usecount + 9];//阈值
            //if (Settingfile_line[0, usecount + 9] == "最大值累计")
            //{
            //    radioButton_channel_no.Checked = false;
            //    radioButton_channel_yes.Checked = true;
            //}
            //else
            //{
            //    radioButton_channel_no.Checked = true;
            //    radioButton_channel_yes.Checked = false;
            //}
            if (Settingfile_line[0, usecount + 10] == "CSLE")
            {
                radioButton_model_RUSLE.Checked = false;
                radioButton_model_CSLE.Checked = true;                
            }
            else
            {
                radioButton_model_RUSLE.Checked = true;
                radioButton_model_CSLE.Checked = false;
                
            }
            if (Settingfile_line[0, usecount + 11] == "MAX")
            {
                radioButton_Cumulated_Total.Checked = false;
                radioButton_Cumulated_Max.Checked = true;                
            }
            else
            {
                radioButton_Cumulated_Total.Checked = true;
                radioButton_Cumulated_Max.Checked = false;        
            }
        }
        private void Remember_Status()
        {
            Settingfile_line[0, usecount] = x1.ToString();
            Settingfile_line[1, usecount] = x2.ToString();
            Settingfile_line[2, usecount] = x3.ToString();
            Settingfile_line[0, dateline] = date1.ToString();
            Settingfile_line[1, dateline] = date2.ToString();
            Settingfile_line[2, dateline] = date3.ToString();
            for (int i = 0; i < 3; i++)
            {
                if (radioButton_cutoff_yes.Checked)
                {
                    Settingfile_line[i, usecount + 1] = "是";
                }
                else
                {
                    Settingfile_line[i, usecount + 1] = "否";
                }
                Settingfile_line[i, usecount + 2] = textBox_cutoff_slopeless.Text;
                Settingfile_line[i, usecount + 3] = textBox_cutoff_slopegreat.Text;
                if (radioButton_sinkfill_yes.Checked)
                {
                    Settingfile_line[i, usecount + 4] = "是";
                }
                else
                {
                    Settingfile_line[i, usecount + 4] = "否";
                }
                if (radioButton_nodata_repair_yes.Checked)
                {
                    Settingfile_line[i, usecount + 5] = "是";
                }
                else
                {
                    Settingfile_line[i, usecount + 5] = "否";
                }
                
                if (radioButton_fill_aver.Checked)
                {
                    Settingfile_line[i, usecount + 6] = "平均值";
                }
                else
                {
                    Settingfile_line[i, usecount + 6] = "最小值";
                }
                if (radioButton_channel_yes.Checked)
                {
                    Settingfile_line[i, usecount + 7] = "考虑沟道";
                }
                else
                {
                    Settingfile_line[i, usecount + 7] = "不考虑沟道";
                }
                if (radioButton_flowdir_sgn.Checked)
                {
                    Settingfile_line[i, usecount + 8] = "单流向";
                }
                else
                {
                    Settingfile_line[i, usecount + 8] = "多流向";
                }
                Settingfile_line[i, usecount + 9] = textBox_threshold.Text;
                if (radioButton_model_RUSLE.Checked)
                {
                    Settingfile_line[0, usecount + 10] = "RUSLE";                    
                }
                else
                {
                    Settingfile_line[0, usecount + 10] = "CSLE";                    
                }
                if (radioButton_Cumulated_Total.Checked)
                {
                    Settingfile_line[0, usecount + 11] = "TOTAL";
                }
                else
                {
                    Settingfile_line[0, usecount + 11] = "MAX";
                }
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!Forbiden)
            {
                Timer_Cancel_Close.Stop();
                Timer_Before_Closed.Start();
                this.Refresh();
                DialogResult Close_Now;
                Close_Now = MessageBox.Show("Exit？", "Warning！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                this.Refresh();
                if (Close_Now == DialogResult.Yes)
                {
                    Timer_Close.Start();
                    //if (Overdue)
                    SettingFile_LookOver(false);
                    e.Cancel = false;
                }
                else
                {
                    //eventLog.Source = "mySource";
                    //eventLog.WriteEntry("Log text");
                    Timer_Before_Closed.Stop();
                    Timer_Cancel_Close.Start();
                    this.Opacity = 1;
                    e.Cancel = true;
                }

            }
            else
            {
                Timer_Cancel_Close.Stop();
                Timer_Before_Closed.Start();
                this.Refresh();
                DialogResult Close_Now;
                Close_Now = MessageBox.Show("Exit？", "Warning！", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                this.Refresh();
                if (Close_Now == DialogResult.Yes)
                {
                    Timer_Close.Start();
                    //if (Overdue)
                    SettingFile_LookOver(false);
                    e.Cancel = false;
                }
                else
                {
                    //eventLog.Source = "mySource";
                    //eventLog.WriteEntry("Log text");                
                    Timer_Before_Closed.Stop();
                    Timer_Cancel_Close.Start();
                    //this.Opacity = 1;
                    e.Cancel = true;
                }
            }
        }
        private void Timer_Before_Closed_Tick(object sender, EventArgs e)
        {
            if (this.Opacity > 0.5)
            {
                this.Opacity -= 0.01;
            }
        }

        private void Timer_Cancel_Close_Tick(object sender, EventArgs e)
        {
            if (this.Opacity < 1)
            {
                this.Opacity += 0.01;
                this.Refresh();
            }
            else
            {
                Timer_Cancel_Close.Stop();
            }
        }

        private void Timer_Close_Tick(object sender, EventArgs e)
        {

            if (this.Opacity > 0)
            {
                this.Opacity -= 0.02;
            }
            else
            {
                Menu_Closed();
            }
        }
        private void Menu_Closed()
        {
            System.Environment.Exit(System.Environment.ExitCode); //关闭系统所有线程
            Application.Exit();
        }

        private void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("Help\\index.htm");
        }

        private void checkBox_SelectAll_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            if (checkBox_Save_SelectAll.Checked)
            {
                for (i = 1; i <= ExportNum; i++)
                {
                    MyCheckBox[i].Checked = true;
                    MyCheckBox[i].Enabled = false;
                }
            }
            else
            {
                for (i = 1; i <= ExportNum; i++)
                {
                    MyCheckBox[i].Checked = false;
                    MyCheckBox[i].Enabled = true;
                }
            }

        }

        private void checkBox_SlopLength_CheckedChanged(object sender, EventArgs e)
        {
            bool SelectAll = true;
            int i;
            for (i = 1; i <= ExportNum; i++)
            {
                if (MyCheckBox[i].Checked == false)
                {
                    SelectAll = false;
                }
            }
            if (SelectAll)
                checkBox_Save_SelectAll.Checked = true;
        }

        private void radioButton_sgl_dir_CheckedChanged(object sender, EventArgs e)
        {
            
            if (radioButton_flowdir_sgn.Checked)
            {
                
                groupBox_flowdirmul_select.Visible = false;
                groupBox_flowdirsgn_select.Visible = true;
                groupbox_slope_exp.Enabled = false;
                groupBox_Cumulated_Way.Enabled = true;

            }
            if (radioButton_flowdir_mul.Checked)
            {
                groupBox_flowdirsgn_select.Visible = false;
                groupBox_flowdirmul_select.Visible = true;
                groupBox_Cumulated_Way.Enabled = false;
                if(radioButton_Mul_FMFD.Checked)
                    groupbox_slope_exp.Enabled = true;


            }
        }

        private void radioButton_mul_dir_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_flowdir_sgn.Checked)
            {
                groupBox_flowdirmul_select.Visible = false;
                groupBox_flowdirsgn_select.Visible = true;
                groupBox_Cumulated_Way.Enabled = true;
                radioButton_channel_no.Enabled = true;
                radioButton_channel_yes.Enabled = true;
                radioButton_channel_yes.Checked = false;
                radioButton_channel_no.Checked = true;

            }
            if (radioButton_flowdir_mul.Checked)
            {
                groupBox_flowdirsgn_select.Visible = false;
                groupBox_flowdirmul_select.Visible = true;
                groupBox_Cumulated_Way.Enabled = false;
                radioButton_channel_no.Enabled = true;
                radioButton_channel_no.Checked = true;
                //radioButton_channel_yes.Enabled = false;
                radioButton_channel_yes.Checked = false;
                radioButton_channel_no.Checked = true;
            }
        }

        private void button_helpInfo_Click(object sender, EventArgs e)
        {
            if (button_helpInfo.Text == "Help>>")
            {
                this.Width = this.Width + Convert.ToInt32(textBox_helpinfo.Width * 1.2);
                button_helpInfo.Text = "Help<<";
            }
            else
            {
                this.Width = this.Width - Convert.ToInt32(textBox_helpinfo.Width * 1.2);
                button_helpInfo.Text = "Help>>";

            }
        }

        private void textBox_helpinfo_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton_Sgn_D8_Click(object sender, EventArgs e)
        {
            //单击时发生
        }
        //以下是帮助文本内容的提示
        private void textBox1_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    输入用于计算的DEM文件路径（*.txt或*.DEM），或通过右边的按钮进行选择。";
        }
        private void textBox2_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    输入结果文件保存的路径（*.txt），或通过右边的按钮进行选择。";

        }
        private void button1_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    选择用于计算的DEM文件（ASCII格式）路径,该文件是ARCGIS通过Raster to ASCII导出的文本文件（*.txt），也可以是ANUDEM使用的*.DEM格式的文件。";

        }
        private void button2_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    选择LS-TOOL结果文件的保存位置。";

        }

        private void textBox5_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    设置输出结果文件的前缀名，如果选择保存的目录中存在该前缀的文件，系统自动修改前缀名，防止覆盖前面的计算结果。";

        }

        private void checkBox_SelectAll_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    保存全部计算结果，包括坡长、坡度、坡长因子、坡度因子、LS因子等五个文件。";

        }

        private void textBox3_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    设置截断因子值。";

        }

        private void checkBox_SlopLength_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    保存坡度文件。";

        }

        private void checkBox_SlopAngle_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    保存坡长文件。";

        }

        private void checkBox_LengthFactor_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    保存坡度因子文件。";

        }

        private void checkBox_SlopFactor_MouseHover(object sender, EventArgs e)
        { 
            textBox_helpinfo.Text = "    保存坡长因子文件。";

        }

        private void checkBox_LSFactor_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    保存LS计算结果。";

        }
        private void groupBox_cutoff_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    设置截断，选择“是”则在计算坡长会在下面第一个条件下截断，如果将“是否沟道截断”也选择“是”，则在计算过程中的截断条件为下面两种。\r\n"+
                "    截断即为累积坡长结束的地方，发生截断的点分为两种情况:\r\n" +
                "    1. 从一个栅格沿着径流方向到下一个栅格的坡度变化率。因为小于2.86º(约5%)的坡面不产生侵蚀（发生沉积），所以当坡度小于和大于2.86º时，根据文献[Van Remortel R D, Hamilton M E, Hickey R J. Estimating the LS Factor for RUSLE Through Iterative Slope Length Processing of Digital Elevation Data[J]. Cartography, 2001, 30 (1): 27-35]的建议将中断因子分别设定为0.7和0.5; \r\n" +
                "    2. 到一个定义好的沟道处，截断[Wischmeier W H, Smith D D. Predicting Rainfall Erosion Losses: A Guide to Conservation Planning with Universal Soil Loss Equation(USLE)[M]. Washington, USA: Department of Agriculture, 1978.]。";
        }
        private void groupBox_sink_fill_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    洼地填充，相当于ARCGIS中的FILL命令，在一个完整的流域或区域内，水流应该可以流入，也可以流出，洼地导致该cell没有流出方向，也就无法继续向下累积坡长。\r\n"+
                "    因此如果用于计算的DEM已经进行过FILL的处理，可以选择“否”，不进行洼地处理，减少程序运行时间，如果您的数据没有进行过FILL的处理，强烈建议您选择“是”。";
        }
        //沟道截断
        private void groupBox_accuway_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    选择“是”则坡长在沟道处截断，选择“否”则坡长在沟道处继续累积。\r\n"+
                "    选择“是”后，程序在计算过程中增加了沟道提取的过程，即河网提取，提取的河网需要使用者设置“阈值”，“阈值”是河网提取的关键，它指的是汇水面积的大小，单位是平方米（㎡），也就是说多大的汇水面积即为沟道。\r\n"+
                "    如：如果设置阈值为1000，则表示当汇水面积达到1000㎡的时候为沟道，坡长在此处停止累积。";

        }
        private void groupBox_threshold_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    设置阈值（㎡）。\r\n"+
                "    “阈值”是指汇水面积的大小，单位是平方米（㎡），该值决定了河网的疏密，值越大河网越稀疏，相反越密集，密集的河网使坡长的最大值减小（平均值减小），稀疏的河网使坡长的最大值增大（平均值增大）。该值的设定需要对当地的实际情况有所熟悉，能够设定较为准确的数值。";
        }
        private void groupBox_nodata_repair_MouseHover(object sender, EventArgs e)
        {
             textBox_helpinfo.Text = "    如果用于计算的DEM中存在内部不连续的无值点，则需要选择“是”进行修复，如果无法确定建议选择“是”。"+
                 "    内部不连续的无值点的填充有两种方式，一种是填充最小值，类似于FILL，一种是填充平均值。用户可以自行选择。";
        }

        private void groupBox_nodata_fill_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    内部不连续的无值点的填充有两种方式，一种是填充最小值，类似于FILL，一种是填充平均值。用户可以自行选择。";
        }
        private void groupBox_flowdir_select_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    流向算法的选择，单流向和多流向";
        }
        private void groupBox_Cumulated_Way_MouseHover(object sender, EventArgs e)
        {
            textBox_helpinfo.Text = "    “最大值”方法基于Van Remortel方法，当坡面汇流时取所有流入的最大值；\r\n"+
                "    “全部流入”方法基于张宏鸣的方法，当坡面汇流时，坡长为所有流入之和";
        }
        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void radioButton_Mul_MS_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton_Mul_FMFD_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_Mul_FMFD.Checked)
            {
                groupbox_slope_exp.Enabled = true;
            }
            else
            {
                groupbox_slope_exp.Enabled = false;
            }
        }

        private void radioButton_flowcutno_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_cutoff_no.Checked)
            {
                groupBox_cutoff_set.Enabled = false;
                radioButton_channel_no.Checked = true;
                radioButton_channel_yes.Checked = false;
                groupBox_threshold.Enabled = false;
       
            }
            else
            {
                groupBox_cutoff_set.Enabled = true;
                
            }
        }

        private void button_language_Click(object sender, EventArgs e)
        {
            if (button_language.Text == "English")
            {
                groupBox_inputpath.Text="DEM data file name";
                button_inputpath.Text = "Load DEM";
                groupBox_outputpath.Text="Output file path";
                button_outputpath.Text = "Save";
                groupBox_profix.Text = "Prefix";
                label_profix.Text = "File prefix";
                groupBox_modeling.Text = "Model";
                //textBox_profix.Text = "";
                groupBox_threshold.Text = "Threshold";
                label_threshold.Text = "Unit(㎡)";
                //groupBox_model.Text = "Model";
                groupBox_save.Text = "Select files to save";
                checkBox_Save_SelectAll.Text = "All";
                checkBox_Save_SlopLength.Text = "S";
                checkBox_Save_SlopAngle.Text = "L";
                checkBox_Save_LengthFactor.Text = "S factor";
                checkBox_Save_SlopFactor.Text = "L factor";
                checkBox_Save_LSFactor.Text = "LS factor";
                groupBox_nodata_repair.Text = "Nodata Fill";
                radioButton_nodata_repair_yes.Text = "Yes";
                radioButton_nodata_repair_no.Text = "No";
                groupBox_nodata_fill.Text = "Nodata treated";
                radioButton_fill_aver.Text = "Average";
                radioButton_fill_min.Text = "Min";
                groupBox_cutoff.Text = "Cutoff ?";
                radioButton_cutoff_yes.Text = "Yes";
                radioButton_cutoff_no.Text = "No";
                groupBox_sink_fill.Text = "Sink fill ";
                radioButton_sinkfill_yes.Text = "Yes";
                radioButton_sinkfill_no.Text = "No";
                groupBox_channelcutoff.Text = "Channel？";
                radioButton_channel_yes.Text = "Yes";
                radioButton_channel_no.Text = "No";
                groupBox_Cumulated_Way.Text = "Cumulated way";
                radioButton_Cumulated_Max.Text = "Max";
                radioButton_Cumulated_Total.Text = "Total";
                groupBox_cutoff_set.Text = "Cutoff slope";
                label_cutoff_slopeless.Text = "Slope<2.75°(5%)";
                //textBox_cutoff_slopeless.Text = "";
                label_cutoff_slopegreat.Text = "Slope≥2.75°(5%)";
                //textBox_cutoff_slopegreat.Text = "";
                groupBox_flowdir_select.Text = "Flow direction";
                radioButton_flowdir_sgn.Text = "SFD";
                radioButton_flowdir_mul.Text = "MFD";
                groupbox_slope_exp.Text = "Slope exp";
                label_slope_exp.Text = "exp";
                groupBox_flowdirsgn_select.Text = "Select SFD";
                groupBox_flowdirmul_select.Text = "Select MFD";
                radioButton_Sgn_D8.Text = "D8";
                radioButton_Sgn_Rho4.Text = "Rho4";
                radioButton_Sgn_Rho8.Text = "Rho8";
                radioButton_Sgn_Rho.Text = "Lea";
                radioButton_Mul_MS.Text = "MS";
                radioButton_Mul_FMFD.Text = "FMFD";
                radioButton_Mul_Dinf.Text = "Dinf";
                radioButton_Mul_DEMON.Text = "DEMON";
                button_start.Text = "C&alculate";
                button_cancel.Text = "E&xit";
                this.Text = "LS-TOOL";
                
                button_language.Text = "中文";

            }
            else
            {
                groupBox_inputpath.Text = "源文件位置";
                button_inputpath.Text = "DEM路径及文件名";
                groupBox_outputpath.Text = "输出文件位置";
                button_outputpath.Text = "结果保存位置";
                groupBox_profix.Text = "设置前缀";
                label_profix.Text = "文件前缀";
                groupBox_modeling.Text = "模型";
                //textBox_profix.Text = "";
                groupBox_threshold.Text = "河网阈值";
                label_threshold.Text = "阈值(㎡)";
                //groupBox_model.Text = "模型选择";
                groupBox_save.Text = "保存文件选择";
                checkBox_Save_SelectAll.Text = "全选";
                checkBox_Save_SlopLength.Text = "坡度";
                checkBox_Save_SlopAngle.Text = "坡长";
                checkBox_Save_LengthFactor.Text = "坡度因子";
                checkBox_Save_SlopFactor.Text = "坡长因子";
                checkBox_Save_LSFactor.Text = "坡度坡长因子";
                groupBox_nodata_repair.Text ="无值点";
                radioButton_nodata_repair_yes.Text = "修复";
                radioButton_nodata_repair_no.Text = "不修复";
                groupBox_nodata_fill.Text = "无值点填充";
                radioButton_fill_aver.Text = "平均值";
                radioButton_fill_min.Text = "最小值";
                groupBox_cutoff.Text = "截断？";
                radioButton_cutoff_yes.Text = "是";
                radioButton_cutoff_no.Text = "否";
                groupBox_Cumulated_Way.Text = "累积方式";
                radioButton_Cumulated_Max.Text = "最大值";
                radioButton_Cumulated_Total.Text = "全部流入";
                groupBox_sink_fill.Text = "洼地填充？";
                radioButton_sinkfill_yes.Text = "是";
                radioButton_sinkfill_no.Text = "否";
                groupBox_channelcutoff.Text = "沟道截断";
                radioButton_channel_yes.Text = "是";
                radioButton_channel_no.Text = "否";
                groupBox_cutoff_set.Text = "截断因子设置";
                label_cutoff_slopeless.Text = "坡度 < 2.75°（5%）";
                //textBox_cutoff_slopeless.Text = "";
                label_cutoff_slopegreat.Text = "坡度 ≥ 2.75°（5%）";
                //textBox_cutoff_slopegreat.Text = "";
                groupBox_flowdir_select.Text="流向算法选择";
                radioButton_flowdir_sgn.Text = "单流向";
                radioButton_flowdir_mul.Text = "多流向";
                groupbox_slope_exp.Text = "坡度指数设置";
                label_slope_exp.Text = "坡度指数";
                groupBox_flowdirsgn_select.Text = "单流向算法选择";
                groupBox_flowdirmul_select.Text = "多流向算法选择";
                radioButton_Sgn_D8.Text = "D8（最大坡降法）";
                radioButton_Sgn_Rho4.Text = "Rho4（随机四方向法）";
                radioButton_Sgn_Rho8.Text = "Rho8（随机八方向法）";
                radioButton_Sgn_Rho.Text = "流向驱动算法";
                radioButton_Mul_MS.Text = "MS（基于坡度）";
                radioButton_Mul_FMFD.Text = "FMFD（基于坡度指数）";
                radioButton_Mul_Dinf.Text = "Dinf（无穷方向法）";
                radioButton_Mul_DEMON.Text = "DEMON（流管法）";
                button_start.Text = "开始计算(&C)";
                button_cancel.Text = "退出(&X)";
                this.Text = "区域LS因子计算工具";

                button_language.Text = "English";
            }
        }

        private void radioButton_channel_yes_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_channel_no.Checked)
            {
                groupBox_threshold.Enabled = false;
            }
            else
            {
                if (radioButton_cutoff_no.Checked)
                {
                    MessageBox.Show("您已经选择了不设置截断，因此无法设置沟道截断","操作错误！",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    groupBox_threshold.Enabled = false;
                    radioButton_channel_no.Checked=true;
                    radioButton_channel_yes.Checked = false;
                }
                else
                    groupBox_threshold.Enabled = true;
            }
        }

        private void radioButton_nodata_repair_no_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_nodata_repair_no.Checked)
            {
                groupBox_nodata_fill.Enabled = false;
                
            }
            else
            {
                groupBox_nodata_fill.Enabled = true;
            }
        }













 








    }
}