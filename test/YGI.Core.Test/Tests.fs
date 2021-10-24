module Issues

open System
open Xunit
open YGI.Common
open YGI.Dto
open TestHelpers


/// Base issue test data we will use to test our updates
let guid        = Guid.NewGuid()
let area        = String50.create "Area" "Area" |> unpackResult
let equipment   = String50.create "Equipment" "Equipment" |> unpackResult
let issueType   = String50.create "IssueType" "IssueType" |> unpackResult
let title       = String100.create "Title" "Title" |> unpackResult
let description = String500.create "Description" "Description" |> unpackResult
let raisedBy    = String100.create "RaisedBy" "RaisedBy" |> unpackResult
let date        = DateTime.Parse("12/10/2019")


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


[<Fact>]
let ``Update with incorrect Virtual Id`` () =

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
      Vid         = Guid.NewGuid()
    }

  let result = Issue.update issue update

  match result with
  | Ok _ -> failwith "Issue should not have been updated"
  | Error err -> Assert.True(err.StartsWith("Failed to update Issue:"))

[<Fact>]
let ``Update with incorrect ItemNo`` () =

  let areaUpdated = String50.create "Area Update" "Update" |> unpackResult

  let update : IssueUpdate =
    {
      ItemNo      = 2
      Title       = title
      Description = description
      Area        = areaUpdated
      Equipment   = equipment
      IssueType   = issueType
      Status      = Status.Unopened
      Vid         = guid
    }

  let result = Issue.update issue update

  match result with
  | Ok _ -> failwith "Issue should not have been updated"
  | Error err -> Assert.True(err.StartsWith("Failed to update Issue:"))
