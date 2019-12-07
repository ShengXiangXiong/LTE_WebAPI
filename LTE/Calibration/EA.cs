using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LTE.InternalInterference;
using LTE.Geometric;
using System.Reflection; // 引用这个才能使用Missing字段 
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using System.Diagnostics; // 记录程序运行时间
using System.Data;
//70--462

namespace LTE.Calibration
{
    public class EA
    {
        public static void initEA(int popSize, int gen, int sceneNum,
                    ref Dictionary<string, List<DTInfo>> dtPwrDic1, ref Dictionary<string, TrajInfo> rayDic1, int frequence1)
        {
            POPSIZE = popSize;
            MAXGENS = gen;
            Scen = sceneNum;
            dtPwrDic = dtPwrDic1;
            rayDic = rayDic1;

            foreach (string key in dtPwrDic.Keys) {
                dtCnt += dtPwrDic[key].Count;
            }
            smaObj = (int)(dtPwrDic.Count * 0.3);

            frequence = frequence1;
        }

        #region 成员变量
        public static int frequence;
        public static int Scen = 4;     //4个场景

        public static int MAXGENS = 500;         //进化的最大代数
        public static int POPSIZE = 50;     //种群规模
        public double PXOVER0 = 0.9;        //交叉概率
        public double PMUTATION0 = 0.4;    //变异概率
        public double PXOVER = 0.9;         //交叉概率
        public double PMUTATION = 0.4;     //变异概率
        public double PSELECT = 0.9;        //选择优良个体的概率
        public static int objNum = 2;       //目标个数

        public int generation;     //进化到第几代
        public Entity Best;        //最终的最好个体

        public static Random r;

        public static int dtCnt = 0; //路测点数量
        public static int smaObj = 0;   // 局部路测点数量
        public static int scenNum = 4;     // 场景数量
        public static int coeNum = 7;      // 要校正的系数数量

        //日志根文件
        public static string basePath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase+ "..\\LTE\\Calibration\\";

        // 真实路测接收信号强度的字典
        public static Dictionary<string, List<DTInfo>> dtPwrDic;

        public static Dictionary<string, TrajInfo> rayDic;

        public static Dictionary<string, List<long>> gridsStatics;

        public static HashSet<String> highErrorGrids;//从文档中读取的误差最大的topK个栅格的key

        public static Dictionary<string, List<ErrorGridStaticInfo>> errorGridStaticInfoDic; //统计误差大的栅格点的所有信息 

        public static void initGridsStatics() {
            gridsStatics = new Dictionary<string, List<long>>();
            highErrorGrids = new HashSet<string>();
        }

        public static void initMaxErrorGridsInfoDic() {
            errorGridStaticInfoDic = new Dictionary<string, List<ErrorGridStaticInfo>>();
            errorGridStaticInfoDic.Add("[20,无穷)", new List<ErrorGridStaticInfo>());//计算值-路测值
            errorGridStaticInfoDic.Add("[15,20)", new List<ErrorGridStaticInfo>());
            errorGridStaticInfoDic.Add("[10,15)", new List<ErrorGridStaticInfo>());
            errorGridStaticInfoDic.Add("[5,10)", new List<ErrorGridStaticInfo>());
            errorGridStaticInfoDic.Add("[0,5)", new List<ErrorGridStaticInfo>());
            errorGridStaticInfoDic.Add("[-5,0)", new List<ErrorGridStaticInfo>());
            errorGridStaticInfoDic.Add("[-10,-5)", new List<ErrorGridStaticInfo>());
            errorGridStaticInfoDic.Add("[-15,-10)", new List<ErrorGridStaticInfo>());
            errorGridStaticInfoDic.Add("[-20,-15)", new List<ErrorGridStaticInfo>());
            errorGridStaticInfoDic.Add("(-20,-无穷)", new List<ErrorGridStaticInfo>());
        }

        public static double[] gaps = new double[coeNum];//差异度段

        public static int configInfoFlag = 0; //写配置信息的标志变量


