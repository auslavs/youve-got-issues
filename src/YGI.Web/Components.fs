module YGI.Components
  open Giraffe.GiraffeViewEngine
  open Giraffe.GiraffeViewEngine.Accessibility
  open System

  type Page =
    | Index
    | Config
    | Login
    | Register

  let navbar activePage =

      let isActive (p:Page) =
          if p = activePage then "active" else String.Empty

      let href (p:Page) (link:string) =
          if p = activePage then _href "#" else _href link 

      nav [ _class "navbar navbar-expand-lg navbar-light bg-light"] [
          a [ _class "navbar-brand"; _href "#"] [ str "You've Got Issues" ]
          button [ _class "navbar-toggler"; _type "button"; _data "toggle" "collapse"; _data "target" "#navbarNav"; _ariaControls "navbarNav"; _ariaExpanded "false"; _ariaLabel "Toggle navigation" ] [
              span [ _class "navbar-toggler-icon"] []
          ]
          div [ _class "collapse navbar-collapse"; _id "navbarNav" ] [
              ul [ _class "navbar-nav" ] [
                  li [ _class ("nav-item " + (isActive Page.Index))  ] [
                      a [_class "nav-link"; href Page.Index "/" ]  [ str "Home"; span [_class "sr-only"] [ str "(current)"] ]
                  ]
                  li [ _class ("nav-item " + (isActive Config)) ] [
                      a [_class "nav-link"; href Config "/config" ] [ str "Config"]
                  ]
              ]
          ]
      ]

  let formTextbox id formName friendlyName =
    div [ _class "form-group"] [ 
      label [ _for id ] [ str friendlyName ]
      input [ _id id; _name formName; _class "form-control"; _type "text"; _required ] ]

  let formSelect id formName friendlyName optionLst =
    let lst = optionLst |> List.sort |> List.map (fun x -> option [] [ str x])

    div [ _class "form-group"] [ 
      label [ _for id ] [ str friendlyName ]
      select [ _id id; _name formName; _class "form-control"; _required ] lst ]

  let formFreeSelect id formName friendlyName optionLst =
    
    let lst = optionLst |> List.sort |> List.map (fun x -> option [] [ str x])
    let lstId = id + "-lst"
    div [ _class "form-group"] [ 
      label [ _for id ] [ str friendlyName ]

      input [ _id id; _name formName; _class "form-control"; _list lstId; _type "text"; _required ]
      datalist [_id lstId ] lst ]
      //select [ _id id; _name formName; _class "form-control"; _required ] lst ]

  let card header title text btnText = 
      div [ _class "card"; ] [
          div [ _class "card-header" ] [ str header]
          div [ _class "card-body"] [
              h5 [ _class "card-title"] [ str title ]
              p [ _class "card-text" ] [ str text ]
              a [ _href "#"; _class "btn btn-primary"] [str btnText ]
          ]
      ]

  let layout page (content: XmlNode list) =

    let bootstrapCss = "https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css"
    let jQuery       = "https://code.jquery.com/jquery-3.4.1.slim.min.js"
    let bootstrapJs  = "https://stackpath.bootstrapcdn.com/bootstrap/4.4.1/js/bootstrap.min.js"
    

    html [] [
        head [] [
            title []  [ encodedText "You've Got Issues" ]
            //link [ _rel "manifest"; _href "manifest.json" ]
            //link [ _rel "manifest"; _href "/manifest.webmanifest" ]
          
            link [ _rel "stylesheet"; _type "text/css"; _href bootstrapCss ]
            script [ _type "application/javascript"; _src jQuery; _crossorigin "anonymous" ] []
            script [ _type "application/javascript"; _src bootstrapJs; _crossorigin "anonymous" ] []
            //script [ _type "application/javascript"; _src "/js/bootstrap.bundle.min.js" ] []
            //script [] [rawText "if ('serviceWorker' in navigator) { navigator.serviceWorker.register('/sw.js'); }"] 
        ]
        body [] [ navbar page; div [ _class "container-fluid" ] content ]
    ]