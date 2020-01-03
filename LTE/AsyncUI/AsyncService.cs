using System;
using System.Collections.Generic;
using System.Text;

namespace LTE.AsyncUI
{
    #region 通用的一些委托
    /// <summary>
    /// 异步方法的委托
    /// </summary>
    /// <param name="args">异步方法所需的参数</param>
    /// <returns>异步方法返回值</returns>
    public delegate object AsyncMethodHandle(params object[] args);

    /// <summary>
    /// 异步结束事件的委托
    /// </summary>
    /// <param name="args">异步方法的返回值</param>
    public delegate void AsyncFinishedHandle(object args);

    /// <summary>
    /// 异步异常事件的委托
    /// </summary>
    /// <param name="e">异常信息</param>
    public delegate void AsyncExceptionHandle(Exception e);
    #endregion 通用的一些委托

    /// <summary>
    /// 为通用的异步调用（主要针对UI）提供支持
    /// </summary>
    public class AsyncService
    {
        #region 公用的方法

        #region 开始异步调用
        /// <summary>
        /// 开始异步调用
        /// </summary>
        /// <param name="method">异步调用执行的方法</param>
        /// <param name="args">异步方法所需的参数</param>
        public void BeginAsync(AsyncMethodHandle method, params object[] args)
        {
            if (!this.isBusy)
            {
                lock (this.syncObject)
                {
                    this.method = method;
                    isBusy = true;
                }
                this.method.BeginInvoke(args, CallBackMethod, currentAsyncId.ToString());
            }
            else
            {
                throw new Exception("The service is busy.");
            }
        }
        #endregion 开始异步调用

        #region 取消当前的异步方法
        /// <summary>
        /// 取消当前的异步方法
        /// </summary>
        public void CancelAsync()
        {
            if (this.isBusy)
            {
                SetVarAtAsyncFinished();
            }
        }
        #endregion 取消当前的异步方法

        #endregion 公用的方法

        #region 公用的属性
        /// <summary>
        /// 异步方法结束事件
        /// </summary>
        public event AsyncFinishedHandle AsyncFinished;

        /// <summary>
        /// 异步方法出现异常事件
        /// </summary>
        public event AsyncExceptionHandle AsyncException;

        /// <summary>
        /// 指示当前的异步方法是否正忙
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return isBusy;
            }
        }
        #endregion 公用的属性

        #region 私有的方法

        #region 异步方法的回调方法
        /// <summary>
        /// 异步方法的回调方法
        /// </summary>
        /// <param name="ar">回调参数</param>
        private void CallBackMethod(IAsyncResult ar)
        {

            long asyncId = long.Parse(ar.AsyncState as string);
            if (asyncId == this.currentAsyncId)
            {
                object ret = this.method.EndInvoke(ar);
                if (ret is Exception)
                {
                    ExceptionEventFire(ret as Exception);
                }
                else
                {
                    FinishedEventFire(ret);
                }
            }


        }
        #endregion 异步方法的回调方法

        #region 异步方法结束触发事件
        /// <summary>
        /// 异步方法结束触发事件
        /// </summary>
        /// <param name="ret">异步方法返回的值</param>
        private void FinishedEventFire(object ret)
        {
            if (AsyncFinished != null)
            {
                if (AsyncFinished.Target is System.Windows.Forms.Form)
                {
                    System.Windows.Forms.Form f = AsyncFinished.Target as System.Windows.Forms.Form;
                    f.BeginInvoke(AsyncFinished, ret);
                }
                else if (AsyncFinished.Target is System.Windows.Forms.Control)
                {
                    System.Windows.Forms.Control c = AsyncFinished.Target as System.Windows.Forms.Control;
                    if (!c.IsDisposed)
                    {
                        c.BeginInvoke(AsyncFinished, ret);
                    }
                    //c.BeginInvoke(AsyncFinished, ret);
                }
                else
                {
                    AsyncFinished(ret);
                }
            }
            else
            {
                //当前无订阅异步出错的事件处理
                throw new Exception("异步完成事件为空!");

            }

            SetVarAtAsyncFinished();
        }
        #endregion 异步方法结束触发事件

        #region 异步发生异常触发事件
        /// <summary>
        /// 异步发生异常触发事件
        /// </summary>
        /// <param name="e">异常信息</param>
        private void ExceptionEventFire(Exception e)
        {
            if (AsyncException != null)
            {
                if (AsyncException.Target is System.Windows.Forms.Form)
                {
                    System.Windows.Forms.Form f = AsyncException.Target as System.Windows.Forms.Form;
                    f.BeginInvoke(AsyncException, e);
                }

                if (AsyncException.Target is System.Windows.Forms.Control)
                {
                    System.Windows.Forms.Control c = AsyncException.Target as System.Windows.Forms.Control;
                    c.BeginInvoke(AsyncException, e);
                }
                else
                {
                    //当前无订阅异步出错的事件处理
                    throw new Exception("异步出错事件为空!");
                }
            }
            SetVarAtAsyncFinished();
        }
        #endregion 异步发生异常触发事件

        #region 在异步结束后设置初始值
        /// <summary>
        /// 在异步结束后设置初始值
        /// </summary>
        private void SetVarAtAsyncFinished()
        {
            lock (this.syncObject)
            {
                isBusy = false;
                currentAsyncId = (currentAsyncId >= long.MaxValue - 1) ? long.MinValue : currentAsyncId + 1;
                method = null;
            }
        }
        #endregion 在异步结束后设置初始值

        #endregion 私有的方法

        #region 私用的字段
        /// <summary>
        /// 异步调用的方法的委托
        /// </summary>
        private AsyncMethodHandle method;

        /// <summary>
        /// 当前异步调用的序号
        /// </summary>
        private long currentAsyncId = long.MinValue;

        /// <summary>
        /// 异步调用是否在忙
        /// </summary>
        private bool isBusy = false;

        /// <summary>
        /// 用于同步的对象
        /// </summary>
        private object syncObject = new object();
        #endregion 私有的字段
    }
}
