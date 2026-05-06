using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SAP.Middleware.Connector;
using SAPVpis.Net47.Services;

namespace SAPVpis.Net47.Forms
{
    // .NET equivalent of Delphi Vnos.pas (TFvnos)
    public class VnosForm : Form
    {
        // translatable labels (need field references for ApplyTranslations)
        private readonly Label _lblOddelek;
        private readonly Label _lblArtikel;
        private readonly Label _lblDatum;
        private readonly Label _lblKratki;
        private readonly Label _lblNaziv;
        private readonly Label _lblLots;
        private readonly Label _lblOper;
        private readonly Label _lblChar;
        // translatable menu items
        private readonly ToolStripMenuItem _menuPregled;
        private readonly ToolStripMenuItem _menuZgodovina;
        private readonly ToolStripMenuItem _menuKontSarze;
        private readonly ToolStripMenuItem _menuLanguage;
        // data controls
        private readonly ListBox _listDepartments;
        private readonly TextBox _txtArtikel;
        private readonly DateTimePicker _dtpDatum;
        private readonly ComboBox _cmbKratkiTekst;
        private readonly ListBox _listLots;
        private readonly ListBox _listOperations;
        private readonly ListBox _listCharacteristics;
        private readonly TextBox _txtNazivIzdelka;
        private readonly Label _lblStatus;
        private readonly Button _btnLoadLots;
        private readonly Button _btnLoadOperations;
        private readonly Button _btnLoadChar;
        private readonly Button _btnEnterMeasurements;
        private readonly Button _btnExit;
        private readonly Button _btnDiagnose;

