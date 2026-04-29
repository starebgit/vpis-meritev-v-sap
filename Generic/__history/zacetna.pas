unit zacetna;

interface

uses
  Winapi.Windows, Winapi.Messages, System.SysUtils, System.Variants, System.Classes, Vcl.Graphics,
  Vcl.Controls, Vcl.Forms, Vcl.Dialogs, Vcl.StdCtrls, Vcl.ExtCtrls,Data.Win.ADODB,
  Data.DB, deklar, Vcl.Grids, Vcl.Samples.Spin, Vcl.Menus, ComObj;

  const
    DODK = 6 ;
type
  TFzacetna = class(TForm)
    Panel1: TPanel;
    Label1: TLabel;
    List1: TListBox;
    Delavnice: TLabel;
    List2: TListBox;
    Label2: TLabel;
    List3: TListBox;
    ADOQuery1: TADOQuery;
    list4: TListBox;
    ADOQuery2: TADOQuery;
    Label3: TLabel;
    Panel2: TPanel;
    Panel3: TPanel;
    Karakti: TStringGrid;
    Label4: TLabel;
    list5: TListBox;
    attri: TStringGrid;
    SpinEdit1: TSpinEdit;
    Button2: TButton;
    SpinEdit2: TSpinEdit;
    Label5: TLabel;
    Label6: TLabel;
    Button1: TButton;
    Button3: TButton;
    MainMenu1: TMainMenu;
    Lokalnabaza1: TMenuItem;
    Linijekode1: TMenuItem;
    Kodaare1: TMenuItem;
    arekarakteristike1: TMenuItem;
    Button4: TButton;
    Button5: TButton;
    Meritve1: TMenuItem;
    Button6: TButton;
    Button7: TButton;
    Panel4: TPanel;
    Postaje1: TMenuItem;
    Stroji1: TMenuItem;
    procedure List1Click(Sender: TObject);
    procedure FormActivate(Sender: TObject);
    procedure List2Click(Sender: TObject);
    procedure List3Click(Sender: TObject);
    procedure list4Click(Sender: TObject);
    procedure list5Click(Sender: TObject);
    procedure Button2Click(Sender: TObject);
    procedure Button1Click(Sender: TObject);
    procedure Button3Click(Sender: TObject);
    procedure Linijekode1Click(Sender: TObject);
    procedure Kodaare1Click(Sender: TObject);
    procedure arekarakteristike1Click(Sender: TObject);
    procedure Button4Click(Sender: TObject);
    procedure Button5Click(Sender: TObject);
    procedure Meritve1Click(Sender: TObject);
    procedure Button6Click(Sender: TObject);
    procedure Button7Click(Sender: TObject);
    procedure KaraktiClick(Sender: TObject);
    procedure Postaje1Click(Sender: TObject);
    procedure Stroji1Click(Sender: TObject);
  private
    listEnote : Tlist ;
    ListSkup : Tlist ;
    ListStroji : Tlist ;
    nkar1,nkar2 : Integer ;
    stcl,stvp : integer ;
    srz : string ;
    orod : string ;
    lokalb : boolean ;
    procedure FillDelavnice ;
    procedure FillDelEnote ;
    procedure FillStroji ;
    Procedure isciKoda ;
    procedure Pocisti1(list : Tlist) ;
    procedure BrisiTabelo ;
    function preveriData : boolean  ;
    procedure ZapisSAP ;
    Procedure nanovo ;
    Procedure lokalkoda ;
    function PreveriMer(jj,kk: integer) : boolean ;
  public
    { Public declarations }
  end;

var
  Fzacetna: TFzacetna;

implementation

{$R *.dfm}

uses sap, ComPort, lokalBaza, lokalmeritve, postaje, stroji;



procedure TFzacetna.Button1Click(Sender: TObject);
 var bb : boolean ;
      i : integer ;
begin
  screen.Cursor := crHourGlass;
  // for i := 1 to MSTROJ do strvkl[i] := Fsinapro.PreveriStroj(i) ;
  // ZapisSemafor ;
  bb := Preveridata ;
  if bb then
  begin
    try
      ZapisSAP ;
      nanovo
    except
    end;
  end;
  screen.Cursor := crDefault;
end;


