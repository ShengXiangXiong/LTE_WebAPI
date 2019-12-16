using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTE.Utils
{
    /// <summary>
    /// 小顶堆,T类型需要实现 IComparable 接口
    /// </summary>
    public class MinHeap<T> where T : IComparable
    {
        private T[] container; // 存放堆元素的容器
        private int capacity;  // 堆的容量，最大可以放多少个元素
        private int count; // 堆中已经存储的数据个数

        public int getCount()
        {
            return count;
        }
        public int getCapacity()
        {
            return capacity;
        }

        public bool isFull()
        {
            return count >= capacity;
        }

        public bool isEmpty()
        {
            return count == 0;
        }
        public MinHeap(int _capacity)
        {
            container = new T[_capacity + 1];
            capacity = _capacity;
            count = 0;
        }
        //插入一个元素
        public bool AddItem(T item)
        {
            if (count >= capacity)
            {
                return false;
            }
            ++count;
            container[count] = item;
            int i = count;
            while (i / 2 > 0 && container[i].CompareTo(container[i / 2]) < 0)
            {
                // 自下往上堆化，交换 i 和i/2 元素
                T temp = container[i];
                container[i] = container[i / 2];
                container[i / 2] = temp;
                i = i / 2;
            }
            return true;
        }
        //获取最小的元素
        public T GetMinItem()
        {
            if (count == 0)
            {
                return default(T);
            }
            T result = container[1];
            return result;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T GetItemByIndex(int index)
        {
            if (index < count)
            {
                return container[index + 1];
            }
            return default(T);
        }
        //删除最小的元素，即堆顶元素
        public bool DeteleMinItem()
        {
            if (count == 0)
            {
                return false;
            }
            container[1] = container[count];
            container[count] = default(T);
            --count;
            UpdateHeap(container, count, 1);
            return true;
        }
        //从某个节点开始从上向下 堆化
        private void UpdateHeap(T[] a, int n, int i)
        {
            while (true)
            {
                int maxPos = i;
                //遍历左右子树，确定那个是最小的元素
                if (i * 2 <= n && a[i].CompareTo(a[i * 2]) > 0)
                {
                    maxPos = i * 2;
                }
                if (i * 2 + 1 <= n && a[maxPos].CompareTo(a[i * 2 + 1]) > 0)
                {
                    maxPos = i * 2 + 1;
                }
                if (maxPos == i)
                {
                    break;
                }
                T temp = container[i];
                container[i] = container[maxPos];
                container[maxPos] = temp;
                i = maxPos;
            }
        }
    }
}