        // ---------------- 配置参数 ----------------------------
        public static double degreeThreshold =4;//控制初始差异的阈值
        public static double errorTopKRatio = 0;//0.05; // 从总的栅格中删除相应占比的大误差栅格 

       
        public static void initGens(ref double[,] gen, bool isinitGaps,bool isRandom)
        {
            //// ------------------- 反射和绕射均为二次函数配置 ----------------
            ////抛物线 过点（0，1） 最低点（pai，10e-4）
            //double A = 0.10132;
            //double B = -0.62832;
            //double C = 1;
            //double A_wave_magnitude = 0.6;
            //double B_wave_magnitude = 0.6;
            //double C_wave_magnitude = 0.2;

            // ------------------- 反射和绕射均为指数配置 ----------------
            //double A_fanshe = 0;
            //double B_fanshe = 0.6366619772;// 2/pai
            //double C_fanshe = 0;
            //double A_fanshe_wave_magnitude = 0;
            //double B_fanshe_wave_magnitude = B_fanshe*2;
            //double C_fanshe_wave_magnitude = 0;

            //double A_raoshe = 0;
            //double B_raoshe = 1.27323954/2;
            //double C_raoshe = 0;
            //double A_raoshe_wave_magnitude = 0;
            //double B_raoshe_wave_magnitude = B_fanshe * 2;
            //double C_raoshe_wave_magnitude = 0;

            //只考虑直射
            //double A_fanshe = 0;
            //double B_fanshe = 0.0;// 2/pai
            //double C_fanshe = 0;
            //double A_fanshe_wave_magnitude = 0;
            //double B_fanshe_wave_magnitude = B_fanshe * 2;
            //double C_fanshe_wave_magnitude = 0;

            //double A_raoshe = 0;
            //double B_raoshe = 0;
            //double C_raoshe = 0;
            //double A_raoshe_wave_magnitude = 0;
            //double B_raoshe_wave_magnitude = B_fanshe * 2;
            //double C_raoshe_wave_magnitude = 0;

            //降到0.5，     A=−0.159154945806786,B = 1
            //降到0.1，     A=−0.2864789024522149,B=1
            //降到0.05，    A=−0.3023943970328934,B = 1
            //降到0.01,     A=−0.3151267926974363,B=1
            //    0.005，  A=−0.3167183421555042,B=1
            //    0.001，  A=−0.3179915817219585,B=1
            //    0.0005   A =−0.3181507366677653,B = 1
            //



            //double A_fanshe = 0;
            //double B_fanshe = 0.540798947;//log(2*10^(-2))/-pai   //1.17741872;//log(2*10^(-4))/-pai       2/3.141592653;// 2/pai
            //double C_fanshe = 0;
            //double A_fanshe_wave_magnitude = 0;
            //double B_fanshe_wave_magnitude = B_fanshe * 2;
            //double C_fanshe_wave_magnitude = 0;

            //double A_raoshe = 0;
            //double B_raoshe = 0.318309886; //log(2*10^(-2))/-pai    //0.954929659;//log(10*10^(-4))/-pai //2/3.141592653;
            //double C_raoshe = 0;
            //double A_raoshe_wave_magnitude = 0;
            //double B_raoshe_wave_magnitude = B_raoshe * 2;
            //double C_raoshe_wave_magnitude = 0;

            double zhisheExpectedValue = 1;
            double zhishe_wave_magnitude = 2;

            double A_fanshe = 0.10132;
            double B_fanshe = -0.62832;//log(2*10^(-2))/-pai   //1.17741872;//log(2*10^(-4))/-pai       2/3.141592653;// 2/pai
            double C_fanshe = 1;
            double A_fanshe_wave_magnitude = Math.Abs(A_fanshe) * 2;
            double B_fanshe_wave_magnitude = Math.Abs(B_fanshe) * 2;
            double C_fanshe_wave_magnitude = 0.2;

            double A_raoshe = 0.10132;
            double B_raoshe = -0.62832; //log(2*10^(-2))/-pai    //0.954929659;//log(10*10^(-4))/-pai //2/3.141592653;
            double C_raoshe = 1;
            double A_raoshe_wave_magnitude = Math.Abs(A_raoshe) * 2;
            double B_raoshe_wave_magnitude = Math.Abs(B_raoshe) * 2;
            double C_raoshe_wave_magnitude = 0.2;

           
            int segment = 5;

            if (configInfoFlag == 0) {
                configInfoFlag = 1;
                String infos = "";
                infos += " 配置参数:\n 反射 A: " + A_fanshe.ToString() + "  B: " + B_fanshe.ToString() + "  C: " + C_fanshe.ToString()+"\n";
                infos += " 参数搜索范围: A:"+ A_fanshe_wave_magnitude.ToString() + "  B: " + B_fanshe_wave_magnitude.ToString() + "  C: " + C_fanshe_wave_magnitude.ToString() + "\n";
                infos += " 绕射: A:"+ A_raoshe.ToString() + "  B: " + B_raoshe.ToString() + "  C: " + C_raoshe.ToString() + "\n";
                infos += " 参数搜索范围: A:" + A_raoshe_wave_magnitude.ToString() + "  B: " + B_raoshe_wave_magnitude.ToString() + "  C: " + C_raoshe_wave_magnitude.ToString() + "\n";

                string path = basePath+@"galog.txt";
                StreamWriter sw = new StreamWriter(path, true, Encoding.Default);
                sw.WriteLine(infos);
                sw.Close();
            }

            if (isinitGaps) {     
                for (int j = 0; j < coeNum; j++)
                {
                    switch (j)
                    {
                        case 0:
                            gaps[j] = zhishe_wave_magnitude / segment;
                            break;
                        case 1:
                            gaps[j] = A_fanshe_wave_magnitude / segment;
                            break;
                        case 2:
                            gaps[j] = B_fanshe_wave_magnitude / segment;
                            break;
                        case 3:
                            gaps[j] = C_fanshe_wave_magnitude / segment;
                            break;
                        case 4:
                            gaps[j] = A_raoshe_wave_magnitude / segment;
                            break;
                        case 5:
                            gaps[j] = B_raoshe_wave_magnitude / segment;
                            break;
                        case 6:
                            gaps[j] = C_raoshe_wave_magnitude / segment;
                            break;

                        /* 您可以有任意数量的 case 语句 */
                        default: /* 可选的 */
                            break;
                    }
                }
            }


            for (int i = 0; i < scenNum; i++)
            {             
                for (int k = 0; k < coeNum; k++)
                {
                    int j;
                    if (isRandom)
                    {
                        j = ThreadSafeRandom.Next(0, coeNum);

                        if (r.NextDouble() < 0.4)
                        {//以一定概率不变这个系数
                            continue;
                        }
                    }
                    else {
                        j = k;
                    }

                    switch (j)
                    {

                        case 0:
                            gen[i, j] = zhisheExpectedValue + r.NextDouble() * zhishe_wave_magnitude- zhishe_wave_magnitude/2;
                            break;
                        case 1:
                            gen[i, j] = A_fanshe + r.NextDouble() * A_fanshe_wave_magnitude - A_fanshe_wave_magnitude / 2; //A必须>0
                            break;
                        case 2:
                            gen[i, j] = B_fanshe + r.NextDouble() * B_fanshe_wave_magnitude - B_fanshe_wave_magnitude / 2;
                            break;
                        case 3:
                            gen[i, j] = C_fanshe + r.NextDouble() * C_fanshe_wave_magnitude - C_fanshe_wave_magnitude / 2;
                            break;
                        case 4:
                            gen[i, j] = A_raoshe + r.NextDouble() * A_raoshe_wave_magnitude - A_raoshe_wave_magnitude / 2; //A必须>0
                            break;
                        case 5:
                            gen[i, j] = B_raoshe + r.NextDouble() * B_raoshe_wave_magnitude - B_raoshe_wave_magnitude / 2;
                            break;
                        case 6:
                            gen[i, j] = C_raoshe + r.NextDouble() * C_raoshe_wave_magnitude - C_raoshe_wave_magnitude / 2;
                            break;

                        /* 您可以有任意数量的 case 语句 */
                        default: /* 可选的 */
                            break;
                    }

                }            
            }

        }


        public static Dictionary<string, double> GridsOfSpecialType 
                                                 = new Dictionary<string, double>(); // 存储特定轨迹类型的栅格集合和预测误差



        public class Entity
        {
            public double BigRes;
            public double SmaRes;

            public int index;            // 下标
            public double[,] gen;   //一个染色方案
            public double[] fitnessVec;     // 局部目标和整体目标
            public double fitness;       // 通过AHP，最后从优胜集中选出一个最优解

            public static Object objUniqueLock = new Object();

            #region 差异度初始化
            public static List<double[,]> initGens = new List<double[,]>();// 存储一份全部的初始化基因个体
           
            

            /// <summary>
            /// 返回当前个体和idx对应个体的差异度的值
            /// </summary>
            /// <param name="A">当前个体 系数数组</param>
            /// <param name="idx">另一个个体idx</param>
            /// <returns></returns>
            public static double compareDegree(ref double[,] A, int idx)
            {
                double[,] B = initGens[idx];
                double sum = 0;
                for (int i = 0; i < scenNum; i++)
                {
                    for (int j = 0; j < coeNum; j++)
                    {

                        sum += (int)(Math.Abs(A[i, j] - B[i, j]) / gaps[j]);
                    }

                }

                return sum;
            }

            //判断初始化时一个个体是否和其他所有已生成个体差异度大于阈值
            public static bool isValid(ref double[,] gen)
            {

                double degreeThreshold = EA.degreeThreshold; //大于个体基因所有参数的差异度阈值 才算有效的初始化个体基因

                double degree = 0;

                for (int i = 0; i < initGens.Count; i++)
                {

                    degree = compareDegree(ref gen, i);
                    if (degree < degreeThreshold)
                        return false;
                }


                return true;
            }

