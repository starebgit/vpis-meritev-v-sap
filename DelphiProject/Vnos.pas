//Zaèetni modul programa

unit Vnos;

interface

uses
  Winapi.Windows, Winapi.Messages, System.SysUtils, System.Variants, System.Classes, Vcl.Graphics,
  Vcl.Controls, Vcl.Forms, Vcl.Dialogs, Vcl.StdCtrls, Vcl.ExtCtrls,
  SAPFunctionsOCX_TLB, Vcl.OleCtrls, SAPLogonCtrl_TLB, VpisKar, single, KonTocke, VpisSkup, Sqlsarze,pomoc,PrijavaSAP, deklar;

type
  singler = record
    sarza : string ;
    oper : string ;
    karak : string ;
    eval : char ;
    zapored : integer ;
  end;
   sarz = record
     kd : string ;
     naziv : string ;
   end;
  TFvnos = class(TForm)
    Panel1: TPanel;
    Label1: TLabel;
    Edit1: TEdit;
    Edit2: TEdit;
    Label2: TLabel;
    Label3: TLabel;
    SarzeIzbor: TListBox;
    SAPLogonControl1: TSAPLogonControl;
    SAPFunctions1: TSAPFunctions;
    Button1: TButton;
    Label4: TLabel;
    OperIzbor: TListBox;
    Button2: TButton;
    Label5: TLabel;
    KarakIzbor: TListBox;
    Button3: TButton;
    Button4: TButton;
    Label6: TLabel;
    Label7: TLabel;
    Edit3: TEdit;
    Button5: TButton;
    Label8: TLabel;
    edit4: TComboBox;
    Button6: TButton;
    Button8: TButton;
    Oddelki: TListBox;
    procedure Button1Click(Sender: TObject);
    procedure Button2Click(Sender: TObject);
    procedure Button3Click(Sender: TObject);
    procedure FormActivate(Sender: TObject);
    procedure Button4Click(Sender: TObject);
    procedure Button5Click(Sender: TObject);
    procedure SarzeIzborClick(Sender: TObject);
    procedure edit4Click(Sender: TObject);
    procedure Button6Click(Sender: TObject);
    procedure Button8Click(Sender: TObject);
    procedure OddelkiClick(Sender: TObject);
  private
    tabl,tabv : variant ;
    srz : string ;                 // Kontrolna šarža
    opr : string ;                 // štev. operacije
    connection : variant ;
    nazivp : string ;
    ddat, ttim : TdateTime ;
    spx : integer ;
    nc : integer ;
    srzLista : Tlist ;
    plant : String ;
    procedure Prijava(var connection : variant) ;
    function Getmat(mt,opr : string) : string ;
    Procedure GetIzbsar(var listSr : tstringList) ;
  public
    Function Zapis(srz,opr,nazivp,odl : string ; dd : TdateTime ; karlist : Tlist) : boolean ;
  end;



var
  Fvnos: TFvnos;

implementation

{$R *.dfm}

uses vpisMer, odlocitev, SqlTable;

function TFvnos.Getmat(mt,opr : string) : string ;
  var funct1 : variant ;
begin
  SAPFunctions1.Connection := Idispatch(Connection);
  funct1 := SAPFunctions1.add('ZRFC_CONV_MATNR_GET');
  funct1.exports('RFC_INPUT').value := mt ;
  funct1.exports('RFC_IO_KENNZ').value := opr ;
  Funct1.call ;
  result := funct1.imports('RFC_OUTPUT').value ;
end;


procedure TFvnos.OddelkiClick(Sender: TObject);
  var listsrz : TstringList ;
      odd : string ;
begin
   odd := oddelki.Items[oddelki.ItemIndex] ;
   listsrz := TstringList.Create ;
   Fsqlsarze.Getsarze(odd,listsrz) ;
   SarzeIzbor.Items := listsrz ;
   listsrz.Free ;
end;

// lista kontrolnih šarž
procedure TFvnos.Button1Click(Sender: TObject);
  var funct,funct1,tabc : variant ;
      j,ll : integer ;
      lista : TstringList ;
      mat,tx : string ;
      pz : ^sarz ;
