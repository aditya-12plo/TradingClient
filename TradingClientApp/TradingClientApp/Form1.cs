using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TradingClientApp
{
    public partial class Form1 : Form
    {
        CTSFixClient _ctsFixClient;

        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if(_ctsFixClient==null)
            {
                _ctsFixClient = new CTSFixClient();
                _ctsFixClient.OnProgress += ShowProgress;
            }

           await _ctsFixClient.Initialize(ShowProgress,CancellationToken.None);

            _ctsFixClient.Connect();

        }

        private void ShowProgress(string msg)
        {
            if(InvokeRequired)
            {
                Invoke(new Action<string>(ShowProgress), msg);
                return;
            }
            richTextBox1.Text +=$"\r\n{msg}";
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(_ctsFixClient!=null)
            {
                _ctsFixClient.Disconnect();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _ctsFixClient.SendSecurityDefinitionRequest(ShowProgress);
        }
    }
}
