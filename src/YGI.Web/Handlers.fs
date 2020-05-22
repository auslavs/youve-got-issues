module Handlers
  open Giraffe
  open Microsoft.AspNetCore.Http
  open FSharp.Control.Tasks.V2
  open YGI
  open YGI.Common
  open YGI.Dto
  open YGI.Events
  open System
  open System.IO
  open System.Text
  open Newtonsoft.Json
  open Microsoft.AspNetCore.Mvc
  open Microsoft.Extensions.Primitives

  let GetRequestId (ctx : HttpContext) = 
    let result,cid = ctx.Items.TryGetValue "MS_AzureFunctionsRequestID"
    match result with
    | false -> failwith "Failed to get request id"
    | true -> cid.ToString()

  let getProjectList =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let! summary = Storage.getProjectSummary ()
        return! (Successful.OK summary) next ctx 
      }

  let getUserDetails (ctx : HttpContext) =
    let logger = Logging.log <| ctx.GetLogger()
    let user = ctx.User
    if user = null then failwith "Failed to retrieve user"

    try 
#if DEBUG
      {
        GivenName = "GivenName"
        Surname   = "Surname"
        Email     = "Email"
        Upn       = "Upn"
      }
#else
      let givenName = user.Claims |> Seq.find(fun c -> c.Type = Constants.Claims.GivenName)
      logger Logging.Info <| sprintf "GivenName: %s" givenName.Value

      let surname = user.Claims |> Seq.find(fun c -> c.Type = Constants.Claims.Surname)
      logger Logging.Info <| sprintf "Surname: %s" surname.Value

      let email = user.Claims |> Seq.find(fun c -> c.Type = Constants.Claims.Email)
      logger Logging.Info <| sprintf "Email: %s" email.Value

      let upn = user.Claims |> Seq.find(fun c -> c.Type = Constants.Claims.Upn)
      logger Logging.Info <| sprintf "Upn: %s" upn.Value
    
      {
        GivenName = givenName.Value
        Surname   = surname.Value
        Email     = email.Value
        Upn       = upn.Value
      }

#endif
    with _ -> failwith "User not logged in."


  let getClaims =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let logger = Logging.log <| ctx.GetLogger()
        let user = ctx.User

        if user = null then failwith "Failed to retrieve user"

        let claims : string = 
            (StringBuilder(), user.Claims)
            ||> Seq.fold (fun sb claim -> sb.AppendFormat("{0}\t{1}\n", claim.Type, claim.Value ))
            |> string

        logger Logging.Info claims

        return! (Successful.OK claims) next ctx 
      }

  let getUser =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let logger = Logging.log <| ctx.GetLogger()
        let user = getUserDetails ctx

        let ygiUserStr = JsonConvert.SerializeObject(user)

        logger Logging.Info ygiUserStr

        return! (Successful.OK user) next ctx 
      }

  let getProjectIssuesList proj =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
          let logger = Logging.log <| ctx.GetLogger()
          let! state = Storage.getProject logger proj ()
          let updateStatus (s:ProjectStateDto) = { s with StatusTypes = Status.statusOptions |> List.toArray }
          match state with 
          | Some s -> 
            return! (Successful.OK (updateStatus s)) next ctx 
          | None -> 
            let response = RequestErrors.NOT_FOUND "Page not found"
            return! response next ctx            
        }

  let createNewProject =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let cid = GetRequestId ctx
        let logger = Logging.log <| ctx.GetLogger()
        let user = getUserDetails ctx
        let! dto = ctx.BindJsonAsync<NewProjectDto>()

        let! taskResult = taskResult {
          let event = YgiEvent.create cid user dto.ProjectNumber dto
          return! Api.CreateNewProject logger event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }

  let createNewIssue projNum : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let cid = GetRequestId ctx
        let logger = Logging.log <| ctx.GetLogger()
        let user = getUserDetails ctx
        let! dto = ctx.BindJsonAsync<NewIssueDto>()

        let! taskResult = taskResult {
          let event = YgiEvent.create cid user projNum dto
          return! Api.CreateNewIssue logger projNum event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }

  let getIssueDetail proj id : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let logger = Logging.log <| ctx.GetLogger()

        let! taskResult = taskResult {
          return! Api.GetIssueModel logger proj id
        }

        let response =
          match taskResult with
          | Ok issueDetail -> (Successful.OK issueDetail) next ctx
          | Error err -> (RequestErrors.NOT_FOUND err) next ctx

        return! response           
      }

  let updateIssue projNum _ : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let cid = GetRequestId ctx
        let logger = Logging.log <| ctx.GetLogger()
        let user = getUserDetails ctx
        let! dto = ctx.BindJsonAsync<IssueUpdateDto>()

        let! taskResult = taskResult {
          let event = YgiEvent.create cid user projNum dto
          return! Api.UpdateIssue logger projNum event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }

  let addComment projNum issueNo : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let cid = GetRequestId ctx
        let logger = Logging.log <| ctx.GetLogger()
        let user = getUserDetails ctx
        let! dto = ctx.BindFormAsync<NewCommentDto>()
        let username = sprintf "%s %s" user.GivenName user.Surname
        let dto = { dto with CommentBy = username}

        let! taskResult = taskResult {
          let event = YgiEvent.create cid user projNum dto
          return! Api.AddNewComment logger projNum issueNo event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }

  let uploadAttachment projNum issueItemNo : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let cid = GetRequestId ctx
        let logger = Logging.log <| ctx.GetLogger()
        let user = getUserDetails ctx

        let uploadAttachment = YGI.Storage.uploadAttachment logger
        let createEvent      = YgiEvent.create cid user projNum

        let toFileStream seq (file:IFormFile) = 
          let stream = new MemoryStream()
          file.CopyTo(stream)
          stream.Seek(0L, SeekOrigin.Begin) |> ignore
          {
            Seq           = seq
            Id            = Guid.NewGuid().ToString()
            ProjectNumber = projNum
            IssueItemNo   = issueItemNo
            Filename      = file.FileName
            Extension     = Path.GetExtension(file.FileName)
            ContentType   = file.ContentType
            Stream        = stream
          } 

        /// Convert to domain friendly Attachment Stream
        let files = ctx.Request.Form.Files |> Seq.mapi (fun i f -> toFileStream i f)

        /// Upload each attachment
        let! uploadDetails = 
          files
          |> Seq.map uploadAttachment
          |> Threading.Tasks.Task.WhenAll

        let uploadEvent = createEvent uploadDetails
        let! result = Api.AddAttachments logger projNum uploadEvent

        let response =
          match result with
          | Result.Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
       }

  let exportProject projNum : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let updateStatus (s:ProjectStateDto) = { s with StatusTypes = Status.statusOptions |> List.toArray }
        let logger = Logging.log <| ctx.GetLogger()
        let! opt = Storage.getProject logger projNum ()
        let dto = 
          match opt with
          | Some dto -> updateStatus dto
          | None -> 
            failwith "Failed to export project"

        let bytes = ExcelExport.Export logger dto ()
        let contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        let headers = 
          dict [
            "Content-Disposition", StringValues("attachment");
            "filename",  StringValues(projNum + ".xlsx") ]

        ctx.Response.ContentType <- contentType
        headers |> Seq.iter ctx.Response.Headers.Add
        ctx.Response.ContentLength <- bytes.Length |> int64 |> Nullable

        return! ctx.WriteBytesAsync (bytes)
      }