function TFzacetna.PreveriMer(jj,kk: integer) : boolean ;
    var xx,sp,zg : single ;
 begin
   try
     xx := StrToFloat(karakti.Cells[kk +DODK,jj]) ;
     sp := StrToFloat(karakti.Cells[2,jj]) ;
     zg := StrToFloat(karakti.Cells[3,jj]) ;
     if (xx-sp > -0.0001) and (zg-xx > -0.0001) then result := true else result := false
   except
     result := false
   end;
 end ;

procedure TFzacetna.Stroji1Click(Sender: TObject);
begin
  Fstroji.pogled ;
end;

// priprava za zapis v SAP
procedure TFzacetna.ZapisSAP ;
   var ii,kk : integer ;
       j,jj,stkar,vzz: Integer;
       pv : ^karmer ;
       karlist : Tlist ;
       merl,odl : string ;
       dd : TdateTime ;
       evalList : Tlist ;
       px : ^evaluac ;
       vred : boolean ;
       nazivp : string ;
       opr : string ;
       slb,stnp : integer ;

  procedure  cistiList1 ;
    var i : integer ;
  begin
    if assigned(karlist) then
    begin
      for i := 0 to (karlist.Count - 1) do
      begin
        pv := karlist[i];
        Dispose(pv);
      end;
      karlist.Free ;
    end;
  end;

  procedure  cistiListE ;
    var i : integer ;
  begin
    if assigned(evallist) then
    begin
      for i := 0 to (evallist.Count - 1) do
      begin
        px := evallist[i];
        Dispose(px);
      end;
      evallist.Free ;
    end;
  end;

begin
  // vzz := StrToInt(karakti.Cells[3,1]) ;

   opr := Fsap.BeriOperac(srz)  ;
   nazivp := 'Strojna' ;
 //  merl := ListBox1.Items[ListBox1.ItemIndex] ;    // merilec
   karlist := Tlist.Create ;
   ii := 0 ;
   odl := 'A' ;
 //  stvz := 12 ;
   evalList := TList.Create ;
   for jj := 1 to nkar1 do
   begin
     new(px) ;
     px^.ev := 'A' ;
     px^.st := 0 ;
     for kk := 1 to stcl do
     begin
       new(pv) ;
       pv^.stkar := karakti.Cells[0,jj];
       pv^.stmer := ii+1 ;
       pv^.skupi := 'X' ;
       pv^.tip := '01';
       vred := preverimer(jj,kk-1) ;
       if  vred then
       begin
         pv^.eval := 'A'
       end else
       begin
         pv^.eval := 'R' ;
         px^.ev := 'R' ;
         px^.st := px^.st + 1 ;
       end ;
       pv^.merit :=  karakti.Cells[kk+DODK-1,jj] ;
       karlist.Add(pv) ;
       Inc(ii) ;
     end ;
     evalList.add(px) ;
   end ;

   for jj := 1 to nkar2 do
   begin
     slb := StrToInt(attri.cells[2,jj]);
     new(px) ;
     if slb = 0 then px^.ev := 'A' else px^.ev := 'R';
     px^.stkar := attri.cells[0,jj];
     px^.st := slb ;
     for kk := 1 to stvp do
     begin
       new(pv) ;
       pv^.stkar := attri.cells[0,jj];
       pv^.stmer := ii+1 ;
       pv^.skupi := '' ;
       pv^.tip := '02';
       stnp := StrToInt(attri.cells[2,jj]) ;
       pv^.stevnp := stnp ;
       if stnp > 0 then pv^.eval :=  'R'  else pv^.eval := 'A';
       inc(ii) ;
       karlist.Add(pv) ;
     end ;
     new(pv) ;

     evalList.add(px) ;
   end;

   dd := now ;
   if karlist.Count > 0 then Fsap.zapis(srz,opr,nazivp,merl,odl,orod,dd,karlist,evalList)  ;
//   label5.Caption := IntToStr(stvz) ;
   CistiListE ;
   CistiList1 ;
  // merex := false ;
end;

procedure TFzacetna.Button2Click(Sender: TObject);
  var stk,row,col,std : Integer ;
      mer : single ;
begin
  stk :=  spinEdit1.value ;
   mer := FComPort.preberi(stk) ;
   row := Karakti.Row ;
   col := karakti.Col ;
   std := spinedit2.Value ;
   karakti.Cells[col,row] := FloatTostrF(mer,ffFixed,10,std) ;
