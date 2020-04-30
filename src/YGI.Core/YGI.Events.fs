namespace YGI.Events

  open YGI.Common
  open YGI.Dto
  open Microsoft.Azure.Cosmos.Table

  type YgiEvent =
    | AddNewProject of YgiEvent<NewProjectDto>
    | AddNewIssue of YgiEvent<NewIssueDto>

  type AddNewProjectEvent(evt:YgiEvent<NewProjectDto>) =
    inherit TableEntity(partitionKey=evt.State.ProjectNumber, rowKey=evt.Cid)
    new() = AddNewProjectEvent()

  type AddNewIssueEvent(evt:YgiEvent<NewIssueDto>) =
    inherit TableEntity(partitionKey=evt.State.ProjectNumber, rowKey=evt.Cid)
    new() = AddNewIssueEvent()

  module AddNewProjectEvent =

    let create state cid =
      { State = state; Cid = cid }

  module AddNewIssueEvent =

    let create state cid =
      { State = state; Cid = cid }