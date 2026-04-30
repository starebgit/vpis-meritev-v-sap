using System;
using System.Windows.Forms;
using SAPVpis.Net47.Services;

namespace SAPVpis.Net47.Forms
{
    public class MainForm : Form
    {
        private readonly Button _btnSapTrial;
        private readonly Button _btnSapTrialE4q;
        private readonly TextBox _txtStatus;

        public MainForm()
        {
            Text = "SAPVpis.Net47 - SAP Trial Runner";
            Width = 700;
            Height = 420;

            _btnSapTrial = new Button
            {
                Text = "Run SAP Trial",
                Left = 20,
                Top = 20,
                Width = 140,
            };
            _btnSapTrial.Click += BtnSapTrialOnClick;

            _btnSapTrialE4q = new Button
            {
                Text = "Run E4Q Trial",
                Left = 180,
                Top = 20,
                Width = 140,
            };
            _btnSapTrialE4q.Click += BtnSapTrialE4qOnClick;

            _txtStatus = new TextBox
            {
                Left = 20,
                Top = 60,
                Width = 640,
                Height = 300,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
            };

            Controls.Add(_btnSapTrial);
            Controls.Add(_btnSapTrialE4q);
            Controls.Add(_txtStatus);
        }

        private void BtnSapTrialOnClick(object sender, EventArgs e)
        {
            var result = SapConnectionSmokeTester.RunTrial();
            _txtStatus.Text = result.Success
                ? "SAP trial passed." + Environment.NewLine + result.Message
                : "SAP trial failed." + Environment.NewLine + result.Message;
        }

        private void BtnSapTrialE4qOnClick(object sender, EventArgs e)
        {
            var result = SapConnectionSmokeTester.RunTrialForSystem("E4Q");
            _txtStatus.Text = result.Success
                ? "E4Q trial passed." + Environment.NewLine + result.Message
                : "E4Q trial failed." + Environment.NewLine + result.Message;
        }
    }
}
