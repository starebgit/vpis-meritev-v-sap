using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using SAPVpis.Net47.Services;

namespace SAPVpis.Net47.Forms
{
    // .NET equivalent of Delphi SqlTable.pas TFSqlTable (Prikaz/Button1/Button2/Button3)
    // Master-detail view: SapVzr (headers) + SapChr (characteristics per entry)
    public class SqlViewForm : Form
    {
        private readonly DataGridView _gridVzr;
        private readonly DataGridView _gridChr;
        private readonly Label _lblVzr;
        private readonly Label _lblChr;
        private readonly Button _btnRefresh;
        private readonly Button _btnResend;
        private readonly Button _btnDelete;
        private readonly Button _btnClose;
        private readonly Label _lblStatus;
        private bool _loading;

        public SqlViewForm()
        {
            Text = AppTranslations.T("sql_title");
            Width = 1150;
            Height = 720;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            Font = UiStyle.BodyFont;

            _lblVzr = new Label { Text = AppTranslations.T("lbl_vzr"), Left = 8, Top = 8, AutoSize = true };

            _gridVzr = new DataGridView
            {
                Left = 8, Top = 26, Width = 1114, Height = 200,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                BackgroundColor = Color.White
            };
            _gridVzr.SelectionChanged += OnVzrSelectionChanged;

            _lblChr = new Label { Text = AppTranslations.T("lbl_chr_detail"), Left = 8, Top = 234, AutoSize = true };

            _gridChr = new DataGridView
            {
                Left = 8, Top = 252, Width = 1114, Height = 340,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                BackgroundColor = Color.White
            };

            _btnRefresh = new Button { Text = AppTranslations.T("btn_refresh"), Left = 8, Top = 600, Width = 100, Height = 32 };
            _btnRefresh.Click += (s, e) => Refresh();

            _btnResend = new Button
            {
                Text = AppTranslations.T("btn_send_sap"),
                Left = 118, Top = 600, Width = 130, Height = 32
            };
            _btnResend.Click += BtnResend_Click;

            _btnDelete = new Button
            {
                Text = AppTranslations.T("btn_delete_entry"),
                Left = 258, Top = 600, Width = 130, Height = 32
            };
            _btnDelete.Click += BtnDeleteAll_Click;

            _btnClose = new Button { Text = AppTranslations.T("btn_close"), Left = 1012, Top = 600, Width = 110, Height = 32 };
            _btnClose.Click += (s, e) => Close();

            _lblStatus = new Label
            {
                Text = string.Empty,
                Left = 488, Top = 610, Width = 520, AutoSize = false,
                ForeColor = Color.DarkRed
            };

            Controls.AddRange(new Control[]
            {
                _lblVzr, _gridVzr,
                _lblChr, _gridChr,
                _btnRefresh, _btnResend, _btnDelete, _btnClose, _lblStatus
            });

            UiStyle.ApplySecondary(_btnRefresh);
            UiStyle.ApplySecondary(_btnResend);
            UiStyle.ApplyDanger(_btnDelete);
            UiStyle.ApplySecondary(_btnClose);

            var tip = new ToolTip { InitialDelay = 350, ReshowDelay = 200 };
            tip.SetToolTip(_btnRefresh, AppTranslations.T("tip_refresh"));
            tip.SetToolTip(_btnResend,  AppTranslations.T("tip_send_sap"));
            tip.SetToolTip(_btnDelete,  AppTranslations.T("tip_delete_entry"));
            tip.SetToolTip(_btnClose,   AppTranslations.T("tip_close"));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Refresh();
        }

        public new void Refresh()
        {
            _loading = true;
            try
            {
                var vzrDt = SapMeasurementSqlRepository.LoadSapVzr();
                _gridVzr.DataSource = vzrDt;
                _gridChr.DataSource = null;
                _lblStatus.Text = string.Empty;
            }
            catch (Exception ex)
            {
                ShowStatus(AppTranslations.T("msg_read_vzr_err") + ex.Message);
            }
            finally
            {
                _loading = false;
            }
        }

        private void OnVzrSelectionChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            var zap = GetSelectedZap();
            if (zap < 0) return;
            try
            {
                _gridChr.DataSource = SapMeasurementSqlRepository.LoadSapChr(zap);
            }
            catch (Exception ex)
            {
                ShowStatus(AppTranslations.T("msg_read_chr_err") + ex.Message);
            }
        }