begin
   Screen.Cursor := crHourGlass ;
   label6.Caption := '' ;
   Prijava(connection) ;
   if Connection.LogOn(0,true) = true then  (* parameter "true" = SilentLogOn *)
   begin
      srzlista := Tlist.Create ;
      lista := TstringList.Create ;
      if edit1.Text <> '' then      // vpisana je bila koda izdelka
      begin
        nc := 1 ;
        tx := edit1.Text ;
        //opr := 'I' ;
        //mat := getmat(tx,opr) ;
        mat := koda18(tx) ;
        // pridobim listo kontrolnih šarž
        SAPFunctions1.Connection := Idispatch(Connection);
        funct := sapFunctions1.add('BAPI_INSPLOT_GETLIST');
        funct.exports('MATERIAL').value := mat ;
        funct.exports('PLANT').value := plant  ;
        funct.exports('CREAT_DAT').value := NizToDatum(edit2.Text)   ;
        funct.exports('STATUS_UD').value := ''   ;
        funct.call ;
        tabc := funct.TABLES.item('INSPLOT_LIST');

        if tabc.rowcount > 0 then
        begin
          edit3.Text := tabc.value[1,13] ;
          new(pz) ;
          pz^.kd := tabc.value[1,10] ;
          pz^.naziv := tabc.value[1,10] ;
          srzlista.add(pz) ;             // srzlista v tem primeru niti ni pomembna
          for j := 1 to tabc.rowCount do lista.Add(tabc.value[j,1]+ '  ' +tabc.value[j,12])
        end else
          label6.Caption := 'Nisem našel nobene kontrolne šarže' ;
      end else
      begin          // vpisan je bil kratki test
        nc := 2 ;
        // pridobim listo kontrolnih šarž
        SAPFunctions1.Connection := Idispatch(Connection);
        funct := sapFunctions1.add('BAPI_INSPLOT_GETLIST');
        funct.exports('PLANT').value := plant  ;
        funct.exports('CREAT_DAT').value := NizToDatum(edit2.Text)   ;
        funct.exports('MAX_ROWS').value := 19000   ;
        funct.exports('STATUS_UD').value := ''   ;
        funct.call ;
        tabc := funct.TABLES.item('INSPLOT_LIST');

        tx := edit4.Text ;
        ll := length(tx) ;
        for j := 1 to tabc.rowCount do
        begin
          if tx = copy(tabc.value[j,12],1,ll) then    // preverim èe ustrezajo kratkemu tekstu
          begin
            lista.Add(tabc.value[j,1] + '  ' + tabc.value[j,12] + Format('%8d',[j])) ;
            new(pz) ;
            pz^.kd := tabc.value[j,10] ;
            pz^.naziv := tabc.value[j,10] ;
            srzlista.add(pz) ;
          end;
        end;
      end ;
      SarzeIzbor.items := lista ;
      lista.Free ;
   end else
     Showmessage('Prijava v SAP ni bila uspešna');
   Screen.Cursor := crDefault ;
end;

// lista operacij
procedure TFvnos.Button2Click(Sender: TObject);
  var ii,j,ll : Integer ;
      connection,funct,funct1,tabm,tabr : variant ;
      lista : TstringList ;
      mt,stx : string ;
      storno : boolean ;
      pz : ^sarz ;
