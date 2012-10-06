using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;

using System.Windows.Forms; 
using System.Threading;
using System.Diagnostics;
namespace LengthSlope
{
    class LS_SingleDir_D8Class
    {
        ////////////////////////////////////////////////////////////////


        /*****************************LS*******************************/


        ////////////////////////////////////////////////////////////////
        //public void LSBegin(string inPath, string outPath, string prefix, bool ifExcessFile, bool ifRepair, float lessThan5, float above5, bool unit, bool fillWay,bool cumway,bool ExportAll,Object form)
        //
        public void LSBegin(string inPath, string outPath, string prefix, bool ifRepair, float lessThan5, float above5, bool unit, bool fillWay, bool flowcut, bool fillsink, float threshold, bool CumulatedWay, int RUSLE_CSLE, bool Channel_Consider, bool ExportAll, CheckBox[] MyCheckBox, Object form)    
        {
            
            Form1 form1 = form as Form1;
            form1.form2.progressTextBox.Text = "准备读入数据Loading DEM……";
            form1.form2.progressBar.Visible = true;
            form1.form2.progressBar.Value = 0;
            
            form1.form2.progressTextBox.Update();
            form1.form2.progressBar.Value++;
           
            DemData demData = new DemData();
            demData.fillWay = fillWay;
            demData.inPath = inPath;
            demData.flowcut = flowcut;
             
            //下载数据源基本信息
            form1.form2.progressTextBox.Text = "获取数据源基本信息Checking basic info……";
            form1.form2.progressTextBox.Update();
            LoadDEMHeaderData(demData.inPath, ref demData);
 
            demData.preOutPath = outPath + prefix;
            demData.scf_lt5 = lessThan5;
            demData.scf_ge5 = above5;

            demData.meterOrFeet = unit;
            demData.ifRepair= ifRepair;//是否修复
            demData.threshold = threshold;
            
            string logFilePath = String.Concat(outPath, "\\", prefix, "LogFile.txt");//日志文件路径
            //打开日志文件
            OpenLogFile(logFilePath);
            LogStartUp(demData);
            LogDemHeader(demData);
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "申请内存空间Applying for new memory......";
            form1.form2.progressTextBox.Update();
            //开辟空间
            float[] demMap = new float[demData.imagNrows * demData.imagNcols];
            bool[] noDataMap = new bool[demData.imagNrows * demData.imagNcols];
          
            //开辟空间失败，写入日志文件，并退出程序
            if (demMap.Length == 0)
                LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "DEM_Map"); //写入日志文件
            if (noDataMap.Length == 0)
                LogFailedExit(demData.imagNcols * demData.imagNrows, 1, "bool", "NODATA Grid Map"); //写入日志文件
       
            //对最外两层填充为无值
            #region FillAroundCell
            {
                int c = 0;
                for (int i = 0; i < demData.imagNrows; i++)
                    for (int j = 0; j < demData.imagNcols; j++)
                    {
                        if (i < 2 || i > (demData.imagNrows - 3))
                        {
                            c = i * demData.imagNcols + j;
                            if (demData.noDateType)
                            {
                                demMap[c] = demData.floatNoData;
                            }
                            else
                            {
                                demMap[c] = demData.intNoData;
                            }
                            noDataMap[c] = true;//填充无值点
                        }
                        else
                        {
                            //不是第一行和最后一行
                            if (j < 2 || (j > demData.imagNcols - 3))
                            {
                                c = i * demData.imagNcols + j;
                                if (demData.noDateType)
                                {
                                    demMap[c] = demData.floatNoData;
                                }
                                else
                                {
                                    demMap[c] = demData.intNoData;
                                }
                                noDataMap[c] = true;
                            }
                        }
                    }
            }
            #endregion
            form1.form2.progressTextBox.Text = "读取DEM......";
            form1.form2.progressTextBox.Update();
            form1.form2.progressBar.Value++;

            ReadDEMElevations(ref demData, ref demMap, ref noDataMap); //读取DEM
            //Console.WriteLine("4");
            form1.form2.progressTextBox.Text = "核查DEM的数据类型Checking DEM data types......";
            form1.form2.progressTextBox.Update();
            form1.form2.progressBar.Value++;
            demData.floatOrInt = VerifyDEMDataType(ref demData); //核实DEM的数据类型
            LogDataTypeDEM(demData); // 将数据类型写入日志文件

            form1.form2.progressTextBox.Text = "填充内部无值区Filling inner nodata area......";
            form1.form2.progressTextBox.Update();
            form1.form2.progressBar.Value++;
            //填充内部无值区
            LocateInteriorNODATACells(demData, ref noDataMap, ref demMap);

            if(ExportAll)//if (demData.ifExcessFile)            
            {
                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                form1.form2.progressTextBox.Text = "重写修订后的DEM数据Saving fixed DEM......";
                form1.form2.progressTextBox.Update();

                string pathFileName = String.Concat(outPath, "\\", prefix, "orig_dem.txt");
                WriteDEMGrid(pathFileName, demData, noDataMap, demMap); //打开一个文件，写DEM
                LogWroteDEM(pathFileName); //写日志文件
            }

            form1.form2.progressTextBox.Text = "洼地填充，环面周围8个点Filling......";
            form1.form2.progressTextBox.Update();
            form1.form2.progressBar.Value++;
            
            bool sf = true;
            bool af = true;
            //如果选择了不进行洼地填充
            if (fillsink == false)
            {
                sf = false;
                af = false;
            }
            while (sf && af)
            {
                sf = FillSinks(demData, ref noDataMap, ref demMap); //填充洼地周围8个点
                af = AnnulusFill(demData, ref noDataMap, ref demMap); //填充环面周围16个点                
            }
            

            form1.form2.progressTextBox.Update();
            form1.form2.progressBar.Value++;

            if (ExportAll)//if (demData.ifExcessFile)            
            {
                form1.form2.progressTextBox.Text = "写填充完的DEM数据Saving Filled DEM......";
                form1.form2.progressTextBox.Update();
                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                string demFill = String.Concat(outPath, "\\", prefix, "DemFill.txt");
                WriteDEMGrid(demFill, demData, noDataMap, demMap); //写填充完的DEM数据
                LogWroteDEM(demFill); //写日志文件
            }
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "申请内存空间Applying for new memory......";
            form1.form2.progressTextBox.Update();

