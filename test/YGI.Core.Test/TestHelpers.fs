module TestHelpers

  let unpackResult result =
    match result with
    | Ok x -> x
    | Error err -> failwithf "%A" err

  let listEquals a b = List.fold (&&) true (List.zip a b |> List.map (fun (aa,bb) -> aa=bb))