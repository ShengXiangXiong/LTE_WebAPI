using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;

namespace LTE.login
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Login_Load(object sender, EventArgs e)
        {

        }

        private bool isSucceed = false;

        private void btnCancel_Click(object sender, EventArgs e)
        {
            isSucceed = false;
            this.Close();
        }

        #region 用户认证
        /// <summary>
        /// 检查用户是否合法
        /// </summary>
        /// <param name="name">用户名</param>
        /// <param name="pwd">密码</param>
        /// <param name="cityName">用户所属城市名</param>
        /// <returns>真 OR 假</returns>
        private bool CheckUser(string name, string pwd)
        {
            //return true;
            //判断用户名密码以及地市名是否匹配
            try
            {
                Hashtable ht = new Hashtable();
                ht["name"] = name;
                ht["pwd"] = pwd;
                DataTable dt = DB.IbatisHelper.ExecuteQueryForDataTable("getUser", ht);
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show(this, "用户名或密码错误！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (System.Data.SqlClient.SqlException err)
            {
                if (err.Message.IndexOf("连接超时") != -1)
                {
                    MessageBox.Show(this, "数据库连接超时，请稍后重试", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                else if (err.Message.IndexOf("侦听") != -1)
                {
                    MessageBox.Show(this, "数据库连接超时，请稍后重试", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                else
                {
                    MessageBox.Show(this, err.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
            }
            catch (System.Exception err)
            {
                MessageBox.Show(this, err.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
        }
        #endregion 用户认证

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (CheckUser(this.tbUsername.Text, this.tbPwd.Text))
            {
                isSucceed = true;
                MainForm mainForm = new MainForm(this);
                mainForm.Show();
                this.Visible = false;
            }
            else
            {
                isSucceed = false;
            }
        }
    }
}