            // 控制差异度的初始化方式
            public void entityDiversityInit()
            {
                gen = new double[scenNum, coeNum];


                int maxLoopTimes = 50;//初始化时，最多寻找初始化系数的次数
                int loopTime = 0;//循环次数
                while (true)
                {
                    EA.initGens(ref gen, true,false);


                    // 检查这个个体初始化系数是否符合差异度要求
                    loopTime++;
                    if (isValid(ref gen) || loopTime >= maxLoopTimes)
                    {
                        initGens.Add(gen);
                        break;
                    }

                }


                fitnessVec = new double[objNum];
                for (int i = 0; i < objNum; i++)
                    fitnessVec[i] = 0;
            }
            #endregion 


            public Entity()
            {
                gen = new double[scenNum, coeNum];

                EA.initGens(ref gen, false,false);

                fitnessVec = new double[objNum];
                for (int i = 0; i < objNum; i++)
                    fitnessVec[i] = 0;
            }


            //重写了判断相等的方法，用于选择过程判断集合是否已包含该元素
            public override bool Equals(object obj)
            {
                double epison = 0.000001;

                Entity e = obj as Entity;
                for (int i = 0; i < objNum; i++)
                    if (Math.Abs(this.fitnessVec[i] - e.fitnessVec[i]) > epison) {
                        return false;
                    }
                return true;
            }

            public void setParam(double[,] newGen)
            {
                gen = newGen;
            }

            public bool EqualsEntity(object obj)
            {
                double epison = 0.000001;

                Entity e = obj as Entity;
                for (int k = 0; k < Scen; k++)
                {
                    for (int j = 0; j < coeNum; j++)
                    {
                        if (Math.Abs(this.gen[k, j] - e.gen[k, j]) > epison) {
                            return false;
                        }
                    }
                }
                return true;
            }

            //获取适应度
            public void getFit() {
                getFitByAllDTPoints();
            }


            //计算均方误差
            public void getFitByAllDTPoints() {

                // ------------ 栅格统计相关 --------------
                double errThreshold = 20;

                //计算均方误差
                double sum = 0; //目标的误差和
                int dtCnt = 0;
                foreach (string key in dtPwrDic.Keys)
                {
                    
                    //跳过栅格误差大的点 不计算入误差
                    if (highErrorGrids.Contains(key)) {
                        continue;
                    }

                    //如果不是所选轨迹类型就不计算入误差
                    if (!EA.GridsOfSpecialType.ContainsKey(key)) {
                        continue;
                    }

                    ErrorGridStaticInfo errorGridStaticInfo = null;
                    double   recePwr = rayDic[key].calcWithNewFormula(ref this.gen, 4, frequence,ref errorGridStaticInfo); //单位为dbm

                    foreach (DTInfo dtInfo in dtPwrDic[key]) //dtPwr单位为dbm
                    {
                        //加锁解决并发出现NaN的问题
                        lock (objUniqueLock)
                        {
                            if (!double.IsNaN(recePwr))
                            {
                                sum += Math.Pow((recePwr - dtInfo.pwrDbm), 2);
                                dtCnt++;
                            }
                        }


                        double err = Math.Abs((recePwr - dtInfo.pwrDbm));

                        List<long> tmpList = new List<long>(); // 确定默认为0 ??
                        tmpList.Add(0);

                        if ( EA.gridsStatics.ContainsKey(key) == false) {
                            
                            EA.gridsStatics[key] = tmpList;
                        }

                        if (err >= errThreshold) {

                            EA.gridsStatics[key][0] += 1;

                            if (EA.gridsStatics[key][0] > 1) {
                                string keytemp = key;
                            }
                        }



                    }
                }

                //调试用
                if (dtCnt == 0) {
                    int test = 0;
                }

                fitnessVec[1] = Math.Sqrt(sum / dtCnt);  // 整体，单位为dbm
                fitnessVec[0] = fitnessVec[1];// 暂时将局部也设置为整体
            }

            //打印指定小区的轨迹信息到文件
            public void writeTrajInfoToFile(HashSet<string> keySet)
            {
                foreach (string key in keySet)
                {
                    double tempDt = dtPwrDic[key][0].pwrDbm;
                    //暂时调用calc_Max代替打印轨迹的方法
                    String outPath = basePath+@"TopKhighErrorGridsInfo.txt"; ;
                    double recePwr = rayDic[key].calc_Max(ref this.gen, 4, frequence, tempDt, key,outPath); //单位为dbm
                }
            }


            //用于统计大误差点栅格信息到结构体，jin 
            public void getFitByAllDTPointsForSingle()
            {
                //计算均方误差
                double sum = 0; //目标的误差和
                int dtCnt = 0;
                foreach (string key in dtPwrDic.Keys)
                {

                    //跳过栅格误差大的点 不计算入误差
                    if (highErrorGrids.Contains(key))
                    {
                        continue;
                    }

                    //如果不是所选轨迹类型就不计算入误差
                    if (!EA.GridsOfSpecialType.ContainsKey(key))
                    {
                        continue;
                    }

                    ErrorGridStaticInfo errorGridStaticInfo = new ErrorGridStaticInfo();
                    double recePwr = rayDic[key].calcWithNewFormula(ref this.gen, 4, frequence,ref errorGridStaticInfo); //单位为dbm

                    //调试用
                    if (dtPwrDic[key].Count != 1) {//查看一个栅格是否有不是只一个路测点的情况
                        int errPoint = 1;
                    }

                    foreach (DTInfo dtInfo in dtPwrDic[key]) //dtPwr单位为dbm
                    {
                        //加锁解决并发出现NaN的问题
                        lock (objUniqueLock)
                        {
                            if (!double.IsNaN(recePwr))
                            {
                                sum += Math.Pow((recePwr - dtInfo.pwrDbm), 2);
                                dtCnt++;

                                //加入写入栅格误差信息结构体的逻辑，jin
                                double gridError = recePwr - dtInfo.pwrDbm;
                                errorGridStaticInfo.setInfo(key,gridError,recePwr,dtInfo.pwrDbm);

                                if (gridError >= 20)
                                {
                                    errorGridStaticInfoDic["[20,无穷)"].Add(errorGridStaticInfo);
                                }
                                else if (gridError >= 15 && gridError < 20)
                                {
                                    errorGridStaticInfoDic["[15,20)"].Add(errorGridStaticInfo);
                                }
                                else if (gridError >= 10 && gridError < 15)
                                {
                                    errorGridStaticInfoDic["[10,15)"].Add(errorGridStaticInfo);
                                }
                                else if (gridError >= 5 && gridError < 10)
                                {
                                    errorGridStaticInfoDic["[5,10)"].Add(errorGridStaticInfo);
                                }
                                else if (gridError >= 0 && gridError < 5)
                                {
                                    errorGridStaticInfoDic["[0,5)"].Add(errorGridStaticInfo);
                                }
                                else if (gridError >= -5 && gridError < 0)
                                {
                                    errorGridStaticInfoDic["[-5,0)"].Add(errorGridStaticInfo);
                                }
                                else if (gridError >= -10 && gridError < -5)
                                {
                                    errorGridStaticInfoDic["[-10,-5)"].Add(errorGridStaticInfo);
                                }
                                else if (gridError >= -15 && gridError < -10)
                                {
                                    errorGridStaticInfoDic["[-15,-10)"].Add(errorGridStaticInfo);
                                }
                                else if (gridError >= -20 && gridError < -15)
                                {
                                    errorGridStaticInfoDic["[-20,-15)"].Add(errorGridStaticInfo);
                                }
                                else if(gridError<-20)
                                {
                                    errorGridStaticInfoDic["(-20,-无穷)"].Add(errorGridStaticInfo);
                                }
                            }
                        }
                    }
                }
            }