            float[] slopeAng = new float[demData.imagNcols * demData.imagNrows];//坡度
            if (slopeAng == null)
                LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "slope angle");
            float[] slopeLen = new float[demData.imagNcols * demData.imagNrows];//坡长
            if (slopeLen == null)
                LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "cell length");
            byte[] inFlow = new byte[demData.imagNcols * demData.imagNrows];//流入
            if (inFlow == null)
                LogFailedExit(demData.imagNcols * demData.imagNrows, 1, "FLOWTYPE", "inflow direction");
            byte[] outFlow = new byte[demData.imagNcols * demData.imagNrows];//流出
            if (outFlow == null)
                LogFailedExit(demData.imagNcols * demData.imagNrows, 1, "FLOWTYPE", "outflow direction");
            bool[] flowFlag = new bool[demData.imagNcols * demData.imagNrows];//因子
            if (flowFlag == null)
                LogFailedExit(demData.imagNcols * demData.imagNrows, 1, "bool", "flow cutoff rrid map");

            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "对数据进行缓冲区处理Bufferring......";
            form1.form2.progressTextBox.Update();

            //初始化最外两层
            #region
            {
                int c = 0;
                for (int i = 0; i < demData.imagNrows; i++)
                    for (int j = 0; j < demData.imagNcols; j++)
                    {
                        c = i * demData.imagNcols + j;
                        if (i < 2 || i > (demData.imagNrows - 3))
                        {
                            slopeAng[c] = (float)0.0;
                            slopeLen[c] = (float)0.0;
                            outFlow[c] = (byte)0;
                            inFlow[c] = (byte)0;
                            flowFlag[c] = false;
                        }
                        else if (j < 2 || j > (demData.imagNcols - 3))
                        {
                            slopeAng[c] = (float)0.0;
                            slopeLen[c] = (float)0.0;
                            outFlow[c] = (byte)0;
                            inFlow[c] = (byte)0;
                            flowFlag[c] = false;
                        }
                    }
            }
            #endregion
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "获取坡度，流向Calculating slope angle and slope aspect......";
            form1.form2.progressTextBox.Update();

            downSlope(demData, noDataMap, demMap, ref slopeAng, ref slopeLen, ref outFlow); //获取坡度，坡长，流向
            if (ExportAll)//if (demData.ifExcessFile)            
            {
                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                form1.form2.progressTextBox.Text = "写坡长文件Saving slope length......";
                form1.form2.progressTextBox.Update();
                string cell_len = String.Concat(outPath, "\\", prefix, "cell_len.txt");
                WriteSlopeLenFile(cell_len, ref demData, ref noDataMap, ref slopeLen); //打开并写坡长文件，然后关闭
                LogWroteDEM(cell_len); //写日志文件            

                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                form1.form2.progressTextBox.Text = "写坡长指数文件Saving slope exponent......";
                form1.form2.progressTextBox.Update();
                string slp_exp = String.Concat(outPath, "\\", prefix, "slp_exp.txt");
                WriteSlopeExponentFile(slp_exp, demData, noDataMap, slopeAng,RUSLE_CSLE); //计算坡长指数m
                LogWroteDEM(slp_exp); //写日志文件

                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                form1.form2.progressTextBox.Text = "写坡度因子文件Saving S factor......";
                form1.form2.progressTextBox.Update();
                string slp_fac = String.Concat(outPath, "\\", prefix, "slp_fac.txt");
                WriteSlopeFactorFile(slp_fac, demData, noDataMap, slopeAng); //写坡度因子
                LogWroteDEM(slp_fac); //写日志文件

                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                form1.form2.progressTextBox.Text = "写坡向数据文件Saving outflow......";
                form1.form2.progressTextBox.Update();
                string out_flow = String.Concat(outPath, "\\", prefix, "outflow.txt");
                WriteDirectionArray(out_flow, ref demData, ref noDataMap, ref outFlow); //写坡向数组
                LogWroteDEM(out_flow); //写日志文件
            }
 
            if (MyCheckBox[1].Checked )
            {
                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                form1.form2.progressTextBox.Text = "写坡度文件Saving slope angle......";
                form1.form2.progressTextBox.Update();
                string slp_ang = String.Concat(outPath, "\\", prefix, "slp_ang.txt");
                WriteSlopeAngFile(slp_ang, demData, noDataMap, slopeAng); //打开并写坡度文件，然后关闭
                LogWroteDEM(slp_ang); //写日志文件
            }

            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "计算流入Calculating inflow......";
            form1.form2.progressTextBox.Update();
            SetInFlow(demData, ref noDataMap, ref outFlow, ref inFlow); //设置流入
       
            if(ExportAll)//if (demData.ifExcessFile)
            {
                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                form1.form2.progressTextBox.Text = "写坡向数据Saving inflow......";
                form1.form2.progressTextBox.Update();
                string in_flow = String.Concat(outPath, "\\", prefix, "inflow.txt");
                WriteDirectionArray(in_flow, ref demData, ref noDataMap, ref inFlow); //写坡向数组
                LogWroteDEM(in_flow); //写日志文件
            }

            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "建立坡度单元Calculating cell slope length......";
            form1.form2.progressTextBox.Update();
            if (flowcut)//是否设置了截断
            {
                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                form1.form2.progressTextBox.Text = "正在获取河网Calculating channel networks......";
                form1.form2.progressTextBox.Update();
                BuildSlopeCutGrid(demData, outFlow, slopeAng, noDataMap, flowFlag);
                if (Channel_Consider)
                {
                    Int32[] ChannelNetworks = new Int32[demData.imagNcols * demData.imagNrows];
                    CalcChannelNetworks(demData, ref ChannelNetworks, outFlow, noDataMap);
                    ReBuildSlopeCutGrid(demData, outFlow, slopeAng, noDataMap, flowFlag, ChannelNetworks, threshold);//建立截断单元
                    if (ExportAll)//if (demData.ifExcessFile)            
                    {
                        form1.form2.progressTextBox.Text = "写河网数据Saving Channel networks......";
                        form1.form2.progressTextBox.Update();
                        form1.form2.progressBar.Value++;
                        form1.form2.progressBar.Maximum++;
                        string Channel = String.Concat(outPath, "\\", prefix, "flowaccu.txt");
                        WriteDEMGrid(Channel, demData, noDataMap, ChannelNetworks); //写河网数据
                        LogWroteDEM(Channel); //写日志文件
                    }
                    ChannelNetworks = new int[1];
                    ChannelNetworks = null;
                }
            }
            else
            {
                WithoutSlopeCutGrid(demData, noDataMap, flowFlag);//没有截断          
            }
            if(ExportAll)//if (demData.ifExcessFile)            
            {
                form1.form2.progressBar.Value++;
                form1.form2.progressTextBox.Text = "建立坡度单元Calculating ......";
                form1.form2.progressTextBox.Update();
                form1.form2.progressBar.Maximum++;

                string slp_cut = String.Concat(outPath, "\\", prefix, "slp_cut.txt");
                WriteBoolGrid(slp_cut, demData, noDataMap, flowFlag); //写布尔单元
                LogWroteDEM(slp_cut); //写日志文件

            }
            demMap = new float[1];
                demMap=null;
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "申请内存空间Applying for new memory......";
            form1.form2.progressTextBox.Update();
            float[] cumlen = new float[demData.imagNcols * demData.imagNrows];
            if (cumlen == null)
            {
                LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "cumulative Length"); //申请内存空间失败写日志
            }
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "设置缓冲区Buffering......";
            form1.form2.progressTextBox.Update();
            //外围填充
            #region FillAround
            {
                int c = 0;
                for (int i = 0; i < demData.imagNrows; i++)
                {
                    for (int j = 0; j < demData.imagNcols; j++)
                    {
                        c = i * demData.imagNcols + j;
                        if (i < 2 || i > demData.imagNrows - 3)
                        {
                            cumlen[c] = (float)0.0;
                        }
                        else if (j < 2 || j > demData.imagNcols - 3)
                        {
                            cumlen[c] = (float)0.0;
                        }
                    }
                }
            }
            #endregion
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "初始化累积坡长文件Initial slope length......";
            form1.form2.progressTextBox.Update();
            InitCumulativeLength(demData, inFlow, outFlow, noDataMap,flowFlag ,slopeLen,ref cumlen); //初始化累积坡长文件
            slopeLen= new float[1]; 
                slopeLen= null;
            if(ExportAll)//if (demData.ifExcessFile)            
            {
                form1.form2.progressBar.Value++;
                form1.form2.progressTextBox.Text = "写坡长文件Saving initial slope length......";
                form1.form2.progressTextBox.Update();
                form1.form2.progressBar.Maximum++;

                string init_len = String.Concat(outPath, "\\", prefix, "init_len.txt");
                WriteSlopeLenFile(init_len, ref demData, ref noDataMap, ref cumlen); //写坡长文件
                LogWroteDEM(init_len); //写日志文件
            }
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "计算累积坡长Calculating slope length......";
            form1.form2.progressTextBox.Update();
            if (CumulatedWay)
                CalcCumulativeLength(demData, outFlow, noDataMap, flowFlag, cumlen); //计算最长累计坡长!!!!!!!
            else
                CalcCumulativeLength_Sum(demData, outFlow, noDataMap, flowFlag, cumlen); //计算最大累计坡长!!!!!!!
                  
            if (MyCheckBox[2].Checked)
            {
                form1.form2.progressBar.Value++;
                form1.form2.progressTextBox.Text = "计算累积坡长Calculating slope length......";
                form1.form2.progressTextBox.Update();
                form1.form2.progressBar.Maximum++;
                string slp_len = String.Concat(outPath, "\\", prefix, ".txt");//20111129春梅要求与DEM文件名一致
                WriteSlopeLenFile(slp_len, ref demData, ref noDataMap, ref cumlen); //写坡长文件
                LogWroteDEM(slp_len); //写日志文件
            }
            outFlow = new byte[1];
            outFlow = null;
            inFlow = new byte[1];
            inFlow = null;
     
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "申请内存空间Applying for new memory......";
            form1.form2.progressTextBox.Update();

            float[] slp_lgth_ft = new float[demData.imagNcols * demData.imagNrows];
            if (slp_lgth_ft == null)
            {
                LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "cumulative Length in Feet");
            }
            if (!demData.meterOrFeet)
            {
                form1.form2.progressTextBox.Text = "进行单位转换......";
                form1.form2.progressTextBox.Update();
                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;

                ConvertLengthToFeet(demData, cumlen, slp_lgth_ft); //当前值除以0.3048的变换meter->feet的变化
             
                if (demData.ifExcessFile)
                {
                    form1.form2.progressTextBox.Text = "写坡长(feet)......";
                    form1.form2.progressTextBox.Update();
                    form1.form2.progressBar.Value++;
                    form1.form2.progressBar.Maximum++;

                    string slp_in_ft = String.Concat(outPath, "\\", prefix, "slp_in_ft.txt");
                    WriteSlopeLenFeet(slp_in_ft, demData, noDataMap, slp_lgth_ft); //写坡长（单位feet）
                    LogWroteDEM(slp_in_ft); //写日志文件
                }
                cumlen = new float[1];
                cumlen = null;
            }
            form1.form2.progressTextBox.Text = "申请内存空间Applying for new memory......";
            form1.form2.progressTextBox.Update();
            form1.form2.progressBar.Value++;

            float[] ruslel = new float[demData.imagNcols * demData.imagNrows];
            if (ruslel == null)
            {
                if(RUSLE_CSLE==1)
                    LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "CSLE L"); //申请内存单元失败后写日志文件
                else
                    LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "RUSLE L"); //申请内存单元失败后写日志文件

            }
            form1.form2.progressTextBox.Text = "设置缓冲区Buffering......";
            form1.form2.progressTextBox.Update();
            form1.form2.progressBar.Value++;
            //外围赋值
            #region
            {
                int c = 0;
                for (int i = 0; i < demData.imagNrows; i++)
                    for (int j = 0; j < demData.imagNcols; j++)
                    {
                        c = i * demData.imagNcols + j;
                        if (i < 2 || i > demData.imagNrows - 3)
                            ruslel[c] = (float)0.0;
                        else if (j < 2 || j > demData.imagNcols - 3)
                            ruslel[c] = (float)0.0;
                    }
            }
            #endregion
            if (!demData.meterOrFeet)
            {
                form1.form2.progressTextBox.Text = "计算(feet)......";
                form1.form2.progressTextBox.Update();
                form1.form2.progressBar.Value++;
                
                Calculate_CLSE_L_Feet(demData, slopeAng, slp_lgth_ft, ruslel); //计算csle(feet)
                //计算rusle(feet)
            }
            else
            {
                form1.form2.progressTextBox.Text = "计算(meter)......";
                form1.form2.progressTextBox.Update();
                form1.form2.progressBar.Value++;             
                Calculate_L(demData, slopeAng, cumlen, ruslel,RUSLE_CSLE); //计算csle/rusle(meter)                
            }
            if (!demData.meterOrFeet)
            {
                slp_lgth_ft = new float[1];
                slp_lgth_ft = null;
            }
            else
            {
                cumlen = new float[1];
                cumlen = null;
            }

            //if (demData.ifExcessFile)
            if (MyCheckBox[4].Checked)
            {
                form1.form2.progressTextBox.Text = "写坡长因子文件Saving L factor......";
                form1.form2.progressTextBox.Update();
                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                string rusle_l;
                if(RUSLE_CSLE==1)//计算csle
                    rusle_l = String.Concat(outPath, "\\", prefix, "CSLE_L.txt");
                else
                    rusle_l = String.Concat(outPath, "\\", prefix, "RUSLE_L.txt");
                Write_FloatGrid(rusle_l, demData, noDataMap, ruslel); //将rusle1储存为浮点型
                LogWroteDEM(rusle_l); //写日志文件
            }
            //Console.WriteLine("8");
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "申请内存空间Applying for new memory......";
            form1.form2.progressTextBox.Update();

            float[] rusles = new float[demData.imagNcols * demData.imagNrows];
            if (rusles == null)
            {
                if(RUSLE_CSLE==1)
                    LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "CLSE S"); //申请内存空间失败写日志
                else
                    LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "RUSLE S");
            }
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "设置缓冲区Buffering......";
            form1.form2.progressTextBox.Update();
            //外围填充
            #region
            {
                int c = 0;
                for (int i = 0; i < demData.imagNrows; i++)
                    for (int j = 0; j < demData.imagNcols; j++)
                    {
                        c = i * demData.imagNcols + j;
                        if (i < 2 || i > demData.imagNrows - 3)
                            rusles[c] = (float)0.0;
                        else if (j < 2 || j > demData.imagNcols - 3)
                            rusles[c] = (float)0.0;
                    }
            }
            #endregion
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "计算坡度因子Calculating S factor......";
            form1.form2.progressTextBox.Update();
            Calculate_S(demData, slopeAng, rusles,RUSLE_CSLE); //
            slopeAng = new float[1];
            slopeAng = null;
      
            if (MyCheckBox[3].Checked)
            {
                form1.form2.progressTextBox.Text = "写坡度因子文件Saving S factor......";
                form1.form2.progressTextBox.Update();
                form1.form2.progressBar.Value++;
                form1.form2.progressBar.Maximum++;
                string rusle_s;
                if(RUSLE_CSLE==1)
                    rusle_s = String.Concat(outPath, "\\", prefix, "CSLE_S.txt");
                else
                    rusle_s = String.Concat(outPath, "\\", prefix, "RUSLE_S.txt");
                Write_FloatGrid(rusle_s, demData, noDataMap, rusles); //将rusle1储存为浮点型
                LogWroteDEM(rusle_s); //写日志文件
            }
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "申请内存空间Applying for new memory......";
            form1.form2.progressTextBox.Update();

            float[] ruslels = new float[demData.imagNcols * demData.imagNrows];

            if (ruslels == null)
            {
                if(RUSLE_CSLE==1)
                    LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "CSLE LS"); //申请内存空间失败写日志
                else
                    LogFailedExit(demData.imagNcols * demData.imagNrows, 4, "float", "RUSLE LS");
            }
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "设置缓冲区Buffering......";
            form1.form2.progressTextBox.Update();

            //外围填充
            #region
            {
                int c = 0;
                for (int i = 0; i < demData.imagNrows; i++)
                    for (int j = 0; j < demData.imagNcols; j++)
                    {
                        c = i * demData.imagNcols + j;
                        if (i < 2 || i > demData.imagNrows - 3)
                            ruslels[c] = (float)0.0;
                        else if (j < 2 || j > demData.imagNcols - 3)
                            ruslels[c] = (float)0.0;
                    }
            }
            #endregion
            form1.form2.progressTextBox.Text = "计算坡度坡长Calculating LS factor......";
            form1.form2.progressTextBox.Update();
            form1.form2.progressBar.Value++;

            Calculate_LS2(demData, ruslel, rusles, ruslels); //将rusle2储存
            ruslel = new float[1];            
            ruslel = null;
            if (MyCheckBox[5].Checked)
            {
                form1.form2.progressTextBox.Text = "写坡度坡长文件Save LS factor......";
                form1.form2.progressTextBox.Update();
                form1.form2.progressBar.Value++;
                string ruslels2;
                if(RUSLE_CSLE==1)
                    ruslels2 = String.Concat(outPath, "\\", prefix, "CSLE_LS.txt");
                else
                    ruslels2 = String.Concat(outPath, "\\", prefix, "RUSLE_LS.txt");

                Write_LS2(ruslels2, demData, noDataMap, ruslels);
                LogWroteDEM(ruslels2);
            }
            ruslels = new float[1];
            rusles = null;
            form1.form2.progressBar.Value++;
            form1.form2.progressTextBox.Text = "计算结束Calculation Succeed";
            form1.form2.progressTextBox.Update();
            Debug.WriteLine("form1.form2.progressBar.Value = "+form1.form2.progressBar.Value);
            Log_CLSE_LSWarning(RUSLE_CSLE);
            CloseLogFile();
            if (ExportAll==false)
            {
                DeleteLogFile(logFilePath);
            }
        }

        //##############################################

        //                   InData

        //##############################################
        #region InData
        private StreamReader demFile;
        private int BarMaxNum;//用于存放进度条最大值  BarMaxNum = demData.nrows;

        public void LoadDEMHeaderData(string inFileName, ref DemData demData)
        {
            //检验文件是否存在
            if (!File.Exists(inFileName))
            {
                throw (new FileNotFoundException(inFileName + " does not exist!"));
            }
            string line;
            demFile = new StreamReader(inFileName);

            //赋值ncols
            line = demFile.ReadLine().Trim();
            Debug.WriteLine(line);
            int tmpLastIndex = line.LastIndexOf(' ');
            demData.ncols = int.Parse(line.Substring(tmpLastIndex + 1));

            //赋值nrows
            line =  demFile.ReadLine().Trim();
            tmpLastIndex =line. LastIndexOf(' ');//由行值为进度条赋值
            demData.nrows = int.Parse(line.Substring(tmpLastIndex + 1));
            BarMaxNum = demData.nrows;
            Debug.WriteLine(line);
            //赋值xllcorner
            line = demFile.ReadLine().Trim();
            tmpLastIndex = line.LastIndexOf(' ');
            demData.xllcorner = double.Parse(line.Substring(tmpLastIndex + 1));
            Debug.WriteLine(line);
            //赋值yllcorner
            line = demFile.ReadLine().Trim();
            tmpLastIndex = line.LastIndexOf(' ');
            demData.yllcorner = double.Parse(line.Substring(tmpLastIndex + 1));
            Debug.WriteLine(line);
            //赋值cellsize
            line = demFile.ReadLine().Trim();
            tmpLastIndex = line.LastIndexOf(' ');
            char[] numPart = line.Substring(tmpLastIndex + 1).ToCharArray();//将栅格的长度转换成字符串，目的是为了判断是否为浮点型
            Debug.WriteLine(line);
            demData.cellType = false;
            bool tmpFlag = false;
            //通过'.'来判断是否为浮点型
            for (int i = 0; i < numPart.Length; i++)
            {
                if (numPart[i] == '.')
                {
                    demData.cellType = true;
                }
            }
            demData.cellSize = float.Parse(line.Substring(tmpLastIndex + 1));

            //赋值NOTDATA
            line = demFile.ReadLine().Trim();
            tmpLastIndex = line.LastIndexOf(' ');
            numPart = line.Substring(tmpLastIndex + 1).ToCharArray();//相同的方法来处理
            for (int i = 0; i < numPart.Length; i++)
            {
                if (numPart[i] == '.')
                {
                    tmpFlag = true;
                }
            }
            demData.noDateType = tmpFlag;
            demData.floatNoData = float.Parse(line.Substring(tmpLastIndex + 1));
            Debug.WriteLine(demData.floatNoData);

            demData.intNoData = (int)demData.floatNoData;//09.12.01修改       
            demData.imagNcols = demData.ncols + 4;
            demData.imagNrows = demData.nrows + 4;

        }

        public void ReadDEMElevations(ref DemData demData, ref float[] demMap, ref bool[] noDataMap)
        {
            string[] tmp = new string[demData.ncols];
            float[] tmp1 = new float[demData.nrows * demData.ncols];
            /****************************/
            string aString;
            int count = 0;
            while ((aString = demFile.ReadLine()) != null)
            {
                string[] sArray = aString.Split(' ');
                int i = 0;
                while (i < sArray.Length)
                {
                    //Debug.WriteLine("the string:" + sArray [i]);
                    //tmp1[count] = float.Parse(sArray[i]);
                    if (sArray[i] != "")
                    {
                        tmp1[count] = Convert.ToSingle(sArray[i]);
                        count++;
                        i++;
                    }
                    else
                    {
                        i++;
                    }

                }
                sArray = null;
                if (count == demData.nrows * demData.ncols)
                    break;     

            }
            /****************************/
            int real = 0;
            int imag = 0;
            for (int i = 2; i < demData.imagNrows - 2; i++)
                for (int j = 2; j < demData.imagNcols - 2; j++)
                {
                    imag = i * demData.imagNcols + j;
                    real = (i - 2) * demData.ncols + (j - 2);

                    if (tmp1[real] != demData.floatNoData)
                        demMap[imag] = tmp1[real];
                    else
                        demMap[imag] = (float)32767.0;
                    noDataMap[imag] = (tmp1[real] == demData.floatNoData);
                    if (demMap[imag] < 0.0)
                    {
                        LogNegativeDEM(i - 2, j - 2, tmp1[real]);
                    }
                }
            tmp1 = null;
            tmp = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            demFile.Close();
        }

        public bool VerifyDEMDataType(ref DemData demData)
        {
            StreamReader demFileBack = new StreamReader(demData.inPath);
            //读取六行，空过文件的起始标题
            demFileBack.ReadLine();
            demFileBack.ReadLine();
            demFileBack.ReadLine();
            demFileBack.ReadLine();
            demFileBack.ReadLine();
            string aString;
            bool fpFlag = false;
            int tPrecision = 0;
            demData.dataPrecision = 0;

            while ((aString = demFileBack.ReadLine()) != null)
            {
                string[] sArray = aString.Split(' ');
                int j = 0;
                while (j < sArray.Length)
                {
                    char[] array = sArray[j].ToCharArray();
                    for (int i = 0; i < array.Length; i++)
                    {

                        tPrecision = 0;
                        if (array[i] == '.')
                        {
                            fpFlag = true;             
                            tPrecision = array.Length - i - 1;
                            if (tPrecision > demData.dataPrecision)
                                demData.dataPrecision = tPrecision;
                            break;
                        }
                    }
                    j++;

                }
                sArray = null;
            }
            demFileBack.Close();
            return fpFlag;
        }
        #endregion

        //##############################################

        //                OutData

        //##############################################

        #region OutData
        private StreamWriter sw;
        private StreamWriter fp;
        public void OpenLogFile(string filename)
        {
            sw = new StreamWriter(filename);
        }
        public void DeleteLogFile(string filename)
        {              
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                } 
        }
        public void LogStartUp(DemData dd)
        {
            sw.WriteLine();
            sw.WriteLine("User Selected options:");
            sw.WriteLine("  DEM DATA file name:        {0}", dd.inPath);
            sw.WriteLine("  Output path prefix:        {0}", dd.preOutPath);
            if (dd.meterOrFeet)
                sw.WriteLine("  DEM elevation units:       Meters");
            else
                sw.WriteLine("  DEM elevation units:       Feet");
            if (dd.ifExcessFile)//是否生成过度文件
                sw.WriteLine("  Write intermediate files:  YES");
            else
                sw.WriteLine("  Write intermediate files:  NO");

            if (dd.flowcut)
            {
                sw.WriteLine(" Slope cutoff factors");
                sw.WriteLine("   Less than 5 percent:     {0}", dd.scf_lt5);
                sw.WriteLine("   Greater equal 5 percent: {0}", dd.scf_ge5);
            }
            else
            {
                sw.WriteLine("Without considering slope cutoff");
            }

            if (dd.threshold > 0)
                sw.WriteLine("Define Channels as pixels an accumulated area threshold: {0} Square meters", dd.threshold);
            else
                sw.WriteLine("Without defined Channels");
            if (dd.ifRepair)
                sw.WriteLine("  Interior NODATA cells:     Fill if possible");
            else
                sw.WriteLine("  Interior NODATA cells:     Do not attempt to fill");

            if (dd.ifRepair)
                sw.WriteLine("  Interior NODATA clusters:  Process anyway!");
            else
                sw.WriteLine("  Interior NODATA clusters:  Terminate program");
            if (dd.fillWay)
                sw.WriteLine("  Interior NODATA point:  fill with average value of around 8-cells");
            else
                sw.WriteLine("  Interior NODATA point:  fill with min value of around 8-cells");

        }
        public void LogDemHeader(DemData dd)
        {
            sw.WriteLine("Processing data for dem data set containing:");
            sw.WriteLine("  ncols          {0:d}", dd.ncols);
            sw.WriteLine("  nrows          {0:d}", dd.nrows);
            sw.WriteLine("  xllcorner      {0:f6}", dd.xllcorner);
            sw.WriteLine("  xllcorner      {0:f6}", dd.yllcorner);
            if (dd.cellType)
            {
                sw.WriteLine("  cellsize       {0:f4}", dd.cellSize);
            }
            else
            {
                sw.WriteLine("  cellsize       {0:d}", (int)dd.cellSize);
            }

            if (dd.noDateType)
            {
                sw.WriteLine("  DEM NODATA     {0:f1}", dd.floatNoData);
            }
            else
            {
                sw.WriteLine("  DEM NODATA     {0:d}", dd.intNoData);
            }
            sw.WriteLine("  Float NODATA   -9.9");
            sw.WriteLine("  Int NODATA     -9");
        }
        public void LogFailedExit(int elements, int size, string type, string str)
        {
            sw.WriteLine("Unable to allocate memory for a dynamic {0} array.", type);
            sw.WriteLine("Such an array would have [{0}] elements", elements);
            sw.WriteLine("That's over {0} megabytes of storage", elements * size / 1000000);
            sw.WriteLine("This array would have stored the {0} values", str);
            sw.WriteLine("A guess is that your machine need about a {0} megaByte pagefile", elements * 18 / 1000000);
            sw.WriteLine("to handle this dataset.  NOTE: This is an apporximate size.");
            sw.WriteLine("Program execution has been halted!");
            CloseLogFile();
            Environment.Exit(0);
        }
        public void LogNegativeDEM(int nrows, int ncols, float demValue)
        {
            sw.WriteLine("Warning, DEM file contains negative values - Check Logfile if not expected!");
            sw.WriteLine("Warning, DEM file contains negative value(s)!");
            sw.WriteLine("Unless your working with data containing elevations below sea level");
            sw.WriteLine("This indicates an anomaly in your DEM data which may require attention");
            sw.WriteLine("Isolated negative values surrounded by valid DEM values will be sink filled");
            sw.WriteLine("Isolated negative values surrounded by NODATA cells will be rewritten as NODATA");
            sw.WriteLine("The negative cell(s) and their values are shown below");
            sw.WriteLine("DEM Cell[{0}][{1}] contains a negative value of {2}", nrows, ncols, demValue);
        }
        public void LogDataTypeDEM(DemData dd)
        {
            if (dd.floatOrInt)
            {
                sw.WriteLine("DEM file contains floating point DEM data");
                sw.WriteLine("DEM file contains {0} places decimal precision", dd.dataPrecision);
            }
            else
                sw.WriteLine("DEM file contains integer DEM data");
        }
        public void LogFillPoint(int i, int j, float fillValue)
        {
            sw.WriteLine("Cell [{0}][{1}] is an internal surrounded by cells which have valid DEM data", i, j);
            sw.WriteLine("It has filled by value {0}", fillValue);
        }
        public void LogExit()
        {
            sw.WriteLine("Dem data has cluster ,your chose option \"AnyWayProcess\"is false\nProgram execution has been halted!");
            CloseLogFile();
            Environment.Exit(0);
        }
        public void LogInteriorCluster(int r, int c)
        {
            sw.WriteLine("Cell[{0}][{1}] appears to be clustered!", r, c);
        }
        public void LocateInteriorNODATACells(DemData demData, ref bool[] noDataMap, ref float[] demMap)
        {
            int i; // Loop Counter.
            int j; // Loop Counter.


            int c, n, w, e, s; // Center, North, West, East, South
            int nw, ne, se, sw; // NorthWest. NorthEast, SouthEast, SouthWest

            bool done = false; // Iterative flag   
            int rows = demData.imagNrows;
            int cols = demData.imagNcols;

            bool[] ex_nd = new bool[rows * cols]; // EXternal_Nodata boolean map array
            if (ex_nd == null)
            {
                LogFailedExit(rows * cols, 1, "bool", "Temp File to locate interior NODATA points");
            }
            //把最外两层直接赋值true，因为最外两层是没有意义的，其他的值赋为false
            for (i = 0; i < rows; i++)
            {
                for (j = 0; j < cols; j++)
                {
                    if (i < 2 || i > rows - 3)
                    {
                        c = i * cols + j;
                        ex_nd[c] = true;
                    }
                    else if (j < 2 || j > cols - 3)
                    {
                        c = i * cols + j;
                        ex_nd[c] = true;

                    }
                    else
                    {
                        c = i * cols + j;
                        ex_nd[c] = false;
                    }
                }
            }

            int count = 0; // Counts number of interior NODATA cells
            while (!done)
            {
                done = true;
                for (i = 2; i < rows - 2; i++)
                    for (j = 2; j < cols - 2; j++)
                    {
                        c = i * cols + j;
                        //有数据
                        if (!noDataMap[c] || ex_nd[c])
                        {
                            continue;
                        }
                        nw = c - cols - 1;
                        n = c - cols;
                        ne = c - cols + 1;
                        w = c - 1;
                        e = c + 1;
                        sw = c + cols - 1;
                        s = c + cols;
                        se = c + cols + 1;

                        if (ex_nd[nw] || ex_nd[n] || ex_nd[ne] || ex_nd[w] ||
                            ex_nd[sw] || ex_nd[e] || ex_nd[se] || ex_nd[s])
                        {
                            ex_nd[c] = true;
                            done = false;
                        }
                    }
                for (i = rows - 3; i > 1; i--)
                    for (j = cols - 2; j > 1; j--)
                    {
                        c = i * cols + j;
                        if (!noDataMap[c] || ex_nd[c])
                        {
                            continue;
                        }
                        nw = c - cols - 1;
                        n = c - cols;
                        ne = c - cols + 1;
                        w = c - 1;
                        e = c + 1;
                        sw = c + cols - 1;
                        s = c + cols;
                        se = c + cols + 1;

                        if (ex_nd[nw] || ex_nd[n] ||
                            ex_nd[ne] || ex_nd[w] ||
                            ex_nd[sw] || ex_nd[e] ||
                            ex_nd[se] || ex_nd[s])
                        {
                            ex_nd[c] = true;
                            done = false;
                        }
                    }
            }
            for (i = 2; i < rows - 2; i++)
                for (j = 2; j < cols - 2; j++)
                    if (ex_nd[i * cols + j] != noDataMap[i * cols + j])
                    {
                        c = i * cols + j;
                        nw = c - cols - 1;
                        n = c - cols;
                        ne = c - cols + 1;
                        w = c - 1;
                        e = c + 1;
                        sw = c + cols - 1;
                        s = c + cols;
                        se = c + cols + 1;
                        if (ex_nd[nw] || ex_nd[n] ||
                            ex_nd[ne] || ex_nd[w] ||
                           ex_nd[sw] || ex_nd[e] ||
                           ex_nd[se] || ex_nd[s])
                        {
                            LogInteriorCluster(i - 2, j - 2);
                        }
                        if (demData.ifRepair)
                        {
                            if (demData.fillWay)
                            {
                                int count1 = 0;
                                float temp = 0.0f;
                                if (!noDataMap[n])
                                {
                                    temp = demMap[n];
                                    ++count1;
                                }
                                if (!noDataMap[ne])
                                {
                                    temp += demMap[ne];
                                    ++count1;
                                }
                                if (!noDataMap[e])
                                {
                                    temp += demMap[e];
                                    ++count1;
                                }
                                if (!noDataMap[se])
                                {
                                    temp += demMap[se];
                                    ++count1;
                                }
                                if (!noDataMap[s])
                                {
                                    temp += demMap[s];
                                    ++count1;
                                }
                                if (!noDataMap[sw])
                                {
                                    temp += demMap[sw];
                                    ++count1;
                                }
                                if (!noDataMap[w])
                                {
                                    temp += demMap[w];
                                    ++count1;
                                }
                                if (!noDataMap[nw])
                                {
                                    temp += demMap[nw];
                                    ++count1;
                                }
                                if (count1 != 0)
                                {
                                    demMap[c] = temp / count1;
                                    noDataMap[c] = false;
                                    LogFillPoint(i - 2, j - 2, demMap[c]);
                                }
                            }else
                            {
                            float temp = 9999.0f;//存一个较大的值
                            if (!noDataMap[n])
                                temp = demMap[n];
                            if (!noDataMap[ne])
                                temp = (temp < demMap[ne] ? temp : demMap[ne]);
                            if (!noDataMap[e])
                                temp = (temp < demMap[e] ? temp : demMap[e]);
                            if (!noDataMap[se])
                                temp = (temp < demMap[se] ? temp : demMap[se]);
                            if (!noDataMap[s])
                                temp = (temp < demMap[s] ? temp : demMap[s]);
                            if (!noDataMap[sw])
                                temp = (temp < demMap[sw] ? temp : demMap[se]);
                            if (!noDataMap[w])
                                temp = (temp < demMap[w] ? temp : demMap[w]);
                            if (!noDataMap[nw])
                                temp = (temp < demMap[nw] ? temp : demMap[nw]);
                            demMap[c] = temp;
                            noDataMap[c] = false;
                            LogFillPoint(i - 2, j - 2, temp);}
                        }
                        else
                        {
                            LogExit();
                        }
                        count++;
                    }

            if (count == 0)
            {
                LogVerifiedDEM();
            }
            else
            {
                LogVerifiedDEM1(count);
            }
            ex_nd = null;
        }
        public void LogVerifiedDEM()
        {
            sw.Write("There are no interior NODATA cells ");
            sw.WriteLine("surrounded by valid DEM cells");
        }
        public void LogVerifiedDEM1(int count)
        {
            sw.WriteLine("There are {0} cell have filled !", count);
        }
        public void LogFileError(string fileNamepPath, string lff_action)
        {
            sw.WriteLine("An error occurred while opening file: {0}", fileNamepPath);

            if (lff_action[0] == 'r')
                sw.WriteLine("Unable to read from this file");
            if (lff_action[0] == 'r')
                sw.WriteLine("Program execution halted");
            if (lff_action[0] == 'w')
                sw.WriteLine("Unable to write to this file");
            if (lff_action[0] == 'w')
                sw.WriteLine("Program execution halted");
            Environment.Exit(0);
        }
        public void WriteDEMGrid(string fileNamePath, DemData dd, bool[] noDataMap, float[] demMap)
        {
            int i, j;
            int cols = dd.imagNcols;
            int rows = dd.imagNrows;

            fp = new StreamWriter(fileNamePath);

            if (fp == null) // Handle error opening file
                LogFileError(fileNamePath, "w");

            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);

            fp.WriteLine("NODATA_value  {0}", dd.intNoData);
            int c = 0;
            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    if (noDataMap[c])
                    {
                        fp.Write(" {0}", dd.intNoData);
                    }
                    else
                    {
                        if (dd.floatOrInt)
                        {
                            if (dd.dataPrecision == 1)
                                fp.Write(" {0:f1}", demMap[c]);
                            else if (dd.dataPrecision == 2)
                                fp.Write(" {0:f2}", demMap[c]);
                            else
                                fp.Write(" {0:f3}", demMap[c]);
                        }
                        else
                        {
                            fp.Write(" {0}", (int)demMap[c]);
                        }
                    }
                }
                fp.WriteLine();
            }
            fp.Flush();
            fp.Close();
        }
        public void WriteDEMGrid(string fileNamePath, DemData dd, bool[] noDataMap, int[] demMap)
        {
            int i, j;
            int cols = dd.imagNcols;
            int rows = dd.imagNrows;

            fp = new StreamWriter(fileNamePath);

            if (fp == null) // 
                LogFileError(fileNamePath, "w");

            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);

            fp.WriteLine("NODATA_value  {0}", dd.intNoData);
            int c = 0;
            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    if (noDataMap[c])
                    {
                        fp.Write(" {0}", dd.intNoData);
                    }
                    else
                    {
                        if (dd.floatOrInt)
                        {
                            if (dd.dataPrecision == 1)
                                fp.Write(" {0:f1}", demMap[c]);
                            else if (dd.dataPrecision == 2)
                                fp.Write(" {0:f2}", demMap[c]);
                            else
                                fp.Write(" {0:f3}", demMap[c]);
                        }
                        else
                        {
                            fp.Write(" {0}", (int)demMap[c]);
                        }
                    }
                }
                fp.WriteLine();
            }
            fp.Flush();
            fp.Close();
        }
        
        bool Toggle = true;
        public void LogFill(int i, int j, int precision, float old, float min)
        {
            bool flag;
            bool start = true;
            if (start)
            {
                flag = true;
                start = false;
            }
            else
                flag = Toggle;
            if (flag)
            {
                if (precision == 0)
                    sw.Write("Cell[{0}][{1}] with a value of {2}", i, j, (int)old);
                else
                    if (precision == 1)
                        sw.Write("Cell[{0}][{1}] with a value of {2:f1}", i, j, old);
                    else if (precision == 2)
                        sw.Write("Cell[{0}][{1}] with a value of {2:f2}", i, j, old);
                    else
                        sw.Write("Cell[{0}][{1}] with a value of {2:f3}", i, j, old);


                if (precision == 0)
                    sw.WriteLine(" was sink filled to {0}", min);
                else if (precision == 1)
                    sw.WriteLine(" was sink filled to {0:f1}", min);
                else if (precision == 2)
                    sw.WriteLine(" was sink filled to {0:f2}", min);
                else
                    sw.WriteLine(" was sink filled to {0:f3}", min);

                Toggle = false;
            }
            else
            {
                if (precision == 0)
                    sw.WriteLine("[{0}][{1}], {2}, {3}", i, j, (int)old, min)
                ;
                else
                    if (precision == 1)
                        sw.WriteLine("[{0}][{1}], {2:f2}, {3:f1}", i, j, old, min);
                    else if (precision == 2)
                        sw.WriteLine("[{0}][{1}], {2:f2}, {3:f2}", i, j, old, min);
                    else
                        sw.WriteLine("[{0}][{1}], {2:f3}, {3:f3}", i, j, old, min);

            }
        }
        public void LogWroteDEM(string fileName)
        {
            sw.WriteLine("Data successfully written to: {0}", fileName);
        }
        public void LogAnnulusFill(int i, int j, int precision, float old, float min)
        {
            bool flag;
            bool start = true;

            if (start)
                flag = start = false;
            else
                flag = Toggle;

            if (!flag)
            {
                if (precision == 0)
                    sw.Write("Cell[{0}][{1}] with a value of {2}", i, j, (int)old);
                else if (precision == 1)
                    sw.Write("Cell[{0}][{1}] with a value of {2:f1}", i, j, old);
                else if (precision == 2)
                    sw.Write("Cell[{0}][{1}] with a value of {2:f2}", i, j, old);
                else
                    sw.Write("Cell[{0}][{1}] with a value of {2:f3}", i, j, old);

                if (precision == 0)
                    sw.WriteLine(" It was annulus filled to {0}", (int)min);
                else if (precision == 1)
                    sw.WriteLine(" It was annulus filled to {0:f1}", min);
                else if (precision == 2)
                    sw.WriteLine(" It was annulus filled to {0:f2}", min);
                else
                    sw.WriteLine(" It was annulus filled to {0:f3}", min);

                Toggle = true;
            }
            else
            {
                if (precision == 0)
                    sw.WriteLine("[{0}][{1}], {2}, {3}", i, j, (int)old, min);
                else if (precision == 1)
                    sw.WriteLine("[{0}][{1}], {2:f1}, {3:f1}", i, j, old, min);
                else if (precision == 2)
                    sw.WriteLine("[{0}][{1}], {2:f2}, {3:f2}", i, j, old, min);
                else
                    sw.WriteLine("[{0}][{1}], {2:f3}, {3:f3}", i, j, old, min);

            }
        }
        public void Log_CLSE_LSWarning(int rusle_csle)
        {
            if(rusle_csle==1)
            sw.WriteLine("The CLSE LS data has been saved !");
            else
            sw.WriteLine("The RUSLE LS data has been saved !");
        }
        
        public void WriteSlopeLenFile(string fileName, ref DemData dd, ref bool[] noDataMap, ref float[] slopeLen)
        {
            int rows, cols;
            int i, j;
            rows = dd.imagNrows;
            cols = dd.imagNcols;

            fp = new StreamWriter(fileName);

            if (fp == null)
                LogFileError(fileName, "w");

            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);

            fp.WriteLine("NODATA_value  -9.9");

            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    if (noDataMap[i * cols + j])
                        fp.Write(" -9.9");
                    else
                        fp.Write(" {0:f3}", slopeLen[i * cols + j]);
                }
                fp.WriteLine();
            }
            fp.Flush();
            fp.Close();
        }
        public void WriteSlopeAngFile(string fileName, DemData dd, bool[] noDataMap, float[] slopeAng)
        {
            int rows, cols;
            int i, j;
            rows = dd.imagNrows;
            cols = dd.imagNcols;

            fp = new StreamWriter(fileName);
            if (fp == null) // Handle error opening file
                LogFileError(fileName, "w");

            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);

            fp.WriteLine("NODATA_value  -9.9");
            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                    if (noDataMap[i * cols + j])
                        fp.Write(" -9.9");
                    else
                        fp.Write(" {0:f4}", slopeAng[i * cols + j]);
                fp.WriteLine();
            }

            fp.Flush();
            fp.Close();
        }
        public void WriteSlopeExponentFile(string fileName, DemData dd, bool[] noDataMap, float[] slopeAng,int rusle_csle)
        {
            int rows, cols;
            int i, j;

            rows = dd.imagNrows;
            cols = dd.imagNcols;

            fp = new StreamWriter(fileName);
            if (fp == null) // Handle error opening file
                LogFileError(fileName, "w");


            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);

            //fp.WriteLine("NODATA_value  {0}", dd.intNoData);

            fp.WriteLine("NODATA_value  -9.9");

            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    if (noDataMap[i * cols + j])
                        fp.Write("-9.9");
                    else
                        if(rusle_csle==1)
                            fp.Write(" {0:f2}", TableLookUp_csle(slopeAng[i * cols + j]));
                        else
                            fp.Write(" {0:f2}", TableLookUp_rusle(slopeAng[i * cols + j]));
                }
                fp.WriteLine();
            }
            fp.Flush();
            fp.Close();
        }
        public void WriteSlopeFactorFile(string fileName, DemData dd, bool[] noDataMap, float[] slopeAng)
        {
            int rows, cols;
            int i, j;
            rows = dd.imagNrows;
            cols = dd.imagNcols;

            fp = new StreamWriter(fileName);
            if (fp == null) 
                LogFileError(fileName, "w");

            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);
            fp.WriteLine("NODATA_value  -9.9");

            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    if (noDataMap[i * cols + j])
                        fp.Write(" -9.9");
                    else
                    {
                        if (slopeAng[i * cols + j] < 2.8624)
                            fp.Write(" {0:f1}", dd.scf_lt5);
                        else
                            fp.Write(" {0:f1}", dd.scf_ge5);
                    }

                }
                fp.WriteLine();
            }
            fp.Flush();
            fp.Close();
        }
        public void WriteDirectionArray(string fileName, ref DemData dd, ref bool[] noDatamMap, ref byte[] outFlow)
        {
            int rows, cols;
            int i, j;
            rows = dd.imagNrows;
            cols = dd.imagNcols;
            fp = new StreamWriter(fileName);
            if (fp == null) 
                LogFileError(fileName, "w");
            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);
           
            fp.WriteLine("NODATA_value  -9");
            int count = 0;
            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    if (noDatamMap[i * cols + j])
                        fp.Write(" -9");
                    else
                    {
                        if (outFlow[i * cols + j] == 0)
                        {
                            count++;
                        }
                        fp.Write(" {0}", outFlow[i * cols + j]);
                    }
                }
                fp.WriteLine();
            }
            Debug.WriteLine("count = " + count);
            fp.Flush();
            fp.Close();
        }
        public void Write_Direction_Array(string fileName, ref DemData dd, ref bool[] noDataMap, ref byte[] inMap)
        {
            int rows, cols;
            int i, j;
            rows = dd.imagNrows;
            cols = dd.imagNcols;
            fp = new StreamWriter(fileName);
            if (fp == null) 
                LogFileError(fileName, "w");
            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);
          
            fp.WriteLine("NODATA_value  -9");

            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    if (noDataMap[i * cols + j])

                        fp.Write(" -9");
                    else
                        fp.Write(" {0}", (int)inMap[i * cols + j]);
                }
                fp.WriteLine();
            }
            fp.Flush();
            fp.Close();
        }
        public void WriteBoolGrid(string fileName, DemData dd, bool[] noDataMap, bool[] grid)
        {
            int rows, cols;
            int i, j;
            rows = dd.imagNrows;
            cols = dd.imagNcols;

            fp = new StreamWriter(fileName);
            if (fp == null)
                LogFileError(fileName, "w");

            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);
            fp.WriteLine("NODATA_value  -9");

            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    if (grid[i * cols + j])
                        fp.Write(" 1");
                    else
                        fp.Write(" 0");
                }
                fp.WriteLine();
            }
            fp.Flush();
            fp.Close();
        }
        public void LogCumulativeProgress(int lcp_count, int lcp_p1, int lcp_p2)
        {
            if (lcp_count == 1)
                sw.WriteLine("Beginning cumulative length calculations");
            sw.WriteLine("Pass # {0,-3:d} Part 1 hits = {1,-9:d}	Part # 2 {2,-9:d}", lcp_count, lcp_p1, lcp_p2);
            if (lcp_p1 == 0 && lcp_p2 == 0)
                sw.WriteLine("Cumulative length calculations completed");
        }
        //关闭日志文件
        public void CloseLogFile()
        {
            sw.Flush();
            sw.Close();
        }
        public void WriteSlopeLenFeet(string fileName, DemData dd, bool[] noDataMap, float[] grid)
        {
            int rows, cols;
            int i, j;

            rows = dd.imagNrows;
            cols = dd.imagNcols;

            fp = new StreamWriter(fileName);
            if (fp == null)
                LogFileError(fileName, "w");

            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);

            fp.WriteLine("NODATA_value  -9.9");
            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    if (noDataMap[i * cols + j])
                        fp.Write(" -9.9");
                    else
                        fp.Write(" {0:f3}", grid[i * cols + j]);
                }
                fp.WriteLine();

            }
            fp.Flush();
            fp.Close();
        }
        public void Write_FloatGrid(string fileName, DemData dd, bool[] noDataMap, float[] grid)
        {
            int rows, cols;
            int i, j;
            rows = dd.imagNrows;
            cols = dd.imagNcols;
            fp = new StreamWriter(fileName);
            if (fp == null) // Handle error opening file
                LogFileError(fileName, "w");


            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);

            fp.WriteLine("NODATA_value  -9.9");

            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                    if (noDataMap[i * cols + j])
                        fp.Write(" -9.9");
                    else
                        fp.Write(" {0:f4}", grid[i * cols + j]);
                fp.WriteLine();
            }
            fp.Flush();
            fp.Close();
        }
        
        public void Write_LS2(string fileName, DemData dd, bool[] noDataMap, float[] grid)
        {
            int rows, cols;
            int i, j;

            rows = dd.imagNrows;
            cols = dd.imagNcols;
            fp = new StreamWriter(fileName);
            if (fp == null) // Handle error opening file
                LogFileError(fileName, "w");


            fp.WriteLine("ncols         {0}", dd.ncols);
            fp.WriteLine("nrows         {0}", dd.nrows);
            fp.WriteLine("xllcorner     {0:f6}", dd.xllcorner);
            fp.WriteLine("yllcorner     {0:f6}", dd.yllcorner);
            if (dd.cellType)
                fp.WriteLine("cellsize      {0:f4}", dd.cellSize);
            else
                fp.WriteLine("cellsize      {0}", (int)dd.cellSize);

            fp.WriteLine("NODATA_value -9");
            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                    if (noDataMap[i * cols + j])
                        fp.Write(" -9");
                    else if ((int)(grid[i * cols + j] + 0.5) != 0)
                        fp.Write(" {0:f2}", (grid[i * cols + j] + 0.5));
                    else
                        fp.Write(" 0.01");//原始值为1,20110610改为0.01
                fp.WriteLine();
            }
            fp.Flush();
            fp.Close();
        }

        #endregion
        //##############################################



        //                    DemRepair



        //##############################################

        #region DemRepair
        public bool FillSinks(DemData demData, ref bool[] noDataMap, ref float[] demMap)
        {
            bool flag = false;
            int i; /*Loop Counter.*/
            int j; /*Loop Counter.*/
            int rows; /*Number of rows.*/
            int cols; /*Number of columns.*/
            float min; /*Temp value */
            float old; /*temp debugging value */
            int nw, n, ne, w, c, e, sw, s, se;
            rows = demData.imagNrows;
            cols = demData.imagNcols;
            float Min = (float)100000.0;
            min = Min;
            for (i = 2; i < demData.imagNrows - 2; i++)
                for (j = 2; j < demData.imagNcols - 2; j++)
                {
                    c = i * cols + j;
                    nw = c - cols - 1;
                    n = c - cols;
                    ne = c - cols + 1;
                    w = c - 1;
                    e = c + 1;
                    sw = c + cols - 1;
                    s = c + cols;
                    se = c + cols + 1;
                    if (noDataMap[c])
                        continue;
                    min = (float)100000.0; //放一个最大值，
                    //判断周围是否有值，只要有一个无值则继续循环
                    if (noDataMap[nw] && noDataMap[n] && noDataMap[ne] &&
                        noDataMap[e] && noDataMap[se] && noDataMap[s]
                        && noDataMap[sw] && noDataMap[w])
                    {
                        continue;
                    }
                    min = demMap[nw] < min ? demMap[nw] : min;
                    min = demMap[n] < min ? demMap[n] : min;
                    min = demMap[ne] < min ? demMap[ne] : min;
                    min = demMap[e] < min ? demMap[e] : min;
                    min = demMap[se] < min ? demMap[se] : min;
                    min = demMap[s] < min ? demMap[s] : min;
                    min = demMap[sw] < min ? demMap[sw] : min;
                    min = demMap[w] < min ? demMap[w] : min;
                    // 如果最小值大于中心值，说明是洼地，则进行填充
                    if (min > demMap[c])
                    {
                        flag = true;
                        old = demMap[c];
                        demMap[c] = min;
                        LogFill(i - 2, j - 2, demData.dataPrecision, old, min);
                    }
                }
            return flag;
        }
        public bool AnnulusFill(DemData demData, ref bool[] noDataMap, ref float[] demMap)
        {
            bool flag = false;
            int i; /*Loop Counter.*/
            int j; /*Loop Counter.*/
            int rows; /*Number of rows.*/
            int cols; /*Number of columns.*/
            float min; /*Temp value */
            float old; /*Used for log file */
            int nw, n, ne, w, c, e, sw, s, se;
            int nnww, nnw, nn, nne, nnee;
            int nww, nee, ww, ee, sww, see;
            int ssww, ssw, ss, sse, ssee;
            /*张杰的修改*/
            rows = demData.imagNrows;
            cols = demData.imagNcols;
            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;

                    if (noDataMap[c])
                    {
                        continue;
                    }
                    nw = c - cols - 1;
                    n = c - cols;
                    ne = c - cols + 1;
                    w = c - 1;
                    e = c + 1;
                    sw = c + cols - 1;
                    s = c + cols;
                    se = c + cols + 1;


                    if (noDataMap[nw] || noDataMap[n] || noDataMap[ne] || noDataMap[w] ||
                        noDataMap[e] || noDataMap[sw] || noDataMap[s] || noDataMap[se])
                    {
                        continue;
                    }

                    min = demMap[nw] < demMap[n] ? demMap[nw] : demMap[n];
                    min = demMap[ne] < min ? demMap[ne] : min;
                    min = demMap[w] < min ? demMap[w] : min;
                    min = demMap[e] < min ? demMap[e] : min;
                    min = demMap[sw] < min ? demMap[sw] : min;
                    min = demMap[s] < min ? demMap[s] : min;
                    min = demMap[se] < min ? demMap[se] : min;

                    if (GlobalConstants.FUZZ < Math.Abs(min - demMap[c]))
                    {
                        continue;
                    }
                    nn = n - cols;
                    nnww = nn - 2;
                    nnw = nn - 1;
                    nne = nn + 1;
                    nnee = nn + 2;
                    nww = nw - 1;
                    nee = ne + 1;
                    ww = w - 1;
                    ee = e + 1;
                    sww = sw - 1;
                    see = se + 1;
                    ss = s + cols;
                    ssww = ss - 2;
                    ssw = ss - 1;
                    sse = ss + 1;
                    ssee = ss + 2;

                    // Move on to the next cell if the annulus ring contains a nodata cell

                    if (noDataMap[nnww] || noDataMap[nnw] || noDataMap[nn] || noDataMap[nne] ||
                        noDataMap[nnee] || noDataMap[nww] || noDataMap[nee] || noDataMap[ww] ||
                        noDataMap[ee] || noDataMap[sww] || noDataMap[see] || noDataMap[ssww] ||
                        noDataMap[ssw] || noDataMap[ss] || noDataMap[sse] || noDataMap[ssee])
                    {
                        continue;
                    }

                    // Determine the minimum value within the annulus ring.

                    min = demMap[nnww] < demMap[nnw] ? demMap[nnww] : demMap[nnw];
                    min = min < demMap[nn] ? min : demMap[nn];
                    min = min < demMap[nne] ? min : demMap[nne];
                    min = min < demMap[nnee] ? min : demMap[nnee];
                    min = min < demMap[nww] ? min : demMap[nww];
                    min = min < demMap[nee] ? min : demMap[nee];
                    min = min < demMap[ww] ? min : demMap[ww];
                    min = min < demMap[ss] ? min : demMap[ee];
                    min = min < demMap[sww] ? min : demMap[sww];
                    min = min < demMap[see] ? min : demMap[see];
                    min = min < demMap[ssww] ? min : demMap[ssww];
                    min = min < demMap[ssw] ? min : demMap[ssw];
                    min = min < demMap[ss] ? min : demMap[ss];
                    min = min < demMap[sse] ? min : demMap[sse];
                    min = min < demMap[ssee] ? min : demMap[ssee];

                    if (min > demMap[c])
                    {
                        flag = true;
                        old = demMap[c];
                        demMap[c] = min;
                        if (demMap[nw] < min) demMap[nw] = min;
                        if (demMap[n] < min) demMap[n] = min;
                        if (demMap[ne] < min) demMap[ne] = min;
                        if (demMap[w] < min) demMap[w] = min;
                        if (demMap[e] < min) demMap[e] = min;
                        if (demMap[sw] < min) demMap[sw] = min;
                        if (demMap[s] < min) demMap[s] = min;
                        if (demMap[se] < min) demMap[se] = min;
                        LogAnnulusFill(i - 2, j - 2, demData.dataPrecision, old, min);
                    }
                }
            }

            return flag;
        }

        #endregion

        //##############################################



        //                    CalcData



        //##############################################
        #region CalcData
        public void downSlope(DemData demData, bool[] noDataMap, float[] demMap, ref float[] slopeAng, ref float[] cellLen, ref byte[] outFlow)
        {
            float angle;
            float bestAngle;
            float bestCellLen;
            byte bestDirection;
            float diagCellSize; /* Diagonal cell size.*/
            float deg; /* Change to degrees.*/
            float cellSize; /* Cell Size.*/
            int rows; /* Number of rows.*/
            int cols; /* Number of columns.*/
            int nw, n, ne, w, c, e, sw, s, se;

            rows = demData.imagNrows;
            cols = demData.imagNcols;
            cellSize = demData.cellSize;
            diagCellSize = cellSize * (float)1.4142136; // for NW, NE, SE, SW 
            deg = (float)57.29577951308;
   
            bool flag = false;

            //因为数组多增加了两周，所以在处理的时候不用考虑越界，这样程序写起来就简单多了
            for (int i = 2; i < rows - 2; i++)
            {
                for (int j = 2; j < cols - 2; j++)
                {
                    //maxDiffer = (float)-1.0;
                    bestCellLen = (float)0.0;
                    bestDirection = (byte)0.0;
                    bestAngle = 0.0f;
                    angle = 0.0f;
                    c = i * cols + j;
                    if (noDataMap[c])
                    {
                        slopeAng[c] = (float)0.0;
                        cellLen[c] = (float)0.0;
                        outFlow[c] = (byte)0.0;
                        continue;
                    }
                    flag = false;
                    nw = c - cols - 1;
                    n = c - cols;
                    ne = c - cols + 1;
                    w = c - 1;
                    e = c + 1;
                    sw = c + cols - 1;
                    s = c + cols;
                    se = c + cols + 1;
                    /*张杰的修改*/
                    //西
                    if (!noDataMap[w] && demMap[w] < demMap[c])
                    {
                        //angle = deg * (float)Math.Atan((demMap[c] - demMap[w]) / cellSize);
                        if (outFlow[w] == GlobalConstants.E)
                        {
                            // 这里防止对流现象的出现
                            // zhangjie.10.06.10
                        }
                        else
                        {
                            //differ = demMap[c] - demMap[w];
                            angle = deg * (float)Math.Atan((demMap[c] - demMap[w]) / cellSize);
                           
                            if (angle > bestAngle)
                            {
                                bestAngle = angle;
                                bestDirection = GlobalConstants.W;
                                bestCellLen = cellSize;
                                flag = true;
                            }
                            if (angle == 0.0 && !flag)
                            {
                                bestAngle =  GlobalConstants.MIN_SLOPE;
                                bestDirection = GlobalConstants.W;
                                bestCellLen = cellSize;
                                flag = true;
                            }
                        }

                    }//西北
                    if (!noDataMap[nw] && demMap[nw] < demMap[c])
                    {
                        if (outFlow[nw] == GlobalConstants.SE)
                        {

                        }
                        else
                        {
                            angle = deg * (float)Math.Atan((demMap[c] - demMap[nw]) / diagCellSize);
                            //differ = demMap[c] - demMap[nw];
                            if (angle > bestAngle)
                            {
                                bestAngle = angle;
                                bestDirection = GlobalConstants.NW;
                                bestCellLen = diagCellSize;
                                flag = true;
                            }
                            if (angle == 0.0 && !flag)
                            {
                                bestAngle = GlobalConstants.MIN_SLOPE;
                                bestDirection = GlobalConstants.NW;
                                bestCellLen = diagCellSize;
                                flag = true;
                            }

                        }
                    }//北
                    if (!noDataMap[n] && demMap[n] < demMap[c])
                    {
                        if (outFlow[n] == GlobalConstants.S)
                        {
                           // Debug.WriteLine("outFlow[n] = "+outFlow[n].ToString());
                        }
                        else
                        {
                            angle = deg * (float)Math.Atan((demMap[c] - demMap[n]) / cellSize);
                            //differ = demMap[c] - demMap[n];
                            if (angle > bestAngle)
                            {
                                bestAngle = angle;
                                bestDirection = GlobalConstants.N;
                                bestCellLen = cellSize;
                                flag = true;
                            }
                            if (angle == 0.0 && !flag)
                            {
                                bestAngle = GlobalConstants.MIN_SLOPE;
                                bestDirection = GlobalConstants.N;
                                bestCellLen = cellSize;
                                flag = true;
                            }
   
                        }
                    }//东北
                    if (!noDataMap[ne] && demMap[ne] < demMap[c])
                    {
                        if (outFlow[ne] == GlobalConstants.SW)
                        {

                        }
                        else
                        {
                            angle = deg * (float)Math.Atan((demMap[c] - demMap[ne]) / diagCellSize);
                            //differ = demMap[c] - demMap[ne];
                            if (angle > bestAngle)
                            {
                                bestAngle = angle;
                                bestDirection = GlobalConstants.NE;
                                bestCellLen = diagCellSize;
                                flag = true;
                            }
                            if (angle == 0.0 && !flag)
                            {
                                bestAngle = GlobalConstants.MIN_SLOPE;
                                bestDirection = GlobalConstants.NE;
                                bestCellLen = diagCellSize;
                                flag = true;
                            }
                        }
                    }//东
                    if (!noDataMap[e] && demMap[e] < demMap[c])
                    {
                         angle = deg * (float)Math.Atan((demMap[c] - demMap[e]) / cellSize);
                        //differ = demMap[c] - demMap[e];
                         if (angle > bestAngle)
                         {
                             bestAngle = angle;
                             bestDirection = GlobalConstants.E;
                             bestCellLen = cellSize;
                             flag = true;
                         }
                         if (angle == 0.0 && !flag )
                         {
                             bestAngle = GlobalConstants.MIN_SLOPE;
                             bestDirection = GlobalConstants.E;
                             bestCellLen = cellSize;
                             flag = true;
                         }
                    }//东南
                    if (!noDataMap[se] && demMap[se] < demMap[c])
                    {
                        angle = deg * (float)Math.Atan((demMap[c] - demMap[se]) / diagCellSize);
                        //differ = demMap[c] - demMap[se];
                        
                        if (angle > bestAngle)
                        {
                            bestAngle = angle;
                            bestDirection = GlobalConstants.SE;
                            bestCellLen = diagCellSize;
                            flag = true;
                        }
                        if (angle == 0.0 && !flag)
                        {
                            bestAngle = GlobalConstants.MIN_SLOPE;
                            bestDirection = GlobalConstants.SE;
                            bestCellLen = diagCellSize;
                            flag = true;
                        }
                    }//南
                    if (!noDataMap[s] && demMap[s] < demMap[c])
                    {
                        angle = deg * (float)Math.Atan((demMap[c] - demMap[s]) / cellSize);
                        //differ = demMap[c] - demMap[s];
                         if (angle > bestAngle)
                         {
                             bestAngle = angle;
                             bestDirection = GlobalConstants.S;
                             bestCellLen = cellSize;
                             flag = true;
                         }
                         if (angle == 0.0 && !flag)
                         {
                             bestAngle = GlobalConstants.MIN_SLOPE;
                             bestDirection = GlobalConstants.S;
                             bestCellLen = cellSize;
                             flag = true;
                         }

                    }
                    //西南
                    if (!noDataMap[sw] && demMap[sw] < demMap[c])
                    {
                        angle = deg * (float)Math.Atan((demMap[c] - demMap[sw]) / diagCellSize);
                        //differ = demMap[c] - demMap[sw];
                        if (angle > bestAngle)
                        {
                            bestAngle = angle;
                            bestDirection = GlobalConstants.SW;
                            bestCellLen = diagCellSize;
                            flag = true;
                        }
                        if (angle == 0.0 && !flag)
                        {
                            bestAngle = GlobalConstants.MIN_SLOPE;
                            bestDirection = GlobalConstants.SW;
                            bestCellLen = diagCellSize;
                            flag = true;
                        }
                    }
                    if (flag == false)//这里以前为maxDiffer == 0.0
                    {
                        //Debug.WriteLine("0");
                        //Debug.WriteLine("X = "+ i.ToString() + "Y = " + j.ToString());
                        slopeAng[c] = GlobalConstants.MIN_SLOPE; //float(0.1)			
                        //09.12.01上午修改
                        outFlow[c] = GlobalConstants.NONE;
                        //Debug.WriteLine("demmap[w] = " + demMap[w].ToString());
                        //Debug.WriteLine("outFlow[n] = " + outFlow[n].ToString());
                        if (!noDataMap[w] && demMap[w] <= demMap[c])
                        {
                            cellLen[c] = cellSize;
                        }
                         else if (!noDataMap[nw] && demMap[nw] <= demMap[c])
                        {
                            cellLen[c] = diagCellSize;
                        }
                        else if (!noDataMap[n] && demMap[n] <= demMap[c])
                        {
                            //Debug.WriteLine("n");
                          
                            cellLen[c] = cellSize;
                        }
                         else if (!noDataMap[ne] && demMap[ne] <= demMap[c])
                        {
                            //Debug.WriteLine("ne");
                            
                            cellLen[c] = diagCellSize;
                        }
                        else if (!noDataMap[e] && demMap[e] <= demMap[c])
                        {
                           // Debug.WriteLine("e");
                           
                            cellLen[c] = cellSize;
                        }
                        else if (!noDataMap[se] && demMap[se] <= demMap[c])
                        {
                            //Debug.WriteLine("se");
                          
                            cellLen[c] = diagCellSize;
                        }
                        else if (!noDataMap[se] && demMap[se] <= demMap[c])
                        {
                            //Debug.WriteLine("se");
                            
                            cellLen[c] = diagCellSize;
                        }
                        else  if (!noDataMap[s] && demMap[s] <= demMap[c])
                        {
                            //Debug.WriteLine("s");
                          
                            cellLen[c] = cellSize;
                        }
                        else if (!noDataMap[sw] && demMap[sw] <= demMap[c])
                        {
                            //Debug.WriteLine("sw");
                           
                            cellLen[c] = diagCellSize;
                        }
                        else
                        {
                            cellLen[c] = diagCellSize;
                        }
                    }
                    else
                    {
                        slopeAng[c] = bestAngle;
                        cellLen[c] = bestCellLen;
                        outFlow[c] = bestDirection;
                    }

                }
            }
        }
        public void SetInFlow(DemData dd, ref bool[] noDataMap, ref byte[] outMap, ref byte[] inMap)
        {
            int rows; /*Number of rows.*/
            int cols; /*Number of columns.*/
            int nw, n, ne, w, c, e, sw, s, se;
            rows = dd.imagNrows;
            cols = dd.imagNcols;
            for (int i = 2; i < rows - 2; i++)
            {
                for (int j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    inMap[c] = 0;
                    if (noDataMap[c])
                    {
                        continue;
                    }
                    nw = c - cols - 1;
                    n = c - cols;
                    ne = c - cols + 1;
                    w = c - 1;
                    e = c + 1;
                    sw = c + cols - 1;
                    s = c + cols;
                    se = c + cols + 1;
                    //西北
                    if (outMap[nw] == GlobalConstants.SE)
                        inMap[c] += GlobalConstants.NW;
                    //北
                    if (outMap[n] == GlobalConstants.S)
                        inMap[c] += GlobalConstants.N;
                    //东北
                    if (outMap[ne] == GlobalConstants.SW)
                        inMap[c] += GlobalConstants.NE;
                    //西
                    if (outMap[w] == GlobalConstants.E)
                        inMap[c] += GlobalConstants.W;
                    //东
                    if (outMap[e] == GlobalConstants.W)
                        inMap[c] += GlobalConstants.E;
                    //西南
                    if (outMap[sw] == GlobalConstants.NE)
                        inMap[c] += GlobalConstants.SW;
                    //南
                    if (outMap[s] == GlobalConstants.N)
                        inMap[c] += GlobalConstants.S;
                    //东南
                    if (outMap[se] == GlobalConstants.NW)
                        inMap[c] += GlobalConstants.SE;

                }
            }
        }
        //符合坡度变化就设置截断点

        public void BuildSlopeCutGrid(DemData dd, byte[] outFlow, float[] slopeAng, bool[] noDataMap, bool[] flowCut)
        {
            int i, j;
            int c, ft;
            int rows;
            int cols;
            float slpEndFactor;

            rows = dd.imagNrows;
            cols = dd.imagNcols;

            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    if (noDataMap[c])
                    {
                        flowCut[c] = true;
                        continue;
                    }
                    switch (outFlow[c])
                    {
                        case GlobalConstants.E:
                            ft = c + 1;
                            break;
                        case GlobalConstants.SE:
                            ft = c + cols + 1;
                            break;
                        case GlobalConstants.S:
                            ft = c + cols;
                            break;
                        case GlobalConstants.SW:
                            ft = c + cols - 1;
                            break;
                        case GlobalConstants.W:
                            ft = c - 1;
                            break;
                        case GlobalConstants.NW:
                            ft = c - cols - 1;
                            break;
                        case GlobalConstants.N:
                            ft = c - cols;
                            break;
                        case GlobalConstants.NE:
                            ft = c - cols + 1;
                            break;
                        default:
                            flowCut[c] = true;
                            continue;
                    };
                    slpEndFactor = slopeAng[c] < 2.8624 ? dd.scf_lt5 : dd.scf_ge5;
                    flowCut[c] = slopeAng[ft] < slopeAng[c] * slpEndFactor;
                    //flowCut[c] = false;
                }
            }
        }
        //获取河网
        public void CalcChannelNetworks(DemData dd, ref Int32[] Cumlen_ChannelNetworks, byte[] outflow, bool[] nd)
        {
            int rows; // Number of rows.
            int cols; // Number of columns.
            int count = 0; //记录循环次数
            int hits = 0, hits1 = 0;
            int nw, n, ne, w, c, e, sw, s, se;
            int inicumarea = 1;//每个单元格的面积
            bool done = false;
            Int32 cumarea;
 
            rows = dd.imagNrows;
            cols = dd.imagNcols;
    
            for (int i = 2; i < rows - 2; i++)
            {
                for (int j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    if (nd[c])
                    {
                        Cumlen_ChannelNetworks[c] = 0;
                        continue;
                    } //如果无值，跳出该次循环
                    Cumlen_ChannelNetworks[c] = inicumarea;
                }
            }
            while (!done && count < 10000)
            {
                count++;
                done = true;
                hits = 0;
                for (int i = 2; i < rows - 2; i++)
                {
                    for (int j = 2; j < cols - 2; j++)
                    {
                        c = i * cols + j;
                        if (nd[c])
                        {
                            continue;
                        } //如果无值，跳出该次循环

                        cumarea =0; // 累计坡长为0.0

                        nw = c - cols - 1;
                        n = c - cols;
                        ne = c - cols + 1;
                        w = c - 1;
                        e = c + 1;
                        sw = c + cols - 1;
                        s = c + cols;
                        se = c + cols + 1;

                        //西
                        if (outflow[w] == GlobalConstants.E)
                        {
                            cumarea += Cumlen_ChannelNetworks[w];
                        }
                        //西北
                        if (outflow[nw] == GlobalConstants.SE)
                        {
                            cumarea += Cumlen_ChannelNetworks[nw];
                        }
                        //北                          
                        if (outflow[n] == GlobalConstants.S )
                        {
                            cumarea += Cumlen_ChannelNetworks[n];
                        }
                        //东北
                        if (outflow[ne] == GlobalConstants.SW)
                        {
                            cumarea += Cumlen_ChannelNetworks[ne];
                        }
                        //东
                        if (outflow[e] == GlobalConstants.W)
                        {
                            cumarea += Cumlen_ChannelNetworks[e];
                        }
                        //东南
                        if (outflow[se] == GlobalConstants.NW)
                        {
                            cumarea += Cumlen_ChannelNetworks[se];
                        }
                        //南
                        if (outflow[s] == GlobalConstants.N )
                        {
                            cumarea += Cumlen_ChannelNetworks[s];
                        }
                        //西南
                        if (outflow[sw] == GlobalConstants.NE)
                        {
                            cumarea += Cumlen_ChannelNetworks[sw];
                        }
                        if (cumarea > 0.0)
                        {
                            cumarea += inicumarea;
                            if (cumarea > Cumlen_ChannelNetworks[c])
                            {
                                hits++;
                                done = false;
                                Cumlen_ChannelNetworks[c] = cumarea;
                            }
                        }
                    } // END for(i = 0; i < rows; i++) FIRST PART
                } // END for(j = 0; j < cols, j++)	  FIRST PART

                if (hits == hits1)
                    count = 10000;

                hits1 = hits;
                hits = 0;
                //为什么要反方向重新计算呢？
                for (int i = rows - 3; i >= 2; i--)
                {
                    // SECOND PART
                    for (int j = cols - 3; j >= 2; j--)
                    {
                        // SECOND PART
                        c = i * cols + j;
                        if (nd[c])
                        {
                            continue;
                        }
                        cumarea =0;

                        nw = c - cols - 1;
                        n = c - cols;
                        ne = c - cols + 1;
                        w = c - 1;
                        e = c + 1;
                        sw = c + cols - 1;
                        s = c + cols;
                        se = c + cols + 1;
                        //西北
                        if (outflow[nw] == GlobalConstants.SE)
                        {
                            cumarea += Cumlen_ChannelNetworks[nw];
                        }//北
                        if (outflow[n] == GlobalConstants.S )
                        {
                            cumarea += Cumlen_ChannelNetworks[n];
                        }//东北
                        if (outflow[ne] == GlobalConstants.SW )
                        {
                            cumarea += Cumlen_ChannelNetworks[ne];
                        }//西
                        if (outflow[w] == GlobalConstants.E )
                        {
                            cumarea += Cumlen_ChannelNetworks[w];
                        } //东
                        if (outflow[e] == GlobalConstants.W)
                        {
                            cumarea += Cumlen_ChannelNetworks[e];
                        }//西南
                        if (outflow[sw] == GlobalConstants.NE )
                        {
                            cumarea += Cumlen_ChannelNetworks[sw];
                        }//南
                        if (outflow[s] == GlobalConstants.N )
                        {
                            cumarea += Cumlen_ChannelNetworks[s];
                        }//东南
                        if (outflow[se] == GlobalConstants.NW )
                        {
                            cumarea += Cumlen_ChannelNetworks[se];
                        }
                        if (cumarea > 0.0)
                        {
                            cumarea += inicumarea;
                            if (cumarea > Cumlen_ChannelNetworks[c])
                            {
                                done = false;
                                hits++;
                                Cumlen_ChannelNetworks[c] = cumarea;
                            }
                        }
                    } // END for(i = 0; i < rows; i++)  SECOND PART
                } // END for(j = 0; j < cols, j++)  SECOND PART

                LogCumulativeProgress(count, hits1, hits);

            }

        }
        //重新建立截断
        public void ReBuildSlopeCutGrid(DemData dd, byte[] outFlow, float[] slopeAng, bool[] noDataMap, bool[] flowCut, Int32[] Cumlen_ChannelNetworks, float threshold)
        {
            int i, j;
            int c, ft;
            int rows;
            int cols;
            float slpEndFactor;

            rows = dd.imagNrows;
            cols = dd.imagNcols;
            threshold = threshold / (dd.cellSize * dd.cellSize);//转换为面积
            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    if (noDataMap[c])
                    {
                        flowCut[c] = true;
                        continue;
                    }
                    switch (outFlow[c])
                    {
                        case GlobalConstants.E:
                            ft = c + 1;
                            break;
                        case GlobalConstants.SE:
                            ft = c + cols + 1;
                            break;
                        case GlobalConstants.S:
                            ft = c + cols;
                            break;
                        case GlobalConstants.SW:
                            ft = c + cols - 1;
                            break;
                        case GlobalConstants.W:
                            ft = c - 1;
                            break;
                        case GlobalConstants.NW:
                            ft = c - cols - 1;
                            break;
                        case GlobalConstants.N:
                            ft = c - cols;
                            break;
                        case GlobalConstants.NE:
                            ft = c - cols + 1;
                            break;
                        default:
                            flowCut[c] = true;
                            continue;
                    };
                    slpEndFactor = slopeAng[c] < 2.8624 ? dd.scf_lt5 : dd.scf_ge5;
                    flowCut[c] = slopeAng[ft] < slopeAng[c] * slpEndFactor;
                    if (Cumlen_ChannelNetworks[ft] >= threshold)
                        flowCut[c] = true;//如果大于设定的汇流面积则依然为截断
                    //flowCut[c] = false;
                }
            }
        }
        //没有流向才有截断
        public void WithoutSlopeCutGrid(DemData dd, bool[] noDataMap, bool[] flowCut)
        {
            int i, j;
            int c, ft;
            int rows;
            int cols;
            float slpEndFactor;

            rows = dd.imagNrows;
            cols = dd.imagNcols;

            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    if (noDataMap[c])
                    {
                        flowCut[c] = true;
                        continue;
                    }
                    else
                    {
                        flowCut[c] = false;
                    }
                }
            }
        }
        public void InitCumulativeLength(DemData dd, byte[] inFlow, byte[] outFlow, bool[] noDataMap, bool[] flowCut, float[] slopeLen,ref float[] cumlen)
        {
            int i; //  Loop Counter.
            int j; //  Loop Counter.
            int c; //  Cell Index value  
            int rows; //  Number of rows.
            int cols; //  Number of columns. 
            float cellorth; //  Width of cell in cardinal direction
            float celldiag; //  Diagonal width of cell
            int nw, n, ne, w, e, sw, s, se;
            rows = dd.imagNrows;
            cols = dd.imagNcols;

            cellorth = dd.cellSize; //单元格长度
            celldiag = (float)Math.Sqrt(2.0) * dd.cellSize; //对角线长度

            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    cumlen[c] = (float)0.0;
                    if (noDataMap[c])
                        continue;
                    else
                    {
                        nw = c - cols - 1;
                        n = c - cols;
                        ne = c - cols + 1;
                        w = c - 1;
                        e = c + 1;
                        sw = c + cols - 1;
                        s = c + cols;
                        se = c + cols + 1;
                        switch (outFlow[c])
                        {
                            case GlobalConstants.N:
                            case GlobalConstants.E:
                            case GlobalConstants.S:
                            case GlobalConstants.W:
                                if (flowCut[c])
                                //    if (flowCut[n] && flowCut[ne] && flowCut[e] && flowCut[se] && flowCut[s]
                                //&& flowCut[sw] && flowCut[w] && flowCut[nw])
                                //        cumlen[c] =0;
                                //    else 
                                        cumlen[c] = cellorth/2;
                                else
                                    cumlen[c] = cellorth;
                                break;
                            case GlobalConstants.NW:
                            case GlobalConstants.NE:
                            case GlobalConstants.SW:
                            case GlobalConstants.SE:
                                if(flowCut[c])
                               //     if (flowCut[n] && flowCut[ne] && flowCut[e] && flowCut[se] && flowCut[s]
                               //&& flowCut[sw] && flowCut[w] && flowCut[nw])
                               //         cumlen[c] = 0;
                               //     else
                                        cumlen[c] = celldiag/2;
                                else
                                    cumlen[c] = celldiag;
                                break;
                            default:
                               // if (flowCut[n] && flowCut[ne] && flowCut[e] && flowCut[se] && flowCut[s]
                               //&& flowCut[sw] && flowCut[w] && flowCut[nw])
                               //     cumlen[c] = 0;
                               // else
                                    cumlen[c] = slopeLen[c]/2; 
                                break;
                        }
                    }
                }
            }
        }
        //累计最大坡长
        public void CalcCumulativeLength(DemData dd, byte[] outflow, bool[] nd, bool[] breakflow, float[] cumlen)
        {
            int rows; /*Number of rows.*/
            int cols; /*Number of columns.*/
            int count = 0; //记录循环次数
            int hits, hits1=0;
            int nw, n, ne, w, c, e, sw, s, se;
            bool done = false;
            float cumlength;
            float diagcellsize;

            rows = dd.imagNrows;
            cols = dd.imagNcols;

            diagcellsize = (float)Math.Sqrt(2.0) * dd.cellSize;
            while (!done && count < 10000)
            {
                count++;
                done = true;
                hits = 0;                
                for (int i = 2; i < rows - 2; i++)
                {
                    for (int j = 2; j < cols - 2; j++)
                    {
                        c = i * cols + j;
                        if (nd[c])
                        {
                            continue;
                        } //如果无值，跳出该次循环
                        //if (breakflow[c])
                        //{
                        //    continue;
                        //}//如果截断，跳出循环
                        cumlength = (float)0.0; // 累计坡长为0.0

                        nw = c - cols - 1;
                        n = c - cols;
                        ne = c - cols + 1;
                        w = c - 1;
                        e = c + 1;
                        sw = c + cols - 1;
                        s = c + cols;
                        se = c + cols + 1;

                        //西
                        if (outflow[w] == GlobalConstants.E && !breakflow[w] && cumlen[w] > cumlength)
                        {
                            cumlength = cumlen[w];
                        }
                        //西北
                        if (outflow[nw] == GlobalConstants.SE && !breakflow[nw] && cumlen[nw] > cumlength)
                        {
                            cumlength = cumlen[nw];
                        }
                        //北                          
                        if (outflow[n] == GlobalConstants.S && !breakflow[n] && cumlen[n] > cumlength)
                        {
                            cumlength = cumlen[n];
                        }
                        //东北
                        if (outflow[ne] == GlobalConstants.SW && !breakflow[ne] && cumlen[ne] > cumlength)
                        {
                            cumlength = cumlen[ne];
                        } 
                        //东
                        if (outflow[e] == GlobalConstants.W && !breakflow[e] && cumlen[e] > cumlength)
                        {
                            cumlength = cumlen[e];
                        }
                        //东南
                        if (outflow[se] == GlobalConstants.NW && !breakflow[se] && cumlen[se] > cumlength)
                        {
                            cumlength = cumlen[se];
                        }
                        //南
                        if (outflow[s] == GlobalConstants.N && !breakflow[s] && cumlen[s] > cumlength)
                        {
                            cumlength = cumlen[s];
                        }
                        //西南
                        if (outflow[sw] == GlobalConstants.NE && !breakflow[sw] && cumlen[sw] > cumlength)
                        {
                            cumlength = cumlen[sw];
                        }
                        if (cumlength > 0.0)
                        {
                            if (outflow[c] == GlobalConstants.N || outflow[c] == GlobalConstants.E || outflow[c] == GlobalConstants.S ||
                                outflow[c] == GlobalConstants.W)
                                if(breakflow[c])                                    
                                    cumlength += dd.cellSize/2;
                                else
                                    cumlength += dd.cellSize;
                            else if (outflow[c] == GlobalConstants.NE || outflow[c] == GlobalConstants.NW || outflow[c] == GlobalConstants.SE ||
                                     outflow[c] == GlobalConstants.SW)
                                if (breakflow[c])
                                    cumlength += diagcellsize/2;
                                else
                                    cumlength += diagcellsize;
                            else
                                cumlength += dd.cellSize/2;
                            if (cumlength > cumlen[c])
                            {
                                hits++;
                                done = false;
                                cumlen[c] = cumlength;
                            }
                        }
                    } // END for(i = 0; i < rows; i++) FIRST PART
                } // END for(j = 0; j < cols, j++)	  FIRST PART

                if (hits == hits1)
                    count = 10000;

                hits1 = hits;
                hits = 0;
                //为什么要反方向重新计算呢？
                for (int i = rows - 3; i >= 2; i--)
                {
                    // SECOND PART
                    for (int j = cols - 3; j >= 2; j--)
                    {
                        // SECOND PART
                        c = i * cols + j;
                        if (nd[c])
                        {
                            continue;
                        }
                        //if (breakflow[c])
                        //{
                        //    continue;
                        //}//如果截断，跳出循环
                        cumlength = (float)0.0;

                        nw = c - cols - 1;
                        n = c - cols;
                        ne = c - cols + 1;
                        w = c - 1;
                        e = c + 1;
                        sw = c + cols - 1;
                        s = c + cols;
                        se = c + cols + 1;
                        //西北
                        if (outflow[nw] == GlobalConstants.SE && !breakflow[nw] && cumlen[nw] > cumlength)
                        {
                            cumlength = cumlen[nw];
                        }//北
                        if (outflow[n] == GlobalConstants.S && !breakflow[n] && cumlen[n] > cumlength)
                        {
                            cumlength = cumlen[n];
                        }//东北
                        if (outflow[ne] == GlobalConstants.SW && !breakflow[ne] && cumlen[ne] > cumlength)
                        {
                            cumlength = cumlen[ne];
                        }//西
                        if (outflow[w] == GlobalConstants.E && !breakflow[w] && cumlen[w] > cumlength)
                        {
                            cumlength = cumlen[w];
                        } //东
                        if (outflow[e] == GlobalConstants.W && !breakflow[e] && cumlen[e] > cumlength)
                        {
                            cumlength = cumlen[e];
                        }//西南
                        if (outflow[sw] == GlobalConstants.NE && !breakflow[sw] && cumlen[sw] > cumlength)
                        {
                            cumlength = cumlen[sw];
                        }//南
                        if (outflow[s] == GlobalConstants.N && !breakflow[s] && cumlen[s] > cumlength)
                        {
                            cumlength = cumlen[s];
                        }//东南
                        if (outflow[se] == GlobalConstants.NW && !breakflow[se] && cumlen[se] > cumlength)
                        {
                            cumlength = cumlen[se];
                        }
                        if (cumlength > 0.0)
                        {
                            if (outflow[c] == GlobalConstants.N || outflow[c] == GlobalConstants.E || outflow[c] == GlobalConstants.S ||
                                outflow[c] == GlobalConstants.W)
                                if (breakflow[c])
                                    cumlength += dd.cellSize / 2;
                                else
                                    cumlength += dd.cellSize;
                            else if (outflow[c] == GlobalConstants.NE || outflow[c] == GlobalConstants.NW || outflow[c] == GlobalConstants.SE ||
                                     outflow[c] == GlobalConstants.SW)
                                if (breakflow[c])
                                    cumlength += diagcellsize / 2;
                                else
                                    cumlength += diagcellsize;
                            else
                                cumlength += dd.cellSize/2;
                            if (cumlength > cumlen[c])
                            {
                                done = false;
                                hits++;
                                cumlen[c] = cumlength;                                
                            }
                        }
                    } // END for(i = 0; i < rows; i++)  SECOND PART
                } // END for(j = 0; j < cols, j++)  SECOND PART

                LogCumulativeProgress(count, hits1, hits);

            }

        }
        //累计总坡长
        public void CalcCumulativeLength_Sum(DemData dd, byte[] outflow, bool[] nd, bool[] breakflow, float[] cumlen)
        {
            int rows; /*Number of rows.*/
            int cols; /*Number of columns.*/
            int count = 0; //记录循环次数
            int hits, hits1 = 0;
            int nw, n, ne, w, c, e, sw, s, se;
            bool done = false;
            float cumlength;
            float diagcellsize;

            rows = dd.imagNrows;
            cols = dd.imagNcols;

            diagcellsize = (float)Math.Sqrt(2.0) * dd.cellSize;
            while (!done && count < 10000)
            {
                count++;
                done = true;
                hits = 0;
                for (int i = 2; i < rows - 2; i++)
                {
                    for (int j = 2; j < cols - 2; j++)
                    {
                        c = i * cols + j;
                        if (nd[c])
                        {
                            continue;
                        } //如果无值，跳出该次循环

                        cumlength = (float)0.0; // 累计坡长为0.0

                        nw = c - cols - 1;
                        n = c - cols;
                        ne = c - cols + 1;
                        w = c - 1;
                        e = c + 1;
                        sw = c + cols - 1;
                        s = c + cols;
                        se = c + cols + 1;

                        //西
                        if (outflow[w] == GlobalConstants.E && !breakflow[w] )
                        {
                            cumlength += cumlen[w];
                        }
                        //西北
                        if (outflow[nw] == GlobalConstants.SE && !breakflow[nw] )
                        {
                            cumlength += cumlen[nw];
                        }
                        //北                          
                        if (outflow[n] == GlobalConstants.S && !breakflow[n])
                        {
                            cumlength += cumlen[n];
                        }
                        //东北
                        if (outflow[ne] == GlobalConstants.SW && !breakflow[ne])
                        {
                            cumlength += cumlen[ne];
                        }
                        //东
                        if (outflow[e] == GlobalConstants.W && !breakflow[e] )
                        {
                            cumlength += cumlen[e];
                        }
                        //东南
                        if (outflow[se] == GlobalConstants.NW && !breakflow[se] )
                        {
                            cumlength += cumlen[se];
                        }
                        //南
                        if (outflow[s] == GlobalConstants.N && !breakflow[s])
                        {
                            cumlength += cumlen[s];
                        }
                        //西南
                        if (outflow[sw] == GlobalConstants.NE && !breakflow[sw] )
                        {
                            cumlength += cumlen[sw];
                        }
                        if (cumlength > 0.0)
                        {
                            if (outflow[c] == GlobalConstants.N || outflow[c] == GlobalConstants.E || outflow[c] == GlobalConstants.S ||
                                outflow[c] == GlobalConstants.W)
                                cumlength += dd.cellSize;
                            else if (outflow[c] == GlobalConstants.NE || outflow[c] == GlobalConstants.NW || outflow[c] == GlobalConstants.SE ||
                                     outflow[c] == GlobalConstants.SW)
                                cumlength += diagcellsize;
                            else
                                cumlength += dd.cellSize;
                            if (cumlength > cumlen[c])
                            {
                                hits++;
                                done = false;
                                cumlen[c] = cumlength;
                            }
                        }
                    } 
                } 

                if (hits == hits1)
                    count = 10000;

                hits1 = hits;
                hits = 0;
              
                for (int i = rows - 3; i >= 2; i--)
                {
                    // SECOND PART
                    for (int j = cols - 3; j >= 2; j--)
                    {
                        // SECOND PART
                        c = i * cols + j;
                        if (nd[c])
                        {
                            continue;
                        }
                        cumlength = (float)0.0;

                        nw = c - cols - 1;
                        n = c - cols;
                        ne = c - cols + 1;
                        w = c - 1;
                        e = c + 1;
                        sw = c + cols - 1;
                        s = c + cols;
                        se = c + cols + 1;
                        //西北
                        if (outflow[nw] == GlobalConstants.SE && !breakflow[nw] )
                        {
                            cumlength += cumlen[nw];
                        }//北
                        if (outflow[n] == GlobalConstants.S && !breakflow[n] )
                        {
                            cumlength += cumlen[n];
                        }//东北
                        if (outflow[ne] == GlobalConstants.SW && !breakflow[ne] )
                        {
                            cumlength += cumlen[ne];
                        }//西
                        if (outflow[w] == GlobalConstants.E && !breakflow[w] )
                        {
                            cumlength += cumlen[w];
                        } //东
                        if (outflow[e] == GlobalConstants.W && !breakflow[e] )
                        {
                            cumlength += cumlen[e];
                        }//西南
                        if (outflow[sw] == GlobalConstants.NE && !breakflow[sw] )
                        {
                            cumlength += cumlen[sw];
                        }//南
                        if (outflow[s] == GlobalConstants.N && !breakflow[s] )
                        {
                            cumlength += cumlen[s];
                        }//东南
                        if (outflow[se] == GlobalConstants.NW && !breakflow[se] )
                        {
                            cumlength += cumlen[se];
                        }
                        if (cumlength > 0.0)
                        {
                            if (outflow[c] == GlobalConstants.N || outflow[c] == GlobalConstants.E || outflow[c] == GlobalConstants.S ||
                                outflow[c] == GlobalConstants.W)
                                cumlength += dd.cellSize;
                            else if (outflow[c] == GlobalConstants.NE || outflow[c] == GlobalConstants.NW || outflow[c] == GlobalConstants.SE ||
                                     outflow[c] == GlobalConstants.SW)
                                cumlength += diagcellsize;
                            if (cumlength > cumlen[c])
                            {
                                done = false;
                                hits++;
                                cumlen[c] = cumlength;
                            }
                        }
                    } 
                } 

                LogCumulativeProgress(count, hits1, hits);

            }

        }
        //累积周围平均坡长算法
        public void CalcCumulativeLength_New(DemData dd, byte[] outflow, bool[] nd, bool[] breakflow, float[] cumlen)
        {
            try
            {
                int rows; /*Number of rows.*/
                int cols; /*Number of columns.*/
                int count_while = 0; //记录循环次数
                int hits, hits1 = 0;
                int nw, n, ne, w, c, e, sw, s, se, count;
                bool done = false;
                float cumlength;
                float diagcellsize;
                string outflow_array;

                rows = dd.imagNrows;
                cols = dd.imagNcols;

                diagcellsize = (float)Math.Sqrt(2.0) * dd.cellSize;
                while (!done && count_while < 10000)
                {
                    count_while++;
                    done = true;
                    hits = 0;
                    for (int i = 2; i < rows - 2; i++)
                    {
                        for (int j = 2; j < cols - 2; j++)
                        {
                            c = i * cols + j;
                            if (nd[c])
                            {
                                continue;
                            } //如果无值，跳出该次循环

                            cumlength = (float)0.0; // 累计坡长为0.0
                            count = 0;
                            outflow_array = "";

                            nw = c - cols - 1;
                            n = c - cols;
                            ne = c - cols + 1;
                            w = c - 1;
                            e = c + 1;
                            sw = c + cols - 1;
                            s = c + cols;
                            se = c + cols + 1;

                            //西
                            if (outflow[w] == GlobalConstants.E && !breakflow[w] )
                            {
                                //cumlength = cumlen[w];
                                //outflow_array = outflow_array + "w";
                                //count++;
                                cumlength += cumlen[w];
                            }
                            //西北
                            if (outflow[nw] == GlobalConstants.SE && !breakflow[nw] )
                            {
                                //cumlength = cumlen[nw];
                                //if(outflow_array.Length>0)
                                //    outflow_array = outflow_array + ",nw";
                                //else
                                //    outflow_array = outflow_array + "nw";
                                //count++;
                                cumlength += cumlen[nw];
                            }
                            //北                          
                            if (outflow[n] == GlobalConstants.S && !breakflow[n] )
                            {
                                //cumlength = cumlen[n];
                                //if (outflow_array.Length > 0)
                                //    outflow_array = outflow_array + ",n";
                                //else
                                //    outflow_array = outflow_array + "n";
                                //count++;
                                cumlength += cumlen[n];
                            }
                            //东北
                            if (outflow[ne] == GlobalConstants.SW && !breakflow[ne] )
                            {
                                //cumlength = cumlen[ne];
                                //if (outflow_array.Length > 0)
                                //    outflow_array = outflow_array + ",ne";
                                //else
                                //    outflow_array = outflow_array + "ne";
                                //count++;
                                cumlength += cumlen[ne];
                            }
                            //东
                            if (outflow[e] == GlobalConstants.W && !breakflow[e])
                            {
                                //cumlength = cumlen[e];
                                //if (outflow_array.Length > 0)
                                //    outflow_array = outflow_array + ",e";
                                //else
                                //    outflow_array = outflow_array + "e";
                                //count++;
                                cumlength += cumlen[e];
                            }
                            //东南
                            if (outflow[se] == GlobalConstants.NW && !breakflow[se] )
                            {
                                //cumlength = cumlen[se];
                                //if (outflow_array.Length > 0)
                                //    outflow_array = outflow_array + ",se";
                                //else
                                //    outflow_array = outflow_array + "se";
                                //count++;
                                cumlength += cumlen[se];
                            }
                            //南
                            if (outflow[s] == GlobalConstants.N && !breakflow[s])
                            {
                                //cumlength = cumlen[s];
                                //if (outflow_array.Length > 0)
                                //    outflow_array = outflow_array + ",s";
                                //else
                                //    outflow_array = outflow_array + "s";
                                //count++;
                                cumlength += cumlen[s];
                            }
                            //西南
                            if (outflow[sw] == GlobalConstants.NE && !breakflow[sw] )
                            {
                                //cumlength = cumlen[sw];
                                //if (outflow_array.Length > 0)
                                //outflow_array = outflow_array + ",sw";
                                //else
                                //outflow_array = outflow_array + "sw";
                                //count++;
                                cumlength += cumlen[sw];
                            }
                            if (cumlength > 0.0)
                            {
                                if (outflow[c] == GlobalConstants.N || outflow[c] == GlobalConstants.E || outflow[c] == GlobalConstants.S ||
                                    outflow[c] == GlobalConstants.W)
                                    cumlength += dd.cellSize;
                                else if (outflow[c] == GlobalConstants.NE || outflow[c] == GlobalConstants.NW || outflow[c] == GlobalConstants.SE ||
                                         outflow[c] == GlobalConstants.SW)
                                    cumlength += diagcellsize;
                                if (cumlength > cumlen[c])
                                {
                                    hits++;
                                    done = false;
                                    cumlen[c] = cumlength;
                                }
                            }
                        } 
                    } 

                    if (hits == hits1)
                        count = 10000;

                    hits1 = hits;
                    hits = 0;
                    //为什么要反方向重新计算呢？
                    for (int i = rows - 3; i >= 2; i--)
                    {
                        // SECOND PART
                        for (int j = cols - 3; j >= 2; j--)
                        {
                            // SECOND PART
                            c = i * cols + j;
                            if (nd[c])
                            {
                                continue;
                            }
                            cumlength = (float)0.0;
                            outflow_array = "";
                            count = 0;

                            nw = c - cols - 1;
                            n = c - cols;
                            ne = c - cols + 1;
                            w = c - 1;
                            e = c + 1;
                            sw = c + cols - 1;
                            s = c + cols;
                            se = c + cols + 1;
                            //西北
                            if (outflow[nw] == GlobalConstants.SE && !breakflow[nw] )
                            {
                                //cumlength = cumlen[nw];
                                //outflow_array = outflow_array + "nw";
                                //count++;
                                cumlength += cumlen[nw];
                            }//北
                            if (outflow[n] == GlobalConstants.S && !breakflow[n] )
                            {
                                //cumlength = cumlen[n];
                                //if (outflow_array.Length > 0)
                                //outflow_array = outflow_array + ",n";
                                //else
                                //outflow_array = outflow_array + "n";
                                //count++;
                                cumlength += cumlen[n];
                            }//东北
                            if (outflow[ne] == GlobalConstants.SW && !breakflow[ne] )
                            {
                                //cumlength = cumlen[ne];
                                //if (outflow_array.Length > 0)
                                //outflow_array = outflow_array + ",ne";
                                //else
                                //outflow_array = outflow_array + "ne";
                                //count++;
                                cumlength += cumlen[ne];
                            }//西
                            if (outflow[w] == GlobalConstants.E && !breakflow[w] )
                            {
                                //cumlength = cumlen[w];
                                //if (outflow_array.Length > 0)
                                //outflow_array = outflow_array + ",w";
                                //else
                                //outflow_array = outflow_array + "w";
                                //count++;
                                cumlength += cumlen[w];
                            } //东
                            if (outflow[e] == GlobalConstants.W && !breakflow[e] )
                            {
                                //cumlength = cumlen[e];
                                //if (outflow_array.Length > 0)
                                //outflow_array = outflow_array + ",e";
                                //else
                                //outflow_array = outflow_array + "e";
                                //count++;
                                cumlength += cumlen[e];
                            }//西南
                            if (outflow[sw] == GlobalConstants.NE && !breakflow[sw] )
                            {
                                //cumlength = cumlen[sw];
                                //if (outflow_array.Length > 0)
                                //outflow_array = outflow_array + ",sw";
                                //else
                                //outflow_array = outflow_array + "sw";
                                //count++;
                                cumlength += cumlen[sw];
                            }//南
                            if (outflow[s] == GlobalConstants.N && !breakflow[s] )
                            {
                                //cumlength = cumlen[s];
                                //if (outflow_array.Length > 0)
                                // outflow_array = outflow_array + ",s";
                                //else
                                // outflow_array = outflow_array + "s";
                                //count++;
                                cumlength += cumlen[s];
                            }//东南
                            if (outflow[se] == GlobalConstants.NW && !breakflow[se] )
                            {
                                //cumlength = cumlen[se];
                                //if (outflow_array.Length > 0)
                                //    outflow_array = outflow_array + ",se";
                                //else
                                //    outflow_array = outflow_array + "se";
                                //count++;
                                cumlength += cumlen[se];
                            }
                            
                            if (cumlength > 0.0)
                            {
                                if (outflow[c] == GlobalConstants.N || outflow[c] == GlobalConstants.E || outflow[c] == GlobalConstants.S ||
                                    outflow[c] == GlobalConstants.W)
                                    cumlength += dd.cellSize;
                                else if (outflow[c] == GlobalConstants.NE || outflow[c] == GlobalConstants.NW || outflow[c] == GlobalConstants.SE ||
                                         outflow[c] == GlobalConstants.SW)
                                    cumlength += diagcellsize;
                                if (cumlength > cumlen[c])
                                {
                                    done = false;
                                    hits++;
                                    cumlen[c] = cumlength;
                                }
                            }
                        } // END for(i = 0; i < rows; i++)  SECOND PART
                    } // END for(j = 0; j < cols, j++)  SECOND PART
                    LogCumulativeProgress(count_while, hits1, hits);
                }
            }
            catch (Exception ex)
            {
                // 
                MessageBox.Show(ex.ToString());
            }

        }
        public void ConvertLengthToFeet(DemData dd, float[] slp_len_cum, float[] slp_lgth_ft)
        {
            int i; /*Loop Counter.*/
            int j; /*Loop Counter.*/
            int c;
            int rows; /*Number of rows.*/
            int cols; /*Number of columns.*/
            /*张杰的修改*/
            rows = dd.imagNrows;
            cols = dd.imagNcols;

            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    slp_lgth_ft[c] = slp_len_cum[c] / (float)0.3048;
                }
            }
        }
        public void Calculate_L(DemData dd, float[] slopeAng, float[] slp_lgth_ft, float[] ruslel,int rusle_csle)
        {
            int i; /*Loop Counter.*/
            int j; /*Loop Counter.*/
            int c;
            int rows; /*Number of rows.*/
            int cols; /*Number of columns.*/
            rows = dd.imagNrows;
            cols = dd.imagNcols;
            if(rusle_csle==1)//运行CSLE模型
                for (i = 2; i < rows - 2; i++)
                {
                    for (j = 2; j < cols - 2; j++)
                    {
                        c = i * cols + j;
                        ruslel[c] = (float)Math.Pow(slp_lgth_ft[c] / (float)22.1, TableLookUp_csle(slopeAng[c])); //x的y次幂
                    } 
                } 
            else//运行RUSLE
                for (i = 2; i < rows - 2; i++)
                {
                    for (j = 2; j < cols - 2; j++)
                    {
                        c = i * cols + j;
                        ruslel[c] = (float)Math.Pow(slp_lgth_ft[c] / (float)22.1, TableLookUp_rusle(slopeAng[c])); //x的y次幂
                    } 
                } 
        }        
        public void Calculate_CLSE_L_Feet(DemData dd, float[] slopeAng, float[] slp_lgth_ft, float[] ruslel)
        {
            int i; /*Loop Counter.*/
            int j; /*Loop Counter.*/
            int c;
            int rows; /*Number of rows.*/
            int cols; /*Number of columns.*/
            rows = dd.imagNrows;
            cols = dd.imagNcols;
            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    ruslel[c] = (float)Math.Pow(slp_lgth_ft[c] / (float)72.6, TableLookUp_csle(slopeAng[c])); //x的y次幂
                } // END for(i = 0; i < rows; i++) 
            } // END for(j = 0; j < cols, j++)  
        }
        public void Calculate_S(DemData dd, float[] downSlpAng, float[] rusles,int rusle_csle)
        {
            int i; //	Loop Counter
            int j; //	Loop Counter
            int c;
            int rows; //	Number of rows
            int cols; //	Number of columns
            float deg;
            deg = (float)57.2958; //	ASK RICK
            rows = dd.imagNrows;
            cols = dd.imagNcols;
            if(rusle_csle==1)
                for (i = 2; i < rows - 2; i++)
                {
                    for (j = 2; j < cols - 2; j++)
                    {
                        c = i * cols + j;
                        if (downSlpAng[c] >= 10)
                            rusles[c] = (float)(21.9 * (Math.Sin(downSlpAng[c] / deg)) - (float)0.96);
                        else if (downSlpAng[c] >= 5)
                            rusles[c] = (float)(16.8 * (Math.Sin(downSlpAng[c] / deg)) - (float)0.50);
                        else
                            rusles[c] = (float)(10.8 * (Math.Sin(downSlpAng[c] / deg)) + (float)0.03);
                    } 
                }
            else
                for (i = 2; i < rows - 2; i++)
                {
                    for (j = 2; j < cols - 2; j++)
                    {
                        c = i * cols + j;
                        if (downSlpAng[c] >= 5.1428)
                            rusles[c] = (float) (16.8 * (Math.Sin(downSlpAng[c] / deg)) - (float)0.50);
                        else
                            rusles[c] = (float) (10.8 * (Math.Sin(downSlpAng[c] / deg)) + (float)0.03);                      
                    }
                }

        }
        public void Calculate_LS2(DemData dd, float[] ruslel, float[] rusles, float[] ruslels2)
        {
            int i; /*Loop Counter.*/
            int j; /*Loop Counter.*/
            int c;
            int rows; /*Number of rows.*/
            int cols; /*Number of columns.*/
            rows = dd.imagNrows;
            cols = dd.imagNcols;
            for (i = 2; i < rows - 2; i++)
            {
                for (j = 2; j < cols - 2; j++)
                {
                    c = i * cols + j;
                    //ruslels2[c] = (float)((int)(ruslel[c] * rusles[c] * 100 + 0.5));
                    ruslels2[c] = (float)((int)(ruslel[c] * rusles[c] * 100 + 0.5)/100);
                }
            }
        }
        #endregion

        //##############################################



        //                    CalcLib



        //##############################################

        #region CalcLib

        public float TableLookUp_csle(float v)
        {
            double temp = 0.56;

            if (v <= 1)
                temp = 0.2;
            else if (v < 3)
                temp = 0.3;
            else if (v < 5)
                temp = 0.4;
            else
                temp = 0.5;

            return (float)temp;
        }
        float TableLookUp_rusle (float v) 
        {
	        double temp = 0.56;

	        if (v <= 0.101) 
		        temp = 0.01;
	        else if (v < 0.2) 
		        temp = 0.02;
	        else if (v < 0.4) 
		        temp = 0.04;
	        else if (v < 0.85) 
		        temp = 0.08;//原始值为0.8，应该是错误的
	        else if (v < 1.4) 
		        temp = 0.14;
	        else if (v < 2.0) 
		        temp = 0.18;
	        else if (v < 2.6) 
		        temp = 0.22;
	        else if (v < 3.1) 
		        temp = 0.25;
	        else if (v < 3.7) 
		        temp = 0.28;
	        else if (v < 5.2) 
		        temp = 0.32;
	        else if (v < 6.3) 
		        temp = 0.35;
	        else if (v < 7.4) 
		        temp = 0.37;
	        else if (v < 8.6) 
		        temp = 0.40;
	        else if (v < 10.3) 
		        temp = 0.41;
	        else if (v < 12.9) 
		        temp = 0.44;
	        else if (v < 15.7) 
		        temp = 0.47;
	        else if (v < 20.0) 
		        temp = 0.49;
	        else if (v < 25.8) 
		        temp = 0.52;
	        else if (v < 31.5) 
		        temp = 0.54;
	        else if (v < 37.2) 
		        temp = 0.55;
	        return (float)temp;
        }
        #endregion
    }
}