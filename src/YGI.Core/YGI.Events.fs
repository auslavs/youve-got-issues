namespace YGI.Events

  open YGI.Common
  open YGI.Dto
  open Microsoft.Azure.Cosmos.Table
  open Newtonsoft.Json

  type YgiEvent =
    | AddNewProject of YgiEvent<NewProjectDto>
    | AddNewIssue of YgiEvent<NewIssueDto>
    | UpdateIssue of YgiEvent<IssueUpdateDto>

  type AddNewProjectEvent(evt:YgiEvent<NewProjectDto>) =
    inherit TableEntity(partitionKey=evt.ProjectNumber, rowKey=evt.Cid)
    new(evt) = AddNewProjectEvent(evt)
    member val EventType = "AddNewProject" with get,set
    member val Event = (JsonConvert.SerializeObject evt) with get,set

  type AddNewIssueEvent(evt:YgiEvent<NewIssueDto>) =
    inherit TableEntity(partitionKey=evt.ProjectNumber, rowKey=evt.Cid)
    new(evt) = AddNewIssueEvent(evt)
    member val EventType = "AddNewIssue" with get,set
    member val Event = (JsonConvert.SerializeObject evt) with get,set

  type UpdateIssueEvent(evt:YgiEvent<IssueUpdateDto>) =
    inherit TableEntity(evt.ProjectNumber, evt.Cid)
    new(evt) = UpdateIssueEvent(evt)
    member val EventType = "UpdateIssue" with get,set
    member val Event = (JsonConvert.SerializeObject evt) with get,set

  type AddNewCommentEvent(evt:YgiEvent<NewCommentDto>) =
    inherit TableEntity(evt.ProjectNumber, evt.Cid)
    new(evt) = AddNewCommentEvent(evt)
    member val EventType = "AddNewComment" with get,set
    member val Event = (JsonConvert.SerializeObject evt) with get,set

  type AddAttachementEvent(evt:YgiEvent<AttachmentDetailsDto []>) =
    inherit TableEntity(partitionKey=evt.ProjectNumber, rowKey=evt.Cid)
    new(evt) = AddAttachementEvent(evt)
    member val EventType = "AddAttachement" with get,set
    member val Event = (JsonConvert.SerializeObject evt) with get,set

  module YgiEvent =

    let create cid user projNum state  =
      { Cid = cid; User = user; ProjectNumber = projNum; State = state;  }