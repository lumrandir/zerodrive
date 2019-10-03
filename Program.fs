// Learn more about F# at http://fsharp.org

open System
open Auth

[<EntryPoint>]
let main argv =
  { client_id = "623570b4-9363-4a1c-b5a9-f7328ad4e5d3"
  ; response_type = "code"
  ; redirect_uri = "http://localhost:8080"
  ; scope = "offline_access onedrive.readwrite"
  } |> Auth.print_auth_url
  0