            //用于统计最小二乘法的矩阵信息，jin
            public void getFitByAllDTPointsForSingleToMatrix(ref double[,] X,ref double[] Y) {
                int i = 0;
                foreach (string key in dtPwrDic.Keys) {
                    //跳过栅格误差大的点 不计算入误差
                    if (highErrorGrids.Contains(key))
                    {
                        continue;
                    }

                    //如果不是所选轨迹类型就不计算入误差
                    if (!EA.GridsOfSpecialType.ContainsKey(key))
                    {
                        continue;
                    }

                    //获得key对应栅格路测值
                    if (dtPwrDic[key].Count > 1) {
                        string testInfo = "栅格中路测点数量超过一个";
                    }
                    double dtPwrDbm = dtPwrDic[key][0].pwrDbm;//路测值

                    //获得key对应的其他信息
                    double[] W=new double[4];
                    double y = -1;
                    rayDic[key].calcWithNewFormulaToMatrix(ref this.gen, 4, frequence,dtPwrDbm,ref W,ref y);
                    for (int j = 0; j < 4; ++j) {
                        X[i,j] = W[j];
                        Y[i] = y;
                    }
                    ++i;
                } 
                
                
                
                //计算均方误差
                    double sum = 0; //目标的误差和
                    int dtCnt = 0;
                foreach (string key in dtPwrDic.Keys)
                {


                    ErrorGridStaticInfo errorGridStaticInfo = new ErrorGridStaticInfo();
                    double recePwr = rayDic[key].calcWithNewFormula(ref this.gen, 4, frequence, ref errorGridStaticInfo); //单位为dbm

                    //调试用
                    if (dtPwrDic[key].Count != 1)
                    {//查看一个栅格是否有不是只一个路测点的情况
                        int errPoint = 1;
                    }

                    foreach (DTInfo dtInfo in dtPwrDic[key]) //dtPwr单位为dbm
                    {
                        //加锁解决并发出现NaN的问题
                        lock (objUniqueLock)
                        {
                            if (!double.IsNaN(recePwr))
                            {
                                sum += Math.Pow((recePwr - dtInfo.pwrDbm), 2);
                                dtCnt++;

                                //加入写入栅格误差信息结构体的逻辑，jin
                                double gridError = recePwr - dtInfo.pwrDbm;
                                errorGridStaticInfo.setInfo(key, gridError, recePwr, dtInfo.pwrDbm);

                                if (gridError >= 20)
                                {
                                    errorGridStaticInfoDic["[20,无穷)"].Add(errorGridStaticInfo);
                                }
                                
                            }
                        }
                    }
                }
            }

            //计算均方误差 特殊 单次调用使用
            public int[] getFitForSingle()
            {

                // ------------ 栅格统计相关 --------------
                double errThreshold = 20;
                int binNum = 10; //约定第0位存储大于errThreshold的次数

                //计算均方误差
                double sum = 0; //目标的误差和
                int dtCnt = 0;

                int receHeigher = 0;//计算值更大
                int dtHeigher = 0;//计算值更小
                int[] receHeighOrLow = new int[2];//默认0为计算值更大，1为计算值更小

                foreach (string key in dtPwrDic.Keys)
                {
                    //跳过栅格误差大的点 不计算入误差
                    if (highErrorGrids.Contains(key))
                    {
                        continue;
                    }

                    //暂时调用calc_Max
                    double tempDt = dtPwrDic[key][0].pwrDbm;
                    String outPath = basePath + @"result.GuiJiTongJi.txt";
                    double recePwr = rayDic[key].calc_Max(ref this.gen, 4, frequence,tempDt,key,outPath); //单位为dbm

                    foreach (DTInfo dtInfo in dtPwrDic[key]) //dtPwr单位为dbm
                    {
                        if (recePwr > dtInfo.pwrDbm) {
                            receHeigher += 1;
                        }
                        else {
                            dtHeigher += 1;
                        }
                    }
                }

                receHeighOrLow[0] = receHeigher;
                receHeighOrLow[1] = dtHeigher;
                return receHeighOrLow;
            }
    }


        List<Entity> population;     //种群
        List<Entity> newpopulation;  //新种群
        Pareto pareto;            //非支配解集
        Pareto newpareto;         //新的非支配解集
        Pareto bestPareto;        //最终非支配解集
        List<Entity> q;

        #endregion

        #region  //计算交叉概率
        public double PCross()
        {
            double pc = PXOVER0 * Math.Exp(-0.41 * generation / MAXGENS);
            return pc;
        }
        #endregion

        #region//计算变异概率
        double PMutate()
        {
            double pm = 0;
            if (generation <= 50)
                pm = PMUTATION0 * Math.Exp(0.4 * generation / MAXGENS);
            else
                pm = PMUTATION0 * Math.Exp(0.51 * generation / MAXGENS);
            //double pm = PMUTATION0 * Math.Exp(0.69 * generation / MAXGENS);
            return pm;
        }
        #endregion

        #region/////////////////////////////////////////遗传算法////////////////////////////////////////////////


        void swap(ref int p, ref int q)
        {
            int tmp = p;
            p = q;
            q = tmp;
        }

        //种群初始化
        void init()
        {
            r = new Random();
            population = new List<Entity>();     //种群
            newpopulation = new List<Entity>();  //新种群
            pareto = new Pareto(POPSIZE, scenNum, objNum);
            newpareto = new Pareto(POPSIZE, scenNum, objNum);
            bestPareto = new Pareto(POPSIZE, scenNum, objNum);
            q = new List<Entity>();

            int i;
            for (i = 0; i < POPSIZE; i++)
            {
                Entity E = new Entity();
                E.index = i;
               

                Entity newE = new Entity();
                newE.index = i;

                population.Add(E);
                newpopulation.Add(newE);
            }

            Entity.initGens.Clear();
            //初始化时，在基础初始化后，再进行差异度初始化，更改gen保证差异度（只进行一次）
            for (i = 0; i < POPSIZE; i++) {
                population[i].entityDiversityInit();             
            }
        }

        //评价函数
        void evaluate()
        {
            for (int i = 0; i < POPSIZE; i++)
                population[i].getFit();

            PXOVER = PCross();
            PMUTATION = PMutate();
        }