        // Delphi: Button1Click — re-send to SAP if PRENOS != 'X'
        private void BtnResend_Click(object sender, EventArgs e)
        {
            var zap = GetSelectedZap();
            if (zap < 0) { MessageBox.Show(AppTranslations.T("msg_sel_vzr_row")); return; }

            var vzrRow = GetSelectedVzrRow();
            if (vzrRow == null) return;

            var prenos = (vzrRow["PRENOS"] as string ?? string.Empty).Trim();
            if (prenos == "X")
            {
                MessageBox.Show(AppTranslations.T("msg_already_sent"));
                return;
            }

            var insplot = (vzrRow["INSPLOT"] as string ?? string.Empty).Trim();
            var inspoper = (vzrRow["INSPOPER"] as string ?? string.Empty).Trim();
            var rmk = (vzrRow["NAZIVKK"] as string ?? string.Empty).Trim();
            var datum = vzrRow["DATUM"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(vzrRow["DATUM"]);

            var chrDt = SapMeasurementSqlRepository.LoadSapChr(zap);
            if (chrDt.Rows.Count == 0)
            {
                MessageBox.Show(AppTranslations.T("msg_no_chr_rows"));
                return;
            }

            var overallVal = "A";
            var postRows = new List<SapInspectionRecordPostService.CharRow>();
            foreach (DataRow r in chrDt.Rows)
            {
                var eval = (r["EVAL"] as string ?? string.Empty).Trim();
                if (eval == "R") overallVal = "R";
                postRows.Add(new SapInspectionRecordPostService.CharRow
                {
                    InspChar = (r["STKAR"] as string ?? string.Empty).Trim(),
                    IsIndividual = (r["SKUPNI"] as string ?? string.Empty).Trim(),
                    CharType = (r["TIP"] as string ?? string.Empty).Trim(),
                    ResultValue = (r["MERITEV"] as string ?? string.Empty).Trim(),
                    Valuation = eval,
                    Remark = (r["OPOMBA"] as string ?? string.Empty).Trim(),
                    SequentialIndex = r["STMER"] == DBNull.Value ? 1 : Convert.ToInt32(r["STMER"])
                });
            }

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var result = new SapInspectionRecordPostService().PostAllResults(
                    SapSessionFactory.GetDestination(),
                    insplot, inspoper, rmk,
                    SapSessionFactory.GetPlant(), datum, overallVal, postRows);

                if (result.Success)
                {
                    SapMeasurementSqlRepository.Prenos(zap);
                    Refresh();
                    ShowStatus(AppTranslations.T("msg_send_ok") + zap);
                }
                else
                {
                    ShowStatus(AppTranslations.T("msg_sap_err_pfx") + result.Message);
                    MessageBox.Show(AppTranslations.T("msg_sap_err") + "\n" + result.Message);
                }
            }
            catch (Exception ex)
            {
                ShowStatus(AppTranslations.T("msg_send_err_pfx") + ex.Message);
                MessageBox.Show(AppTranslations.T("msg_send_err_pfx") + ex.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        // Delphi: Button3Click — delete all SapChr + SapVzr for selected ZAP
        private void BtnDeleteAll_Click(object sender, EventArgs e)
        {
            var zap = GetSelectedZap();
            if (zap < 0) { MessageBox.Show(AppTranslations.T("msg_sel_vzr_row")); return; }
            if (MessageBox.Show(AppTranslations.T("msg_confirm_del_all") + zap + "?",
                    AppTranslations.T("confirm_title"), MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try { SapMeasurementSqlRepository.DeleteVzrWithDetails(zap); Refresh(); }
            catch (Exception ex) { ShowStatus(AppTranslations.T("msg_del_err") + ex.Message); }
        }

        private long GetSelectedZap()
        {
            if (_gridVzr.CurrentRow == null) return -1;
            var v = _gridVzr.CurrentRow.Cells["ZAP"].Value;
            return v == null || v == DBNull.Value ? -1L : Convert.ToInt64(v);
        }

        private DataRow GetSelectedVzrRow()
        {
            if (_gridVzr.CurrentRow == null) return null;
            var drv = _gridVzr.CurrentRow.DataBoundItem as DataRowView;
            return drv?.Row;
        }

        private void ShowStatus(string msg) { _lblStatus.Text = msg; }
    }
}
