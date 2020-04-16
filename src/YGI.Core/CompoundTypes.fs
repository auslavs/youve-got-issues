namespace YGI.Common
  
  open System

  type YgiEvent<'T> = {
    State : 'T
    Cid   : string
  }

  type UnvalidatedNewIssue = {
    Area        : String50
    Equipment   : String50
    IssueType   : String50
    Issue       : String500
    RaisedBy    : String100
  }

  type Issue = {
    ItemNo      : int
    Area        : String50
    Equipment   : String50
    IssueType   : String50
    Issue       : String500
    Comments    : String500 list
    Resolution  : String500 option
    RaisedBy    : String100
    Status      : Status
    Raised      : DateTime
    LastChanged : DateTime
    DateClosed  : DateTime option
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
        Issue       = uni.Issue
        Comments    = []
        Resolution  = None
        RaisedBy    = uni.RaisedBy
        Status      = Status.Unopened
        Raised      = DateTime.Now
        LastChanged = DateTime.Now
        DateClosed  = None
        Vid         = new Guid()
      }

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
      let issues = (newIssue::state.Issues)

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

    let addProject (summary:Summary) projSummary = 
      
      let projList = 
        projSummary::summary.ProjList 
        |> List.sortByDescending (fun x -> x.ProjectNumber)

      {summary with ProjList = projList}

    