        //保存遗传后的非支配解集
        void keep_the_best()
        {
            pareto.NDSet.Clear();
            List<Entity> sortPop = new List<Entity>(); //排好序后的种群
            copy(ref sortPop, ref population);

            int countCalls = 0;
            pareto.quickSort(sortPop, 0, sortPop.Count - 1, countCalls);      //得到非支配解集
            //  改为二次函数后，quickSort出现无限循环的问题，暂时替换为quickSort1！！！todo
            //pareto.quickSort1(sortPop, 0, sortPop.Count - 1);  //得到当代的非支配解集

            if (pareto.NDSet.Count > pareto.maxParetoSize)    //非支配解集过大
            {
                pareto.inGrid();       //将非支配解集放入网格中
                pareto.controlNum();   //用网格法控制非支配解数量，删除多余非支配解
            }
        }

        private static readonly Object mutex = new object();

        // 交叉函数：选择两个个体交叉
        void crossover()
        {
            int one = 0;
            int first = 0;
            Parallel.For(0, (int)(POPSIZE * 0.1), mem =>
              {
                  double x = ThreadSafeRandom.NextDouble();
                  if (x < PXOVER)
                  {
                      lock (mutex)
                      {
                          ++first;
                      }
                      if (first % 2 == 0)   //mem与one交叉
                      {
                          Xover(one, mem);
                      }
                      else
                      {
                          lock (mutex)
                          {
                              one = mem;
                          }
                      }
                  }
              });

            PXOVER = PCross();
        }

        // 非并行，解决两次计算cal结果不同的问题。交叉函数：选择两个个体交叉
        void crossoverNoParallel()
        {
            int one = 0;
            int first = 0;
            for(int mem=0;mem< (int)(POPSIZE * 0.1);++mem)
            {
                double x = ThreadSafeRandom.NextDouble();
                if (x < PXOVER)
                {
                    lock (mutex)
                    {
                        ++first;
                    }
                    if (first % 2 == 0)   //mem与one交叉
                    {
                        Xover(one, mem);
                    }
                    else
                    {
                        lock (mutex)
                        {
                            one = mem;
                        }
                    }
                }
            };

            PXOVER = PCross();
        }

        void crossoverNew() {
            int i = 0;
            for (i = 0; i < POPSIZE * 0.1; ++i) {
                double x = ThreadSafeRandom.NextDouble();
                if (x < PXOVER) {
                    Xover(i, (int)(x + ThreadSafeRandom.Next(100, 200) * 0.01));
                }
            }

            int one = 0;
            int first = 0;
            for (int j = (int)(POPSIZE * 0.2); j < POPSIZE; ++j) {
                double x = ThreadSafeRandom.NextDouble();
                if (x < PXOVER)
                {
                    ++first;
                    if (first % 2 == 0)   //i与one交叉
                    {
                        Xover(one, j);
                    }
                    else
                    {
                        one = j;
                    }
                }
            }

            PXOVER = PCross();
        }

        //交叉   
        void Xover(int one, int two)
        {
            int i;
            Entity X = new Entity();

            for (int j = 0; j < Scen; j++)
            {
                for (i = 0; i < coeNum; i++)
                {
                    X.gen[j, i] = population[one].gen[j, i];
                }

                int k = r.Next(0, coeNum);
                X.gen[j, k] = population[two].gen[j, k];
            }

            X.getFit();

            lock (q)
            {
                q.Add(X);
            }
        }

        // 变异函数，随机选择某一个体
        void mutate()
        {
            Parallel.For(0, POPSIZE, i =>
            //for(int i=0;i<POPSIZE;i++)
            {
                lock (population[i])
                {
                    double r1 = ThreadSafeRandom.NextDouble();  //随机选择变异个体
                    Entity E = new Entity();
                    Entity S = population[i];
                    copy(ref E, ref S);

                    if (r1 < PMUTATION)   //变异
                    {
                        EA.initGens(ref E.gen, false, true);


                        E.getFit();
                        lock (q)
                        {
                            q.Add(E);
                        }
                    }
                }
            });

            PMUTATION = PMutate();
        }

        void copy(ref Entity D, Entity S)
        {
            for (int j = 0; j < objNum; j++)
                D.fitnessVec[j] = S.fitnessVec[j];
            for (int i = 0; i < Scen; i++)
            {
                for (int j = 0; j < coeNum; j++)
                {
                    D.gen[i, j] = S.gen[i, j];
                }
            }
        }
        void copy(ref Entity D, ref Entity S)
        {
            for (int j = 0; j < objNum; j++)
                D.fitnessVec[j] = S.fitnessVec[j];
            for (int i = 0; i < Scen; i++)
            {
                for (int j = 0; j < coeNum; j++)
                {
                    D.gen[i, j] = S.gen[i, j];
                }
            }
        }
        //选择函数，保证优秀的个体得以生存 
        void select()
        {
            for (int i = 0; i < POPSIZE; i++)
            {
                Entity E = new Entity();
                Entity S = population[i];
                copy(ref E, ref S);
                q.Add(E);
            }
            int testbef = 1;
            pareto.quickSort1(q, 0, q.Count - 1);
            int testafter = 2;
            //i = 0;
            //int k = 0;
            //while (i < POPSIZE && k < q.Count)
            //{
            //    Entity node = q[k++];
            //    double p = r.NextDouble();
            //    if (p < PSELECT)
            //    {
            //        population[i++] = node;
            //    }
            //}

            population.Clear();
            for (int i = 0; i < POPSIZE; ++i) {
                Entity tempNode = new Entity();
                copy(ref tempNode, q[i]);
                population.Add(tempNode);
            }
            //清空集合
            q.Clear();
        }

        void copy(ref List<Entity> D, ref List<Entity> S)
        {
            for (int i = 0; i < S.Count; i++)
            {
                Entity E = new Entity();
                E.index = i;
                for (int j = 0; j < objNum; j++)
                    E.fitnessVec[j] = S[i].fitnessVec[j];
                for (int k = 0; k < Scen; k++)
                {
                    for (int j = 0; j < coeNum; j++)
                    {
                        E.gen[k, j] = S[i].gen[k, j];
                    }
                }
                D.Add(E);
            }
        }

        //更新非支配集合，去掉与上一代非支配集比较
        void elitistNew() {
            List<Entity> newq = new List<Entity>();
            newpareto.NDSet.Clear();
            copy(ref newq, ref population);


            //改为二次函数后，quickSort出现无限循环的问题，暂时替换为quickSort1！！！todo
            newpareto.quickSort1(newq, 0, newq.Count - 1);  //得到当代的非支配解集

            //将得到的最终非支配解集复制回pareto
            pareto.NDSet.Clear();

            //单目标使用
            pareto.NDSet.Add(newq[0]);
            //多目标使用
            //for (int i = 0; i < newpareto.NDSet.Count; i++)
            //    pareto.NDSet.Add(newpareto.NDSet[i]);

            ////待验证正确性，单目标先注释掉
            //if (pareto.NDSet.Count > pareto.maxParetoSize)    //非支配解集过大
            //{
            //    pareto.inGrid();       //将非支配解集放入网格中
            //    pareto.controlNum();   //用网格法控制非支配解数量，删除多余非支配解
            //}
        }