        private readonly ToolTip _tooltip = new ToolTip { InitialDelay = 350, ReshowDelay = 200 };
        private int _searchMode;  // 0=SQL dept, 1=material (nc=1), 2=shorttext (nc=2)
        private List<SapInspectionListService.InspectionLotEntry> _sapLots;
        private string _selectedLot;
        private string _selectedOperation;
        private List<SapCharacteristicService.CharacteristicEntry> _characteristics;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public VnosForm()
        {
            Icon = CreateAppIcon();
            Text = AppTranslations.T("vnos_title");
            Width = 1200;
            Height = 760;
            BackColor = Color.FromArgb(180, 240, 220); // modern mint-cyan (was Delphi $02E0FFD0)
            MaximizeBox = false;
            Font = UiStyle.BodyFont;

            // MenuStrip
            var menuStrip = new MenuStrip { Font = UiStyle.MenuFont };
            _menuPregled = new ToolStripMenuItem(AppTranslations.T("menu_view"));
            _menuZgodovina = new ToolStripMenuItem(AppTranslations.T("menu_history"));
            _menuZgodovina.Click += (s, e) => { using (var frm = new SqlViewForm()) frm.ShowDialog(this); };
            _menuKontSarze = new ToolStripMenuItem(AppTranslations.T("menu_kontsarze"));
            _menuKontSarze.Click += (s, e) => { using (var frm = new SqlSarzeForm()) frm.ShowDialog(this); };
            _menuPregled.DropDownItems.Add(_menuZgodovina);
            _menuPregled.DropDownItems.Add(_menuKontSarze);

            _menuLanguage = new ToolStripMenuItem(AppTranslations.T("menu_language"));
            var menuSlo = new ToolStripMenuItem("SLO") { Checked = true };
            var menuEn  = new ToolStripMenuItem("EN");
            var menuDe  = new ToolStripMenuItem("DE");

            Action updateLangChecks = () =>
            {
                menuSlo.Checked = AppTranslations.CurrentLanguage == AppLanguage.SLO;
                menuEn.Checked  = AppTranslations.CurrentLanguage == AppLanguage.EN;
                menuDe.Checked  = AppTranslations.CurrentLanguage == AppLanguage.DE;
            };

            menuSlo.Click += (s, e) => { AppTranslations.SetLanguage(AppLanguage.SLO); updateLangChecks(); };
            menuEn.Click  += (s, e) => { AppTranslations.SetLanguage(AppLanguage.EN);  updateLangChecks(); };
            menuDe.Click  += (s, e) => { AppTranslations.SetLanguage(AppLanguage.DE);  updateLangChecks(); };
            _menuLanguage.DropDownItems.AddRange(new ToolStripItem[] { menuSlo, menuEn, menuDe });

            menuStrip.Items.Add(_menuPregled);
            menuStrip.Items.Add(_menuLanguage);
            MainMenuStrip = menuStrip;

            AppTranslations.LanguageChanged += (s, e) => { ApplyTranslations(); _menuLanguage.Text = AppTranslations.T("menu_language"); };

            const int lx = 8;   // left panel x
            const int lw = 188; // left panel width
            const int rx = 204; // right content x offset
            const int to = 26;  // top offset for MenuStrip

            // --- Left panel: departments ---
            _lblOddelek = new Label { Text = AppTranslations.T("lbl_dept"), Left = lx, Top = to + 8, AutoSize = true };
            _listDepartments = new ListBox { Left = lx, Top = to + 26, Width = lw, Height = 634 };
            _listDepartments.SelectedIndexChanged += OnDepartmentSelected;

            // --- Search row ---
            _lblArtikel = new Label { Text = AppTranslations.T("lbl_code"), Left = rx, Top = to + 14, AutoSize = true };
            _txtArtikel = new TextBox { Left = rx + 56, Top = to + 10, Width = 130 };

            _lblDatum = new Label { Text = AppTranslations.T("lbl_date_from"), Left = rx + 198, Top = to + 14, AutoSize = true };
            _dtpDatum = new DateTimePicker { Left = rx + 268, Top = to + 10, Width = 120, Format = DateTimePickerFormat.Short };

            _lblKratki = new Label { Text = AppTranslations.T("lbl_shorttext"), Left = rx + 400, Top = to + 14, AutoSize = true };
            _cmbKratkiTekst = new ComboBox { Left = rx + 488, Top = to + 10, Width = 200, DropDownStyle = ComboBoxStyle.DropDown };
            _cmbKratkiTekst.Click += OnKratkiTekstClick;

            _btnLoadLots = new Button { Text = AppTranslations.T("btn_load_lots"), Left = rx + 698, Top = to + 8, Width = 130, Height = 26 };
            _btnLoadLots.Click += BtnLoadLots_Click;

            // --- Naziv izdelka row ---
            _lblNaziv = new Label { Text = AppTranslations.T("lbl_item_name"), Left = rx, Top = to + 44, AutoSize = true };
            _txtNazivIzdelka = new TextBox { Left = rx + 100, Top = to + 40, Width = 500, ReadOnly = true, BackColor = Color.FromArgb(180, 240, 220) };

            // --- Lots ---
            _lblLots = new Label { Text = AppTranslations.T("lbl_lots"), Left = rx, Top = to + 74, AutoSize = true };
            _listLots = new ListBox { Left = rx, Top = to + 92, Width = 952, Height = 130 };
            _listLots.SelectedIndexChanged += OnLotSelected;
            _listLots.DoubleClick += BtnLoadOperations_Click;

            // --- Operations ---
            _btnLoadOperations = new Button { Text = AppTranslations.T("btn_load_ops"), Left = rx, Top = to + 234, Width = 150, Height = 26 };
            _btnLoadOperations.Click += BtnLoadOperations_Click;
            _lblOper = new Label { Text = AppTranslations.T("lbl_ops"), Left = rx + 160, Top = to + 239, AutoSize = true };
            _listOperations = new ListBox { Left = rx, Top = to + 268, Width = 952, Height = 96 };
            _listOperations.SelectedIndexChanged += OnOperationSelected;
            _listOperations.DoubleClick += BtnLoadChar_Click;

            // --- Characteristics ---
            _btnLoadChar = new Button { Text = AppTranslations.T("btn_load_char"), Left = rx, Top = to + 374, Width = 180, Height = 26 };
            _btnLoadChar.Click += BtnLoadChar_Click;
            _lblChar = new Label { Text = AppTranslations.T("lbl_char"), Left = rx + 190, Top = to + 379, AutoSize = true };
            _listCharacteristics = new ListBox { Left = rx, Top = to + 408, Width = 952, Height = 150 };
            _listCharacteristics.DoubleClick += BtnEnterMeasurements_Click;

            // --- Bottom ---
            _btnEnterMeasurements = new Button { Text = AppTranslations.T("btn_enter_meas"), Left = rx, Top = to + 570, Width = 140, Height = 30 };
            _btnEnterMeasurements.Click += BtnEnterMeasurements_Click;

            _btnExit = new Button { Text = AppTranslations.T("btn_exit"), Left = rx + 150, Top = to + 570, Width = 80, Height = 30 };
            _btnExit.Click += (s, e) => Application.Exit();

            _btnDiagnose = new Button
            {
                Text = "BAPI Diagnostika",
                Left = rx + 700, Top = to + 570, Width = 130, Height = 30,
                BackColor = Color.LightYellow,
                Visible = false
            };
            _btnDiagnose.Click += BtnDiagnose_Click;

            _lblStatus = new Label
            {
                Text = string.Empty,
                Left = rx + 240,
                Top = to + 576,
                Width = 460,
                AutoSize = false,
                ForeColor = Color.DarkRed
            };

            Controls.AddRange(new Control[]
            {
                menuStrip,
                _lblOddelek, _listDepartments,
                _lblArtikel, _txtArtikel,
                _lblDatum, _dtpDatum,
                _lblKratki, _cmbKratkiTekst,
                _btnLoadLots,
                _lblNaziv, _txtNazivIzdelka,
                _lblLots, _listLots,
                _btnLoadOperations, _lblOper, _listOperations,
                _btnLoadChar, _lblChar, _listCharacteristics,
                _btnEnterMeasurements, _btnExit, _btnDiagnose, _lblStatus
            });

            // Modern flat button styling. "Vnesi meritve" is the primary CTA on this form
            // (matches the primary "Vpis" inside the dialog it opens). "Izhod" gets soft-danger
            // styling to signal it closes the application.
            UiStyle.ApplySecondary(_btnLoadLots);
            UiStyle.ApplySecondary(_btnLoadOperations);
            UiStyle.ApplySecondary(_btnLoadChar);
            UiStyle.ApplyPrimary(_btnEnterMeasurements);
            UiStyle.ApplyDanger(_btnExit);
            UiStyle.ApplySecondary(_btnDiagnose);

            ApplyTooltips();
        }

