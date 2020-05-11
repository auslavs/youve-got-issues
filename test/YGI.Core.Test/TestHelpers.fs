module TestHelpers

  open System

  let unpackResult result =
    match result with
    | Ok x -> x
    | Error err -> failwithf "%A" err

  let listEquals a b = List.fold (&&) true (List.zip a b |> List.map (fun (aa,bb) -> aa=bb))

  let ranStr n = 
    let r = Random()
    let chars = Array.concat([[|'a' .. 'z'|];[|'A' .. 'Z'|];[|'0' .. '9'|]])
    let sz = Array.length chars in
    String(Array.init n (fun _ -> chars.[r.Next sz]))