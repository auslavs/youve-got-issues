module Issues.Updates

open System
open Xunit
open YGI.Common
open YGI.Dto
open TestHelpers


/// Base issue test data we will use to test our updates
let guid        = Guid.NewGuid();
let area        = String50.create "Area" "Area" |> unpackResult
let equipment   = String50.create "Equipment" "Equipment" |> unpackResult
let issueType   = String50.create "IssueType" "IssueType" |> unpackResult
let title       = String100.create "Title" "Title" |> unpackResult
let description = String500.create "Description" "Description" |> unpackResult
let raisedBy    = String100.create "RaisedBy" "RaisedBy" |> unpackResult
let date        = DateTime.Parse("12/10/2019")

let TitlesEqual       (o:Issue,u:Issue) = Assert.Equal(o.Title, u.Title); (o,u)
let DescriptionsEqual (o:Issue,u:Issue) = Assert.Equal(o.Description, u.Description); (o,u)
let AreasEqual        (o:Issue,u:Issue) = Assert.Equal(o.Area, u.Area); (o,u)
let EquipmentsEqual   (o:Issue,u:Issue) = Assert.Equal(o.Equipment, u.Equipment); (o,u)
let IssueTypesEqual   (o:Issue,u:Issue) = Assert.Equal(o.IssueType, u.IssueType); (o,u)
let RaisedBysEqual    (o:Issue,u:Issue) = Assert.Equal(o.RaisedBy, u.RaisedBy); (o,u)
let CommentsEqual     (o:Issue,u:Issue) = Assert.True(listEquals o.Comments u.Comments); (o,u)



/// Base issue we will use to test our updates
let issue = 
  {
    Issue.ItemNo = 1
    Area         = area
    Equipment    = equipment
    IssueType    = issueType
    Title        = title
    Description  = description
    Comments     = []
    Resolution   = None
    Attachments  = []
    RaisedBy     = raisedBy
    Status       = Status.Unopened
    Raised       = date
    LastChanged  = date
    DateClosed   = None
    Vid          = guid
  }

[<Fact>]
let ``Update Title`` () =
  
  let newdata      = ranStr 100
  let titleUpdated = String100.create "Title" newdata |> unpackResult
  
  let update : IssueUpdate =
    {
      ItemNo      = 1
      Title       = titleUpdated
      Description = description
      Area        = area
      Equipment   = equipment
      IssueType   = issueType
      Status      = Status.Unopened
      Vid         = guid
    }
  
  let result = Issue.update issue update
  
  let updatedIssue = 
    match result with
    | Ok i -> i
    | Error err -> failwithf "%A" err
  
  /// Check appropriate fields have been updated
  Assert.NotEqual(issue.Vid, updatedIssue.Vid)
  Assert.NotEqual(issue.Title, updatedIssue.Title)
  Assert.Equal(updatedIssue.Title, titleUpdated)
  Assert.True (updatedIssue.LastChanged > issue.LastChanged)
  
  /// Check appropriate fields remain the same

  Assert.Equal(issue.Description, updatedIssue.Description)
  Assert.Equal(issue.Area, updatedIssue.Area)
  Assert.Equal(issue.Equipment, updatedIssue.Equipment)
  Assert.Equal(issue.IssueType, updatedIssue.IssueType)
  Assert.True(listEquals issue.Comments updatedIssue.Comments)
  Assert.Equal(issue.Resolution, updatedIssue.Resolution)
  Assert.Equal(issue.Raised, updatedIssue.Raised)
  Assert.Equal(issue.RaisedBy, updatedIssue.RaisedBy)
  Assert.Equal(issue.Status, updatedIssue.Status)
  Assert.Equal(issue.DateClosed, updatedIssue.DateClosed)

[<Fact>]
let ``Update Description`` () =
  
  let newdata = ranStr 500
  let descriptionUpdated = String500.create "Description" newdata |> unpackResult
  
  let update : IssueUpdate =
    {
      ItemNo      = 1
      Title       = title
      Description = descriptionUpdated
      Area        = area
      Equipment   = equipment
      IssueType   = issueType
      Status      = Status.Unopened
      Vid         = guid
    }
  
  let result = Issue.update issue update
  
  let updatedIssue = 
    match result with
    | Ok i -> i
    | Error err -> failwithf "%A" err

 
  /// Check appropriate fields have been updated
  Assert.NotEqual(issue.Vid, updatedIssue.Vid)
  Assert.NotEqual(issue.Description, updatedIssue.Description)
  Assert.Equal(updatedIssue.Description, descriptionUpdated)
  Assert.True (updatedIssue.LastChanged > issue.LastChanged)
  
  /// Check appropriate fields remain the same
  (issue, updatedIssue)
  |> TitlesEqual
  |> AreasEqual
  |> IssueTypesEqual
  |> CommentsEqual
  |> ignore

[<Fact>]
let ``Update Area`` () =

  let areaUpdated = String50.create "Area Update" "Update" |> unpackResult

  let update : IssueUpdate =
    {
      ItemNo      = 1
      Title       = title
      Description = description
      Area        = areaUpdated
      Equipment   = equipment
      IssueType   = issueType
      Status      = Status.Unopened
      Vid         = guid
    }

  let result = Issue.update issue update

  let updatedIssue = 
    match result with
    | Ok i -> i
    | Error err -> failwithf "%A" err

  /// Check appropriate fields have been updated
  Assert.NotEqual(issue.Vid, updatedIssue.Vid)
  Assert.NotEqual(issue.Area, updatedIssue.Area)
  Assert.Equal(updatedIssue.Area, areaUpdated)
  Assert.True (updatedIssue.LastChanged > issue.LastChanged)

  /// Check appropriate fields remain the same
  Assert.Equal(issue.Title, updatedIssue.Title)
  Assert.Equal(issue.Description, updatedIssue.Description)
  Assert.Equal(issue.Equipment, updatedIssue.Equipment)
  Assert.Equal(issue.IssueType, updatedIssue.IssueType)
  Assert.Equal(issue.Status, updatedIssue.Status)
  Assert.True(listEquals issue.Comments updatedIssue.Comments)
  Assert.Equal(issue.Resolution, updatedIssue.Resolution)
  Assert.Equal(issue.Raised, updatedIssue.Raised)
  Assert.Equal(issue.RaisedBy, updatedIssue.RaisedBy)
  Assert.Equal(issue.DateClosed, updatedIssue.DateClosed)