        private void ApplyTooltips()
        {
            _tooltip.SetToolTip(_btnLoadLots,         AppTranslations.T("tip_load_lots"));
            _tooltip.SetToolTip(_btnLoadOperations,   AppTranslations.T("tip_load_ops"));
            _tooltip.SetToolTip(_btnLoadChar,         AppTranslations.T("tip_load_char"));
            _tooltip.SetToolTip(_btnEnterMeasurements,AppTranslations.T("tip_enter_meas"));
            _tooltip.SetToolTip(_btnExit,             AppTranslations.T("tip_exit"));
        }

        private void ApplyTranslations()
        {
            Text = AppTranslations.T("vnos_title");
            _lblOddelek.Text = AppTranslations.T("lbl_dept");
            _lblArtikel.Text = AppTranslations.T("lbl_code");
            _lblDatum.Text = AppTranslations.T("lbl_date_from");
            _lblKratki.Text = AppTranslations.T("lbl_shorttext");
            _btnLoadLots.Text = AppTranslations.T("btn_load_lots");
            _lblNaziv.Text = AppTranslations.T("lbl_item_name");
            _lblLots.Text = AppTranslations.T("lbl_lots");
            _btnLoadOperations.Text = AppTranslations.T("btn_load_ops");
            _lblOper.Text = AppTranslations.T("lbl_ops");
            _btnLoadChar.Text = AppTranslations.T("btn_load_char");
            _lblChar.Text = AppTranslations.T("lbl_char");
            _btnEnterMeasurements.Text = AppTranslations.T("btn_enter_meas");
            _btnExit.Text = AppTranslations.T("btn_exit");
            _menuPregled.Text = AppTranslations.T("menu_view");
            _menuZgodovina.Text = AppTranslations.T("menu_history");
            _menuKontSarze.Text = AppTranslations.T("menu_kontsarze");
            ApplyTooltips();
        }

