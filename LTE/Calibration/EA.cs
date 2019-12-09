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
//70--462

namespace LTE.Calibration
{
    public class EA
    {
        public static void initEA(int popSize, int gen, int sceneNum,
                    ref Dictionary<string, double> meaPwr1, ref Dictionary<string, TrajInfo> rayDic1, int frequence1)
        {
            POPSIZE = popSize;
            MAXGENS = gen;
            Scen = sceneNum;
            meaPwr = meaPwr1;
            rayDic = rayDic1;
            smaObj = (int)(meaPwr.Count * 0.3);
            frequence = frequence1;
        }

        #region 成员变量
        public static int frequence;
        public static int Scen = 3;     //3个场景

        public static int MAXGENS = 500;         //进化的最大代数
        public static int POPSIZE = 50;     //种群规模
        public double PXOVER0 = 0.9;        //交叉概率
        public double PMUTATION0 = 0.1;    //变异概率
        public double PXOVER = 0.9;         //交叉概率
        public double PMUTATION = 0.1;     //变异概率
        public double PSELECT = 0.9;        //选择优良个体的概率
        public static int objNum = 2;       //目标个数

        public int generation;     //进化到第几代
        public Entity Best;        //最终的最好个体

        public static Random r;

        public static int smaObj = 100;   // 局部路测点数量
        public static int scenNum = 3;     // 场景数量
        public static int coeNum = 3;      // 要校正的系数数量

        // 真实路测，这里是根据射线轨迹得到的路测加上随机扰动的结果，为模拟路测
        public static Dictionary<string, double> meaPwr;

        public static Dictionary<string, TrajInfo> rayDic;

        public class Entity
        {
            public double BigRes;
            public double SmaRes;

            public int index;            // 下标
            public double[,] gen;   //一个染色方案
            public double[] fitnessVec;     // 局部目标和整体目标
            public double fitness;       // 通过AHP，最后从优胜集中选出一个最优解

            public Entity()
            {
                gen = new double[scenNum, coeNum];
                for (int i = 0; i < scenNum; i++)
                {
                    for (int j = 0; j < coeNum; j++)
                    {
                        if (j == 0)
                        {
                            gen[i, j] = r.Next(200) * 0.01;
                        }
                        else
                        {
                            gen[i, j] = r.NextDouble();
                        }
                    }
                }

                fitnessVec = new double[objNum];
                for (int i = 0; i < objNum; i++)
                    fitnessVec[i] = 0;
            }

            public override bool Equals(object obj)
            {
                Entity e = obj as Entity;
                for (int i = 0; i < objNum; i++)
                    if (this.fitnessVec[i] != e.fitnessVec[i])
                        return false;
                return true;
            }

