object FSqlSarze: TFSqlSarze
  Left = 0
  Top = 0
  Caption = 'FSqlSarze'
  ClientHeight = 534
  ClientWidth = 803
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'Tahoma'
  Font.Style = []
  OldCreateOrder = False
  OnCreate = FormCreate
  PixelsPerInch = 96
  TextHeight = 13
  object Panel1: TPanel
    Left = 0
    Top = 0
    Width = 803
    Height = 41
    Align = alTop
    BevelOuter = bvLowered
    TabOrder = 0
    object Button1: TButton
      Left = 24
      Top = 11
      Width = 75
      Height = 25
      Caption = 'Prepis'
      TabOrder = 0
      OnClick = Button1Click
    end
    object Button2: TButton
      Left = 152
      Top = 10
      Width = 75
      Height = 25
      Caption = 'Bri'#353'i'
      TabOrder = 1
      OnClick = Button2Click
    end
  end
  object Panel2: TPanel
    Left = 0
    Top = 41
    Width = 803
    Height = 493
    Align = alClient
    BevelOuter = bvLowered
    TabOrder = 1
    object DBGrid1: TDBGrid
      Left = 1
      Top = 1
      Width = 801
      Height = 491
      Align = alClient
      DataSource = DataSource1
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWindowText
      Font.Height = -13
      Font.Name = 'Tahoma'
      Font.Style = []
      ParentFont = False
      TabOrder = 0
      TitleFont.Charset = DEFAULT_CHARSET
      TitleFont.Color = clWindowText
      TitleFont.Height = -11
      TitleFont.Name = 'Tahoma'
      TitleFont.Style = []
    end
  end
  object DataSource1: TDataSource
    DataSet = ADOQuery1
    Left = 144
    Top = 177
  end
  object ADOQuery1: TADOQuery
    ConnectionString = 
      'Provider=SQLOLEDB.1;Integrated Security=SSPI;Persist Security In' +
      'fo=False;Initial Catalog=SapKontrola;Data Source=172.20.11.155\t' +
      'sspica'
    CursorType = ctStatic
    Parameters = <>
    SQL.Strings = (
      'select * from Kontsarze')
    Left = 288
    Top = 177
  end
end
