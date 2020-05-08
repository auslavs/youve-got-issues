﻿module YGI.Constants

  let [<Literal>] StorageConnStr = "AzureWebJobsStorage"
  let [<Literal>] ContainerId    = "youve-got-issues" 
  let [<Literal>] Summary        = "summary/summary.json"
  let [<Literal>] EventTable     = "events"

  let projectBlobPath projNum = projNum + "/"+ projNum + ".json"
  let projectAttachmentsPath projNum (issueNo:int) filename = 
    sprintf "%s/att/%i/%s" projNum issueNo filename
  