end;

procedure TFzacetna.Button3Click(Sender: TObject);
begin
  Application.Terminate ;
end;

procedure TFzacetna.Button4Click(Sender: TObject);
  var list : Tlist ;
      i : integer ;
      pp : ^zapis ;
begin
   lokalb := true ;
  // button1.Enabled := false ;
   button5.Enabled := true ;
   listStroji := Tlist.Create ;
   FlokalBaza.getlinije(listStroji) ;
   for i := 0 to liststroji.count-1 do
   begin
     pp := liststroji[i] ;
     list3.items.add(pp^.naziv) ;
   end;
end;

procedure TFzacetna.Button5Click(Sender: TObject);
  var idm : integer ;
      i,j,stv : Integer ;
      poz,eval,opo,oro : string ;
      xx : single ;
      bb : boolean ;
begin
  oro := '' ;
  idm := FlokalMeritve.ZapisSeznam(srz,oro) ;
  for i := 1 to karakti.RowCount-1 do
  begin
    poz := karakti.Cells[0,i] ;

    for j := 1 to karakti.ColCount- DODK do
    begin
      xx := StrToFloat(karakti.Cells[j+DODK-1,i]) ;
      bb := PreveriMer(i,j-1) ;
      if bb then eval := 'A' else eval := 'R' ;
      FlokalMeritve.ZapisMer1(idm,j,poz,eval,xx) ;
    end;
  end;
  for i := 1 to attri.RowCount-1 do
  begin
    poz := attri.Cells[0,i] ;
    stv := StrToIntDef(attri.Cells[2,i],0) ;
    if stv = 0 then eval := 'A' else eval := 'R' ;
    opo := attri.Cells[3,i] ;
    FlokalMeritve.ZapisMer2(idm,stv,poz,eval,opo) ;
  end;
end;


procedure TFzacetna.Button6Click(Sender: TObject);
  var excel : variant ;
      i,j : Integer ;
begin
  excel := GetActiveOLEObject('Excel.application');
  for i := 1 to 7 do
    for j := 1 to 10 do
    begin
      karakti.Cells[j+DODK-1,i] := excel.cells[i,j]
    end;
end;

procedure TFzacetna.Button7Click(Sender: TObject);
begin
  FLokalMeritve.LokalSap ;
end;

function TFzacetna.preveriData : boolean  ;
  var jj,kk : Integer ;
      xx : single ;
      bb : boolean ;
begin
  bb := true ;
  for jj  := 1 to nkar1 do
  begin
    if not bb  then break ;
    for kk := 1 to stcl do
    begin
      try
        xx :=  StrToFloat(karakti.Cells[kk+DODK-1,jj]) ;
      except
        //ShowMessage('Podatki o meritvah niso vredu') ;
        karakti.row := jj ;
        karakti.col := kk+DODK-1 ;
        ShowMessage('Podatki o meritvah niso vredu') ;
        karakti.SetFocus ;
        bb := false ;
        break
      end;
    end;
  end;
  if bb then
  begin
    for jj := 1 to nkar2 do
      if trim(attri.Cells[2,jj]) = '' then
      begin
        ShowMessage('Podatki o atrib. karakteristikah niso vpisani') ;
        bb := false ;
        break
      end;
  end;
  result := bb ;
end;


procedure TFzacetna.FillDelavnice ;
  var ii : integer ;
      pp : ^zapis ;
      ident : integer ;
begin
   //Delavnice.Caption := 'Delavnice' ;
   ii := List1.ItemIndex ;
   pp := ListEnote[ii] ;
   ident := pp^.ident ;
   Pocisti1(ListSkup) ;
   ListSkup := Tlist.Create ;
   AdoQuery1.SQL.Clear ;
   AdoQuery1.sql.Add('Select * from t_pod_delavnica where st_OrgEnota_ID = :ID') ;
   AdoQuery1.Parameters[0].value := ident ;
   AdoQuery1.Parameters[0].name := 'ID' ;
   AdoQuery1.Open ;
   Adoquery1.First ;
   List2.Items.Clear ;
   while Not AdoQuery1.Eof  do
   begin
     List2.Items.Add(Adoquery1.FieldByName('naz_delavnica').value) ;
     new(pp) ;
     pp^.ident := Adoquery1.FieldByName('st_delavnica_id').value ;
     pp^.naziv := Adoquery1.FieldByName('naz_delavnica').value  ;
     ListSkup.Add(pp) ;
   //  dispose(pp) ;
     AdoQuery1.Next ;
   end;
   Adoquery1.Close ;
