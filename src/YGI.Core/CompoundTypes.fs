namespace YGI.Common
  
  open System

  type YgiEvent<'T> = {
    Cid           : string
    ProjectNumber : string
    State         : 'T
  }

  type UnvalidatedNewIssue = {
    Area        : String50
    Equipment   : String50
    IssueType   : String50
    Title       : String100
    Description : String500
    RaisedBy    : String100
  }

  type AttachmentDetails = {
    Id            : string
    ProjectNumber : string
    IssueItemNo   : int
    Filename      : string
    Extension     : string
    ContentType   : string
    RelativeUrl   : string
  }

  type Issue = {
    ItemNo      : int
    Area        : String50
    Equipment   : String50
    IssueType   : String50
    Title       : String100
    Description : String500
    Comments    : String500 list
    Resolution  : String500 option
    Attachments : AttachmentDetails list
    RaisedBy    : String100
    Status      : Status
    Raised      : DateTime
    LastChanged : DateTime
    DateClosed  : DateTime option
    Vid         : Guid
  }

  type IssueUpdate = {
    ItemNo      : int
    Title       : String100
    Description : String500
    Area        : String50
    Equipment   : String50
    IssueType   : String50
    Status      : Status
    Vid         : Guid
  }

  type ProjectState = {
    ProjectNumber  : String50
    ProjectName    : String50
    AreaList       : String50 list 
    EquipmentTypes : String50 list
    IssueTypes     : String50 list
    Issues         : Issue list
    LastChanged    : DateTime
    Vid            : Guid
  }

  type ProjectSummary = {
    ProjectNumber  : String50
    ProjectName    : String50
    OpenIssues     : int
  }

  type Summary = {
    ProjList    : ProjectSummary list
    LastChanged : DateTime
  }

  module Issue =
    let create itemNo (uni:UnvalidatedNewIssue) : Issue =
      {
        ItemNo      = itemNo
        Area        = uni.Area
        Equipment   = uni.Equipment
        IssueType   = uni.IssueType
        Title       = uni.Title
        Description = uni.Description
        Comments    = []
        Resolution  = None
        Attachments = []
        RaisedBy    = uni.RaisedBy
        Status      = Status.Unopened
        Raised      = DateTime.Now
        LastChanged = DateTime.Now
        DateClosed  = None
        Vid         = new Guid()
      }

    let update (issue:Issue) (update:IssueUpdate) =
      if issue.ItemNo = update.ItemNo && issue.Vid = update.Vid then
        Ok {
          issue with 
            Title       = update.Title
            Description = update.Description
            Area        = update.Area
            Equipment   = update.Equipment
            IssueType   = update.IssueType
            Status      = update.Status
            LastChanged = DateTime.Now
            Vid         = Guid.NewGuid()
        }
      else 
        Error <| sprintf "Failed to update Issue: %A, Update: %A" issue update

    let addAttachment (issue:Issue) (attachment:AttachmentDetails) =
      if issue.ItemNo = attachment.IssueItemNo then

        let attachments = 
          attachment::issue.Attachments
          |> List.sortBy(fun a -> a.Filename)

        Ok {
          issue with 
            Attachments = attachments
            LastChanged = DateTime.Now
            Vid         = Guid.NewGuid()
        }
      else 
        Error <| sprintf "Failed to add attchment to Issue: %A, Attachment: %A" issue attachment

    let addAttachments (issue:Issue) (attachments:AttachmentDetails list) =

      let rec loop i aLst =
        match aLst with
        | [] -> Ok i
        | x::xs -> 
          let result = addAttachment i x
          match result with 
          | Ok updatedIssue -> loop updatedIssue xs
          | Error err -> Error err

      loop issue attachments

  module ProjectState =

    let toProjectNumberStr (state:ProjectState) = 
      let (String50 projNum) = state.ProjectNumber
      projNum

    let numberOfOpenIssues (state:ProjectState) =
      state.Issues 
      |> List.filter(fun i -> i.Status <> Closed) 
      |> List.length

    let private getNextIssueNumber (issuesList:Issue list) =
      let add1 i = i + 1
      match issuesList with
      | [] -> 1
      | lst -> lst |> List.map (fun i -> i.ItemNo) |> List.max |> add1

    let private distinctAndSort (lst:'T list when 'T : equality) : 'T list =
      lst |> List.distinct |> List.sort

    let private getAreaList (issuesList:Issue list) =
      issuesList |> List.map (fun i -> i.Area) |> distinctAndSort

    let private getEquipmentList (issuesList:Issue list) =
      issuesList |> List.map (fun i -> i.Area) |> distinctAndSort

    let private getIssueTypesList (issuesList:Issue list) =
      let lst1 = issuesList |> List.map (fun i -> i.IssueType) |> List.distinct
      [ DefaultIssueTypes.value; lst1 ] |> List.concat |> distinctAndSort

    let private addOrReplace (lst:Issue list) (issue:Issue) =
      let rec search (input:Issue list) acc found = 
        match input with
        | [] -> if found then acc else (issue::acc)
        | x::xs ->
          if issue.ItemNo = x.ItemNo then 
            search xs (issue::acc) true
          else
            search xs (x::acc) found
      search lst [] false

    /// <summary>
    /// Adds a new issue to the IssueListState and returns the new state.
    /// </summary>
    /// <param name="state">Existing state that the issue is to be added to</param>
    /// <param name="issue">Issue to be added to the state</param>
    /// <returns>A new state with the new issue added to teh issues list/returns>
    let addIssue (state:ProjectState) (issue:UnvalidatedNewIssue) = 

      /// Create a new issue and append it to the issues list
      let newIssueNum = getNextIssueNumber state.Issues  
      let newIssue = issue |> Issue.create newIssueNum
      let issues = (newIssue::state.Issues) |> List.sortBy (fun i -> i.ItemNo)

      /// Recalculate the types lists
      let areaLst    = issues |> getAreaList
      let equipLst   = issues |> getEquipmentList
      let issueTypes = issues |> getIssueTypesList

      { state with 
          AreaList = areaLst; 
          EquipmentTypes = equipLst; 
          IssueTypes = issueTypes; 
          Issues = issues; 
          LastChanged = DateTime.Now
          Vid = new Guid()
      }

    let updateIssue (state:ProjectState) (update:IssueUpdate) =
      
      let result = 
        state.Issues 
        |> List.tryFind (fun i -> i.ItemNo = update.ItemNo)
        |> Option.map (fun i -> Issue.update i update)
        |> function
        | Some issueResult -> issueResult
        | None -> Error <| sprintf "Could not find Issue %i" update.ItemNo

      result 
      |> Result.map (addOrReplace state.Issues)
      |> Result.bind (fun newIssuesList -> Ok { state with Issues = newIssuesList } )

    let addAttachments (state:ProjectState) (attachments:AttachmentDetails list) =

      let getItemNo (aLst:AttachmentDetails list) =

        let itemNo = function i -> i.IssueItemNo

        let (|Empty|AllMatch|NoMatch|) (lst:AttachmentDetails list) =
          if lst.IsEmpty then 
            Empty
          elif lst.Length = 1 then AllMatch
          else
            let head = lst |> List.head |> itemNo
            let tail = lst |> List.tail |> List.map itemNo
            if tail |> List.forall((=) head) then AllMatch else NoMatch
        
        match aLst with
        | Empty  -> Error <| sprintf "Could not find any attachments"
        | AllMatch -> Ok (aLst |> List.head |> itemNo)
        | NoMatch -> Error <| sprintf "Attachments must all be for the same issue %A" attachments

      let findIssue itemNo = 
        state.Issues 
        |> List.tryFind (fun i -> i.ItemNo = itemNo)
        |> function
        | Some issueResult -> Ok issueResult
        | None -> Error <| sprintf "Could not find Issue %i" itemNo

      result {
        let! itemNo = getItemNo attachments
        let! issue = findIssue itemNo
        let! updatedIssue = Issue.addAttachments issue attachments
        let  updatedIssuesLst = addOrReplace state.Issues updatedIssue
        return { state with Issues = updatedIssuesLst }
      }

    let toProjectSummary (state:ProjectState) =
      {
        ProjectNumber  = state.ProjectNumber
        ProjectName    = state.ProjectName
        OpenIssues     = numberOfOpenIssues state
      }

  module Summary =

    let init =
      {
        Summary.ProjList = []
        LastChanged = DateTime.Now
      }

    let addOrReplaceProject (summary:Summary) projSummary = 

      let addOrReplace (lst:ProjectSummary list) (proj:ProjectSummary) =
        let rec search input acc found = 
          match input with
          | [] -> if found then acc else (proj::acc)
          | x::xs ->
            if proj.ProjectNumber = x.ProjectNumber then 
              search xs (proj::acc) true
            else
              search xs (x::acc) found
        search lst [] false

      let projList = 
        addOrReplace summary.ProjList projSummary
        |> List.sortByDescending (fun x -> x.ProjectNumber)

      {summary with ProjList = projList}

    


