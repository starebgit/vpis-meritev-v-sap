using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SAPVpis.Net47.Services;

namespace SAPVpis.Net47.Forms
{
    // .NET equivalent of Delphi SqlSarze.pas (TFSqlSarze).
    // Master table view of KontSarze with two actions:
    //   "Prepis" — exports all rows to sarze.txt in app directory
    //   "Briši"  — deletes the selected row (with confirmation)
    public class SqlSarzeForm : Form
    {
        private readonly DataGridView _grid;
        private readonly Button _btnPrepis;
        private readonly Button _btnBrisi;
        private readonly Button _btnClose;
        private readonly Label _lblStatus;

        public SqlSarzeForm()
        {
            Text = AppTranslations.T("kontsarze_title");
            Width = 1450;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            Font = UiStyle.BodyFont;

            // Top action panel (Delphi: Panel1)
            _btnPrepis = new Button
            {
                Text = AppTranslations.T("btn_prepis"),
                Left = 16, Top = 6, Width = 110, Height = 32
            };
            _btnPrepis.Click += BtnPrepis_Click;

            _btnBrisi = new Button
            {
                Text = AppTranslations.T("btn_brisi"),
                Left = 134, Top = 6, Width = 110, Height = 32
            };
            _btnBrisi.Click += BtnBrisi_Click;

            _btnClose = new Button
            {
                Text = AppTranslations.T("btn_close"),
                Left = 1306, Top = 6, Width = 110, Height = 32
            };
            _btnClose.Click += (s, e) => Close();

            _lblStatus = new Label
            {
                Text = string.Empty,
                Left = 220, Top = 14, Width = 1080, AutoSize = false,
                ForeColor = Color.DarkRed
            };

            // Main grid (Delphi: Panel2 with DBGrid1)
            _grid = new DataGridView
            {
                Left = 8, Top = 44, Width = 1416, Height = 500,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                BackgroundColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.AddRange(new Control[]
            {
                _btnPrepis, _btnBrisi, _btnClose, _lblStatus, _grid
            });

            UiStyle.ApplySecondary(_btnPrepis);
            UiStyle.ApplyDanger(_btnBrisi);
            UiStyle.ApplySecondary(_btnClose);

            var tip = new ToolTip { InitialDelay = 350, ReshowDelay = 200 };
            tip.SetToolTip(_btnPrepis, AppTranslations.T("tip_prepis"));
            tip.SetToolTip(_btnBrisi,  AppTranslations.T("tip_brisi"));
            tip.SetToolTip(_btnClose,  AppTranslations.T("tip_close"));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ReloadData();
        }

        private void ReloadData()
        {
            try
            {
                _grid.DataSource = SapKontSarzeRepository.LoadAll();
                _lblStatus.Text = string.Empty;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = AppTranslations.T("msg_kontsarze_load_err") + ex.Message;
            }
        }

        // Delphi Button1Click — write all rows to sarze.txt as: sarza,oddelek,tekst,ktocka
        private void BtnPrepis_Click(object sender, EventArgs e)
        {
            var dt = _grid.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show(AppTranslations.T("msg_no_data"));
                return;
            }

            var path = Path.Combine(Application.StartupPath, "sarze.txt");
            try
            {
                using (var sw = new StreamWriter(path, false, Encoding.UTF8))
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        var sr  = ReadField(r, "SARZA");
                        var odd = ReadField(r, "ODDELEK");
                        var txt = ReadField(r, "TEKST");
                        var toc = ReadField(r, "KTOCKA");
                        sw.WriteLine(sr + "," + odd + "," + txt + "," + toc);
                    }
                }
                _lblStatus.Text = string.Format(AppTranslations.T("msg_export_ok"), path);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = AppTranslations.T("msg_export_err") + ex.Message;
                MessageBox.Show(AppTranslations.T("msg_export_err") + ex.Message);
            }
        }

        // Delphi Button2Click — confirm + delete current row
        private void BtnBrisi_Click(object sender, EventArgs e)
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show(AppTranslations.T("msg_select_to_delete"));
                return;
            }

            if (MessageBox.Show(AppTranslations.T("msg_confirm_del"),
                    AppTranslations.T("confirm_title"),
                    MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            var sarza = ReadField(row, "SARZA");
            var odd   = ReadField(row, "ODDELEK");

            try
            {
                var n = SapKontSarzeRepository.DeleteRow(sarza, odd);
                _lblStatus.Text = string.Format(AppTranslations.T("msg_delete_ok"), n);
                ReloadData();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = AppTranslations.T("msg_del_err") + ex.Message;
                MessageBox.Show(AppTranslations.T("msg_del_err") + ex.Message);
            }
        }

        private DataRow GetSelectedRow()
        {
            if (_grid.CurrentRow == null) return null;
            var drv = _grid.CurrentRow.DataBoundItem as DataRowView;
            return drv?.Row;
        }

        private static string ReadField(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column)) return string.Empty;
            var v = row[column];
            return v == DBNull.Value ? string.Empty : v.ToString().Trim();
        }
    }
}