            //获取适应度
            public void getFit()
            {
                double sum1 = 0;
                double sum2 = 0;
                int i = 0;

                foreach (string key in meaPwr.Keys)
                {
                    double recePwr = rayDic[key].calc(ref this.gen, 3, frequence);

                    if (i < smaObj)
                        sum1 += Math.Pow((recePwr - meaPwr[key]), 2);  // 局部，重点栅格，这里先假设为 1/3 ~ 1/4
                    else
                        sum2 += Math.Pow((recePwr - meaPwr[key]), 2);
                    i++;
                }

                fitnessVec[0] = Math.Sqrt(sum1 / smaObj);  // 局部
                fitnessVec[1] = Math.Sqrt((sum1 + sum2) / meaPwr.Count);  // 整体
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
            pareto.quickSort(sortPop, 0, sortPop.Count - 1);      //得到非支配解集
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
            Parallel.For(0, POPSIZE, mem =>
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

            //X.getFit();

            lock (q)
            {
                q.Add(X);
            }
        }

        // 变异函数，随机选择某一个体
        void mutate()
        {
            Parallel.For(0, POPSIZE, i =>
            {
                lock (population[i])
                {
                    double r1 = ThreadSafeRandom.NextDouble();  //随机选择变异个体
                    Entity E = new Entity();
                    Entity S = population[i];
                    copy(ref E, ref S);

                    if (r1 < PMUTATION)   //变异
                    {
                        for (int k = 0; k < Scen; k++)
                        {
                            for (int j = 0; j < coeNum; j++)
                            {
                                int index = ThreadSafeRandom.Next(0, coeNum);
                                if (index == 0)
                                {
                                    E.gen[k, index] = r.Next(200) * 0.01;
                                }
                                else
                                {
                                    E.gen[k, index] = r.NextDouble();
                                }
                            }
                        }
                        lock (q)
                        {
                            q.Add(E);
                        }
                    }
                }
            });
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
            int i;
            for (i = 0; i < POPSIZE; i++)
            {
                Entity E = new Entity();
                Entity S = population[i];
                copy(ref E, ref S);
                q.Add(E);
            }
            pareto.quickSort1(q, 0, q.Count - 1);
            i = 0;
            int k = 0;
            while (i < POPSIZE && k < q.Count)
            {
                Entity node = q[k++];
                double p = r.NextDouble();
                if (p < PSELECT)
                {
                    population[i++] = node;
                }
            }
            i = 0;
            k = 0;
            while (i < POPSIZE)
            {
                population[i++] = q[k++];
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
            newpareto.quickSort(newq, 0, newq.Count - 1);  //得到当代的非支配解集
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

        //报告模拟进展情况
        void report()
        {
            try
            {
                sw.WriteLine(generation);
                double[] avg = new double[objNum];
                for (int i = 0; i < pareto.NDSet.Count; i++)
                {
                    for (int j = 0; j < scenNum; j++)
                    {
                        for (int k = 0; k < coeNum; k++)
                        {
                            sw.Write(pareto.NDSet[i].gen[j, k] + "\t");
                        }
                        sw.WriteLine();
                    }
                    for (int j = 0; j < objNum; j++)
                    {
                        sw.Write(pareto.NDSet[i].fitnessVec[j] + "\t");
                        avg[j] += pareto.NDSet[i].fitnessVec[j];
                    }
                    sw.WriteLine();
                };
                sw.WriteLine();
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

        public void GaMain()
        {
            DateTime t0 = DateTime.Now;

            StreamWriter result;
            string path1 = @"result.txt";
            result = File.CreateText(path1);

            // 初始误差
            double sum1 = 0;
            double sum2 = 0;
            int i = 0;
            foreach (string key in meaPwr.Keys)
            {
                if (i < smaObj)
                    sum1 += Math.Pow((rayDic[key].sumPwrDbm - meaPwr[key]), 2);  // 局部，重点栅格，这里先假设为 1/3 ~ 1/4
                else
                    sum2 += Math.Pow((rayDic[key].sumPwrDbm - meaPwr[key]), 2);
                i++;
            }
            double err1 = Math.Sqrt(sum1 / smaObj);  // 局部
            double err2 = Math.Sqrt((sum1 + sum2) / meaPwr.Count);  // 整体
            result.WriteLine("初始局部误差与总体误差：");
            result.WriteLine(err1 + "\t" + err2);
            result.WriteLine();

            string currentDirectory = System.Environment.CurrentDirectory;
            string currentTime = DateTime.Now.ToString();
            string path = @"galog.txt";
            string path_avg = @"galog_avg.txt";

            //创建并写入(将覆盖已有文件)
            sw = File.CreateText(path);
            swAvg = File.CreateText(path_avg);

            generation = 0;
            init();
            evaluate();         //评价函数，可以由用户自定义，该函数取得每个基因的适应度
            keep_the_best();    //保存每次遗传后的最佳基因

            while (generation < MAXGENS)
            {
                generation++;
                select();     //选择函数：用于最大化合并杰出模型的标准比例选择，保证最优秀的个体得以生存
                crossover();  //杂交函数：选择两个个体来杂交，这里用单点杂交 
                mutate();     //变异函数：被该函数选中后会使得某一变量被一个随机的值所取代 
                report();     //报告模拟进展情况
                evaluate();   //评价函数，可以由用户自定义，该函数取得每个基因的适应度
                elitist();    //搜寻杰出个体函数：找出最好和最坏的个体。如果某代的最好个体比前一代的最好个体要坏，那么后者将会取代当前种群的最坏个体 
            }

            sw.Close();
            swAvg.Close();

            double[] wight = { 0.7423, 0.2577, 0 };          //各目标的权重
            Best = pareto.AHP(POPSIZE, wight);

            result.WriteLine("每个场景的校正系数：");
            for (i = 0; i < scenNum; i++)
            {
                for (int j = 0; j < coeNum; j++)
                    result.Write(Best.gen[i, j] + "\t");
                result.WriteLine();
            }
            result.WriteLine();
            result.WriteLine("局部误差与总体误差：");
            for (i = 0; i < objNum; i++)
            {
                result.Write(Best.fitnessVec[i] + "\t");
            }
            result.WriteLine();

            DateTime t1 = DateTime.Now;
            result.WriteLine("耗时：" + (t1 - t0).TotalMilliseconds / 60000 + " min");
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