        //更新非支配解集
        //找出当代中的非支配解，与上一代非支配解集进行比较
        //如果当代非支配解比前一代非支配解差，后者将取代当代最坏个体
        void elitist()
        {
            //List<Entity> p = population;
            List<Entity> newq = new List<Entity>();
            List<Entity> sortPop = new List<Entity>();
            copy(ref newq, ref population);
            copy(ref sortPop, ref population);
            newpareto.NDSet.Clear();

            int countCalls = 0;
            newpareto.quickSort(newq, 0, newq.Count - 1, countCalls );  //得到当代的非支配解集
            //  改为二次函数后，quickSort出现无限循环的问题，暂时替换为quickSort1！！！todo
            //newpareto.quickSort1(newq, 0, newq.Count - 1);  //得到当代的非支配解集

            pareto.quickSort1(sortPop, 0, sortPop.Count - 1);  //对所有个体排序
            Entity NDS = new Entity();
            copy(ref NDS, sortPop[0]);
            List<Entity> add = new List<Entity>();
            int index = 0;
            for (int i = 0; i < newpareto.NDSet.Count; i++)
            {
                for (int j = 0; j < pareto.NDSet.Count; j++)
                {
                    if (pareto.NDSet[j].index != -1)
                    {
                        int flag = pareto.domain(newpareto.NDSet[i], pareto.NDSet[j]);
                        if (flag == -1)  //前一代的最好个体更好
                        {
                            population[sortPop[POPSIZE - index - 1].index] = pareto.NDSet[j];  //前一代的最好个体替换当代最坏个体
                            index++;
                            break;
                        }
                        else if (flag == 1)  //当代最好个体更好
                        {
                            //将前一代非支配集中所有被该个体支配的个体删除
                            for (int k = 0; k < pareto.NDSet.Count; k++)
                                if (pareto.NDSet[k].index != -1 && pareto.domain(newpareto.NDSet[i], pareto.NDSet[k]) == 1)
                                    pareto.NDSet[k].index = -1;
                            //将该个体加入非支配集中
                            add.Add(newpareto.NDSet[i]);
                            break;
                        }
                        else if (flag != -1 && j == pareto.NDSet.Count - 1)
                            add.Add(newpareto.NDSet[i]);
                    }
                }
            }

            bestPareto.NDSet.Clear();
            for (int i = 0; i < pareto.NDSet.Count; i++)
                if (pareto.NDSet[i].index != -1)
                    bestPareto.NDSet.Add(pareto.NDSet[i]);

            //将得到的最终非支配解集复制回pareto
            pareto.NDSet.Clear();
            for (int i = 0; i < bestPareto.NDSet.Count; i++)
                pareto.NDSet.Add(bestPareto.NDSet[i]);
            for (int i = 0; i < add.Count; i++)
                pareto.NDSet.Add(add[i]);
            if (pareto.NDSet.Count > pareto.maxParetoSize)    //非支配解集过大
            {
                pareto.inGrid();       //将非支配解集放入网格中
                pareto.controlNum();   //用网格法控制非支配解数量，删除多余非支配解
            }

            if (pareto.NDSet.Count == 0)  //如果解集中没有非支配解
            {
                pareto.NDSet.Add(NDS);  //将排好序的第一个个体加入
            }
        }

        public static double convertw2dbm(double w)
        {
            return 10 * (Math.Log10(w) + 3);
        }

        StreamWriter sw;
        StreamWriter swAvg;
        StreamWriter pointLogFile;

        //报告模拟进展情况
        void report(double elapsedTime)
        {
            string path = basePath + @"galog.txt";
            StreamWriter swLocal = new StreamWriter(path, true, Encoding.Default);
            swLocal.WriteLine(generation);
            swLocal.WriteLine("本轮时间：" + elapsedTime.ToString());

            try
            {
               
                double[] avg = new double[objNum];
                for (int i = 0; i < pareto.NDSet.Count; i++)
                {
                    for (int j = 0; j < scenNum; j++)
                    {
                        String tmpStr = "";
                        for (int k = 0; k < coeNum; k++)
                        {
                            tmpStr+= pareto.NDSet[i].gen[j, k].ToString()+"    ";
                        }
                        swLocal.WriteLine(tmpStr);
                    }
                    for (int j = 0; j < objNum; j++)
                    {
                        swLocal.Write(pareto.NDSet[i].fitnessVec[j] + "\t");
                        avg[j] += pareto.NDSet[i].fitnessVec[j];
                    }
                    swLocal.WriteLine();
                };
                swLocal.WriteLine();
                swAvg.Write(generation + "\t");
                for (int j = 0; j < objNum; j++)
                {
                    avg[j] /= pareto.NDSet.Count;
                    swAvg.Write(avg[j] + "\t");
                }
                swAvg.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }

            swLocal.Close();
        }

        class GridPwr
        {
            string cellId;
            double pwrDbm;

            public GridPwr(string ci, double p)
            {
                cellId = ci;
                pwrDbm = p;
            }
        };

        //将局部误差与总体误差写入文件
        private void writeErrorToFile(StreamWriter resultFile, double[] fitnessVec) {
            resultFile.WriteLine("局部误差与总体误差：");
            for (int i = 0; i < objNum; i++)
            {
                double error = fitnessVec[i];
                //double errorW = ConvertUtil.convertw2dbm(error);
                resultFile.Write(error.ToString("0.00") + "\t");
                //result.Write(errorW.ToString("0.00") + "\t");
            }
        }

        //将errorTopK误差大errorTopK个栅格的key放入highErrorGrids
        private HashSet<String> getTopHighErrorKey(String gridsStaticsPath, int errorTopK) {
            HashSet<String> gridsSet = new HashSet<string>();
            StreamReader sr1 = new StreamReader(gridsStaticsPath, Encoding.Default);
            String line; 
            int lineNum = 0;
            while ((line = sr1.ReadLine()) != null && lineNum < errorTopK)
            {  

                String[] list = line.Split();
                gridsSet.Add(list[1]);
                lineNum++;
            }
            sr1.Close();

            return gridsSet;
        }