begin
  Screen.Cursor := crHourGlass ;
  ii := SarzeIzbor.ItemIndex ;
  if ii < 0 then
  begin
    ShowMessage('Kontrolna šarža ni izbrana') ;
    exit ;
  end;

  srz := Copy(sarzeIzbor.Items[ii],1,12) ;

  // zapis kode in naziva material v primeru ko je bil vpisan kratki tekst
  if nc = 2 then
  begin
    ll := length(sarzeIzbor.Items[ii]) ;
    stx  := Copy(sarzeIzbor.Items[ii],ll-7,8) ;
    j := StrToInt(stx) ;
    pz := srzlista[ii] ;
    mt := pz^.kd ;
    edit1.Text := Getmat(mt,'O') ;
    edit3.Text := pz^.naziv ;
  end;

  Prijava(connection) ;
  if Connection.LogOn(0,true) = true then  (* parameter "true" = SilentLogOn *)
  begin
      SAPFunctions1.Connection := Idispatch(Connection);
      funct1 := sapFunctions1.add('BAPI_INSPLOT_GETDETAIL');
      funct1.exports('NUMBER').value := srz ;
      funct1.exports('LANGUAGE').value[1] := 5  ;
      funct1.call ;

      // preverim ali je bila kont. šarža stronirana
      tabm := funct1.TABLES.ITEM('SYSTEM_STATUS');
      storno := false ;
      for j := 1 to tabm.rowcount do
      begin
        stx := tabm.value[j,1] ;
        if stx = 'I0224' then storno := true ;
      end;

      if storno then
      begin
        ShowMessage('Kontrolna šarža je stornirana') ;
        exit ;
      end;

      // preberem operacije in jih zapišem v list box
      SAPFunctions1.Connection := Idispatch(Connection);
      funct := sapFunctions1.add('BAPI_INSPLOT_GETOPERATIONS');
      funct.exports('NUMBER').value := srz ;
      funct.call ;
      tabr := funct.TABLES.item('INSPOPER_LIST');
      lista := TstringList.Create ;
      for j := 1 to tabr.rowCount do
      begin
        lista.Add(tabr.value[j,2]+'  '+tabr.value[j,4])
      end;
      OperIzbor.items := lista ;
      lista.Free ;
   end;
   Screen.Cursor := crDefault ;
end;

// lista karakteristik
procedure TFvnos.Button3Click(Sender: TObject);
  var ii,j : integer ;
      connection,funct,funct1 : variant ;
      lista : TstringList ;
      spk,spt : string ;
begin
  Screen.Cursor := crHourGlass ;
  ii := SarzeIzbor.ItemIndex ;
  if ii < 0 then
  begin
    ShowMessage('Kontrolna šarža ni izbrana') ;
    exit ;
  end;
  //srz := Copy(sarzeIzbor.Items[ii],1,12) ;
  ii := OperIzbor.ItemIndex ;
  if ii < 0 then
  begin
    ShowMessage('Operacija ni izbrana') ;
    exit ;
  end;
  opr := Copy(OperIzbor.Items[ii],1,4) ;

  Prijava(connection) ;
  if Connection.LogOn(0,true) = true then  (* parameter "true" = SilentLogOn *)
  begin
      SAPFunctions1.Connection := Idispatch(Connection);
      funct := sapFunctions1.add('BAPI_INSPOPER_GETDETAIL');
      funct.exports('INSPLOT').value := srz ;
      funct.exports('INSPOPER').value := opr ;
      funct.exports('READ_INSPPOINTS').value := 'X' ;
      funct.exports('READ_CHAR_REQUIREMENTS').value := 'X' ;
     // funct.exports('READ_SINGLE_RESULTS').value := 'X' ;
      //funct.exports('READ_CHAR_RESULTS').value := 'X' ;
      //funct.exports('READ_SAMPLE_RESULTS').value := 'X' ;
      funct.call ;
      tabv := funct.TABLES.item('INSPPOINTS');
      tabl := funct.TABLES.item('CHAR_REQUIREMENTS');
     // tabk := funct.TABLES.item('CHAR_RESULTS');
     // tabs := funct.TABLES.item('SINGLE_RESULTS');
     // tabN := funct.TABLES.item('SAMPLE_RESULTS');
      lista := TstringList.Create ;
      //for J:= 1 to TABN.ROWCOUNT do  Showmessage(IntTostr(Tabn.value[j,3]) + '/'+ intTostr(tabn.value[j,4])) ;

     // ShowMessage(intTostr(tabk.value[1,4])) ;
     // ShowMessage(tabk.value[1,4]) ;
    //  for j := 1 to 15 do ShowMessage(tabk.value[j,10]) ;
     // ShowMessage(tabk.value[1,25]) ;
    // showMessage(IntTostr(tabv.Rowcount)) ;
     //showMessage('p'+tabv.value[1,5]) ;
     // showmessage(IntTostr(tabv.value[1,4])) ;
      for j := 1 to tabl.rowCount do
      begin
        spk := tabl.value[j,9] + '/' + tabl.value[j,10] ;
        spt :=  tabl.value[j,45] + '-' + tabl.value[j,44] ;
      //  spt :=  tabk.value[j,4] + '-' + tabl.value[j,44] ;
        lista.Add(tabl.value[j,3]+ '  ' + IntToStr(tabl.value[j,14]) + '  ' + tabl.value[j,5]+ '  ' + tabl.value[j,6] + ' ' + spk+ ' ' + spt )
      end;
      KarakIzbor.items := lista ;
      nazivp := '' ;
      spx := 0 ;
      lista.Free ;
   end;
    Screen.Cursor := crDefault ;