end;

procedure TFzacetna.FillDelEnote ;
  var pp : ^zapis ;
begin
   ListEnote := Tlist.create ;
   AdoQuery1.SQL.Clear ;
 //  Showmessage('1a') ;
   AdoQuery1.sql.Add('Select * from t_pod_orgenota') ;
   try
      AdoQuery1.Open ;
   except
       on E: Exception do ShowMessage(E.Message) ;
   end;
 //  Showmessage('1b') ;
 //  Adoquery1.First ;
   if adoQuery1.eof then ShowMessage('ne morem odperti') ;
   while Not AdoQuery1.Eof  do
   begin
     List1.Items.Add(Adoquery1.FieldByName('naz_orgenota').value) ;
     new(pp) ;
     pp^.ident := Adoquery1.FieldByName('st_orgenota_id').value ;
     pp^.naziv := Adoquery1.FieldByName('naz_orgenota').value  ;
     ListEnote.Add(pp) ;
     AdoQuery1.Next ;
   end;
   Adoquery1.Close ;
end;

procedure TFzacetna.FillStroji ;
  var ii : integer ;
      pp : ^zapis ;
      ident : integer ;
      str : int64 ;
begin
   //Label2.Caption := 'Stroji' ;
   ii := List2.ItemIndex ;
   pp := ListSkup[ii] ;
   ident := pp^.ident ;
   Pocisti1(ListStroji) ;
   Liststroji := Tlist.Create ;
   AdoQuery1.SQL.Clear ;
   {if izbor_del = 0 then AdoQuery1.sql.Add('Select * from _data_t_pod_stroj where (st_delavnica_ID = :ID)')
                    else AdoQuery1.sql.Add('Select * from DimStroj where (TEDStevilka = :ID) and (datumStop IS NULL)') ;   }
   AdoQuery1.sql.Add('Select * from t_pod_stroj where (st_delavnica_ID = :ID)') ;
   AdoQuery1.Parameters[0].value := ident ;
   AdoQuery1.Parameters[0].name := 'ID' ;
   AdoQuery1.Open ;
   Adoquery1.First ;
   List3.Items.Clear ;
   while Not AdoQuery1.Eof  do
   begin
     str := Adoquery1.FieldByName('st_stroj_ID').value ;
     AdoQuery2.SQL.Clear ;
     AdoQuery2.sql.Add('Select * from t_del_stanjestroj where st_stroj_ID  = :ST') ;
     AdoQuery2.Parameters[0].value := str ;
     AdoQuery2.Parameters[0].name := 'ST' ;
     AdoQuery2.Open ;
     if not adoquery2.FieldByName('cas_zacetek').isnull then
     begin
       new(pp) ;
       pp^.ident := str ;
       pp^.naziv := Adoquery1.FieldByName('naz_stroja').value  ;
       List3.Items.Add(pp^.naziv) ;
       ListStroji.Add(pp) ;
     end ;
     adoquery2.Close ;
     AdoQuery1.Next ;
   end;
   Adoquery1.Close ;
end;

Procedure TFzacetna.lokalkoda ;
  var ii : integer ;
      pp : ^zapis ;
      ident : integer ;
      lista : TstringList ;
      deln,kd : string ;
      jx,ll : integer ;
begin
   ii := List3.ItemIndex ;
   pp := Liststroji[ii] ;
   orod := pp^.naziv ;
   ident := pp^.ident ;
   lista := Tstringlist.Create ;
   FlokalBaza.getKode(ident,lista) ;
   list4.Items := lista ;
   Lista.free ;
end ;

procedure TFzacetna.Meritve1Click(Sender: TObject);
begin
  FLokalMeritve.prikaz ;
end;

Procedure TFzacetna.isciKoda ;
  var ii : integer ;
      pp : ^zapis ;
      ident : integer ;
      lista : TstringList ;
      deln,kd : string ;
      jx,ll : integer ;