        // Delphi: FormActivate — set date to today-30, load departments
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _dtpDatum.Value = DateTime.Today.AddDays(-30);
            LoadDepartments();
        }

        private void LoadDepartments()
        {
            try
            {
                var depts = SapKontSarzeRepository.GetDepartments();
                _listDepartments.Items.Clear();
                foreach (var d in depts)
                    _listDepartments.Items.Add(d);
            }
            catch (Exception ex)
            {
                ShowStatus(AppTranslations.T("msg_err_dept") + ex.Message);
            }
        }

        // Delphi: OddelkiClick — load sarze from SQL for selected department
        private void OnDepartmentSelected(object sender, EventArgs e)
        {
            if (_listDepartments.SelectedIndex < 0) return;
            var odd = _listDepartments.SelectedItem.ToString();
            try
            {
                var sarze = SapKontSarzeRepository.GetSarze(odd);
                _listLots.Items.Clear();
                _sapLots = null;
                _searchMode = 0;
                foreach (var s in sarze)
                    _listLots.Items.Add(s.Sarza + "  " + s.Tekst);
                ShowStatus(string.Empty);
            }
            catch (Exception ex)
            {
                ShowStatus(AppTranslations.T("msg_err") + ex.Message);
            }
        }

        // Delphi: edit4Click — clicking short-text combo clears artikel and lots
        private void OnKratkiTekstClick(object sender, EventArgs e)
        {
            _txtArtikel.Text = string.Empty;
            _txtNazivIzdelka.Text = string.Empty;
            _listLots.Items.Clear();
            _sapLots = null;
        }

        // Delphi: SarzeIzborClick — selecting a lot clears downstream panels
        private void OnLotSelected(object sender, EventArgs e)
        {
            _listOperations.Items.Clear();
            _listCharacteristics.Items.Clear();
            _selectedLot = null;
            _selectedOperation = null;
            _characteristics = null;
        }

        private void OnOperationSelected(object sender, EventArgs e)
        {
            _listCharacteristics.Items.Clear();
            _characteristics = null;
            _selectedOperation = null;
        }