end;

//vpis rezultatov
procedure TFvnos.Button4Click(Sender: TObject);
   var ii,kk : integer ;
       j,jj,stkar,stlin: Integer;

       funct,funct1,connection,tab,tabb : variant ;
       nkar,stkr,kar1,kar2 : string ;
       itb,ix,istm,nn,iss : integer ;
       xx,x1,x2 : double ;
       pp : ^singler ;

       vredu,bb,vrd,bx : boolean ;
       chh : string ;
       rmk : string ;
       dbred : string ;
       odl,opp : string ;

       px : ^insPoint ;
       inps : integer ;
       nazp,pvp : string ;
       srd : double ;
       pb : ^karako ;
       pv : ^karak ;
       list_kar : Tlist ;
       izap : longint ;
       skpp,stip : string ;
       karlist : Tlist ;

   procedure   setins ;
   begin
     nazivp := nazp;
     ddat := date;
     ttim := time;
   end;
begin
   jj := 0 ;
   //itb := Tabs.rowCount ;

   stkar := tabl.value[ii+1,14];        // število vzorcev
   nkar := tabl.value[ii+1,5];          // naziv karakteristike
   stkr := tabl.value[ii+1,3];          // številka karakterisitke

   List_kar := tlist.Create ;
   for kk := 1 to tabl.Rowcount do
   begin
       if tabl.value[kk,9] = 'X' then istm := StrToInt(tabl.value[kk,14]) else istm := 1 ;
       for nn := 1 to istm do
       begin
         new(pb) ;
         pb^.naziv := tabl.value[kk,5] ;
         pb^.stkar := kk ;
         pb^.stmerit := nn ;
         if tabl.value[kk,6] = '01' then pb^.tip := 'X' else pb^.tip := ' ' ;
         pb^.sptol := tabl.value[kk,45] ;
         pb^.zgtol := tabl.value[kk,44] ;
         pb^.posam := tabl.value[kk,9] ;
         list_kar.add(pb) ;
       end ;
   end ;
   if srz = '790000033363' then nazivp := 'PLAT6' else nazivp := '' ;

   FvpisMer.TabelVpis(list_kar,bb,nazivp) ;
   if bb then
   begin
     izap := FSqlTable.ZapisVzr(srz,opr,nazivp) ;
     karlist := Tlist.Create ;
     odl := 'A' ;
    // for kk := 1 to tabl.Rowcount do
     iss := 1 ;
     for nn := 0 to List_kar.count-1 do
     begin
        pb := List_kar[nn] ;
        if pb^.merit <> '' then
        begin
          kk := pb^.stkar ;
          stkr := tabl.value[kk,3];
          skpp := tabl.value[kk,9];
          stip := tabl.value[kk,6];
          new(pv) ;
          pv^.stkar := tabl.value[kk,3];
         // pv^.stmer := pb^.stmerit ;
          pv^.stmer := kk ;
          inc(iss) ;
          pv^.skupi := skpp;
          pv^.tip := stip;
          pv^.eval := pb^.eval ;
          pv^.opom := pb^.opom ;
          pv^.merit := pb^.merit ;
          if pb^.eval = 'R' then odl := 'R' ;
          karlist.Add(pv) ;
          FSqltable.VpisChr(izap,kk,stkr,skpp,stip,pb^.merit,pb^.eval,pb^.opom) ;
        end ;
     end;

     bx := Zapis(srz,opr,nazivp,odl,now,karlist)  ;
     if bx then FsqlTable.prenos(izap) ;
     karlist.Free ;
 //    for j := 1 to Funct.Tables.Item('CHAR_RESULTS').rowcount do Funct.Tables.Item('CHAR_RESULTS').deleterow ;
 //    for j := 1 to Funct.Tables.Item('SINGLE_RESULTS').rowcount do Funct.Tables.Item('SINGLE_RESULTS').deleterow ;
   end ;
   list_kar.free ;
