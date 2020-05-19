module ExcelExport
  open DocumentFormat.OpenXml.Spreadsheet
  open DocumentFormat.OpenXml.Packaging
  open DocumentFormat
  open DocumentFormat.OpenXml
  open System.IO.Packaging
  open System.IO
  open System

  let Export () = 

    use stream = new MemoryStream()

    try 

      
      use doc : SpreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook)

      let workbookPart : WorkbookPart = doc.AddWorkbookPart()
      workbookPart.Workbook <- new Workbook();
      let worksheetPart : WorksheetPart = workbookPart.AddNewPart<WorksheetPart>()
      worksheetPart.Worksheet <- new Worksheet(new SheetData())
   
      // create sheet data
      let sheetData : SheetData = worksheetPart.Worksheet.AppendChild(new SheetData())

      // create a row and add a data to it
      let row = sheetData.AppendChild(new Row())

      let cell = row.AppendChild(new Cell())
      cell.CellValue <- new CellValue("5")
      cell.DataType <- CellValues.Number |> EnumValue

      // save the worksheet
      worksheetPart.Worksheet.Save();

      // create the sheet properties
      let sheets = doc.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
      let sheet  = doc.WorkbookPart.Workbook.Sheets.AppendChild(new Sheet())
      sheet.Id <- doc.WorkbookPart.GetIdOfPart(worksheetPart) |> StringValue
      sheet.SheetId <- UInt32Value.FromUInt32(doc.WorkbookPart.Workbook.Sheets.ChildElements.Count + 1 |> uint32)
      sheet.Name <- "MyFirstSheet" |> StringValue

      doc.WorkbookPart.Workbook.Save()
    with ex -> 
      failwith "fail"
      

    //// Add a WorkbookPart to the document.
    //let workbook = new Workbook()
    //let workbookpart : WorkbookPart = spreadsheetDocument.AddWorkbookPart()
    //workbookpart.Workbook <- workbook

    //// Add a WorksheetPart to the WorkbookPart.
    //let worksheetPart : WorksheetPart = workbookpart.AddNewPart<WorksheetPart>()
    //let sheetData = new SheetData();
    //worksheetPart.Worksheet <- new Worksheet(sheetData)

    //// Add Sheets to the Workbook.
    ////let sheets : Sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
    //let sheets = new Sheets()
    //spreadsheetDocument.WorkbookPart.Workbook.Sheets <- new Sheets()
    

    //// Append a new worksheet and associate it with the workbook.
    //let sheet = Sheet()
    //sheet.Id <- spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart) |> StringValue
    //sheet.SheetId <- UInt32Value.FromUInt32(2 |> uint32)
    //sheet.Name <- "mySheet" |> StringValue

    

    //let headerRow = new DocumentFormat.OpenXml.Spreadsheet.Row()
    //let cell = new DocumentFormat.OpenXml.Spreadsheet.Cell()
    //cell.DataType <- DocumentFormat.OpenXml.Spreadsheet.CellValues.String |> EnumValue
    //cell.CellValue <- new DocumentFormat.OpenXml.Spreadsheet.CellValue("Sup?")
    //headerRow.AppendChild(cell) |> ignore

    //sheetData.AppendChild(headerRow) |> ignore


    //sheets.Append(sheet)
    ////spreadsheetDocument.WorkbookPart.Workbook.Sheets.Append(sheet)

    //workbookpart.Workbook.Save();
    //spreadsheetDocument.Save();
    //spreadsheetDocument.Close();

    let bytes = stream.ToArray()
    stream.Position <- 0L
    stream, bytes