        public void GaMain(int runTypeTag)
        {
            Console.WriteLine("start gamain.");

            //计时
            Stopwatch swTime = new Stopwatch();
            swTime.Start();

            //本此跑的标记信息
            String infoStr = "1个小区 去掉误差大于20点 发射功率32 下倾角已修正 ";

            // -- 记录栅格轨迹在优化整个过程中的误差信息 -----------------
            String gridsStaticsPath = basePath + "GridsStaticsFirst.txt";

            EA.initGridsStatics();  // 全局初始化统计词典

            EA.initMaxErrorGridsInfoDic();//全局初始化误差大的点的栅格所有信息，jin

            //runTypeTag==1,表示去掉误差大的栅格点
            //将误差大的点放入highErrorGrids，以便后面处理；更改gridsStaticsPath路径，防止第二遍覆盖第一遍
            if (runTypeTag == 1)
            {
                
                int errorTopK = (int)(EA.errorTopKRatio*EA.GridsOfSpecialType.Count);//删掉误差最大的errorTopK个栅格
                HashSet<String> highErrorTmpSet=getTopHighErrorKey(gridsStaticsPath, errorTopK);
                highErrorGrids = new HashSet<string>(highErrorTmpSet);

                gridsStaticsPath = basePath + @"GridsStaticsSecond.txt";
            }


            StreamWriter sw1 = new StreamWriter(gridsStaticsPath, false, Encoding.Default);//误差排序文件使用覆盖模式，以便读取到最新排序结果


            //记录系数校正最终结果
            string path1 = basePath + @"result.txt";
            StreamWriter result = new StreamWriter(path1, true, Encoding.Default);
            result.WriteLine("============"+ infoStr+" ======================");

            //计算误差，并记录栅格、路测点、计算值信息。
            string pointLogPath = basePath + @"pointLog.txt";
            pointLogFile = File.CreateText(pointLogPath);
            pointLogFile.WriteLine("CellID，gxid，gyid 路测点真实值 计算值：");

            //计算初始误差
            double sum = 0; //目标的误差和
            int dtCnt = 0;
            int elemCnt = 0;
            foreach (string key in dtPwrDic.Keys)
            {
                //跳过栅格误差大的点 不计算入误差
                if (highErrorGrids.Contains(key))
                {
                    continue;
                }

                double recePwr = rayDic[key].sumPwrDbm;

                elemCnt = 0;
                foreach (DTInfo dtInfo in dtPwrDic[key]) //dtPwr单位为dbm
                {
                    sum += Math.Pow((recePwr - dtInfo.pwrDbm), 2);
                    dtCnt++;
                    elemCnt++;

                    pointLogFile.WriteLine(key + "\t" + dtInfo.pwrDbm.ToString("0.00") + recePwr.ToString("0.00"));
                }

                if (elemCnt > 1) {

                }
            }
            pointLogFile.Close();

            double err1 = Math.Sqrt(sum / dtCnt);  // 整体，单位为dbm
            double err2 = err1;// 暂时将局部也设置为整体

            #region//计算标准差    
            //计算标准差
            double stanDevDT = 0;//标准差
            double stanDevAdj = 0;
            double stanDevDiffer = 0;
            int k = 0;
            //初始化，计算标准差用
            double sumDT = 0;
            double sumAdj = 0;
            double sumDiffer = 0;

            int cnt = 0;
            foreach (string key in dtPwrDic.Keys)
            {
                double recePwr = rayDic[key].sumPwrDbm;

                foreach (DTInfo dtInfo in dtPwrDic[key])
                {
                    double dtPwrW = ConvertUtil.convertdbm2w(dtInfo.pwrDbm);

                    //为计算标准差的准备
                    sumDT += dtInfo.pwrDbm;
                    sumAdj += recePwr;
                    sumDiffer += recePwr - dtInfo.pwrDbm;

                    cnt++;
                }
            }
            double avgDT = sumDT / cnt;//均值
            double avgDiffer = sumDiffer / cnt;
            double avgAdj = sumAdj / cnt;

            foreach (string key in dtPwrDic.Keys)
            {
                foreach (DTInfo dtInfo in dtPwrDic[key])
                {
                    //为计算标准差的准备
                    stanDevDT += Math.Pow((dtInfo.pwrDbm - avgDT), 2);
                    stanDevAdj += Math.Pow((rayDic[key].sumPwrDbm - avgAdj), 2);
                    stanDevDiffer += Math.Pow((rayDic[key].sumPwrDbm - dtInfo.pwrDbm - avgDiffer), 2);

                    k++;
                }
            }
            stanDevDT = Math.Sqrt(stanDevDT / (k - 1));
            stanDevDiffer = Math.Sqrt(stanDevDiffer / (k - 1));
            stanDevAdj = Math.Sqrt(stanDevAdj / (k - 1));
            #endregion

            //写入文件
            result.WriteLine("初始局部误差与总体误差：");
            result.WriteLine(err1.ToString("0.00") + "\t" + err2.ToString("0.00"));
            //result.WriteLine(errW1.ToString("0.00") + "\t" + errW2.ToString("0.00"));
            result.WriteLine();

            //写入标准差
            result.WriteLine("初始路测、计算值、差值的标准差：");
            result.WriteLine(stanDevDT.ToString("0.00") + "\t" + stanDevAdj.ToString("0.00") + "\t" + stanDevDiffer.ToString("0.00"));
            result.WriteLine();

            string currentDirectory = System.Environment.CurrentDirectory;
            string currentTime = DateTime.Now.ToString();

            //记录每次迭代的系数和误差
            string path = basePath+@"galog.txt";
            string path_avg = basePath + @"galog_avg.txt";
            sw = new StreamWriter(path, true, Encoding.Default);
            sw.WriteLine("============" + infoStr + " ======================");
            sw.Close();
            swAvg = new StreamWriter(path_avg, true, Encoding.Default);
            swAvg.WriteLine("============" + infoStr + " ======================");

            generation = 0;
            init();
            evaluate();         //评价函数，可以由用户自定义，该函数取得每个基因的适应度
            keep_the_best();    //保存每次遗传后的最佳基因


            //计时开始
           
            while (generation < MAXGENS)
            {
                Stopwatch reportTime = new Stopwatch();
                reportTime.Start();


                generation++;
                
                select();     //选择函数：用于最大化合并杰出模型的标准比例选择，保证最优秀的个体得以生存
                crossover();  //杂交函数：选择两个个体来杂交，这里用单点杂交 
                              //crossoverNew(); //新的杂交函数，改变杂交策略
                mutate();     //变异函数：被该函数选中后会使得某一变量被一个随机的值所取代 

                //计时结束
                reportTime.Stop();
                double elapsedTime = reportTime.ElapsedMilliseconds;//毫秒               

                
                elitistNew(); //去掉两轮精英集的比较，jin

                report(elapsedTime);     //报告模拟进展情况
                                         //evaluate();   //评价函数，可以由用户自定义，该函数取得每个基因的适应度
                                         //elitist();    //搜寻杰出个体函数：找出最好和最坏的个体。如果某代的最好个体比前一代的最好个体要坏，那么后者将会取代当前种群的最坏个体 

            }

            swAvg.Close();

            // 把栅格误差超过阈值的次数统计信息写入文件
            List<KeyValuePair<string, List<long>>> lst = new List<KeyValuePair<string, List<long>>>(gridsStatics);
            // 倒序排序
            lst.Sort(delegate (KeyValuePair<string, List<long>> s1, KeyValuePair<string, List<long>> s2)
            {
                return s2.Value[0].CompareTo(s1.Value[0]);
            });
            // 排序后的结果写入文件
            foreach(KeyValuePair < string, List<long>> keyVal in lst){
                sw1.WriteLine("小区及栅格id "+keyVal.Key + "      栅格误差超过阈值的次数 "+keyVal.Value[0].ToString() );
            }
            sw1.Close();

            

            //double[] wight = { 0.7423, 0.2577, 0 };          //各目标的权重
            double[] weight = { 0.5, 0.5 };
            Best = pareto.AHP(POPSIZE, weight);

            //查看最优个体时 统计最大误差点信息 jin
            Best.getFitByAllDTPointsForSingle();
            string pathGridsStatic = basePath + @"gridsErrorStaticJin.txt";
            StreamWriter swGridsStatic = new StreamWriter(pathGridsStatic, true, Encoding.Default);
            swGridsStatic.WriteLine("============" + infoStr + " ======================");

            ////得到最小二乘法所用矩阵
            //double[,] X=new double[6352,4];
            //double[] Y=new double[6352];
            //Best.getFitByAllDTPointsForSingleToMatrix(ref X,ref Y);
            //double[,] YMatrix = new double[6352, 1];
            //for (int i = 0; i < 6352; ++i) {
            //    YMatrix[i, 0] = Y[i];
            //}
            //double[,] XT = Matrix.Transpose(X);
            //double[,] tempMatrix=Matrix.Athwart(Matrix.MultiplyMatrix(XT, X));
            //double[,] thetaMatrix = Matrix.MultiplyMatrix(Matrix.MultiplyMatrix(tempMatrix, XT), YMatrix);
            ////影响最优系数，一定要注释掉
            //for (int i = 0; i < 4; ++i) {
            //    Best.gen[i,0]=thetaMatrix[i,0];
            //}
            //Best.getFitByAllDTPoints();


            //统计总体指标
            double keyCnt = 0;//记录总的key(cellID,gxid,gyid)的数量
            foreach (string errSection in errorGridStaticInfoDic.Keys)
            {
                keyCnt += errorGridStaticInfoDic[errSection].Count;
            }
            foreach (string errSection in errorGridStaticInfoDic.Keys) {
                swGridsStatic.WriteLine("误差区间:" + errSection + " 误差区间栅格数量 " + errorGridStaticInfoDic[errSection].Count+" 占总百分比 "+ Math.Round(errorGridStaticInfoDic[errSection].Count/ keyCnt, 3));
                swGridsStatic.WriteLine("---");
            }

            //误差最大点1
            double maxError1 = -1;
            double maxErrorTrajId1 = -1;
            foreach (ErrorGridStaticInfo errorGridStaticInfo in errorGridStaticInfoDic["[20,无穷)"])
            {
                if (errorGridStaticInfo.error > maxError1) {
                    maxError1 = errorGridStaticInfo.error;
                    maxErrorTrajId1 = errorGridStaticInfo.maxPowerTrajKey;
                }
            }
            swGridsStatic.WriteLine("计算值-路测值[20,无穷)： 最大误差值" + maxError1 + " 最大误差轨迹id " + maxErrorTrajId1);
            swGridsStatic.WriteLine("---");
            //误差最大点2
            double maxError2 = 0;
            double maxErrorTrajId2 = -1;
            foreach (ErrorGridStaticInfo errorGridStaticInfo in errorGridStaticInfoDic["(-20,-无穷)"])
            {
                if (Math.Abs(errorGridStaticInfo.error) > Math.Abs(maxError2))
                {
                    maxError2 = Math.Abs(errorGridStaticInfo.error);
                    maxErrorTrajId2 = errorGridStaticInfo.maxPowerTrajKey;
                }
            }
            swGridsStatic.WriteLine("计算值-路测值(-20,-无穷)： 最大误差值" + maxError2 + " 最大误差轨迹id " + maxErrorTrajId2);
            swGridsStatic.WriteLine("---");
            //统计每一条数据指标
            foreach (string errSection in errorGridStaticInfoDic.Keys) {
                foreach (ErrorGridStaticInfo errorGridStaticInfo in errorGridStaticInfoDic[errSection]) {
                    swGridsStatic.WriteLine("区间:" + errSection + " cellId，gridX，gridY:" + errorGridStaticInfo.key);
                    swGridsStatic.WriteLine("路测值：" + errorGridStaticInfo.dtValDbm.ToString() + " 计算值：" + errorGridStaticInfo.calcValDbm.ToString()+" 误差："+errorGridStaticInfo.error.ToString());
                    swGridsStatic.WriteLine("距离:" +errorGridStaticInfo.distance.ToString()+" 直射系数:"+errorGridStaticInfo.amendDirSum.ToString());
                    swGridsStatic.WriteLine("轨迹id：" + errorGridStaticInfo.maxPowerTrajKey.ToString() + " p0:" + errorGridStaticInfo.p0.ToString() + " d1:" + errorGridStaticInfo.d1.ToString() + " d2:" + errorGridStaticInfo.d2.ToString());
                }
            }
            swGridsStatic.Close();


            //查看最优个体时 真实值与计算值高/低的栅格数量情况  todo更新
            string pathGrids = basePath + @"gridsHeighORLow.txt";
            StreamWriter swGrids = new StreamWriter(pathGrids, false, Encoding.Default);
            swGrids.WriteLine("============" + infoStr + " ======================");
            int[] heighOrLow=Best.getFitForSingle();
            swGrids.WriteLine("rece is heigher "+heighOrLow[0].ToString() + " rece is lower: " + heighOrLow[1].ToString());
            swGrids.Close();

            //查看最优个体时，计算值和路测值，以及轨迹信息  todo更新
            Best.writeTrajInfoToFile(highErrorGrids);

            result.WriteLine("每个场景的校正系数：");
            for (int i = 0; i < scenNum; i++)
            {
                for (int j = 0; j < coeNum; j++)
                    result.Write(Best.gen[i, j].ToString("0.00") + "\t");
                result.WriteLine();
            }
            result.WriteLine();
            writeErrorToFile(result, Best.fitnessVec);//将局部误差与整体误差写入文件
            result.WriteLine();

            swTime.Stop();
            result.WriteLine("用时 s :" + swTime.ElapsedMilliseconds / 60000 + " min");
            result.Close();
            //System.Diagnostics.Process.Start("notepad.exe", "result.txt");

            // 写入数据库
            System.Data.DataTable dtable = new System.Data.DataTable();
            dtable.Columns.Add("Scene");
            dtable.Columns.Add("DirectCoefficient");
            dtable.Columns.Add("ReflectCoefficient");
            dtable.Columns.Add("DiffracteCoefficient");

            for (int j = 0; j < scenNum; j++)
            {
                System.Data.DataRow thisrow = dtable.NewRow();
                thisrow["Scene"] = j;
                thisrow["DirectCoefficient"] = Best.gen[j, 0];
                thisrow["ReflectCoefficient"] = Best.gen[j, 1];
                thisrow["DiffracteCoefficient"] = Best.gen[j, 2];
                dtable.Rows.Add(thisrow);
            }

            DB.IbatisHelper.ExecuteDelete("DeleteAdjCoefficient", null);
            using (System.Data.SqlClient.SqlBulkCopy bcp = new System.Data.SqlClient.SqlBulkCopy(DB.DataUtil.ConnectionString))
            {
                bcp.BatchSize = dtable.Rows.Count;
                bcp.BulkCopyTimeout = 1000;
                bcp.DestinationTableName = "tbAdjCoefficient";
                bcp.WriteToServer(dtable);
                bcp.Close();
            }
            dtable.Clear();

        }
        #endregion ///////////////////遗传算法结束//////////////////////////////////////////////////////
    }
}
