namespace YGI.Dto

  open System
  open YGI.Common
  open System.IO

  type AttachmentStream = {
    Seq           : int
    Id            : string
    ProjectNumber : string
    IssueItemNo   : int
    Filename      : string
    Extension     : string
    ContentType   : string
    Stream        : Stream
  }

  [<CLIMutable>]
  type AttachmentDetailsDto = {
    Id            : string
    ProjectNumber : string
    IssueItemNo   : int
    Filename      : string
    Extension     : string
    ContentType   : string
    RelativeUrl   : string
  }

  module AttachmentDetailsDto = 
    let toAttachmentDetails (dto:AttachmentDetailsDto) : Result<AttachmentDetails, string> = 
      result {
        return {
          Id            = dto.Id
          ProjectNumber = dto.ProjectNumber
          IssueItemNo   = dto.IssueItemNo
          Filename      = dto.Filename
          Extension     = dto.Extension
          ContentType   = dto.ContentType
          RelativeUrl   = dto.RelativeUrl
        }
      }

    let fromAttachmentDetails (a:AttachmentDetails) : AttachmentDetailsDto = 
      {
        Id            = a.Id
        ProjectNumber = a.ProjectNumber
        IssueItemNo   = a.IssueItemNo
        Filename      = a.Filename
        Extension     = a.Extension
        ContentType   = a.ContentType
        RelativeUrl   = a.RelativeUrl
      }

  [<CLIMutable>]
  type NewIssueDto = {
    ProjectNumber : string
    Area          : string
    Equipment     : string
    IssueType     : string
    Title         : string
    Description   : string
    RaisedBy      : string
  }

  module internal NewIssueDto =

    let toUnvalidatedIssue (dto:NewIssueDto) =
      result {
        let! area        = dto.Area        |> String50.create  "Area"
        let! equip       = dto.Equipment   |> String50.create  "Equipment"
        let! iType       = dto.IssueType   |> String50.create  "Type"
        let! title       = dto.Title       |> String100.create "Title"
        let! description = dto.Description |> String500.create "Description"
        let! raisedBy    = dto.RaisedBy    |> String100.create "RaisedBy"

        let domainObj : UnvalidatedNewIssue = {
            Area        = area
            Equipment   = equip
            IssueType   = iType
            Title       = title
            Description = description
            RaisedBy    = raisedBy
          } 
        
        return domainObj
      }

  
  [<CLIMutable>]
  type IssueDto = {
    ItemNo      : int
    Area        : string
    Equipment   : string
    IssueType   : string
    Title       : string
    Description : string
    Comments    : string []
    Resolution  : string
    Attachments : AttachmentDetailsDto []
    RaisedBy    : string
    Status      : string
    Raised      : DateTime
    LastChanged : DateTime
    DateClosed  : DateTime option
    Vid         : Guid
  }

  module internal IssueDto =

    let toIssue (dto:IssueDto) : Result<Issue,string> =     
      result {

        let string500ArrayHelper fieldName arr = 
          arr |> Array.map (String500.create fieldName) |> Array.toList |> Result.sequence

        let attachmentArrayHelper arr = 
          match arr with
          | null -> Ok []
          | _ -> arr |> Array.map (AttachmentDetailsDto.toAttachmentDetails) |> Array.toList |> Result.sequence

        let! area        = String50.create "Area" dto.Area
        let! equip       = String50.create "Equipment" dto.Equipment
        let! issueType   = String50.create "IssueType" dto.IssueType
        let! title       = String100.create "Title" dto.Title
        let! description = String500.create "Description" dto.Description
        let! comments    = string500ArrayHelper "Comments" dto.Comments
        let! resolution  = String500.createOption "Resolution" dto.Resolution
        let! attachments = attachmentArrayHelper dto.Attachments
        let! raisedBy    = String100.create "RaisedBy" dto.RaisedBy
        let! status      = Status.fromStr dto.Status

        return 
          {
            ItemNo      = dto.ItemNo
            Area        = area
            Equipment   = equip
            IssueType   = issueType
            Title       = title
            Description = description
            Comments    = comments
            Resolution  = resolution
            Attachments = attachments
            RaisedBy    = raisedBy
            Status      = status
            Raised      = dto.Raised
            LastChanged = dto.LastChanged
            DateClosed  = dto.DateClosed
            Vid         = dto.Vid
          }
      }

    let fromIssue (i:Issue) = {
      ItemNo      = i.ItemNo
      Area        = String50.value i.Area
      Equipment   = String50.value i.Equipment
      IssueType   = String50.value i.IssueType
      Title       = String100.value i.Title
      Description = String500.value i.Description
      Comments    = i.Comments |> List.map String500.value |> List.toArray
      Resolution  = i.Resolution |> Option.map String500.value |> Option.defaultValue ""
      Attachments = i.Attachments |> List.map AttachmentDetailsDto.fromAttachmentDetails |> List.toArray
      RaisedBy    = String100.value i.RaisedBy
      Status      = Status.toStr i.Status
      Raised      = i.Raised
      LastChanged = i.LastChanged
      DateClosed  = i.DateClosed
      Vid         = i.Vid
    }


  [<CLIMutable>]
  type NewProjectDto = {
    ProjectNumber  : string
    ProjectName    : string
  }

  module internal NewProjectDto =

    let toProjectState (dto:NewProjectDto) : Result<ProjectState,string> = 
      result {
        let! projNum  = String50.create "ProjectNumber" dto.ProjectNumber
        let! projName = String50.create "ProjectName" dto.ProjectName

        let state = 
          {
            ProjectNumber  = projNum
            ProjectName    = projName
            AreaList       = []
            EquipmentTypes = []
            IssueTypes     = DefaultIssueTypes.value
            Issues         = []
            LastChanged    = DateTime.Now
            Vid            = new Guid()
          }

        return state
      }

  [<CLIMutable>]
  type ProjectStateDto = {
    ProjectNumber  : string
    ProjectName    : string
    AreaList       : string []
    EquipmentTypes : string [] 
    IssueTypes     : string []
    Issues         : IssueDto []
    LastChanged    : DateTime
    Vid            : Guid
  }

  module internal ProjectStateDto =

    let toProjectState (dto:ProjectStateDto) : Result<ProjectState,string> = 
      result {

        let string50ArrayHelper fieldName arr = 
          arr |> Array.map (String50.create fieldName) |> Array.toList |> Result.sequence

        let! projNum    = String50.create "ProjectNumber" dto.ProjectNumber
        let! projName   = String50.create "ProjectName" dto.ProjectName
        let! areaLst    = string50ArrayHelper "AreaList" dto.AreaList
        let! equipTypes = string50ArrayHelper "EquipmentTypes" dto.EquipmentTypes
        let! issueTypes = string50ArrayHelper "IssueTypes" dto.IssueTypes
        let! issues     = dto.Issues
                          |> Array.sortBy(fun i -> i.ItemNo)
                          |> Array.map IssueDto.toIssue 
                          |> Array.toList 
                          |> Result.sequence

        return
          {
            ProjectNumber  = projNum
            ProjectName    = projName
            AreaList       = areaLst
            EquipmentTypes = equipTypes
            IssueTypes     = issueTypes
            Issues         = issues
            LastChanged    = dto.LastChanged
            Vid            = dto.Vid
          }
      }

    let fromProjectState (state:ProjectState) : ProjectStateDto = 
      {
        ProjectNumber  = String50.value state.ProjectNumber
        ProjectName    = String50.value state.ProjectName
        AreaList       = state.AreaList |> List.map String50.value |> List.toArray
        EquipmentTypes = state.EquipmentTypes |> List.map String50.value |> List.toArray
        IssueTypes     = state.IssueTypes |> List.map String50.value |> List.toArray
        Issues         = state.Issues |> List.map IssueDto.fromIssue |> List.toArray
        LastChanged    = state.LastChanged
        Vid            = state.Vid
      
      }

  [<CLIMutable>]
  type IssueDetailViewDto = {
    ProjectNumber  : string
    ProjectName    : string
    AreaList       : string []
    EquipmentTypes : string []
    IssueTypes     : string []
    StatusTypes    : string []
    Issue          : IssueDto
  }

  module internal IssueDetailViewDto =
    let fromProjectStateDto issueNum (state:ProjectStateDto)  : Result<IssueDetailViewDto,string> = 

      let issueOpt = state.Issues |> Array.tryFind (fun i -> i.ItemNo = issueNum)

      match issueOpt with
      | None -> Result.Error <| sprintf "Could not find Issue: %i" issueNum
      | Some issue -> 
        let view = 
          {
            ProjectNumber  = state.ProjectNumber
            ProjectName    = state.ProjectName
            AreaList       = state.AreaList
            EquipmentTypes = state.EquipmentTypes
            IssueTypes     = state.IssueTypes
            StatusTypes    = Status.statusOptions |> List.toArray
            Issue          = issue
          }
        Result.Ok view
    
  [<CLIMutable>]
  type IssueUpdateDto = {
    ItemNo      : int
    Title       : string
    Description : string
    Area        : string
    Equipment   : string
    IssueType   : string
    Status      : string
    Vid         : string
  }

  module IssueUpdateDto = 
    let toIssueUpdate (dto:IssueUpdateDto) : Result<IssueUpdate, string> = 
      result {
        let! title       = String100.create "Title" dto.Title
        let! description = String500.create "Description" dto.Description
        let! area        = String50.create "Area" dto.Area
        let! equip       = String50.create "Equipment" dto.Equipment
        let! issueType   = String50.create "IssueType" dto.IssueType
        let! status      = Status.fromStr dto.Status
        let! vid         = Vid.create dto.Vid

        return {
          ItemNo      = dto.ItemNo
          Title       = title
          Description = description
          Area        = area
          Equipment   = equip
          IssueType   = issueType
          Status      = status
          Vid         = vid
        }
      }


  [<CLIMutable>]
  type ProjectSummaryDto = {
    ProjectNumber  : string
    ProjectName    : string
    OpenIssues     : string
  }

  module internal ProjectSummaryDto =

    let toProjectSummaryDto (summary:ProjectSummary) = 
      {
        ProjectNumber  = String50.value summary.ProjectNumber
        ProjectName    = String50.value summary.ProjectName
        OpenIssues     = summary.OpenIssues |> string
      }

    let fromProjectSummaryDto (dto:ProjectSummaryDto) : Result<ProjectSummary, string> = 
      result {
        let! projNum    = String50.create "ProjectNumber" dto.ProjectNumber
        let! projName   = String50.create "ProjectName" dto.ProjectName
        let! openIssues = Int.create "OpenIssues" dto.OpenIssues

        return {
          ProjectNumber  = projNum
          ProjectName    = projName
          OpenIssues     = openIssues
        }
      }


  [<CLIMutable>]
  type SummaryDto = {
    ProjList    : ProjectSummaryDto []
    LastChanged : DateTime
  }

  module internal SummaryDto =

    let toSummaryDto (summary:Summary) = 
      {
        ProjList    = summary.ProjList |> List.map ProjectSummaryDto.toProjectSummaryDto |> List.toArray
        LastChanged = summary.LastChanged
      }

    let fromSummaryDto (dto:SummaryDto) : Result<Summary,string> = 
      result {
        let! projList = 
          dto.ProjList 
          |> Array.toList 
          |> List.map ProjectSummaryDto.fromProjectSummaryDto 
          |> Result.sequence

        return {
            ProjList    = projList
            LastChanged = dto.LastChanged
          }
      }
      

      