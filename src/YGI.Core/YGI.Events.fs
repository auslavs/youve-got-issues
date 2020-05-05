namespace YGI.Events

  open YGI.Common
  open YGI.Dto
  open Microsoft.Azure.Cosmos.Table

  type YgiEvent =
    | AddNewProject of YgiEvent<NewProjectDto>
    | AddNewIssue of YgiEvent<NewIssueDto>
    | UpdateIssue of YgiEvent<IssueUpdateDto>

  type AddNewProjectEvent(evt:YgiEvent<NewProjectDto>) =
    inherit TableEntity(partitionKey=evt.ProjectNumber, rowKey=evt.Cid)
    new() = AddNewProjectEvent()

  type AddNewIssueEvent(evt:YgiEvent<NewIssueDto>) =
    inherit TableEntity(partitionKey=evt.ProjectNumber, rowKey=evt.Cid)
    new() = AddNewIssueEvent()

  type UpdateIssueEvent(evt:YgiEvent<IssueUpdateDto>) =
    inherit TableEntity(partitionKey=evt.ProjectNumber, rowKey=evt.Cid)
    new() = UpdateIssueEvent()

  module YgiEvent =

    let create cid projNum state  =
      { Cid = cid; ProjectNumber = projNum; State = state;  }

  //module AddNewProjectEvent =

  //  let create cid projNum state  =
  //    { Cid = cid; ProjectNumber = projNum; State = state;  }

  //module AddNewIssueEvent =

  //  let create cid projNum state =
  //    { Cid = cid; ProjectNumber = projNum; State = state;  }

  //module UpdateIssueEvent =

  //  let create cid projNum state =
  //    { Cid = cid; ProjectNumber = projNum; State = state;  }