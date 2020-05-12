module YGI.Constants

  let [<Literal>] StorageConnStr = "AzureWebJobsStorage"
  let [<Literal>] ContainerId    = "youve-got-issues" 
  let [<Literal>] Summary        = "summary/summary.json"
  let [<Literal>] EventTable     = "events"

  let projectBlobPath projNum = projNum + "/"+ projNum + ".json"
  let projectAttachmentsPath projNum (issueNo:int) filename = 
    sprintf "%s/att/%i/%s" projNum issueNo filename
  
  module Claims = 
    let [<Literal>] GivenName  = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"
    let [<Literal>] Surname  = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname"
    let [<Literal>] Email = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
    let [<Literal>] Upn = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn"