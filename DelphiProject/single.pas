unit single;

interface

uses
  Winapi.Windows, Winapi.Messages, System.SysUtils, System.Variants, System.Classes, Vcl.Graphics,
  Vcl.Controls, Vcl.Forms, Vcl.Dialogs, Vcl.Grids, Vcl.ExtCtrls;

type
  TFsingle = class(TForm)
    Panel1: TPanel;
    tabela: TStringGrid;
  private
    { Private declarations }
  public
    { Public declarations }
  end;

var
  Fsingle: TFsingle;

implementation

{$R *.dfm}

end.