end;

procedure TFvnos.Button5Click(Sender: TObject);
var
  i,j: Integer;
begin
  //ShowMessage(intTostr(tabs.rowcount)) ;
 { Fsingle.Tabela.RowCount :=  tabk.rowcount +1 ;
  for i := 1 to tabk.rowcount do
  begin
    for j := 0 to 25 do Fsingle.Tabela.cells[j,i] := tabk.value[i,j+1] ;
  end;
  Fsingle.ShowModal ;  }
  srzlista.free ;
  Application.Terminate ;
end;

// vpogled v podatke na SQL serverju
procedure TFvnos.Button6Click(Sender: TObject);
begin
  Fsqltable.Prikaz ;
end;


procedure TFvnos.Button8Click(Sender: TObject);
begin
   Fsqlsarze.pokazi ;
end;

procedure TFvnos.edit4Click(Sender: TObject);
begin
  Edit1.Text := '' ;
  edit3.Text := '' ;
  SarzeIzbor.Items.Clear ;
end;

//IntTostr(tabc.value[j,14
//13.22453.002/10
procedure TFvnos.FormActivate(Sender: TObject);
  var listsr,listodd : TStringList ;
begin
  panel1.Color := $02E0FFD0 ;
  edit2.Text := DateTostr(date-30) ;
 // listsr := TstringList.Create ;
 // GetIzbsar(listsr) ;
 // sarzeIzbor.Items := listsr ;
 // listsr.free ;

  listodd := TstringList.Create ;
  Fsqlsarze.getodd(listodd);
  oddelki.Items := listodd ;
  Listodd.Free ;
end;

procedure TFVnos.Prijava(var connection : variant) ;
  var prija : Tprijava  ;
begin
  FPrijavaSAP.getPrijava(prija) ;
  if trim(prija.upo) = 'rsg_rfc_1' then plant := '0401' else plant := '1061' ;
  Connection := SAPLogoncontrol1.newConnection;
  with prija do
  begin
    Connection.User              := trim(upo) ;
    Connection.System            := trim(sistem);
    Connection.Client            := trim(cli);
    Connection.ApplicationServer := trim(applic);
    Connection.SystemNumber      := IntTostr(sisnum) ;
    Connection.Password          := trim(ges);
    Connection.Language          := trim(jezik) ;
  end ;

 { Connection                   := SAPLogoncontrol1.newConnection;
  Connection.User              := 'rsg_rfc_1' ;
  Connection.System            := 'P01';
  Connection.Client            := '101';
 //- Connection.ApplicationServer := 'sapr3.egoproducts.com';
  // Connection.ApplicationServer := 'sapr3.egoproducts.com';
  Connection.ApplicationServer := '10.0.2.41';

  Connection.SystemNumber      := '20';
  Connection.Password          := 'khg+987w';
  Connection.Language          := 'sl' ;   }
  {
//Connection                   := SAPLogoncontrol1.newConnection;
 // Connection.User              := 'boncinar' ;
 // Connection.User              := 'rsg_rfc_1' ;
// Connection.System            := 'E01';
  Connection.System            := 'Q01';
  Connection.Client            := '101';
 // Connection.ApplicationServer := '10.0.2.159';
 // Connection.ApplicationServer := '10.0.2.200';   sapq01.egoproducts.com
 Connection.ApplicationServer :=  'sapq01.egoproducts.com' ;
//  Connection.SystemNumber      := '00';
 Connection.SystemNumber      := '20';
  //Connection.Password          := 'bongetsap';
 //  Connection.Password          := 'bon123sap';
    Connection.Password          := 'khg+987w';
  Connection.Language          := 'sl'     }