begin
   ii := List3.ItemIndex ;
   pp := Liststroji[ii] ;
   orod := pp^.naziv ;
   ident := pp^.ident ;

    AdoQuery1.SQL.Clear ;
   {if izbor_del = 0 then AdoQuery1.sql.Add('Select * from _data_t_pod_stroj where (st_delavnica_ID = :ID)')
                    else AdoQuery1.sql.Add('Select * from DimStroj where (TEDStevilka = :ID) and (datumStop IS NULL)') ;   }
   AdoQuery1.sql.Add('Select * from t_del_proiz where (st_stroj_ID = :ID) and (cas_vpis >= :DAT)') ;
   AdoQuery1.Parameters[0].value := ident ;
   AdoQuery1.Parameters[0].name := 'ID' ;
   AdoQuery1.Parameters[1].value := date ;
   AdoQuery1.Parameters[1].name := 'DAT' ;
   AdoQuery1.Open ;
   Adoquery1.First ;
   lista := TstringList.Create ;
   while not AdoQuery1.eof do
   begin
     deln := Adoquery1.FieldByName('st_delnal_id').Value ;
     if lista.IndexOf(deln) < 0 then  lista.Add(deln) ;
     Adoquery1.next ;
   end;
   Adoquery1.Close ;
   list4.Items.Clear ;
   for ii  := 0 to lista.Count - 1 do
   begin
     deln := lista[ii] ;
     AdoQuery1.SQL.Clear ;
     AdoQuery1.sql.Add('Select * from t_pod_delnalog where st_delnal_ID = :ID') ;
     AdoQuery1.Parameters[0].value := deln ;
     AdoQuery1.Parameters[0].name := 'ID' ;
     AdoQuery1.Open ;
     kd := Adoquery1.FieldByName('st_izdelek_id').Value ;
     Adoquery1.Close ;

     AdoQuery1.SQL.Clear ;
     AdoQuery1.sql.Add('Select * from t_pod_izdelek where st_izdelek_ID = :ID') ;
     AdoQuery1.Parameters[0].value := kd ;
     AdoQuery1.Parameters[0].name := 'ID' ;
     AdoQuery1.Open ;
     kd := Adoquery1.FieldByName('klasifikacija').Value ;
     Adoquery1.Close ;
     jx := pos('/',kd) ;
     if jx > 0 then
     begin
       ll := length(kd) ;
       kd[ll] := '0' ;
     end;

     list4.Items.Add(kd)
   end;

end;

procedure TFzacetna.KaraktiClick(Sender: TObject);
  var row : Integer ;
      ss : string ;
begin
  if karakti.Col = DODK-1 then
  begin
    row := karakti.Row ;
    ss := Karakti.Cells[DODK-1,row] ;
    if ss = '' then Karakti.Cells[DODK-1,row] := 'X'
               else Karakti.Cells[DODK-1,row] := ''
  end;
end;

procedure TFzacetna.Kodaare1Click(Sender: TObject);
begin
  FlokalBaza.KodPrikaz ;
end;

procedure TFzacetna.FormActivate(Sender: TObject);
begin
  FillDelEnote ;
  panel2.Color := $02FFCCFF ;
  karakti.Cells[0,0] := 'Pozicija' ;
  karakti.Cells[1,0] := 'Naziv' ;
  karakti.Cells[2,0] := 'Sp. meja' ;
  karakti.Cells[3,0] := 'Zg. meja' ;
  karakti.Cells[4,0] := 'Metoda' ;
  karakti.Cells[5,0] := 'Oznaka' ;
  karakti.colwidths[1] := 140 ;
  attri.Cells[0,0] := 'Pozicija' ;
  attri.Cells[1,0] := 'Naziv' ;
  attri.Cells[2,0] := 'Slabi' ;
  attri.Cells[3,0] := 'Opombe' ;
  attri.colwidths[1] := 160 ;
  attri.colwidths[3] := 260 ;
  lokalb := false ;
end;

procedure TFzacetna.Linijekode1Click(Sender: TObject);
begin
  FlokalBaza.LinPrikaz ;
end;

procedure TFzacetna.List1Click(Sender: TObject);
begin
  FillDelavnice ;
end;

