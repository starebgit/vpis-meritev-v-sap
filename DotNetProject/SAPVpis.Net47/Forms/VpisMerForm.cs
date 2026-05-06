using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SAPVpis.Net47.Forms
{
    // .NET equivalent of Delphi vpisMer.pas (TFvpisMer.Tabelvpis path).
    // Modal DataGridView for entering measurement results with auto A/R evaluation
    // (preveri) from tolerances. Returns filled rows + remark on OK.
    public class VpisMerForm : Form
    {
        // Equivalent of Delphi `karako` record.
        public sealed class MeasurementRow
        {
            public string Naziv { get; set; }            // CHAR_DESCR
            public int StKarakteristike { get; set; }    // 1-based char index in CHAR_REQUIREMENTS (kk)
            public int StMeritve { get; set; }           // measurement # within characteristic (nn)
            public string Tip { get; set; }              // 'X' = numeric (CHAR_TYPE='01'), ' ' otherwise
            public string Posam { get; set; }            // SINGLE_RES ('X' = individual)
            public string LowerTol { get; set; }
            public string UpperTol { get; set; }
            public string Result { get; set; }
            public string Eval { get; set; }             // 'A' or 'R'
            public string Comment { get; set; }
        }

        private const int ColNaziv     = 0;
        private const int ColStMer     = 1;
        private const int ColTip       = 2;
        private const int ColPosam     = 3;
        private const int ColResult    = 4;
        private const int ColLowerTol  = 5;
        private const int ColUpperTol  = 6;
        private const int ColEval      = 7;
        private const int ColComment   = 8;

        private readonly DataGridView _grid;
        private readonly TextBox _txtRmk;
        private readonly Label _lblRmk;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;
        private readonly List<MeasurementRow> _rows;

        public bool Saved { get; private set; }
        public string Rmk { get; private set; }
        public IReadOnlyList<MeasurementRow> Rows { get { return _rows; } }

        public VpisMerForm(IEnumerable<MeasurementRow> rows, string initialRmk)
        {
            _rows = new List<MeasurementRow>(rows);
            Rmk = string.Empty;

            Text = AppTranslations.T("vpis_title");
            Width = 1100;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(255, 192, 255); // Delphi $02FFC0FF
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = UiStyle.BodyFont;

            _grid = new DataGridView
            {
                Left = 8,
                Top = 8,
                Width = 1066,
                Height = 460,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                EditMode = DataGridViewEditMode.EditOnEnter,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false
            };
            BuildColumns();
            FillGrid();

            _grid.CellEndEdit += OnCellEndEdit;
            _grid.CellClick += OnCellClick;

            _lblRmk = new Label { Text = AppTranslations.T("lbl_ctrl_pt"), Left = 8, Top = 480, AutoSize = true };
            _txtRmk = new TextBox { Left = 130, Top = 477, Width = 400, Text = initialRmk ?? string.Empty, CharacterCasing = CharacterCasing.Upper };

            _btnOk = new Button { Text = AppTranslations.T("btn_ok_vpis"), Left = 870, Top = 506, Width = 100, Height = 34 };
            _btnOk.Click += BtnOk_Click;
            AcceptButton = _btnOk;

            _btnCancel = new Button
            {
                Text = AppTranslations.T("btn_cancel"),
                Left = 980,
                Top = 506,
                Width = 100,
                Height = 34,
                DialogResult = DialogResult.Cancel
            };
            CancelButton = _btnCancel;

            Controls.AddRange(new Control[] { _grid, _lblRmk, _txtRmk, _btnOk, _btnCancel });

            // The single primary action in the entire app — submitting measurements to SAP.
            UiStyle.ApplyPrimary(_btnOk);
            UiStyle.ApplySecondary(_btnCancel);

            var tip = new ToolTip { InitialDelay = 350, ReshowDelay = 200 };
            tip.SetToolTip(_btnOk,     AppTranslations.T("tip_ok_vpis"));
            tip.SetToolTip(_btnCancel, AppTranslations.T("tip_cancel"));

            Recalculate();
        }

        private void BuildColumns()
        {
            _grid.Columns.Clear();
            _grid.Columns.Add(MakeCol(AppTranslations.T("col_char"),       230, true));
            _grid.Columns.Add(MakeCol(AppTranslations.T("col_meas_no"),     55, true));
            _grid.Columns.Add(MakeCol(AppTranslations.T("col_type"),        55, true));
            _grid.Columns.Add(MakeCol(AppTranslations.T("col_individual"),  55, true));
            _grid.Columns.Add(MakeCol(AppTranslations.T("col_result"),      90, false));
            _grid.Columns.Add(MakeCol(AppTranslations.T("col_lower_tol"),   70, true));
            _grid.Columns.Add(MakeCol(AppTranslations.T("col_upper_tol"),   70, true));
            _grid.Columns.Add(MakeCol(AppTranslations.T("col_eval"),        45, false));
            _grid.Columns.Add(MakeCol(AppTranslations.T("col_comment"),    290, false));
        }

        private static DataGridViewTextBoxColumn MakeCol(string header, int width, bool readOnly)
        {
            return new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                Width = width,
                ReadOnly = readOnly,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
        }

        private void FillGrid()
        {
            _grid.Rows.Clear();
            foreach (var r in _rows)
            {
                _grid.Rows.Add(
                    r.Naziv ?? string.Empty,
                    r.StMeritve.ToString(CultureInfo.InvariantCulture),
                    r.Tip ?? string.Empty,
                    r.Posam ?? string.Empty,
                    r.Result ?? string.Empty,
                    r.LowerTol ?? string.Empty,
                    r.UpperTol ?? string.Empty,
                    r.Eval ?? string.Empty,
                    r.Comment ?? string.Empty);
            }
        }

        // Delphi TablaClick: clicking col 7 toggles A <-> R
        private void OnCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != ColEval) return;
            var cell = _grid.Rows[e.RowIndex].Cells[ColEval];
            var v = (cell.Value as string) ?? string.Empty;
            cell.Value = v == "A" ? "R" : "A";
        }

        // Delphi TablaSetEditText (col 7) + preveri after Rezultat edit
        private void OnCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _grid.Rows[e.RowIndex];

            if (e.ColumnIndex == ColEval)
            {
                var v = ((row.Cells[ColEval].Value as string) ?? string.Empty).Trim().ToUpperInvariant();
                if (v != "A" && v != "R" && v != string.Empty) v = "R";
                row.Cells[ColEval].Value = v;
            }
            else if (e.ColumnIndex == ColResult)
            {
                EvaluateRow(e.RowIndex);
            }
        }

        // Delphi: preveri — re-evaluate all rows
        private void Recalculate()
        {
            for (var i = 0; i < _grid.Rows.Count; i++)
                EvaluateRow(i);
        }

        private void EvaluateRow(int rowIdx)
        {
            var row = _grid.Rows[rowIdx];
            var tip = ((row.Cells[ColTip].Value as string) ?? string.Empty).Trim();
            if (tip != "X") return; // Delphi: only when tip='X'

            var resultStr = ((row.Cells[ColResult].Value as string) ?? string.Empty).Trim();
            if (resultStr.Length == 0) return; // Delphi: only when Cells[4,i] <> ''

            double value;
            if (!TryParseDecimal(resultStr, out value)) return;

            var bb = true;

            var lowerStr = ((row.Cells[ColLowerTol].Value as string) ?? string.Empty).Trim();
            if (lowerStr.Length > 0)
            {
                double lower;
                if (TryParseDecimal(lowerStr, out lower) && value < lower) bb = false;
            }
            var upperStr = ((row.Cells[ColUpperTol].Value as string) ?? string.Empty).Trim();
            if (upperStr.Length > 0)
            {
                double upper;
                if (TryParseDecimal(upperStr, out upper) && value > upper) bb = false;
            }

            row.Cells[ColEval].Value = bb ? "A" : "R";
        }

        private static bool TryParseDecimal(string s, out double value)
        {
            // Accept either decimal separator (SAP often returns periods, user may type comma)
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return true;
            if (double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return true;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        // Delphi Button1Click: edit1 (rmk) must be filled, then close with kn=1
        private void BtnOk_Click(object sender, EventArgs e)
        {
            _grid.EndEdit();

            if (string.IsNullOrWhiteSpace(_txtRmk.Text))
            {
                MessageBox.Show(AppTranslations.T("msg_fill_ctrl_pt"));
                return;
            }

            Recalculate();

            for (var i = 0; i < _rows.Count && i < _grid.Rows.Count; i++)
            {
                var row = _grid.Rows[i];
                _rows[i].Result = ((row.Cells[ColResult].Value as string) ?? string.Empty).Trim();
                _rows[i].Eval = ((row.Cells[ColEval].Value as string) ?? string.Empty).Trim();
                _rows[i].Comment = ((row.Cells[ColComment].Value as string) ?? string.Empty).Trim();
            }

            Rmk = (_txtRmk.Text ?? string.Empty).Trim().ToUpperInvariant();
            Saved = true;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