        // Delphi: Button1Click — load inspection lots from SAP via BAPI_INSPLOT_GETLIST
        private void BtnLoadLots_Click(object sender, EventArgs e)
        {
            ShowStatus(string.Empty);
            _listLots.Items.Clear();
            _sapLots = null;
            _searchMode = 0;
            _txtNazivIzdelka.Text = string.Empty;

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var destination = SapSessionFactory.GetDestination();
                var plant = SapSessionFactory.GetPlant();
                var dateFrom = _dtpDatum.Value.Date;
                var service = new SapInspectionListService();

                if (!string.IsNullOrWhiteSpace(_txtArtikel.Text))
                {
                    _searchMode = 1;
                    var lots = service.GetByMaterial(destination, _txtArtikel.Text.Trim(), plant, dateFrom);
                    _sapLots = lots;
                    if (lots.Count > 0)
                        _txtNazivIzdelka.Text = lots[0].TxtMat;
                    foreach (var l in lots)
                        _listLots.Items.Add(l.LotNumber + "  " + l.ShortText);
                    if (lots.Count == 0)
                        ShowStatus(AppTranslations.T("msg_no_lots"));
                }
                else
                {
                    _searchMode = 2;
                    var prefix = _cmbKratkiTekst.Text.Trim();
                    var allLots = service.GetByPlant(destination, plant, dateFrom);
                    var filtered = new List<SapInspectionListService.InspectionLotEntry>();
                    foreach (var l in allLots)
                    {
                        if (prefix.Length == 0 || l.ShortText.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                            filtered.Add(l);
                    }
                    _sapLots = filtered;
                    foreach (var l in filtered)
                        _listLots.Items.Add(l.LotNumber + "  " + l.ShortText);
                    if (filtered.Count == 0)
                        ShowStatus(AppTranslations.T("msg_no_lots"));
                }
            }
            catch (Exception ex)
            {
                ShowStatus(AppTranslations.T("msg_err") + ex.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        // Delphi: Button2Click — storno check + load operations
        private void BtnLoadOperations_Click(object sender, EventArgs e)
        {
            if (_listLots.SelectedIndex < 0)
            {
                MessageBox.Show(AppTranslations.T("msg_no_lot_sel"));
                return;
            }

            var lotItem = _listLots.SelectedItem.ToString();
            var lot = lotItem.Length >= 12 ? lotItem.Substring(0, 12).Trim() : lotItem.Trim();

            if (_searchMode == 2 && _sapLots != null && _listLots.SelectedIndex < _sapLots.Count)
                _txtNazivIzdelka.Text = _sapLots[_listLots.SelectedIndex].Material;

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var destination = SapSessionFactory.GetDestination();

                var lotService = new SapInspectionLotService();
                var lotResult = lotService.CheckIsOpen(destination, lot, "5");
                foreach (var code in lotResult.StatusCodes)
                {
                    if (code == "I0224")
                    {
                        MessageBox.Show(AppTranslations.T("msg_lot_cancelled"));
                        return;
                    }
                }

                var opService = new SapInspectionOperationService();
                var opResult = opService.ReadOperations(destination, lot);
                _listOperations.Items.Clear();
                foreach (var op in opResult.Operations)
                    _listOperations.Items.Add(op.ToString());

                _selectedLot = lot;
                ShowStatus(string.Empty);
            }
            catch (Exception ex)
            {
                ShowStatus(AppTranslations.T("msg_err") + ex.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        // Delphi: Button3Click — load characteristics via BAPI_INSPOPER_GETDETAIL
        private void BtnLoadChar_Click(object sender, EventArgs e)
        {
            if (_listLots.SelectedIndex < 0)
            {
                MessageBox.Show(AppTranslations.T("msg_no_lot_sel"));
                return;
            }
            if (_listOperations.SelectedIndex < 0)
            {
                MessageBox.Show(AppTranslations.T("msg_no_op_sel"));
                return;
            }

            var opItem = _listOperations.SelectedItem.ToString();
            var opr = opItem.Length >= 4 ? opItem.Substring(0, 4).Trim() : opItem.Trim();
            _selectedOperation = opr;

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var destination = SapSessionFactory.GetDestination();
                var charService = new SapCharacteristicService();
                _characteristics = charService.GetCharacteristics(destination, _selectedLot, opr);
                _listCharacteristics.Items.Clear();

                foreach (var ch in _characteristics)
                {
                    var spk = ch.IsIndividual + "/" + ch.SampleRes;
                    var spt = ch.LowerTol + "-" + ch.UpperTol;
                    _listCharacteristics.Items.Add(
                        ch.InspChar + "  " + ch.SampleSize + "  " + ch.Name + "  " + ch.ControlMethod
                        + " " + spk + " " + spt);
                }
                ShowStatus(string.Empty);
            }
            catch (Exception ex)
            {
                ShowStatus(AppTranslations.T("msg_err") + ex.Message);
                _characteristics = null;
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        // Delphi: Button4Click — build measurement rows and open vpisMer modal
        private void BtnEnterMeasurements_Click(object sender, EventArgs e)
        {
            if (_characteristics == null || _characteristics.Count == 0)
            {
                MessageBox.Show(AppTranslations.T("msg_no_char"));
                return;
            }

            var rows = new List<VpisMerForm.MeasurementRow>();
            var kk = 0;
            foreach (var ch in _characteristics)
            {
                kk++;
                var istm = ch.MeasurementCount;
                for (var nn = 1; nn <= istm; nn++)
                {
                    rows.Add(new VpisMerForm.MeasurementRow
                    {
                        Naziv = ch.Name,
                        StKarakteristike = kk,
                        StMeritve = nn,
                        Tip = ch.ControlMethod == "01" ? "X" : " ",
                        Posam = ch.IsIndividual,
                        LowerTol = ch.LowerTol,
                        UpperTol = ch.UpperTol
                    });
                }
            }

            var initialRmk = _selectedLot == "790000033363" ? "PLAT6" : string.Empty;

            using (var dlg = new VpisMerForm(rows, initialRmk))
            {
                dlg.ShowDialog(this);
                if (!dlg.Saved) return;

                var overallVal = "A";
                var postRows = new List<SapInspectionRecordPostService.CharRow>();
                var seqIdx = 0;
                foreach (var r in dlg.Rows)
                {
                    if (string.IsNullOrEmpty(r.Result) && string.IsNullOrEmpty(r.Eval) && string.IsNullOrEmpty(r.Comment))
                        continue;
                    seqIdx++;
                    var charEntry = _characteristics[r.StKarakteristike - 1];
                    if (r.Eval == "R") overallVal = "R";
                    postRows.Add(new SapInspectionRecordPostService.CharRow
                    {
                        InspChar = charEntry.InspChar,
                        IsIndividual = charEntry.IsIndividual,
                        CharType = charEntry.ControlMethod,
                        ResultValue = r.Result,
                        Valuation = r.Eval,
                        Remark = r.Comment,
                        SequentialIndex = seqIdx
                    });
                }

                if (!IsStep10PostEnabled())
                {
                    MessageBox.Show(string.Format(AppTranslations.T("msg_post_disabled"),
                        dlg.Rmk, postRows.Count, dlg.Rows.Count, overallVal));
                    return;
                }

                if (postRows.Count == 0)
                {
                    MessageBox.Show(AppTranslations.T("msg_no_meas"));
                    return;
                }

                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    var now = DateTime.Now;
                    var sqlEnabled = IsStep11SqlEnabled();

                    var izap = -1L;
                    if (sqlEnabled)
                    {
                        try
                        {
                            izap = SapMeasurementSqlRepository.ZapisVzr(
                                _selectedLot, _selectedOperation, dlg.Rmk, now);
                            if (izap > 0)
                            {
                                foreach (var r in postRows)
                                    SapMeasurementSqlRepository.VpisChr(
                                        izap, r.SequentialIndex, r.InspChar,
                                        r.IsIndividual, r.CharType,
                                        r.ResultValue, r.Valuation, r.Remark);
                            }
                        }
                        catch (Exception exSql)
                        {
                            ShowStatus(AppTranslations.T("msg_sql_vzr_err") + exSql.Message);
                        }
                    }

                    var svc = new SapInspectionRecordPostService();
                    var postResult = svc.PostAllResults(
                        SapSessionFactory.GetDestination(),
                        _selectedLot, _selectedOperation, dlg.Rmk,
                        SapSessionFactory.GetPlant(), now, overallVal, postRows);

                    if (sqlEnabled && postResult.Success && izap > 0)
                    {
                        try { SapMeasurementSqlRepository.Prenos(izap); }
                        catch (Exception exSql) { ShowStatus(AppTranslations.T("msg_sql_pren_err") + exSql.Message); }
                    }

                    var sqlNote = sqlEnabled
                        ? (izap > 0 ? "\nSQL ZAP=" + izap + (postResult.Success ? " (prenos=X)" : " (prenos='')") : "\nSQL: ZapisVzr failed")
                        : "\n(sap.step11.enable_sql_write=false)";

                    if (postResult.Success)
                        MessageBox.Show(AppTranslations.T("msg_sap_ok") + "\n" + postResult.Message + sqlNote);
                    else
                        MessageBox.Show(AppTranslations.T("msg_sap_err") + "\n" + postResult.Message + sqlNote);
                }
                catch (Exception ex)
                {
                    ShowStatus(AppTranslations.T("msg_err_post") + ex.Message);
                    MessageBox.Show(AppTranslations.T("msg_err_post") + ex.Message);
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }
        }

        // Temporary diagnostic: dumps all actual BAPI field names + values so we can fix field-name guesses
        private void BtnDiagnose_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var destination = SapSessionFactory.GetDestination();
                var plant = SapSessionFactory.GetPlant();
                var sb = new StringBuilder();

                sb.AppendLine("=== BAPI_INSPLOT_GETLIST import parameters ===");
                try
                {
                    var fList = destination.Repository.CreateFunction("BAPI_INSPLOT_GETLIST");
                    var meta = fList.Metadata;
                    for (var i = 0; i < meta.ParameterCount; i++)
                    {
                        var p = meta[i];
                        sb.AppendLine(string.Format("  [{0,3}] {1,-30} dir={2}", i, p.Name, p.Direction));
                    }

                    sb.AppendLine();
                    sb.AppendLine("=== INSPLOT_LIST fields (PLANT only, MAX_ROWS=1) ===");
                    fList.SetValue("PLANT", plant);
                    fList.SetValue("MAX_ROWS", 1);
                    fList.Invoke(destination);
                    var listTable = fList.GetTable("INSPLOT_LIST");
                    sb.AppendLine("RowCount: " + listTable.RowCount);
                    if (listTable.RowCount > 0)
                    {
                        listTable.CurrentIndex = 0;
                        var ltList = listTable.Metadata.LineType;
                        for (var i = 0; i < ltList.FieldCount; i++)
                        {
                            var name = ltList[i].Name;
                            string val;
                            try { val = (listTable.GetString(name) ?? string.Empty).Trim(); }
                            catch { val = "?ERR"; }
                            sb.AppendLine(string.Format("  [{0,3}] {1,-30} = '{2}'", i + 1, name, val));
                        }
                    }
                }
                catch (Exception exList)
                {
                    sb.AppendLine("NAPAKA: " + exList.Message);
                }

                sb.AppendLine();
                sb.AppendLine("=== BAPI_INSPOPER_RECORDRESULTS parameters ===");
                try
                {
                    var fRec = destination.Repository.CreateFunction("BAPI_INSPOPER_RECORDRESULTS");
                    var metaRec = fRec.Metadata;
                    for (var i = 0; i < metaRec.ParameterCount; i++)
                    {
                        var pm = metaRec[i];
                        sb.AppendLine(string.Format("  [{0,3}] {1,-30} dir={2}", i, pm.Name, pm.Direction));
                    }
                    sb.AppendLine();
                    sb.AppendLine("=== INSPPOINTDATA structure fields ===");
                    try
                    {
                        var ipMeta = fRec.GetStructure("INSPPOINTDATA").Metadata;
                        for (var i = 0; i < ipMeta.FieldCount; i++)
                            sb.AppendLine(string.Format("  [{0,3}] {1,-30} type={2}", i + 1, ipMeta[i].Name, ipMeta[i].DataType));
                    }
                    catch (Exception exIp) { sb.AppendLine("  INSPPOINTDATA: " + exIp.Message); }

                    foreach (var tblName in new[] { "CHAR_RESULTS", "SINGLE_RESULTS", "SAMPLE_RESULTS" })
                    {
                        sb.AppendLine();
                        sb.AppendLine("=== " + tblName + " fields ===");
                        try
                        {
                            var lt = fRec.GetTable(tblName).Metadata.LineType;
                            for (var i = 0; i < lt.FieldCount; i++)
                                sb.AppendLine(string.Format("  [{0,3}] {1,-30} type={2}", i + 1, lt[i].Name, lt[i].DataType));
                        }
                        catch (Exception exTbl) { sb.AppendLine("  " + tblName + ": " + exTbl.Message); }
                    }
                }
                catch (Exception exRec)
                {
                    sb.AppendLine("BAPI_INSPOPER_RECORDRESULTS meta error: " + exRec.Message);
                }

                if (string.IsNullOrEmpty(_selectedLot))
                {
                    sb.AppendLine();
                    sb.AppendLine(AppTranslations.T("diag_select_lot"));
                }
                else
                {
                    var lot = _selectedLot.PadLeft(12, '0');

                    sb.AppendLine();
                    sb.AppendLine("=== BAPI_INSPLOT_GETOPERATIONS: INSPOPER_LIST (row 1) ===");
                    var f1 = destination.Repository.CreateFunction("BAPI_INSPLOT_GETOPERATIONS");
                    f1.SetValue("NUMBER", lot);
                    f1.Invoke(destination);
                    var opTable = f1.GetTable("INSPOPER_LIST");
                    sb.AppendLine("RowCount: " + opTable.RowCount);
                    if (opTable.RowCount > 0)
                    {
                        opTable.CurrentIndex = 0;
                        var lt1 = opTable.Metadata.LineType;
                        for (var i = 0; i < lt1.FieldCount; i++)
                        {
                            var name = lt1[i].Name;
                            string val;
                            try { val = (opTable.GetString(name) ?? string.Empty).Trim(); }
                            catch { val = "?ERR"; }
                            if (val.Length > 0)
                                sb.AppendLine(string.Format("  [{0,3}] {1,-30} = '{2}'", i + 1, name, val));
                        }
                    }

                    if (!string.IsNullOrEmpty(_selectedOperation))
                    {
                        sb.AppendLine();
                        sb.AppendLine("=== BAPI_INSPOPER_GETDETAIL: CHAR_REQUIREMENTS (row 1) ===");
                        var f2 = destination.Repository.CreateFunction("BAPI_INSPOPER_GETDETAIL");
                        f2.SetValue("INSPLOT", lot);
                        f2.SetValue("INSPOPER", _selectedOperation);
                        f2.SetValue("READ_CHAR_REQUIREMENTS", "X");
                        f2.SetValue("READ_INSPPOINTS", "X");
                        f2.Invoke(destination);
                        var charTable = f2.GetTable("CHAR_REQUIREMENTS");
                        sb.AppendLine("RowCount: " + charTable.RowCount);
                        if (charTable.RowCount > 0)
                        {
                            charTable.CurrentIndex = 0;
                            var lt2 = charTable.Metadata.LineType;
                            for (var i = 0; i < lt2.FieldCount; i++)
                            {
                                var name = lt2[i].Name;
                                string val;
                                try { val = (charTable.GetString(name) ?? string.Empty).Trim(); }
                                catch { val = "?ERR"; }
                                sb.AppendLine(string.Format("  [{0,3}] {1,-30} = '{2}'", i + 1, name, val));
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine();
                        sb.AppendLine(AppTranslations.T("diag_select_op"));
                    }
                }

                var dlg = new Form
                {
                    Text = "BAPI Diagnostika - field names",
                    Width = 720, Height = 620,
                    StartPosition = FormStartPosition.CenterParent
                };
                var txt = new TextBox
                {
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    Dock = DockStyle.Fill,
                    Font = new Font("Courier New", 9),
                    Text = sb.ToString(),
                    WordWrap = false
                };
                dlg.Controls.Add(txt);
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(AppTranslations.T("msg_err") + ex.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private static Icon CreateAppIcon()
        {
            using (var bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                // SAP-blue background
                g.FillRectangle(new SolidBrush(Color.FromArgb(0, 112, 192)), 0, 0, 32, 32);
                // SAP-gold accent bar at bottom
                g.FillRectangle(new SolidBrush(Color.FromArgb(247, 181, 0)), 0, 26, 32, 6);
                // "SAP" centered in white
                using (var f = new Font("Arial Black", 11, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    var sz = g.MeasureString("SAP", f);
                    var x = (32 - sz.Width) / 2f;
                    g.DrawString("SAP", f, Brushes.White, x, 5f);
                }

                IntPtr hIcon = bmp.GetHicon();
                try { return (Icon)Icon.FromHandle(hIcon).Clone(); }
                finally { DestroyIcon(hIcon); }
            }
        }

        private static bool IsStep10PostEnabled()
        {
            var v = ConfigurationManager.AppSettings["sap.step10.enable_post"];
            return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStep11SqlEnabled()
        {
            var v = ConfigurationManager.AppSettings["sap.step11.enable_sql_write"];
            return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private void ShowStatus(string msg)
        {
            _lblStatus.Text = msg;
        }
    }
}