procedure TFzacetna.List2Click(Sender: TObject);
begin
  FillStroji ;
end;

procedure TFzacetna.List3Click(Sender: TObject);
begin
  if lokalb then lokalKoda else IsciKoda ;
  List5.Items.Clear ;
  brisiTabelo ;
end;

procedure TFzacetna.arekarakteristike1Click(Sender: TObject);
begin
  Flokalbaza.SarPrikaz ;
end;

procedure TFzacetna.BrisiTabelo ;
  var i,j : integer ;
begin
  for i := 1 to  karakti.rowcount-1 do
    for j := 0 to karakti.Colcount-1 do karakti.cells[j,i] := '' ;
  for i := 1 to  attri.rowcount-1 do
    for j := 0 to attri.Colcount-1 do attri.cells[j,i] := ''

end;

procedure TFzacetna.list4Click(Sender: TObject);
  var kd : string ;
      listsrz : TstringList ;
begin
  listsrz := TstringList.Create ;
  kd := List4.Items[list4.ItemIndex] ;
//  kd := '00055.600.38' ;
  if lokalb then Flokalbaza.getsarze(kd,listsrz) else Fsap.getKonsarza(kd,date-1009,listsrz)   ;
  list5.Items := listsrz ;
  listsrz.Free ;
end;


Procedure TFzacetna.nanovo ;
  var i,j : Integer ;
begin
  for j := DODK+1 to karakti.ColCount-1 do karakti.Cells[j,0] := '' ;
  for i := 1  to Karakti.rowcount-1 do
    for j :=  0 to karakti.ColCount-1 do
      karakti.Cells[j,i] := '' ;
  for i := 1  to attri.rowcount-1 do
    for j :=  0 to attri.ColCount-1 do
      attri.Cells[j,i] := '' ;
  karakti.repaint ;
  {Image1.Canvas.FillRect(Image1.Canvas.Cliprect) ;
  Image2.Canvas.FillRect(Image2.Canvas.Cliprect) ;
  Image3.Canvas.FillRect(Image3.Canvas.Cliprect) ;
  Image4.Canvas.FillRect(Image4.Canvas.Cliprect) ; }
end;

procedure TFzacetna.list5Click(Sender: TObject);
  var listkar1 : tlist ;
      listkar2 : tlist ;
      pp : ^karak ;
      pk : ^atrib ;
      ii : integer ;
begin
  srz := List5.Items[list5.ItemIndex] ;
  listkar1 := Tlist.create ;
  listkar2 := Tlist.create ;
  if lokalb then Flokalbaza.GetKar(srz,stcl,stvp,listkar1,listkar2)
            else Fsap.getkarakt(srz,stcl,stvp,listkar1,listkar2) ;
  karakti.RowCount := listkar1.count+1 ;
  karakti.colcount := DODK + stcl ;
  nkar1 := listkar1.count ;
  nkar2 := listkar2.Count ;
  for ii := 0 to listkar1.count-1 do
  begin
    pp := listkar1[ii] ;
    karakti.Cells[0,ii+1] := pp^.poz ;
    karakti.Cells[1,ii+1] := pp^.naziv ;
    karakti.Cells[2,ii+1] := pp^.spmeja ;
    karakti.Cells[3,ii+1] := pp^.zgmeja ;
    karakti.Cells[4,ii+1] := pp^.metoda ;
  end;
  for ii := 1 to stcl do karakti.Cells[DODK-1+ii,0] := 'Meritev ' + intTostr(ii) ;

  attri.RowCount := listkar2.Count +1 ;
  for ii := 0 to listkar2.count-1 do
  begin
    pk := listkar2[ii] ;
    attri.Cells[0,ii+1] := pk^.poz ;
    attri.Cells[1,ii+1] := pk^.naziv ;
  end;
  Listkar1.Free ;
  Listkar2.Free ;
end;

procedure TFzacetna.Pocisti1(list : Tlist) ;
  var i : integer ;
      pp : ^zapis ;
begin
  if assigned(list) then
  begin
    for i := 0 to (List.Count - 1) do
    begin
      pp := List[i];
      Dispose(pp);
    end;
    List.Free ;
  end;
end;

procedure TFzacetna.Postaje1Click(Sender: TObject);
begin
  Fpostaje.pregled ;
end;

end.