end ;

procedure TFvnos.SarzeIzborClick(Sender: TObject);
begin
  OperIzbor.Items.Clear ;
  KarakIzbor.Items.Clear ;
end;

function TFvnos.Zapis(srz,opr,nazivp,odl : string ; dd : TdateTime ; karlist : Tlist) : boolean ;
  var funct,funct1 : variant ;
      tab,tabb : variant ;
      dniz,tniz : string ;
      dt,tm :TdateTime ;
      kk,j : integer ;
      pv : ^karak ;
      stkr,skpp,stip,opom,dbred,merit : string ;
      stmr : integer ;
      vredu : boolean ;
begin
  dniz := DateTostr(dd) ;
  dt := NizToDatum(dniz) ;
  tniz := TimeTostr(dd) ;
  tm := StrToTime(tniz) ;
  Prijava(connection) ;
  if Connection.LogOn(0,true) = true then  (* parameter "true" = SilentLogOn *)
  begin
     SAPFunctions1.Connection := Idispatch(Connection);
     funct := sapFunctions1.add('BAPI_INSPOPER_RECORDRESULTS');
     funct.exports('INSPLOT').value := srz ;
     funct.exports('INSPOPER').value := opr ;
     funct.exports('INSPPOINTDATA').value(1) := srz ;
     funct.exports('INSPPOINTDATA').value(2) := opr ;
     funct.exports('INSPPOINTDATA').value(12) := nazivp ;
     funct.exports('INSPPOINTDATA').value(16) := dt;
     funct.exports('INSPPOINTDATA').value(17) := tm ;
     funct.exports('INSPPOINTDATA').value(18) := '3' ;
     funct.exports('INSPPOINTDATA').value(19) := plant ;
     funct.exports('INSPPOINTDATA').value(20) := 'A/R' ;
     funct.exports('INSPPOINTDATA').value(21) := 'A/R' ;
     funct.exports('INSPPOINTDATA').value(22) := odl ;


     for kk := 1 to karlist.count do
     begin
       pv := karlist[kk-1] ;
       stkr := pv^.stkar ;
       stmr := pv^.stmer ;
       skpp := pv^.skupi ;
       stip := pv^.tip ;
       dbred := pv^.eval ;
       opom := pv^.opom ;
       merit := pv^.merit ;
       funct.TABLES.Item('CHAR_RESULTS').APPENDROW.Value('INSPLOT'):= srz;
       Funct.Tables.Item('CHAR_RESULTS').Value[kk,2] := opr ;
       Funct.Tables.Item('CHAR_RESULTS').Value[kk,3] := stkr;  ;
       Funct.Tables.Item('CHAR_RESULTS').Value[kk,25] := opom;

       if skpp = 'X' then        // Posamezen vnos
       begin
         funct.TABLES.Item('SINGLE_RESULTS').APPENDROW.Value('INSPLOT'):= srz;
         Funct.Tables.Item('SINGLE_RESULTS').Value[kk,2] := opr ;
         Funct.Tables.Item('SINGLE_RESULTS').Value[kk,3] := stkr ;
         Funct.Tables.Item('SINGLE_RESULTS').Value[kk,4] := 1 ;
         Funct.Tables.Item('SINGLE_RESULTS').Value[kk,5] := kk ;
         Funct.Tables.Item('SINGLE_RESULTS').Value[kk,7] := 'X' ;
         if stip = '02' then
             Funct.Tables.Item('SINGLE_RESULTS').Value[kk,13] := dbred
         else
             Funct.Tables.Item('SINGLE_RESULTS').Value[kk,10] := merit ;

        funct.Tables.Item('CHAR_RESULTS').Value[kk,4] := 'X' ;
        Funct.Tables.Item('CHAR_RESULTS').Value[kk,5] := 'X' ;
        { Funct.Tables.Item('CHAR_RESULTS').Value[kk,8] := dbred;
         Funct.Tables.Item('CHAR_RESULTS').Value[kk,15] := merit ;
         Funct.Tables.Item('CHAR_RESULTS').Value[kk,25] := opom ;   }
        // Funct.Tables.Item('CHAR_RESULTS').Value[kk,21] := date ;
        // Funct.Tables.Item('CHAR_RESULTS').Value[kk,22] := time ;
       end else
       begin
          funct.TABLES.Item('SAMPLE_RESULTS').APPENDROW.Value('INSPLOT'):= srz;
          Funct.Tables.Item('SAMPLE_RESULTS').Value[kk,2] := opr ;
          Funct.Tables.Item('SAMPLE_RESULTS').Value[kk,3] := stkr ;
          Funct.Tables.Item('SAMPLE_RESULTS').Value[kk,4] := kk ;
          Funct.Tables.Item('SAMPLE_RESULTS').Value[kk,6] := 'X' ;
          Funct.Tables.Item('SAMPLE_RESULTS').Value[kk,7] := 'X' ;
         // Funct.Tables.Item('SAMPLE_RESULTS').Value[kk,13] := 0 ;
          if stip = '02' then
             Funct.Tables.Item('SAMPLE_RESULTS').Value[kk,10] := dbred
          else
             Funct.Tables.Item('SAMPLE_RESULTS').Value[kk,17] := merit ;

          Funct.Tables.Item('CHAR_RESULTS').Value[kk,4] := 'X' ;
          Funct.Tables.Item('CHAR_RESULTS').Value[kk,5] := 'X' ;
        //  Funct.Tables.Item('CHAR_RESULTS').Value[kk,13] := 0 ;
          Funct.Tables.Item('CHAR_RESULTS').Value[kk,8] := dbred;
          Funct.Tables.Item('CHAR_RESULTS').Value[kk,15] := merit ;
          Funct.Tables.Item('CHAR_RESULTS').Value[kk,25] := opom ;
        end;
        //FSqltable.VpisChr(izap,kk,stkr,skpp,stip,pb^.merit,dbred,pb^.opom) ;
      end ;
      funct.call ;
      tab := funct.imports.item('RETURN');
      tabb := funct.tables.Item('RETURNTABLE') ;
      if tab.value[1] = 'E' then vredu := false else vredu := true;
      if tabb.rowcount > 0 then
           Showmessage(tabb.value[1,1] + '/' + tabB.value[1,4] + '/' + tab.value[8]) ;
      if vredu  then
      begin
          SAPFunctions1.Connection := Idispatch(Connection);
          funct1 := sapFunctions1.add('BAPI_TRANSACTION_COMMIT');
          if not funct1.call then
          begin
            showMessage(Funct1.exception) ;
            result := false ;
          end else
            result := true
      end else result := false ;
     for j := 1 to Funct.Tables.Item('CHAR_RESULTS').rowcount do Funct.Tables.Item('CHAR_RESULTS').deleterow ;
     for j := 1 to Funct.Tables.Item('SINGLE_RESULTS').rowcount do Funct.Tables.Item('SINGLE_RESULTS').deleterow ;
     for j := 1 to Funct.Tables.Item('SAMPLE_RESULTS').rowcount do Funct.Tables.Item('SAMPLE_RESULTS').deleterow ;
  end;
end;

Procedure TFVnos.GetIzbsar(var listSr : tstringList) ;
  var ff : textfile ;
      vrs,imed : string ;
begin
  imed := 'IzbiraSar.txt' ;
  if Fileexists(imed)  then AssignFile(ff,imed) else exit ;
  Reset(ff) ;
  while not eof(ff) do
  begin
    readln(ff,vrs) ;
    Listsr.add(vrs) ;
  end;
  CloseFile(ff) ;
end;

end.
