using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTE.Calibration
{
    class Pareto
    {
        public int maxParetoSize;       //Pareto优胜集容量
        public List<EA.Entity> NDSet;   //非支配解集
        public List<EA.Entity>[] grid;  //用一维集合表示多维网格
        public int gridNum;             //每一维上的分割次数
        public int objNum;              //目标个数
        public int scen;                //场景个数
        public int len;                 //grid的大小
        public int coeNum;              // 系数个数


        public Pareto(int POPSIZE, int scenNum, int objNum1)
        {
            maxParetoSize = POPSIZE / 2;
            NDSet = new List<EA.Entity>();
            gridNum = 5;
            objNum = objNum1;
            scen = scenNum;
            coeNum = 3;
            len = (int)Math.Pow(gridNum, objNum);
        }

        /*
           通过适应度值，计算A与B间支配关系
           1：A占支配地位
          -1：B占支配地位
           0：A=B
          11: 互相非支配
        */
        public int domain(EA.Entity A, EA.Entity B)
        {
            int i;
            HashSet<int> set = new HashSet<int>();
            for (i = 0; i < objNum; i++)
            {
                if (A.fitnessVec[i] == B.fitnessVec[i])
                    set.Add(0);
                else if (A.fitnessVec[i] < B.fitnessVec[i])
                    set.Add(1);
                else
                    set.Add(-1);
            }
            if (set.Contains(0) && set.Count() == 1)
                return 0;
            else if (!set.Contains(-1))
                return 1;
            else if (!set.Contains(1))
                return -1;
            else
                return 11;
        }

        //对population[start..end]进行一趟快速排序，
        //将支配population[start]的个体或与population[start]不相关的个体存放到population[start..i-1]中，
        //将被population[start]支配的个体存放到population[i+1..end]中
        public int quickPass(List<EA.Entity> population, int start, int end)
        {
            int i = start;
            int j = end;
            EA.Entity x = population[start];
            bool non_domminated_sign = true;
            int flag;
            EA.Entity E;
            while (i < j)
            {
                while (i < j && ((flag = domain(x, E = population[j])) == 1 || flag == 0))
                {
                    j--;
                    if ((flag = domain(E = population[j], x)) == 1)
                        non_domminated_sign = false;
                }
                population[i] = population[j];
                while (i < j && (flag = domain(E = population[i], x)) != -1)
                {
                    i++;
                    if ((flag = domain(E = population[i], x)) == 1)
                        non_domminated_sign = false;
                }
                population[j] = population[i];
            }
            population[i] = x;
            if (non_domminated_sign)
            {
                EA.Entity EE = new EA.Entity();
                copy(ref EE, ref x);
                NDSet.Add(EE);
            }
            return i;
        }

        //构造优胜集的快速排序法
        public void quickSort(List<EA.Entity> population, int start, int end)
        {
            if (start <= end)
            {
                int k = quickPass(population, start, end);
                quickSort(population, start, k-1);
            }
        }

        //对population[start..end]进行一趟快速排序，
        //将支配population[start]的个体或与population[start]不相关的个体存放到population[start..i-1]中，
        //将被population[start]支配的个体存放到population[i+1..end]中
        public int quickPass1(List<EA.Entity> population, int start, int end)
        {
            int i = start;
            int j = end;
            EA.Entity x = population[start];
            while (i < j)
            {
                while (i < j && (domain(x, population[j]) == 1 || domain(x, population[j]) == 0))
                {
                    j--;
                }
                population[i] = population[j];
                while (i < j && domain(population[i], x) != -1)
                {
                    i++;
                }
                population[j] = population[i];
            }
            population[i] = x;
            return i;
        }

        //对整体的快速排序法
        public void quickSort1(List<EA.Entity> population, int start, int end)
        {
            if (start < end)
            {
                int k = quickPass1(population, start, end);
                quickSort1(population, start, k - 1);
                quickSort1(population, k + 1, end);
            }
        }

        //将Pareto前沿中的个体放入每个网格中
        public void inGrid()
        {
            grid = new List<EA.Entity>[len];
            for (int i = 0; i < len; i++)
                grid[i] = new List<EA.Entity>();

            double[] max = new double[objNum];
            double[] min = new double[objNum];
            for (int i = 0; i < objNum; i++)
            {
                max[i] = min[i] = NDSet[0].fitnessVec[i];
            }
            for (int i = 1; i < NDSet.Count; i++)
            {
                for (int j = 0; j < objNum; j++)
                {
                    if (max[j] < NDSet[i].fitnessVec[j])
                        max[j] = NDSet[i].fitnessVec[j];
                    if (min[j] > NDSet[i].fitnessVec[j])
                        min[j] = NDSet[i].fitnessVec[j];
                }
            }
            double[] range = new double[objNum]; //域宽
            for (int i = 0; i < objNum; i++)
                range[i] = max[i] - min[i];
            double[] d = new double[objNum];     //该维上网格的宽度
            for (int i = 0; i < objNum; i++)
                d[i] = range[i] / gridNum + 1;

            //网格编号方式：从左到右，从上到下，依次递增。第三维上，从底到高递增，多维上依次类推
            for (int i = 0; i < NDSet.Count; i++)
            {
                double[] pos = new double[objNum];
                for (int j = 0; j < objNum; j++)
                    pos[j] = (NDSet[i].fitnessVec[j] - min[j]) / d[j];   //计算该个体所在网格位置
                int index = 0;
                for (int j = 0; j < objNum; j++)
                    index += (int)pos[j] * (int)Math.Pow(gridNum, j);  //将多维维换算为一维 
                grid[index].Add(NDSet[i]);
            }
        }

        //删除优胜集中多余个体。用网格法控制优胜集中个数
        public void controlNum()
        {
            int deleteNum = NDSet.Count - maxParetoSize;  //要删除的非支配解数量
            int d = 0;  //已经删除的个体数
            while (d < deleteNum)
            {
                //获取包含最多个体数的网格
                int maxIndex = 0;
                int maxCount = grid[0].Count;
                for (int i = 1; i < len; i++)
                {
                    if (grid[i].Count > maxCount)
                    {
                        maxCount = grid[i].Count;
                        maxIndex = i;
                    }
                }

                //选取该网格中的最后一个个体，将其删除
                grid[maxIndex].RemoveAt(grid[maxIndex].Count - 1);
                d++;
            }

            //将网格中的个体复制回优胜集
            NDSet.Clear();
            for (int i = 0; i < len; i++)
            {
                for (int j = 0; j < grid[i].Count; j++)
                    NDSet.Add(grid[i][j]);
            }
        }

        //层次分析法，从优胜集中选出一个最好个体
        public EA.Entity AHP(int POPSIZE, double[] wight)
        {
            double best = Math.Pow(POPSIZE, objNum);
            int index = 0;
            double[] max = new double[objNum];
            double[] min = new double[objNum];
            for (int i = 0; i < NDSet.Count; i++)
            {
                for (int j = 0; j < objNum; j++)
                {
                    if (max[j] < NDSet[i].fitnessVec[j])
                        max[j] = NDSet[i].fitnessVec[j];
                    if (min[j] > NDSet[i].fitnessVec[j])
                        min[j] = NDSet[i].fitnessVec[j];
                }
            }
            double[] avg = new double[objNum];
            for (int i = 0; i < objNum; i++)
            {
                avg[i] = (max[i] + min[i]) / 2;
            }
            for (int i = 0; i < NDSet.Count; i++)
            {
                NDSet[i].fitness = 0;
                for (int j = 0; j < objNum; j++)
                    NDSet[i].fitness += (NDSet[i].fitnessVec[j] / avg[j]) * wight[j];
                if (NDSet[i].fitness < best)
                {
                    best = NDSet[i].fitness;
                    index = i;
                }
            }
            EA.Entity Best = NDSet[index];
            return Best;
        }

        //把S复制到D
        void copy(ref EA.Entity D, ref EA.Entity S)
        {
            for (int j = 0; j < objNum; j++)
                D.fitnessVec[j] = S.fitnessVec[j];
            for (int i = 0; i < scen; i++)
            {
                for (int k = 0; k < coeNum; k++)
                    D.gen[i, k] = S.gen[i, k];
            }
        }
    }
}


