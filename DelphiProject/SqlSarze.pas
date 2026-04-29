unit SqlSarze;

interface

uses
  Winapi.Windows, Winapi.Messages, System.SysUtils, System.Variants, System.Classes, Vcl.Graphics,
  Vcl.Controls, Vcl.Forms, Vcl.Dialogs, Data.DB, Data.Win.ADODB, Vcl.Grids,
  Vcl.DBGrids, Vcl.ExtCtrls, Vcl.StdCtrls;

type
  TFSqlSarze = class(TForm)
    Panel1: TPanel;
    Panel2: TPanel;
    DBGrid1: TDBGrid;
    DataSource1: TDataSource;
    ADOQuery1: TADOQuery;
    Button1: TButton;
    Button2: TButton;
    procedure Button1Click(Sender: TObject);
    procedure FormCreate(Sender: TObject);
    procedure Button2Click(Sender: TObject);
  private
    { Private declarations }
  public
    Procedure Pokazi ;
    Procedure getodd(listodd : tstringlist) ;
    Procedure getsarze(odd : string ; listsrz : tstringlist) ;
  end;

var
  FSqlSarze: TFSqlSarze;

implementation

{$R *.dfm}

Procedure TFsqlsarze.Pokazi ;
begin
  width := 1450 ;
  Panel2.Width := 700 ;
  ADOQuery1.SQL.Clear ;
  ADOquery1.SQL.add('SELECT * FROM Kontsarze') ;
  AdoQuery1.Open ;
  ShowModal ;
  AdoQuery1.Close  ;
end ;

procedure TFSqlSarze.Button1Click(Sender: TObject);
  var ff : textfile ;
      imed,sr,odd,txt,toc : string ;
begin
  imed := 'sarze.txt' ;
  AssignFile(ff,imed) ;
  Rewrite(ff) ;
  ADOquery1.first ;
  while not Adoquery1.eof do
  begin
     sr := AdoQuery1.FieldByName('sarza').Value ;
     odd := AdoQuery1.FieldByName('oddelek').Value ;
     if Adoquery1.Fieldbyname('tekst').isnull then txt := ''
                                else txt := Adoquery1.Fieldbyname('tekst').value ;
     if Adoquery1.Fieldbyname('ktocka').isnull then toc := ''
                                else toc := Adoquery1.Fieldbyname('ktocka').value ;
     Writeln(ff,sr+ ',' + odd + ',' + txt + ',' + toc) ;
     ADOquery1.Next ;
  end;
  CloseFile(ff) ;
end;


procedure TFSqlSarze.Button2Click(Sender: TObject);
   var ii : integer ;
begin
  ii := MessageDlg('Ali zares želiš izbrisati',mtConfirmation,[mbYes, mbNo],0);
  if ii = 6 then  Adoquery1.Delete
end;

procedure TFSqlSarze.FormCreate(Sender: TObject);

  var dir : string ;
begin
  dir := ExtractfilePath(application.ExeName) ;
  Adoquery1.connectionString := 'FILE NAME=' + dir + 'SAPVpis.udl';
end;


Procedure TFsqlsarze.getodd(listodd : tstringlist) ;
   var imed,vrs,odd : string ;
       ff : textfile ;
       i1 : integer ;
begin
  ADOQuery1.SQL.Clear ;
  ADOquery1.SQL.add('SELECT  DISTINCT ODDELEK FROM Kontsarze') ;
  try
    AdoQuery1.Open ;
    ADOquery1.first ;
    while not Adoquery1.eof do
    begin
       listodd.Add(Adoquery1.Fieldbyname('oddelek').value) ;
       ADOquery1.Next ;
    end;
    AdoQuery1.Close  ;
  except
    imed := 'sarze.txt' ;
    AssignFile(ff,imed) ;
    Reset(ff) ;
    while not eof(ff) do
    begin
      readln(ff,vrs) ;
      odd := copy(vrs,14,3) ;
      if listodd.IndexOf(odd) < 0 then listodd.Add(odd)
    end;
  end;
end;

Procedure TFsqlsarze.getsarze(odd : string ; listsrz : tstringlist) ;
  var tx : string ;
      imed,vrs,t_odd,sr : string ;
      ff : textfile ;
begin
  ADOQuery1.SQL.Clear ;
  ADOquery1.SQL.add('SELECT  sarza, tekst  FROM Kontsarze WHERE ODDELEK = :ODD') ;
  ADOQuery1.parameters[0].value := odd ;
  ADOQuery1.parameters[0].name  := 'ODD' ;
  try
    AdoQuery1.Open ;
    ADOquery1.first ;
    while not Adoquery1.eof do
    begin
      if Adoquery1.Fieldbyname('tekst').isnull then tx := ''
      else tx := Adoquery1.Fieldbyname('tekst').value ;

      listSRZ.Add(Adoquery1.Fieldbyname('SARZA').value + '  ' + tx) ;
      ADOquery1.Next ;
    end;
    AdoQuery1.Close  ;
  except
    imed := 'sarze.txt' ;
    AssignFile(ff,imed) ;
    Reset(ff) ;
    while not eof(ff) do
    begin
      readln(ff,vrs) ;
      t_odd := copy(vrs,14,3) ;
      if t_odd = odd then
      begin
        sr := copy(vrs,1,12) ;
        tx := copy(vrs,18,40) ;
        tx := sr + '  ' + tx ;
        if listsrz.IndexOf(tx) < 0 then listsrz.Add(tx)
      end;
    end ;
  end;
end;


end.
