unit KonTocke;

interface

uses
  Winapi.Windows, Winapi.Messages, System.SysUtils, System.Variants, System.Classes, Vcl.Graphics,
  Vcl.Controls, Vcl.Forms, Vcl.Dialogs, Vcl.StdCtrls, Vcl.ExtCtrls;

type
  InsPoint = record
    naziv : string ;
    datum : TdateTime ;
    time : TdateTime ;
  end;
  TFKonTocke = class(TForm)
    Panel1: TPanel;
    List1: TListBox;
    Label1: TLabel;
    Button1: TButton;
    Label2: TLabel;
    Edit1: TEdit;
    procedure Button1Click(Sender: TObject);
  private
    rx : integer ;
    cc : string ;
  public
    function Izbira(listPoint : Tlist; var naz : string) : integer ;
  end;

var
  FKonTocke: TFKonTocke;

implementation

{$R *.dfm}
procedure TFKonTocke.Button1Click(Sender: TObject);
  var ix : integer ;
begin
  ix := List1.ItemIndex ;
  cc := '' ;
  if ix = List1.Items.count-1 then
  begin
   rx := -2 ;
   if edit1.Text <> '' then
   begin
     cc := edit1.Text ;
     close
   end else Showmessage('Vpiši naziv kontrolne toèke')
  end else
  begin
    rx := List1.ItemIndex ;
    close
  end;

end;

function TFkonTocke.Izbira(listPoint : Tlist; var naz : string) : integer ;
  var i,ix : integer ;
  px : ^inspoint ;
begin
  for i := 0 to listPoint.count-1 do
  begin
    px := listPoint[i] ;
    list1.items.add(Format('%-10s',[px^.naziv]) + dateTostr(px^.datum)+ ' ' + TimeTostr(px^.time)) ;
  end;
  List1.Items.Add('Novi') ;
  ShowModal ;
  result := rx ;
  naz := cc ;
  list1.Items.Clear ;
end;
end.
