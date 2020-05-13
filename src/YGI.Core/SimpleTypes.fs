namespace YGI.Common
  
  open System

  type String50  = private String50  of string
  type String100 = private String100 of string
  type String500 = private String500 of string
  type Vid       = private Vid of Guid

  type Status =
    | Unopened
    | Open
    | InProgress
    | Verify
    | Closed

  type YgiError =
    | StorageError of string
    | ApplicationError of string

  module Int =
    let create fieldName (str:string) =
      let res, value = Int32.TryParse str
      match res with
      | true -> Ok value
      | false -> 
        let msg = sprintf "failed to create field %s when parsing int %s" fieldName str
        Error msg

  module ConstrainedType =

    /// Create a constrained string using the constructor provided
    /// Return Error if input is null, empty, or length > maxLen
    let createString fieldName ctor maxLen str = 
        if String.IsNullOrEmpty(str) then
            let msg = sprintf "%s must not be null or empty" fieldName 
            Error msg
        elif str.Length > maxLen then
            let msg = sprintf "%s must not be more than %i chars" fieldName maxLen 
            Error msg 
        else
            Ok (ctor str)

    /// Create a optional constrained string using the constructor provided
    /// Return None if input is null, empty. 
    /// Return error if length > maxLen
    /// Return Some if the input is valid
    let createStringOption fieldName ctor maxLen str = 
        if String.IsNullOrEmpty(str) then
            Ok None
        elif str.Length > maxLen then
            let msg = sprintf "%s must not be more than %i chars" fieldName maxLen 
            Error msg 
        else
            Ok (ctor str |> Some)

    /// Create a constrained integer using the constructor provided
    /// Return Error if input is less than minVal or more than maxVal
    let createInt fieldName ctor minVal maxVal i = 
        if i < minVal then
            let msg = sprintf "%s: Must not be less than %i" fieldName minVal
            Error msg
        elif i > maxVal then
            let msg = sprintf "%s: Must not be greater than %i" fieldName maxVal
            Error msg
        else
            Ok (ctor i)

    /// Create a constrained decimal using the constructor provided
    /// Return Error if input is less than minVal or more than maxVal
    let createDecimal fieldName ctor minVal maxVal i = 
        if i < minVal then
            let msg = sprintf "%s: Must not be less than %M" fieldName minVal
            Error msg
        elif i > maxVal then
            let msg = sprintf "%s: Must not be greater than %M" fieldName maxVal
            Error msg
        else
            Ok (ctor i)

    /// Create a constrained string using the constructor provided
    /// Return Error if input is null. empty, or does not match the regex pattern
    let createLike fieldName  ctor pattern str = 
        if String.IsNullOrEmpty(str) then
            let msg = sprintf "%s: Must not be null or empty" fieldName 
            Error msg
        elif System.Text.RegularExpressions.Regex.IsMatch(str,pattern) then
            Ok (ctor str)
        else
            let msg = sprintf "%s: '%s' must match the pattern '%s'" fieldName str pattern
            Error msg 

  module String50 =

    /// Return the value inside a String50
    let value (String50 str) = str

    /// Create a String50 from a string
    /// Return Error if input is null, empty, or length > 50
    let create fieldName str = 
        ConstrainedType.createString fieldName String50 50 str

    /// Create an String50 from a string
    /// Return None if input is null, empty. 
    /// Return error if length > maxLen
    /// Return Some if the input is valid
    let createOption fieldName str = 
        ConstrainedType.createStringOption fieldName String50 50 str

  module String100 =

    /// Return the value inside a String100
    let value (String100 str) = str

    /// Create a String100 from a string
    /// Return Error if input is null, empty, or length > 100
    let create fieldName str = 
        ConstrainedType.createString fieldName String100 100 str

    /// Create a String100 from a string
    /// Return None if input is null, empty. 
    /// Return error if length > maxLen
    /// Return Some if the input is valid
    let createOption fieldName str = 
        ConstrainedType.createStringOption fieldName String100 100 str

  module String500 =

    /// Return the value inside a String500
    let value (String500 str) = str

    /// Create a String500 from a string
    /// Return Error if input is null, empty, or length > 500
    let create fieldName str = 
        ConstrainedType.createString fieldName String500 500 str

    /// Create an array of String500 from a string array
    /// Return Error if input is null, empty, or length > 500
    let createArr fieldName arr = 
      arr |> Array.map (create fieldName) |> Array.toList |> Result.sequence

    /// Create a String500 from a string
    /// Return None if input is null, empty. 
    /// Return error if length > maxLen
    /// Return Some if the input is valid
    let createOption fieldName str = 
        ConstrainedType.createStringOption fieldName String500 500 str

  module Vid = 
    let value (Vid guid) = guid

    let create (str:string) = 
      let result, value = Guid.TryParse(str)
      match result with
      | true -> Ok value
      | false -> Error <| sprintf "Failed to parse Vid: %s" str

  module DefaultIssueTypes =

    let value =
      let result = 
          [ 
            ("Task"            |> String50.create "Default Issue Types"); 
            ("Missing Parts"   |> String50.create "Default Issue Types");
            ("Parts Shortages" |> String50.create "Default Issue Types"); 
            ("Issue"           |> String50.create "Default Issue Types"); 
            ("Potential Issue" |> String50.create "Default Issue Types"); 
            ("Rectification"   |> String50.create "Default Issue Types"); 
            ("Enhancement"     |> String50.create "Default Issue Types"); 
          ] |> Result.sequence
      match result with
      | Ok r -> r
      | Error err -> failwithf "Error creating Default Issue Types - %s" err

  module Status =

    let [<Literal>] private UNOPENED   = "Unopened"
    let [<Literal>] private OPEN       = "Open"
    let [<Literal>] private INPROGRESS = "In Progress"
    let [<Literal>] private VERIFY     = "Verify"
    let [<Literal>] private CLOSED     = "Closed"

    let toStr s =
      match s with
      | Unopened   -> UNOPENED
      | Open       -> OPEN
      | InProgress -> INPROGRESS
      | Verify     -> VERIFY
      | Closed     -> CLOSED

    let fromStr status =
      match status with
      | UNOPENED   -> Ok Unopened
      | OPEN       -> Ok Open
      | INPROGRESS -> Ok InProgress
      | VERIFY     -> Ok Verify
      | CLOSED     -> Ok Closed
      | _             -> Error <| sprintf "Failed to parse Status: %s" status

    let statusOptions =
      [
        UNOPENED
        OPEN
        INPROGRESS
        VERIFY
        CLOSED
      ]

  [<AutoOpen>]
  module Error =
    let mapStorageErr str = Result.Error str |> Result.mapError StorageError
    let mapAppErr str     = Result.Error str |> Result.mapError ApplicationError
  
    let value (mErr:YgiError) =
      match mErr with
      | StorageError err -> err
      | ApplicationError err -> err