object Fzacetna: TFzacetna
  Left = 0
  Top = 0
  Caption = 'Fzacetna'
  ClientHeight = 997
  ClientWidth = 1485
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'Tahoma'
  Font.Style = []
  Menu = MainMenu1
  OldCreateOrder = False
  Position = poScreenCenter
  OnActivate = FormActivate
  PixelsPerInch = 96
  TextHeight = 13
  object Panel1: TPanel
    Left = 0
    Top = 0
    Width = 1485
    Height = 297
    Align = alTop
    BevelOuter = bvLowered
    Color = clBackground
    Font.Charset = EASTEUROPE_CHARSET
    Font.Color = clWindowText
    Font.Height = -15
    Font.Name = 'Arial'
    Font.Style = []
    ParentBackground = False
    ParentFont = False
    TabOrder = 0
    object Label1: TLabel
      Left = 32
      Top = 8
      Width = 93
      Height = 17
      Caption = 'Delovna enota'
    end
    object Delavnice: TLabel
      Left = 264
      Top = 8
      Width = 64
      Height = 17
      Caption = 'Delavnice'
    end
    object Label2: TLabel
      Left = 664
      Top = 8
      Width = 33
      Height = 17
      Caption = 'Stroji'
    end
    object Label3: TLabel
      Left = 1256
      Top = 8
      Width = 34
      Height = 17
      Caption = 'Koda'
    end
    object Label4: TLabel
      Left = 1256
      Top = 145
      Width = 103
      Height = 17
      Caption = 'Kontrolna '#353'ar'#382'a'
    end
    object List1: TListBox
      Left = 48
      Top = 31
      Width = 193
      Height = 97
      Font.Charset = EASTEUROPE_CHARSET
      Font.Color = clWindowText
      Font.Height = -16
      Font.Name = 'Arial Narrow'
      Font.Style = [fsBold]
      ItemHeight = 20
      ParentFont = False
      TabOrder = 0
      OnClick = List1Click
    end
    object List2: TListBox
      Left = 296
      Top = 31
      Width = 329
      Height = 234
      ItemHeight = 17
      TabOrder = 1
      OnClick = List2Click
    end
    object List3: TListBox
      Left = 680
      Top = 31
      Width = 489
      Height = 234
      ItemHeight = 17
      TabOrder = 2
      OnClick = List3Click
    end
    object list4: TListBox
      Left = 1272
      Top = 31
      Width = 161
      Height = 97
      ItemHeight = 17
      TabOrder = 3
      OnClick = list4Click
    end
    object list5: TListBox
      Left = 1272
      Top = 168
      Width = 161
      Height = 97
      ItemHeight = 17
      TabOrder = 4
      OnClick = list5Click
    end
    object Button4: TButton
      Left = 56
      Top = 168
      Width = 129
      Height = 33
      Caption = 'Iz lokalne baze '
      TabOrder = 5
      OnClick = Button4Click
    end
  end
  object Panel2: TPanel
    Left = 0
    Top = 297
    Width = 1485
    Height = 344
    Align = alTop
    BevelOuter = bvLowered
    Color = clBtnHighlight
    Font.Charset = EASTEUROPE_CHARSET
    Font.Color = clWindowText
    Font.Height = -13
    Font.Name = 'Arial'
    Font.Style = []
    ParentBackground = False
    ParentFont = False
    TabOrder = 1
    object Label5: TLabel
      Left = 1169
      Top = 10
      Width = 59
      Height = 16
      Caption = #352't. kanala'
    end
    object Label6: TLabel
      Left = 1328
      Top = 10
      Width = 94
      Height = 16
      Caption = #352'tevilo decimalk'
    end
    object Panel3: TPanel
      Left = 1
      Top = 1
      Width = 1136
      Height = 342
      Align = alLeft
      BevelOuter = bvLowered
      TabOrder = 0
      object Karakti: TStringGrid
        Left = 1
        Top = 1
        Width = 1134
        Height = 340
        Align = alClient
        ColCount = 6
        FixedCols = 5
        Options = [goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goRangeSelect, goEditing, goTabs]
        TabOrder = 0
        OnClick = KaraktiClick
        RowHeights = (
          24
          24
          24
          24
          24)
      end
    end
    object SpinEdit1: TSpinEdit
      Left = 1192
      Top = 32
      Width = 82
      Height = 26
      MaxValue = 0
      MinValue = 0
      TabOrder = 1
      Value = 1
    end
    object Button2: TButton
      Left = 1176
      Top = 88
      Width = 114
      Height = 33
      Caption = 'Prenos meritev'
      TabOrder = 2
      OnClick = Button2Click
    end
    object SpinEdit2: TSpinEdit
      Left = 1341
      Top = 32
      Width = 81
      Height = 26
      MaxValue = 0
      MinValue = 0
      TabOrder = 3
      Value = 2
    end
    object Button1: TButton
      Left = 1176
      Top = 144
      Width = 114
      Height = 33
      Caption = 'Prepis v SAP'
      TabOrder = 4
      OnClick = Button1Click
    end
    object Button3: TButton
      Left = 1176
      Top = 264
      Width = 114
      Height = 33
      Caption = 'Konec '
      TabOrder = 5
      OnClick = Button3Click
    end
    object Button5: TButton
      Left = 1320
      Top = 144
      Width = 113
      Height = 33
      Caption = 'Prepis v lok. bazo'
      Enabled = False
      TabOrder = 6
      OnClick = Button5Click
    end
    object Button6: TButton
      Left = 1392
      Top = 224
      Width = 75
      Height = 25
      Caption = 'Button6'
      TabOrder = 7
      OnClick = Button6Click
    end
    object Button7: TButton
      Left = 1320
      Top = 88
      Width = 113
      Height = 33
      Caption = 'Lok. baza->SAP'
      TabOrder = 8
      OnClick = Button7Click
    end
  end
  object Panel4: TPanel
    Left = 0
    Top = 641
    Width = 1485
    Height = 356
    Align = alClient
    TabOrder = 2
    object attri: TStringGrid
      Left = 1
      Top = 1
      Width = 632
      Height = 354
      Align = alLeft
      ColCount = 4
      Options = [goFixedVertLine, goFixedHorzLine, goVertLine, goHorzLine, goRangeSelect, goEditing]
      TabOrder = 0
    end
  end
  object ADOQuery1: TADOQuery
    ConnectionString = 
      'Provider=SQLOLEDB.1;Password=ticmis;Persist Security Info=True;U' +
      'ser ID=sinabit;Initial Catalog=SinaproNG;Data Source=mes'
    Parameters = <>
    Left = 1200
    Top = 40
  end
  object ADOQuery2: TADOQuery
    ConnectionString = 
      'Provider=SQLOLEDB.1;Password=ticmis;Persist Security Info=True;U' +
      'ser ID=sinabit;Initial Catalog=SinaproNG;Data Source=mes'
    Parameters = <>
    Left = 1200
    Top = 96
  end
  object MainMenu1: TMainMenu
    Left = 16
    Top = 248
    object Lokalnabaza1: TMenuItem
      Caption = 'Lokalna baza'
      object Linijekode1: TMenuItem
        Caption = 'Linije-kode'
        OnClick = Linijekode1Click
      end
      object Kodaare1: TMenuItem
        Caption = 'Koda-'#353'ar'#382'e'
        OnClick = Kodaare1Click
      end
      object arekarakteristike1: TMenuItem
        Caption = #352'ar'#382'e-karakteristike'
        OnClick = arekarakteristike1Click
      end
      object Meritve1: TMenuItem
        Caption = 'Meritve'
        OnClick = Meritve1Click
      end
    end
    object Postaje1: TMenuItem
      Caption = 'Postaje'
      OnClick = Postaje1Click
    end
    object Stroji1: TMenuItem
      Caption = 'Stroji'
      OnClick = Stroji1Click
    end
  end
end
