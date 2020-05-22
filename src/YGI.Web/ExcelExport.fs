module ExcelExport
  open DocumentFormat.OpenXml.Spreadsheet
  open DocumentFormat.OpenXml.Packaging
  open DocumentFormat.OpenXml
  open System.IO
  open System
  open YGI.Dto
  open YGI.Logging

  let private ``0u`` = UInt32Value(0u)
  let private ``1u`` = UInt32Value(1u)
  let private ``2u`` = UInt32Value(2u)


  let DefaultFonts = 
    
    let fonts0 = new Fonts()
    fonts0.Count <- ``1u``
    fonts0.KnownFonts <- true |> BooleanValue

    let font0 = new Font()
    let fontSize0 = new FontSize()
    fontSize0.Val <- 11.0 |> DoubleValue
    let fontName0 = new FontName()
    fontName0.Val <- "Calibri" |> StringValue
    let fontFamilyNumbering0 = new FontFamilyNumbering()
    fontFamilyNumbering0.Val <- 2 |> Int32Value
    let fontScheme0 = new FontScheme()
    fontScheme0.Val <- new EnumValue<FontSchemeValues>(FontSchemeValues.Minor)

    font0.Append(fontSize0 :> OpenXmlElement)
    font0.Append(fontName0 :> OpenXmlElement)
    font0.Append(fontFamilyNumbering0 :> OpenXmlElement)
    font0.Append(fontScheme0 :> OpenXmlElement)

    fonts0.Append(font0 :> OpenXmlElement)
    fonts0

  let DefaultFills =
    let fills = new Fills()
    fills.Count <- ``2u``

    let fill0 = new Fill()
    let patternFill0 = new PatternFill()
    patternFill0.PatternType <- PatternValues.None |> EnumValue
    fill0.Append(patternFill0 :> OpenXmlElement)

    let fill1 = new Fill()
    let patternFill1 = new PatternFill()
    patternFill1.PatternType <- PatternValues.Gray125 |> EnumValue
    fill1.Append(patternFill1 :> OpenXmlElement)

    fills.Append(fill0 :> OpenXmlElement)
    fills.Append(fill1 :> OpenXmlElement)
    fills

  let DefaultBorders =
    let borders = new Borders()
    borders.Count <- ``1u``
    let border0 = new Border()

    let leftBorder0 = new LeftBorder();
    let rightBorder0 = new RightBorder();
    let topBorder0 = new TopBorder();
    let bottomBorder0 = new BottomBorder();
    let diagonalBorder0 = new DiagonalBorder();

    border0.Append(leftBorder0 :> OpenXmlElement);
    border0.Append(rightBorder0 :> OpenXmlElement);
    border0.Append(topBorder0 :> OpenXmlElement);
    border0.Append(bottomBorder0 :> OpenXmlElement);
    border0.Append(diagonalBorder0 :> OpenXmlElement);
    borders.Append(border0 :> OpenXmlElement);
    borders
    
  let DefaultCellStyleFormats =
    let cellStyleFormats = new CellStyleFormats()
    cellStyleFormats.Count <- ``1u``
    
    let cellFormat0 = new CellFormat()
    cellFormat0.NumberFormatId <- ``0u``
    cellFormat0.FontId <- ``0u``
    cellFormat0.FillId <- ``0u``
    cellFormat0.BorderId <- ``0u``

    cellStyleFormats.Append(cellFormat0 :> OpenXmlElement);
    cellStyleFormats

  let DefaultCellFormats =
    let cellFormats = new CellFormats()
    cellFormats.Count <- ``2u``

    // Default Cell
    let cellFormat0 = new CellFormat()
    cellFormat0.NumberFormatId <- ``0u``
    cellFormat0.FontId <- ``0u``
    cellFormat0.FillId <- ``0u``
    cellFormat0.BorderId <- ``0u``
    cellFormat0.FormatId <- ``0u``
    
    // Wrap Cell
    let cellFormat1 = new CellFormat()
    cellFormat1.NumberFormatId <- ``0u``
    cellFormat1.FontId <- ``0u``
    cellFormat1.FillId <- ``0u``
    cellFormat1.BorderId <- ``0u``
    cellFormat1.FormatId <- ``0u``
    cellFormat1.ApplyAlignment <- true |> BooleanValue
    
    let alignment0 = new Alignment()
    alignment0.Horizontal <- HorizontalAlignmentValues.Left |> EnumValue
    alignment0.Vertical <- VerticalAlignmentValues.Top |> EnumValue
    alignment0.WrapText <- true  |> BooleanValue
    cellFormat1.Append(alignment0 :> OpenXmlElement)

    cellFormats.Append(cellFormat0 :> OpenXmlElement)
    cellFormats.Append(cellFormat1 :> OpenXmlElement)

    cellFormats

  let GenerateWorkbookStylesPartContent( stylesPart : inref<WorkbookStylesPart>) =
    
    let stylesheet = new Stylesheet()
    stylesheet.Append(DefaultFonts :> OpenXmlElement)
    stylesheet.Append(DefaultFills :> OpenXmlElement)
    stylesheet.Append(DefaultBorders :> OpenXmlElement)
    stylesheet.Append(DefaultCellStyleFormats :> OpenXmlElement)
    stylesheet.Append(DefaultCellFormats :> OpenXmlElement)
    stylesPart.Stylesheet <- stylesheet
    stylesPart.Stylesheet.Save()

  let DefaultColumns =
    let columns = new Columns();

    let column (index:int) (width:float) = 
      let c = new Column()
      c.Min <- index |> uint32 |> UInt32Value
      c.Max <- index |> uint32 |> UInt32Value
      c.Width <- width |> DoubleValue
      c.CustomWidth <- true |> BooleanValue
      c

    [
      column 1 4.0   // itemNo
      column 2 15.0  // area
      column 3 15.0  // equipment
      column 4 15.0  // issueType
      column 5 70.0  // issue
      column 6 15.0  // raisedBy
      column 7 15.0  // raised
      column 8 15.0  // lastChanged
      column 9 15.0  // status
    ] 
    |> List.map (fun x -> x :> OpenXmlElement)
    |> List.iter columns.Append

    columns

  type ExportSheet (logger:Logger,projNo:string) =

    do logger Info <| sprintf "Exporting Project: %s" projNo

    // Create the spreadsheet and the stream we will write it to
    let stream = new MemoryStream()
    let doc : SpreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook)
    
    let workbookPart : WorkbookPart = doc.AddWorkbookPart()
    do  workbookPart.Workbook <- new Workbook();

    let worksheetPart : WorksheetPart = workbookPart.AddNewPart<WorksheetPart>()
    do  worksheetPart.Worksheet <- new Worksheet(new SheetData())
   
    // Create sheet data
    let sheetData : SheetData = worksheetPart.Worksheet.AppendChild(new SheetData())

    // Create the sheet properties
    let _ = doc.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
    let sheet  = doc.WorkbookPart.Workbook.Sheets.AppendChild(new Sheet())
    do  sheet.Id <- doc.WorkbookPart.GetIdOfPart(worksheetPart) |> StringValue
    do  sheet.SheetId <- UInt32Value.FromUInt32(doc.WorkbookPart.Workbook.Sheets.ChildElements.Count + 1 |> uint32)
    do  sheet.Name <- projNo |> StringValue

    do logger Info "Before Style Sheet"

    // Add Stylesheet
    let workbookStylesPart : WorkbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
    do GenerateWorkbookStylesPartContent &workbookStylesPart
    do workbookStylesPart.Stylesheet.Save()

    do logger Info "After Style Sheet"

    // Add Columns
    let _ = worksheetPart.Worksheet.InsertAt(DefaultColumns :> OpenXmlElement, 0)

    do  doc.WorkbookPart.Workbook.Save()

    member __.MemStream = stream
    member __.Sheet = sheet
    member __.SheetData = sheetData
    member __.Save()= 
      worksheetPart.Worksheet.Save()
      doc.WorkbookPart.Workbook.Save()
    member __.Close() = doc.Close();

    member __.AddIssue (issue:IssueDto) =
      let row :Row = sheetData.AppendChild(new Row())

      let addCell (row:Row) (cellType:CellValues) (value:string) =
        let cell = row.AppendChild(new Cell())
        cell.CellValue <- new CellValue(value)
        cell.DataType <- cellType |> EnumValue

      let addWrapCell (row:Row) (cellType:CellValues) (value:string) =
        let cell = row.AppendChild(new Cell())
        cell.CellValue <- new CellValue(value)
        cell.DataType <- cellType |> EnumValue
        cell.StyleIndex <- UInt32Value(1u)

      let addNumberCell = addCell row CellValues.Number
      let addStringCell = addCell row CellValues.String
      
      addNumberCell (issue.ItemNo.ToString())
      addStringCell issue.Area
      addStringCell issue.Equipment
      addStringCell issue.IssueType
      addStringCell issue.Title
      addStringCell issue.RaisedBy
      addStringCell (issue.Raised.ToString("dd/MM/yyyy"))
      addStringCell (issue.LastChanged.ToString("dd/MM/yyyy"))
      addStringCell issue.Status

      /// Description Row
      let descriptionRow :Row = sheetData.AppendChild(new Row())
      [0..3] |> List.iter (fun _ -> addCell descriptionRow CellValues.String "")
      addWrapCell descriptionRow CellValues.String issue.Description

      /// Comments Row
      let addComment (comment:CommentDto) =
        let row :Row = sheetData.AppendChild(new Row())
        [0..3] |> List.iter (fun _ -> addCell row CellValues.String "")
        addWrapCell row CellValues.String comment.Comment
        addCell row CellValues.String comment.CommentBy
        addCell row CellValues.String (comment.Raised.ToString("dd/MM/yyyy"))

      issue.Comments |> Array.iter addComment

      

    interface IDisposable with 
      member __.Dispose() =
        doc.Dispose()
        stream.Dispose()

  let Export logger (proj:ProjectStateDto) () =

    use doc = new ExportSheet(logger,proj.ProjectNumber)

    proj.Issues |> Array.iter doc.AddIssue

    // save the worksheet
    doc.Save()

    // close the document
    doc.Close()

    doc.MemStream.ToArray()